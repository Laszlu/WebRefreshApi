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
    {
        var model = _modelResolver.GetModel(AgentStage.Extract);
        var maxTokens = _modelResolver.GetMaxTokens(AgentStage.Extract);
        return _agentClient.SendAsync(model, maxTokens, prompt, sourceHtml, ct);
    }

    public Task<string> SendHtmlGenerateMessage(string prompt, string siteSpecJson, CancellationToken ct = default)
    {
        var model = _modelResolver.GetModel(AgentStage.Generate);
        var maxTokens = _modelResolver.GetMaxTokens(AgentStage.Generate);
        return _agentClient.SendAsync(model, maxTokens, prompt, siteSpecJson, ct);
    }
}