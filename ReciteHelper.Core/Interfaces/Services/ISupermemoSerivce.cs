namespace ReciteHelper.Core.Interfaces.Services;

public interface ISuperMemoService
{
    double CalculateEFValue(double currentEF, int quality);

    Task<int> PredictQualityAsync(double relativeRate, double similarity);
}