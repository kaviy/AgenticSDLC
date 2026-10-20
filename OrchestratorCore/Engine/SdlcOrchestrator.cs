namespace OrchestratorCore.Engine;

using OrchestratorCore.Models;

/// <summary>
/// Core orchestration engine that manages workflow execution with gates and approval logic.
/// </summary>
public class SdlcOrchestrator
{
    private readonly WorkflowExecution _execution;
    private readonly Dictionary<string, Func<WorkflowStep, Task<bool>>> _stepHandlers;

    public SdlcOrchestrator(string workflowName, string scenario)
    {
        _execution = new WorkflowExecution
        {
            Name = workflowName,
            Scenario = scenario,
            Description = $"Execution of {workflowName} for {scenario} scenario"
        };

        _stepHandlers = new Dictionary<string, Func<WorkflowStep, Task<bool>>>();
    }

    /// <summary>
    /// Register a handler for a specific workflow step.
    /// </summary>
    public void RegisterStepHandler(string stepId, Func<WorkflowStep, Task<bool>> handler)
    {
        _stepHandlers[stepId] = handler;
    }

    /// <summary>
    /// Add a step to the workflow.
    /// </summary>
    public WorkflowStep AddStep(
        SdlcPhase phase,
        string title,
        string description,
        List<string>? dependencies = null,
        List<string>? requiredApprovals = null)
    {
        var step = new WorkflowStep
        {
            Phase = phase,
            Title = title,
            Description = description,
            Dependencies = dependencies ?? new(),
            RequiredApprovals = requiredApprovals ?? new()
        };

        _execution.Steps.Add(step);

        if (requiredApprovals?.Count > 0)
        {
            _execution.ApprovalGates[step.Id] = new ApprovalGate
            {
                StepId = step.Id,
                RequiredApprovers = requiredApprovals
            };
        }

        LogAudit("StepAdded", "System", step.Id, $"Added step: {title}");
        return step;
    }

    /// <summary>
    /// Set the global execution context.
    /// </summary>
    public void SetContext(string key, object? value)
    {
        _execution.Context[key] = value;
        LogAudit("ContextSet", "System", null, $"Context[{key}] = {value?.ToString() ?? "null"}");
    }

    /// <summary>
    /// Get a value from the execution context.
    /// </summary>
    public object? GetContext(string key)
    {
        return _execution.Context.TryGetValue(key, out var value) ? value : null;
    }

    /// <summary>
    /// Log an approval decision.
    /// </summary>
    public void RecordApproval(string stepId, string approver, ApprovalStatus status, string? reason = null)
    {
        if (!_execution.ApprovalGates.TryGetValue(stepId, out var gate))
        {
            throw new InvalidOperationException($"No approval gate found for step {stepId}");
        }

        gate.Approvals[approver] = new ApprovalDecision
        {
            Approver = approver,
            Status = status,
            Reason = reason
        };

        // Check if all required approvals are received
        var allApprovalReceived = gate.RequiredApprovers.All(a => gate.Approvals.ContainsKey(a));

        if (allApprovalReceived)
        {
            var allApproved = gate.Approvals.Values.All(a => a.Status == ApprovalStatus.Approved);
            gate.IsApproved = allApproved;

            if (!allApproved)
            {
                var rejectedApprovers = gate.Approvals.Where(kvp => kvp.Value.Status == ApprovalStatus.Rejected)
                    .Select(kvp => kvp.Key)
                    .ToList();
                gate.Reason = $"Rejected by: {string.Join(", ", rejectedApprovers)}";
            }
        }

        LogAudit("ApprovalRecorded", approver, stepId, $"Approval: {status}. Reason: {reason}");
    }

    /// <summary>
    /// Check if all dependencies for a step are met.
    /// </summary>
    public bool CanExecuteStep(WorkflowStep step)
    {
        // All dependencies must be completed
        return step.Dependencies.All(depId =>
        {
            var depStep = _execution.Steps.FirstOrDefault(s => s.Id == depId);
            return depStep?.Status == PhaseStatus.Completed;
        });
    }

    /// <summary>
    /// Check if a step awaiting approval can proceed.
    /// </summary>
    public bool IsApprovalComplete(string stepId)
    {
        if (!_execution.ApprovalGates.TryGetValue(stepId, out var gate))
        {
            return true; // No approval gate, can proceed
        }

        return gate.IsApproved;
    }

