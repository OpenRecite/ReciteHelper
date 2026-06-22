using CommunityToolkit.Mvvm.ComponentModel;
using ReciteHelper.Core.Entities;
using ReciteHelper.Core.Enums;
using System.Windows;

namespace ReciteHelper.Wpf.ViewModels;

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
