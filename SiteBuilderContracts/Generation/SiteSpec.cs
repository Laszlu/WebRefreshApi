namespace SiteBuilderContracts.Generation;

public class SiteSpec
{
    public List<NavItem> Nav { get; set; } = new();
    public List<PageSpec> Pages { get; set; } = new();
}