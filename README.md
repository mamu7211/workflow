# Workflow Engine

A DAG-based (Directed Acyclic Graph) workflow engine built with .NET 10.0 and [.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/). Workflows are defined as directed acyclic graphs where nodes (activities) are connected by edges (connections), supporting parallel branches, conditional paths, and long-running workflows with suspend/resume.

Built from scratch -- no Elsa Core, Workflow Core, or other workflow libraries.

## Table of Contents

- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Build & Run](#build--run)
  - [Running Tests](#running-tests)
- [Architecture](#architecture)
  - [Solution Structure](#solution-structure)
  - [System Overview](#system-overview)
  - [Infrastructure](#infrastructure)
- [Core Concepts](#core-concepts)
  - [Workflow Definition](#workflow-definition)
  - [DAG Execution Model](#dag-execution-model)
  - [Activities](#activities)
  - [Expressions](#expressions)
  - [Suspend & Resume](#suspend--resume)
- [Usage Guide](#usage-guide)
  - [Defining a Workflow](#defining-a-workflow)
  - [Executing a Workflow](#executing-a-workflow)
  - [Creating Custom Activities](#creating-custom-activities)
  - [Serialization](#serialization)
- [Activity Types](#activity-types)
- [API Reference](#api-reference)
  - [WorkflowExecutionEngine](#workflowexecutionengine)
  - [DagValidator](#dagvalidator)
  - [DagTraverser](#dagtraverser)
  - [ActivityRegistry](#activityregistry)
  - [IExpressionEvaluator](#iexpressionevaluator)
  - [IWorkflowInstanceStore](#iworkflowinstancestore)
- [Development](#development)
  - [Project Conventions](#project-conventions)
  - [Branching Strategy](#branching-strategy)
  - [Roadmap](#roadmap)

## Getting Started

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker](https://www.docker.com/) (required for Aspire infrastructure: PostgreSQL, RabbitMQ, MailHog)

### Build & Run

```bash
# Clone the repository
git clone <repository-url>
cd workflow

# Build the entire solution
dotnet build Workflow.sln

# Start the full stack via Aspire (PostgreSQL, RabbitMQ, MailHog, API, Frontend)
dotnet run --project Workflow.AppHost
```

The Aspire dashboard will open automatically, providing access to all services and their logs.

### Running Tests

```bash
# Run all tests
dotnet test

# Run only the engine unit tests (no Docker required)
dotnet test Workflow.Engine.Tests

# Run with detailed output
dotnet test --verbosity normal
```

> **Note:** Integration tests in `Workflow.Tests` require Docker to be running, as they spin up the full Aspire infrastructure.

## Architecture

### Solution Structure

```
Workflow.sln
├── Workflow.Engine                 Core library: DAG model, execution engine, expressions
├── Workflow.Engine.Activities      Built-in activity implementations
├── Workflow.Engine.Tests           Unit tests for engine + activities (MSTest)
├── Workflow.ApiService             REST API for workflow management (ASP.NET Core)
├── Workflow.Web                    Blazor frontend with visual node editor
├── Workflow.Web.Tests              Component tests for Blazor frontend (bUnit)
├── Workflow.AppHost                Aspire orchestration host
├── Workflow.ServiceDefaults        Shared config (OpenTelemetry, health checks, resilience)
├── Workflow.Tests                  Aspire integration tests
├── bruno/                          API testing collection (Bruno)
└── docs/                           Design documents and phase specifications
```

| Project | Type | Dependencies |
|---------|------|-------------|
| `Workflow.Engine` | Class Library | None (pure C#) |
| `Workflow.Engine.Activities` | Class Library | Engine |
| `Workflow.Engine.Tests` | MSTest 4.0.1 | Engine, Activities |
| `Workflow.ApiService` | ASP.NET Core Web | Engine, Activities, ServiceDefaults |
| `Workflow.Web` | Blazor WASM | Engine |
| `Workflow.AppHost` | Aspire Host | ApiService, Web |

### System Overview

```
┌──────────────────────────────────────────────────────────────────┐
│                      Blazor WASM Frontend                        │
│   ┌──────────────┐  ┌──────────────┐  ┌──────────────────────┐  │
│   │ Node-Editor  │  │  Monitoring  │  │   User Task Inbox    │  │
│   └──────┬───────┘  └──────┬───────┘  └──────────┬───────────┘  │
└──────────┼─────────────────┼─────────────────────┼──────────────┘
           │                 │                     │
           ▼                 ▼                     ▼
┌──────────────────────────────────────────────────────────────────┐
│                       REST API (ApiService)                      │
│                                                                  │
│   ┌──────────────────────────────────────────────────────────┐   │
│   │              Workflow Execution Engine                    │   │
│   │   ┌────────────┐  ┌────────────┐  ┌──────────────────┐  │   │
│   │   │ DAG Valid. │  │ DAG Trav.  │  │ Expression Eval. │  │   │
│   │   └────────────┘  └────────────┘  └──────────────────┘  │   │
│   └──────────────────────────────────────────────────────────┘   │
│   ┌──────────────────────────────────────────────────────────┐   │
│   │                   Activity Registry                      │   │
│   │   Log │ Email │ HTTP │ Delay │ UserTask │ RabbitMQ │ ... │   │
│   └──────────────────────────────────────────────────────────┘   │
└───────────┬──────────────────┬──────────────────┬────────────────┘
            │                  │                  │
            ▼                  ▼                  ▼
     ┌──────────────┐  ┌──────────────┐  ┌──────────────┐
     │  PostgreSQL   │  │   RabbitMQ   │  │   MailHog    │
     └──────────────┘  └──────────────┘  └──────────────┘
```

### Infrastructure

All infrastructure is managed by .NET Aspire and runs in Docker containers.

| Service | Purpose | Access |
|---------|---------|--------|
| PostgreSQL | Workflow database | Via Aspire service discovery |
| PgAdmin | Database management UI | Auto-configured by Aspire |
| RabbitMQ | Message queue for async activities | Management UI via Aspire |
| MailHog | SMTP testing | Web UI on port 8025, SMTP on port 1025 |

## Core Concepts

### Workflow Definition

A workflow is a directed acyclic graph defined as a JSON document. It contains:

- **Activities** -- the nodes of the graph, each with a type, properties, and visual position
- **Connections** -- directed edges between activities, optionally with conditions
- **Variables** -- shared state accessible to all activities during execution

```json
{
  "id": "approval-workflow",
  "name": "Approval Process",
  "version": 1,
  "activities": [
    {
      "id": "start",
      "type": "Log",
      "displayName": "Start",
      "properties": { "message": "Workflow started" },
      "x": 100, "y": 200
    },
    {
      "id": "approval",
      "type": "UserTask",
      "displayName": "Approval",
      "properties": { "title": "Review request" },
      "x": 300, "y": 200
    },
    {
      "id": "notify",
      "type": "SendEmail",
      "displayName": "Notify",
      "properties": {
        "to": "${applicantEmail}",
        "subject": "Request approved"
      },
      "x": 500, "y": 200
    }
  ],
  "connections": [
    { "sourceActivityId": "start", "targetActivityId": "approval" },
    {
      "sourceActivityId": "approval",
      "targetActivityId": "notify",
      "condition": "${result} == \"approved\""
    }
  ],
  "variables": {
    "applicantEmail": "user@example.com",
    "result": null
  }
}
```

### DAG Execution Model

The engine validates and executes workflows using the following algorithm:

1. **Validate** the graph using Kahn's algorithm (cycle detection, reachability checks)
2. **Identify start nodes** -- activities with no incoming connections
3. **Execution loop:**
   - Determine all currently executable activities (predecessors completed, conditions met)
   - Execute all executable activities **in parallel** (`Task.WhenAll`)
   - Process results:
     - **Completed** -- continue to next activities
     - **Suspended** -- persist state and pause workflow
     - **Faulted** -- mark workflow as failed
   - Repeat until all activities are completed or workflow is suspended/faulted

#### Parallel Branches

When a node has multiple outgoing connections, downstream activities execute in parallel:

```
        ┌──── B ────┐
A ──────┤            ├──── D
        └──── C ────┘
```

After A completes, B and C start simultaneously. D starts only after both B and C are completed.

#### Conditional Paths

Connections can have conditions evaluated by the expression engine. Only connections with fulfilled (or empty) conditions are followed:

```
                ┌──── Approve ──── (condition: ${result} == "yes")
Decision ───────┤
                └──── Reject  ──── (condition: ${result} == "no")
```

### Activities

Every activity inherits from `ActivityBase` and implements `ExecuteAsync`:

```csharp
public abstract class ActivityBase
{
    public abstract string Type { get; }
    public abstract Task<ActivityResult> ExecuteAsync(
        ActivityContext context, CancellationToken cancellationToken = default);
}
```

Activities receive an `ActivityContext` providing access to:

- **Properties** -- configuration from the workflow definition (typed via `GetProperty<T>`)
- **Variables** -- shared workflow state (read via `GetVariable<T>`, write via `SetVariable`)
- **ServiceProvider** -- dependency injection container for external services

Activities return one of three result types:

| Result | Method | Effect |
|--------|--------|--------|
| Completed | `ActivityResult.Completed(output?)` | Activity finished, output stored, continue |
| Faulted | `ActivityResult.Faulted(error)` | Activity failed, workflow marked as faulted |
| Suspended | `ActivityResult.SuspendExecution(reason)` | Workflow paused, state persisted |

### Expressions

The expression evaluator supports a lightweight syntax for variable substitution and conditions:

| Syntax | Example | Description |
|--------|---------|-------------|
| Variable | `${variableName}` | Substitutes variable value |
| Nested | `${outputs.result.code}` | Dot-notation for nested values |
| Equals | `${status} == "approved"` | String equality |
| Not equals | `${status} != "rejected"` | String inequality |
| Greater than | `${count} > 5` | Numeric comparison |
| Less than | `${count} < 10` | Numeric comparison |
| Greater/equal | `${count} >= 5` | Numeric comparison |
| Less/equal | `${count} <= 10` | Numeric comparison |
| Truthy | `${isActive}` | Boolean truthiness check |

### Suspend & Resume

Long-running activities (UserTask, Delay, WebhookTrigger, RabbitMQ) can suspend the workflow. The full workflow state is persisted and execution resumes when the external event completes:

| Activity | Resume Trigger |
|----------|---------------|
| Delay | Background service detects expired timer |
| UserTask | Manual API call (`POST /api/tasks/{id}/complete`) |
| WebhookTrigger | Incoming HTTP request (`POST /api/webhooks/{correlationId}`) |
| RabbitMqSubscribe | Background consumer receives message |

## Usage Guide

### Defining a Workflow

```csharp
using Workflow.Engine.Models;

var definition = new WorkflowDefinition
{
    Name = "My Workflow",
    Activities =
    [
        new ActivityNode { Id = "step1", Type = "Log", Properties = new() { ["message"] = "Hello" } },
        new ActivityNode { Id = "step2", Type = "Log", Properties = new() { ["message"] = "${greeting}" } }
    ],
    Connections =
    [
        new Connection { SourceActivityId = "step1", TargetActivityId = "step2" }
    ],
    Variables = new() { ["greeting"] = "World" }
};
```

### Executing a Workflow

```csharp
using Workflow.Engine.Activities;
using Workflow.Engine.Execution;
using Workflow.Engine.Expressions;

// Set up the activity registry
var registry = new ActivityRegistry();
registry.Register<LogActivity>();
// ... register more activities

// Create the engine
var engine = new WorkflowExecutionEngine(
    registry,
    new SimpleExpressionEvaluator(),
    serviceProvider);

// Start a workflow
var instance = await engine.StartAsync(definition);

// Check status
Console.WriteLine(instance.Status); // Completed, Suspended, Faulted

// Resume a suspended workflow
if (instance.Status == WorkflowStatus.Suspended)
{
    instance = await engine.ResumeAsync(
        definition,
        instance,
        resumeActivityId: "userTask1",
        resumeData: new() { ["approved"] = true });
}
```

### Creating Custom Activities

```csharp
using Workflow.Engine.Activities;

public class NotifySlackActivity : ActivityBase
{
    public override string Type => "NotifySlack";

    public override async Task<ActivityResult> ExecuteAsync(
        ActivityContext context, CancellationToken cancellationToken = default)
    {
        var channel = context.GetProperty<string>("channel");
        var message = context.GetProperty<string>("message");

        if (string.IsNullOrEmpty(channel))
            return ActivityResult.Faulted("Channel is required");

        // Access services via DI
        var httpClient = context.ServiceProvider.GetRequiredService<IHttpClientFactory>()
            .CreateClient("slack");

        // ... send notification

        return ActivityResult.Completed(new()
        {
            ["sent"] = true,
            ["timestamp"] = DateTime.UtcNow
        });
    }
}

// Register the custom activity
registry.Register<NotifySlackActivity>();
```

### Serialization

Workflow definitions and instances serialize to/from JSON with camelCase naming and string enums:

```csharp
using Workflow.Engine.Serialization;

// Serialize
string json = WorkflowJsonConverter.Serialize(definition);

// Deserialize
WorkflowDefinition? loaded = WorkflowJsonConverter.DeserializeDefinition(json);

// Also works for instances
string instanceJson = WorkflowJsonConverter.Serialize(instance);
WorkflowInstance? loadedInstance = WorkflowJsonConverter.DeserializeInstance(instanceJson);
```

## Activity Types

| Type | Category | Suspends? | Description |
|------|----------|-----------|-------------|
| `Log` | Utility | No | Log a message |
| `SetVariable` | Utility | No | Set a workflow variable |
| `Delay` | Control Flow | Yes | Wait for a time duration |
| `SendEmail` | Integration | No | Send email via SMTP |
| `HttpRequest` | Integration | No | Execute HTTP request |
| `UserTask` | Human | Yes | Wait for manual approval |
| `RabbitMqPublish` | Messaging | No | Publish message to queue |
| `RabbitMqSubscribe` | Messaging | Yes | Wait for queue message |
| `DatabaseQuery` | Data | No | Execute SQL query |
| `ScriptExecution` | Scripting | No | Run C# script (Roslyn) |
| `WebhookTrigger` | Integration | Yes | Wait for HTTP callback |

## API Reference

### WorkflowExecutionEngine

The main entry point for workflow execution.

```csharp
public sealed class WorkflowExecutionEngine
{
    // Start a new workflow instance
    Task<WorkflowInstance> StartAsync(
        WorkflowDefinition definition,
        Dictionary<string, object?>? inputVariables = null,
        CancellationToken cancellationToken = default);

    // Resume a suspended workflow
    Task<WorkflowInstance> ResumeAsync(
        WorkflowDefinition definition,
        WorkflowInstance instance,
        string? resumeActivityId = null,
        Dictionary<string, object?>? resumeData = null,
        CancellationToken cancellationToken = default);
}
```

**StartAsync** validates the DAG, creates a `WorkflowInstance`, merges input variables with definition defaults, and enters the execution loop.

**ResumeAsync** marks the suspended activity as completed, merges resume data into variables, and re-enters the execution loop.

### DagValidator

Static validator using Kahn's algorithm for topological sorting.

```csharp
public static class DagValidator
{
    static DagValidationResult Validate(WorkflowDefinition definition);
}

public sealed class DagValidationResult
{
    bool IsValid { get; }
    List<string> Errors { get; }            // Validation errors
    List<string> TopologicalOrder { get; }   // Sorted activity IDs
    List<string> StartActivityIds { get; }   // Activities with no predecessors
}
```

**Validations performed:**
1. Referential integrity -- connections reference existing activity IDs
2. Start nodes -- at least one activity with no incoming connections
3. Cycle detection -- Kahn's algorithm detects if not all nodes are reachable
4. Reachability -- all activities must be reachable from a start node

### DagTraverser

Determines which activities can execute at any point during workflow execution.

```csharp
public static class DagTraverser
{
    static Task<List<string>> GetExecutableActivitiesAsync(
        WorkflowDefinition definition,
        WorkflowInstance instance,
        IExpressionEvaluator expressionEvaluator);
}
```

An activity is executable when:
- Its status is `Pending`
- All predecessor activities are `Completed`
- At least one incoming connection has a fulfilled condition (OR semantics)

### ActivityRegistry

Registry for activity type resolution. Supports parameterless constructors and factory-based registration.

```csharp
public sealed class ActivityRegistry
{
    void Register<T>() where T : ActivityBase, new();
    void Register<T>(Func<IServiceProvider, T> factory) where T : ActivityBase, new();
    void Register(string type, Func<IServiceProvider, ActivityBase> factory);
    ActivityBase Resolve(string type, IServiceProvider serviceProvider);
    IReadOnlyList<string> GetRegisteredTypes();
}
```

Type resolution is **case-insensitive** -- registering `"Log"` matches resolving `"log"`.

### IExpressionEvaluator

Interface for expression evaluation. The default implementation is `SimpleExpressionEvaluator`.

```csharp
public interface IExpressionEvaluator
{
    Task<object?> EvaluateAsync(string expression, Dictionary<string, object?> variables);
    Task<bool> EvaluateConditionAsync(string condition, Dictionary<string, object?> variables);
}
```

### IWorkflowInstanceStore

Persistence interface for workflow instances. Implemented by `EfWorkflowInstanceStore` in the API service.

```csharp
public interface IWorkflowInstanceStore
{
    Task SaveAsync(WorkflowInstance instance, CancellationToken ct = default);
    Task<WorkflowInstance?> GetAsync(string instanceId, CancellationToken ct = default);
    Task<IReadOnlyList<WorkflowInstance>> GetByStatusAsync(WorkflowStatus status, CancellationToken ct = default);
}
```

## Development

### Project Conventions

| Convention | Value |
|-----------|-------|
| Framework | .NET 10.0 |
| Nullable reference types | Enabled |
| Implicit usings | Enabled |
| Serialization | System.Text.Json (no Newtonsoft) |
| Database | PostgreSQL via EF Core (Code-First) |
| Async/Await | All I/O operations |
| Test framework | MSTest 4.0.1 |
| Frontend tests | bUnit |
| API testing | Bruno (file-based collection in `bruno/`) |
| API docs | OpenAPI at `/openapi/v1.json` |

### Branching Strategy

Each implementation phase is developed in its own feature branch and merged into `master` upon completion:

```
master
├── feature/01-engine-core
├── feature/02-activities
├── feature/03-api-service
├── feature/04-frontend-designer
└── feature/05-infrastructure
```

### Roadmap

| Phase | Description | Status |
|-------|-------------|--------|
| 0 | Project structure, documentation, Aspire infrastructure | Done |
| 1 | Engine core: DAG model, execution engine, expression evaluator | Done |
| 2 | Built-in activities (11 types) | Done |
| 3 | REST API, EF Core persistence, background services | Done |
| 4 | Blazor WASM frontend with visual node editor | Done |
| 5 | Integration tests, demo workflows, end-to-end verification | Planned |

Detailed specifications for each phase are in [`docs/specs/`](docs/specs/). Implementation progress is tracked in [`docs/specs/progress.md`](docs/specs/progress.md).
