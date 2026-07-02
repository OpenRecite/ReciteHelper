namespace ReciteHelper.Wpf.Models;

public sealed class ResourceCenterSearchResult
{
    public List<ResourceCenterItem> Items { get; set; } = [];
    public int Total { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
    public int TotalPages { get; set; } = 1;
}
