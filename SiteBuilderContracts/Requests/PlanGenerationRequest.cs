using SiteBuilderContracts.Pipeline;

namespace SiteBuilderContracts.Requests;

public sealed class PlanGenerationRequest
{
    public DesignPlan Plan { get; set; } = new();
    public string? DesignSpecOverride { get; set; }
    public string? RepairInstructions { get; set; }
}
