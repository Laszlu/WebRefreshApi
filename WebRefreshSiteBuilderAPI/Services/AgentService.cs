using Anthropic;
using Anthropic.Models.Messages;
using Microsoft.Extensions.Options;
using SiteBuilderContracts.Agents;
using SiteBuilderContracts.Config;

namespace WebRefreshSiteBuilderAPI.Services;

public class AgentService : IAgentService
{
    private readonly IAgentClient _agentClient;
    private readonly IAgentModelResolver _modelResolver;

    public AgentService(IAgentClient agentClient, IAgentModelResolver modelResolver)
    {
        _agentClient = agentClient;
        _modelResolver = modelResolver;
    }

    public Task<string> SendHtmlExtractMessage(string prompt, string sourceHtml, CancellationToken ct = default)
        => SendMessage(AgentStage.Extract, prompt, sourceHtml, ct);

    public Task<string> SendMessage(AgentStage stage, string prompt, string content, CancellationToken ct = default)
    {
        var model = _modelResolver.GetModel(stage);
        var maxTokens = _modelResolver.GetMaxTokens(stage);
        return _agentClient.SendAsync(model, maxTokens, prompt, content, ct);
    }

    public Task<string> SendHtmlGenerateMessage(string prompt, string siteSpecJson, CancellationToken ct = default)
        => SendMessage(AgentStage.Generate, prompt, siteSpecJson, ct);
}
