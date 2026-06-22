using ReciteHelper.Core.ValueObjects;

namespace ReciteHelper.Core.Interfaces.Services;

public interface IRecentProjectService
{
    Task<IReadOnlyList<RecentProject>> LoadAsync();
    Task<IReadOnlyList<RecentProject>> AddOrUpdateAsync(string projectPath, string? projectName = null);
    Task<IReadOnlyList<RecentProject>> RemoveMissingAsync();
}
