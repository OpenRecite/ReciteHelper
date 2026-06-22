using ReciteHelper.Core.DTOs;
using ReciteHelper.Core.Entities;

namespace ReciteHelper.Core.Interfaces.Services;

public interface IQuizService
{
    public Task<AnswerResult> ProcessAnswerAsync(Question question,
        string userAnswer,
        DateTime startTime);
}
