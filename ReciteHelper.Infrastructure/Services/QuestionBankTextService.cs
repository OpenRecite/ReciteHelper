using ReciteHelper.Core.Interfaces.Services;
using ReciteHelper.Infrastructure.Utilities;
using System.Text;

namespace ReciteHelper.Infrastructure.Services;

public sealed class QuestionBankTextService : IQuestionBankTextService
{
    public Task<string> ExtractAsync(IEnumerable<string> questionBankPaths)
    {
        var totalText = new StringBuilder();

        foreach (var path in questionBankPaths.Where(path => !string.IsNullOrWhiteSpace(path)))
        {
            try
            {
                totalText.AppendLine(ExtractText.FromAutomatic(path));
            }
            catch
            {
                continue;
            }
        }

        return Task.FromResult(totalText.ToString());
    }
}
