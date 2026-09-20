namespace SiteBuilderContracts.Agents;

public interface IAgentService
{
    Task<string> SendMessage(AgentStage stage, string prompt, string content, CancellationToken ct = default);
    Task<string> SendHtmlExtractMessage(string prompt, string sourceHtml, CancellationToken ct = default);
    Task<string> SendHtmlGenerateMessage(string prompt, string siteSpecJson, CancellationToken ct = default);
}
