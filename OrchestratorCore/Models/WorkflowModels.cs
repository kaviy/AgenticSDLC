using System.Text.Json.Serialization;

namespace OrchestratorCore.Models;

/// <summary>
/// Represents the phase of the SDLC workflow.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SdlcPhase
{
    Requirement = 0,
    Design = 1,
    Implementation = 2,
    Testing = 3,
    Validation = 4,
    Release = 5
}

/// <summary>
/// Represents the status of a workflow phase.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PhaseStatus
{
    Pending = 0,
    InProgress = 1,
    AwaitingApproval = 2,
    Approved = 3,
    Rejected = 4,
    Completed = 5,
    Failed = 6,
    RolledBack = 7
}

/// <summary>
/// Represents a single step within an SDLC phase.
/// </summary>
public class WorkflowStep
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("phase")]
    public SdlcPhase Phase { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public PhaseStatus Status { get; set; } = PhaseStatus.Pending;

    [JsonPropertyName("dependencies")]
    public List<string> Dependencies { get; set; } = new();

    [JsonPropertyName("requiredApprovals")]
    public List<string> RequiredApprovals { get; set; } = new();

    [JsonPropertyName("output")]
    public Dictionary<string, object?>? Output { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("startTime")]
    public DateTime? StartTime { get; set; }

    [JsonPropertyName("endTime")]
    public DateTime? EndTime { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Represents an approval gate for a workflow step.
/// </summary>
public class ApprovalGate
{
    [JsonPropertyName("stepId")]
    public string StepId { get; set; } = string.Empty;

    [JsonPropertyName("requiredApprovers")]
    public List<string> RequiredApprovers { get; set; } = new();

    [JsonPropertyName("approvals")]
    public Dictionary<string, ApprovalDecision> Approvals { get; set; } = new();

    [JsonPropertyName("isApproved")]
    public bool IsApproved { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}

/// <summary>
/// Represents an approval decision for a gate.
/// </summary>
public class ApprovalDecision
{
    [JsonPropertyName("approver")]
    public string Approver { get; set; } = string.Empty;

    [JsonPropertyName("decision")]
    public ApprovalStatus Status { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ApprovalStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}

/// <summary>
/// Represents a complete workflow execution context.
/// </summary>
public class WorkflowExecution
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("scenario")]
    public string Scenario { get; set; } = string.Empty;

    [JsonPropertyName("steps")]
    public List<WorkflowStep> Steps { get; set; } = new();

    [JsonPropertyName("currentPhase")]
    public SdlcPhase CurrentPhase { get; set; } = SdlcPhase.Requirement;

    [JsonPropertyName("status")]
    public WorkflowStatus Status { get; set; } = WorkflowStatus.Active;

    [JsonPropertyName("approvalGates")]
    public Dictionary<string, ApprovalGate> ApprovalGates { get; set; } = new();

    [JsonPropertyName("context")]
    public Dictionary<string, object?> Context { get; set; } = new();

    [JsonPropertyName("auditLog")]
    public List<AuditEntry> AuditLog { get; set; } = new();

    [JsonPropertyName("metrics")]
    public ExecutionMetrics Metrics { get; set; } = new();

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("lastModifiedAt")]
    public DateTime LastModifiedAt { get; set; } = DateTime.UtcNow;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum WorkflowStatus
{
    Active = 0,
    Paused = 1,
    Completed = 2,
    Failed = 3,
    RolledBack = 4
}

/// <summary>
/// Audit entry for traceability and governance.
/// </summary>
public class AuditEntry
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("eventType")]
    public string EventType { get; set; } = string.Empty;

    [JsonPropertyName("actor")]
    public string Actor { get; set; } = "System";

    [JsonPropertyName("stepId")]
    public string? StepId { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("metadata")]
    public Dictionary<string, object?> Metadata { get; set; } = new();
}

/// <summary>
/// Captures execution metrics for the workflow.
/// </summary>
public class ExecutionMetrics
{
    [JsonPropertyName("successRate")]
    public double SuccessRate { get; set; }

    [JsonPropertyName("totalSteps")]
    public int TotalSteps { get; set; }

    [JsonPropertyName("completedSteps")]
    public int CompletedSteps { get; set; }

    [JsonPropertyName("failedSteps")]
    public int FailedSteps { get; set; }

    [JsonPropertyName("retryCount")]
    public int RetryCount { get; set; }

    [JsonPropertyName("rollbackCount")]
    public int RollbackCount { get; set; }

    [JsonPropertyName("startTime")]
    public DateTime StartTime { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("endTime")]
    public DateTime? EndTime { get; set; }

    [JsonPropertyName("totalDurationSeconds")]
    public double? TotalDurationSeconds
    {
        get => EndTime.HasValue ? (EndTime.Value - StartTime).TotalSeconds : null;
    }

    public void UpdateMetrics(List<WorkflowStep> steps)
    {
        TotalSteps = steps.Count;
        CompletedSteps = steps.Count(s => s.Status == PhaseStatus.Completed);
        FailedSteps = steps.Count(s => s.Status == PhaseStatus.Failed);
        SuccessRate = TotalSteps > 0 ? (CompletedSteps / (double)TotalSteps) * 100 : 0;
    }
}
