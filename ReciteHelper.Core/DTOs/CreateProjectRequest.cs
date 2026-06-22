using ReciteHelper.Core.Enums;

namespace ReciteHelper.Core.DTOs;

public sealed record CreateProjectRequest(
    string ProjectName,
    string StoragePath,
    IReadOnlyList<string> QuestionBankPaths,
    string DeepSeekKey,
    MissingStrategy MissingStrategy);
