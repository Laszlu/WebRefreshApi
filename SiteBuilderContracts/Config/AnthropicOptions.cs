namespace SiteBuilderContracts.Config;

public class AnthropicOptions
{
    public const string SectionName = "Anthropic";
    public string ApiKey { get; set; } = string.Empty;
    public int ExtractMaxTokens { get; set; } = 2000;
    public int GenerateMaxTokens { get; set; } = 20000;
    public string ExtractModel { get; set; } = string.Empty;
    public string GenerateModel { get; set; } = string.Empty;
}