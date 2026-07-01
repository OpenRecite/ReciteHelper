using ReciteHelper.Core.Entities;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ReciteHelper.Wpf.Models;

public sealed class WrongQuestionImportCandidate : INotifyPropertyChanged
{
    private bool _isSelected;

    public int Number { get; init; }
    public Question Question { get; init; } = new();
    public string CorrectAnswer { get; init; } = string.Empty;
    public string UserAnswer { get; init; } = string.Empty;
    public bool HasSimilarQuestion { get; init; }
    public double Similarity { get; init; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
                return;

            _isSelected = value;
            OnPropertyChanged();
        }
    }

    public string DuplicateHint => HasSimilarQuestion
        ? $"在题库中已存在高相似度题目，相似度 {Similarity:P0}。"
        : string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
