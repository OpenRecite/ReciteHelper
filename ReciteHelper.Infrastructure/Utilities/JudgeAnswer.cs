using FuzzyString;
using ReciteHelper.Core.Entities;
using ReciteHelper.Infrastructure.Algorithms;

namespace ReciteHelper.Infrastructure.Utilities;

public static class JudgeAnswer
{
    public static bool Run(string? userAnswer, string? correctAnswer)
    {
        ArgumentNullException.ThrowIfNull(userAnswer, nameof(userAnswer));
        ArgumentNullException.ThrowIfNull(correctAnswer, nameof(correctAnswer));

        if (string.IsNullOrEmpty(userAnswer))
            return false;

        var tolerance = FuzzyStringComparisonTolerance.Strong;
        var comparisonOptions = new List<FuzzyStringComparisonOptions>
        {
            FuzzyStringComparisonOptions.UseOverlapCoefficient,
            FuzzyStringComparisonOptions.UseLongestCommonSubsequence,
            FuzzyStringComparisonOptions.UseLongestCommonSubstring
        };

        var similarity = new CosineSimilarity();
        var score = similarity.Calculate(userAnswer, correctAnswer);

        var isCorrect = userAnswer.ApproximatelyEquals(correctAnswer, comparisonOptions, tolerance);
        if (userAnswer.Length >= 15)
            score -= .2d;

        return isCorrect || score > .4;
    }

    public static bool Run(Question question, string? userAnswer)
    {
        if (question.IsSingleChoice)
            return question.IsCorrectChoiceAnswer(userAnswer);

        if (question.IsTrueFalse)
            return question.IsCorrectTrueFalseAnswer(userAnswer);

        if (question.IsFillBlank)
        {
            var userAnswers = Question.SplitBlankAnswers(userAnswer);
            var correctAnswers = question.GetCorrectAnswers();
            return correctAnswers.Count > 0 &&
                   userAnswers.Count == correctAnswers.Count &&
                   correctAnswers.Select((answer, index) => Run(userAnswers[index], answer)).All(result => result);
        }

        return !string.IsNullOrWhiteSpace(userAnswer) &&
               !string.IsNullOrWhiteSpace(question.CorrectAnswer) &&
               Run(userAnswer, question.CorrectAnswer);
    }

    public static double CalculateSimilarity(string? userAnswer, string? correctAnswer)
    {
        if (string.IsNullOrEmpty(userAnswer) && string.IsNullOrEmpty(correctAnswer))
            return 1.0;

        if (string.IsNullOrEmpty(userAnswer) || string.IsNullOrEmpty(correctAnswer))
            return 0.0;

        var maxLength = Math.Max(userAnswer.Length, correctAnswer.Length);
        if (maxLength == 0)
            return 1.0;

        var distance = ComputeLevenshteinDistance(userAnswer, correctAnswer);
        return 1.0 - distance / maxLength;
    }

    private static int ComputeLevenshteinDistance(string source, string target)
    {
        var sourceLength = source.Length;
        var targetLength = target.Length;
        var distances = new int[sourceLength + 1, targetLength + 1];

        if (sourceLength == 0)
            return targetLength;

        if (targetLength == 0)
            return sourceLength;

        for (var i = 0; i <= sourceLength; distances[i, 0] = i++) { }
        for (var j = 0; j <= targetLength; distances[0, j] = j++) { }

        for (var i = 1; i <= sourceLength; i++)
        {
            for (var j = 1; j <= targetLength; j++)
            {
                var cost = target[j - 1] == source[i - 1] ? 0 : 1;
                distances[i, j] = Math.Min(
                    Math.Min(distances[i - 1, j] + 1, distances[i, j - 1] + 1),
                    distances[i - 1, j - 1] + cost);
            }
        }

        return distances[sourceLength, targetLength];
    }
}
