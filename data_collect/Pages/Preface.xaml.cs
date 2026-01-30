using ReciteHelper.DataCollect.Model;
using System.Windows;
using System.Windows.Controls;

namespace ReciteHelper.DataCollect.Pages
{
    /// <summary>
    /// Interaction logic for Preface.xaml
    /// </summary>
    public partial class Preface : Page
    {
        public Preface()
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
