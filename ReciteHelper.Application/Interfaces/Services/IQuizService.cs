using ReciteHelper.Application.DTOs;
using ReciteHelper.Core.Entities;

namespace ReciteHelper.Application.Interfaces.Services;

public interface IQuizService
{
    public Task<AnswerResult> ProcessAnswerAsync(Question question,
        string userAnswer,
        DateTime startTime);
}
