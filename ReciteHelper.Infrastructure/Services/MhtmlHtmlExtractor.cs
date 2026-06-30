using System.Text;
using System.Text.RegularExpressions;

namespace ReciteHelper.Infrastructure.Services;

internal static partial class MhtmlHtmlExtractor
{
    public static string Extract(string mhtml)
    {
        if (string.IsNullOrWhiteSpace(mhtml))
            throw new InvalidDataException("MHTML 试卷文件为空。");

        var boundaryMatch = BoundaryRegex().Match(mhtml);
        if (!boundaryMatch.Success)
            throw new InvalidDataException("无法识别 MHTML 文件的 MIME 边界。");

        var boundary = boundaryMatch.Groups["quoted"].Success
            ? boundaryMatch.Groups["quoted"].Value
            : boundaryMatch.Groups["plain"].Value;
        foreach (var rawPart in mhtml.Split($"--{boundary}", StringSplitOptions.None))
        {
            var part = rawPart.TrimStart('\r', '\n');
            var separatorLength = 4;
            var separatorIndex = part.IndexOf("\r\n\r\n", StringComparison.Ordinal);
            if (separatorIndex < 0)
            {
                separatorLength = 2;
                separatorIndex = part.IndexOf("\n\n", StringComparison.Ordinal);
            }

            if (separatorIndex < 0)
                continue;

            var headers = part[..separatorIndex];
            var contentType = GetHeaderValue(headers, "Content-Type");
            if (!contentType.StartsWith("text/html", StringComparison.OrdinalIgnoreCase))
                continue;

            var body = part[(separatorIndex + separatorLength)..].TrimEnd('\r', '\n');
            var transferEncoding = GetHeaderValue(headers, "Content-Transfer-Encoding");
            return DecodeBody(body, transferEncoding, contentType);
        }

        throw new InvalidDataException("MHTML 文件中没有找到 HTML 主页面。");
    }

    private static string GetHeaderValue(string headers, string name)
    {
        var match = Regex.Match(
            headers,
            $"^{Regex.Escape(name)}:\\s*(?<value>[^\\r\\n]*(?:\\r?\\n[ \\t]+[^\\r\\n]*)*)",
            RegexOptions.IgnoreCase | RegexOptions.Multiline);
        return match.Success
            ? FoldedWhitespaceRegex().Replace(match.Groups["value"].Value, " ").Trim()
            : string.Empty;
    }

    private static string DecodeBody(string body, string transferEncoding, string contentType)
    {
        byte[] bytes;
        if (transferEncoding.Equals("base64", StringComparison.OrdinalIgnoreCase))
        {
            bytes = Convert.FromBase64String(WhitespaceRegex().Replace(body, string.Empty));
        }
        else if (transferEncoding.Equals("quoted-printable", StringComparison.OrdinalIgnoreCase))
        {
            bytes = DecodeQuotedPrintable(body);
        }
        else
        {
            return body;
        }

        var charsetMatch = CharsetRegex().Match(contentType);
        if (charsetMatch.Success)
        {
            try
            {
                return Encoding.GetEncoding(charsetMatch.Groups["charset"].Value).GetString(bytes);
            }
            catch (ArgumentException)
            {
                // Blink exports are UTF-8; fall back when an unknown charset is declared.
            }
        }

        return Encoding.UTF8.GetString(bytes);
    }

    private static byte[] DecodeQuotedPrintable(string value)
    {
        using var output = new MemoryStream(value.Length);
        for (var index = 0; index < value.Length; index++)
        {
            if (value[index] == '=')
            {
                if (index + 1 < value.Length && value[index + 1] == '\n')
                {
                    index++;
                    continue;
                }

                if (index + 2 < value.Length && value[index + 1] == '\r' && value[index + 2] == '\n')
                {
                    index += 2;
                    continue;
                }

                if (index + 2 < value.Length &&
                    TryHex(value[index + 1], out var high) &&
                    TryHex(value[index + 2], out var low))
                {
                    output.WriteByte((byte)((high << 4) | low));
                    index += 2;
                    continue;
                }
            }

            if (value[index] <= 0x7F)
            {
                output.WriteByte((byte)value[index]);
            }
            else
            {
                output.Write(Encoding.UTF8.GetBytes(value[index].ToString()));
            }
        }

        return output.ToArray();
    }

    private static bool TryHex(char value, out int result)
    {
        result = value switch
        {
            >= '0' and <= '9' => value - '0',
            >= 'A' and <= 'F' => value - 'A' + 10,
            >= 'a' and <= 'f' => value - 'a' + 10,
            _ => -1
        };
        return result >= 0;
    }

    [GeneratedRegex("boundary\\s*=\\s*(?:\"(?<quoted>[^\"]+)\"|(?<plain>[^\\s;]+))", RegexOptions.IgnoreCase)]
    private static partial Regex BoundaryRegex();

    [GeneratedRegex("\\r?\\n[ \\t]+")]
    private static partial Regex FoldedWhitespaceRegex();

    [GeneratedRegex("\\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex("charset\\s*=\\s*[\"']?(?<charset>[^\"';\\s]+)", RegexOptions.IgnoreCase)]
    private static partial Regex CharsetRegex();
}
