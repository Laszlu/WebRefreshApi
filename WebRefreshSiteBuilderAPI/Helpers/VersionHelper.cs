using Newtonsoft.Json;
using WebRefreshSiteBuilderAPI.Data;

namespace WebRefreshSiteBuilderAPI.Helpers;

public class VersionHelper
{
    public static VersionInfo? LoadVersionInfo()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "version.json");
        if (!File.Exists(path))
            return null;

        var json = File.ReadAllText(path);
        return JsonConvert.DeserializeObject<VersionInfo>(json);
    }
}
