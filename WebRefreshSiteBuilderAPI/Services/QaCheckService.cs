using System.Diagnostics;
using SiteBuilderContracts.Requests;
using SiteBuilderContracts.Responses;
using WebRefreshSiteBuilderAPI.Data;
using HtmlAgilityPack;

namespace WebRefreshSiteBuilderAPI.Services;

public class QaCheckService
{
    private readonly ILogger<QaCheckService> _logger;

    private static readonly string[] ForbiddenJsPatterns =
        ["fetch(", "XMLHttpRequest", "eval(", "innerHTML ="];

    public QaCheckService(ILogger<QaCheckService> logger)
    {
        _logger = logger;
    }

    public async Task<SiteBuilderApiResultWithPayload<QaResponse>> RunQaAsync(QaRequest request)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "qa-" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);

        try
        {
            foreach (var file in request.Files)
            {
                var path = Path.GetFullPath(Path.Combine(tempDir, file.FileName));
                if (!path.StartsWith(tempDir + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                    return SiteBuilderApiResultWithPayload<QaResponse>.Fail(SiteBuilderActionResult.RequestContentMissing, null, "Generated filename escapes the QA directory.");
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                await File.WriteAllTextAsync(path, file.Content);
            }

            var issues = new List<string>();

            if (request.Files.Any(f => f.FileName.EndsWith(".html")))
                issues.AddRange(await RunCheckAsync("npx", $"html-validate {tempDir}/*.html"));

            if (request.Files.Any(f => f.FileName.EndsWith(".css")))
                issues.AddRange(await RunCheckAsync("npx", $"stylelint {tempDir}/*.css"));

            foreach (var htmlFile in request.Files.Where(f => f.FileName.EndsWith(".html")))
            {
                var path = Path.Combine(tempDir, htmlFile.FileName);
                issues.AddRange(await RunCheckAsync("npx", $"axe {path} --exit"));
            }
            
            foreach (var jsFile in request.Files.Where(f => f.FileName.EndsWith(".js")))
            {
                issues.AddRange(CheckJsScope(jsFile.Content));
            }
            issues.AddRange(CheckHtmlReferences(request.Files));
            if (request.ExpectedSiteSpec is not null)
                issues.AddRange(CheckContentPreservation(request.Files, request.ExpectedSiteSpec));
            
            var result = new QaResponse { Passed = issues.Count == 0, Issues = issues };

            return SiteBuilderApiResultWithPayload<QaResponse>.Success(result);
        }
        finally
        {
            // Always clean up, even if a check throws — temp files shouldn't accumulate on the server.
            try { Directory.Delete(tempDir, recursive: true); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to clean up temp dir {Dir}", tempDir); }
        }
    }

    private List<string> CheckJsScope(string js)
        => ForbiddenJsPatterns.Where(js.Contains).Select(f => $"Forbidden JS pattern used: {f}").ToList();

    private static List<string> CheckHtmlReferences(IEnumerable<SiteBuilderContracts.Generation.SiteFile> files)
    {
        var names = files.Select(f => f.FileName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var issues = new List<string>();
        foreach (var file in files.Where(f => f.FileName.EndsWith(".html", StringComparison.OrdinalIgnoreCase)))
        {
            var doc = new HtmlDocument(); doc.LoadHtml(file.Content);
            foreach (var node in doc.DocumentNode.SelectNodes("//a[@href]|//img[@src]") ?? Enumerable.Empty<HtmlNode>())
            {
                var value = node.GetAttributeValue(node.Name == "a" ? "href" : "src", "");
                if (string.IsNullOrWhiteSpace(value) || value.StartsWith('#') || Uri.TryCreate(value, UriKind.Absolute, out _)) continue;
                var local = value.Split('#')[0].Split('?')[0];
                if (local.EndsWith(".html", StringComparison.OrdinalIgnoreCase) && !names.Contains(local)) issues.Add($"{file.FileName} references missing generated page: {value}");
                if (node.Name == "img" && string.IsNullOrWhiteSpace(node.GetAttributeValue("alt", ""))) issues.Add($"{file.FileName} has image without alt text: {value}");
            }
        }
        return issues;
    }

    private static List<string> CheckContentPreservation(IEnumerable<SiteBuilderContracts.Generation.SiteFile> files, SiteBuilderContracts.Generation.SiteSpec spec)
    {
        var allHtml = string.Join("\n", files.Where(f => f.FileName.EndsWith(".html", StringComparison.OrdinalIgnoreCase)).Select(f => f.Content));
        var issues = new List<string>();
        foreach (var text in spec.Pages.SelectMany(p => p.Sections).Select(s => s.BodyText).Where(t => !string.IsNullOrWhiteSpace(t)))
            if (!allHtml.Contains(text, StringComparison.Ordinal)) issues.Add("Generated output omitted source body text.");
        foreach (var image in spec.Pages.SelectMany(p => p.Sections).SelectMany(s => s.Images).Where(i => i.Role is "logo" or "content"))
            if (!allHtml.Contains(image.Src, StringComparison.Ordinal)) issues.Add($"Generated output omitted source image: {image.Src}");
        return issues.Distinct().ToList();
    }

    private async Task<List<string>> RunCheckAsync(string cmd, string args)
    {
        var psi = new ProcessStartInfo(cmd, args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        try
        {
            using var proc = Process.Start(psi)!;
            var output = await proc.StandardOutput.ReadToEndAsync();
            var error = await proc.StandardError.ReadToEndAsync();
            await proc.WaitForExitAsync();

            return proc.ExitCode != 0
                ? [$"{cmd} {args.Split(' ')[0]} failed: {(string.IsNullOrWhiteSpace(output) ? error : output)}"]
                : [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run {Cmd} {Args}", cmd, args);
            return [$"Could not run check: {cmd} ({ex.Message})"];
        }
    }
}
