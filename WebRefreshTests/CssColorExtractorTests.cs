using Shouldly;
using SiteBuilderContracts.Generation;
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
    public void ResolveBrandColors_CssSourcedPage_PreferredOverLlmSourced()
    {
        var pages = new List<PageSpec>
        {
            new() { Brand = new BrandSignals { PrimaryColor = "#llmguess", Source = "llm" } },
            new() { Brand = new BrandSignals { PrimaryColor = "#realcolor", Source = "css" } }
        };

        var result = CssColorExtractionHelper.ResolveBrandColors(pages);

        result.PrimaryColor.ShouldBe("#realcolor");
        result.Source.ShouldBe("css");
    }

    [Fact]
    public void ResolveBrandColors_NoCssAnywhere_FallsBackToLlm()
    {
        var pages = new List<PageSpec>
        {
            new() { Brand = new BrandSignals { PrimaryColor = "#llmguess", Source = "llm" } }
        };

        var result = CssColorExtractionHelper.ResolveBrandColors(pages);

        result.Source.ShouldBe("llm");
    }

    [Fact]
    public void ResolveBrandColors_NoSignalsAtAll_ReturnsNoneSource()
    {
        var pages = new List<PageSpec> { new() { Brand = new BrandSignals() } };

        var result = CssColorExtractionHelper.ResolveBrandColors(pages);

        result.Source.ShouldBe("none");
        result.PrimaryColor.ShouldBeNull();
    }
}