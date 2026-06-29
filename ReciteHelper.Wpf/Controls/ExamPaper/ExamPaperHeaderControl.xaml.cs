using ReciteHelper.Wpf.Models;
using System.Windows.Controls;

namespace ReciteHelper.Wpf.Controls.ExamPaper;

public partial class ExamPaperHeaderControl : UserControl
{
    public ExamPaperHeaderControl()
    {
        InitializeComponent();
    }

    public void SetPage(ExamPaperPage page)
    {
        ExamTitleText.Text = string.IsNullOrWhiteSpace(page.ExamTitle)
            ? $"{page.AcademicYearText.Replace("学年", " 学年")}期末学业水平考试"
            : page.ExamTitle;
        SubjectText.Text = page.SubjectName;
    }
}
