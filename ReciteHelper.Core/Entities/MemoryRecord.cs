using ReciteHelper.Core.Scheduling;
using System.Text.Json.Serialization;

namespace ReciteHelper.Core.Entities;

/// <summary>
/// Persisted memory state of one question under the FSRS-6 model.
/// Three numbers fully determine future scheduling; the review history is not needed.
/// </summary>
public sealed class MemoryRecord
{
    public const string CurrentAlgorithm = "FSRS-6";

    [JsonPropertyName("stability")]
    public double Stability { get; set; }

    [JsonPropertyName("difficulty")]
    public double Difficulty { get; set; }

    [JsonPropertyName("last_review")]
    public DateTime LastReview { get; set; }

    [JsonPropertyName("reps")]
    public int Reps { get; set; }

    [JsonPropertyName("lapses")]
    public int Lapses { get; set; }

    [JsonPropertyName("algorithm")]
    public string Algorithm { get; set; } = CurrentAlgorithm;

    [JsonIgnore]
    public MemoryState State => new(Stability, Difficulty);

    public static MemoryRecord From(MemoryState state, DateTime reviewedAt, int reps, int lapses) => new()
    {
        Stability = state.Stability,
        Difficulty = state.Difficulty,
        LastReview = reviewedAt,
        Reps = reps,
        Lapses = lapses
    };

    public void Apply(MemoryState state, DateTime reviewedAt, ReviewGrade grade)
    {
        Stability = state.Stability;
        Difficulty = state.Difficulty;
        LastReview = reviewedAt;
        Reps++;
        if (!ReviewGrader.IsRecall(grade)) Lapses++;
    }
}
