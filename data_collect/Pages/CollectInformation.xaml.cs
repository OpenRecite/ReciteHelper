using ReciteHelper.DataCollect.Model;
using System.Windows;
using System.Windows.Controls;

namespace ReciteHelper.DataCollect.Pages
{
    public partial class CollectInformation : Page
    {
        public UserBasic UserData { get; private set; } = new UserBasic();

        public CollectInformation()
        {
            InitializeComponent();
            InitializeDisciplines();
        }

        private void InitializeDisciplines()
        {
            string[] disciplines = new string[]
            {
                "哲学", "经济学", "法学", "教育学", "文学", "历史学",
                "理学", "工学", "农学", "医学", "军事学", "管理学",
                "艺术学", "交叉学科"
            };

            foreach (var discipline in disciplines)
            {
                DisciplineComboBox.Items.Add(discipline);
            }

            if (DisciplineComboBox.Items.Count > 0)
            {
                DisciplineComboBox.SelectedIndex = 0;
            }
        }

        private void Field_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateFields();
        }

        private void Field_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ValidateFields();
        }

        private void ValidateFields()
        {
            bool isValid = true;

            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(EmailTextBox.Text) ||
                !IsValidEmail(EmailTextBox.Text))
            {
                isValid = false;
            }

            if (DisciplineComboBox.SelectedItem == null)
            {
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(MajorTextBox.Text))
            {
                isValid = false;
            }

            NextButton.IsEnabled = isValid;
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.NavigateToStep(2);
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            SaveUserData();

            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.GoToNextStep();
            }
        }

        private void SaveUserData()
        {
            UserData.Name = NameTextBox.Text.Trim();
            UserData.Email = EmailTextBox.Text.Trim();
            UserData.AcademicDiscipline = DisciplineComboBox.SelectedItem?.ToString() ?? "";
            UserData.Major = MajorTextBox.Text.Trim();
            UserData.Workplace = WorkplaceTextBox.Text.Trim();

            ProjectManager.CurrentProject.UserBasic = UserData;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (ProjectManager.CurrentProject.UserBasic is not null)
                if (Window.GetWindow(this) is MainWindow mainWindow)
                    mainWindow.GoToNextStep();
        }
    }
}