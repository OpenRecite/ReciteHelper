namespace ReciteHelper.Application.Interfaces.Services;

public interface IPromptProvider
{
    Task<string> GetPromptAsync(string promptName);
}
