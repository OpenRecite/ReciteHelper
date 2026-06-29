using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using ReciteHelper.Core.Interfaces.Services;
using ReciteHelper.Infrastructure.Algorithms;

namespace ReciteHelper.Infrastructure.Services;

public class SbertModelJudge : IAnswerJudge
{
    private readonly string _modelPath = Path.Combine(
        AppContext.BaseDirectory,
        "Resources",
        "Models",
        "sbert.onnx");
    private readonly string _vocabPath = Path.Combine(
        AppContext.BaseDirectory,
        "Resources",
        "vocab.txt");

#pragma warning disable CS0618
    private readonly LevenshteinDistanceJudge _fallbackJudge = new();
#pragma warning restore CS0618

    public async Task<double> CalculateSimilarityAsync(string userAnswer, string correctAnswer)
    {
        if (!CanUseSbert())
            return await _fallbackJudge.CalculateSimilarityAsync(userAnswer, correctAnswer);

        var user = userAnswer;
        var target = correctAnswer;

        var builder = Kernel.CreateBuilder();

        builder.AddBertOnnxEmbeddingGenerator(
            onnxModelPath: _modelPath,
            vocabPath: _vocabPath
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

        return result;
    }

    public async Task<bool> JudgeAsync(string? userAnswer, string? correctAnswer)
    {
        ArgumentNullException.ThrowIfNull(userAnswer, nameof(userAnswer));
        ArgumentNullException.ThrowIfNull(correctAnswer, nameof(correctAnswer));

        if (!CanUseSbert())
        {
            var similarity = await _fallbackJudge.CalculateSimilarityAsync(userAnswer, correctAnswer);
            return similarity >= .70d;
        }

        return await CalculateSimilarityAsync(userAnswer, correctAnswer) >= .70d;
    }

    private bool CanUseSbert()
    {
        return File.Exists(_modelPath) && File.Exists(_vocabPath);
    }
}
