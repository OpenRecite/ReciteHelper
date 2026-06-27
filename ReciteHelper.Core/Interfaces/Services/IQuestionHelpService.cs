using ReciteHelper.Core.Aggregates;
using ReciteHelper.Core.DTOs;
using ReciteHelper.Core.Entities;

namespace ReciteHelper.Core.Interfaces.Services;

public interface IQuestionHelpService
{
    Task<IReadOnlyList<KnowledgeBaseMatch>> FindMatchesAsync(
        Project project,
        Question question,
        CancellationToken cancellationToken = default);

    Task<string> ExplainAsync(
        Question question,
        string userAnswer,
        IReadOnlyList<KnowledgeBaseMatch> matches,
        CancellationToken cancellationToken = default);
}
