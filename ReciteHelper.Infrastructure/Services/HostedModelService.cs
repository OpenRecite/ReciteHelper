using ReciteHelper.Core.Interfaces.Configuration;
using ReciteHelper.Core.Configuration;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace ReciteHelper.Infrastructure.Services;

public sealed class HostedModelService(IConfigService configService)
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromMinutes(10)
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default)
    {
        var config = await configService.LoadAsync();
        return !string.IsNullOrWhiteSpace(config.HostedLicenseId) ||
               !string.IsNullOrWhiteSpace(config.HostedLicenseCode);
    }

    public async Task<HostedLicenseStatus> ValidateAsync(CancellationToken cancellationToken = default)
    {
        var config = await configService.LoadAsync();
        var serviceUrl = ResolveServiceUrl(config);
        if (string.IsNullOrWhiteSpace(config.HostedLicenseId))
            return new HostedLicenseStatus(false, "尚未激活托管服务。");

        try
        {
            var response = await PostAsync<HostedValidationResponse>(
                serviceUrl,
                "/api/licenses/validate",
                new HostedValidationRequest(
                    config.HostedLicenseId.Trim(),
                    GetMachineId()),
                cancellationToken);

            return new HostedLicenseStatus(
                response.IsValid,
                response.Message,
                response.ExpiresAtUtc,
                response.QuotaRemaining);
        }
        catch (Exception ex)
        {
            return new HostedLicenseStatus(false, ex.Message);
        }
    }

    public async Task<HostedLicenseStatus> ActivateAsync(
        string activationCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(activationCode))
            return new HostedLicenseStatus(false, "请输入激活码。");

        var config = await configService.LoadAsync();
        var serviceUrl = ResolveServiceUrl(config);
        try
        {
            var response = await PostAsync<HostedActivationResponse>(
                serviceUrl,
                "/api/licenses/activate",
                new HostedActivationRequest(
                    activationCode.Trim(),
                    GetMachineId(),
                    GetClientVersion()),
                cancellationToken);

            SaveHostedLicenseId(response.LicenseId);
            return new HostedLicenseStatus(true, "激活成功。", response.ExpiresAtUtc, response.QuotaRemaining);
        }
        catch (Exception ex)
        {
            return new HostedLicenseStatus(false, ex.Message);
        }
    }

    public async Task<string> RunChatAsync(
        string prompt,
        string? instructions = null,
        CancellationToken cancellationToken = default)
    {
        var activation = await EnsureActivatedAsync(cancellationToken);
        var response = await PostAsync<HostedChatResponse>(
            activation.ServiceUrl,
            "/api/hosted/chat",
            new HostedChatRequest(
                activation.LicenseId,
                GetMachineId(),
                prompt,
                instructions),
            cancellationToken);

        return response.Text;
    }

    public async Task<IReadOnlyList<float[]>> EmbedTextsAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default)
    {
        var activation = await EnsureActivatedAsync(cancellationToken);
        var response = await PostAsync<HostedEmbeddingResponse>(
            activation.ServiceUrl,
            "/api/hosted/embeddings",
            new HostedEmbeddingRequest(
                activation.LicenseId,
                GetMachineId(),
                texts),
            cancellationToken);

        return response.Vectors;
    }

    private async Task<HostedActivation> EnsureActivatedAsync(CancellationToken cancellationToken)
    {
        var config = await configService.LoadAsync();
        var serviceUrl = ResolveServiceUrl(config);
        if (!string.IsNullOrWhiteSpace(config.HostedLicenseId))
        {
            return new HostedActivation(serviceUrl, config.HostedLicenseId.Trim());
        }

        if (string.IsNullOrWhiteSpace(config.HostedLicenseCode))
            throw new InvalidOperationException("未配置 DeepSeek/Qwen Key，也未配置托管服务激活码。请配置 API Key，或在 Config.xml 中填写 HostedLicenseCode。");

        var response = await PostAsync<HostedActivationResponse>(
            serviceUrl,
            "/api/licenses/activate",
            new HostedActivationRequest(
                config.HostedLicenseCode.Trim(),
                GetMachineId(),
                GetClientVersion()),
            cancellationToken);

        SaveHostedLicenseId(response.LicenseId);
        return new HostedActivation(serviceUrl, response.LicenseId);
    }

    private static async Task<T> PostAsync<T>(
        string serviceUrl,
        string path,
        object request,
        CancellationToken cancellationToken)
    {
        var response = await HttpClient.PostAsJsonAsync(
            new Uri(new Uri(serviceUrl), path),
            request,
            JsonOptions,
            cancellationToken);

        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken)
                   ?? throw new InvalidOperationException("托管服务返回空响应。");

        var error = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new InvalidOperationException($"托管服务请求失败：{response.StatusCode} {ExtractErrorMessage(error)}");
    }

    private static string NormalizeServiceUrl(string? value)
    {
        var url = string.IsNullOrWhiteSpace(value) ? "http://localhost:5000" : value.Trim();
        return url.EndsWith('/') ? url : $"{url}/";
    }

    private static string ExtractErrorMessage(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return "服务端未返回错误详情。";

        try
        {
            using var document = JsonDocument.Parse(content);
            if (document.RootElement.TryGetProperty("message", out var message))
                return message.GetString() ?? content;
            if (document.RootElement.TryGetProperty("detail", out var detail))
                return detail.GetString() ?? content;
        }
        catch
        {
        }

        return content;
    }

    private static string ResolveServiceUrl(ConfigOptions config)
    {
        return NormalizeServiceUrl(
            !string.IsNullOrWhiteSpace(config.HostedServiceUrl)
                ? config.HostedServiceUrl
                : config.ResourceCenterServerUrl);
    }

    private static string GetMachineId()
    {
        var raw = $"{Environment.MachineName}|{Environment.UserName}|{Environment.OSVersion.VersionString}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
    }

    private static string GetClientVersion()
    {
        return typeof(HostedModelService).Assembly.GetName().Version?.ToString() ?? "unknown";
    }

    private static void SaveHostedLicenseId(string licenseId)
    {
        var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config.xml");
        var document = File.Exists(configPath)
            ? XDocument.Load(configPath)
            : new XDocument(new XElement("Config"));
        var root = document.Root ?? new XElement("Config");
        if (document.Root is null)
            document.Add(root);

        SetElement(root, "HostedLicenseId", licenseId);
        SetElement(root, "HostedLicenseCode", string.Empty);
        document.Save(configPath);
    }

    private static void SetElement(XElement root, string name, string value)
    {
        var element = root.Element(name);
        if (element is null)
        {
            element = new XElement(name);
            root.Add(element);
        }

        element.Value = value;
    }

    private sealed record HostedActivation(string ServiceUrl, string LicenseId);

    private sealed record HostedActivationRequest(string ActivationCode, string MachineId, string ClientVersion);

    private sealed record HostedActivationResponse(string LicenseId, DateTime ExpiresAtUtc, int QuotaRemaining);

    private sealed record HostedValidationRequest(string LicenseId, string MachineId);

    private sealed record HostedValidationResponse(bool IsValid, string Message, DateTime? ExpiresAtUtc, int QuotaRemaining);

    private sealed record HostedChatRequest(string LicenseId, string MachineId, string Prompt, string? Instructions);

    private sealed record HostedChatResponse(string Text);

    private sealed record HostedEmbeddingRequest(string LicenseId, string MachineId, IReadOnlyList<string> Texts);

    private sealed record HostedEmbeddingResponse(List<float[]> Vectors);
}

public sealed record HostedLicenseStatus(
    bool IsValid,
    string Message,
    DateTime? ExpiresAtUtc = null,
    int QuotaRemaining = 0);
