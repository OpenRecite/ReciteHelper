using System.Windows.Controls;

namespace ReciteHelper.Wpf.Controls.ExamPaper;

public partial class ExamSectionHeaderControl : UserControl
{
    public ExamSectionHeaderControl()
    {
        InitializeComponent();
    }

    public void SetContent(string title, string description)
    {
        TitleRun.Text = $"{title}：";
        DescriptionRun.Text = description;
    }
}
