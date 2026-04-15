namespace ReciteHelper.Core.Interfaces.Services;

public interface IOcrService
{
    string RecognizeText(string imagePath);
}