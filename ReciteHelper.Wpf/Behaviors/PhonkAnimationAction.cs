using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Microsoft.Xaml.Behaviors;
using ReciteHelper.Core.EventArgs;

namespace ReciteHelper.Wpf.Behaviors;

public class PhonkAnimationAction : TriggerAction<DependencyObject>
{
    public static readonly DependencyProperty TargetImageProperty =
        DependencyProperty.Register(nameof(TargetImage), typeof(Image), typeof(PhonkAnimationAction));

    public static readonly DependencyProperty TargetPlayerProperty =
        DependencyProperty.Register(nameof(TargetPlayer), typeof(MediaElement), typeof(PhonkAnimationAction));

    public Image TargetImage
    {
        get => (Image)GetValue(TargetImageProperty);
        set => SetValue(TargetImageProperty, value);
    }

    public MediaElement TargetPlayer
    {
        get => (MediaElement)GetValue(TargetPlayerProperty);
        set => SetValue(TargetPlayerProperty, value);
    }

    protected override void Invoke(object parameter)
    {
        if (TargetImage is null || TargetPlayer is null) return;

        if (parameter is not PhonkEventArgs args) return;

        // Set image
        TargetImage.Source = new BitmapImage(new Uri(args.ImageUri));
        TargetImage.Visibility = Visibility.Visible;

        // Play sound effect
        TargetPlayer.Source = new Uri(args.SoundFile);
        TargetPlayer.Play();

        // Animation: Slide in from the right
        var translate = TargetImage.RenderTransform as TranslateTransform ?? new TranslateTransform();
        TargetImage.RenderTransform = translate;

        var storyboard = new Storyboard();

        var moveAnim = new DoubleAnimation
        {
            From = 1000,
            To = 0,
            Duration = TimeSpan.FromMilliseconds(250),
            EasingFunction = new BackEase { Amplitude = 0.8, EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(moveAnim, TargetImage);
        Storyboard.SetTargetProperty(moveAnim, new PropertyPath("RenderTransform.X"));

        var opacityAnim = new DoubleAnimation
        {
            To = 1,
            Duration = TimeSpan.FromMilliseconds(50)
        };
        Storyboard.SetTarget(opacityAnim, TargetImage);
        Storyboard.SetTargetProperty(opacityAnim, new PropertyPath(Image.OpacityProperty));

        storyboard.Children.Add(moveAnim);
        storyboard.Children.Add(opacityAnim);

        storyboard.Completed += (s, e) =>
        {
            // Hide after 5 seconds
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            timer.Tick += (timerSender, timerArgs) =>
            {
                timer.Stop();
                TargetImage.Visibility = Visibility.Collapsed;
                TargetPlayer.Stop();
            };
            timer.Start();
        };

        storyboard.Begin();
    }
}