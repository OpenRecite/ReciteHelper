namespace ReciteHelper.Core.DTOs;

public class ReviewOptions
{
    public int TotalCount { get; set; } = 20;
    public double NewQuestionRatio { get; set; } = 0.3;
    public bool PrioritizeWeakPoints { get; set; } = true;
}
