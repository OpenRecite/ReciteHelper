using System.Text.Json.Serialization;

namespace ReciteHelper.Core.ValueObjects;

public sealed class QuestionOption
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    public string DisplayText => $"{NormalizeId(Id)}. {Text}".Trim();

    public static string NormalizeId(string? id)
    {
        return (id ?? string.Empty).Trim().TrimEnd('.').ToUpperInvariant();
    }
}
