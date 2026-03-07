using ReciteHelper.Core.Exceptions;
using ReciteHelper.SharedKernel;

namespace ReciteHelper.Core.ValueObjects;

public class ExamSettings : ValueObject
{
    public string CourseNumber { get; init; }
    public int ExamTimeMinutes { get; init; }
    public int QuestionCount { get; init; }
    public int ScorePerQuestion { get; init; }
    public Dictionary<string, double>? ChapterWeights { get; init; }

    private ExamSettings(string courseNumber, int examTimeMinutes, int questionCount, int scorePerQuestion, Dictionary<string, double>? chapterWeights)
    {
        CourseNumber = courseNumber;
        ExamTimeMinutes = examTimeMinutes;
        QuestionCount = questionCount;
        ScorePerQuestion = scorePerQuestion;
        ChapterWeights = chapterWeights;
    }

    public static ExamSettings Create(string courseNumber, int examTimeMinutes, int questionCount, int scorePerQuestion, Dictionary<string, double>? chapterWeights)
    {
        return Create(() =>
        {
            return new ExamSettings(courseNumber, examTimeMinutes, questionCount, scorePerQuestion, chapterWeights); ;
        });
    }

    public override T Clone<T>()
    {
        return (T)(object)new ExamSettings(CourseNumber, ExamTimeMinutes, QuestionCount, ScorePerQuestion, ChapterWeights);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield break;
    }

    protected override void Validate()
    {
        var errors = new List<string>();

        if (QuestionCount < 0)
            errors.Add("The score must greater than 0.");

        if (ExamTimeMinutes < 0)
            errors.Add("The exam time must not be less than 0.");

        unchecked
        {
            var sumScore = QuestionCount * ScorePerQuestion;

            if (sumScore < 0)
                errors.Add("Total score overflow.");
        }

        if (errors.Any())
        {
            throw new ValidationException("Domain verification failed.")
            {
                Errors = errors
            };
        }
    }
}
