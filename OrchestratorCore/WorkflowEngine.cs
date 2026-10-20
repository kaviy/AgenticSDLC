namespace OrchestratorCore;

using OrchestratorCore.Abstractions;
using OrchestratorCore.Models;
using System.Diagnostics;

/// <summary>
/// Workflow execution engine coordinating SDLC stages with DAG execution,
/// dependency resolution, safety controls, and audit-grade observability.
/// </summary>
public class WorkflowEngine
{
    private readonly Dictionary<string, IStage> _stages = new();
    private readonly SafetyControls _safetyControls;
    private readonly AuditTrail _auditTrail;
    private readonly OrchestrationMetrics _metrics;

    public WorkflowEngine(SafetyControls? safetyControls = null, AuditTrail? auditTrail = null)
    {
        _safetyControls = safetyControls ?? new SafetyControls();
        _auditTrail = auditTrail ?? new AuditTrail();
        _metrics = new OrchestrationMetrics();
    }

    public IReadOnlyDictionary<string, IStage> Stages => _stages.AsReadOnly();
    public IReadOnlyList<AuditEntry> AuditEntries => _auditTrail.Entries;
    public OrchestrationMetrics Metrics => _metrics;

    /// <summary>Register a stage in the workflow.</summary>
    public void RegisterStage(IStage stage)
    {
        if (_stages.ContainsKey(stage.Name))
            throw new InvalidOperationException($"Stage '{stage.Name}' is already registered.");
        _stages[stage.Name] = stage;
    }

