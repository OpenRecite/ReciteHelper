using ReciteHelper.Core.Interfaces.Services;
using ReciteHelper.Core.Entities;
using ReciteHelper.Infrastructure.Utilities;

namespace ReciteHelper.Infrastructure.Services;

public sealed class ExamAnswerService : IExamAnswerService
{
    public bool IsCorrect(Question question, string? userAnswer)
    {
        return JudgeAnswer.Run(question, userAnswer);
    }
}
