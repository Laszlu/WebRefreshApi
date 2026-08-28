using SiteBuilderContracts.Generation;

namespace SiteBuilderContracts.Requests;

public class QaRequest
{
    public List<SiteFile> Files { get; set; } = new();
}