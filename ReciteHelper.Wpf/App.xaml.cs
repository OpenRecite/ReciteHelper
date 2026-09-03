using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReciteHelper.Core.Interfaces.Configuration;
using ReciteHelper.Core.Interfaces.Services;
using ReciteHelper.Application.Services;
using ReciteHelper.Infrastructure.Configuration;
using ReciteHelper.Infrastructure.Services;
using ReciteHelper.Wpf.Views;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace ReciteHelper.Wpf;

public partial class App : System.Windows.Application
{
    // This should normally stay false. Set it to true only when building a
    // customer-specific edition that offers hosted activation as an alternative
    // to user-provided DeepSeek/Qwen API keys.
    private static readonly bool EnableHostedActivation = false;

    private IServiceProvider? _serviceProvider;

    private async void Application_Startup(object sender, StartupEventArgs e)
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        var services = new ServiceCollection();

        var configService = new ConfigService();
        ReciteHelper.Core.Configuration.ConfigOptions appConfig;
        try
        {
            appConfig = await configService.LoadAsync();
        }
        catch
        {
            appConfig = new ReciteHelper.Core.Configuration.ConfigOptions();
            MessageBox.Show(
                BuildStartupConfigurationMessage(),
                "需要完成配置",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
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
        services.AddSingleton<IExamSourceTextReader, ExamSourceTextReader>();
        services.AddSingleton<IExamSetRepository, JsonExamSetRepository>();
        services.AddSingleton<IExamSetImportService, ExamSetImportService>();
        services.AddSingleton<IProjectFileService, ProjectFileService>();
        services.AddSingleton<IProjectCreationService, ProjectCreationService>();
        services.AddSingleton<IQuestionBankTextService, QuestionBankTextService>();
        services.AddSingleton<HostedModelService>();
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
        services.AddSingleton<IReviewScheduler, FsrsReviewScheduler>();
        services.AddSingleton<IReviewPersonalizationService, ReviewPersonalizationService>();

        services.AddSingleton<ActivationWindow>();
        services.AddSingleton<MainWindow>();

        _serviceProvider = services.BuildServiceProvider();

        var hostedModelService = _serviceProvider.GetRequiredService<HostedModelService>();
        if (!await EnsureModelAccessAsync(appConfig, hostedModelService))
        {
            Shutdown();
            return;
        }

        Models.Config.Use(await configService.LoadAsync());

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private async Task<bool> EnsureModelAccessAsync(
        ReciteHelper.Core.Configuration.ConfigOptions config,
        HostedModelService hostedModelService)
    {
        if (HasLocalModelKeys(config))
            return true;

        if (!EnableHostedActivation)
        {
            MessageBox.Show(
                "未检测到可用的 DeepSeekKey 和 QwenKey。\n\n请打开 Config.xml，填写 DeepSeekKey 与 QwenKey，保存后重新启动软件。",
                "需要填写 API Key",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return false;
        }

        if (!string.IsNullOrWhiteSpace(config.HostedLicenseId))
        {
            var validation = await hostedModelService.ValidateAsync();
            if (validation.IsValid)
                return true;
        }

        var activationWindow = _serviceProvider!.GetRequiredService<ActivationWindow>();
        return activationWindow.ShowDialog() == true;
    }

    private static bool HasLocalModelKeys(ReciteHelper.Core.Configuration.ConfigOptions config)
    {
        return !string.IsNullOrWhiteSpace(config.DeepSeekKey) &&
               !string.IsNullOrWhiteSpace(config.QwenKey);
    }

    private static string BuildStartupConfigurationMessage()
    {
        return EnableHostedActivation
            ? "配置文件无法读取。软件需要 DeepSeek/Qwen API Key，或一站式服务激活码才能使用。"
            : "配置文件无法读取。软件需要填写 DeepSeekKey 和 QwenKey 后才能使用。";
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
