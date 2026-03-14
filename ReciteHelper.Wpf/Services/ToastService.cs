using ReciteHelper.Wpf.Controls;
using System.Windows.Threading;

namespace ReciteHelper.Wpf.Services;

public class ToastService : IToastService
{
    private readonly ToastControl _toastControl;
    private readonly Dispatcher _dispatcher;

    public ToastService(ToastControl toastControl)
    {
        _toastControl = toastControl;
        _dispatcher = _toastControl.Dispatcher;
    }

    public void ShowInfo(string message, int duration = 3000)
    {
        Show(message, ToastType.Info, duration);
    }

    public void ShowWarning(string message, int duration = 3000)
    {
        Show(message, ToastType.Warning, duration);
    }

    public void ShowError(string message, int duration = 3000)
    {
        Show(message, ToastType.Error, duration);
    }

    public void Show(string message, ToastType type, int duration = 3000)
    {
        _dispatcher.Invoke(() =>
        {
            _toastControl.Type = type;
            _toastControl.Message = message;

            // Auto hiding
            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(duration)
            };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                _toastControl.Hide();
            };
            timer.Start();
        });
    }
}