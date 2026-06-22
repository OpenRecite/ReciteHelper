namespace ReciteHelper.Core.Interfaces.Services;

public interface IAiChatService
{
    Task<string> RunAsync(string deepSeekKey, string prompt, string? instructions = null);
}
