using Microsoft.Extensions.Options;
using SiteBuilderContracts.Agents;
using SiteBuilderContracts.Config;

namespace WebRefreshSiteBuilderAPI.Agents;

public class OpenAiModelResolver: IAgentModelResolver
{
    private readonly OpenAiOptions _options;

    public OpenAiModelResolver(IOptions<OpenAiOptions> options) => _options = options.Value;

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