using ReciteHelper.SharedKernel;
using System.Text.Json.Serialization;

namespace ReciteHelper.Core.ValueObjects;

public class RecentProject : ValueObject
{
    [JsonConstructor]
    public RecentProject() { }

    private RecentProject(string? projectName, string projectPath, DateTime lastAccessed)
    {
        ProjectName = projectName;
        ProjectPath = projectPath;
        LastAccessed = lastAccessed;

        Validate();
    }

    public string? ProjectName { get; init; }
    public string ProjectPath { get; init; }
    public DateTime LastAccessed { get; private set; }

    public override T Clone<T>()
    {
        return (T)(object)new RecentProject(ProjectName, ProjectPath, LastAccessed);
    }

    public RecentProject ModifyLastAccessedTime(DateTime lastAccessedTime)
    {
        var recentProject = new RecentProject(ProjectName, ProjectPath, lastAccessedTime);

        return recentProject;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        if (ProjectName is null || ProjectPath is null) yield break;

        yield return ProjectName;
        yield return ProjectPath;
    }

    public static RecentProject Create(string? projectName, string? projectPath, DateTime lastAccessed)
    {
        return Create(() =>
        {
            return new RecentProject(projectName, projectPath, lastAccessed);
        });
    }


    protected override void Validate()
    {
        if (string.IsNullOrEmpty(ProjectPath))
            throw new ArgumentException("The project path cannot be null.");
    }
}
