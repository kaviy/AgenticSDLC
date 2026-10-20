# Agentic SDLC Orchestration Platform

A .NET 7 reference implementation of an **agentic software-development-lifecycle (SDLC) orchestrator**. It models the lifecycle (Requirement → Design → Implementation → Testing → Validation → Release) as a governed workflow with dependency ordering, approval gates, retries, rollback, safe-stop controls, and a full audit trail.

A **URL Shortener** service is used as the sample product the orchestrator "builds", and a console app runs three end-to-end scenarios against it: greenfield, brownfield, and ambiguous/conflicting requirements.

---

## Table of Contents

- [Key Features](#key-features)
- [Solution Structure](#solution-structure)
- [Architecture](#architecture)
- [Getting Started](#getting-started)
- [Running the Scenarios](#running-the-scenarios)
- [Running the Tests](#running-the-tests)
- [Workflow Output](#workflow-output)
- [URL Shortener Service](#url-shortener-service)
- [Extending the Platform](#extending-the-platform)
- [Known Limitations](#known-limitations)

---

## Key Features

| Capability | Description |
|---|---|
| **Phase-based workflow** | Six SDLC phases: `Requirement`, `Design`, `Implementation`, `Testing`, `Validation`, `Release`. |
| **Step dependencies** | A step runs only when all of its dependency steps are `Completed`. |
| **Approval gates** | Steps can require named approvers; the workflow pauses until every required approver approves. Any rejection blocks the step. |
| **DAG stage engine** | `WorkflowEngine` topologically sorts pluggable `IStage` implementations by their declared dependencies. |
| **Entry / exit gates** | Preconditions and postconditions validated before and after each stage. |
| **Safety controls** | Retry with exponential backoff, rollback of prior stages on failure, and safe-stop on critical error patterns (`CRITICAL`, `FATAL`, `SECURITY`, `BREACH`, `DATA_LOSS`). |
| **Audit trail** | Every step, gate, retry, rollback, approval, and context change is recorded for traceability. |
| **Metrics** | Success rate, step counts, retries, rollbacks, and durations. |
| **Persistence** | Workflow executions are saved as indented JSON files for later inspection. |

---

## Solution Structure

```
AgenticAI/
├── AgenticSDLC.sln
├── OrchestratorCore/            # Orchestration library
│   ├── Abstractions/            #   IStage, IEntryGate, IExitGate
│   ├── Engine/                  #   SdlcOrchestrator (step/approval-based workflow)
│   ├── Models/                  #   WorkflowExecution, WorkflowStep, ExecutionContext, StageResult, ...
│   ├── Persistence/             #   WorkflowExecutionRepository (JSON files)
│   ├── Stages/                  #   BaseStage, CommonGates, SDLC stage implementations
│   ├── WorkflowEngine.cs        #   DAG stage engine with gates, retry, rollback
│   ├── SafetyControls.cs        #   Retry / rollback / fallback / safe-stop policies + metrics
│   └── AuditTrail.cs            #   Audit-grade event log
├── UrlShortenerService/         # Sample product built by the workflow
│   ├── Domain/                  #   ShortenedUrl, AccessLog, UrlAnalytics
│   ├── Data/ · Repository/      #   In-memory repositories
│   ├── Services/ · Service/     #   Shortening, analytics, health-check services
│   └── API/                     #   UrlShortenerApi (request/response handlers)
├── ScenarioExecutor/            # Console app that runs the demo scenarios
│   └── Scenarios/               #   Greenfield, Brownfield, Ambiguous
├── OrchestratorCore.Tests/      # xUnit tests for the orchestrator
└── UrlShortenerService.Tests/   # xUnit tests for the URL shortener
```

**Project references**

```
ScenarioExecutor ──► OrchestratorCore
        └──────────► UrlShortenerService
OrchestratorCore.Tests ──► OrchestratorCore
UrlShortenerService.Tests ──► UrlShortenerService
```

---

## Architecture

The library offers two complementary ways to drive a workflow.

### 1. `SdlcOrchestrator` — step-based workflow

Used by the demo scenarios. You add steps to phases, register an async handler per step, and execute.

```csharp
var orchestrator = new SdlcOrchestrator("Build URL Shortener", "Greenfield");

var req = orchestrator.AddStep(
    SdlcPhase.Requirement,
    "Analyze and Normalize Requirements",
    "Parse and clarify the requirement");

var design = orchestrator.AddStep(
    SdlcPhase.Design,
    "Design Architecture",
    "Define components and APIs",
    dependencies: new() { req.Id },
    requiredApprovals: new() { "architect" });

orchestrator.RegisterStepHandler(req.Id, async step =>
{
    orchestrator.SetContext("requirement", "Build a URL shortener");
    return true; // return false to fail the step
});

orchestrator.RecordApproval(design.Id, "architect", ApprovalStatus.Approved);

var result = await orchestrator.ExecuteWorkflowAsync();
```

Execution walks the phases in order. For a step with required approvals the workflow pauses (`WorkflowStatus.Paused`) until all approvers approve. A failed step marks the workflow `Failed`. `RollbackAsync(stepId)` marks all later steps as `RolledBack`.

### 2. `WorkflowEngine` — DAG stage engine

Registers `IStage` implementations and runs them in dependency order.

```
RequirementAnalysis → ArchitectureDesign → Implementation → Testing → Documentation → ReleaseReadiness
```

For each stage the engine:

1. Validates **entry gates** (a failing entry gate skips the stage).
2. Executes the stage with **retry + exponential backoff**.
3. On failure: checks **critical-error patterns** (safe-stop), otherwise **rolls back prior stages** in reverse order.
4. Validates **exit gates**.
5. Records everything in the **audit trail** and **metrics**.

```csharp
var engine = new WorkflowEngine();             // default SafetyControls + AuditTrail
engine.RegisterStage(new RequirementAnalysisStage());
engine.RegisterStage(new ArchitectureDesignStage());
// ...
var context = new ExecutionContext { Requirement = "Build a URL shortener" };
await engine.ExecuteAsync(context);

Console.WriteLine(engine.Metrics);
```

### Safety defaults

| Policy | Default |
|---|---|
| Retry | 3 attempts, 100 ms initial delay, ×2 backoff, 10 s max delay |
| Rollback | Enabled; roll back prior stages on failure |
| Safe-stop | Stop on critical error patterns (`CRITICAL`, `FATAL`, `SECURITY`, `BREACH`, `DATA_LOSS`) |

All defaults are configurable by passing a customised `SafetyControls` instance to `WorkflowEngine`.

---

## Getting Started

### Prerequisites

- [.NET 7 SDK](https://dotnet.microsoft.com/download/dotnet/7.0)
- Visual Studio 2022+ or any editor with .NET support (optional)

### Clone and build

```bash
git clone https://github.com/kaviy/NewRepo.git
cd NewRepo        # or the folder containing AgenticSDLC.sln
dotnet restore
dotnet build
```

---

## Running the Scenarios

`ScenarioExecutor` runs one or all demo scenarios. Pass the scenario name as the first argument; with no argument, all three run.

```bash
dotnet run --project ScenarioExecutor                 # all scenarios
dotnet run --project ScenarioExecutor -- greenfield
dotnet run --project ScenarioExecutor -- brownfield
dotnet run --project ScenarioExecutor -- ambiguous
```

| Scenario | Goal | What it demonstrates |
|---|---|---|
| **Greenfield** | Build the URL shortener from scratch | Requirement analysis, design decisions, implementation, testing, validation, and release checklist. |
| **Brownfield** | Add analytics to an existing shortener | Impact analysis, additive (non-breaking) integration, integration tests, blue-green release planning. |
| **Ambiguous** | "Ultra-fast **and** full analytics" under a storage/compute budget | Conflict detection, problem decomposition, trade-off analysis (async + sampling chosen), and a validation matrix with an explicit accepted trade-off. |

---

## Running the Tests

```bash
dotnet test
```

- **OrchestratorCore.Tests** covers step creation, context storage, dependency checks, step status transitions, approval tracking/rejection, and full workflow completion.
- **UrlShortenerService.Tests** covers URL shortening, invalid-URL rejection, lookup (hit and miss), redirect with access recording, daily-access analytics, and listing.

Tests use xUnit 2.4.2.

---

## Workflow Output

Each scenario run persists its `WorkflowExecution` as `<execution-id>.json` in a `workflows/` folder next to the executable (e.g. `ScenarioExecutor/bin/Debug/net7.0/workflows/`). Each file contains:

- Steps with phase, status, dependencies, outputs, and timestamps
- Approval gates and decisions
- The execution context (requirements, architecture, results)
- A complete audit log
- Metrics (success rate, completed/failed steps, retries, rollbacks, duration)

Use `WorkflowExecutionRepository` to save, load, list, or delete executions programmatically.

---

## URL Shortener Service

The sample product exposes the following operations through `UrlShortenerApi` (a lightweight handler layer returning `(StatusCode, JsonResponse)`; it is not wired to an HTTP host):

| Operation | Route (intended) | Description |
|---|---|---|
| `Shorten` | `POST /api/shorten` | Create a short code for a URL (optional custom length and expiry). |
| `Resolve` | `GET /api/{shortCode}` | Return the original URL and record a hit. |
| `GetAnalytics` | `GET /api/analytics/{shortCode}` | Per-URL access totals, daily breakdown, top referrers. |
| `Health` | `GET /api/health` | Uptime, request counts, success rate. |
| `GetStats` | `GET /api/stats` | Aggregate URL, access, and health statistics. |

Short codes are Base62 (`a–z`, `A–Z`, `0–9`), 6 characters by default, with collision retry. URLs without a scheme are normalised to `https://`.

---

## Extending the Platform

**Add a new stage**

1. Derive from `BaseStage` (in `OrchestratorCore/Stages`).
2. Set `Name`, `Description`, and `Dependencies`.
3. Implement `ExecuteAsync` (and `RollbackAsync` if the stage has side effects).
4. Attach entry/exit gates (e.g. `CommonGates.StageSuccessGate`, `RequiredStageOutputsExistGate`).
5. Register it with `WorkflowEngine.RegisterStage(...)`.

**Add a new scenario**

1. Create a class in `ScenarioExecutor/Scenarios` that builds a `SdlcOrchestrator`, adds steps, and registers handlers.
2. Add it to the `args` switch in `Program.cs`.

**Add a gate or policy**

Implement `IEntryGate` / `IExitGate`, or adjust `SafetyControls` (retry, rollback, fallback, safe-stop patterns).

---

## Known Limitations

- **Simulated agents.** Scenario step handlers and the built-in stages simulate work and print results; they do not call an LLM or generate real code.
- **In-memory storage.** The URL shortener repositories keep data in memory and lose it on restart.
- **No HTTP host.** `UrlShortenerApi` is a handler class, not an ASP.NET endpoint.
- **Duplicate implementations.** `UrlShortenerService` contains two repository variants (`Data/` and `Repository/`) and two service variants (`Services/` and `Service/`); consolidating them would simplify the codebase.
- **Two `AuditEntry` types.** One lives in `OrchestratorCore.Models` (used by `SdlcOrchestrator`) and another in `OrchestratorCore` (used by `AuditTrail`); consider unifying them.
- **Fallback policy** (`FallbackPolicy`) is defined but not yet applied by the engine.



