using ReciteHelper.DataCollect.Model;
using System.Windows;
using System.Windows.Controls;

namespace ReciteHelper.DataCollect.Pages
{
    public partial class Notice : Page
    {
        public Notice()
        {
            InitializeComponent();
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.GoToNextStep();
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (ProjectManager.CurrentProject.UserBasic is not null)
                if (Window.GetWindow(this) is MainWindow mainWindow)
                    mainWindow.GoToNextStep();
        }
    }
}