    /// <summary>
    /// Execute a single step.
    /// </summary>
    public async Task<bool> ExecuteStepAsync(string stepId)
    {
        var step = _execution.Steps.FirstOrDefault(s => s.Id == stepId) ?? 
            throw new InvalidOperationException($"Step {stepId} not found");

        if (!CanExecuteStep(step))
        {
            LogAudit("StepSkipped", "System", stepId, "Dependencies not met");
            return false;
        }

        step.Status = PhaseStatus.InProgress;
        step.StartTime = DateTime.UtcNow;
        LogAudit("StepStarted", "System", stepId, $"Executing: {step.Title}");

        try
        {
            var handler = _stepHandlers.TryGetValue(stepId, out var h) ? h : null;

            if (handler != null)
            {
                var result = await handler(step);
                if (!result)
                {
                    step.Status = PhaseStatus.Failed;
                    LogAudit("StepFailed", "System", stepId, "Handler returned false");
                    return false;
                }
            }

            step.Status = PhaseStatus.Completed;
            step.EndTime = DateTime.UtcNow;
            LogAudit("StepCompleted", "System", stepId, $"Completed: {step.Title}");
            _execution.Metrics.UpdateMetrics(_execution.Steps);
            return true;
        }
        catch (Exception ex)
        {
            step.Status = PhaseStatus.Failed;
            step.Error = ex.Message;
            step.EndTime = DateTime.UtcNow;
            LogAudit("StepFailed", "System", stepId, $"Exception: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Execute the workflow sequentially through all phases.
    /// </summary>
    public async Task<WorkflowExecutionResult> ExecuteWorkflowAsync()
    {
        LogAudit("WorkflowStarted", "System", null, $"Starting workflow: {_execution.Name}");
        _execution.Status = WorkflowStatus.Active;

        foreach (var phase in Enum.GetValues(typeof(SdlcPhase)).Cast<SdlcPhase>())
        {
            _execution.CurrentPhase = phase;
            var phaseSteps = _execution.Steps.Where(s => s.Phase == phase).ToList();

            foreach (var step in phaseSteps)
            {
                if (step.RequiredApprovals.Count > 0)
                {
                    step.Status = PhaseStatus.AwaitingApproval;
                    LogAudit("AwaitingApproval", "System", step.Id, $"Waiting for approvals on: {step.Title}");

                    if (!IsApprovalComplete(step.Id))
                    {
                        LogAudit("ApprovalNotReceived", "System", step.Id, "Cannot proceed without approval");
                        _execution.Status = WorkflowStatus.Paused;
                        return new WorkflowExecutionResult 
                        { 
                            Success = false, 
                            Message = $"Approval required for step: {step.Title}" 
                        };
                    }

                    step.Status = PhaseStatus.Approved;
                }

                var result = await ExecuteStepAsync(step.Id);
                if (!result)
                {
                    _execution.Status = WorkflowStatus.Failed;
                    return new WorkflowExecutionResult
                    {
                        Success = false,
                        Message = $"Step failed: {step.Title}. Error: {step.Error}"
                    };
                }
            }
        }

        _execution.Status = WorkflowStatus.Completed;
        _execution.Metrics.EndTime = DateTime.UtcNow;
        LogAudit("WorkflowCompleted", "System", null, "All steps completed successfully");

        return new WorkflowExecutionResult { Success = true };
    }

    /// <summary>
    /// Rollback the workflow to a previous state.
    /// </summary>
    public async Task<bool> RollbackAsync(string toStepId)
    {
        var targetStep = _execution.Steps.FirstOrDefault(s => s.Id == toStepId);
        if (targetStep == null)
        {
            return false;
        }

        LogAudit("RollbackInitiated", "System", toStepId, $"Rolling back to step: {targetStep.Title}");

        var stepsToRollback = _execution.Steps
            .Where(s => _execution.Steps.IndexOf(s) > _execution.Steps.IndexOf(targetStep))
            .ToList();

        foreach (var step in stepsToRollback)
        {
            step.Status = PhaseStatus.RolledBack;
            step.EndTime = DateTime.UtcNow;
            LogAudit("StepRolledBack", "System", step.Id, $"Rolled back: {step.Title}");
        }

        _execution.Metrics.RollbackCount++;
        _execution.Status = WorkflowStatus.Active;
        return true;
    }

    /// <summary>
    /// Pause the workflow.
    /// </summary>
    public void Pause()
    {
        _execution.Status = WorkflowStatus.Paused;
        LogAudit("WorkflowPaused", "System", null, "Workflow paused");
    }

    /// <summary>
    /// Resume the workflow from a paused state.
    /// </summary>
    public void Resume()
    {
        if (_execution.Status == WorkflowStatus.Paused)
        {
            _execution.Status = WorkflowStatus.Active;
            LogAudit("WorkflowResumed", "System", null, "Workflow resumed");
        }
    }

    /// <summary>
    /// Log an audit entry.
    /// </summary>
    private void LogAudit(string eventType, string actor, string? stepId, string description)
    {
        var entry = new AuditEntry
        {
            EventType = eventType,
            Actor = actor,
            StepId = stepId,
            Description = description
        };

        _execution.AuditLog.Add(entry);
        _execution.LastModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Get the current execution state.
    /// </summary>
    public WorkflowExecution GetExecution() => _execution;

    /// <summary>
    /// Get the execution summary.
    /// </summary>
    public WorkflowExecutionSummary GetSummary()
    {
        return new WorkflowExecutionSummary
        {
            WorkflowId = _execution.Id,
            WorkflowName = _execution.Name,
            Scenario = _execution.Scenario,
            Status = _execution.Status,
            CurrentPhase = _execution.CurrentPhase,
            Metrics = _execution.Metrics,
            AuditLogCount = _execution.AuditLog.Count,
            TotalDurationSeconds = _execution.Metrics.TotalDurationSeconds
        };
    }
}

/// <summary>
/// Result of a workflow execution.
/// </summary>
public class WorkflowExecutionResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Summary of workflow execution for reporting.
/// </summary>
public class WorkflowExecutionSummary
{
    public string WorkflowId { get; set; } = string.Empty;
    public string WorkflowName { get; set; } = string.Empty;
    public string Scenario { get; set; } = string.Empty;
    public WorkflowStatus Status { get; set; }
    public SdlcPhase CurrentPhase { get; set; }
    public ExecutionMetrics Metrics { get; set; } = new();
    public int AuditLogCount { get; set; }
    public double? TotalDurationSeconds { get; set; }
}
