using ReciteHelper.Core.Interfaces.Services;
using ReciteHelper.Infrastructure.Utilities;

namespace ReciteHelper.Infrastructure.Services;

public sealed class ExamSourceTextReader : IExamSourceTextReader
{
    private static readonly HashSet<string> SupportedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".txt", ".html", ".htm", ".mhtml", ".mht" };

    public async Task<string> ReadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("找不到待导入的试卷文件。", filePath);

        var extension = Path.GetExtension(filePath);
        if (!SupportedExtensions.Contains(extension))
            throw new NotSupportedException("仅支持导入 PDF、TXT 或学堂在线 HTML/MHTML 试卷。");

        if (extension.Equals(".html", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".htm", StringComparison.OrdinalIgnoreCase))
        {
            var html = await File.ReadAllTextAsync(filePath, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return XuetangXExamHtmlFilter.Extract(html);
        }

        if (extension.Equals(".mhtml", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".mht", StringComparison.OrdinalIgnoreCase))
        {
            var mhtml = await File.ReadAllTextAsync(filePath, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return XuetangXExamHtmlFilter.Extract(MhtmlHtmlExtractor.Extract(mhtml));
        }

        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return (string)ExtractText.FromAutomatic(filePath);
        }, cancellationToken);
    }
}
