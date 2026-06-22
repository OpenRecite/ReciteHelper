using LlmTornado;
using LlmTornado.Agents;
using LlmTornado.Chat.Models;
using ReciteHelper.Core.DTOs;
using ReciteHelper.Core.Interfaces.Services;
using ReciteHelper.Core.Aggregates;
using ReciteHelper.Core.Entities;
using ReciteHelper.Core.Enums;
using ReciteHelper.Core.ValueObjects;
using ReciteHelper.Infrastructure.Utilities;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;

namespace ReciteHelper.Infrastructure.Services;

public sealed class ProjectCreationService : IProjectCreationService
{
    private const int ChunkSize = 500;
    private readonly IPromptProvider _promptProvider;
    private readonly IProjectFileService _projectFileService;

    public ProjectCreationService(IPromptProvider promptProvider, IProjectFileService projectFileService)
    {
        _promptProvider = promptProvider;
        _projectFileService = projectFileService;
    }

    public async Task<CreateProjectResult> CreateAsync(
        CreateProjectRequest request,
        IProgress<ProjectCreationProgress>? progress = null)
    {
        var projectDir = Path.Combine(request.StoragePath, request.ProjectName);
        Directory.CreateDirectory(projectDir);

        var copiedQuestionBanks = CopyQuestionBanks(request.QuestionBankPaths, projectDir);
        var project = new Project
        {
            ProjectName = request.ProjectName,
            QuestionBankPath = string.Join(';', copiedQuestionBanks) + ";",
            Chapters = [],
            StoragePath = request.StoragePath
        };

        var firstExtension = Path.GetExtension(copiedQuestionBanks.FirstOrDefault() ?? string.Empty);
        if (firstExtension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            Report(progress, 1, 1, 0, 0, 1, 1);
            project.Chapters = await ProcessTextAsync(
                ExtractText.FromAutomatic(copiedQuestionBanks[0]),
                request.DeepSeekKey,
                request.MissingStrategy,
                progress);
        }
        else if (firstExtension.Equals(".meg", StringComparison.OrdinalIgnoreCase))
        {
            var mergeFile = (MergeFile)ExtractText.FromAutomatic(copiedQuestionBanks[0]);
            if (mergeFile is null)
                throw new InvalidOperationException("题库文件已损坏！");

            if (mergeFile.ClusterType == FileClusterType.Sequential)
            {
                var result = new List<Chapter>();
                var round = 1;
                foreach (var item in mergeFile.Contents)
                {
                    Report(progress, 1, 1, 0, 0, round, mergeFile.Contents.Count);
                    var cluster = await ProcessTextAsync(item, request.DeepSeekKey, request.MissingStrategy, progress);
                    if (cluster is not null)
                        result.AddRange(cluster);
                    round++;
                }

                project.Chapters = result;
            }
        }

        await _projectFileService.SaveProjectAsync(project);

        return new CreateProjectResult(
            project,
            Path.Combine(projectDir, $"{request.ProjectName}.rhproj"));
    }

    private static List<string> CopyQuestionBanks(IEnumerable<string> questionBankPaths, string projectDir)
    {
        var copied = new List<string>();

        foreach (var bankPath in questionBankPaths.Where(path => !string.IsNullOrWhiteSpace(path)))
        {
            var destination = Path.Combine(projectDir, Path.GetFileName(bankPath));
            File.Copy(bankPath, destination, true);
            copied.Add(destination);
        }

        return copied;
    }

    private async Task<List<Chapter>?> ProcessTextAsync(
        string text,
        string deepSeekKey,
        MissingStrategy missingStrategy,
        IProgress<ProjectCreationProgress>? progress)
    {
        try
        {
            var scanTotal = (int)Math.Ceiling(text.Length / (double)ChunkSize);
            Report(progress, 1, scanTotal, 0, scanTotal, null, null);

            return await ClusterQuestionsAsync(text, deepSeekKey, missingStrategy, progress);
        }
        catch
        {
            return null;
        }
    }

    private static TornadoAgent BuildAgent(string deepSeekKey, string? instructions = null)
    {
        var api = new TornadoApi(deepSeekKey);

        return new TornadoAgent(
            client: api,
            model: ChatModel.DeepSeek.Models.Chat,
            name: "ArchitectBot",
            instructions: instructions ?? "You are an assistant who is good at extracting knowledge.");
    }

    private static List<Chunk> BuildChunks(string text)
    {
        var totalChunks = (int)Math.Ceiling(text.Length / (double)ChunkSize);
        var chunks = new List<Chunk>();

        for (var i = 0; i < totalChunks; i++)
        {
            var startIndex = i * ChunkSize;
            var length = Math.Min(ChunkSize, text.Length - startIndex);
            chunks.Add(Chunk.Create(text.Substring(startIndex, length), false, i));
        }

        return chunks;
    }

