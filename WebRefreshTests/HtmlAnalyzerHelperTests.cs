using Shouldly;
using SiteBuilderContracts.Generation;
using WebRefreshSiteBuilderAPI.Helpers;

namespace WebRefreshTests;

public class HtmlAnalyzerHelperTests
{
    [Fact]
    public void TryParsePageSpec_ValidJson_ReturnsPageSpec()
    {
        var json = File.ReadAllText("./analyzer_test.json");

        var result = HtmlAnalyzerHelper.TryParsePageSpec(json);

        result.ShouldNotBeNull();
        //result.PageTitle.ShouldBe("Home");
        //result.Nav.Count.ShouldBe(1);
    }

    [Fact]
    public void TryParsePageSpec_MalformedJson_ReturnsNull()
    {
        var result = HtmlAnalyzerHelper.TryParsePageSpec("not json at all");
        result.ShouldBeNull();
    }

    [Theory]
    [InlineData("```json\n{\"pageTitle\":\"x\"}\n```", "{\"pageTitle\":\"x\"}")]
    [InlineData("{\"pageTitle\":\"x\"}", "{\"pageTitle\":\"x\"}")]
    public void StripCodeFence_HandlesFencedAndUnfencedInput(string input, string expected)
    {
        HtmlAnalyzerHelper.StripCodeFence(input).ShouldBe(expected);
    }

    [Theory]
    [InlineData("https://site.com/about", 3, "about.html")]
    [InlineData("https://site.com/", 3, "index.html")]
    [InlineData("(direct input)", 1, "index.html")]
    public void DeriveFileName_ProducesExpectedNames(string url, int pageCount, string expected)
    {
        HtmlAnalyzerHelper.DeriveFileName(url, pageCount).ShouldBe(expected);
    }

    [Fact]
    public void BuildInternalNav_DropsLinksToPagesNotInSet()
    {
        var pages = new List<PageSpec>
        {
            new() { Url = "https://site.com/", SuggestedFileName = "index.html",
                    Nav = [new NavItem { Label = "About", Href = "/about" },
                           new NavItem { Label = "Careers", Href = "/careers" }] },
            new() { Url = "https://site.com/about", SuggestedFileName = "about.html" }
            // note: no "careers" page in the set
        };

        var nav = HtmlAnalyzerHelper.BuildInternalNav(pages);

        nav.ShouldContain(n => n.Href == "about.html");
        nav.ShouldNotContain(n => n.Label == "Careers");
    }
}