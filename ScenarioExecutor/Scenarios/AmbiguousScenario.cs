using OrchestratorCore.Engine;
using OrchestratorCore.Models;
using OrchestratorCore.Persistence;

namespace ScenarioExecutor.Scenarios;

/// <summary>
/// Ambiguous Scenario: Conflicting requirements that require trade-off analysis and decision-making.
/// Challenge: "Build a URL shortener that is both ultra-fast and has full analytics"
/// Constraint: Limited storage and compute budget
/// This scenario demonstrates how decomposition helps identify and resolve trade-offs.
/// </summary>
public class AmbiguousScenario
{
    private readonly SdlcOrchestrator _orchestrator;
    private readonly WorkflowExecutionRepository _repository;

    public AmbiguousScenario()
    {
        _orchestrator = new SdlcOrchestrator("Trade-off Analysis", "Ambiguous");
        _repository = new WorkflowExecutionRepository("./workflows");
    }

    /// <summary>
    /// Execute the ambiguous scenario.
    /// </summary>
    public async Task ExecuteAsync()
    {
        Console.WriteLine("=== Ambiguous Scenario: Conflicting Requirements ===\n");

        DefineRequirementPhase();
        DefineDesignPhase();
        DefineImplementationPhase();
        DefineValidationPhase();
        DefineReleasePhase();

        var result = await _orchestrator.ExecuteWorkflowAsync();

        if (result.Success)
        {
            Console.WriteLine("\n✓ Ambiguous workflow resolved and completed!");
        }
        else
        {
            Console.WriteLine($"\n✗ Workflow failed: {result.Message}");
        }

        var summary = _orchestrator.GetSummary();
        Console.WriteLine($"\nExecution Summary:");
        Console.WriteLine($"  - Status: {summary.Status}");
        Console.WriteLine($"  - Success Rate: {summary.Metrics.SuccessRate:F1}%");
        Console.WriteLine($"  - Total Duration: {summary.TotalDurationSeconds:F2}s");

        var execution = _orchestrator.GetExecution();
        await _repository.SaveExecutionAsync(execution);
        Console.WriteLine($"\nExecution saved: {execution.Id}");
    }

    private void DefineRequirementPhase()
    {
        var requirementStep = _orchestrator.AddStep(
            SdlcPhase.Requirement,
            "Analyze Conflicting Requirements",
            "Parse and identify conflicting constraints in requirement"
        );

        _orchestrator.RegisterStepHandler(requirementStep.Id, async (step) =>
        {
            Console.WriteLine($"\n[AMBIGUOUS-REQUIREMENT] {step.Title}");
            Console.WriteLine("  → Analyzing conflicting requirements...\n");

            var conflictingReq = new
            {
                Requirement = "Build URL shortener that is both ultra-fast AND has full analytics",
                Goal = "Serve 10K req/sec with <50ms p99 latency",
                TrackingGoal = "Comprehensive analytics (all access details, referrers, geo, etc.)",
                Constraint = "Limited storage (100GB max) and compute budget",
                Conflicts = new[] 
                {
                    "Full analytics requires storing every access → Storage overhead",
                    "<50ms latency requires in-memory operations → Memory pressure",
                    "Storing 100% of access data conflicts with latency target"
                }
            };

            _orchestrator.SetContext("conflictingRequirement", conflictingReq);

            Console.WriteLine("  ⚠ Identified Conflicts:");
            foreach (var conflict in conflictingReq.Conflicts)
            {
                Console.WriteLine($"    {conflict}");
            }

            Console.WriteLine("\n  → Decomposing problem into sub-problems:");

            Console.WriteLine("    1. Analysis: Which analytics dimensions are critical?");
            Console.WriteLine("    2. Storage: Can we use tiered storage strategy?");
            Console.WriteLine("    3. Performance: Can we use async/deferred analytics?");
            Console.WriteLine("    4. Resolution: What trade-off is acceptable?");

            await Task.Delay(150);
            return true;
        });
    }

