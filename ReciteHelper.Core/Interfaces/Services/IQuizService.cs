using ReciteHelper.Core.Aggregates;
using ReciteHelper.Core.DTOs;
using ReciteHelper.Core.Entities;

namespace ReciteHelper.Core.Interfaces.Services;

public interface IQuizService
{
    /// <summary>
    /// Judges the answer, records the review against the memory model and appends a review tag
    /// to the question.  The question entity is updated in place.
    /// </summary>
    Task<AnswerResult> ProcessAnswerAsync(Project project, Question question, string userAnswer, DateTime startTime);
}
