using Microsoft.Playwright;
using SiteBuilderContracts.Generation;
using SiteBuilderContracts.Pipeline;

namespace WebRefreshSiteBuilderAPI.Services;

public sealed class BrowserRenderService
{
    private readonly ILogger<BrowserRenderService> _logger;
    public BrowserRenderService(ILogger<BrowserRenderService> logger) => _logger = logger;

    public async Task<BrowserRenderResult> RenderRemoteAsync(string url, CancellationToken ct = default) =>
        await RenderAsync(async page => await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 20_000 }), ct);

    public async Task<BrowserRenderResult> RenderFilesAsync(IEnumerable<SiteFile> files, CancellationToken ct = default)
    {
        var html = files.FirstOrDefault(f => f.FileName.EndsWith(".html", StringComparison.OrdinalIgnoreCase));
        
        if (html is null) return new BrowserRenderResult { Error = "No generated HTML file was supplied." };
        
        var css = files.FirstOrDefault(f => f.FileName.Equals("style.css", StringComparison.OrdinalIgnoreCase))?.Content ?? string.Empty;
        
        var js = files.FirstOrDefault(f => f.FileName.Equals("script.js", StringComparison.OrdinalIgnoreCase))?.Content ?? string.Empty;
        
        var document = html.Content.Replace("</head>", $"<style>{css}</style></head>", StringComparison.OrdinalIgnoreCase)
            .Replace("</body>", $"<script>{js}</script></body>", StringComparison.OrdinalIgnoreCase);
        
        return await RenderAsync(async page => await page.SetContentAsync(document, new() { WaitUntil = WaitUntilState.NetworkIdle }), ct);
    }

    private async Task<BrowserRenderResult> RenderAsync(Func<IPage, Task> load, CancellationToken ct)
    {
        try
        {
            using var playwright = await Playwright.CreateAsync();
            
            await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
            
            var page = await browser.NewPageAsync(new() { ViewportSize = new ViewportSize { Width = 1440, Height = 900 } });
            
            await load(page);
            
            var screenshot = await page.ScreenshotAsync(new() { FullPage = true, Type = ScreenshotType.Png });
            
            var layout = await page.Locator("header, nav, main, section, footer, article, img").EvaluateAllAsync<LayoutRectangle[]>("els => els.slice(0, 100).map((el, i) => { const r = el.getBoundingClientRect(); return { selector: el.tagName.toLowerCase() + ':nth-of-type(' + (i + 1) + ')', x: r.x, y: r.y, width: r.width, height: r.height }; })");
            
            return new BrowserRenderResult { ScreenshotBase64 = Convert.ToBase64String(screenshot), Layout = layout.ToList() };
        }
        catch (Exception ex) when (ex is PlaywrightException or FileNotFoundException)
        {
            _logger.LogWarning(ex, "Browser render unavailable");
            
            return new BrowserRenderResult { Error = "Browser render unavailable. Install Playwright Chromium with 'playwright install chromium'." };
        }
    }
}

public sealed class BrowserRenderResult { public string? ScreenshotBase64 { get; set; } public List<LayoutRectangle> Layout { get; set; } = new(); public string? Error { get; set; } }
