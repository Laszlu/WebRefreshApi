using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using SiteBuilderContracts.Agents;
using SiteBuilderContracts.Requests;
using WebRefreshSiteBuilderAPI.Data;
using WebRefreshSiteBuilderAPI.Services;

namespace WebRefreshTests;

public class HtmlAnalyzerServiceTests
{
    [Fact]
    public async Task AnalyzeHtmlAsync_NoPages_ReturnsFailure()
    {
        var agentService = Substitute.For<IAgentService>();
        var logger = Substitute.For<ILogger<HtmlAnalyzerService>>();
        var sut = new HtmlAnalyzerService(agentService, logger);

        var result = await sut.AnalyzeHtmlAsync(new HtmlAnalysisRequest { Pages = [] });

        (result.ActionResult == SiteBuilderActionResult.RequestContentMissing).ShouldBeTrue();
    }

    [Fact]
    public async Task AnalyzeHtmlAsync_ValidPage_ReturnsSiteSpecWithMatchingPageCount()
    {
        var agentService = Substitute.For<IAgentService>();
        agentService.SendHtmlExtractMessage(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("""{"pageTitle":"Home","nav":[],"sections":[],"existingComponents":[]}""");

        var logger = Substitute.For<ILogger<HtmlAnalyzerService>>();
        var sut = new HtmlAnalyzerService(agentService, logger);

        var request = new HtmlAnalysisRequest
        {
            Pages = [new PageInput { Url = "https://site.com", Html = "<html></html>" }]
        };

        var result = await sut.AnalyzeHtmlAsync(request);

        (result.ActionResult == SiteBuilderActionResult.Success).ShouldBeTrue();
        result.Payload!.SiteSpec.Pages.Count.ShouldBe(1);
    }
    
    [Fact]
    public async Task AnalyzeHtmlAsync_CssColorsPresent_OverrideLlmColors()
    {
        var agentService = Substitute.For<IAgentService>();
        agentService.SendHtmlExtractMessage(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("""
                     {"pageTitle":"Home","nav":[],"sections":[],"existingComponents":[],
                      "brand":{"primaryColor":"#llmguess","accentColor":null,"backgroundColor":null,"rawColorHints":[]}}
                     """);

        var logger = Substitute.For<ILogger<HtmlAnalyzerService>>();
        var sut = new HtmlAnalyzerService(agentService, logger);

        var request = new HtmlAnalysisRequest
        {
            Pages =
            [
                new PageInput
                {
                    Url = "https://site.com",
                    Html = "<html></html>",
                    StylesheetContents = [":root { --x: #realcolor; }"]
                }
            ]
        };

        var result = await sut.AnalyzeHtmlAsync(request);

        result.Payload!.SiteSpec.Pages[0].Brand.PrimaryColor.ShouldBe("#realcolor");
        result.Payload.SiteSpec.Pages[0].Brand.Source.ShouldBe("css");
    }
}