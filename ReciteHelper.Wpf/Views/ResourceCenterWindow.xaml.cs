using Microsoft.Win32;
using ReciteHelper.Core.Interfaces.Services;
using ReciteHelper.Wpf.Models;
using ReciteHelper.Wpf.Services;
using System.IO;
using System.Windows;

namespace ReciteHelper.Wpf.Views;

public partial class ResourceCenterWindow : Window
{
    private const int PageSize = 12;
    private readonly ResourceCenterClient _client = new();
    private readonly IProjectFileService _projectFileService;
    private readonly Func<string, string?, Task> _addRecentProjectAsync;
    private readonly string _serverUrl;
    private int _currentPage = 1;
    private int _totalPages = 1;
    private string? _selectedUploadFilePath;
    private CancellationTokenSource? _loadCancellation;

    public ResourceCenterWindow(
        string serverUrl,
        IProjectFileService projectFileService,
        Func<string, string?, Task> addRecentProjectAsync)
    {
        _serverUrl = serverUrl;
        _projectFileService = projectFileService;
        _addRecentProjectAsync = addRecentProjectAsync;
        InitializeComponent();
        ServerAddressText.Text = $"服务器：{serverUrl}";
        Loaded += async (_, _) => await LoadResourcesAsync(1);
    }

    private async Task LoadResourcesAsync(int page)
    {
        _loadCancellation?.Cancel();
        _loadCancellation = new CancellationTokenSource();
        var cancellationToken = _loadCancellation.Token;

        try
        {
            SetBusy("正在连接资源中心...");
            var result = await _client.SearchAsync(
                _serverUrl,
                page,
                PageSize,
                UploaderSearchTextBox.Text,
                SchoolSearchTextBox.Text,
                SubjectSearchTextBox.Text,
                cancellationToken);

            _currentPage = result.Page;
            _totalPages = Math.Max(1, result.TotalPages);
            ResourceItemsControl.ItemsSource = result.Items;
            ResultSummaryText.Text = result.Total == 0
                ? "没有找到匹配资源"
                : $"共 {result.Total} 个资源";
            StatusText.Text = result.Total == 0 ? "可以调整检索条件或上传新的项目包" : string.Empty;
            UpdatePagination();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            ResourceItemsControl.ItemsSource = null;
            ResultSummaryText.Text = "资源中心暂时不可用";
            StatusText.Text = ex.Message;
            UpdatePagination();
        }
    }

    private void UpdatePagination()
    {
        PageText.Text = $"第 {_currentPage} / {_totalPages} 页";
        PreviousPageButton.IsEnabled = _currentPage > 1;
        NextPageButton.IsEnabled = _currentPage < _totalPages;
    }

    private void SetBusy(string message)
    {
        ResultSummaryText.Text = message;
        StatusText.Text = string.Empty;
    }

    private async void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadResourcesAsync(1);
    }

    private async void SearchTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (!IsLoaded)
            return;

        await LoadResourcesAsync(1);
    }

    private void SelectUploadFileButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "选择要上传的 ReciteHelper 项目包",
            Filter = "ReciteHelper项目包 (*.rhp)|*.rhp",
            Multiselect = false
        };

        if (dialog.ShowDialog(this) != true)
            return;

        _selectedUploadFilePath = dialog.FileName;
        SelectedUploadFileText.Text = Path.GetFileName(dialog.FileName);
    }

    private async void UploadButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_selectedUploadFilePath))
        {
            MessageBox.Show("请先选择要上传的 .rhp 项目包。", "无法上传", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(UploaderTextBox.Text) ||
            string.IsNullOrWhiteSpace(SchoolTextBox.Text) ||
            string.IsNullOrWhiteSpace(SubjectTextBox.Text))
        {
            MessageBox.Show("请完整填写上传者、学校和科目名称。", "元数据不完整", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            IsEnabled = false;
            await _client.UploadAsync(
                _serverUrl,
                _selectedUploadFilePath,
                UploaderTextBox.Text,
                SchoolTextBox.Text,
                SubjectTextBox.Text);
            MessageBox.Show("资源上传成功。", "上传完成", MessageBoxButton.OK, MessageBoxImage.Information);
            await LoadResourcesAsync(1);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"上传失败：{ex.Message}", "上传失败", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsEnabled = true;
        }
    }

    private async void DownloadButton_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not ResourceCenterItem item)
            return;

        var dialog = new OpenFolderDialog
        {
            Title = "选择项目导入后的存放目录"
        };

        if (dialog.ShowDialog(this) != true)
            return;

        var tempDirectory = Path.Combine(Path.GetTempPath(), "ReciteHelperResourceCenter", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        var tempPath = Path.Combine(tempDirectory, item.FileName);

        try
        {
            IsEnabled = false;
            await _client.DownloadAsync(_serverUrl, item, tempPath);
            var importedProject = await _projectFileService.ImportProjectArchiveAsync(tempPath, dialog.FolderName);
            await _addRecentProjectAsync(importedProject.ProjectPath, importedProject.ProjectName);
            MessageBox.Show(
                $"资源已下载并导入：\n{importedProject.ProjectPath}",
                "导入完成",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"下载或导入失败：{ex.Message}", "导入失败", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsEnabled = true;
            TryDeleteFile(tempPath);
            TryDeleteDirectory(tempDirectory);
        }
    }

    private async void PreviousPageButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPage > 1)
            await LoadResourcesAsync(_currentPage - 1);
    }

    private async void NextPageButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPage < _totalPages)
            await LoadResourcesAsync(_currentPage + 1);
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // Temporary download cleanup is best-effort only.
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }
        catch
        {
            // Temporary download cleanup is best-effort only.
        }
    }
}
