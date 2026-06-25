using ReciteHelper.Core.DTOs;
using ReciteHelper.Core.Interfaces.Services;
using ReciteHelper.Core.Aggregates;
using ReciteHelper.Core.ValueObjects;
using ReciteHelper.Infrastructure.Utilities;
using System.IO.Compression;
using System.Text.Json;

namespace ReciteHelper.Infrastructure.Services;

public sealed class ProjectFileService : IProjectFileService
{
    public bool ProjectExists(string projectPath)
    {
        return File.Exists(projectPath);
    }

    public async Task<Project?> OpenProjectAsync(string projectPath)
    {
        if (!File.Exists(projectPath))
            return null;

        await using var stream = File.OpenRead(projectPath);
        var project = await JsonSerializer.DeserializeAsync<Project>(stream);
        project?.UpdateLastAccessed();

        if (project?.KnowledgeBasePath is not null)
        {
            var knowledgeBasePath = ResolveProjectRelativePath(projectPath, project.KnowledgeBasePath);
            if (File.Exists(knowledgeBasePath))
                project.LoadKnowledgeBase(new FileVectorStore(knowledgeBasePath));
        }

        return project;
    }

    public async Task SaveProjectAsync(Project project)
    {
        if (project.StoragePath is null || project.ProjectName is null)
            throw new ArgumentException("Project storage path or project name is missing.");

        var projectPath = Path.Combine(project.StoragePath, project.ProjectName, $"{project.ProjectName}.rhproj");
        Directory.CreateDirectory(Path.GetDirectoryName(projectPath)!);

        var json = JsonSerializer.Serialize(project, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(projectPath, json);
    }

    public async Task<ImportedProject> ImportProjectArchiveAsync(string archivePath)
    {
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var tempFolder = Path.Combine(baseDirectory, "temp");

        Directory.CreateDirectory(tempFolder);
        Directory.Clear(tempFolder);
        ZipFile.ExtractToDirectory(archivePath, tempFolder);

        var manifestPath = Path.Combine(tempFolder, "manifest.json");
        await using var manifestStream = File.OpenRead(manifestPath);
        var manifest = await JsonSerializer.DeserializeAsync<Manifest>(manifestStream);

        if (manifest?.ProjectFile is null)
            throw new ArgumentException("Incomplete manifest file.");

        var importedFileName = manifest.ProjectFile.Replace("_exp", "");
        var exactFolder = Path.Combine(baseDirectory, "imports", Path.GetFileNameWithoutExtension(manifest.ProjectFile));
        Directory.CreateDirectory(exactFolder);

        var sourcePath = Path.Combine(tempFolder, manifest.ProjectFile);
        var destinationPath = Path.Combine(exactFolder, importedFileName);
        File.Copy(sourcePath, destinationPath, true);

        if (!string.IsNullOrWhiteSpace(manifest.KnowledgeBaseFile))
        {
            var sourceKnowledgeBasePath = Path.Combine(tempFolder, manifest.KnowledgeBaseFile);
            if (File.Exists(sourceKnowledgeBasePath))
            {
                var destinationKnowledgeBasePath = Path.Combine(exactFolder, Path.GetFileName(manifest.KnowledgeBaseFile));
                File.Copy(sourceKnowledgeBasePath, destinationKnowledgeBasePath, true);
            }
        }

        return new ImportedProject(destinationPath, importedFileName);
    }

    public async Task<string> ExportProjectArchiveAsync(Project project, string? version)
    {
        if (project.StoragePath is null || project.ProjectName is null)
            throw new ArgumentException("Project storage path or project name is missing.");

        var folderPath = Path.Combine(project.StoragePath, project.ProjectName);
        var outputFolderPath = Path.Combine(folderPath, "output");
        Directory.CreateDirectory(outputFolderPath);
        Directory.Clear(outputFolderPath);

        var exportProjectFileName = $"{project.ProjectName}_exp.rhproj";
        var manifest = Manifest.Create(
            project.QuestionBankPath,
            version,
            exportProjectFileName,
            project.KnowledgeBasePath);

        var manifestString = JsonSerializer.Serialize(
            manifest,
            new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(Path.Combine(outputFolderPath, "manifest.json"), manifestString);

        var sourceProjectPath = Path.Combine(folderPath, $"{project.ProjectName}.rhproj");
        var exportProjectPath = Path.Combine(outputFolderPath, exportProjectFileName);
        File.Copy(sourceProjectPath, exportProjectPath, true);

        if (!string.IsNullOrWhiteSpace(project.KnowledgeBasePath))
        {
            var sourceKnowledgeBasePath = ResolveProjectRelativePath(sourceProjectPath, project.KnowledgeBasePath);
            if (File.Exists(sourceKnowledgeBasePath))
            {
                var exportKnowledgeBasePath = Path.Combine(outputFolderPath, Path.GetFileName(project.KnowledgeBasePath));
                File.Copy(sourceKnowledgeBasePath, exportKnowledgeBasePath, true);
            }
        }

        await ResetExportedAnswerStatusAsync(exportProjectPath);

        var archivePath = Path.Combine(folderPath, "rh_output.zip");
        if (File.Exists(archivePath))
            File.Delete(archivePath);

        await ZipFile.CreateFromDirectoryAsync(outputFolderPath, archivePath);
        return archivePath;
    }

    private static async Task ResetExportedAnswerStatusAsync(string exportProjectPath)
    {
        Project? project;
        await using (var readStream = File.OpenRead(exportProjectPath))
        {
            project = await JsonSerializer.DeserializeAsync<Project>(readStream);
        }

        if (project?.Chapters is null)
            return;

        foreach (var chapter in project.Chapters)
        {
            if (chapter.Questions is null)
                continue;

            chapter.Questions.ForEach(question => question.Status = null);
        }

        var clearText = JsonSerializer.Serialize(project, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(exportProjectPath, clearText);
    }

    private static string ResolveProjectRelativePath(string projectPath, string path)
    {
        return Path.IsPathRooted(path)
            ? path
            : Path.Combine(Path.GetDirectoryName(projectPath)!, path);
    }
}
