namespace ReciteHelper.Wpf.Models;

public sealed class ResourceCenterItem
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Uploader { get; set; } = string.Empty;
    public string School { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime UploadedAtUtc { get; set; }

    public string SizeText => SizeBytes switch
    {
        >= 1024 * 1024 => $"{SizeBytes / 1024d / 1024d:F1} MB",
        >= 1024 => $"{SizeBytes / 1024d:F1} KB",
        _ => $"{SizeBytes} B"
    };

    public string UploadedAtText => UploadedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
}
