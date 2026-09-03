namespace ReciteHelper.Core.Scheduling;

/// <summary>
/// The FSRS-6 update rules evaluated on <see cref="Dual"/> numbers, so that the
/// gradient of any downstream loss with respect to the 21 parameters is obtained
/// in a single forward pass.  The formulas are identical to <see cref="FsrsAlgorithm"/>;
/// a unit test checks that the two agree to rounding error.
/// </summary>
public sealed class FsrsDualModel
{
    private readonly Dual[] _w = new Dual[SchedulerParameters.Count];

    public FsrsDualModel(IReadOnlyList<double> weights)
    {
        for (var i = 0; i < SchedulerParameters.Count; i++)
            _w[i] = Dual.Parameter(weights[i], i);
    }

    public Dual this[int index] => _w[index];

    public Dual Decay => -_w[20];

    public Dual Factor => Dual.Pow(0.9, 1.0 / Decay) - 1.0;

    public Dual Retrievability(in Dual stability, double elapsedDays)
    {
        if (elapsedDays <= 0) return 1.0;
        return Dual.Pow(1.0 + Factor * elapsedDays / stability, Decay);
    }

    public (Dual Stability, Dual Difficulty) InitialState(ReviewGrade grade)
    {
        var g = (int)grade;
        var stability = Dual.Clamp(_w[g - 1], FsrsAlgorithm.MinStability, FsrsAlgorithm.MaxStability);
        var difficulty = Dual.Clamp(InitialDifficulty(g), FsrsAlgorithm.MinDifficulty, FsrsAlgorithm.MaxDifficulty);
        return (stability, difficulty);
    }

    public (Dual Stability, Dual Difficulty) NextState(in Dual s, in Dual d, double elapsedDays, ReviewGrade grade)
    {
        var g = (int)grade;
        Dual newStability;
        if (elapsedDays < 1)
        {
            newStability = SameDayStability(s, g);
        }
        else
        {
            var r = Retrievability(s, elapsedDays);
            newStability = g > 1 ? StabilityAfterRecall(s, d, r, g) : StabilityAfterLapse(s, d, r);
        }

        var newDifficulty = NextDifficulty(d, g);
        return (
            Dual.Clamp(newStability, FsrsAlgorithm.MinStability, FsrsAlgorithm.MaxStability),
            Dual.Clamp(newDifficulty, FsrsAlgorithm.MinDifficulty, FsrsAlgorithm.MaxDifficulty));
    }

    private Dual InitialDifficulty(int grade) => _w[4] - Dual.Exp(_w[5] * (grade - 1)) + 1.0;

    private Dual NextDifficulty(in Dual d, int grade)
    {
        var deltaD = -_w[6] * (grade - 3);
        var damped = d + deltaD * (10.0 - d) / 9.0;
        var target = InitialDifficulty((int)ReviewGrade.Easy);
        return _w[7] * target + (1.0 - _w[7]) * damped;
    }

    private Dual StabilityAfterRecall(in Dual s, in Dual d, in Dual r, int grade)
    {
        Dual hardPenalty = grade == (int)ReviewGrade.Hard ? _w[15] : 1.0;
        Dual easyBonus = grade == (int)ReviewGrade.Easy ? _w[16] : 1.0;
        return s * (1.0
                    + Dual.Exp(_w[8])
                    * (11.0 - d)
                    * Dual.Pow(s, -_w[9])
                    * (Dual.Exp((1.0 - r) * _w[10]) - 1.0)
                    * hardPenalty
                    * easyBonus);
    }

    private Dual StabilityAfterLapse(in Dual s, in Dual d, in Dual r)
    {
        var lapse = _w[11]
                    * Dual.Pow(d, -_w[12])
                    * (Dual.Pow(s + 1.0, _w[13]) - 1.0)
                    * Dual.Exp((1.0 - r) * _w[14]);
        var ceiling = s / Dual.Exp(_w[17] * _w[18]);
        return Dual.Min(lapse, ceiling);
    }

    private Dual SameDayStability(in Dual s, int grade)
    {
        var sinc = Dual.Exp(_w[17] * (grade - 3 + _w[18])) * Dual.Pow(s, -_w[19]);
        if (grade >= (int)ReviewGrade.Hard) sinc = Dual.Max(sinc, 1.0);
        return s * sinc;
    }
}
