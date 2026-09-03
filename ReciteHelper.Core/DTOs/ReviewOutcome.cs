using ReciteHelper.Core.Scheduling;

namespace ReciteHelper.Core.DTOs;

/// <summary>Result of recording one review against the memory model.</summary>
public sealed class ReviewOutcome
{
    public required ReviewGrade Grade { get; init; }

    /// <summary>Whole study days since the previous review; 0 for a first exposure or same-day review.</summary>
    public required int ElapsedDays { get; init; }

    /// <summary>Predicted recall probability immediately before this review (1 for a first exposure).</summary>
    public required double RetrievabilityBefore { get; init; }

    public required MemoryState StateAfter { get; init; }

    /// <summary>Days until retrievability decays to the desired retention.</summary>
    public required double NextIntervalDays { get; init; }

    public required bool IsFirstReview { get; init; }
}
