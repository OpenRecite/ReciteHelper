using ReciteHelper.Core.Entities;
using ReciteHelper.SharedKernel;
using System.Reflection.Metadata;
using System.Text.Json.Serialization;

namespace ReciteHelper.Core.ValueObjects;

public class Question : ValueObject
{
    [JsonConstructor]
    public Question() { }

    [JsonPropertyName("status")]
    public bool? Status { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("review_tag")]
    public List<ReviewTag> ReviewTag { get; set; } = [];

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

        Validate();
    }

    public override T Clone<T>()
    {
        throw new NotImplementedException();
    }

    public static Question Create(bool? status, string? text, List<ReviewTag> reviewTags, string? correctAnswer, double efValue)
    {
        return Create(() =>
        {
            return new Question(status, text, reviewTags, correctAnswer, efValue);
        });
    }

    public Question AddReviewTag(ReviewTag reviewTag)
    {
        var reviewTags = ReviewTag;
        reviewTags.Add(reviewTag);

        var question = new Question(Status, Text, reviewTags, CorrectAnswer, EFValue);
        return question;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        if (Text is not null && CorrectAnswer is not null)
        {
            yield return Text;
            yield return CorrectAnswer;
        }
        else
        {
            // Incomplete questions are considered abnormal and will not be compared
            yield break;
        }

    }

    protected override void Validate()
    {
        if (EFValue < 0)
            throw new ArgumentException("The EF value cannot be less than 0.");
    }
}
