using OrchestratorCore.Engine;
using OrchestratorCore.Models;
using OrchestratorCore.Persistence;
using UrlShortenerService.Repository;
using UrlShortenerService.Service;

namespace ScenarioExecutor.Scenarios;

/// <summary>
/// Brownfield Scenario: Add analytics feature to an existing URL shortener.
/// Demonstrates incremental enhancement, impact analysis, and integration testing.
/// </summary>
public class BrownfieldScenario
{
    private readonly SdlcOrchestrator _orchestrator;
    private readonly WorkflowExecutionRepository _repository;
    private UrlRepository? _urlRepository;
    private UrlShortenerService.Service.UrlShortenerService? _urlService;

    public BrownfieldScenario()
    {
        _orchestrator = new SdlcOrchestrator("Add Analytics Feature", "Brownfield");
        _repository = new WorkflowExecutionRepository("./workflows");
    }

    /// <summary>
    /// Execute the brownfield scenario.
    /// </summary>
    public async Task ExecuteAsync()
    {
        Console.WriteLine("=== Brownfield Scenario: Add Analytics Feature ===\n");

        // Simulate existing system
        InitializeExistingSystem();

        DefineRequirementPhase();
        DefineDesignPhase();
        DefineImplementationPhase();
        DefineIntegrationPhase();
        DefineValidationPhase();
        DefineReleasePhase();

        var result = await _orchestrator.ExecuteWorkflowAsync();

        if (result.Success)
        {
            Console.WriteLine("\n✓ Brownfield workflow completed successfully!");
        }
        else
        {
            Console.WriteLine($"\n✗ Brownfield workflow failed: {result.Message}");
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

    private void InitializeExistingSystem()
    {
        Console.WriteLine("[EXISTING SYSTEM] Initializing existing URL shortener...");

        _urlRepository = new UrlRepository();
        _urlService = new UrlShortenerService.Service.UrlShortenerService(_urlRepository);

        // Create some existing URLs
        var url1 = _urlService.ShortenUrl("https://github.com/dotnet/roslyn");
        var url2 = _urlService.ShortenUrl("https://www.microsoft.com");

        // Record some accesses
        _urlService.GetRedirectUrl(url1.ShortCode, "Chrome", "192.168.1.1");
        _urlService.GetRedirectUrl(url1.ShortCode, "Firefox", "192.168.1.2");

        Console.WriteLine($"  ✓ System initialized with {_urlService.ListUrls().Count} URLs\n");
    }

    private void DefineRequirementPhase()
    {
        var requirementStep = _orchestrator.AddStep(
            SdlcPhase.Requirement,
            "Analyze Enhancement Request",
            "Evaluate the request to add enhanced analytics without breaking existing functionality"
        );

        _orchestrator.RegisterStepHandler(requirementStep.Id, async (step) =>
        {
            Console.WriteLine($"\n[BROWNFIELD-REQUIREMENT] {step.Title}");
            Console.WriteLine("  → Analyzing enhancement scope...");

            var enhancement = new
            {
                Feature = "Enhanced Analytics",
                NewCapabilities = new[] 
                { 
                    "Daily access breakdown",
                    "Top referrers tracking",
                    "Geographic distribution (future)"
                },
                BackwardCompatibility = "Must maintain existing APIs",
                RiskAreas = new[] 
                { 
                    "Data structure changes",
                    "Migration of existing data",
                    "Performance impact of analytics queries"
                }
            };

            _orchestrator.SetContext("enhancement", enhancement);
            Console.WriteLine("  ✓ Enhancement analyzed:");
            foreach (var cap in enhancement.NewCapabilities)
            {
                Console.WriteLine($"    - {cap}");
            }

            await Task.Delay(100);
            return true;
        });
    }

    private void DefineDesignPhase()
    {
        var designStep = _orchestrator.AddStep(
            SdlcPhase.Design,
            "Design Analytics Integration",
            "Plan how analytics will be integrated without impacting existing code",
            new List<string> { _orchestrator.GetExecution().Steps[0].Id }
        );

        _orchestrator.RegisterStepHandler(designStep.Id, async (step) =>
        {
            Console.WriteLine($"\n[BROWNFIELD-DESIGN] {step.Title}");
            Console.WriteLine("  → Designing integration points...");

            var design = new
            {
                Strategy = "Embedded analytics tracking",
                ImpactedModules = new[] { "UrlService", "Repository" },
                NewClasses = new[] { "AccessLog", "UrlAnalytics" },
                MigrationPlan = "In-memory state preserved, no data loss",
                ApiChanges = "Additive only (GetAnalytics method added)"
            };

            _orchestrator.SetContext("analyticsDesign", design);
            Console.WriteLine("  ✓ Design decisions:");
            Console.WriteLine($"    - Strategy: {design.Strategy}");
            Console.WriteLine($"    - API Changes: {design.ApiChanges}");

            step.Output = new Dictionary<string, object?>
            {
                { "impactedModules", design.ImpactedModules },
                { "newClasses", design.NewClasses },
                { "breakingChanges", "none" }
            };

            await Task.Delay(100);
            return true;
        });
    }

    private void DefineImplementationPhase()
    {
        var implStep = _orchestrator.AddStep(
            SdlcPhase.Implementation,
            "Implement Analytics Feature",
            "Integrate analytics tracking into existing service",
            new List<string> { _orchestrator.GetExecution().Steps[1].Id }
        );

        _orchestrator.RegisterStepHandler(implStep.Id, async (step) =>
        {
            Console.WriteLine($"\n[BROWNFIELD-IMPLEMENTATION] {step.Title}");
            Console.WriteLine("  → Enhancing service with analytics...");

            if (_urlService == null)
                return false;

            // Feature is already implemented in the service
            // This simulates adding GetAnalytics method
            var existingUrls = _urlService.ListUrls();
            Console.WriteLine($"  ✓ Enhanced {existingUrls.Count} existing URLs with analytics capability");

            // Verify existing data still works
            foreach (var url in existingUrls)
            {
                var analytics = _urlService.GetAnalytics(url.ShortCode);
                if (analytics != null && analytics.TotalAccesses > 0)
                {
                    Console.WriteLine($"    - {url.ShortCode}: {analytics.TotalAccesses} accesses tracked");
                }
            }

            step.Output = new Dictionary<string, object?>
            {
                { "urlsEnhanced", existingUrls.Count },
                { "analyticsTracked", true }
            };

            await Task.Delay(150);
            return true;
        });
    }

    private void DefineIntegrationPhase()
    {
        var integrationStep = _orchestrator.AddStep(
            SdlcPhase.Testing,
            "Integration Testing",
            "Test analytics feature with existing functionality",
            new List<string> { _orchestrator.GetExecution().Steps[2].Id }
        );

        _orchestrator.RegisterStepHandler(integrationStep.Id, async (step) =>
        {
            Console.WriteLine($"\n[BROWNFIELD-INTEGRATION] {step.Title}");
            Console.WriteLine("  → Running integration tests...");

            if (_urlService == null)
                return false;

            // Test 1: Existing URL still works
            var url = _urlService.ShortenUrl("https://example.com/integration-test");
            var retrieved = _urlService.GetUrl(url.ShortCode);
            if (retrieved == null)
            {
                step.Error = "Existing functionality broken";
                return false;
            }

            // Test 2: Analytics works with new accesses
            _urlService.GetRedirectUrl(url.ShortCode, "Safari", "10.0.0.1", "https://twitter.com");
            var analytics = _urlService.GetAnalytics(url.ShortCode);
            if (analytics == null || analytics.TotalAccesses != 1)
            {
                step.Error = "Analytics tracking broken";
                return false;
            }

            // Test 3: Referrer tracking
            if (analytics.TopReferrers.Count == 0)
            {
                step.Error = "Referrer tracking not working";
                return false;
            }

            Console.WriteLine("  ✓ Integration tests passed:");
            Console.WriteLine("    - Existing functionality: PASS");
            Console.WriteLine("    - New analytics tracking: PASS");
            Console.WriteLine("    - Referrer tracking: PASS");

            step.Output = new Dictionary<string, object?>
            {
                { "integrationTestsRun", 3 },
                { "integrationTestsPassed", 3 },
                { "backwardCompatibility", "verified" }
            };

            await Task.Delay(200);
            return true;
        });
    }

    private void DefineValidationPhase()
    {
        var validationStep = _orchestrator.AddStep(
            SdlcPhase.Validation,
            "Validate Enhancement",
            "Ensure analytics meets requirements and no data is lost",
            new List<string> { _orchestrator.GetExecution().Steps[3].Id }
        );

        _orchestrator.RegisterStepHandler(validationStep.Id, async (step) =>
        {
            Console.WriteLine($"\n[BROWNFIELD-VALIDATION] {step.Title}");
            Console.WriteLine("  → Validating enhancement requirements...");

            if (_urlService == null)
                return false;

            var allUrls = _urlService.ListUrls();
            var urlsWithAnalytics = 0;

            foreach (var url in allUrls)
            {
                var analytics = _urlService.GetAnalytics(url.ShortCode);
                if (analytics != null)
                {
                    urlsWithAnalytics++;
                }
            }

            Console.WriteLine($"  ✓ Validation checks:");
            Console.WriteLine($"    - Total URLs preserved: {allUrls.Count}");
            Console.WriteLine($"    - URLs with analytics: {urlsWithAnalytics}");
            Console.WriteLine($"    - Data integrity: OK");

            step.Output = new Dictionary<string, object?>
            {
                { "dataIntegrity", "verified" },
                { "requirementsMet", true },
                { "performanceImpact", "low" }
            };

            await Task.Delay(100);
            return true;
        });
    }

    private void DefineReleasePhase()
    {
        var releaseStep = _orchestrator.AddStep(
            SdlcPhase.Release,
            "Release Enhancement",
            "Mark analytics enhancement as ready for production",
            new List<string> { _orchestrator.GetExecution().Steps[4].Id }
        );

        _orchestrator.RegisterStepHandler(releaseStep.Id, async (step) =>
        {
            Console.WriteLine($"\n[BROWNFIELD-RELEASE] {step.Title}");
            Console.WriteLine("  → Finalizing brownfield release...");

            Console.WriteLine("  ✓ Release checklist:");
            Console.WriteLine("    - Migration plan: Additive only");
            Console.WriteLine("    - Rollback plan: N/A (no breaking changes)");
            Console.WriteLine("    - Deployment strategy: Blue-green deployment");
            Console.WriteLine("    - Monitoring: Analytics query performance tracked");

            step.Output = new Dictionary<string, object?>
            {
                { "releaseStatus", "approved" },
                { "productionReady", true },
                { "deploymentStrategy", "blue-green" }
            };

            await Task.Delay(200);
            return true;
        });
    }
}
