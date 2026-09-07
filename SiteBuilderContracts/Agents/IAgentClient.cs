namespace SiteBuilderContracts.Agents;

public interface IAgentClient
{
    Task<string> SendAsync(string model, int maxTokens, string systemPrompt, string userContent, CancellationToken ct = default);
}