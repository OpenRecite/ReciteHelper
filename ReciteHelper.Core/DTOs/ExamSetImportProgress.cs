using ReciteHelper.Core.Enums;

namespace ReciteHelper.Core.DTOs;

public sealed record ExamSetImportProgress(
    ExamSetImportStage Stage,
    string Message,
    int? Completed = null,
    int? Total = null);
