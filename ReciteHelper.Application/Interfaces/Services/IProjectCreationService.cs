using ReciteHelper.Application.DTOs;

namespace ReciteHelper.Application.Interfaces.Services;

public interface IProjectCreationService
{
    Task<CreateProjectResult> CreateAsync(
        CreateProjectRequest request,
        IProgress<ProjectCreationProgress>? progress = null);
}
