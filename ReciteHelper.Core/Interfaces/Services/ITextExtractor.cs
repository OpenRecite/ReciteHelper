namespace ReciteHelper.Core.Interfaces.Services;

public interface ITextExtractor
{
    string ExtractFromPdf(string filePath);
    string ExtractFromImage(string imagePath);
}