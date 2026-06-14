namespace ReciteHelper.Application.Interfaces.Services;

public interface IQuestionBankTextService
{
    Task<string> ExtractAsync(IEnumerable<string> questionBankPaths);
}
