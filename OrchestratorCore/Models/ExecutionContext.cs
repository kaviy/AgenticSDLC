namespace OrchestratorCore.Models;

/// <summary>
/// Cross-stage execution context preserving decision lineage and outputs.
/// </summary>
public class ExecutionContext
{
    /// <summary>Unique identifier for this execution run.</summary>
    public string ExecutionId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Original requirement text.</summary>
    public string Requirement { get; set; } = string.Empty;

    /// <summary>Clarifications or normalizations applied to requirement.</summary>
    public List<string> RequirementClarifications { get; set; } = new();

    /// <summary>Architectural decisions made during design stage.</summary>
    public Dictionary<string, string> ArchitectureDecisions { get; set; } = new();

    /// <summary>Implementation outputs (generated code, API specs, etc.).</summary>
    public Dictionary<string, object?> ImplementationOutputs { get; set; } = new();

    /// <summary>Test results and validation status.</summary>
    public Dictionary<string, object?> TestResults { get; set; } = new();

    /// <summary>Generated documentation.</summary>
    public Dictionary<string, object?> Documentation { get; set; } = new();

    /// <summary>Release readiness assessment.</summary>
    public Dictionary<string, object?> ReleaseAssessment { get; set; } = new();

    /// <summary>All stage execution results in order.</summary>
    public List<(string StageName, StageResult Result)> ExecutionHistory { get; set; } = new();

    /// <summary>Approval gates and their status.</summary>
    public List<(string GateName, bool IsApproved, string Reason)> ApprovalGates { get; set; } = new();

    /// <summary>Free-form metadata for extension stages.</summary>
    public Dictionary<string, object?> Metadata { get; set; } = new();

    /// <summary>Timestamp when execution started.</summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Completed at timestamp.</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>Add execution history entry.</summary>
    public void AddExecutionHistory(string stageName, StageResult result)
    {
        ExecutionHistory.Add((stageName, result));
    }

    /// <summary>Record an approval gate status.</summary>
    public void RecordApprovalGate(string gateName, bool isApproved, string reason = "")
    {
        ApprovalGates.Add((gateName, isApproved, reason));
    }

    /// <summary>Get execution duration in milliseconds.</summary>
    public long GetDurationMs()
    {
        var endTime = CompletedAt ?? DateTime.UtcNow;
        return (long)(endTime - StartedAt).TotalMilliseconds;
    }
}
