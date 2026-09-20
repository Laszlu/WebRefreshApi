namespace SiteBuilderContracts.Requests;

public sealed class FullPipelineRequest
{
    public string Url { get; set; } = string.Empty;
    public int? MaxPagesOverride { get; set; }
    public string? DesignSpecOverride { get; set; }
    // One repair attempt avoids unbounded cost and makes the endpoint predictable.
    public int MaxRepairAttempts { get; set; } = 1;
}
