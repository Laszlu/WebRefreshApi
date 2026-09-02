namespace SiteBuilderContracts.Agents;

public interface IAgentService
{
    Task<string> SendHtmlExtractMessage(string prompt, string sourceHtml, CancellationToken ct = default);
    Task<string> SendHtmlGenerateMessage(string prompt, string siteSpecJson, CancellationToken ct = default);
}