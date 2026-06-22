using ReciteHelper.Core.Aggregates;
using ReciteHelper.Core.ValueObjects;

namespace ReciteHelper.Core.Interfaces.Services;

public interface IExamSettingsService
{
    Task SaveAsync(Project project, ExamSettings settings);
}
