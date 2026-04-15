using ReciteHelper.Core.Interfaces.Services;

namespace ReciteHelper.Infrastructure.Services;

public class OcrService : IOcrService
{
    public string RecognizeText(string imagePath)
    {
        // 简化实现，后续可以集成 OCR 服务
        return string.Empty;
    }
}