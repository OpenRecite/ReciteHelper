using ReciteHelper.Core.DTOs;

namespace ReciteHelper.Wpf.Utilities;

/// <summary>Human-readable summary of a scheduler outcome for the quiz window.</summary>
public static class MemoryHintFormatter
{
    public static string Format(ReviewOutcome outcome)
    {
        var stability = outcome.StateAfter.Stability;
        var stabilityText = stability >= 1
            ? $"{stability:0.#} 天"
            : $"{stability * 24:0.#} 小时";
        var interval = Math.Max(1, (int)Math.Round(outcome.NextIntervalDays));
        var recall = outcome.IsFirstReview
            ? "首次作答"
            : $"答前预测回忆率 {outcome.RetrievabilityBefore * 100:0}%";

        return $"{recall} · 记忆稳定度 {stabilityText} · 建议 {interval} 天后复习";
    }
}
