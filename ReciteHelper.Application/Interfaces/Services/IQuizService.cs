using ReciteHelper.Application.DTOs;

namespace ReciteHelper.Application.Interfaces.Services;

public interface IQuizService
{
    public Task<AnswerResult> ProcessAnswerAsync();
}
