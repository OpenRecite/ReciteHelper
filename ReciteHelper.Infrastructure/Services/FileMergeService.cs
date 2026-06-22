using ReciteHelper.Core.Interfaces.Services;
using ReciteHelper.Core.Enums;
using ReciteHelper.Core.ValueObjects;
using ReciteHelper.Infrastructure.Utilities;
using System.Text.Json;

namespace ReciteHelper.Infrastructure.Services;

public sealed class FileMergeService : IFileMergeService
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".docx",
        ".pptx",
        ".pdf",
        ".txt",
        ".meg"
    };

    public bool IsSupportedFile(string filePath)
    {
        return SupportedExtensions.Contains(Path.GetExtension(filePath));
    }

    public async Task MergeAsync(IEnumerable<string> filePaths, FileClusterType clusterType, string outputPath)
    {
        var contents = new List<string>();

        foreach (var filePath in filePaths)
        {
            if (Path.GetExtension(filePath).Equals(".meg", StringComparison.OrdinalIgnoreCase))
            {
                var mergeFile = (MergeFile)ExtractText.FromAutomatic(filePath);
                contents.AddRange(mergeFile.Contents);
                continue;
            }

            contents.Add(ExtractText.FromAutomatic(filePath));
        }

        var merge = MergeFile.Create(contents, clusterType);
        var json = JsonSerializer.Serialize(merge);

        await File.WriteAllTextAsync(outputPath, json);
    }
}
