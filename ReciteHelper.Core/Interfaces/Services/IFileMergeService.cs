using ReciteHelper.Core.Enums;

namespace ReciteHelper.Core.Interfaces.Services;

public interface IFileMergeService
{
    bool IsSupportedFile(string filePath);

    Task MergeAsync(IEnumerable<string> filePaths, FileClusterType clusterType, string outputPath);
}
