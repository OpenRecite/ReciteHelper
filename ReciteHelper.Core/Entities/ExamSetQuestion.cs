using System.Text.Json.Serialization;

namespace ReciteHelper.Core.Entities;

public sealed class ExamSetQuestion
{
    [JsonPropertyName("number")]
    public int Number { get; set; }

    [JsonPropertyName("score")]
    public int Score { get; set; }

    [JsonPropertyName("question")]
    public Question Question { get; set; } = new();

    [JsonPropertyName("explanation")]
    public string Explanation { get; set; } = string.Empty;
}
