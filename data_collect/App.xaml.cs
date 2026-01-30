using System.Windows;
using ReciteHelper.DataCollect.Model;

namespace ReciteHelper.DataCollect
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ProjectManager.Load();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            ProjectManager.Save();
            base.OnExit(e);
        }
    }
}