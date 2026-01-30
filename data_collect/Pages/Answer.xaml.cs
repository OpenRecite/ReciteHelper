using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using ReciteHelper.DataCollect.Model;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ReciteHelper.DataCollect.Pages
{
    public partial class Answer : Page
    {
        private int _currentQuestionIndex = 0;
        private int _selectedScore = -1;
        private DateTime _answerStartTime;
        private double _typingSpeed;
        private double _similarity;

        public Answer()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadUserInfo();
            LoadCurrentQuestion();
        }

        private void LoadUserInfo()
        {
            var project = ProjectManager.CurrentProject;
            if (project.UserBasic != null && !string.IsNullOrEmpty(project.UserBasic.Name))
            {
                UserNameText.Text = project.UserBasic.Name;
            }
            else
            {
                UserNameText.Text = "匿名用户";
            }
        }

        private void LoadCurrentQuestion()
        {
            var project = ProjectManager.CurrentProject;

            if (project.Questions.Count == 0)
            {
                ShowCompletionMessage();
                return;
            }

            if (_currentQuestionIndex >= project.Questions.Count)
            {
                ShowCompletionMessage();
                return;
            }

            var question = project.Questions[_currentQuestionIndex];
            QuestionText.Text = question.Content;

            ProgressText.Text = $"进度：{_currentQuestionIndex + 1}/{project.Questions.Count}";

            AnswerTextBox.Text = "";
            AnswerTextBox.IsEnabled = true;
            SubmitAnswerButton.IsEnabled = true;
            ReviewPanel.Visibility = Visibility.Collapsed;
            CompletionPanel.Visibility = Visibility.Collapsed;

            _answerStartTime = DateTime.Now;
        }

        private void SubmitAnswerButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(AnswerTextBox.Text))
            {
                MessageBox.Show("请输入您的答案", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var timeSpent = DateTime.Now - _answerStartTime;
            var charCount = AnswerTextBox.Text.Length;
            _typingSpeed = (timeSpent.TotalSeconds > 0) ? (charCount / timeSpent.TotalSeconds * 60) : 0;

            var project = ProjectManager.CurrentProject;
            var question = project.Questions[_currentQuestionIndex];
            CorrectAnswerText.Text = question.Answer;

            AnswerTextBox.IsEnabled = false;
            SubmitAnswerButton.IsEnabled = false;

            ReviewPanel.Visibility = Visibility.Visible;

            ResetScoreButtons();
            _selectedScore = -1;
            SubmitScoreButton.IsEnabled = false;
            ScoreDescriptionText.Text = "请点击上方数字选择评分（0-5）";
        }

        private void ScoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string scoreStr)
            {
                int score = int.Parse(scoreStr);
                _selectedScore = score;

                ResetScoreButtons();
                button.Background = new SolidColorBrush(Color.FromRgb(0x4A, 0x6F, 0xA5));
                button.Foreground = Brushes.White;

                string[] descriptions = {
                    "0分：完全不会",
                    "1分：完全遗忘",
                    "2分：错误回忆（但看到答案后能理解）",
                    "3分：困难但最终正确回忆",
                    "4分：较难但正确回忆",
                    "5分：完美回忆"
                };

                if (score >= 0 && score <= 5)
                {
                    ScoreDescriptionText.Text = descriptions[score];
                }

                SubmitScoreButton.IsEnabled = true;
            }
        }

        private void ResetScoreButtons()
        {
            Score0Button.Background = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0));
            Score1Button.Background = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0));
            Score2Button.Background = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0));
            Score3Button.Background = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0));
            Score4Button.Background = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0));
            Score5Button.Background = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0));

            Score0Button.Foreground = Brushes.Black;
            Score1Button.Foreground = Brushes.Black;
            Score2Button.Foreground = Brushes.Black;
            Score3Button.Foreground = Brushes.Black;
            Score4Button.Foreground = Brushes.Black;
            Score5Button.Foreground = Brushes.Black;
        }

        internal static int PredictQValue<TValue>(TValue relRelative, TValue similarity)
    where TValue : struct, INumber<TValue>
        {

            // Load model
            string modelPath = "xgboost_predQ.onnx";
            using var session = new InferenceSession(modelPath);

            // The model is expected to have an accuracy of approximately
            // 70% ​​and is currently undergoing further training
            float[] inputData = [
                float.CreateChecked(relRelative),
            float.CreateChecked(similarity) * 100f
            ];
            int[] dimensions = [1, 2];
            var inputTensor = new DenseTensor<float>(inputData, dimensions);

            var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("float_input", inputTensor)
        };
            using var results = session.Run(inputs);
            var label = results.First(r => r.Name == "label").AsEnumerable<long>().First();
            var probs = results.First(r => r.Name == "probabilities").AsEnumerable<float>().ToArray();

            float maxProb = 0;
            int maxIndex = 0;
            for (int i = 0; i < probs.Length; i++)
            {
                if (probs[i] > maxProb)
                    maxIndex = i;
                maxProb = Math.Max(maxProb, probs[i]);
            }

            // Predict result
            return maxIndex;
        }

        private void SubmitScoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedScore < 0) return;

            var project = ProjectManager.CurrentProject;
            var question = project.Questions[_currentQuestionIndex];

            _similarity = CalculateSimilarity(AnswerTextBox.Text.Trim(), question.Answer.Trim());

            var record = new AnswerRecord
            {
                DateTime = DateTime.Now,
                Speed = (int)_typingSpeed,
                Similarity = (int)(_similarity * 100),
                QMark = _selectedScore,
                QPredict = PredictQValue(_typingSpeed / ProjectManager.CurrentProject.Speed, _similarity)
            };

            question.AnswerRecords.Add(record);
            question.UserAnswer = AnswerTextBox.Text.Trim();

            UpdateEFValue(question, _selectedScore);

            ProjectManager.Save();

            _currentQuestionIndex++;

            if (_currentQuestionIndex >= project.Questions.Count)
            {
                ShowCompletionMessage();
            }
            else
            {
                LoadCurrentQuestion();
            }
        }

        private double CalculateSimilarity(string userAnswer, string correctAnswer)
        {
            if (string.IsNullOrEmpty(userAnswer) && string.IsNullOrEmpty(correctAnswer))
                return 1.0;

            if (string.IsNullOrEmpty(userAnswer) || string.IsNullOrEmpty(correctAnswer))
                return 0.0;

            int maxLength = Math.Max(userAnswer.Length, correctAnswer.Length);
            if (maxLength == 0) return 1.0;

            double distance = ComputeLevenshteinDistance(userAnswer, correctAnswer);
            return 1.0 - (distance / maxLength);
        }

        private int ComputeLevenshteinDistance(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            if (n == 0) return m;
            if (m == 0) return n;

            for (int i = 0; i <= n; d[i, 0] = i++) { }
            for (int j = 0; j <= m; d[0, j] = j++) { }

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            return d[n, m];
        }

        private void UpdateEFValue(Question question, int qScore)
        {
            if (question.AnswerRecords.Count < 2)
                return;

            double oldEF = question.EFScore;
            double newEF;

            if (qScore >= 3)
            {
                double factor = 0.1 - (5 - qScore) * (0.08 + (5 - qScore) * 0.02);
                newEF = oldEF + factor;
                newEF = Math.Max(1.3, newEF);
            }
            else
            {
                newEF = oldEF - 0.2;
                newEF = Math.Max(1.3, newEF);
            }

            question.EFScore = Math.Round(newEF, 2);
        }

        private void ShowCompletionMessage()
        {
            QuestionText.Visibility = Visibility.Collapsed;
            AnswerTextBox.Visibility = Visibility.Collapsed;
            SubmitAnswerButton.Visibility = Visibility.Collapsed;
            ReviewPanel.Visibility = Visibility.Collapsed;
            CompletionPanel.Visibility = Visibility.Visible;
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            string helpText = "评分标准说明：\n\n" +
                            "5分：完美回忆 - 瞬间想起，完全确定\n" +
                            "4分：较难但正确回忆 - 稍作思考想起，比较确定\n" +
                            "3分：困难但最终正确回忆 - 努力回忆后想起，有些犹豫\n" +
                            "2分：错误回忆（但看到答案后能理解）\n" +
                            "1分：完全遗忘\n" +
                            "0分：完全不会";

            MessageBox.Show(helpText, "评分标准", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void FinishButton_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.Close();
            }
        }
    }
}