namespace OrchestratorCore.Stages;

using OrchestratorCore.Abstractions;
using OrchestratorCore.Models;

/// <summary>Base class for implementing common entry/exit gates.</summary>
public class CommonGates
{
    /// <summary>Gate: Validate that all dependencies have been executed.</summary>
    public class DependenciesExistGate : IEntryGate
    {
        private readonly IEnumerable<string> _dependencies;

        public string Name => "DependenciesExist";

        public DependenciesExistGate(IEnumerable<string> dependencies)
        {
            _dependencies = dependencies;
        }

        public Task<GateValidationResult> ValidateAsync(ExecutionContext context)
        {
            var executedStages = context.ExecutionHistory.Select(h => h.StageName).ToHashSet();
            var missingDeps = _dependencies.Where(d => !executedStages.Contains(d)).ToList();

            if (missingDeps.Any())
                return Task.FromResult(GateValidationResult.Fail(Name, $"Missing dependencies: {string.Join(", ", missingDeps)}"));

            return Task.FromResult(GateValidationResult.Pass(Name));
        }
    }

    /// <summary>Gate: Validate that prior stage outputs are not null/empty.</summary>
    public class RequiredStageOutputsExistGate : IEntryGate
    {
        private readonly string _stageName;
        private readonly string[] _requiredOutputKeys;

        public string Name => "RequiredOutputsExist";

        public RequiredStageOutputsExistGate(string stageName, params string[] requiredKeys)
        {
            _stageName = stageName;
            _requiredOutputKeys = requiredKeys;
        }

        public Task<GateValidationResult> ValidateAsync(ExecutionContext context)
        {
            var stageResult = context.ExecutionHistory
                .FirstOrDefault(h => h.StageName == _stageName).Result;

            if (stageResult == null)
                return Task.FromResult(GateValidationResult.Fail(Name, $"No outputs from stage '{_stageName}'"));

            var missingKeys = _requiredOutputKeys.Where(k => !stageResult.Output.ContainsKey(k)).ToList();
            if (missingKeys.Any())
                return Task.FromResult(GateValidationResult.Fail(Name, $"Missing outputs: {string.Join(", ", missingKeys)}"));

            return Task.FromResult(GateValidationResult.Pass(Name));
        }
    }

    /// <summary>Gate: Validate that stage executed successfully.</summary>
    public class StageSuccessGate : IExitGate
    {
        public string Name => "StageSuccess";

        public Task<GateValidationResult> ValidateAsync(ExecutionContext context, StageResult result)
        {
            if (!result.IsSuccess)
                return Task.FromResult(GateValidationResult.Fail(Name, $"Stage failed: {result.Message}"));

            return Task.FromResult(GateValidationResult.Pass(Name));
        }
    }

    /// <summary>Gate: Validate that stage produced required outputs.</summary>
    public class OutputCompletenessGate : IExitGate
    {
        private readonly string[] _requiredKeys;

        public string Name => "OutputCompleteness";

        public OutputCompletenessGate(params string[] requiredKeys)
        {
            _requiredKeys = requiredKeys;
        }

        public Task<GateValidationResult> ValidateAsync(ExecutionContext context, StageResult result)
        {
            var missingKeys = _requiredKeys.Where(k => !result.Output.ContainsKey(k)).ToList();
            if (missingKeys.Any())
                return Task.FromResult(GateValidationResult.Fail(Name, $"Missing output keys: {string.Join(", ", missingKeys)}"));

            return Task.FromResult(GateValidationResult.Pass(Name, "All required outputs present"));
        }
    }

    /// <summary>Gate: Validate that stage execution completed within acceptable time.</summary>
    public class DurationGate : IExitGate
    {
        private readonly long _maxDurationMs;

        public string Name => "DurationCheck";

        public DurationGate(long maxDurationMs)
        {
            _maxDurationMs = maxDurationMs;
        }

        public Task<GateValidationResult> ValidateAsync(ExecutionContext context, StageResult result)
        {
            if (result.DurationMs > _maxDurationMs)
                return Task.FromResult(GateValidationResult.Fail(Name, 
                    $"Stage took {result.DurationMs}ms, exceeded limit of {_maxDurationMs}ms"));

            return Task.FromResult(GateValidationResult.Pass(Name));
        }
    }
}

/// <summary>Base class for implementing SDLC stages.</summary>
public abstract class BaseStage : IStage
{
    public abstract string Name { get; }
    public abstract string Description { get; }
    public virtual IEnumerable<string> Dependencies => Enumerable.Empty<string>();
    public virtual IEnumerable<IEntryGate> EntryGates => Enumerable.Empty<IEntryGate>();
    public virtual IEnumerable<IExitGate> ExitGates => Enumerable.Empty<IExitGate>();

    public abstract Task<StageResult> ExecuteAsync(ExecutionContext context);

    public virtual Task<bool> RollbackAsync(ExecutionContext context)
    {
        return Task.FromResult(true);
    }

    public virtual bool CanSkip() => false;

    protected StageResult Success(string message = "", Dictionary<string, object?>? output = null)
    {
        return new StageResult
        {
            IsSuccess = true,
            Message = message,
            Output = output ?? new(),
            ExecutedAt = DateTime.UtcNow,
            DurationMs = 0
        };
    }

    protected StageResult Failure(string message, Exception? ex = null)
    {
        return new StageResult
        {
            IsSuccess = false,
            Message = message,
            Error = ex,
            ExecutedAt = DateTime.UtcNow,
            DurationMs = 0
        };
    }
}
