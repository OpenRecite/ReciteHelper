using ReciteHelper.Core.Interfaces.Services;
using ReciteHelper.Core.ValueObjects;
using System.Text.Json;

namespace ReciteHelper.Infrastructure.Services;

public sealed class RecentProjectService : IRecentProjectService
{
    private const int MaxRecentProjects = 10;
    private readonly string _recentProjectsPath;

    public RecentProjectService()
    {
        _recentProjectsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "recent_projects.json");
    }

    public async Task<IReadOnlyList<RecentProject>> LoadAsync()
    {
        if (!File.Exists(_recentProjectsPath))
            return [];

        await using var stream = File.OpenRead(_recentProjectsPath);
        var projects = await JsonSerializer.DeserializeAsync<List<RecentProject>>(stream);
        return Sort(projects ?? []);
    }

    public async Task<IReadOnlyList<RecentProject>> AddOrUpdateAsync(string projectPath, string? projectName = null)
    {
        var projects = (await LoadAsync()).ToList();
        var existingIndex = projects.FindIndex(
            project => string.Equals(project.ProjectPath, projectPath, StringComparison.OrdinalIgnoreCase));

        var displayName = projectName ?? Path.GetFileName(projectPath);
        var updated = RecentProject.Create(displayName, projectPath, DateTime.Now);

        if (existingIndex >= 0)
            projects[existingIndex] = updated;
        else
            projects.Add(updated);

        projects = Sort(projects).Take(MaxRecentProjects).ToList();
        await SaveAsync(projects);

        return projects;
    }

    public async Task<IReadOnlyList<RecentProject>> RemoveMissingAsync()
    {
        var projects = (await LoadAsync())
            .Where(project => File.Exists(project.ProjectPath))
            .ToList();

        await SaveAsync(projects);
        return Sort(projects);
    }

    private async Task SaveAsync(IReadOnlyList<RecentProject> projects)
    {
        var json = JsonSerializer.Serialize(projects, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_recentProjectsPath, json);
    }

    private static List<RecentProject> Sort(IEnumerable<RecentProject> projects)
    {
        return projects
            .OrderByDescending(project => project.LastAccessed)
            .ToList();
    }
}
