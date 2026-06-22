using ReciteHelper.Core.DTOs;
using ReciteHelper.Core.Aggregates;

namespace ReciteHelper.Core.Interfaces.Services;

public interface IProjectFileService
{
    Task<Project?> OpenProjectAsync(string projectPath);
    Task SaveProjectAsync(Project project);
    Task<ImportedProject> ImportProjectArchiveAsync(string archivePath);
    Task<string> ExportProjectArchiveAsync(Project project, string? version);
    bool ProjectExists(string projectPath);
}
