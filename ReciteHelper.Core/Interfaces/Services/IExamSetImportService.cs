using ReciteHelper.Core.Aggregates;
using ReciteHelper.Core.DTOs;

namespace ReciteHelper.Core.Interfaces.Services;

public interface IExamSetImportService
{
    Task<IReadOnlyList<ExamSet>> ImportAsync(
        Project project,
        string sourceFilePath,
        string deepSeekKey,
        IProgress<ExamSetImportProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
