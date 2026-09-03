using ReciteHelper.Core.Entities;

namespace ReciteHelper.Core.Scheduling;

/// <summary>
/// Rebuilds an FSRS-6 <see cref="MemoryRecord"/> from the review tags written by
/// earlier (SM-2 based) versions of ReciteHelper, so that existing projects keep
/// their learning history when the scheduler is upgraded.
/// </summary>
public static class LegacyReviewMigrator
{
    /// <summary>
    /// Replays the timestamped tags in chronological order.  Returns null when the
    /// question has never been reviewed.
    /// </summary>
    public static MemoryRecord? Replay(IReadOnlyList<ReviewTag> tags, SchedulerParameters parameters)
    {
        if (tags.Count == 0) return null;

        MemoryRecord? record = null;
        foreach (var tag in tags.OrderBy(t => t.Time))
        {
            var grade = tag.Grade > 0 ? (ReviewGrade)tag.Grade : ReviewGrader.FromLegacyQuality(tag.QValue);
            if (record is null)
            {
                record = MemoryRecord.From(FsrsAlgorithm.InitialState(grade, parameters), tag.Time, 1,
                    ReviewGrader.IsRecall(grade) ? 0 : 1);
                continue;
            }

            var elapsed = ReviewCalendar.ElapsedDays(record.LastReview, tag.Time);
            record.Apply(FsrsAlgorithm.NextState(record.State, elapsed, grade, parameters), tag.Time, grade);
        }

        return record;
    }

    /// <summary>Ensures the question carries a memory record; returns true if one was created.</summary>
    public static bool EnsureMemory(Question question, SchedulerParameters parameters)
    {
        if (question.Memory is not null) return false;
        var record = Replay(question.ReviewTag, parameters);
        if (record is null) return false;
        question.Memory = record;
        return true;
    }
}
