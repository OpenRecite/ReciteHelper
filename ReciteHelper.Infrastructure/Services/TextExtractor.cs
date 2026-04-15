using ReciteHelper.Core.Interfaces.Services;
using System.IO;

namespace ReciteHelper.Infrastructure.Services;

public class TextExtractor : ITextExtractor
{
    public string ExtractFromPdf(string filePath)
    {
        // 简化实现，后续可以集成 PDF 解析库
        return string.Empty;
    }

    public string ExtractFromImage(string imagePath)
    {
        // 简化实现，后续可以集成 OCR 服务
        return string.Empty;
    }

    public string ExtractFromText(string filePath)
    {
        return File.ReadAllText(filePath);
    }
}