    /// <summary>Execute the workflow with topological ordering of stages.</summary>
    public async Task<ExecutionContext> ExecuteAsync(ExecutionContext context)
    {
        _metrics.TotalExecutions++;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _auditTrail.LogMetric(context.ExecutionId, "WorkflowStart", DateTime.UtcNow);

            // Topological sort the dependency graph
            var executionOrder = TopologicalSort();

            // Execute stages in order
            foreach (var stageName in executionOrder)
            {
                var stage = _stages[stageName];

                // Check entry gates
                var entryGatesValid = await ValidateEntryGatesAsync(context, stage);
                if (!entryGatesValid)
                {
                    _auditTrail.LogMetric(context.ExecutionId, "StageSkipped", stageName);
                    continue;
                }

                // Execute stage with retry logic
                var result = await ExecuteStageWithRetryAsync(context, stage);
                context.AddExecutionHistory(stageName, result);

                if (!result.IsSuccess)
                {
                    _auditTrail.LogStageComplete(context.ExecutionId, stageName, result);

                    if (_safetyControls.IsCriticalError(result.Message, result.Error))
                    {
                        _auditTrail.LogMetric(context.ExecutionId, "SafeStop", "Critical error detected");
                        _metrics.FailedExecutions++;
                        throw new OrchestrationException($"Critical error in stage '{stageName}': {result.Message}", result.Error);
                    }

                    if (_safetyControls.Rollback.RollbackPriorStagesOnFailure)
                    {
                        await RollbackPriorStagesAsync(context, stageName);
                    }

                    _metrics.FailedExecutions++;
                    throw new OrchestrationException($"Stage '{stageName}' failed: {result.Message}");
                }

                // Validate exit gates
                var exitGatesValid = await ValidateExitGatesAsync(context, stage, result);
                if (!exitGatesValid)
                {
                    _auditTrail.LogMetric(context.ExecutionId, "ExitGateFailed", stageName);
                    _metrics.FailedExecutions++;
                    throw new OrchestrationException($"Exit gates failed for stage '{stageName}'");
                }

                _auditTrail.LogStageComplete(context.ExecutionId, stageName, result);
                _metrics.MaxStageDurationMs = Math.Max(_metrics.MaxStageDurationMs, result.DurationMs);
                _metrics.MinStageDurationMs = Math.Min(_metrics.MinStageDurationMs, result.DurationMs);
            }

            context.CompletedAt = DateTime.UtcNow;
            _metrics.SuccessfulExecutions++;
            _metrics.TotalDurationMs += stopwatch.ElapsedMilliseconds;

            _auditTrail.LogMetric(context.ExecutionId, "WorkflowComplete", $"Success in {stopwatch.ElapsedMilliseconds}ms");
            return context;
        }
        catch (Exception ex)
        {
            context.CompletedAt = DateTime.UtcNow;
            _auditTrail.LogMetric(context.ExecutionId, "WorkflowFailed", ex.Message);
            throw;
        }
    }

    private async Task<StageResult> ExecuteStageWithRetryAsync(ExecutionContext context, IStage stage)
    {
        var startTime = DateTime.UtcNow;
        var lastException = (Exception?)null;

        for (int attempt = 1; attempt <= _safetyControls.Retry.MaxRetries; attempt++)
        {
            _auditTrail.LogStageStart(context.ExecutionId, stage.Name);

            try
            {
                var result = await stage.ExecuteAsync(context);
                result.RetryCount = attempt - 1;
                return result;
            }
            catch (Exception ex)
            {
                lastException = ex;
                if (attempt < _safetyControls.Retry.MaxRetries)
                {
                    var delayMs = _safetyControls.GetRetryDelayMs(attempt);
                    _auditTrail.LogRetry(context.ExecutionId, stage.Name, attempt, ex.Message);
                    _metrics.TotalRetries++;
                    await Task.Delay(delayMs);
                }
            }
        }

        var duration = (int)((DateTime.UtcNow - startTime).TotalMilliseconds);
        return new StageResult
        {
            IsSuccess = false,
            Message = $"Stage failed after {_safetyControls.Retry.MaxRetries} retries",
            Error = lastException,
            DurationMs = duration,
            RetryCount = _safetyControls.Retry.MaxRetries
        };
    }

    private async Task RollbackPriorStagesAsync(ExecutionContext context, string failedStageName)
    {
        var stagesInOrder = context.ExecutionHistory.Select(h => h.StageName).ToList();
        var failedIndex = stagesInOrder.IndexOf(failedStageName);

        if (failedIndex <= 0) return;

        // Rollback in reverse order
        for (int i = failedIndex - 1; i >= 0; i--)
        {
            var stageName = stagesInOrder[i];
            if (!_stages.TryGetValue(stageName, out var stage)) continue;

            var success = await stage.RollbackAsync(context);
            _auditTrail.LogRollback(context.ExecutionId, stageName, success);
            _metrics.TotalRollbacks++;
        }
    }

    private async Task<bool> ValidateEntryGatesAsync(ExecutionContext context, IStage stage)
    {
        foreach (var gate in stage.EntryGates)
        {
            var result = await gate.ValidateAsync(context);
            _auditTrail.LogGateValidation(context.ExecutionId, stage.Name, gate.Name, result.IsPassed, result.Messages);

            if (!result.IsPassed) return false;
        }
        return true;
    }

    private async Task<bool> ValidateExitGatesAsync(ExecutionContext context, IStage stage, StageResult stageResult)
    {
        foreach (var gate in stage.ExitGates)
        {
            var result = await gate.ValidateAsync(context, stageResult);
            _auditTrail.LogGateValidation(context.ExecutionId, stage.Name, gate.Name, result.IsPassed, result.Messages);

            if (!result.IsPassed) return false;
        }
        return true;
    }

    private List<string> TopologicalSort()
    {
        var visited = new HashSet<string>();
        var result = new List<string>();

        foreach (var stageName in _stages.Keys)
        {
            if (!visited.Contains(stageName))
                TopologicalSortDFS(stageName, visited, result);
        }

        return result;
    }

    private void TopologicalSortDFS(string stageName, HashSet<string> visited, List<string> result)
    {
        visited.Add(stageName);

        if (_stages.TryGetValue(stageName, out var stage))
        {
            foreach (var dependency in stage.Dependencies)
            {
                if (!visited.Contains(dependency))
                    TopologicalSortDFS(dependency, visited, result);
            }
        }

        result.Add(stageName);
    }
}

/// <summary>Exception thrown during workflow orchestration.</summary>
public class OrchestrationException : Exception
{
    public OrchestrationException(string message) : base(message) { }
    public OrchestrationException(string message, Exception? innerException) : base(message, innerException) { }
}
