namespace ReciteHelper.Core.Interfaces.Services;

public interface IQuestionBankTextService
{
    Task<string> ExtractAsync(IEnumerable<string> questionBankPaths);
}
