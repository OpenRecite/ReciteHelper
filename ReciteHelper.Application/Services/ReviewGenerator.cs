using ReciteHelper.Core.Aggregates;
using ReciteHelper.Core.DTOs;
using ReciteHelper.Core.Entities;
using ReciteHelper.Core.Interfaces.Services;

namespace ReciteHelper.Application.Services;

/// <summary>
/// Builds review sessions from predicted recall probabilities.
/// Policy: a question is due once its predicted recall has dropped below the desired
/// retention.  Due questions are served lowest-recall first, then never-reviewed
/// questions, then the remaining reviewed questions (again lowest-recall first).
/// </summary>
public class ReviewGenerator : IReviewGenerator
{
    private readonly IReviewScheduler _scheduler;
    private readonly Random _random = new();

    public ReviewGenerator(IReviewScheduler scheduler)
    {
        _scheduler = scheduler;
    }

    public List<Question> GenerateReview(Project project, int count)
    {
        ArgumentNullException.ThrowIfNull(project);
        if (count <= 0) throw new ArgumentException("数量必须大于0", nameof(count));

        var ranked = Rank(project, DateTime.Now);
        if (ranked.Count <= count)
            return ResetStatus(ranked.Select(r => r.Question).ToList());

        var due = ranked.Where(r => r.Recall is { } p && p < _scheduler.DesiredRetention);
        var fresh = ranked.Where(r => r.Recall is null).OrderBy(_ => _random.Next());
        var rest = ranked.Where(r => r.Recall is { } p && p >= _scheduler.DesiredRetention);

        var result = due.Concat(fresh).Concat(rest).Take(count).Select(r => r.Question).ToList();
        return ResetStatus(result);
    }

    public List<Question> GenerateParameterizationReview(Project project, ReviewOptions options)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(options);

        var ranked = Rank(project, DateTime.Now);
        var reviewed = ranked.Where(r => r.Recall is not null).ToList();
        var fresh = ranked.Where(r => r.Recall is null).OrderBy(_ => _random.Next()).ToList();

        var newCount = (int)(options.TotalCount * options.NewQuestionRatio);
        var reviewCount = Math.Max(0, options.TotalCount - newCount);

        IEnumerable<Ranked> reviewPart = options.PrioritizeWeakPoints
            ? reviewed // already ordered by ascending recall
            : reviewed.OrderBy(_ => _random.Next());

        var result = reviewPart.Take(reviewCount)
            .Concat(fresh.Take(newCount))
            .Select(r => r.Question)
            .ToList();

        // Top up from whatever is left when one of the pools is short.
        if (result.Count < options.TotalCount)
        {
            var remaining = ranked.Select(r => r.Question).Where(q => !result.Contains(q));
            result.AddRange(remaining.Take(options.TotalCount - result.Count));
        }

        return ResetStatus(result);
    }

    /// <summary>All questions ordered by predicted recall (ascending); never-reviewed questions last.</summary>
    public List<Ranked> Rank(Project project, DateTime now)
    {
        var ranked = new List<Ranked>();
        foreach (var question in project.Chapters?.SelectMany(c => c.Questions ?? []) ?? [])
            ranked.Add(new Ranked(question, _scheduler.PredictRecall(project, question, now)));

        return ranked
            .OrderBy(r => r.Recall is null ? 1 : 0)
            .ThenBy(r => r.Recall ?? 1.0)
            .ThenBy(_ => _random.Next())
            .ToList();
    }

    public readonly record struct Ranked(Question Question, double? Recall);

    private static List<Question> ResetStatus(List<Question> questions)
    {
        questions.ForEach(question => question.Status = null);
        return questions;
    }
}
