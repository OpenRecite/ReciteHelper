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
using System.Text.RegularExpressions;

namespace ReciteHelper.Infrastructure.Services;

public sealed partial class ProjectCreationService : IProjectCreationService
{
    private const int ChunkSize = 500;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IPromptProvider _promptProvider;
    private readonly IProjectFileService _projectFileService;
    private readonly IKnowledgeBaseService _knowledgeBaseService;

    public ProjectCreationService(
        IPromptProvider promptProvider,
        IProjectFileService projectFileService,
        IKnowledgeBaseService knowledgeBaseService)
    {
        _promptProvider = promptProvider;
        _projectFileService = projectFileService;
        _knowledgeBaseService = knowledgeBaseService;
    }

    public async Task<CreateProjectResult> CreateAsync(
        CreateProjectRequest request,
        IProgress<ProjectCreationProgress>? progress = null)
    {
        Report(progress, 1, 1, 0, 0, 1, 1, "正在读取题库文件...", ProjectCreationStage.ReadingText);

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
        var knowledgeBaseSourceText = string.Empty;

        var firstExtension = Path.GetExtension(copiedQuestionBanks.FirstOrDefault() ?? string.Empty);
        if (firstExtension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            knowledgeBaseSourceText = ExtractText.FromAutomatic(copiedQuestionBanks[0]);
            Report(progress, 1, 1, 0, 0, 1, 1, "题库文本读取完成。", ProjectCreationStage.ReadingText);
            project.Chapters = await ProcessTextAsync(
                knowledgeBaseSourceText,
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
                knowledgeBaseSourceText = string.Join(Environment.NewLine, mergeFile.Contents);
                foreach (var item in mergeFile.Contents)
                {
                    Report(progress, 1, 1, 0, 0, round, mergeFile.Contents.Count, "正在读取合并题库片段...", ProjectCreationStage.ReadingText);
                    var cluster = await ProcessTextAsync(item, request.DeepSeekKey, request.MissingStrategy, progress);
                    if (cluster is not null)
                        result.AddRange(cluster);
                    round++;
                }

                project.Chapters = result;
            }
        }

        if (project.Chapters is null || !project.Chapters.Any(chapter => chapter.Questions is { Count: > 0 }))
            throw new InvalidOperationException("项目创建失败：未能从题库生成任何题目，请检查 AI 返回内容或题库文本提取结果。");

        await BuildKnowledgeBaseAsync(project, projectDir, knowledgeBaseSourceText, progress);
        Report(progress, 1, 1, 1, 1, 1, 1, "项目创建完成。", ProjectCreationStage.Completed);

        await _projectFileService.SaveProjectAsync(project);

        return new CreateProjectResult(
            project,
            Path.Combine(projectDir, $"{request.ProjectName}.rhproj"));
    }

