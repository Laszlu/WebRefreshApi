using Anthropic;
using Anthropic.Models.Messages;
using Microsoft.Extensions.Options;
using SiteBuilderContracts.Config;

namespace WebRefreshSiteBuilderAPI.Services;

public class AgentService
{
    private readonly AnthropicClient _anthropicClient;
    
    private readonly AnthropicOptions _options;

    public AgentService(AnthropicClient anthropicClient, IOptions<AnthropicOptions> options)
    {
        _anthropicClient = anthropicClient;
        _options = options.Value;
    }

    public async Task SendHtmlExtractMessage(string prompt, string sourceHtml)
    {
        MessageCreateParams parameters = new MessageCreateParams
        {
            MaxTokens = 1024,
            Messages =
            [
                new MessageParam
                {
                    Role = Role.User,
                    Content = prompt + sourceHtml,
                },
            ],
            Model = Model.ClaudeSonnet4_6
        };

        //var response = await _anthropicClient.
    }
}