using ReciteHelper.Core.Aggregates;
using ReciteHelper.Core.Entities;
using ReciteHelper.Core.ValueObjects;

namespace ReciteHelper.Core.Interfaces.Services;

public interface IExamPaperService
{
    List<Question> Generate(Project project, ExamSettings settings);
}
