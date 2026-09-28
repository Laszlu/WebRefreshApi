using System.Text.RegularExpressions;
using Newtonsoft.Json;
using SiteBuilderContracts.Generation;

namespace WebRefreshSiteBuilderAPI.Helpers;

public class HtmlGeneratorHelper
{
    public static List<SiteFile> ParseFiles(string response)
    {
        var files = new List<SiteFile>();
        var matches = Regex.Matches(response, @"```(\w+):(\S+)\r?\n(.*?)```", RegexOptions.Singleline);

        foreach (Match match in matches)
            files.Add(new SiteFile { FileName = match.Groups[2].Value, Content = DecodeEscapedContent(match.Groups[3].Value) });

        return files;
    }

    private static string DecodeEscapedContent(string content)
    {
        // Some providers return source code as a JSON-escaped string inside a
        // fenced block (e.g. \"<!doctype html>\\n<html lang=\\\"de\\\">\").
        // Decode only when those markers are present; normal HTML/CSS/JS keeps
        // its original whitespace and JavaScript string literals untouched.
        if (!content.Contains("\\n", StringComparison.Ordinal)
            && !content.Contains("\\r", StringComparison.Ordinal)
            && !content.Contains("\\\"", StringComparison.Ordinal))
            return content;

        try
        {
            return JsonConvert.DeserializeObject<string>($"\"{content}\"") ?? content;
        }
        catch (JsonException)
        {
            // A partially escaped model response is still more useful after
            // decoding its markup separators than as literal backslash text.
            return content.Replace("\\r\\n", "\r\n", StringComparison.Ordinal)
                .Replace("\\n", "\n", StringComparison.Ordinal)
                .Replace("\\r", "\r", StringComparison.Ordinal)
                .Replace("\\\"", "\"", StringComparison.Ordinal);
        }
    }
}
