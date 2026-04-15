namespace ReciteHelper.Core.Interfaces.Services;

public interface ISimilarityCalculator
{
    double CalculateSimilarity(string text1, string text2);
}