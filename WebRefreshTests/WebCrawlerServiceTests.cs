using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using SiteBuilderContracts.Crawling;
using WebRefreshSiteBuilderAPI.Services;

namespace WebRefreshTests;

public class WebCrawlerServiceTests
{
    private class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Dictionary<string, string> _responses;
        public FakeHttpMessageHandler(Dictionary<string, string> responses) => _responses = responses;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var url = request.RequestUri!.ToString();

            if (!_responses.ContainsKey(url))
                throw new InvalidOperationException($"FakeHttpMessageHandler received unmapped URL: {url}");

            return Task.FromResult(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(_responses[url])
            });
        }
    }

    [Fact]
    public async Task CrawlAsync_RespectsMaxPages()
    {
        var responses = new Dictionary<string, string>
        {
            ["https://site.com/"] = "<a href='/page2'>2</a><a href='/page3'>3</a>",
            ["https://site.com/page2"] = "<html></html>",
            ["https://site.com/page3"] = "<html></html>"
        };

        var httpClient = new HttpClient(new FakeHttpMessageHandler(responses));
        var options = Options.Create(new CrawlOptions { MaxPages = 2, MaxDepth = 1, TimeoutSeconds = 5 });
        var logger = Substitute.For<ILogger<WebCrawlerService>>();

        var sut = new WebCrawlerService(httpClient, options, logger);
        var result = await sut.CrawlAsync("https://site.com/");

        result.Pages.Count.ShouldBe(2);
    }
    
    [Fact]
    public async Task CrawlAsync_FetchesLinkedStylesheets()
    {
        var responses = new Dictionary<string, string>
        {
            ["https://site.com/"] = "<link rel='stylesheet' href='/style.css'><a href='/page2'>2</a>",
            ["https://site.com/style.css"] = ":root { --brand: #ff0000; }",
            ["https://site.com/page2"] = "<html></html>"
        };

        var httpClient = new HttpClient(new FakeHttpMessageHandler(responses));
        var options = Options.Create(new CrawlOptions { MaxPages = 2, MaxDepth = 1, TimeoutSeconds = 5 });
        var logger = Substitute.For<ILogger<WebCrawlerService>>();

        var sut = new WebCrawlerService(httpClient, options, logger);
        var result = await sut.CrawlAsync("https://site.com/");

        var home = result.Pages.First(p => p.Url == "https://site.com/");
        home.StylesheetContents.ShouldContain(css => css.Contains("#ff0000"));
    }
}