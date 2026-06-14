using ReciteHelper.Core.Aggregates;

namespace ReciteHelper.Application.Interfaces.Services;

public interface IGalGameService
{
    bool Exists(Project project);

    Task<object> CompileStoryAsync(string storyCode);

    Task<IReadOnlyList<object>> LoadStoryLinesAsync(Project project);

    Task SaveStoryLinesAsync(Project project, IEnumerable<object> storyLines);
}
