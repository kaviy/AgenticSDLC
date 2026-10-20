namespace OrchestratorCore;

using OrchestratorCore.Models;

/// <summary>
/// Audit trail entry recording execution decisions and lineage.
/// </summary>
public record AuditEntry(
    string ExecutionId,
    DateTime Timestamp,
    string EventType,
    string StageName,
    string Message,
    Dictionary<string, object?> Metadata
);

/// <summary>
/// Comprehensive audit trail for SDLC orchestration execution.
/// Provides audit-grade observability and decision lineage.
/// </summary>
public class AuditTrail
{
    private readonly List<AuditEntry> _entries = new();
    private readonly object _lock = new();

    public IReadOnlyList<AuditEntry> Entries => _entries.AsReadOnly();

    /// <summary>Log a stage execution start.</summary>
    public void LogStageStart(string executionId, string stageName)
    {
        Log(executionId, "StageStart", stageName, $"Stage '{stageName}' execution started", new());
    }

    /// <summary>Log a stage execution completion.</summary>
    public void LogStageComplete(string executionId, string stageName, StageResult result)
    {
        var metadata = new Dictionary<string, object?>
        {
            { "Success", result.IsSuccess },
            { "DurationMs", result.DurationMs },
            { "RetryCount", result.RetryCount }
        };

        Log(executionId, "StageComplete", stageName, result.Message, metadata);
    }

    /// <summary>Log a gate validation.</summary>
    public void LogGateValidation(string executionId, string stageName, string gateName, bool passed, List<string> messages)
    {
        var metadata = new Dictionary<string, object?> { { "Passed", passed }, { "Messages", messages } };
        Log(executionId, "GateValidation", stageName, $"Gate '{gateName}': {(passed ? "PASS" : "FAIL")}", metadata);
    }

    /// <summary>Log a retry attempt.</summary>
    public void LogRetry(string executionId, string stageName, int attemptNumber, string reason)
    {
        Log(executionId, "Retry", stageName, $"Retry attempt {attemptNumber}: {reason}", new() { { "Attempt", attemptNumber } });
    }

    /// <summary>Log a rollback action.</summary>
    public void LogRollback(string executionId, string stageName, bool success)
    {
        Log(executionId, "Rollback", stageName, $"Rollback {(success ? "succeeded" : "failed")}", new() { { "Success", success } });
    }

    /// <summary>Log an approval gate result.</summary>
    public void LogApprovalGate(string executionId, string gateName, bool isApproved, string reason)
    {
        Log(executionId, "ApprovalGate", gateName, $"Approval: {isApproved} - {reason}", new() { { "IsApproved", isApproved } });
    }

    /// <summary>Log an architecture decision.</summary>
    public void LogArchitectureDecision(string executionId, string decision, string rationale)
    {
        Log(executionId, "ArchitectureDecision", "Design", decision, new() { { "Rationale", rationale } });
    }

    /// <summary>Log a requirement clarification.</summary>
    public void LogRequirementClarification(string executionId, string clarification)
    {
        Log(executionId, "RequirementClarification", "Requirements", clarification, new());
    }

    /// <summary>Log a policy check result.</summary>
    public void LogPolicyCheck(string executionId, string policyName, bool passed, string details)
    {
        Log(executionId, "PolicyCheck", "Governance", $"Policy '{policyName}': {(passed ? "PASS" : "FAIL")}", new() { { "Details", details } });
    }

    /// <summary>Log a metric snapshot.</summary>
    public void LogMetric(string executionId, string metricName, object value)
    {
        Log(executionId, "Metric", "Observability", $"{metricName}: {value}", new() { { "MetricName", metricName }, { "Value", value } });
    }

    /// <summary>Get all entries for an execution.</summary>
    public List<AuditEntry> GetEntriesForExecution(string executionId)
    {
        lock (_lock)
        {
            return _entries.Where(e => e.ExecutionId == executionId).ToList();
        }
    }

    /// <summary>Export audit trail as structured text.</summary>
    public string ExportAsText()
    {
        lock (_lock)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== Audit Trail ===");
            sb.AppendLine($"Total Entries: {_entries.Count}");
            sb.AppendLine();

            var grouped = _entries.GroupBy(e => e.ExecutionId);
            foreach (var executionGroup in grouped)
            {
                sb.AppendLine($"Execution: {executionGroup.Key}");
                foreach (var entry in executionGroup.OrderBy(e => e.Timestamp))
                {
                    sb.AppendLine($"  [{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] {entry.EventType,-20} {entry.StageName,-20} {entry.Message}");
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }

    private void Log(string executionId, string eventType, string stageName, string message, Dictionary<string, object?> metadata)
    {
        lock (_lock)
        {
            _entries.Add(new AuditEntry(executionId, DateTime.UtcNow, eventType, stageName, message, metadata));
        }
    }
}
