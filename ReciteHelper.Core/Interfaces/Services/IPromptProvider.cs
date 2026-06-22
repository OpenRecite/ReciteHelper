namespace ReciteHelper.Core.Interfaces.Services;

public interface IPromptProvider
{
    Task<string> GetPromptAsync(string promptName);
}
