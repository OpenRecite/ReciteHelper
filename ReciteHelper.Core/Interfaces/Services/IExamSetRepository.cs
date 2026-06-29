using ReciteHelper.Core.Aggregates;

namespace ReciteHelper.Core.Interfaces.Services;

public interface IExamSetRepository
{
    Task SaveAsync(Project project, ExamSet examSet, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExamSet>> LoadAllAsync(Project project, CancellationToken cancellationToken = default);
}
