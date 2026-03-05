using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using ReciteHelper.Core.Entities;
using ReciteHelper.Model;
using ReciteHelper.ViewModel;
using System.Windows;

namespace ReciteHelper.Utils;

internal class JudgeAnswer
{
    internal async static Task<bool> RunAsync(string? userAnswer, string? correctAnswer)
    {
        ArgumentNullException.ThrowIfNull(userAnswer, nameof(userAnswer));
        ArgumentNullException.ThrowIfNull(correctAnswer, nameof(correctAnswer));

        var question = new Question()
        {
            CorrectAnswer = correctAnswer,
        };

        return await RunAsync(new QuestionItem() { Question = question, UserAnswer = userAnswer });
    }

    internal static async Task<bool> RunAsync(ExamQuestionItem question)
    {
        ArgumentNullException.ThrowIfNull(question, nameof(question));

        return await RunAsync(question.UserAnswer, question.Question!.CorrectAnswer);
    }

    internal async static Task<bool> RunAsync(
        QuestionItem question, double threshold = .70d) =>
            await CalculateAsync(question) > threshold;

    internal async static Task<double> CalculateAsync(QuestionItem question)
    {
        var user = question.UserAnswer!;
        var target = question.Question!.CorrectAnswer!;

        var builder = Kernel.CreateBuilder();

        builder.AddBertOnnxEmbeddingGenerator(
            onnxModelPath: @"Resources\Models\sbert.onnx",
            vocabPath: @"Resources\vocab.txt"
        );

        var kernel = builder.Build();

        var embeddingGenerator = kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
        var userEmbeddings = await embeddingGenerator.GenerateAsync([user]);
        var targetEmbeddings = await embeddingGenerator.GenerateAsync([target]);

        var vecUser = userEmbeddings[0].Vector;
        var vecTarget = targetEmbeddings[0].Vector;

        var sbertSim = CosineSimilarity.SpecCalculate(vecUser.Span, vecTarget.Span);
        
        var userSet = user.ToHashSet();
        var targetSet = target.ToHashSet();
        var jaccard = (double)userSet.Intersect(targetSet).Count() / userSet.Union(targetSet).Count();

        var sbertWeight = Math.Clamp(target.Length / 7.0, 0.6, 0.9);
        var jaccardWeight = 1.0 - sbertWeight;

        var result = (sbertSim * sbertWeight) + (jaccard * jaccardWeight);
        MessageBox.Show($"Score: {result}");

        return result;
    }

    public static double CalculateSimilarity(QuestionItem question)
    {
        var userAnswer = question.UserAnswer;
        var correctAnswer = question.Question!.CorrectAnswer;

        if (string.IsNullOrEmpty(userAnswer) && string.IsNullOrEmpty(correctAnswer))
            return 1.0;

        if (string.IsNullOrEmpty(userAnswer) || string.IsNullOrEmpty(correctAnswer))
            return 0.0;

        int maxLength = Math.Max(userAnswer.Length, correctAnswer.Length);
        if (maxLength == 0) return 1.0;

        double distance = ComputeLevenshteinDistance(userAnswer, correctAnswer);
        return 1.0 - (distance / maxLength);
    }

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

    public void UpdateEFValue(Question question, int qScore)
    {
        if (question.ReviewTag.Count < 2)
            return;

        double oldEF = question.EFValue;
        double newEF;

        if (qScore >= 3)
        {
            double factor = 0.1 - (5 - qScore) * (0.08 + (5 - qScore) * 0.02);
            newEF = oldEF + factor;
            newEF = Math.Max(1.3, newEF);
        }
        else
        {
            newEF = oldEF - 0.2;
            newEF = Math.Max(1.3, newEF);
        }

        question.EFValue = Math.Round(newEF, 2);
    }

}