    private async Task BuildKnowledgeBaseAsync(
        Project project,
        string projectDir,
        string sourceText,
        IProgress<ProjectCreationProgress>? progress)
    {
        if (string.IsNullOrWhiteSpace(sourceText))
        {
            project.MarkKnowledgeBaseBuildFailed("题库文本为空，未构建知识库。");
            return;
        }

        Report(progress, 0, 1, 0, 1, null, null, "正在生成知识库向量...", ProjectCreationStage.VectorGeneration);

        const string knowledgeBaseFileName = "knowledge-base.json";
        var knowledgeBasePath = Path.Combine(projectDir, knowledgeBaseFileName);

        try
        {
            var store = await _knowledgeBaseService.Build(knowledgeBasePath, sourceText);
            project.AttachKnowledgeBase(knowledgeBaseFileName, store);
            Report(progress, 1, 1, 1, 1, null, null, "知识库向量生成完成。", ProjectCreationStage.VectorGeneration);
        }
        catch (Exception ex)
        {
            if (File.Exists(knowledgeBasePath))
                File.Delete(knowledgeBasePath);

            project.MarkKnowledgeBaseBuildFailed(ex.Message);
            Debug.WriteLine($"Failed to build knowledge base: {ex}");
            Report(progress, 1, 1, 1, 1, null, null, "知识库构建失败，项目将继续创建。", ProjectCreationStage.VectorGeneration);
        }
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
        var scanTotal = (int)Math.Ceiling(text.Length / (double)ChunkSize);
        Report(progress, 0, scanTotal, 0, scanTotal, null, null, "你的资料已经被切割完成，正在分块发送至 AI 生成知识点和相关题目。", ProjectCreationStage.KnowledgeExtraction);

        return await ClusterQuestionsAsync(text, deepSeekKey, missingStrategy, progress);
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
                var jsonContent = ExtractJsonContent(result.Messages.LastOrDefault(x => x.Content is not null)?.Content);
                if (string.IsNullOrWhiteSpace(jsonContent))
                    return;

                var chapter = JsonSerializer.Deserialize<List<Chapter>>(jsonContent, JsonOptions);
                if (chapter is { Count: > 0 })
                {
                    NormalizeGeneratedQuestions(chapter);
                    allChapter.Add(chapter);
                }

                succeededIndexes.Add(chunk.Index);

                var current = Interlocked.Increment(ref progressValue);
                Report(progress, current, chunks.Count, current, chunks.Count, null, null, $"已完成 {current}/{chunks.Count} 个文本块。", ProjectCreationStage.KnowledgeExtraction);
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

    private static string ExtractJsonContent(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return string.Empty;

        var trimmed = content.Trim();
        var fenceStart = trimmed.IndexOf("```", StringComparison.Ordinal);
        if (fenceStart >= 0)
        {
            var jsonStart = trimmed.IndexOf('\n', fenceStart);
            if (jsonStart >= 0)
            {
                var fenceEnd = trimmed.LastIndexOf("```", StringComparison.Ordinal);
                trimmed = fenceEnd > jsonStart
                    ? trimmed[(jsonStart + 1)..fenceEnd].Trim()
                    : trimmed[(jsonStart + 1)..].Trim();
            }
        }

        var arrayStart = trimmed.IndexOf('[');
        var arrayEnd = trimmed.LastIndexOf(']');
        if (arrayStart >= 0 && arrayEnd > arrayStart)
            return trimmed[arrayStart..(arrayEnd + 1)].Trim();

        var objectStart = trimmed.IndexOf('{');
        var objectEnd = trimmed.LastIndexOf('}');
        if (objectStart >= 0 && objectEnd > objectStart)
            return trimmed[objectStart..(objectEnd + 1)].Trim();

        return trimmed;
    }

    private static void NormalizeGeneratedQuestions(List<Chapter> chapters)
    {
        foreach (var chapter in chapters)
        {
            if (chapter.Questions is null || chapter.Questions.Count == 0)
                continue;

            RepairSplitChoiceOptions(chapter.Questions);
            NormalizeMalformedChoiceQuestions(chapter.Questions);
            NormalizeGeneratedQuestionTypes(chapter.Questions);
            RemoveDeclarativeShortAnswerQuestions(chapter);
        }
    }

    private static void RemoveDeclarativeShortAnswerQuestions(Chapter chapter)
    {
        if (chapter.Questions is null || chapter.Questions.Count == 0)
            return;

        chapter.KnowledgePoints ??= [];
        var validQuestions = new List<Question>();

        foreach (var question in chapter.Questions)
        {
            question.Text = question.Text?.Trim();
            question.CorrectAnswer = question.CorrectAnswer?.Trim();

            if (string.IsNullOrWhiteSpace(question.Text))
                continue;

            if (question.IsSingleChoice || IsValidGeneratedQuestion(question))
            {
                validQuestions.Add(question);
                continue;
            }

            if (TryConvertDeclarativeSentenceToBlank(question))
            {
                validQuestions.Add(question);
                continue;
            }

            chapter.KnowledgePoints.Add(KnowledgePoint.Create(
                CreateKnowledgePointName(question.Text),
                question.Text));
        }

        chapter.Questions = validQuestions;
    }

    private static bool IsValidGeneratedQuestion(Question question)
    {
        if (string.IsNullOrWhiteSpace(question.CorrectAnswer) && question.GetCorrectAnswers().Count == 0)
            return false;

        return question.Type switch
        {
            QuestionType.FillBlank =>
                BlankRegex().Matches(question.Text ?? string.Empty).Count is var blankCount &&
                blankCount > 0 &&
                blankCount == question.GetCorrectAnswers().Count,
            QuestionType.TermDefinition => (question.Text ?? string.Empty).StartsWith("名词解释", StringComparison.Ordinal),
            QuestionType.Essay => IsValidShortAnswerStem(question.Text ?? string.Empty),
            _ => false
        };
    }

    private static bool IsValidShortAnswerStem(string text)
    {
        if (BlankRegex().IsMatch(text) || text.Contains('?') || text.Contains('？'))
            return true;

        return ShortAnswerPromptRegex().IsMatch(text);
    }

    private static bool TryConvertDeclarativeSentenceToBlank(Question question)
    {
        var text = question.Text?.Trim();
        var answer = question.CorrectAnswer?.Trim();
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(answer))
            return false;

        if (string.Equals(text, answer, StringComparison.OrdinalIgnoreCase))
            return false;

        var index = text.IndexOf(answer, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
            return false;

        var remainingLength = text.Length - answer.Length;
        if (answer.Length < 2 || remainingLength < 4)
            return false;

        question.Text = text.Remove(index, answer.Length).Insert(index, "________");
        question.Type = QuestionType.FillBlank;
        question.CorrectAnswers = [answer];
        return IsValidShortAnswerStem(question.Text);
    }

    private static void NormalizeGeneratedQuestionTypes(List<Question> questions)
    {
        questions.RemoveAll(question => question.Type == QuestionType.TrueFalse);

        foreach (var question in questions)
        {
            question.CorrectAnswer = question.CorrectAnswer?.Trim();
            question.CorrectAnswers = question.CorrectAnswers
                .Where(answer => !string.IsNullOrWhiteSpace(answer))
                .Select(answer => answer.Trim())
                .ToList();

            if (question.Type == QuestionType.FillBlank && question.CorrectAnswers.Count == 0 &&
                !string.IsNullOrWhiteSpace(question.CorrectAnswer))
            {
                question.CorrectAnswers = [question.CorrectAnswer];
            }

            if (question.Type == QuestionType.TermDefinition &&
                !string.IsNullOrWhiteSpace(question.Text) &&
                !question.Text.StartsWith("名词解释", StringComparison.Ordinal))
            {
                question.Text = $"名词解释：{question.Text.Trim().TrimEnd('。', '？', '?')}";
            }

            if (question.Type != QuestionType.SingleChoice)
            {
                question.Options = [];
                question.CorrectOptionIds = [];
            }

            if (question.Type != QuestionType.FillBlank)
                question.CorrectAnswers = [];
        }
    }

    private static string CreateKnowledgePointName(string text)
    {
        var normalized = WhitespaceRegex().Replace(text, " ").Trim();
        return normalized.Length <= 32 ? normalized : normalized[..32];
    }

    private static void RepairSplitChoiceOptions(List<Question> questions)
    {
        for (var i = 0; i <= questions.Count - 5; i++)
        {
            var stem = questions[i];
            if (stem.Options.Count > 0 || LooksLikeOptionOnlyQuestion(stem, null, out _))
                continue;

            var parsedOptions = new List<QuestionOption>();
            var expectedIds = new[] { "A", "B", "C", "D" };
            var matched = true;

            for (var offset = 0; offset < expectedIds.Length; offset++)
            {
                var optionQuestion = questions[i + offset + 1];
                if (!LooksLikeOptionOnlyQuestion(optionQuestion, expectedIds[offset], out var optionText))
                {
                    matched = false;
                    break;
                }

                parsedOptions.Add(new QuestionOption
                {
                    Id = expectedIds[offset],
                    Text = optionText
                });
            }

            if (!matched)
                continue;

            var correctOptionIds = ResolveCorrectOptionIds(stem, parsedOptions);
            if (correctOptionIds.Count == 0)
                continue;

            stem.Type = QuestionType.SingleChoice;
            stem.Options = parsedOptions;
            stem.CorrectOptionIds = correctOptionIds;
            stem.CorrectAnswer = correctOptionIds[0];

            questions.RemoveRange(i + 1, 4);
        }
    }

    private static void NormalizeMalformedChoiceQuestions(List<Question> questions)
    {
        foreach (var question in questions.Where(question => question.Type == QuestionType.SingleChoice))
        {
            question.Options = question.Options
                .Where(option => !string.IsNullOrWhiteSpace(option.Id) && !string.IsNullOrWhiteSpace(option.Text))
                .GroupBy(option => QuestionOption.NormalizeId(option.Id))
                .Select(group => new QuestionOption
                {
                    Id = group.Key,
                    Text = group.First().Text.Trim()
                })
                .ToList();

            question.CorrectOptionIds = ResolveCorrectOptionIds(question, question.Options);

            if (question.Options.Count == 0 || question.CorrectOptionIds.Count == 0)
            {
                question.Type = QuestionType.Essay;
                question.Options = [];
                question.CorrectOptionIds = [];
            }
        }
    }

    private static List<string> ResolveCorrectOptionIds(Question question, List<QuestionOption> options)
    {
        var ids = question.GetCorrectOptionIds()
            .Where(id => options.Any(option => QuestionOption.NormalizeId(option.Id) == id))
            .ToList();

        if (ids.Count > 0)
            return ids;

        var correctAnswer = question.CorrectAnswer?.Trim();
        if (string.IsNullOrWhiteSpace(correctAnswer))
            return [];

        var matchingOption = options.FirstOrDefault(option =>
            string.Equals(option.Text.Trim(), correctAnswer, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(option.DisplayText.Trim(), correctAnswer, StringComparison.OrdinalIgnoreCase));

        return matchingOption is null ? [] : [QuestionOption.NormalizeId(matchingOption.Id)];
    }

    private static bool LooksLikeOptionOnlyQuestion(Question question, string? expectedId, out string optionText)
    {
        optionText = string.Empty;
        if (!string.IsNullOrWhiteSpace(question.CorrectAnswer) || question.Options.Count > 0)
            return false;

        var match = OptionOnlyRegex().Match(question.Text ?? string.Empty);
        if (!match.Success)
            return false;

        var optionId = QuestionOption.NormalizeId(match.Groups["id"].Value);
        if (!string.IsNullOrWhiteSpace(expectedId) && optionId != expectedId)
            return false;

        optionText = match.Groups["text"].Value.Trim();
        return !string.IsNullOrWhiteSpace(optionText);
    }

    [GeneratedRegex(@"^\s*\(?\s*(?<id>[A-Da-d])\s*\)?\s*[\.、:：\)]\s*(?<text>.+?)\s*$")]
    private static partial Regex OptionOnlyRegex();

    [GeneratedRegex(@"_{2,}|＿{2,}|-{3,}")]
    private static partial Regex BlankRegex();

    [GeneratedRegex(@"^\s*(名词解释|简述|说明|分析|比较|阐述|试述|论述|列举|举例|概括|描述|解释|指出|写出|回答|判断|计算|请|问)|(为什么|为何|如何|怎样|哪些|哪种|哪个|什么|是否|能否)")]
    private static partial Regex ShortAnswerPromptRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

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
        if (allChapter.Count == 0)
            throw new InvalidOperationException("AI 未返回可解析的章节题目 JSON。");

        var chapterNames = new List<string>();
        foreach (var chapter in allChapter)
        {
            foreach (var seg in chapter)
            {
                if (seg.Name is not null)
                    chapterNames.Add(seg.Name);
            }
        }

        Report(progress, null, null, 0, chapterNames.Count, null, null, "正在合并相似章节并整理题目结构。", ProjectCreationStage.TextClustering);
        if (chapterNames.Count == 0)
            return FlattenGeneratedChapters(allChapter);

        var prompt = await _promptProvider.GetPromptAsync("GenerateChapter.txt");
        var clusterResult = await agent.Run($"{prompt}\n{string.Join(' ', chapterNames)}");
        var jsonContent = ExtractJsonContent(clusterResult.Messages.LastOrDefault(x => x.Content is not null)?.Content);
        var cluster = string.IsNullOrWhiteSpace(jsonContent)
            ? []
            : JsonSerializer.Deserialize<List<ChapterCluster>>(jsonContent, JsonOptions) ?? [];

        if (cluster.Count == 0)
            return FlattenGeneratedChapters(allChapter);

        for (var clusterIndex = 0; clusterIndex < cluster.Count; clusterIndex++)
        {
            var single = cluster[clusterIndex];
            Report(progress, null, null, clusterIndex + 1, cluster.Count, null, null, "正在写入聚类后的章节。", ProjectCreationStage.TextClustering);
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

    private static List<Chapter> FlattenGeneratedChapters(List<List<Chapter>> generatedChapters)
    {
        var chapters = new List<Chapter>();

        foreach (var chapterGroup in generatedChapters)
        {
            foreach (var segment in chapterGroup)
            {
                if (segment.Questions is null && segment.KnowledgePoints is null)
                    continue;

                var chapterName = string.IsNullOrWhiteSpace(segment.Name) ? "杂项题目" : segment.Name;
                var current = chapters.FirstOrDefault(chapter => chapter.Name == chapterName);
                if (current is null)
                {
                    current = new Chapter
                    {
                        Name = chapterName,
                        Number = chapters.Count + 1,
                        Questions = [],
                        KnowledgePoints = []
                    };
                    chapters.Add(current);
                }

                if (segment.Questions is not null)
                    current.Questions!.AddRange(segment.Questions);

                if (segment.KnowledgePoints is not null)
                    current.KnowledgePoints!.AddRange(segment.KnowledgePoints);
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
        string? label = null,
        ProjectCreationStage stage = ProjectCreationStage.KnowledgeExtraction)
    {
        progress?.Report(new ProjectCreationProgress(
            scanCurrent ?? 0,
            scanTotal ?? 0,
            clusterCurrent ?? 0,
            clusterTotal ?? 0,
            roundCurrent ?? 0,
            roundTotal ?? 0,
            label,
            stage));
    }
}
