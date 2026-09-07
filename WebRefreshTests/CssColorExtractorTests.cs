using Shouldly;
using SiteBuilderContracts.Generation;
using SiteBuilderContracts.Requests;
using WebRefreshSiteBuilderAPI.Helpers;

namespace WebRefreshTests;

public class CssColorExtractionHelperTests
{
    [Fact]
    public void ExtractDeclaredColors_RootCustomProperties_TakePriority()
    {
        var css = ":root { --primary: #2952e3; --accent: #f7f7f8; }";

        var colors = CssColorExtractionHelper.ExtractDeclaredColors(css);

        colors.ShouldContain("#2952e3");
        colors.ShouldContain("#f7f7f8");
    }

    [Fact]
    public void ExtractDeclaredColors_NoRootVars_FallsBackToFrequency()
    {
        var css = """
                  .btn { color: #ff0000; }
                  .btn-secondary { color: #ff0000; }
                  .rare-thing { color: #00ff00; }
                  """;

        var colors = CssColorExtractionHelper.ExtractDeclaredColors(css);

        colors.IndexOf("#ff0000").ShouldBeLessThan(colors.IndexOf("#00ff00"));
    }

    [Fact]
    public void ExtractDeclaredColors_EmptyCss_ReturnsEmptyList()
    {
        CssColorExtractionHelper.ExtractDeclaredColors("").ShouldBeEmpty();
    }

    [Fact]
    public void ExtractDeclaredColors_MalformedCss_DoesNotThrow()
    {
        Should.NotThrow(() => CssColorExtractionHelper.ExtractDeclaredColors("{ not: valid ; css"));
    }

    [Theory]
    [InlineData("color: #123abc;", true)]
    [InlineData("color: rgb(255, 0, 0);", true)]
    [InlineData("color: hsl(200, 50%, 50%);", true)]
    [InlineData("color: inherit;", false)]
    [InlineData("color: var(--something);", false)]
    public void ExtractDeclaredColors_OnlyMatchesActualColorValues(string declaration, bool shouldFindColor)
    {
        var css = $".x {{ {declaration} }}";
        var colors = CssColorExtractionHelper.ExtractDeclaredColors(css);
        colors.Any().ShouldBe(shouldFindColor);
    }
    
    [Fact]
    public void ResolveBrandColors_SiteWideCssPresent_AppliesUniformly()
    {
        var pages = new List<PageSpec> { new(), new() };
        var siteWideColors = new List<string> { "#FF7D00", "#000000" };

        var result = CssColorExtractionHelper.ResolveBrandColors(pages, siteWideColors);

        result.PrimaryColor.ShouldBe("#FF7D00");
        result.Source.ShouldBe("css");
    }
    
    [Fact]
    public void ExtractSiteWideCssColors_DuplicateStylesheetAcrossPages_ParsedOnce()
    {
        var sharedCss = ":root { --primary: #FF7D00; --accent: #000000; }";
        var pages = new List<PageInput>
        {
            new() { Url = "https://site.com/a", StylesheetContents = [sharedCss] },
            new() { Url = "https://site.com/b", StylesheetContents = [sharedCss] }
        };

        var colors = CssColorExtractionHelper.ExtractSiteWideCssColors(pages);

        colors.ShouldContain("#FF7D00");
        colors.ShouldContain("#000000");
    }
}