using ReciteHelper.Core.Aggregates;
using ReciteHelper.Core.Scheduling;

namespace ReciteHelper.Core.Interfaces.Services;

/// <summary>
/// Fits project-specific scheduler parameters from the learner's own answer history
/// once enough evidence has accumulated.
/// </summary>
public interface IReviewPersonalizationService
{
    /// <summary>
    /// Returns the fit result when parameters were (re)fitted and applied to the project;
    /// null when the policy decided to keep the current parameters.
    /// </summary>
    Task<FitResult?> TryPersonalizeAsync(Project project, CancellationToken cancellationToken = default);
}
