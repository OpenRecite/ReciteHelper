using ReciteHelper.Core.Interfaces.Services;

namespace ReciteHelper.Infrastructure.Services;

public sealed class PromptProvider : IPromptProvider
{
    public async Task<string> GetPromptAsync(string promptName)
    {
        var path = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Images",
            "Prompts",
            promptName);

        return await File.ReadAllTextAsync(path);
    }
}
