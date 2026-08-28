using SiteBuilderContracts.Generation;

namespace SiteBuilderContracts.Responses;

public class HtmlGenerationResponse
{
    public string RawResponse { get; set; } = string.Empty;
    public List<SiteFile> Files { get; set; } = new();
}