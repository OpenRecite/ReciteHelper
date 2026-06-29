using ReciteHelper.Wpf.Models;
using System.Windows.Controls;

namespace ReciteHelper.Wpf.Controls.ExamPaper;

public partial class ExamPaperPageControl : UserControl
{
    public ExamPaperPageControl()
    {
        InitializeComponent();
    }

    public void SetPage(ExamPaperPage page)
    {
        PageContentPanel.Children.Clear();

        if (page.ShowPaperHeader)
        {
            var header = new ExamPaperHeaderControl();
            header.SetPage(page);
            PageContentPanel.Children.Add(header);
        }

        foreach (var element in page.Elements)
        {
            switch (element.Kind)
            {
                case ExamPaperElementKind.SectionHeader:
                    var section = new ExamSectionHeaderControl();
                    section.SetContent(element.Title ?? string.Empty, element.Description ?? string.Empty);
                    PageContentPanel.Children.Add(section);
                    break;
                case ExamPaperElementKind.ChoiceQuestion when element.Question is not null:
                    var choice = new ChoiceQuestionControl();
                    choice.SetQuestion(element.Question, page.IsExamActive);
                    PageContentPanel.Children.Add(choice);
                    break;
                case ExamPaperElementKind.FillBlankQuestion when element.Question is not null:
                    var fillBlank = new FillBlankQuestionControl();
                    fillBlank.SetQuestion(element.Question, page.IsExamActive);
                    PageContentPanel.Children.Add(fillBlank);
                    break;
                case ExamPaperElementKind.TrueFalseQuestion when element.Question is not null:
                    var trueFalse = new TrueFalseQuestionControl();
                    trueFalse.SetQuestion(element.Question, page.IsExamActive);
                    PageContentPanel.Children.Add(trueFalse);
                    break;
                case ExamPaperElementKind.TermDefinitionQuestion when element.Question is not null:
                    var termDefinition = new EssayQuestionControl();
                    termDefinition.SetQuestion(element.Question, page.IsExamActive);
                    PageContentPanel.Children.Add(termDefinition);
                    break;
                case ExamPaperElementKind.EssayQuestion when element.Question is not null:
                    var essay = new EssayQuestionControl();
                    essay.SetQuestion(element.Question, page.IsExamActive);
                    PageContentPanel.Children.Add(essay);
                    break;
            }
        }

        FooterText.Text = $"{page.SubjectName}试题  第 {page.PageNumber} 页（共 {page.TotalPages} 页）";
    }
}
