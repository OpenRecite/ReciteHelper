using ReciteHelper.Infrastructure.Services;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace ReciteHelper.Wpf.Views;

public partial class ActivationWindow : Window
{
    private readonly HostedModelService _hostedModelService;
    private readonly string _configPath;

    public ActivationWindow(HostedModelService hostedModelService)
    {
        _hostedModelService = hostedModelService;
        _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config.xml");
        InitializeComponent();
    }

    private void OpenConfigButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            EnsureConfigFile();
            Process.Start(new ProcessStartInfo
            {
                FileName = _configPath,
                UseShellExecute = true
            });
            StatusText.Foreground = Brushes.ForestGreen;
            StatusText.Text = "已打开 Config.xml。请填写 DeepSeekKey 和 QwenKey，保存后重新启动软件。";
        }
        catch (Exception ex)
        {
            StatusText.Foreground = Brushes.Firebrick;
            StatusText.Text = $"无法打开配置文件：{ex.Message}";
        }
    }

    private async void ActivateButton_Click(object sender, RoutedEventArgs e)
    {
        var code = ActivationCodeTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(code))
        {
            StatusText.Foreground = Brushes.Firebrick;
            StatusText.Text = "请输入激活码。";
            MessageBox.Show(
                "请先输入你获得的激活码，然后再点击“立即激活”。",
                "需要激活码",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        ActivateButton.IsEnabled = false;
        StatusText.Foreground = Brushes.DimGray;
        StatusText.Text = "正在验证激活码...";

        try
        {
            var result = await _hostedModelService.ActivateAsync(code);
            if (!result.IsValid)
            {
                ShowActivationFailed(result.Message);
                return;
            }

            ShowActivationSucceeded(result);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            ShowActivationFailed(ex.Message);
        }
        finally
        {
            ActivateButton.IsEnabled = true;
        }
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void EnsureConfigFile()
    {
        if (File.Exists(_configPath))
            return;

        File.WriteAllText(
            _configPath,
            """
            <Config>
              <Version>v3</Version>
              <DeepSeekKey></DeepSeekKey>
              <QwenKey></QwenKey>
              <ResourceCenterServerUrl>http://localhost:5000</ResourceCenterServerUrl>
              <HostedServiceUrl></HostedServiceUrl>
              <HostedLicenseCode></HostedLicenseCode>
              <HostedLicenseId></HostedLicenseId>
              <MissingStrategy>Ignore</MissingStrategy>
            </Config>
            """);
    }

    private void ShowActivationSucceeded(HostedLicenseStatus result)
    {
        StatusText.Foreground = Brushes.ForestGreen;
        StatusText.Text = $"激活成功。剩余额度：{FormatQuota(result.QuotaRemaining)}。";

        MessageBox.Show(
            BuildSuccessMessage(result),
            "激活成功",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void ShowActivationFailed(string detail)
    {
        StatusText.Foreground = Brushes.Firebrick;
        StatusText.Text = "激活失败，请检查激活码或服务连接。";

        MessageBox.Show(
            BuildFailureMessage(detail),
            "激活失败",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }

    private static string BuildSuccessMessage(HostedLicenseStatus result)
    {
        var expiresAt = result.ExpiresAtUtc is null
            ? "未设置到期时间"
            : result.ExpiresAtUtc.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm");

        return
            "激活已完成，可以开始使用一站式服务。\n\n" +
            $"剩余额度：{FormatQuota(result.QuotaRemaining)}\n" +
            $"有效期至：{expiresAt}\n\n" +
            "后续内容提取、题目生成和知识库向量生成将自动通过服务端完成。";
    }

    private static string BuildFailureMessage(string detail)
    {
        var cleanDetail = string.IsNullOrWhiteSpace(detail)
            ? "服务端未返回具体错误。"
            : detail.Trim();

        return
            "未能完成激活，请按下面顺序检查：\n\n" +
            "1. 激活码是否输入完整，是否已经被其他设备使用。\n" +
            "2. 当前网络是否可以访问配置中的资源中心/托管服务地址。\n" +
            "3. 如果你刚修改过 Config.xml，请保存后重新启动软件。\n\n" +
            $"详细信息：{cleanDetail}";
    }

    private static string FormatQuota(int quotaRemaining)
    {
        return quotaRemaining < 0 ? "不限" : quotaRemaining.ToString();
    }
}
