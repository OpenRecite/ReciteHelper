using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReciteHelper.Core.Interfaces.Configuration;
using ReciteHelper.Core.Interfaces.Services;
using ReciteHelper.Application.Services;
using ReciteHelper.Infrastructure.Configuration;
using ReciteHelper.Infrastructure.Services;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace ReciteHelper.Wpf;

public partial class App : System.Windows.Application
{
    private IServiceProvider? _serviceProvider;

    private async void Application_Startup(object sender, StartupEventArgs e)
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

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
        services.AddSingleton<IKnowledgeBaseService, KnowledgeBaseService>();
        services.AddSingleton<IAiChatService, AiChatService>();
        services.AddSingleton<IQuestionHelpService, QuestionHelpService>();
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

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        ReportUnhandledException(e.Exception);
        e.Handled = true;
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
            ReportUnhandledException(exception);
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        ReportUnhandledException(e.Exception);
        e.SetObserved();
    }

    private static void ReportUnhandledException(Exception exception)
    {
        try
        {
            var logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ReciteHelper");
            Directory.CreateDirectory(logDirectory);
            File.AppendAllText(
                Path.Combine(logDirectory, "crash.log"),
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {exception}{Environment.NewLine}{Environment.NewLine}");
        }
        catch
        {
            // Last-resort error reporting must never throw another exception.
        }

        try
        {
            MessageBox.Show(
                $"程序遇到未处理异常：{exception.Message}",
                "未处理异常",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch
        {
            // The dispatcher may already be shutting down.
        }
    }
}
