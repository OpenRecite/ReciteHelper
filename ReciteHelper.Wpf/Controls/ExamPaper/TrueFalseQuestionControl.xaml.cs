using ReciteHelper.Core.Entities;
using ReciteHelper.Wpf.Models;
using System.Windows;
using System.Windows.Controls;

namespace ReciteHelper.Wpf.Controls.ExamPaper;

public partial class TrueFalseQuestionControl : UserControl
{
    private ExamQuestionItem? _item;
    private bool _isLoading;

    public TrueFalseQuestionControl()
    {
        InitializeComponent();
    }

    public void SetQuestion(ExamQuestionItem item, bool isExamActive)
    {
        _item = item;
        NumberRun.Text = $"{item.Number}. ";
        ScoreRun.Text = $"{item.ScoreLabel} ";
        QuestionRun.Text = item.QuestionText;

        _isLoading = true;
        var answer = Question.NormalizeTrueFalseAnswer(item.UserAnswer);
        TrueRadioButton.IsChecked = answer == "正确";
        FalseRadioButton.IsChecked = answer == "错误";
        TrueRadioButton.IsEnabled = isExamActive;
        FalseRadioButton.IsEnabled = isExamActive;
        _isLoading = false;
    }

    private void Option_Checked(object sender, RoutedEventArgs e)
    {
        if (!_isLoading && _item is not null && sender is RadioButton { Tag: string answer })
            _item.UserAnswer = answer;
    }
}
