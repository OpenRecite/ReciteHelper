using LlmTornado;
using LlmTornado.Agents;
using LlmTornado.Chat.Models;
using ReciteHelper.Core.Interfaces.Services;

namespace ReciteHelper.Infrastructure.Services;

public sealed class AiChatService(HostedModelService hostedModelService) : IAiChatService
{
    public async Task<string> RunAsync(string deepSeekKey, string prompt, string? instructions = null)
    {
        if (string.IsNullOrWhiteSpace(deepSeekKey))
            return await hostedModelService.RunChatAsync(prompt, instructions);

        var api = new TornadoApi(deepSeekKey);
        var agent = new TornadoAgent(
            client: api,
            model: ChatModel.DeepSeek.Models.Chat,
            name: "ArchitectBot",
            instructions: instructions ?? "You are an assistant who is good at extracting knowledge.");

        var response = await agent.Run(prompt);
        return response.Messages.Last().Content ?? string.Empty;
    }
}
