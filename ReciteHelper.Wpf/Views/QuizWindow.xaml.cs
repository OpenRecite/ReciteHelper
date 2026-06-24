using ReciteHelper.Core.Interfaces.Services;
using ReciteHelper.Core.Aggregates;
using ReciteHelper.Core.Entities;
using ReciteHelper.Core.Enums;
using ReciteHelper.Core.ValueObjects;
using ReciteHelper.Wpf.Models;
using ReciteHelper.Wpf.ViewModels;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace ReciteHelper.Wpf.Views;

public partial class QuizWindow : Window, INotifyPropertyChanged
{
    private readonly IQuizService _quizService;
    private readonly IProjectFileService _projectFileService;
    private ObservableCollection<QuestionItem> _questions;
    private LatestBuffer<bool> _latest;
    private int _currentQuestionIndex = 0;
    private int _totalQuestions = 0;
    private string _chapterName = "";
    private Project _project = new();
    private DateTime _startTime = DateTime.Now;
    private string? _selectedChoiceId;

    public QuizWindow(Project project, string chapterName, IQuizService quizService, IProjectFileService projectFileService)
    {
        _quizService = quizService;
        _projectFileService = projectFileService;

        InitializeComponent();
        DataContext = this;

        _project = project;
        _chapterName= chapterName;
        _latest = LatestBuffer<bool>.Create<bool>(Config.Configure.PhonkOptions.WrongCount);

        InitializeQuestions(project.Chapters!.Find(x => x.Name == chapterName)!.Questions!);
        LocateCurrent();
        UpdateDisplay();
    }


    public QuizWindow(Project project, List<Question> recitePlan, IQuizService quizService, IProjectFileService projectFileService)
    {
        _quizService = quizService;
        _projectFileService = projectFileService;

        InitializeComponent();
        DataContext = this;

        _project = project;
        _chapterName= "复习计划";
        _latest = LatestBuffer<bool>.Create<bool>(Config.Configure.PhonkOptions.WrongCount);

        InitializeQuestions(recitePlan);
        LocateCurrent();
        UpdateDisplay();
    }

    private void SwitchToQuestion(int questionNumber)
    {
        if (questionNumber < 1 || questionNumber > _totalQuestions)
            return;

        int targetIndex = questionNumber - 1;

        if (targetIndex == _currentQuestionIndex)
            return;

        _currentQuestionIndex = targetIndex;
        UpdateDisplay();
    }

