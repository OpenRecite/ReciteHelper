namespace ReciteHelper.Core.Scheduling;

/// <summary>
/// Maps what ReciteHelper can observe about an answer to a <see cref="ReviewGrade"/>.
/// The scheduler only ever consumes the judged outcome (binary interface);
/// richer behavioural features are logged in <see cref="Entities.ReviewTag"/>
/// for offline analysis but do not influence scheduling until validated.
/// </summary>
public static class ReviewGrader
{
    /// <summary>Grade derived from the automatic answer judge.</summary>
    public static ReviewGrade FromJudgement(bool isCorrect) => isCorrect ? ReviewGrade.Good : ReviewGrade.Again;

    /// <summary>
    /// Grade derived from a legacy SM-2 quality value (0..5) recorded by earlier versions.
    /// SM-2 treats q ≥ 3 as a successful recall; the same threshold is used here.
    /// </summary>
    public static ReviewGrade FromLegacyQuality(int quality) => quality >= 3 ? ReviewGrade.Good : ReviewGrade.Again;

    /// <summary>Whether a grade counts as a successful recall.</summary>
    public static bool IsRecall(ReviewGrade grade) => grade != ReviewGrade.Again;
}
