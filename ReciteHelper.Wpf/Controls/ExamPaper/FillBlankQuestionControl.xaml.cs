using ReciteHelper.Core.Entities;
using ReciteHelper.Wpf.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ReciteHelper.Wpf.Controls.ExamPaper;

public partial class FillBlankQuestionControl : UserControl
{
    private readonly List<TextBox> _answerBoxes = [];
    private ExamQuestionItem? _item;
    private bool _isLoading;

    public FillBlankQuestionControl()
    {
        InitializeComponent();
    }

    public void SetQuestion(ExamQuestionItem item, bool isExamActive)
    {
        _item = item;
        NumberRun.Text = $"{item.Number}. ";
        ScoreRun.Text = $"{item.ScoreLabel} ";
        QuestionRun.Text = item.QuestionText;
        AnswerFieldsPanel.Children.Clear();
        _answerBoxes.Clear();

        var savedAnswers = Question.SplitBlankAnswers(item.UserAnswer);
        _isLoading = true;
        for (var index = 0; index < Math.Max(1, item.Question?.BlankCount ?? 1); index++)
        {
            var field = CreateAnswerField(index, index < savedAnswers.Count ? savedAnswers[index] : string.Empty, isExamActive);
            _answerBoxes.Add(field);
            AnswerFieldsPanel.Children.Add(CreateFieldContainer(index, field));
        }
        _isLoading = false;
    }

    private TextBox CreateAnswerField(int index, string answer, bool isExamActive)
    {
        var field = new TextBox
        {
            Width = 150,
            Padding = new Thickness(3, 2, 3, 2),
            Background = Brushes.Transparent,
            BorderBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
            BorderThickness = new Thickness(0, 0, 0, 1),
            FontFamily = new FontFamily("Times New Roman,SimSun"),
            FontSize = 16,
            Foreground = new SolidColorBrush(Color.FromRgb(159, 24, 38)),
            IsReadOnly = !isExamActive,
            IsTabStop = isExamActive,
            Text = answer,
            Tag = index
        };
        field.TextChanged += AnswerField_TextChanged;
        return field;
    }

    private static FrameworkElement CreateFieldContainer(int index, TextBox field)
    {
        var panel = new StackPanel
        {
            Margin = new Thickness(0, 0, 18, 7),
            Orientation = Orientation.Horizontal
        };
        panel.Children.Add(new TextBlock
        {
            Margin = new Thickness(0, 0, 5, 0),
            VerticalAlignment = VerticalAlignment.Center,
            FontFamily = new FontFamily("Times New Roman,SimSun"),
            FontSize = 14,
            Text = $"({index + 1})"
        });
        panel.Children.Add(field);
        return panel;
    }

    private void AnswerField_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isLoading || _item is null)
            return;

        _item.UserAnswer = Question.JoinBlankAnswers(_answerBoxes.Select(field => field.Text));
    }
}
