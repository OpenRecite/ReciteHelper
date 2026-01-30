using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ReciteHelper.DataCollect
{
    public partial class MainWindow : Window
    {
        private int currentStep = 1;

        public MainWindow()
        {
            InitializeComponent();
            NavigateToStep(1);
        }

        private void StepNavigation_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is string stepTag)
            {
                int stepNumber = int.Parse(stepTag);

                if (stepNumber <= currentStep || stepNumber == currentStep + 1)
                {
                    NavigateToStep(stepNumber);
                }
                else
                {
                    MessageBox.Show("请按顺序完成前面的步骤", "提示",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        public void NavigateToStep(int stepNumber)
        {
            UpdateStepVisuals(stepNumber);

            switch (stepNumber)
            {
                case 1:
                    ContentFrame.Navigate(new Pages.Preface());
                    break;
                case 2:
                    ContentFrame.Navigate(new Pages.Notice());
                    break;
                case 3:
                    ContentFrame.Navigate(new Pages.CollectInformation());
                    break;
                case 4:
                    ContentFrame.Navigate(new Pages.SpeedTest());
                    break;
                case 5:
                    ContentFrame.Navigate(new Pages.Answer());
                    break;
            }

            currentStep = stepNumber;
        }

        private void UpdateStepVisuals(int activeStep)
        {
            ResetAllSteps();
            SetStepActive(activeStep, true);

            for (int i = 1; i < activeStep; i++)
            {
                SetStepCompleted(i);
            }
        }

        private void ResetAllSteps()
        {
            for (int i = 1; i <= 5; i++)
            {
                SetStepPending(i);
            }
        }

        private void SetStepActive(int step, bool isActive)
        {
            var circle = FindName($"Step{step}Circle") as Ellipse;
            var text = FindName($"Step{step}Text") as TextBlock;
            var check = FindName($"Step{step}Check") as TextBlock;

            if (circle != null && text != null && check != null)
            {
                if (isActive)
                {
                    circle.Stroke = (SolidColorBrush)FindResource("CurrentStepColor");
                    circle.Fill = (SolidColorBrush)FindResource("CurrentStepColor");
                    text.Foreground = (SolidColorBrush)FindResource("CurrentStepColor");
                    text.FontWeight = FontWeights.Bold;
                    check.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void SetStepCompleted(int step)
        {
            var circle = FindName($"Step{step}Circle") as Ellipse;
            var text = FindName($"Step{step}Text") as TextBlock;
            var check = FindName($"Step{step}Check") as TextBlock;

            if (circle != null && text != null && check != null)
            {
                circle.Stroke = (SolidColorBrush)FindResource("CompletedStepColor");
                circle.Fill = (SolidColorBrush)FindResource("CompletedStepColor");
                text.Foreground = (SolidColorBrush)FindResource("CompletedStepColor");
                check.Visibility = Visibility.Visible;
            }
        }

        private void SetStepPending(int step)
        {
            var circle = FindName($"Step{step}Circle") as Ellipse;
            var text = FindName($"Step{step}Text") as TextBlock;
            var check = FindName($"Step{step}Check") as TextBlock;

            if (circle != null && text != null && check != null)
            {
                circle.Stroke = (SolidColorBrush)FindResource("PendingStepColor");
                circle.Fill = Brushes.Transparent;
                text.Foreground = (SolidColorBrush)FindResource("PendingStepColor");
                text.FontWeight = FontWeights.SemiBold;
                check.Visibility = Visibility.Collapsed;
            }
        }

        public void GoToNextStep()
        {
            if (currentStep < 5)
            {
                NavigateToStep(currentStep + 1);
            }
        }
    }
}