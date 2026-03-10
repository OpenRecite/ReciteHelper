using ReciteHelper.Application.DTOs;
using ReciteHelper.Infrastructure.Configuration;

namespace ReciteHelper.Application.Services;

internal class QuizService : IQuizService
{
    private readonly IConfigService _configService;

    public QuizService(IConfigService configService)
    {
        _configService = configService;
    }

    public Task<AnswerResult> ProcessAnswerAsync()
    {
        throw new NotImplementedException();
    }
}
