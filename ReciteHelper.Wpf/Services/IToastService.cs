namespace ReciteHelper.Wpf.Services;

public interface IToastService
{
    void ShowInfo(string message, int duration = 3000);
    void ShowWarning(string message, int duration = 3000);
    void ShowError(string message, int duration = 3000);
    void Show(string message, ToastType type, int duration = 3000);
}

public enum ToastType
{
    Info,
    Warning,
    Error
}