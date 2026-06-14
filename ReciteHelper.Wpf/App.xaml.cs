using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReciteHelper.Application.Interfaces.Configuration;
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
        Models.Config.Use(appConfig);

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
        services.AddSingleton<IReviewGenerator, ReviewGenerator>();
        services.AddSingleton<IExamPaperService, ExamPaperService>();
        services.AddSingleton<IExamSettingsService, ExamSettingsService>();
        services.AddSingleton<IExamAnswerService, ExamAnswerService>();
        services.AddSingleton<IProjectFileService, ProjectFileService>();
        services.AddSingleton<IProjectCreationService, ProjectCreationService>();
        services.AddSingleton<IQuestionBankTextService, QuestionBankTextService>();
        services.AddSingleton<IAiChatService, AiChatService>();
        services.AddSingleton<IStartupCompatibilityService, StartupCompatibilityService>();
        services.AddSingleton<IRecentProjectService, RecentProjectService>();
        services.AddSingleton<IFileMergeService, FileMergeService>();
        services.AddSingleton<IGalGameService, GalGameService>();
        services.AddSingleton<IGalGameCreationService, GalGameCreationService>();
        services.AddSingleton<IPromptProvider, PromptProvider>();
        services.AddSingleton<IPhonkService, PhonkService>();
        services.AddSingleton<ISuperMemoService, SuperMemoService>(); 

        services.AddSingleton<MainWindow>();

        _serviceProvider = services.BuildServiceProvider();

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }
}
