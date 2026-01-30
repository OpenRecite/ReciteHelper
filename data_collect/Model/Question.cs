using System.Text.Json.Serialization;

namespace ReciteHelper.DataCollect.Model;

public class Question
{
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("user_answer")]
    public string UserAnswer { get; set; } = string.Empty;

    [JsonPropertyName("correct_answer")]
    public string Answer { get; set; } = string.Empty;

    [JsonPropertyName("ef")]
    public double EFScore { get; set; } = 2.5;

    [JsonPropertyName("status")]
    public List<AnswerRecord> AnswerRecords { get; set; } = new();
}
