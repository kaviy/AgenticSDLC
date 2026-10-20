namespace OrchestratorCore.Abstractions;

using OrchestratorCore.Models;

/// <summary>
/// Validates preconditions before a stage executes.
/// </summary>
public interface IEntryGate
{
    string Name { get; }
    Task<GateValidationResult> ValidateAsync(ExecutionContext context);
}

/// <summary>
/// Validates postconditions after a stage executes.
/// </summary>
public interface IExitGate
{
    string Name { get; }
    Task<GateValidationResult> ValidateAsync(ExecutionContext context, StageResult result);
}

/// <summary>
/// Represents a single stage in the SDLC orchestration workflow.
/// </summary>
public interface IStage
{
    /// <summary>Unique identifier for this stage.</summary>
    string Name { get; }

    /// <summary>Human-readable description.</summary>
    string Description { get; }

    /// <summary>Names of stages that must execute before this stage.</summary>
    IEnumerable<string> Dependencies { get; }

    /// <summary>Gates that validate preconditions for execution.</summary>
    IEnumerable<IEntryGate> EntryGates { get; }

    /// <summary>Gates that validate postconditions after execution.</summary>
    IEnumerable<IExitGate> ExitGates { get; }

    /// <summary>Execute the stage logic.</summary>
    /// <param name="context">The execution context with prior stage outputs.</param>
    /// <returns>Result of stage execution.</returns>
    Task<StageResult> ExecuteAsync(ExecutionContext context);

    /// <summary>Rollback any changes made by this stage.</summary>
    /// <param name="context">The execution context.</param>
    /// <returns>True if rollback succeeded, false otherwise.</returns>
    Task<bool> RollbackAsync(ExecutionContext context);

    /// <summary>Check if this stage can be skipped safely.</summary>
    bool CanSkip();
}
