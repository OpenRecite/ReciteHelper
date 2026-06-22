using ReciteHelper.SharedKernel;
using System.Text.Json.Serialization;

namespace ReciteHelper.Core.Entities;

public class Question : Entity
{
    [JsonConstructor]
    public Question() { }

    [JsonPropertyName("status")]
    public bool? Status { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

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
}
