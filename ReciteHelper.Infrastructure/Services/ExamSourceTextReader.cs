using ReciteHelper.Core.Interfaces.Services;
using ReciteHelper.Infrastructure.Utilities;

namespace ReciteHelper.Infrastructure.Services;

public sealed class ExamSourceTextReader : IExamSourceTextReader
{
    private static readonly HashSet<string> SupportedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".txt" };

    public async Task<string> ReadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("找不到待导入的试卷文件。", filePath);

        var extension = Path.GetExtension(filePath);
        if (!SupportedExtensions.Contains(extension))
            throw new NotSupportedException("仅支持导入 PDF 或 TXT 试卷。");

        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return (string)ExtractText.FromAutomatic(filePath);
        }, cancellationToken);
    }
}
