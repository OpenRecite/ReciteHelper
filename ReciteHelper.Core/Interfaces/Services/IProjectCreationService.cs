using ReciteHelper.Core.DTOs;
using ReciteHelper.Core.Aggregates;
using ReciteHelper.Core.Entities;
using ReciteHelper.Core.Enums;

namespace ReciteHelper.Core.Interfaces.Services;

public interface IProjectCreationService
{
    Task<CreateProjectResult> CreateAsync(
        CreateProjectRequest request,
        IProgress<ProjectCreationProgress>? progress = null);

    Task AppendSourcesAsync(
        Project project,
        IReadOnlyList<string> sourcePaths,
        string deepSeekKey,
        MissingStrategy missingStrategy,
        IProgress<ProjectCreationProgress>? progress = null);

    Task ImportQuestionsAsync(
        Project project,
        IReadOnlyList<Question> questions,
        string deepSeekKey,
        IProgress<ProjectCreationProgress>? progress = null);
}
