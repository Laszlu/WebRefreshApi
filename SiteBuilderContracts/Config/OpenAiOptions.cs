namespace SiteBuilderContracts.Config;

public class OpenAiOptions
{
    public const string SectionName = "OpenAi";
    public string ApiKey { get; set; } = string.Empty;
    public int MaxTokens { get; set; } = 1024;
    public string ExtractModel { get; set; } = string.Empty;
    public string GenerateModel { get; set; } = string.Empty;
}