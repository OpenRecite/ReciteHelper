// ReciteHelper.Wpf/ViewModels/QuizViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using ReciteHelper.Application.DTOs;
using ReciteHelper.Application.Interfaces.Services;
using ReciteHelper.Application.Services;
using ReciteHelper.Core.Aggregates;
using ReciteHelper.Core.Entities;
using ReciteHelper.Core.Enums;
using ReciteHelper.Core.Interfaces.Services;
using ReciteHelper.Core.ValueObjects;
using ReciteHelper.Infrastructure.Configuration;
using ReciteHelper.Wpf.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;

namespace ReciteHelper.Wpf.ViewModels;

public partial class QuizViewModel : ObservableObject, IDisposable
{
    private readonly IQuizService _quizService;
    private readonly IPhonkService _phonkService;
    private readonly IToastService _toastService;
    private readonly IConfigService _configService;
    private readonly Project _project;
    private readonly string _chapterName;
    private readonly LatestBuffer<bool> _latest;

    private DateTime _currentQuestionStartTime;
    private List<QuestionItemViewModel> _allQuestions;

    [ObservableProperty]
    private ObservableCollection<QuestionItemViewModel> _questions;

    [ObservableProperty]
    private QuestionItemViewModel _currentQuestion;

    [ObservableProperty]
    private int _currentIndex;

    [ObservableProperty]
    private int _totalQuestions;

    [ObservableProperty]
    private string _answerText;

    [ObservableProperty]
    private bool _isAnswerEnabled = true;

    [ObservableProperty]
    private bool _isPrevEnabled;

    [ObservableProperty]
    private bool _isNextEnabled = true;

    [ObservableProperty]
    private Visibility _resultVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private string _resultTitle;

    [ObservableProperty]
    private Brush _resultBackground;

    [ObservableProperty]
    private Brush _resultForeground;

    [ObservableProperty]
    private string _userAnswer;

    [ObservableProperty]
    private string _correctAnswer;

    [ObservableProperty]
    private string _qValueDisplay;

    // Trigger for phonk animation
    public event EventHandler<PhonkEventArgs>? PhonkRequested;

# pragma warning disable CS8618

    public QuizViewModel(
        Project project,
        string chapterName,
        IQuizService quizService,
        IPhonkService phonkService,
        IToastService toastService,
        IConfigService configService)
    {
        _project = project;
        _chapterName = chapterName;
        _quizService = quizService;
        _phonkService = phonkService;
        _toastService = toastService;
        _configService = configService;

        var config = _configService.LoadAsync().Result;
        _latest = LatestBuffer<bool>.Create<bool>(config.PhonkOptions.WrongCount);

        InitializeQuestions();

        _phonkService.PhonkTriggered += OnPhonkTriggered;
    }

    private void InitializeQuestions()
    {
        var questions = _project.Chapters!.Find(x => x.Name == _chapterName)!.Questions!;

        _allQuestions = questions.Select((q, index) => new QuestionItemViewModel
        {
            Number = index + 1,
            Question = q,
            Status = q.Status switch
            {
                true => AnswerStatus.Correct,
                false => AnswerStatus.Wrong,
                null => AnswerStatus.NotAnswered
            }
        }).ToList();

        Questions = new ObservableCollection<QuestionItemViewModel>(_allQuestions);
        TotalQuestions = Questions.Count;

        CurrentIndex = _allQuestions.FindIndex(q => q.Status == AnswerStatus.NotAnswered);
        if (CurrentIndex < 0) CurrentIndex = 0;

        UpdateDisplay();
    }

    partial void OnCurrentIndexChanged(int value)
    {
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        CurrentQuestion = Questions[CurrentIndex];

        IsPrevEnabled = CurrentIndex > 0;
        IsNextEnabled = CurrentIndex < TotalQuestions - 1;

        AnswerText = "";
        IsAnswerEnabled = CurrentQuestion.Status == AnswerStatus.NotAnswered;

        if (CurrentQuestion.Status != AnswerStatus.NotAnswered)
        {
            ShowResult(CurrentQuestion);
        }
        else
        {
            ResultVisibility = Visibility.Collapsed;
            _currentQuestionStartTime = DateTime.Now;
        }

        UpdateAnswerCardStyles();
    }

