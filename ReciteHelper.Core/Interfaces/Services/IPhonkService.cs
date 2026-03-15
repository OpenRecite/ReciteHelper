using ReciteHelper.Core.EventArgs;

namespace ReciteHelper.Core.Interfaces.Services;

public interface IPhonkService
{
    bool IsEnabled { get; }

    Task PlayRandomPhonkAsync();

    event EventHandler<PhonkEventArgs>? PhonkTriggered;
}
