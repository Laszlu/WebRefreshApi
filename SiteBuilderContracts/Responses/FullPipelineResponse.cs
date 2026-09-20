using SiteBuilderContracts.Generation;
using SiteBuilderContracts.Pipeline;

namespace SiteBuilderContracts.Responses;

public sealed class FullPipelineResponse
{
    public EvidenceBundle Evidence { get; set; } = new();
    public DesignPlan Plan { get; set; } = new();
    public List<SiteFile> Files { get; set; } = new();
    public QaResponse DeterministicQa { get; set; } = new();
    public VisualQaResult VisualQa { get; set; } = new();
    public int RepairAttempts { get; set; }
}
