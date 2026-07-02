using ReciteHelper.Core.DTOs;
using ReciteHelper.Core.Aggregates;

namespace ReciteHelper.Core.Interfaces.Services;

public interface IProjectFileService
{
    Task<Project?> OpenProjectAsync(string projectPath);
    Task SaveProjectAsync(Project project);
    Task<ImportedProject> ImportProjectArchiveAsync(string archivePath, string destinationDirectory);
    Task<string> ExportProjectArchiveAsync(Project project, string destinationArchivePath, string? version);
    bool ProjectExists(string projectPath);
}
