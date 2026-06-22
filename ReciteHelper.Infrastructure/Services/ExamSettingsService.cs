using ReciteHelper.Core.Interfaces.Services;
using ReciteHelper.Core.Aggregates;
using ReciteHelper.Core.ValueObjects;
using System.Text.Json;

namespace ReciteHelper.Infrastructure.Services;

public sealed class ExamSettingsService : IExamSettingsService
{
    public async Task SaveAsync(Project project, ExamSettings settings)
    {
        if (project.StoragePath is null || project.ProjectName is null)
            throw new ArgumentException("Project storage path or project name is missing.");

        var projectFolder = Path.Combine(project.StoragePath, project.ProjectName);
        Directory.CreateDirectory(projectFolder);

        var settingsPath = Path.Combine(projectFolder, "exam_settings.json");
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });

        await File.WriteAllTextAsync(settingsPath, json);
    }
}
