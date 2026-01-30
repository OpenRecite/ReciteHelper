using ReciteHelper.DataCollect.Model;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ReciteHelper.DataCollect.Pages
{
    public partial class SpeedTest : Page
    {
        private const string TEST_ARTICLE =
@"在数字时代，信息爆炸性增长对个人记忆力提出了前所未有的挑战。艾宾浩斯遗忘曲线揭示了记忆随时间衰退的规律：学习后一小时，记忆量可能下降至约44%。间隔重复算法（Spaced Repetition）正是为了对抗这种自然遗忘而设计的。

SuperMemo-2是一种经典的间隔重复算法，它通过调整复习间隔来优化长期记忆效果。该算法使用易度因子和复习间隔等参数，根据用户每次复习的评分动态调整下次复习时间。研究表明，合理应用此类算法可将长期记忆保持率提升至80%以上。

认知心理学将记忆分为感觉记忆、短时记忆和长时记忆。工作记忆Working Memory作为短时记忆的一种，容量有限，通常只能保持7±2个信息单元。因此，将信息组织成有意义的组块（Chunking）是提高记忆效率的关键策略。

在技术领域，术语如API（应用程序编程接口）、JSON（JavaScript对象表示法）、SQL（结构化查询语言）等频繁出现。同时，自然常数e=2.71828在科学计算中广泛应用。温度单位如25℃、100℃，以及百分比表示如75.5%、增长率8.3%等都是常见表达。

标点符号的正确使用也很重要：逗号、句号、分号；引号“”和括号（）等。数字与中文混排时要注意，如第3代移动通信技术3G、第5代5G，以及IPv4和IPv6地址格式差异等。

最后，专业领域的名词如机器学习、神经网络、大数据分析、云计算等日益普及。保持持续学习的态度，合理运用记忆方法，能让我们在信息洪流中高效获取和保持知识。";

        private DispatcherTimer _timer;
        private Stopwatch _stopwatch;
        private bool _isTesting = false;
        private int _totalChars;

        public SpeedTest()
        {
            InitializeComponent();
            _stopwatch = new Stopwatch();

            TestArticleText.Text = TEST_ARTICLE;
            _totalChars = TEST_ARTICLE.Length;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(100);
            _timer.Tick += Timer_Tick;

            if (ProjectManager.CurrentProject.Speed > 1.145d)
                if (Window.GetWindow(this) is MainWindow mainWindow)
                    mainWindow.GoToNextStep();
        }

        private void StartStopButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isTesting)
                StartTest();
            else
                EndTest();

        }

        private void StartTest()
        {
            _isTesting = true;
            _stopwatch.Restart();

            TypingTextBox.Clear();
            TypingTextBox.IsEnabled = true;
            TypingTextBox.Focus();

            StartStopButton.Content = "结束测试";
            StartStopButton.Background = (System.Windows.Media.Brush)FindResource("AccentColor");

            _timer.Start();

            CompletionMessage.Visibility = Visibility.Collapsed;
        }

        private void EndTest()
        {
            _isTesting = false;
            _stopwatch.Stop();
            _timer.Stop();

            CalculateResults();

            TypingTextBox.IsEnabled = false;
            StartStopButton.Content = "重新开始";
            StartStopButton.Background = (System.Windows.Media.Brush)FindResource("PrimaryColor");

            CompletionMessage.Text = "测试完成！您的基准打字速度已记录。点击下一步继续。";
            CompletionMessage.Visibility = Visibility.Visible;

            SaveTypingData();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            TimeText.Text = _stopwatch.Elapsed.ToString(@"mm\:ss");

            if (_stopwatch.Elapsed.TotalSeconds > 0)
            {
                int typedChars = TypingTextBox.Text.Length;
                double seconds = _stopwatch.Elapsed.TotalSeconds;
                double cpm = (typedChars / seconds) * 60;

                SpeedText.Text = $"{cpm:F0} 字符/分";
                CharCountText.Text = typedChars.ToString();

                CalculateAccuracy();
            }
        }

        private void CalculateAccuracy()
        {
            string typed = TypingTextBox.Text;
            string target = TEST_ARTICLE;

            int minLength = Math.Min(typed.Length, target.Length);
            int correctChars = 0;

            for (int i = 0; i < minLength; i++)
            {
                if (typed[i] == target[i])
                {
                    correctChars++;
                }
            }

            double accuracy = (minLength > 0) ? (correctChars * 100.0 / minLength) : 100;
            AccuracyText.Text = $"{accuracy:F1}%";
        }

        private void CalculateResults()
        {
            double totalSeconds = _stopwatch.Elapsed.TotalSeconds;
            int typedChars = TypingTextBox.Text.Length;

            double cpm = (totalSeconds > 0) ? (typedChars / totalSeconds * 60) : 0;

            string typed = TypingTextBox.Text;
            string target = TEST_ARTICLE.Substring(0, Math.Min(typed.Length, _totalChars));

            int correct = 0;
            for (int i = 0; i < typed.Length && i < target.Length; i++)
            {
                if (typed[i] == target[i]) correct++;
            }

            double finalAccuracy = (typed.Length > 0) ? (correct * 100.0 / typed.Length) : 0;

            SpeedText.Text = $"{cpm:F0} 字符/分";
            AccuracyText.Text = $"{finalAccuracy:F1}%";
        }

        private void SaveTypingData()
        {
            double totalSeconds = _stopwatch.Elapsed.TotalSeconds;
            int typedChars = TypingTextBox.Text.Length;
            double cpm = (totalSeconds > 0) ? (typedChars / totalSeconds * 60) : 0;

            ProjectManager.CurrentProject.Speed = cpm;

            if (Window.GetWindow(this) is MainWindow mainWindow)
                mainWindow.GoToNextStep();

            Console.WriteLine($"打字速度记录: {cpm:F0} CPM, 准确率: {AccuracyText.Text}, 用时: {TimeText.Text}");
        }

        private void TypingTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isTesting)
            {
                if (TypingTextBox.Text.Length >= TEST_ARTICLE.Length)
                {
                    EndTest();
                }
            }
        }

        private void TypingTextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (!_isTesting && e.Key != System.Windows.Input.Key.Tab)
            {
                e.Handled = true;
            }
        }
    }
}