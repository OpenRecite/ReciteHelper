using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace ReciteHelper.Infrastructure.Services;

internal static partial class XuetangXExamHtmlFilter
{
    public static string Extract(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            throw new InvalidDataException("HTML 试卷文件为空。");

        var questionSegments = ExtractQuestionSegments(html);
        if (questionSegments.Count == 0)
        {
            throw new InvalidDataException(
                "未在 HTML 中找到学堂在线考试结果题目。请从考试结果页保存包含题目和正确答案的完整网页。");
        }

        var output = new StringBuilder();
        var title = ExtractTitle(html);
        if (!string.IsNullOrWhiteSpace(title))
        {
            output.AppendLine($"试卷标题：{title}").AppendLine();
        }

        for (var index = 0; index < questionSegments.Count; index++)
        {
            var questionText = ToPlainText(questionSegments[index]);
            if (string.IsNullOrWhiteSpace(questionText))
                continue;

            output.AppendLine($"--- 第 {index + 1} 题 ---");
            output.AppendLine(questionText).AppendLine();
        }

        return output.ToString().Trim();
    }

    internal static string ExtractTitle(string html)
    {
        var match = HeaderTitleRegex().Match(html);
        return match.Success ? ToPlainText(match.Groups["content"].Value) : string.Empty;
    }

    internal static IReadOnlyList<string> ExtractQuestionSegments(string html)
    {
        var starts = ResultItemStartRegex().Matches(html);
        var segments = new List<string>(starts.Count);
        for (var index = 0; index < starts.Count; index++)
        {
            var start = starts[index].Index;
            var end = index + 1 < starts.Count ? starts[index + 1].Index : html.Length;
            segments.Add(html[start..end]);
        }

        return segments;
    }

    internal static string ToPlainText(string html)
    {
        var text = ScriptAndStyleRegex().Replace(html, " ");
        text = CommentRegex().Replace(text, " ");
        text = ImageRegex().Replace(text, match =>
        {
            var altMatch = AltAttributeRegex().Match(match.Value);
            return altMatch.Success
                ? $" {WebUtility.HtmlDecode(altMatch.Groups["value"].Value)} "
                : " [图片] ";
        });
        text = LineBreakRegex().Replace(text, "\n");
        text = TagRegex().Replace(text, " ");
        text = WebUtility.HtmlDecode(text).Replace('\u00A0', ' ');

        var lines = text
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(line => InlineWhitespaceRegex().Replace(line, " ").Trim())
            .Where(line => line.Length > 0);
        return string.Join(Environment.NewLine, lines);
    }

    [GeneratedRegex("<div\\b[^>]*\\bclass\\s*=\\s*[\"'][^\"']*\\bresult_item\\b[^\"']*[\"'][^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex ResultItemStartRegex();

    [GeneratedRegex("<[^>]+\\bclass\\s*=\\s*[\"'][^\"']*\\bheader-title\\b[^\"']*[\"'][^>]*>(?<content>[\\s\\S]*?)</[^>]+>", RegexOptions.IgnoreCase)]
    private static partial Regex HeaderTitleRegex();

    [GeneratedRegex("<(script|style)\\b[\\s\\S]*?</\\1\\s*>", RegexOptions.IgnoreCase)]
    private static partial Regex ScriptAndStyleRegex();

    [GeneratedRegex("<!--[\\s\\S]*?-->")]
    private static partial Regex CommentRegex();

    [GeneratedRegex("<img\\b[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex ImageRegex();

    [GeneratedRegex("\\balt\\s*=\\s*[\"'](?<value>[^\"']*)[\"']", RegexOptions.IgnoreCase)]
    private static partial Regex AltAttributeRegex();

    [GeneratedRegex("<br\\s*/?>|</(?:div|p|li|ul|ol|h[1-6]|tr|section|article)\\s*>", RegexOptions.IgnoreCase)]
    private static partial Regex LineBreakRegex();

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex TagRegex();

    [GeneratedRegex("[ \\t\\f\\v]+")]
    private static partial Regex InlineWhitespaceRegex();
}
