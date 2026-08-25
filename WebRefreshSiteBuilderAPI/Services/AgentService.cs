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

    public async Task<string> SendHtmlExtractMessage(string prompt, string sourceHtml)
    {
        var response = "";
        
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

        var message = await _anthropicClient.Messages.Create(parameters);
        
        foreach (var block in message.Content)
        {
            if (block.TryPickText(out var textBlock))
            {
                Console.WriteLine(textBlock.Text);
                response += textBlock.Text;
            }
        }
        
        return response;
    }
}