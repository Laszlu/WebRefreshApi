using SiteBuilderContracts.Generation;

namespace SiteBuilderContracts.Requests;

public class HtmlGenerationRequest
{
    public SiteSpec SiteSpec { get; set; } = new();
    public string? DesignSpecOverride { get; set; }
}