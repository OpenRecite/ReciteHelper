using System.Text.RegularExpressions;

namespace ReciteHelper.Core.Services;

public static partial class FillBlankTextNormalizer
{
    public const string BlankMarker = "________";

    private const string FallbackLabel = "待填空位";

    public static string NormalizeForImport(string text, IList<string> correctAnswers)
    {
        var source = (text ?? string.Empty).Trim();
        var explicitMarkerCount = CountExplicitMarkers(source);
        if (explicitMarkerCount > 0)
        {
            TrimAnswers(correctAnswers, explicitMarkerCount);
            return NormalizeExplicitMarkers(source, explicitMarkerCount);
        }

        var baseText = RemoveFallbackSuffix(source);
        var restored = RestoreMissingMarkers(baseText, correctAnswers.ToList(), out var restoredCount);
        if (restoredCount > 0)
        {
            TrimAnswers(correctAnswers, restoredCount);
            return restored;
        }

        var answerCount = Math.Max(1, correctAnswers.Count);
        return $"{baseText.TrimEnd()}　{FallbackLabel}：{string.Join("　", Enumerable.Repeat(BlankMarker, answerCount))}";
    }

    public static string NormalizeForDisplay(string text, IReadOnlyList<string> correctAnswers)
    {
        var source = (text ?? string.Empty).Trim();
        if (!ContainsFallbackSuffix(source))
            return source;

        var baseText = RemoveFallbackSuffix(source);
        var restored = RestoreMissingMarkers(baseText, correctAnswers, out var restoredCount);
        return restoredCount > 0 ? restored : source;
    }

    public static int CountEffectiveMarkers(string text, IReadOnlyList<string> correctAnswers)
    {
        var displayText = NormalizeForDisplay(text, correctAnswers);
        var markerCount = CountExplicitMarkers(displayText);
        return markerCount > 0 ? markerCount : Math.Max(1, correctAnswers.Count);
    }

    private static string NormalizeExplicitMarkers(string text, int answerCount)
    {
        var markerIndex = 0;
        return BlankMarkerRegex().Replace(text, _ =>
        {
            markerIndex++;
            return markerIndex <= answerCount ? BlankMarker : "（　）";
        });
    }

    private static string RestoreMissingMarkers(string text, IReadOnlyList<string> correctAnswers, out int restoredCount)
    {
        restoredCount = 0;
        if (string.IsNullOrWhiteSpace(text) || correctAnswers.Count == 0)
            return text;

        var restored = ReplaceVisibleAnswers(text, correctAnswers, ref restoredCount);
        if (restoredCount >= correctAnswers.Count)
            return restored;

        return ReplaceLostBlankSpaces(restored, correctAnswers.Count - restoredCount, ref restoredCount);
    }

    private static string ReplaceVisibleAnswers(string text, IReadOnlyList<string> correctAnswers, ref int restoredCount)
    {
        var restored = text;
        foreach (var answer in correctAnswers)
        {
            var candidate = SplitAnswerAlternatives(answer)
                .Where(part => part.Length >= 2)
                .OrderByDescending(part => part.Length)
                .FirstOrDefault(part => restored.Contains(part, StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrWhiteSpace(candidate))
                continue;

            var index = restored.IndexOf(candidate, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
                continue;

            restored = restored[..index] + BlankMarker + restored[(index + candidate.Length)..];
            restoredCount++;
        }

        return restored;
    }

    private static string ReplaceLostBlankSpaces(string text, int remainingCount, ref int restoredCount)
    {
        if (remainingCount <= 0)
            return text;

        var candidates = new List<Match>();
        candidates.AddRange(ChineseWordGapRegex().Matches(text).Cast<Match>());
        candidates.AddRange(ChinesePunctuationGapRegex().Matches(text).Cast<Match>());
        candidates.AddRange(ChineseSentenceSubjectGapRegex().Matches(text).Cast<Match>());

        var selected = candidates
            .Where(match => !IsInsideBlankMarker(text, match.Index))
            .OrderBy(match => match.Index)
            .Take(remainingCount)
            .ToList();
        if (selected.Count == 0)
            return text;

        var restored = text;
        for (var index = selected.Count - 1; index >= 0; index--)
        {
            var match = selected[index];
            restored = restored[..match.Index] + $" {BlankMarker} " + restored[(match.Index + match.Length)..];
        }

        restoredCount += selected.Count;
        return restored;
    }

    private static IEnumerable<string> SplitAnswerAlternatives(string answer)
    {
        return Regex.Split(answer.Trim(), @"\s*(?:或|/|、|；|;)\s*")
            .Select(part => part.Trim())
            .Where(part => !string.IsNullOrWhiteSpace(part));
    }

    private static bool IsInsideBlankMarker(string text, int index)
    {
        var start = Math.Max(0, index - BlankMarker.Length);
        var length = Math.Min(text.Length - start, BlankMarker.Length * 2);
        return text.Substring(start, length).Contains(BlankMarker, StringComparison.Ordinal);
    }

    private static void TrimAnswers(IList<string> correctAnswers, int count)
    {
        if (correctAnswers.Count > count)
        {
            for (var index = correctAnswers.Count - 1; index >= count; index--)
                correctAnswers.RemoveAt(index);
        }
    }

    private static int CountExplicitMarkers(string text)
    {
        return BlankMarkerRegex().Matches(text ?? string.Empty).Count;
    }

    private static bool ContainsFallbackSuffix(string text)
    {
        return text.Contains(FallbackLabel, StringComparison.Ordinal);
    }

    private static string RemoveFallbackSuffix(string text)
    {
        return FallbackSuffixRegex().Replace(text ?? string.Empty, string.Empty).TrimEnd();
    }

    [GeneratedRegex(@"_{2,}|＿{2,}")]
    private static partial Regex BlankMarkerRegex();

    [GeneratedRegex(@"\s*待填空位[:：]\s*(?:_{2,}|＿{2,}|\s|　)+$")]
    private static partial Regex FallbackSuffixRegex();

    [GeneratedRegex(@"(?<=[\u4e00-\u9fff])\s+(?=[\u4e00-\u9fff，。；：！？])")]
    private static partial Regex ChineseWordGapRegex();

    [GeneratedRegex(@"(?<=[、，])\s+(?=(?:效应|类型|基因型|种|型|期|体|系|物质|性状|基因|染色体))")]
    private static partial Regex ChinesePunctuationGapRegex();

    [GeneratedRegex(@"(?<=[。；])\s+(?=(?:提出|发现|证明|认为|命名|创立|建立|指出))")]
    private static partial Regex ChineseSentenceSubjectGapRegex();
}
