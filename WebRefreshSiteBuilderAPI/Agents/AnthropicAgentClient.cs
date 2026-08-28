using Anthropic;
using Anthropic.Models.Messages;
using Microsoft.Extensions.Options;
using SiteBuilderContracts.Agents;
using SiteBuilderContracts.Config;

namespace WebRefreshSiteBuilderAPI.Agents;

public class AnthropicAgentClient : IAgentClient
{
    private readonly AnthropicClient _client;
    private readonly AnthropicOptions _options;

    public AnthropicAgentClient(AnthropicClient client, IOptions<AnthropicOptions> options)
    {
        _client = client;
        _options = options.Value;
    }

    public async Task<string> SendAsync(string model, string systemPrompt, string userContent, CancellationToken ct = default)
    {
        var parameters = new MessageCreateParams
        {
            MaxTokens = _options.MaxTokens,
            Model = ParseModel(model),
            Messages = [new MessageParam { Role = Role.User, Content = systemPrompt + userContent }]
        };

        var message = await _client.Messages.Create(parameters, ct);

        var result = "";
        foreach (var block in message.Content)
            if (block.TryPickText(out var textBlock))
                result += textBlock.Text;

        return result;
    }

    private static Model ParseModel(string name) => name switch
    {
        "claude-sonnet-4-6" => Model.ClaudeSonnet4_6,
        "claude-haiku-4-5" => Model.ClaudeHaiku4_5,
        _ => throw new InvalidOperationException($"Unknown Anthropic model: {name}")
    };
}