using ReciteHelper.Core.Enums;

namespace ReciteHelper.Model;

/// <summary>
/// Represents a file to be merged, including its contents and associated cluster type.
/// </summary>
public class MergeFile
{
    public List<string> Contents { get; set; } = new();

    public FileClusterType ClusterType { get; set; }
}