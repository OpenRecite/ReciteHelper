using ReciteHelper.Application.DTOs;

namespace ReciteHelper.Application.Services;

internal interface IQuizService
{
    public Task<AnswerResult> ProcessAnswerAsync();
}
