using OrchestratorCore.Engine;
using OrchestratorCore.Models;
using Xunit;

namespace OrchestratorCore.Tests;

public class SdlcOrchestratorTests
{
    [Fact]
    public void AddStep_CreatesWorkflowStep()
    {
        // Arrange
        var orchestrator = new SdlcOrchestrator("Test", "Unit");

        // Act
        var step = orchestrator.AddStep(
            SdlcPhase.Requirement,
            "Test Step",
            "Test Description"
        );

        // Assert
        Assert.NotNull(step);
        Assert.Equal("Test Step", step.Title);
        Assert.Equal(SdlcPhase.Requirement, step.Phase);
        Assert.Equal(PhaseStatus.Pending, step.Status);
    }

    [Fact]
    public void SetContext_StoresValue()
    {
        // Arrange
        var orchestrator = new SdlcOrchestrator("Test", "Unit");

        // Act
        orchestrator.SetContext("key1", "value1");
        var value = orchestrator.GetContext("key1");

        // Assert
        Assert.Equal("value1", value);
    }

    [Fact]
    public void CanExecuteStep_ReturnsFalseForUnmetDependencies()
    {
        // Arrange
        var orchestrator = new SdlcOrchestrator("Test", "Unit");
        var step1 = orchestrator.AddStep(SdlcPhase.Requirement, "Step 1", "");
        var step2 = orchestrator.AddStep(SdlcPhase.Design, "Step 2", "", new List<string> { step1.Id });

        // Act
        var canExecute = orchestrator.CanExecuteStep(step2);

        // Assert
        Assert.False(canExecute);
    }

    [Fact]
    public async Task ExecuteStepAsync_UpdatesStatus()
    {
        // Arrange
        var orchestrator = new SdlcOrchestrator("Test", "Unit");
        var step = orchestrator.AddStep(SdlcPhase.Requirement, "Test Step", "");

        orchestrator.RegisterStepHandler(step.Id, async (s) =>
        {
            await Task.Delay(10);
            return true;
        });

        // Act
        var result = await orchestrator.ExecuteStepAsync(step.Id);

        // Assert
        Assert.True(result);
        Assert.Equal(PhaseStatus.Completed, step.Status);
        Assert.NotNull(step.StartTime);
        Assert.NotNull(step.EndTime);
    }

    [Fact]
    public void RecordApproval_TracksApprovals()
    {
        // Arrange
        var orchestrator = new SdlcOrchestrator("Test", "Unit");
        var step = orchestrator.AddStep(
            SdlcPhase.Release,
            "Release",
            "",
            requiredApprovals: new List<string> { "reviewer1" }
        );

        // Act
        orchestrator.RecordApproval(step.Id, "reviewer1", ApprovalStatus.Approved);

        // Assert
        Assert.True(orchestrator.IsApprovalComplete(step.Id));
    }

    [Fact]
    public void RecordApproval_RejectsWhenApprovalDenied()
    {
        // Arrange
        var orchestrator = new SdlcOrchestrator("Test", "Unit");
        var step = orchestrator.AddStep(
            SdlcPhase.Release,
            "Release",
            "",
            requiredApprovals: new List<string> { "reviewer1" }
        );

        // Act
        orchestrator.RecordApproval(step.Id, "reviewer1", ApprovalStatus.Rejected, "Not ready");

        // Assert
        Assert.True(orchestrator.IsApprovalComplete(step.Id));
        Assert.False(orchestrator.IsApprovalComplete(step.Id)); // Gate is closed
    }

    [Fact]
    public async Task ExecuteWorkflowAsync_CompletesSuccessfully()
    {
        // Arrange
        var orchestrator = new SdlcOrchestrator("Test", "Unit");

        var req = orchestrator.AddStep(SdlcPhase.Requirement, "Req", "");
        var design = orchestrator.AddStep(SdlcPhase.Design, "Design", "", new List<string> { req.Id });

        orchestrator.RegisterStepHandler(req.Id, async (s) => { await Task.Delay(10); return true; });
        orchestrator.RegisterStepHandler(design.Id, async (s) => { await Task.Delay(10); return true; });

        // Act
        var result = await orchestrator.ExecuteWorkflowAsync();

        // Assert
        Assert.True(result.Success);
        Assert.Equal(WorkflowStatus.Completed, orchestrator.GetExecution().Status);
    }
}
