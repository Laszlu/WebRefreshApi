using SiteBuilderContracts.Generation;
using SiteBuilderContracts.Pipeline;

namespace WebRefreshSiteBuilderAPI.Services;

public sealed class VisualQaService
{
    private readonly BrowserRenderService _browser;
    public VisualQaService(BrowserRenderService browser) => _browser = browser;

    public async Task<VisualQaResult> RunAsync(IEnumerable<SiteFile> files, CancellationToken ct = default)
    {
        var render = await _browser.RenderFilesAsync(files, ct);
        var issues = new List<string>();
        if (render.Error is not null) issues.Add(render.Error);
        if (render.ScreenshotBase64 is null) issues.Add("No browser screenshot was produced.");
        if (render.Layout.Any(r => r.Width <= 0 || r.Height <= 0)) issues.Add("One or more rendered landmarks have an empty rectangle.");
        return new VisualQaResult
        {
            Passed = issues.Count == 0,
            Issues = issues,
            Renders = [new PageEvidence { ScreenshotBase64 = render.ScreenshotBase64, Layout = render.Layout, BrowserError = render.Error }]
        };
    }
}
