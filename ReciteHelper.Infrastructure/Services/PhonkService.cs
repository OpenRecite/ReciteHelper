using ReciteHelper.Core.EventArgs;
using ReciteHelper.Core.Interfaces.Services;
using ReciteHelper.Application.Interfaces.Configuration;

namespace ReciteHelper.Infrastructure.Services;

public class PhonkService : IPhonkService
{
    private readonly IConfigService _configService;
    private readonly Random _random = new();
    private readonly string _soundDirectory;
    private readonly string _imageBaseUri;

    public event EventHandler<PhonkEventArgs>? PhonkTriggered;

    public bool IsEnabled { get; private set; }

    public PhonkService(IConfigService configService)
    {
        _configService = configService;

        var config = _configService.LoadAsync().Result;
        IsEnabled = config.PhonkOptions?.EnablePhonk ?? false;

        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        _soundDirectory = Path.Combine(baseDirectory, "Images", "Phonk", "Soundfx");
        _imageBaseUri = "pack://application:,,,/ReciteHelper;component/Images/Phonk/Caveira/";
    }

    public async Task PlayRandomPhonkAsync()
    {
        if (!IsEnabled) return;

        var number = _random.Next(1, 10);
        var imageUri = $"{_imageBaseUri}caveira{number}.png";
        var soundFile = Path.Combine(_soundDirectory, $"phonk{number}.mp3");

        PhonkTriggered?.Invoke(this, new PhonkEventArgs(imageUri, soundFile));
        await Task.CompletedTask;
    }
}
