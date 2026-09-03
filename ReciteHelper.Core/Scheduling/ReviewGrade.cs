namespace ReciteHelper.Core.Scheduling;

/// <summary>
/// Outcome grade of one review, on the four-level FSRS scale.
/// ReciteHelper judges answers automatically, so only <see cref="Again"/> and
/// <see cref="Good"/> are produced in normal operation (see <see cref="ReviewGrader"/>);
/// the other two levels are kept so that imported or manually graded histories
/// can be replayed without loss.
/// </summary>
public enum ReviewGrade
{
    /// <summary>Recall failed (the answer was judged wrong).</summary>
    Again = 1,

    /// <summary>Recalled with noticeable difficulty.</summary>
    Hard = 2,

    /// <summary>Recalled correctly.</summary>
    Good = 3,

    /// <summary>Recalled effortlessly.</summary>
    Easy = 4
}
