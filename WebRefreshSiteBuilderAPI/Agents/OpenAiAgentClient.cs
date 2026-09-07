using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using SiteBuilderContracts.Agents;
using SiteBuilderContracts.Config;

namespace WebRefreshSiteBuilderAPI.Agents;

public class OpenAiAgentClient : IAgentClient
{
    private readonly OpenAIClient _client;
    private readonly OpenAiOptions _options;

    public OpenAiAgentClient(OpenAIClient client, IOptions<OpenAiOptions> options)
    {
        _client = client;
        _options = options.Value;
    }

    public async Task<string> SendAsync(string model, int maxTokens, string systemPrompt, string userContent, CancellationToken ct = default)
    {
        var chatClient = _client.GetChatClient(ParseModel(model));

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userContent)
        };

        var completionOptions = new ChatCompletionOptions
        {
            MaxOutputTokenCount = maxTokens
        };

        var response = await chatClient.CompleteChatAsync(messages, completionOptions, ct);

        var result = "";
        foreach (var part in response.Value.Content)
            result += part.Text;

        return result;
    }

    private static string ParseModel(string name) => name switch
    {
        "gpt-5" => "gpt-5",
        "gpt-5-mini" => "gpt-5-mini",
        _ => throw new InvalidOperationException($"Unknown OpenAI model: {name}")
    };
}