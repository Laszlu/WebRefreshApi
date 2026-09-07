using Anthropic;
using Anthropic.Models.Messages;
using Microsoft.Extensions.Options;
using SiteBuilderContracts.Agents;
using SiteBuilderContracts.Config;

namespace WebRefreshSiteBuilderAPI.Agents;

public class AnthropicAgentClient : IAgentClient
{
    private readonly AnthropicClient _client;
    private readonly ILogger<AnthropicAgentClient> _logger;

    public AnthropicAgentClient(AnthropicClient client, ILogger<AnthropicAgentClient> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<string> SendAsync(string model, int maxTokens, string systemPrompt, string userContent, CancellationToken ct = default)
    {
        var parameters = new MessageCreateParams
        {
            MaxTokens = maxTokens,
            Model = ParseModel(model),
            Messages = [new MessageParam { Role = Role.User, Content = systemPrompt + userContent }]
        };

        var message = await _client.Messages.Create(parameters, ct);

        _logger.LogInformation(
            "Anthropic call complete. Model={Model}, MaxTokens={MaxTokens}, InputTokens={Input}, OutputTokens={Output}, StopReason={StopReason}",
            model, maxTokens, message.Usage.InputTokens, message.Usage.OutputTokens, message.StopReason);

        var textBlocks = new List<string>();
        foreach (var block in message.Content)
            if (block.TryPickText(out var textBlock))
                textBlocks.Add(textBlock.Text);

        return string.Join("\n", textBlocks);
    }

    private static Model ParseModel(string name) => name switch
    {
        "claude-sonnet" => Model.ClaudeSonnet5,
        "claude-haiku" => Model.ClaudeHaiku4_5_20251001,
        _ => throw new InvalidOperationException($"Unknown Anthropic model: {name}")
    };
}