using System.Text.Json.Serialization;

namespace ReciteHelper.Core.Scheduling;

/// <summary>
/// The 21 parameters of the FSRS-6 memory model, with the population defaults and
/// the admissible ranges published by the open-spaced-repetition project.
/// Instances are immutable; use <see cref="WithWeights"/> to derive a new set.
/// </summary>
public sealed class SchedulerParameters
{
    public const int Count = 21;

    /// <summary>Population defaults of FSRS-6 (fitted on ~20k Anki collections).</summary>
    private static readonly double[] DefaultWeights =
    [
        0.212,  // w0  initial stability after Again
        1.2931, // w1  initial stability after Hard
        2.3065, // w2  initial stability after Good
        8.2956, // w3  initial stability after Easy
        6.4133, // w4  initial difficulty
        0.8334, // w5  initial-difficulty grade offset
        3.0194, // w6  difficulty change per grade step
        0.001,  // w7  difficulty mean reversion
        1.8722, // w8  stability gain after recall (log scale)
        0.1666, // w9  stability saturation exponent
        0.796,  // w10 retrievability bonus after recall
        1.4835, // w11 post-lapse stability scale
        0.0614, // w12 post-lapse difficulty exponent
        0.2629, // w13 post-lapse stability exponent
        1.6483, // w14 post-lapse retrievability exponent
        0.6014, // w15 Hard penalty
        1.8729, // w16 Easy bonus
        0.5425, // w17 same-day stability grade scale
        0.0912, // w18 same-day stability offset
        0.0658, // w19 same-day stability saturation exponent
        0.1542  // w20 forgetting-curve decay
    ];

    private static readonly double[] LowerBounds =
    [
        FsrsAlgorithm.MinStability, FsrsAlgorithm.MinStability, FsrsAlgorithm.MinStability, FsrsAlgorithm.MinStability,
        1.0, 0.001, 0.001, 0.001, 0.0, 0.0, 0.001, 0.001, 0.001, 0.001, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.1
    ];

    private static readonly double[] UpperBounds =
    [
        100.0, 100.0, 100.0, 100.0,
        10.0, 4.0, 4.0, 0.75, 4.5, 0.8, 3.5, 5.0, 0.25, 0.9, 4.0, 1.0, 6.0, 2.0, 2.0, 0.8, 0.8
    ];

    private readonly double[] _w;

    [JsonConstructor]
    public SchedulerParameters(IReadOnlyList<double> weights)
    {
        ArgumentNullException.ThrowIfNull(weights);
        if (weights.Count != Count)
            throw new ArgumentException($"FSRS-6 requires exactly {Count} weights, got {weights.Count}.", nameof(weights));

        _w = new double[Count];
        for (var i = 0; i < Count; i++)
        {
            if (double.IsNaN(weights[i]) || double.IsInfinity(weights[i]))
                throw new ArgumentException($"Weight w{i} is not a finite number.", nameof(weights));
            _w[i] = weights[i];
        }

        Validate();
    }

    public static SchedulerParameters Default { get; } = new(DefaultWeights);

    [JsonPropertyName("weights")]
    public IReadOnlyList<double> Weights => _w;

    public double this[int index] => _w[index];

    public static double LowerBound(int index) => LowerBounds[index];

    public static double UpperBound(int index) => UpperBounds[index];

    /// <summary>Forgetting-curve decay exponent (negative of w20).</summary>
    [JsonIgnore]
    public double Decay => -_w[20];

    /// <summary>
    /// Scale factor of the forgetting curve, chosen so that R(t = S) = 0.9:
    /// factor = 0.9^(1/decay) - 1.
    /// </summary>
    [JsonIgnore]
    public double Factor => Math.Pow(0.9, 1.0 / Decay) - 1.0;

    /// <summary>Returns a copy whose weights are clamped into the admissible ranges.</summary>
    public static SchedulerParameters Clamped(IReadOnlyList<double> weights)
    {
        var w = new double[Count];
        for (var i = 0; i < Count; i++)
            w[i] = Math.Clamp(weights[i], LowerBounds[i], UpperBounds[i]);
        return new SchedulerParameters(w);
    }

    public SchedulerParameters WithWeights(IReadOnlyList<double> weights) => new(weights);

    public bool IsDefault
    {
        get
        {
            for (var i = 0; i < Count; i++)
                if (Math.Abs(_w[i] - DefaultWeights[i]) > 1e-12) return false;
            return true;
        }
    }

    private void Validate()
    {
        for (var i = 0; i < Count; i++)
        {
            if (_w[i] < LowerBounds[i] - 1e-9 || _w[i] > UpperBounds[i] + 1e-9)
                throw new ArgumentOutOfRangeException(nameof(Weights),
                    $"Weight w{i} = {_w[i]} is outside the admissible range [{LowerBounds[i]}, {UpperBounds[i]}].");
        }
    }
}
