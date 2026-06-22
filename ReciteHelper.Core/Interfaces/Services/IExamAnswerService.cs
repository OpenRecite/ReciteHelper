using ReciteHelper.Core.Entities;

namespace ReciteHelper.Core.Interfaces.Services;

public interface IExamAnswerService
{
    bool IsCorrect(Question question, string? userAnswer);
}
