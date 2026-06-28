namespace ReciteHelper.Wpf.Models;

public enum ExamPaperElementKind
{
    SectionHeader,
    ChoiceQuestion,
    EssayQuestion
}

public sealed class ExamPaperElement
{
    public required ExamPaperElementKind Kind { get; init; }
    public ExamQuestionItem? Question { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
}

public sealed class ExamPaperPage
{
    public int PageNumber { get; init; }
    public int TotalPages { get; set; }
    public bool ShowPaperHeader { get; init; }
    public bool IsExamActive { get; set; }
    public required string SubjectName { get; init; }
    public required string AcademicYearText { get; init; }
    public List<ExamPaperElement> Elements { get; } = [];
}
