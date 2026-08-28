namespace SiteBuilderContracts.Agents;

public interface IAgentClient
{
    Task<string> SendAsync(string model, string systemPrompt, string userContent, CancellationToken ct = default);
}