    private void DefineDesignPhase()
    {
        var designStep = _orchestrator.AddStep(
            SdlcPhase.Design,
            "Design Trade-off Solution",
            "Propose a design that balances conflicting requirements",
            new List<string> { _orchestrator.GetExecution().Steps[0].Id }
        );

        _orchestrator.RegisterStepHandler(designStep.Id, async (step) =>
        {
            Console.WriteLine($"\n[AMBIGUOUS-DESIGN] {step.Title}");
            Console.WriteLine("  → Evaluating trade-off options...\n");

            var options = new[]
            {
                new { 
                    Name = "OPTION A: Full Analytics Synchronously", 
                    Pros = "Complete data for every URL",
                    Cons = "Violates <50ms latency SLA (storage write overhead)",
                    Latency = "150-200ms",
                    Storage = "High",
                    Score = 3 
                },
                new { 
                    Name = "OPTION B: Async Analytics with Sampling", 
                    Pros = "Maintains <50ms latency, compresses storage 50x",
                    Cons = "Analytics not 100% accurate (sampling bias)",
                    Latency = "<50ms",
                    Storage = "Low-Medium",
                    Score = 8 
                },
                new { 
                    Name = "OPTION C: Tiered Storage (Hot/Cold)", 
                    Pros = "Full data access + latency target through tiering",
                    Cons = "Operational complexity, higher cost",
                    Latency = "<50ms sync + async cold storage",
                    Storage = "Medium",
                    Score = 7 
                }
            };

            Console.WriteLine("  Trade-off Analysis:");
            foreach (var option in options)
            {
                Console.WriteLine($"\n    {option.Name}");
                Console.WriteLine($"      Latency: {option.Latency}");
                Console.WriteLine($"      Storage: {option.Storage}");
                Console.WriteLine($"      Pros: {option.Pros}");
                Console.WriteLine($"      Cons: {option.Cons}");
                Console.WriteLine($"      Score: {option.Score}/10");
            }

            Console.WriteLine("\n  ✓ RECOMMENDED: Option B (Async + Sampling)");
            Console.WriteLine("    Rationale: Meets latency SLA, practical analytics accuracy");

            var design = new
            {
                SelectedOption = "Option B: Async Analytics with Sampling",
                Rationale = "Meets <50ms latency SLA while providing useful analytics",
                Implementation = new[]
                {
                    "Access tracking: Async queue (non-blocking)",
                    "Analytics dimension: Track top 1% of accesses (sampling)",
                    "Storage: In-memory cache for hot data, disk for cold",
                    "Accuracy trade-off: 1:100 sampling (1% of accesses recorded in detail)"
                },
                RiskMitigation = new[]
                {
                    "Sampling bias documented in analytics API",
                    "High-value URLsmay use opt-in full tracking",
                    "Alerts for anomaly detection (outlier sampling)"
                }
            };

            _orchestrator.SetContext("selectedDesign", design);
            step.Output = new Dictionary<string, object?>
            {
                { "selectedOption", design.SelectedOption },
                { "rationale", design.Rationale },
                { "tradeOffResolved", true }
            };

            await Task.Delay(150);
            return true;
        });
    }

    private void DefineImplementationPhase()
    {
        var implStep = _orchestrator.AddStep(
            SdlcPhase.Implementation,
            "Implement Trade-off Solution",
            "Code the selected design with explicit trade-off handling",
            new List<string> { _orchestrator.GetExecution().Steps[1].Id }
        );

        _orchestrator.RegisterStepHandler(implStep.Id, async (step) =>
        {
            Console.WriteLine($"\n[AMBIGUOUS-IMPLEMENTATION] {step.Title}");
            Console.WriteLine("  → Implementing async sampling analytics...\n");

            var sampleRate = 0.01; // 1% sampling
            var sampledAccesses = 0;
            var totalAccesses = 100;

            Console.WriteLine($"  Configuration:");
            Console.WriteLine($"    - Sampling Rate: 1% (1 in {(int)(1/sampleRate)} accesses)");
            Console.WriteLine($"    - Expected Storage Reduction: 100x");
            Console.WriteLine($"    - Expected Latency Impact: <1ms additional");

            // Simulate sampling
            Random r = new Random();
            for (int i = 0; i < totalAccesses; i++)
            {
                if (r.NextDouble() < sampleRate)
                {
                    sampledAccesses++;
                }
            }

            Console.WriteLine($"\n  Simulation of 100 accesses:");
            Console.WriteLine($"    - Total accesses: {totalAccesses}");
            Console.WriteLine($"    - Sampled accesses: {sampledAccesses} ({sampledAccesses*100.0/totalAccesses:F1}%)");
            Console.WriteLine($"    - Storage saved: {(1-sampledAccesses*1.0/totalAccesses)*100:F1}%");

            Console.WriteLine($"\n  ✓ Implementation complete with trade-off documented:");
            Console.WriteLine($"    - Async queue for non-blocking access recording");
            Console.WriteLine($"    - Sampling layer reduces storage by ~100x");
            Console.WriteLine($"    - GetAnalytics() returns sampled statistics");

            step.Output = new Dictionary<string, object?>
            {
                { "samplingRate", sampleRate },
                { "storageReduction", "100x" },
                { "latencyOverhead", "<1ms" },
                { "tradeOffImplemented", true }
            };

            await Task.Delay(200);
            return true;
        });
    }

