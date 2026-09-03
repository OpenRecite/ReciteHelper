namespace ReciteHelper.Core.Scheduling;

/// <summary>
/// The two latent variables of the DSR memory model for one item.
/// </summary>
/// <param name="Stability">
/// Memory stability S in days: the elapsed time after which the recall
/// probability decays to 90%.
/// </param>
/// <param name="Difficulty">
/// Item difficulty D in [1, 10]; higher values slow down stability growth.
/// </param>
public readonly record struct MemoryState(double Stability, double Difficulty)
{
    public bool IsValid =>
        Stability >= FsrsAlgorithm.MinStability && Stability <= FsrsAlgorithm.MaxStability &&
        Difficulty >= FsrsAlgorithm.MinDifficulty && Difficulty <= FsrsAlgorithm.MaxDifficulty &&
        !double.IsNaN(Stability) && !double.IsNaN(Difficulty);
}
