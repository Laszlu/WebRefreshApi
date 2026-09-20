using SiteBuilderContracts.Generation;

namespace SiteBuilderContracts.Pipeline;

// This is the immutable hand-off between the crawler/browser and every AI stage.
// Raw source remains available so no agent has to invent missing content.
public sealed class EvidenceBundle
{
    public List<PageEvidence> Pages { get; set; } = new();
    public ContentAnalysis Content { get; set; } = new();
    public AgentAnalysis Structure { get; set; } = new();
    public AgentAnalysis Visual { get; set; } = new();
    public NavigationAnalysis Navigation { get; set; } = new();
}

public sealed class PageEvidence
{
    public string Url { get; set; } = string.Empty;
    public string Html { get; set; } = string.Empty;
    public List<string> Colors { get; set; } = new();
    public List<AssetEvidence> Assets { get; set; } = new();
    public List<LinkEvidence> Links { get; set; } = new();
    public DomMetrics Dom { get; set; } = new();
    public string? ScreenshotBase64 { get; set; }
    public List<LayoutRectangle> Layout { get; set; } = new();
    public string? BrowserError { get; set; }
}

public sealed class AssetEvidence { public string Url { get; set; } = string.Empty; public string Kind { get; set; } = string.Empty; public string Alt { get; set; } = string.Empty; }
public sealed class LinkEvidence { public string Label { get; set; } = string.Empty; public string Href { get; set; } = string.Empty; public bool IsInternal { get; set; } }
public sealed class DomMetrics { public int ElementCount { get; set; } public int TextCharacters { get; set; } public int HeadingCount { get; set; } public int ImageCount { get; set; } public int LinkCount { get; set; } }
public sealed class LayoutRectangle { public string Selector { get; set; } = string.Empty; public double X { get; set; } public double Y { get; set; } public double Width { get; set; } public double Height { get; set; } }

public sealed class ContentAnalysis { public List<PageSpec> Pages { get; set; } = new(); public string RawResponse { get; set; } = string.Empty; }
public sealed class AgentAnalysis { public string RawResponse { get; set; } = string.Empty; }
public sealed class NavigationAnalysis { public List<NavItem> Items { get; set; } = new(); public string RawResponse { get; set; } = string.Empty; }

public sealed class DesignPlan
{
    public SiteSpec SiteSpec { get; set; } = new();
    public string InformationArchitecture { get; set; } = string.Empty;
    public List<string> Components { get; set; } = new();
    public string LayoutStrategy { get; set; } = string.Empty;
    public string ResponsiveStrategy { get; set; } = string.Empty;
    public string RawResponse { get; set; } = string.Empty;
}

public sealed class VisualQaResult { public bool Passed { get; set; } public List<string> Issues { get; set; } = new(); public List<PageEvidence> Renders { get; set; } = new(); }