    private void DefineValidationPhase()
    {
        var validationStep = _orchestrator.AddStep(
            SdlcPhase.Validation,
            "Validate Trade-off Acceptability",
            "Verify that the selected trade-off meets business requirements",
            new List<string> { _orchestrator.GetExecution().Steps[2].Id }
        );

        _orchestrator.RegisterStepHandler(validationStep.Id, async (step) =>
        {
            Console.WriteLine($"\n[AMBIGUOUS-VALIDATION] {step.Title}");
            Console.WriteLine("  → Validating trade-off against requirements...\n");

            var requirements = new[]
            {
                new { Requirement = "<50ms p99 latency", Solution = "Async sampling", Met = true },
                new { Requirement = "100GB storage limit", Solution = "1:100 sampling compression", Met = true },
                new { Requirement = "Actionable analytics", Solution = "Top referrers, daily trends tracked", Met = true },
                new { Requirement = "100% accuracy", Solution = "N/A (sampling trade-off)", Met = false }
            };

            Console.WriteLine("  Validation Matrix:");
            int metCount = 0;
            foreach (var req in requirements)
            {
                var status = req.Met ? "✓ YES" : "✗ NO (Trade-off accepted)";
                Console.WriteLine($"    {req.Requirement}: {status}");
                Console.WriteLine($"      Solution: {req.Solution}");
                if (req.Met) metCount++;
            }

            Console.WriteLine($"\n  → Business Decision:");
            Console.WriteLine($"    ✓ 3 of 4 critical requirements met");
            Console.WriteLine($"    ✓ Sampling trade-off approved by product owner");
            Console.WriteLine($"    ✓ Documentation of sampling bias included");
            Console.WriteLine($"    ✓ Monitoring for anomalies (detect sampling issues)");

            step.Output = new Dictionary<string, object?>
            {
                { "requirementsMet", 3 },
                { "totalRequirements", 4 },
                { "tradeOffApproved", true },
                { "validationStatus", "passed with trade-off acceptance" }
            };

            await Task.Delay(100);
            return true;
        });
    }

    private void DefineReleasePhase()
    {
        var releaseStep = _orchestrator.AddStep(
            SdlcPhase.Release,
            "Release with Trade-off Documentation",
            "Release the solution with clear communication of trade-offs",
            new List<string> { _orchestrator.GetExecution().Steps[3].Id }
        );

        _orchestrator.RegisterStepHandler(releaseStep.Id, async (step) =>
        {
            Console.WriteLine($"\n[AMBIGUOUS-RELEASE] {step.Title}");
            Console.WriteLine("  → Preparing release documentation...\n");

            Console.WriteLine("  Release Documentation:");
            Console.WriteLine("  1. Trade-off Summary:");
            Console.WriteLine("     - Chose Option B: Async + Sampling");
            Console.WriteLine("     - Reason: Meets latency SLA with practical analytics");
            Console.WriteLine();
            Console.WriteLine("  2. Customer Communication:");
            Console.WriteLine("     - 'Analytics powered by intelligent sampling'");
            Console.WriteLine("     - 'Delivers insights in <50ms with 100x storage efficiency'");
            Console.WriteLine();
            Console.WriteLine("  3. Internal Documentation:");
            Console.WriteLine("     - Sampling bias: 1:100 ratio");
            Console.WriteLine("     - Expected accuracy: ±1-2% for trending");
            Console.WriteLine("     - Edge case: Opt-in full tracking for high-value URLs");
            Console.WriteLine();
            Console.WriteLine("  4. Monitoring:");
            Console.WriteLine("     - Alert on sampling anomalies");
            Console.WriteLine("     - Dashboard showing sampled vs estimated metrics");

            step.Output = new Dictionary<string, object?>
            {
                { "releaseStatus", "approved" },
                { "productionReady", true },
                { "tradeOffCommunicated", true },
                { "documentationComplete", true }
            };

            await Task.Delay(200);
            return true;
        });
    }
}
