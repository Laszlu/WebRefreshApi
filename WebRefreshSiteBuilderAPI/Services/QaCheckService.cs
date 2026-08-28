using System.Diagnostics;
using SiteBuilderContracts.Requests;
using SiteBuilderContracts.Responses;
using WebRefreshSiteBuilderAPI.Data;

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
                await File.WriteAllTextAsync(Path.Combine(tempDir, file.FileName), file.Content);

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