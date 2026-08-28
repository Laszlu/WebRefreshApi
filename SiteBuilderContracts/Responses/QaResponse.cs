namespace SiteBuilderContracts.Responses;

public class QaResponse
{
    public bool Passed { get; set; }
    public List<string> Issues { get; set; } = new();
}