using ReciteHelper.Wpf.Models;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;

namespace ReciteHelper.Wpf.Services;

public sealed class ResourceCenterClient
{
    private readonly HttpClient _httpClient = new();

    public async Task<ResourceCenterSearchResult> SearchAsync(
        string serverUrl,
        int page,
        int pageSize,
        string? uploader,
        string? school,
        string? subject,
        CancellationToken cancellationToken = default)
    {
        var uri = BuildUri(serverUrl, "/api/resources", new Dictionary<string, string?>
        {
            ["page"] = page.ToString(),
            ["pageSize"] = pageSize.ToString(),
            ["uploader"] = uploader,
            ["school"] = school,
            ["subject"] = subject
        });

        return await _httpClient.GetFromJsonAsync<ResourceCenterSearchResult>(uri, cancellationToken)
            ?? new ResourceCenterSearchResult();
    }

    public async Task UploadAsync(
        string serverUrl,
        string filePath,
        string uploader,
        string school,
        string subject,
        CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(filePath);
        using var content = new MultipartFormDataContent();
        content.Add(new StreamContent(stream), "file", Path.GetFileName(filePath));
        content.Add(new StringContent(uploader.Trim()), "uploader");
        content.Add(new StringContent(school.Trim()), "school");
        content.Add(new StringContent(subject.Trim()), "subject");

        using var response = await _httpClient.PostAsync(BuildUri(serverUrl, "/api/resources"), content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var message = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(message) ? "上传失败。" : message);
        }
    }

    public async Task DownloadAsync(
        string serverUrl,
        ResourceCenterItem item,
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        var uri = BuildUri(serverUrl, $"/api/resources/{item.Id}/download");
        await using var stream = await _httpClient.GetStreamAsync(uri, cancellationToken);
        await using var output = File.Create(destinationPath);
        await stream.CopyToAsync(output, cancellationToken);
    }

    private static Uri BuildUri(string serverUrl, string path, IReadOnlyDictionary<string, string?>? query = null)
    {
        if (string.IsNullOrWhiteSpace(serverUrl))
            throw new InvalidOperationException("尚未配置资源中心服务器地址。");

        var builder = new UriBuilder(new Uri(new Uri(serverUrl.Trim().TrimEnd('/')), path));
        if (query is not null)
        {
            var queryText = string.Join("&", query
                .Where(pair => !string.IsNullOrWhiteSpace(pair.Value))
                .Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value!)}"));
            builder.Query = queryText;
        }

        return builder.Uri;
    }
}
