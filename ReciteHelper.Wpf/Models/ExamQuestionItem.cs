using ReciteHelper.Core.Entities;
using ReciteHelper.Core.Enums;
using ReciteHelper.Core.ValueObjects;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ReciteHelper.Wpf.Models;

public sealed class ExamQuestionItem : INotifyPropertyChanged
{
    private string _userAnswer = string.Empty;
    private ExamAnswerStatus _status;

    public int Number { get; set; }
    public Question? Question { get; set; }
    public int Score { get; set; }

    public string UserAnswer
    {
        get => _userAnswer;
        set
        {
            var normalized = value ?? string.Empty;
            if (_userAnswer == normalized)
                return;

            _userAnswer = normalized;
            Status = string.IsNullOrWhiteSpace(_userAnswer)
                ? ExamAnswerStatus.NotAnswered
                : ExamAnswerStatus.Answered;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsAnswered));
        }
    }

    public ExamAnswerStatus Status
    {
        get => _status;
        set
        {
            if (_status == value)
                return;

            _status = value;
            OnPropertyChanged();
        }
    }

    public string QuestionText => Question?.Text ?? string.Empty;
    public bool IsSingleChoice => Question?.IsSingleChoice is true;
    public bool IsAnswered => !string.IsNullOrWhiteSpace(UserAnswer);
    public IReadOnlyList<QuestionOption> Options => Question?.Options ?? [];
    public string ScoreLabel => $"（{Score}分）";

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
