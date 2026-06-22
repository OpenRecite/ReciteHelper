using ReciteHelper.Core.DTOs;

namespace ReciteHelper.Core.Interfaces.Services;

public interface IProjectCreationService
{
    Task<CreateProjectResult> CreateAsync(
        CreateProjectRequest request,
        IProgress<ProjectCreationProgress>? progress = null);
}
