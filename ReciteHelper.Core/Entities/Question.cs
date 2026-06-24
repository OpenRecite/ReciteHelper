using ReciteHelper.SharedKernel;
using ReciteHelper.Core.Enums;
using ReciteHelper.Core.ValueObjects;
using System.Text.Json.Serialization;

namespace ReciteHelper.Core.Entities;

public class Question : Entity
{
    private List<QuestionOption> _options = [];
    private List<string> _correctOptionIds = [];

    [JsonConstructor]
    public Question() { }

    [JsonPropertyName("status")]
    public bool? Status { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("type")]
    public QuestionType Type { get; set; } = QuestionType.ShortAnswer;

    [JsonPropertyName("options")]
    public List<QuestionOption> Options
    {
        get => _options;
        set => _options = value ?? [];
    }

    [JsonPropertyName("correct_option_ids")]
    public List<string> CorrectOptionIds
    {
        get => _correctOptionIds;
        set => _correctOptionIds = value ?? [];
    }

    [JsonPropertyName("review_tag")]
    public List<ReviewTag> ReviewTag { get; private set; } = [];

    [JsonPropertyName("correct_answer")]
    public string? CorrectAnswer { get; set; }

    [JsonPropertyName("ef_value")]
    public double EFValue { get; set; } = 2.5d;

    private Question(bool? status, string? text, List<ReviewTag> reviewTags, string? correctAnswer, double efValue)
    {
        Status = status;
        Text = text;
        ReviewTag = reviewTags;
        CorrectAnswer = correctAnswer;
        EFValue = efValue;
    }

    [JsonIgnore]
    public bool IsSingleChoice => Type == QuestionType.SingleChoice && Options.Count > 0;

    public string GetCorrectAnswerText()
    {
        if (!IsSingleChoice)
            return CorrectAnswer ?? string.Empty;

        var correctOptionId = GetCorrectOptionIds().FirstOrDefault();
        if (string.IsNullOrWhiteSpace(correctOptionId))
            return CorrectAnswer ?? string.Empty;

        return GetOptionDisplayText(correctOptionId);
    }

    public string GetOptionDisplayText(string? optionId)
    {
        var normalizedId = QuestionOption.NormalizeId(optionId);
        var option = Options.FirstOrDefault(x => QuestionOption.NormalizeId(x.Id) == normalizedId);
        return option is null ? normalizedId : option.DisplayText;
    }

    public IReadOnlyList<string> GetCorrectOptionIds()
    {
        var ids = CorrectOptionIds
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(QuestionOption.NormalizeId)
            .Distinct()
            .ToList();

        if (ids.Count > 0)
            return ids;

        var answerId = ExtractOptionId(CorrectAnswer);
        return string.IsNullOrWhiteSpace(answerId) ? [] : [answerId];
    }

    public bool IsCorrectChoiceAnswer(string? userAnswer)
    {
        if (!IsSingleChoice || string.IsNullOrWhiteSpace(userAnswer))
            return false;

        var userOptionId = ExtractOptionId(userAnswer);
        if (string.IsNullOrWhiteSpace(userOptionId))
            return false;

        return GetCorrectOptionIds().Contains(userOptionId);
    }

    public static string ExtractOptionId(string? answer)
    {
        if (string.IsNullOrWhiteSpace(answer))
            return string.Empty;

        var trimmed = answer.Trim();
        if (trimmed.Length == 0)
            return string.Empty;

        var first = char.ToUpperInvariant(trimmed[0]);
        return first is >= 'A' and <= 'Z' ? first.ToString() : string.Empty;
    }
}
