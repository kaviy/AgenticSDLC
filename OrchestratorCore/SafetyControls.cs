namespace OrchestratorCore;

/// <summary>
/// Safety controls for orchestration: retry, rollback, fallback, safe-stop.
/// </summary>
public class SafetyControls
{
    public class RetryPolicy
    {
        public int MaxRetries { get; set; } = 3;
        public int InitialDelayMs { get; set; } = 100;
        public double BackoffMultiplier { get; set; } = 2.0;
        public int MaxDelayMs { get; set; } = 10000;

        public int GetDelayForAttempt(int attemptNumber) =>
            Math.Min((int)(InitialDelayMs * Math.Pow(BackoffMultiplier, attemptNumber - 1)), MaxDelayMs);
    }

    public class RollbackPolicy
    {
        public bool IsEnabled { get; set; } = true;
        public bool RollbackPriorStagesOnFailure { get; set; } = true;
        public List<string> StagesToRollback { get; set; } = new();
    }

    public class FallbackPolicy
    {
        public string? FallbackStageName { get; set; }
        public bool UseFallbackOnCriticalError { get; set; } = true;
    }

    public class SafeStopPolicy
    {
        public bool StopOnCriticalError { get; set; } = true;
        public List<string> CriticalErrorPatterns { get; set; } = new()
        {
            "CRITICAL", "FATAL", "SECURITY", "BREACH", "DATA_LOSS"
        };
    }

    public RetryPolicy Retry { get; set; } = new();
    public RollbackPolicy Rollback { get; set; } = new();
    public FallbackPolicy Fallback { get; set; } = new();
    public SafeStopPolicy SafeStop { get; set; } = new();

    /// <summary>Determine if an error is critical and should trigger safe-stop.</summary>
    public bool IsCriticalError(string errorMessage, Exception? exception = null)
    {
        var combined = $"{errorMessage} {exception?.Message}".ToUpperInvariant();
        return SafeStop.CriticalErrorPatterns.Any(pattern => combined.Contains(pattern));
    }

    /// <summary>Calculate next retry delay for attempt number.</summary>
    public int GetRetryDelayMs(int attemptNumber) => Retry.GetDelayForAttempt(attemptNumber);
}

/// <summary>
/// Orchestration metrics for reliability tracking.
/// </summary>
public class OrchestrationMetrics
{
    public int TotalExecutions { get; set; }
    public int SuccessfulExecutions { get; set; }
    public int FailedExecutions { get; set; }
    public int TotalRetries { get; set; }
    public int TotalRollbacks { get; set; }
    public long TotalDurationMs { get; set; }
    public long MaxStageDurationMs { get; set; }
    public long MinStageDurationMs { get; set; } = long.MaxValue;

    public double GetSuccessRate() => TotalExecutions == 0 ? 0 : (double)SuccessfulExecutions / TotalExecutions;
    public double GetAverageDurationMs() => TotalExecutions == 0 ? 0 : (double)TotalDurationMs / TotalExecutions;

    public override string ToString() =>
        $"Executions: {TotalExecutions} | Success: {SuccessfulExecutions} ({GetSuccessRate():P}) | " +
        $"Failed: {FailedExecutions} | Retries: {TotalRetries} | Rollbacks: {TotalRollbacks} | " +
        $"Avg Duration: {GetAverageDurationMs():F0}ms | Max Stage: {MaxStageDurationMs}ms";
}
