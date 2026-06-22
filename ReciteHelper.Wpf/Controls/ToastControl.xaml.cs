using ReciteHelper.Wpf.Services;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace ReciteHelper.Wpf.Controls;

public partial class ToastControl : UserControl, INotifyPropertyChanged
{
    public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register("Message", typeof(string), typeof(ToastControl),
            new PropertyMetadata(null, OnMessageChanged));

    public static readonly DependencyProperty TypeProperty =
        DependencyProperty.Register("Type", typeof(ToastType), typeof(ToastControl),
            new PropertyMetadata(ToastType.Info));

    private Storyboard _showStoryboard;
    private Storyboard _hideStoryboard;

    public ToastControl()
    {
        InitializeComponent();
        DataContext = this;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _showStoryboard = (Storyboard)FindResource("ShowAnimation");
        _hideStoryboard = (Storyboard)FindResource("HideAnimation");
    }

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public ToastType Type
    {
        get => (ToastType)GetValue(TypeProperty);
        set => SetValue(TypeProperty, value);
    }

    private static void OnMessageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (ToastControl)d;
        if (!string.IsNullOrEmpty(e.NewValue as string))
        {
            control.Show();
        }
    }

    public void Show()
    {
        if (_showStoryboard == null) return;

        Visibility = Visibility.Visible;
        _showStoryboard.Begin(this);
    }

    public void Hide()
    {
        if (_hideStoryboard == null) return;

        _hideStoryboard.Completed += (s, e) => Visibility = Visibility.Collapsed;
        _hideStoryboard.Begin(this);
    }

    public event PropertyChangedEventHandler PropertyChanged;
}