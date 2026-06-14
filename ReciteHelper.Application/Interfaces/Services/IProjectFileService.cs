using ReciteHelper.Application.DTOs;
using ReciteHelper.Core.Aggregates;

namespace ReciteHelper.Application.Interfaces.Services;

public interface IProjectFileService
{
    Task<Project?> OpenProjectAsync(string projectPath);
    Task SaveProjectAsync(Project project);
    Task<ImportedProject> ImportProjectArchiveAsync(string archivePath);
    Task<string> ExportProjectArchiveAsync(Project project, string? version);
    bool ProjectExists(string projectPath);
}
