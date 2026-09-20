using SiteBuilderContracts.Generation;

namespace SiteBuilderContracts.Requests;

public sealed class BrowserRenderRequest
{
    public string? Url { get; set; }
    public List<SiteFile> Files { get; set; } = new();
}
