using System.Text;
using System.Text.RegularExpressions;
using ReciteHelper.Core.Entities;
using ReciteHelper.Core.Enums;
using ReciteHelper.Core.ValueObjects;

namespace ReciteHelper.Infrastructure.Services;

public sealed record XuetangXQuestionBankImport(Chapter Chapter, string SourceText);

public static partial class XuetangXQuestionBankImporter
{
    public static async Task<XuetangXQuestionBankImport> ImportAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        var content = await File.ReadAllTextAsync(filePath, cancellationToken);
        var extension = Path.GetExtension(filePath);
        var html = extension.Equals(".mhtml", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".mht", StringComparison.OrdinalIgnoreCase)
            ? MhtmlHtmlExtractor.Extract(content)
            : content;

        cancellationToken.ThrowIfCancellationRequested();
        var title = XuetangXExamHtmlFilter.ExtractTitle(html);
        if (string.IsNullOrWhiteSpace(title))
            title = Path.GetFileNameWithoutExtension(filePath);

        var questions = XuetangXExamHtmlFilter.ExtractQuestionSegments(html)
            .Select(ParseQuestion)
            .Where(question => question is not null)
            .Cast<Question>()
            .ToList();
        if (questions.Count == 0)
            throw new InvalidDataException($"未能从“{Path.GetFileName(filePath)}”中解析出题目。");

        return new XuetangXQuestionBankImport(
            new Chapter
            {
                Name = title,
                Number = 1,
                Questions = questions,
                KnowledgePoints = []
            },
            BuildKnowledgeBaseText(title, questions));
    }

    private static Question? ParseQuestion(string segment)
    {
        var stemMatch = StemRegex().Match(segment);
        if (!stemMatch.Success)
            return null;

        var stem = XuetangXExamHtmlFilter.ToPlainText(stemMatch.Groups["content"].Value);
        var typeText = Extract(TypeRegex(), segment);
        var answer = Extract(CorrectAnswerRegex(), segment);
        if (string.IsNullOrWhiteSpace(stem) || string.IsNullOrWhiteSpace(answer))
            return null;

        var optionIds = OptionIdRegex().Matches(segment)
            .Select(match => XuetangXExamHtmlFilter.ToPlainText(match.Groups["content"].Value))
            .Select(QuestionOption.NormalizeId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToList();
        var optionTexts = OptionTextRegex().Matches(segment)
            .Select(match => XuetangXExamHtmlFilter.ToPlainText(match.Groups["content"].Value))
            .ToList();
        var options = optionIds.Zip(optionTexts, (id, text) => new QuestionOption { Id = id, Text = text })
            .Where(option => !string.IsNullOrWhiteSpace(option.Text))
            .ToList();

        var type = ParseType(typeText, options.Count);
        var question = new Question
        {
            Text = stem,
            Type = type
        };

        if (type == QuestionType.SingleChoice)
        {
            var ids = AnswerOptionRegex().Matches(answer.ToUpperInvariant())
                .Select(match => QuestionOption.NormalizeId(match.Value))
                .Where(id => options.Any(option => option.Id == id))
                .Distinct()
                .ToList();
            if (ids.Count == 0)
                return null;

            question.Options = options;
            question.CorrectOptionIds = ids;
            question.CorrectAnswer = ids[0];
        }
        else if (type == QuestionType.FillBlank)
        {
            var answers = answer
                .Split(['；', ';', '\u001F'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
            question.CorrectAnswers = answers.Count > 0 ? answers : [answer];
            question.CorrectAnswer = question.CorrectAnswers[0];
        }
        else if (type == QuestionType.TrueFalse)
        {
            question.CorrectAnswer = Question.NormalizeTrueFalseAnswer(answer);
        }
        else
        {
            question.CorrectAnswer = answer;
        }

        return question;
    }

    private static string BuildKnowledgeBaseText(string title, IReadOnlyList<Question> questions)
    {
        var text = new StringBuilder().AppendLine(title);
        foreach (var question in questions)
        {
            text.AppendLine($"题目：{question.Text}");
            foreach (var option in question.Options)
                text.AppendLine($"{option.DisplayText}");
            text.AppendLine($"正确答案：{question.GetCorrectAnswerText()}").AppendLine();
        }

        return text.ToString();
    }

    private static string Extract(Regex regex, string html)
    {
        var match = regex.Match(html);
        return match.Success
            ? XuetangXExamHtmlFilter.ToPlainText(match.Groups["content"].Value)
            : string.Empty;
    }

    private static QuestionType ParseType(string value, int optionCount)
    {
        if (optionCount >= 2 || value.Contains("选择", StringComparison.Ordinal))
            return QuestionType.SingleChoice;
        if (value.Contains("填空", StringComparison.Ordinal))
            return QuestionType.FillBlank;
        if (value.Contains("判断", StringComparison.Ordinal))
            return QuestionType.TrueFalse;
        if (value.Contains("名词", StringComparison.Ordinal))
            return QuestionType.TermDefinition;
        return QuestionType.Essay;
    }

    [GeneratedRegex("class=[\"'][^\"']*item-type[^\"']*[\"'][^>]*>(?<content>[\\s\\S]*?)</div>", RegexOptions.IgnoreCase)]
    private static partial Regex TypeRegex();

    [GeneratedRegex("<h4\\b[^>]*class=[\"'][^\"']*exam-font[^\"']*[\"'][^>]*>(?<content>[\\s\\S]*?)</h4>", RegexOptions.IgnoreCase)]
    private static partial Regex StemRegex();

    [GeneratedRegex("class=[\"'][^\"']*(?:radio|checkbox)Input[^\"']*[\"'][^>]*>(?<content>[\\s\\S]*?)</span>", RegexOptions.IgnoreCase)]
    private static partial Regex OptionIdRegex();

    [GeneratedRegex("class=[\"'][^\"']*(?:radio|checkbox)Text[^\"']*[\"'][^>]*>(?<content>[\\s\\S]*?)</span>", RegexOptions.IgnoreCase)]
    private static partial Regex OptionTextRegex();

    [GeneratedRegex("正确答案：\\s*</span>\\s*<span[^>]*>(?<content>[\\s\\S]*?)</span>", RegexOptions.IgnoreCase)]
    private static partial Regex CorrectAnswerRegex();

    [GeneratedRegex("[A-Z]")]
    private static partial Regex AnswerOptionRegex();
}