    private void ShowResult(QuestionItemViewModel question)
    {
        ResultVisibility = Visibility.Visible;

        if (question.Status == AnswerStatus.Correct)
        {
            ResultTitle = "回答正确！";
            ResultForeground = new SolidColorBrush(Color.FromRgb(21, 87, 36));
            ResultBackground = new SolidColorBrush(Color.FromRgb(212, 237, 218));
        }
        else
        {
            ResultTitle = "回答错误！";
            ResultForeground = new SolidColorBrush(Color.FromRgb(114, 28, 36));
            ResultBackground = new SolidColorBrush(Color.FromRgb(248, 215, 218));
        }

        UserAnswer = question.UserAnswer ?? "";
        CorrectAnswer = question.Question.CorrectAnswer!;
    }

    private void UpdateAnswerCardStyles()
    {
        foreach (var question in Questions)
        {
            question.IsCurrent = question.Number == CurrentIndex + 1;
        }
    }

    [RelayCommand]
    private async Task SubmitAnswerAsync()
    {
        if (string.IsNullOrWhiteSpace(AnswerText))
        {
            _toastService.ShowWarning("请输入答案");
            return;
        }

        try
        {
            IsAnswerEnabled = false;


            var result = await _quizService.ProcessAnswerAsync(CurrentQuestion.Question, AnswerText.Trim(), _currentQuestionStartTime);

            // Update related status
            CurrentQuestion.UserAnswer = AnswerText.Trim();
            CurrentQuestion.Status = result.IsCorrect ? AnswerStatus.Correct : AnswerStatus.Wrong;

            // Update data
            CurrentQuestion.Question.EFValue = result.NewEFValue;

            // Add review record
            var reviewTag = new ReviewTag
            {
                Rate = result.RRelative,
                Time = DateTime.Now,
                Similarity = result.Similarity,
                QValue = result.QValue
            };
            CurrentQuestion.Question.ReviewTag.Add(reviewTag);

            // Show results
            ShowResult(CurrentQuestion);
            QValueDisplay = $"Q Predict: {result.QValue}";

            // Check & trigger Phonk
            _latest.Add(result.IsCorrect);
            if (_latest.EqualsTo(false) && _phonkService.IsEnabled)
            {
                await _phonkService.PlayRandomPhonkAsync();
            }
        }
        catch (Exception ex)
        {
            _toastService.ShowError($"提交失败：{ex.Message}");
        }
        finally
        {
            IsAnswerEnabled = true;
        }
    }

    [RelayCommand]
    private void SwitchToQuestion(int questionNumber)
    {
        var targetIndex = questionNumber - 1;
        if (targetIndex >= 0 && targetIndex < TotalQuestions)
        {
            CurrentIndex = targetIndex;
        }
    }

    [RelayCommand]
    private void PrevQuestion()
    {
        if (CurrentIndex > 0)
        {
            CurrentIndex--;
        }
    }

    [RelayCommand]
    private void NextQuestion()
    {
        if (CurrentIndex < TotalQuestions - 1)
        {
            CurrentIndex++;
        }
    }

    [RelayCommand]
    private async Task ClearRecordsAsync()
    {
        var confirm = true;
        if (!confirm) return;

        foreach (var question in Questions)
        {
            question.UserAnswer = null;
            question.Status = AnswerStatus.NotAnswered;
            question.Question.Status = null;
        }

        CurrentIndex = 0;
        _toastService.ShowInfo("记录已清空");
    }

    private void OnPhonkTriggered(object? sender, PhonkEventArgs e)
    {
        PhonkRequested?.Invoke(this, e);
    }

    public void Dispose()
    {
        _phonkService.PhonkTriggered -= OnPhonkTriggered;
    }
}

// ReciteHelper.Wpf/ViewModels/QuestionItemViewModel.cs
public partial class QuestionItemViewModel : ObservableObject
{
    [ObservableProperty]
    private int _number;

    [ObservableProperty]
    private Question _question;

    [ObservableProperty]
    private AnswerStatus _status;

    [ObservableProperty]
    private string? _userAnswer;

    [ObservableProperty]
    private bool _isCurrent;

    [ObservableProperty]
    private Style _cardStyle;

    partial void OnStatusChanged(AnswerStatus value)
    {
        UpdateStyle();
    }

    partial void OnIsCurrentChanged(bool value)
    {
        UpdateStyle();
    }

    private void UpdateStyle()
    {
        // 样式逻辑移到 Converter 中处理
    }
}