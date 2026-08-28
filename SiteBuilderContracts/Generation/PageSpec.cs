namespace SiteBuilderContracts.Generation;

public class PageSpec
{
    public string Url { get; set; } = string.Empty;
    public string SuggestedFileName { get; set; } = string.Empty;
    public string PageTitle { get; set; } = string.Empty;
    public List<NavItem> Nav { get; set; } = new();
    public List<Section> Sections { get; set; } = new();
    public List<string> ExistingComponents { get; set; } = new();
}

public class NavItem
{
    public string Label { get; set; } = string.Empty;
    public string Href { get; set; } = string.Empty;
}

public class Section
{
    public string Type { get; set; } = string.Empty;
    public string? Heading { get; set; }
    public string BodyText { get; set; } = string.Empty;
    public List<ImageRef> Images { get; set; } = new();
}

public class ImageRef
{
    public string Src { get; set; } = string.Empty;
    public string Alt { get; set; } = string.Empty;
}