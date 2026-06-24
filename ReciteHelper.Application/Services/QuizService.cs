using ReciteHelper.Core.DTOs;
using ReciteHelper.Core.Interfaces.Configuration;
using ReciteHelper.Core.Interfaces.Services;
using ReciteHelper.Core.Entities;

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

    public async Task<AnswerResult> ProcessAnswerAsync(Question question,
        string userAnswer,
        DateTime startTime)
    {
        var correctAnswer = question.GetCorrectAnswerText();
        var isCorrect = question.IsSingleChoice
            ? question.IsCorrectChoiceAnswer(userAnswer)
            : await _judgeService.JudgeAsync(userAnswer, correctAnswer);
        var similarity = await _judgeService.CalculateSimilarityAsync(userAnswer, correctAnswer);

        var config = await _configService.LoadAsync();
        var duration = DateTime.Now - startTime;
        var rate = userAnswer.Length / duration.TotalSeconds;
        var rRelative = rate / config.RStandard;

        // Adjust short answer
        if (userAnswer.Length <= 12)
        {
            var l = userAnswer.Length;

            // WARNING: This is an EMPIRICAL formula
            var coff = -0.000000464 * Math.Pow(l, 4) + 0.0000746 * Math.Pow(l, 3)
                - 0.0041 * Math.Pow(l, 2) + 0.0895 * l + 0.2497;
            
            rRelative /= coff;
            rRelative /= 1.099d;
        }

        rRelative = Math.Min(rRelative, 1.125d);

        // Calculate related values
        var adjustedSimilarity = isCorrect ? Math.Max(similarity, 83) : similarity;
        var qValue = await _superMemoService.PredictQualityAsync(rRelative, adjustedSimilarity);
        var newEFValue = _superMemoService.CalculateEFValue(question.EFValue, qValue);

        var reviewTag = new ReviewTag
        {
            Rate = rRelative,
            Time = DateTime.Now,
            Similarity = adjustedSimilarity,
            QValue = qValue
        };

        return new AnswerResult
        {
            IsCorrect = isCorrect,
            QValue = qValue,
            NewEFValue = newEFValue,
            ReviewTag = reviewTag,
            RRelative = rRelative,
            Similarity = similarity
        };
    }
}
