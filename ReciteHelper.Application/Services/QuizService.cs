using ReciteHelper.Application.DTOs;
using ReciteHelper.Application.Interfaces.Services;
using ReciteHelper.Core.Interfaces.Services;
using ReciteHelper.Infrastructure.Configuration;

namespace ReciteHelper.Application.Services;

public class QuizService : IQuizService
{
    private readonly IConfigService _configService;
    private readonly IAnswerJudge _judgeService;
    private readonly ISuperMemoService _superMemoService;

    public QuizService(IConfigService configService, 
        IAnswerJudge judgeService, ISuperMemoService superMemoService)
    {
        _configService = configService;
        _judgeService = judgeService;
        _superMemoService = superMemoService;
    }

    public Task<AnswerResult> ProcessAnswerAsync()
    {
        throw new NotImplementedException();
    }
}
