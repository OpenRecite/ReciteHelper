using FuzzyString;
using ReciteHelper.Core.Interfaces.Services;
using ReciteHelper.Infrastructure.Algorithms;

namespace ReciteHelper.Infrastructure.Services;

[Obsolete("This class should only be used on low-performance computers; for modern computers, SbertModelJudge should be used instead to improve accuracy.")]
public class LevenshteinDistanceJudge : IAnswerJudge
{
    private static int ComputeLevenshteinDistance(string s, string t)
    {
        int n = s.Length;
        int m = t.Length;
        int[,] d = new int[n + 1, m + 1];

        if (n == 0) return m;
        if (m == 0) return n;

        for (int i = 0; i <= n; d[i, 0] = i++) { }
        for (int j = 0; j <= m; d[0, j] = j++) { }

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[n, m];
    }

    public Task<double> CalculateSimilarityAsync(string userAnswer, string correctAnswer)
    {
        return Task.Run(() =>
        {
            if (string.IsNullOrEmpty(userAnswer) && string.IsNullOrEmpty(correctAnswer))
                return 1.0;

            if (string.IsNullOrEmpty(userAnswer) || string.IsNullOrEmpty(correctAnswer))
                return 0.0;

            int maxLength = Math.Max(userAnswer.Length, correctAnswer.Length);
            if (maxLength == 0) return 1.0;

            double distance = ComputeLevenshteinDistance(userAnswer, correctAnswer);
            return 1.0 - (distance / maxLength);
        });
    }

    public Task<bool> JudgeAsync(string? userAnswer, string? correctAnswer)
    {
        ArgumentNullException.ThrowIfNull(userAnswer);
        ArgumentNullException.ThrowIfNull(correctAnswer);

        return Task.Run(() =>
        {
            var tolerance = FuzzyStringComparisonTolerance.Strong;
            var comparisonOptions = new List<FuzzyStringComparisonOptions>
        {
            FuzzyStringComparisonOptions.UseOverlapCoefficient,
            FuzzyStringComparisonOptions.UseLongestCommonSubsequence,
            FuzzyStringComparisonOptions.UseLongestCommonSubstring
        };

            var similarity = new CosineSimilarity();
            var score = similarity.Calculate(userAnswer, correctAnswer);

            bool isCorrect = userAnswer.ApproximatelyEquals(
                correctAnswer, comparisonOptions, tolerance);

            if (userAnswer.Length >= 15) score -= .2d;

            return isCorrect || (score > .4);
        });
    }
}
