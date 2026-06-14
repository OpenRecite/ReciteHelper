using ReciteHelper.Core.Entities;

namespace ReciteHelper.Application.Interfaces.Services;

public interface IExamAnswerService
{
    bool IsCorrect(Question question, string? userAnswer);
}
