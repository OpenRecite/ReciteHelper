using ReciteHelper.Core.DTOs;
using ReciteHelper.Core.Enums;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace ReciteHelper.Wpf.Views;

public partial class ProgressWindow : Window, INotifyPropertyChanged
{
    private static readonly Brush ActiveBrush = new SolidColorBrush(Color.FromRgb(0, 122, 204));
    private static readonly Brush InactiveBrush = new SolidColorBrush(Color.FromRgb(214, 222, 232));
    private static readonly Brush ActiveTextBrush = Brushes.White;
    private static readonly Brush InactiveTextBrush = new SolidColorBrush(Color.FromRgb(100, 116, 139));

    private ProjectCreationStage _currentStage = ProjectCreationStage.ReadingText;
    private bool _isClosed;

    public ProgressWindow()
    {
        InitializeComponent();
        DataContext = this;
        Closed += (_, _) => _isClosed = true;
        ApplyStage(ProjectCreationStage.ReadingText, "正在读取题库文件，随后将进入知识提取。");
        SetProgress(0, 1, true, "读取进度");
    }

    public Brush Step1Brush { get => field; private set => SetField(ref field, value); } = ActiveBrush;
    public Brush Step2Brush { get => field; private set => SetField(ref field, value); } = ActiveBrush;
    public Brush Step3Brush { get => field; private set => SetField(ref field, value); } = InactiveBrush;
    public Brush Step4Brush { get => field; private set => SetField(ref field, value); } = InactiveBrush;

    public Brush Step1TextBrush { get => field; private set => SetField(ref field, value); } = ActiveTextBrush;
    public Brush Step2TextBrush { get => field; private set => SetField(ref field, value); } = ActiveTextBrush;
    public Brush Step3TextBrush { get => field; private set => SetField(ref field, value); } = InactiveTextBrush;
    public Brush Step4TextBrush { get => field; private set => SetField(ref field, value); } = InactiveTextBrush;

    public Brush Line1Brush { get => field; private set => SetField(ref field, value); } = ActiveBrush;
    public Brush Line2Brush { get => field; private set => SetField(ref field, value); } = InactiveBrush;
    public Brush Line3Brush { get => field; private set => SetField(ref field, value); } = InactiveBrush;

    public string CurrentTitle { get => field; private set => SetField(ref field, value); } = string.Empty;
    public string CurrentDescription { get => field; private set => SetField(ref field, value); } = string.Empty;
    public string DetailLabel { get => field; private set => SetField(ref field, value); } = string.Empty;
    public string ProgressText { get => field; private set => SetField(ref field, value); } = string.Empty;

    public int ProgressCurrent { get => field; private set => SetField(ref field, value); }
    public int ProgressTotal { get => field; private set => SetField(ref field, value); } = 1;
    public bool IsProgressIndeterminate { get => field; private set => SetField(ref field, value); } = true;

    public void ApplyProgress(ProjectCreationProgress progress)
    {
        if (_isClosed)
            return;

        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(() => ApplyProgress(progress));
            return;
        }

        var stage = progress.Stage < _currentStage ? _currentStage : progress.Stage;
        ApplyStage(stage, progress.Label);

        var (current, total, label) = stage switch
        {
            ProjectCreationStage.ReadingText => (progress.RoundCurrent, progress.RoundTotal, "读取进度"),
            ProjectCreationStage.KnowledgeExtraction => (progress.ScanCurrent, progress.ScanTotal, "知识提取进度"),
            ProjectCreationStage.TextClustering => (progress.ClusterCurrent, progress.ClusterTotal, "文本聚类进度"),
            ProjectCreationStage.VectorGeneration => (progress.ClusterCurrent, progress.ClusterTotal, "向量生成进度"),
            ProjectCreationStage.Completed => (1, 1, "完成状态"),
            _ => (0, 1, "当前进度")
        };

        SetProgress(current, total, total <= 0, label);
    }

    private void ApplyStage(ProjectCreationStage stage, string? label)
    {
        _currentStage = stage;

        var visualStage = stage < ProjectCreationStage.KnowledgeExtraction
            ? ProjectCreationStage.KnowledgeExtraction
            : stage;

        Step1Brush = GetStepBrush(visualStage, ProjectCreationStage.ReadingText);
        Step2Brush = GetStepBrush(visualStage, ProjectCreationStage.KnowledgeExtraction);
        Step3Brush = GetStepBrush(visualStage, ProjectCreationStage.TextClustering);
        Step4Brush = GetStepBrush(visualStage, ProjectCreationStage.VectorGeneration);

        Step1TextBrush = GetStepTextBrush(visualStage, ProjectCreationStage.ReadingText);
        Step2TextBrush = GetStepTextBrush(visualStage, ProjectCreationStage.KnowledgeExtraction);
        Step3TextBrush = GetStepTextBrush(visualStage, ProjectCreationStage.TextClustering);
        Step4TextBrush = GetStepTextBrush(visualStage, ProjectCreationStage.VectorGeneration);

        Line1Brush = visualStage >= ProjectCreationStage.KnowledgeExtraction ? ActiveBrush : InactiveBrush;
        Line2Brush = visualStage >= ProjectCreationStage.TextClustering ? ActiveBrush : InactiveBrush;
        Line3Brush = visualStage >= ProjectCreationStage.VectorGeneration ? ActiveBrush : InactiveBrush;

        (CurrentTitle, CurrentDescription) = GetStageText(stage, label);
    }

    private static Brush GetStepBrush(ProjectCreationStage current, ProjectCreationStage step)
    {
        return current >= step ? ActiveBrush : InactiveBrush;
    }

    private static Brush GetStepTextBrush(ProjectCreationStage current, ProjectCreationStage step)
    {
        return current >= step ? ActiveTextBrush : InactiveTextBrush;
    }

    private static (string Title, string Description) GetStageText(ProjectCreationStage stage, string? label)
    {
        return stage switch
        {
            ProjectCreationStage.ReadingText => (
                "正在读取你的资料",
                label ?? "正在复制并读取题库文件，准备进入知识提取流程。"),
            ProjectCreationStage.KnowledgeExtraction => (
                "你的资料正在被提取知识点",
                label ?? "你的资料已经被切割完成，正在分块发送至 AI 生成知识点和相关题目。"),
            ProjectCreationStage.TextClustering => (
                "正在整理章节结构",
                label ?? "AI 已经生成初步题目，正在合并相似章节并整理题目结构。"),
            ProjectCreationStage.VectorGeneration => (
                "正在生成知识库向量",
                label ?? "正在将知识点转换为本地向量索引，后续可用于知识库检索。"),
            ProjectCreationStage.Completed => (
                "项目创建完成",
                label ?? "题目、章节和本地知识库已经处理完成，正在保存项目文件。"),
            _ => ("正在创建项目", label ?? "项目正在处理中。")
        };
    }

    private void SetProgress(int current, int total, bool isIndeterminate, string label)
    {
        DetailLabel = label;
        IsProgressIndeterminate = isIndeterminate;
        ProgressTotal = Math.Max(total, 1);
        ProgressCurrent = Math.Clamp(current, 0, ProgressTotal);
        ProgressText = isIndeterminate ? "处理中" : $"{ProgressCurrent}/{ProgressTotal}";
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
