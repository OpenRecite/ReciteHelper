using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReciteHelper.Application.Interfaces.Services;
using ReciteHelper.Application.Services;
using ReciteHelper.Core.Interfaces.Services;
using ReciteHelper.Infrastructure.Configuration;
using ReciteHelper.Infrastructure.Services;
using System.Windows;

namespace ReciteHelper.Wpf;

public partial class App : System.Windows.Application
{
    private IServiceProvider? _serviceProvider;

    private async void Application_Startup(object sender, StartupEventArgs e)
    {
        var services = new ServiceCollection();

        var configService = new ConfigService();
        var appConfig = await configService.LoadAsync();

        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        services.AddSingleton<IConfigService>(configService);
        services.AddSingleton(appConfig);

        services.AddSingleton<IAnswerJudge, SbertModelJudge>();
        services.AddSingleton<IQuizService, QuizService>();
        services.AddSingleton<IPhonkService, PhonkService>();
        services.AddSingleton<ISuperMemoService, SuperMemoService>(); 

        services.AddSingleton<MainWindow>();

        _serviceProvider = services.BuildServiceProvider();

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }
}