    private void SwitchButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.CommandParameter is int questionNumber)
        {
            SwitchToQuestion(questionNumber);
        }
    }

    private void InitializeQuestions(List<Question> questions)
    {
        _questions = new ObservableCollection<QuestionItem>();

        for (int i = 0; i < questions.Count; i++)
        {
            _questions.Add(new QuestionItem
            {
                Number = i + 1,
                Question = questions[i],
                Status = questions[i].Status switch
                {
                    true => AnswerStatus.Correct,
                    false => AnswerStatus.Wrong,
                    null => AnswerStatus.NotAnswered
                },
                StatusStyle = (Style)FindResource("AnswerCardButtonStyle")
            });
        }

        _totalQuestions = _questions.Count;
        AnswerCardItemsControl.ItemsSource = _questions;
        UpdateAnswerCardStyles();
    }

    private void UpdateDisplay()
    {
        if (_questions == null || _questions.Count == 0) return;

        var currentQuestion = _questions[_currentQuestionIndex];

        // Update question display
        CurrentQuestionText.Text = (_currentQuestionIndex + 1).ToString();
        TotalQuestionsText.Text = _totalQuestions.ToString();
        QuestionTextBlock.Text = currentQuestion.Question!.Text;

        ConfigureAnswerInput(currentQuestion);

        // Update button state
        PrevButton.IsEnabled = _currentQuestionIndex > 0;
        NextButton.IsEnabled = _currentQuestionIndex < _totalQuestions - 1;

        // Hide the results area (if it's a new question)
        if (currentQuestion.Status == AnswerStatus.NotAnswered)
        {
            ResultArea.Visibility = Visibility.Collapsed;
        }
        else
        {
            ShowResult(currentQuestion);
        }

        UpdateAnswerCardStyles();
    }

    private void ConfigureAnswerInput(QuestionItem currentQuestion)
    {
        var question = currentQuestion.Question!;
        var isEnabled = currentQuestion.Status == AnswerStatus.NotAnswered;

        if (question.IsSingleChoice)
        {
            _selectedChoiceId = Question.ExtractOptionId(currentQuestion.UserAnswer);
            AnswerPromptText.Text = "请选择答案：";
            AnswerTextBox.Text = "";
            AnswerTextBox.Visibility = Visibility.Collapsed;
            ChoiceOptionsItemsControl.Visibility = Visibility.Visible;
            ChoiceOptionsItemsControl.IsEnabled = isEnabled;
            ChoiceOptionsItemsControl.ItemsSource = question.Options;
        }
        else
        {
            _selectedChoiceId = null;
            AnswerPromptText.Text = "请输入答案：";
            ChoiceOptionsItemsControl.ItemsSource = null;
            ChoiceOptionsItemsControl.Visibility = Visibility.Collapsed;
            AnswerTextBox.Visibility = Visibility.Visible;
            AnswerTextBox.Text = "";
            AnswerTextBox.IsEnabled = isEnabled;
        }

        if (isEnabled)
            _startTime = DateTime.Now;
    }

    private void UpdateAnswerCardStyles()
    {
        foreach (var question in _questions)
        {
            // Reset to basic style
            question.StatusStyle = (Style)FindResource("AnswerCardButtonStyle");

            // Apply styles based on status
            switch (question.Status)
            {
                case AnswerStatus.Correct:
                    question.StatusStyle = (Style)FindResource("CorrectAnswerStyle");
                    break;
                case AnswerStatus.Wrong:
                    question.StatusStyle = (Style)FindResource("WrongAnswerStyle");
                    break;
            }

            // If this is the current question, add a border style
            if (question.Number == _currentQuestionIndex + 1)
            {
                var currentStyle = new Style(typeof(Button), question.StatusStyle);
                currentStyle.Setters.Add(new Setter(Button.BorderBrushProperty, new SolidColorBrush(Colors.Blue)));
                currentStyle.Setters.Add(new Setter(Button.BorderThicknessProperty, new Thickness(3)));
                question.StatusStyle = currentStyle;
            }
        }
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("是否确认清空答题记录？", "清空记录",
            MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
        {
            for (int i = 0; i < _questions.Count(); i++)
            {
                _questions[i].UserAnswer = null;
                _questions[i].Status = AnswerStatus.NotAnswered;
            }
        }

        Window_Closing(sender, null!);
        Close();
    }

    private void ShowResult(QuestionItem question)
    {
        ResultArea.Visibility = Visibility.Visible;

        switch (question.Status)
        {
            case AnswerStatus.Correct:
                ResultTitleText.Text = "回答正确！";
                ResultTitleText.Foreground = new SolidColorBrush(Color.FromRgb(21, 87, 36));
                ResultArea.Background = new SolidColorBrush(Color.FromRgb(212, 237, 218));
                ResultArea.BorderBrush = new SolidColorBrush(Color.FromRgb(195, 230, 203));
                break;
            case AnswerStatus.Wrong:
                ResultTitleText.Text = "回答错误！";
                ResultTitleText.Foreground = new SolidColorBrush(Color.FromRgb(114, 28, 36));
                ResultArea.Background = new SolidColorBrush(Color.FromRgb(248, 215, 218));
                ResultArea.BorderBrush = new SolidColorBrush(Color.FromRgb(245, 198, 203));
                break;
        }

        UserAnswerText.Text = question.UserAnswer ?? "";
        CorrectAnswerText.Text = question.Question.GetCorrectAnswerText();
    }

    private void LocateCurrent()
    {
        for (int i = 0; i < _questions.Count(); i++)
        {
            if (_questions[i].Status == AnswerStatus.NotAnswered)
            {
                _currentQuestionIndex = i;
                return;
            }
        }
    }

    private async void SubmitButton_Click(object sender, RoutedEventArgs e)
    {
        var currentQuestion = _questions[_currentQuestionIndex];
        var answerText = GetCurrentAnswerText(currentQuestion);

        if (string.IsNullOrWhiteSpace(answerText))
        {
            MessageBox.Show("请输入答案", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        currentQuestion.UserAnswer = answerText.Trim();

        var answerResult = await _quizService.ProcessAnswerAsync(
            currentQuestion.Question!,
            currentQuestion.UserAnswer,
            _startTime);

        currentQuestion.Status = answerResult.IsCorrect ? AnswerStatus.Correct : AnswerStatus.Wrong;

        // Show result
        ShowResult(currentQuestion);
        AnswerTextBox.IsEnabled = false;
        ChoiceOptionsItemsControl.IsEnabled = false;
        UpdateAnswerCardStyles();

        var tagCount = _questions[_currentQuestionIndex].Question!.ReviewTag.Count;
        answerResult.ReviewTag.SetId(tagCount + 1);
        _questions[_currentQuestionIndex].Question!.ReviewTag.Add(answerResult.ReviewTag);

        currentQuestion.Question!.EFValue = answerResult.NewEFValue;
        QDisplayLabel.Content = $"Q Predict: {answerResult.QValue}";

        // Play phonk effect
        _latest.Add(answerResult.IsCorrect);
        if (_latest.EqualsTo(false) && Config.Configure.PhonkOptions.EnablePhonk)
            await PlayPhonkEffect();
    }

    private string GetCurrentAnswerText(QuestionItem currentQuestion)
    {
        var question = currentQuestion.Question!;
        return question.IsSingleChoice
            ? question.GetOptionDisplayText(_selectedChoiceId)
            : AnswerTextBox.Text.Trim();
    }

    private async Task PlayPhonkEffect()
    {
        var num = Random.Shared.Next(1, 10);
        var caveira = $"pack://application:,,,/ReciteHelper.Wpf;component/Images/Phonk/Caveira/caveira{num}.png";
        string sound = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "Phonk", "Soundfx", $"phonk{num}.mp3");

        PhonkImage.Source = new BitmapImage(new Uri(caveira));
        PhonkPlayer.Source = new Uri(sound, UriKind.Absolute);

        ImageTranslate.X = 1000;
        PhonkImage.Opacity = 0;

        var sb = new Storyboard();

        var moveAnim = new DoubleAnimation
        {
            From = 1000,
            To = 0,
            Duration = TimeSpan.FromMilliseconds(250),
            EasingFunction = new BackEase { Amplitude = 0.8, EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(moveAnim, PhonkImage);
        Storyboard.SetTargetProperty(moveAnim, new PropertyPath("RenderTransform.(TranslateTransform.X)"));

        var opacityAnim = new DoubleAnimation { To = 1, Duration = TimeSpan.FromMilliseconds(50) };
        Storyboard.SetTarget(opacityAnim, PhonkImage);
        Storyboard.SetTargetProperty(opacityAnim, new PropertyPath("Opacity"));

        sb.Children.Add(moveAnim);
        sb.Children.Add(opacityAnim);

        sb.Begin();
        PhonkPlayer.Play();

        await Task.Delay(5000);

        PhonkPlayer.Stop();
        PhonkImage.Source = null;
        PhonkImage.Opacity = 0;
    }

    private void SkipButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentQuestionIndex < _totalQuestions - 1)
        {
            _currentQuestionIndex++;
            UpdateDisplay();
        }
    }

    private void PrevButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentQuestionIndex > 0)
        {
            _currentQuestionIndex--;
            UpdateDisplay();
        }
    }

    private void NextButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentQuestionIndex < _totalQuestions - 1)
        {
            _currentQuestionIndex++;
            UpdateDisplay();
        }
    }

    private void AnswerTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        // Trash
    }

    private void ChoiceOption_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton radioButton)
            _selectedChoiceId = radioButton.Tag?.ToString();
    }


    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private async void Window_Closing(object sender, CancelEventArgs e)
    {
        if (_project is null) return;

        // Save record
        var chapter = _project.Chapters!.Find(x => x.Name == _chapterName)!;
        for (int i = 0; i < _questions.Count; i++)
        {
            chapter.Questions![i].Status = _questions[i].Status switch
            {
                AnswerStatus.NotAnswered => null,
                AnswerStatus.Correct => true,
                AnswerStatus.Wrong => false,
                _ => throw new NotImplementedException("Fuck U")
            };

        }

        await _projectFileService.SaveProjectAsync(_project);
    }
}