    private async Task<Replay> SendChunksAsync(
        List<Chunk> chunks,
        string deepSeekKey,
        IProgress<ProjectCreationProgress>? progress)
    {
        var sendChunks = chunks;
        var agent = BuildAgent(deepSeekKey);
        var allChapter = new ConcurrentBag<List<Chapter>>();
        var succeededIndexes = new ConcurrentBag<int>();
        var progressValue = 0;
        var prompt = await _promptProvider.GetPromptAsync("GenerateQuestion.txt");

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = 16
        };

        await Parallel.ForEachAsync(chunks, parallelOptions, async (chunk, _) =>
        {
            try
            {
                var result = await agent.Run($"{prompt}\n{chunk.Content}");
                var jsonContent = string.Empty;

                foreach (var item in result.Messages)
                {
                    if (item.Content is null)
                        continue;

                    if (item.Content.Contains("```json"))
                    {
                        jsonContent = result.Messages.Last().Content!
                            .Replace("`", "")
                            .Replace("json", "")
                            .Trim();
                        break;
                    }
                }

                var chapter = JsonSerializer.Deserialize<List<Chapter>>(jsonContent);
                if (chapter is { Count: > 0 })
                    allChapter.Add(chapter);

                succeededIndexes.Add(chunk.Index);

                var current = Interlocked.Increment(ref progressValue);
                Report(progress, current + 1, chunks.Count, current + 1, chunks.Count, null, null);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to generate chunk: {ex.Message}");
            }
        });

        var succeeded = succeededIndexes.ToHashSet();
        sendChunks = sendChunks
            .Select(chunk => succeeded.Contains(chunk.Index) ? chunk.MarkAsSucceed() : chunk)
            .ToList();

        return Replay.Create(sendChunks, allChapter);
    }

    private async Task<List<List<Chapter>>> MergeChunksAsync(
        List<Chunk> chunks,
        string deepSeekKey,
        MissingStrategy missingStrategy,
        IProgress<ProjectCreationProgress>? progress)
    {
        var result = new List<List<Chapter>>();

        while (true)
        {
            var replay = await SendChunksAsync(chunks, deepSeekKey, progress);
            var failed = replay.Chunks.Where(chunk => !chunk.IsSuccess).ToList();

            result.AddRange(replay.Chapters);
            if (failed.Count == 0 || missingStrategy == MissingStrategy.Ignore)
                break;

            chunks = [.. failed];
        }

        return result;
    }

    private async Task<List<Chapter>> ClusterQuestionsAsync(
        string text,
        string deepSeekKey,
        MissingStrategy missingStrategy,
        IProgress<ProjectCreationProgress>? progress)
    {
        var agent = BuildAgent(deepSeekKey);
        var chunks = BuildChunks(text);
        var chapters = new List<Chapter>();

        var allChapter = await MergeChunksAsync(chunks, deepSeekKey, missingStrategy, progress);
        var chapterNames = new List<string>();
        foreach (var chapter in allChapter)
        {
            foreach (var seg in chapter)
            {
                if (seg.Name is not null)
                    chapterNames.Add(seg.Name);
            }
        }

        Report(progress, null, null, null, null, null, null, "分块聚类中...");

        var prompt = await _promptProvider.GetPromptAsync("GenerateChapter.txt");
        var clusterResult = await agent.Run($"{prompt}\n{string.Join(' ', chapterNames)}");
        var jsonContent = clusterResult.Messages.Last().Content!.Replace("`", "").Replace("json", "").Trim();
        var cluster = JsonSerializer.Deserialize<List<ChapterCluster>>(jsonContent)
            ?? throw new InvalidOperationException("章节聚类结果无法解析。");

        foreach (var single in cluster)
        {
            foreach (var individual in allChapter)
            {
                foreach (var seg in individual)
                {
                    if (single.Chapters is null || seg.Name is null || !single.Chapters.Contains(seg.Name))
                        continue;

                    if (!chapters.Select(c => c.Name).Contains(single.UnifiedName))
                    {
                        chapters.Add(new Chapter
                        {
                            Name = single.UnifiedName,
                            Number = single.Number,
                            Questions = [],
                            KnowledgePoints = []
                        });
                    }

                    var current = chapters.Find(c => c.Name == single.UnifiedName)!;

                    if (seg.Questions is not null)
                        current.Questions!.AddRange(seg.Questions);

                    if (seg.KnowledgePoints is not null)
                        current.KnowledgePoints!.AddRange(seg.KnowledgePoints);
                }
            }
        }

        return chapters;
    }

    private static void Report(
        IProgress<ProjectCreationProgress>? progress,
        int? scanCurrent,
        int? scanTotal,
        int? clusterCurrent,
        int? clusterTotal,
        int? roundCurrent,
        int? roundTotal,
        string? label = null)
    {
        progress?.Report(new ProjectCreationProgress(
            scanCurrent ?? 0,
            scanTotal ?? 0,
            clusterCurrent ?? 0,
            clusterTotal ?? 0,
            roundCurrent ?? 0,
            roundTotal ?? 0,
            label));
    }
}
