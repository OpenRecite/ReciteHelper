using ReciteHelper.Core.Enums;
using ReciteHelper.Wpf.ViewModels;
using System.Globalization;
using System.Windows.Data;

namespace ReciteHelper.Wpf.Converters;

public class AnswerStatusToStyleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not QuestionItemViewModel question)
            return System.Windows.Application.Current.FindResource("NotAnsweredCardStyle");

        if (question.IsCurrent)
            return System.Windows.Application.Current.FindResource("CurrentQuestionCardStyle");

        return question.Status switch
        {
            AnswerStatus.Correct => System.Windows.Application.Current.FindResource("CorrectAnswerCardStyle"),
            AnswerStatus.Wrong => System.Windows.Application.Current.FindResource("WrongAnswerCardStyle"),
            _ => System.Windows.Application.Current.FindResource("NotAnsweredCardStyle")
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}