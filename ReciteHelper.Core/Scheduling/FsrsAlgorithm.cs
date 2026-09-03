namespace ReciteHelper.Core.Scheduling;

/// <summary>
/// Pure, allocation-free implementation of the FSRS-6 memory model
/// (Difficulty–Stability–Retrievability).  Every function is deterministic and
/// side-effect free; state is carried explicitly by <see cref="MemoryState"/>.
///
/// Notation follows the FSRS reference:
///   R(t, S) = (1 + F·t/S)^(-w20)          with F = 0.9^(-1/w20) - 1, so R(S, S) = 0.9
///   S0(G)   = w[G-1]
///   D0(G)   = w4 - e^{w5 (G-1)} + 1
///   D'      = w7·D0(Easy) + (1 - w7)·(D + ΔD·(10 - D)/9),  ΔD = -w6 (G - 3)
///   S'_rec  = S·(1 + e^{w8}·(11 - D)·S^{-w9}·(e^{w10 (1-R)} - 1)·hard·easy)
///   S'_lap  = min( w11·D^{-w12}·((S+1)^{w13} - 1)·e^{w14 (1-R)},  S / e^{w17 w18} )
///   S'_day  = S·f,  f = e^{w17 (G - 3 + w18)}·S^{-w19}, floored at 1 when G ≥ Hard
/// </summary>
public static class FsrsAlgorithm
{
    public const double MinStability = 0.001;
    public const double MaxStability = 36500.0;
    public const double MinDifficulty = 1.0;
    public const double MaxDifficulty = 10.0;

    /// <summary>Probability of recall after <paramref name="elapsedDays"/> given stability.</summary>
    public static double Retrievability(double stability, double elapsedDays, SchedulerParameters p)
    {
        if (elapsedDays <= 0) return 1.0;
        var s = Math.Max(stability, MinStability);
        return Math.Pow(1.0 + p.Factor * elapsedDays / s, p.Decay);
    }

    /// <summary>
    /// The interval (days) after which retrievability falls to <paramref name="desiredRetention"/>.
    /// Inverse of <see cref="Retrievability"/>: t = S/F · (R^(1/decay) - 1).
    /// </summary>
    public static double IntervalForRetention(double stability, double desiredRetention, SchedulerParameters p)
    {
        if (desiredRetention <= 0 || desiredRetention >= 1)
            throw new ArgumentOutOfRangeException(nameof(desiredRetention), "Desired retention must lie strictly between 0 and 1.");
        var s = Math.Max(stability, MinStability);
        return s / p.Factor * (Math.Pow(desiredRetention, 1.0 / p.Decay) - 1.0);
    }

    /// <summary>Memory state right after the first exposure to an item.</summary>
    public static MemoryState InitialState(ReviewGrade grade, SchedulerParameters p)
    {
        var g = (int)grade;
        if (g is < 1 or > 4) throw new ArgumentOutOfRangeException(nameof(grade));

        var stability = Math.Clamp(p[g - 1], MinStability, MaxStability);
        var difficulty = Math.Clamp(InitialDifficulty(g, p), MinDifficulty, MaxDifficulty);
        return new MemoryState(stability, difficulty);
    }

    /// <summary>
    /// Memory state after a review that happened <paramref name="elapsedDays"/> (whole days,
    /// Anki semantics: 0 = same day) after the previous one.
    /// </summary>
    public static MemoryState NextState(MemoryState state, double elapsedDays, ReviewGrade grade, SchedulerParameters p)
    {
        var g = (int)grade;
        if (g is < 1 or > 4) throw new ArgumentOutOfRangeException(nameof(grade));
        if (!state.IsValid) throw new ArgumentException("Memory state is outside the admissible ranges.", nameof(state));

        var s = state.Stability;
        var d = state.Difficulty;

        double newStability;
        if (elapsedDays < 1)
        {
            newStability = SameDayStability(s, g, p);
        }
        else
        {
            var r = Retrievability(s, elapsedDays, p);
            newStability = g > 1
                ? StabilityAfterRecall(s, d, r, g, p)
                : StabilityAfterLapse(s, d, r, p);
        }

        var newDifficulty = NextDifficulty(d, g, p);

        return new MemoryState(
            Math.Clamp(newStability, MinStability, MaxStability),
            Math.Clamp(newDifficulty, MinDifficulty, MaxDifficulty));
    }

    internal static double InitialDifficulty(int grade, SchedulerParameters p)
        => p[4] - Math.Exp(p[5] * (grade - 1)) + 1.0;

    internal static double NextDifficulty(double d, int grade, SchedulerParameters p)
    {
        var deltaD = -p[6] * (grade - 3);
        var damped = d + deltaD * (10.0 - d) / 9.0;             // linear damping
        var target = InitialDifficulty((int)ReviewGrade.Easy, p);  // mean-reversion target D0(Easy)
        return p[7] * target + (1.0 - p[7]) * damped;
    }

    internal static double StabilityAfterRecall(double s, double d, double r, int grade, SchedulerParameters p)
    {
        var hardPenalty = grade == (int)ReviewGrade.Hard ? p[15] : 1.0;
        var easyBonus = grade == (int)ReviewGrade.Easy ? p[16] : 1.0;
        return s * (1.0
                    + Math.Exp(p[8])
                    * (11.0 - d)
                    * Math.Pow(s, -p[9])
                    * (Math.Exp((1.0 - r) * p[10]) - 1.0)
                    * hardPenalty
                    * easyBonus);
    }

    internal static double StabilityAfterLapse(double s, double d, double r, SchedulerParameters p)
    {
        var lapse = p[11]
                    * Math.Pow(d, -p[12])
                    * (Math.Pow(s + 1.0, p[13]) - 1.0)
                    * Math.Exp((1.0 - r) * p[14]);
        var ceiling = s / Math.Exp(p[17] * p[18]);
        return Math.Min(lapse, ceiling);
    }

    internal static double SameDayStability(double s, int grade, SchedulerParameters p)
    {
        var sinc = Math.Exp(p[17] * (grade - 3 + p[18])) * Math.Pow(s, -p[19]);
        if (grade >= (int)ReviewGrade.Hard) sinc = Math.Max(sinc, 1.0);
        return s * sinc;
    }
}
