namespace SiteBuilderContracts.Agents;

public interface IAgentModelResolver
{
    string GetModel(AgentStage stage);
    int GetMaxTokens(AgentStage stage);
}