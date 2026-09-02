using System.Text.RegularExpressions;
using SiteBuilderContracts.Generation;

namespace WebRefreshSiteBuilderAPI.Helpers;

public class HtmlGeneratorHelper
{
    public static List<SiteFile> ParseFiles(string response)
    {
        var files = new List<SiteFile>();
        var matches = Regex.Matches(response, @"```(\w+):(\S+)\r?\n(.*?)```", RegexOptions.Singleline);

        foreach (Match match in matches)
            files.Add(new SiteFile { FileName = match.Groups[2].Value, Content = match.Groups[3].Value });

        return files;
    }
}