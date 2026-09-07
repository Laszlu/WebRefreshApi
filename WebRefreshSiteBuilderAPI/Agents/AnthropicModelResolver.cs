using Microsoft.Extensions.Options;
using SiteBuilderContracts.Agents;
using SiteBuilderContracts.Config;

namespace WebRefreshSiteBuilderAPI.Agents;

public class AnthropicModelResolver : IAgentModelResolver
{
    private readonly AnthropicOptions _options;
    public AnthropicModelResolver(IOptions<AnthropicOptions> options) => _options = options.Value;

    public string GetModel(AgentStage stage) => stage switch
    {
        AgentStage.Extract => _options.ExtractModel,
        AgentStage.Generate => _options.GenerateModel,
        _ => throw new ArgumentOutOfRangeException(nameof(stage))
    };

    public int GetMaxTokens(AgentStage stage) => stage switch
    {
        AgentStage.Extract => _options.ExtractMaxTokens,
        AgentStage.Generate => _options.GenerateMaxTokens,
        _ => throw new ArgumentOutOfRangeException(nameof(stage))
    };
}