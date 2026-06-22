using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using ReciteHelper.Core.Interfaces.Services;

namespace ReciteHelper.Infrastructure.Services;

public class SuperMemoService : ISuperMemoService
{
    private string _modelPath = string.Empty;
    private readonly ILogger<SuperMemoService> _logger;

    public SuperMemoService(ILogger<SuperMemoService> logger)
    {
        _modelPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Resources", "Models", "xgboost_qvalue.onnx");
        _logger = logger;
    }

    public double CalculateEFValue(double currentEF, int quality)
    {
        var newEF = currentEF + (0.1 - (5 - quality) * (0.08 + (5 - quality) * 0.02));
        return newEF;
    }

    private int PredictQualityIdiot(double relativeRate, double similarity)
    {
        var baseQuality = (int)(similarity / 20);
        return Math.Clamp(baseQuality, 0, 5);
    }

    public async Task<int> PredictQualityAsync(double relativeRate, double similarity)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (!File.Exists(_modelPath))
                {
                    _logger.LogWarning("ONNX 模型不存在，使用传统算法");
                    return PredictQualityIdiot(relativeRate, similarity);
                }

                using var session = new InferenceSession(_modelPath);

                float[] inputData = [
                    (float)relativeRate,
                    (float)similarity * 100f
                ];
                int[] dimensions = [1, 2];
                var inputTensor = new DenseTensor<float>(inputData, dimensions);

                var inputs = new List<NamedOnnxValue>
                {
                    NamedOnnxValue.CreateFromTensor("float_input", inputTensor)
                };

                using var results = session.Run(inputs);
                var probs = results.First(r => r.Name == "probabilities")
                                  .AsEnumerable<float>()
                                  .ToArray();

                return GetMaxIndex(probs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ONNX 预测失败，使用传统算法");
                return PredictQualityIdiot(relativeRate, similarity);
            }
        });
    }

    private int GetMaxIndex(float[] probs)
    {
        int maxIndex = 0;
        float maxProb = probs[0];

        for (int i = 1; i < probs.Length; i++)
        {
            if (probs[i] > maxProb)
            {
                maxProb = probs[i];
                maxIndex = i;
            }
        }
        return maxIndex;
    }
}
