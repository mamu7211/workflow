# Spezifikation 01: Engine Core

## Überblick

Das `Workflow.Engine` Projekt ist eine pure C# Class Library ohne ASP.NET-Abhängigkeiten.
Es enthält das DAG-Modell, die Execution Engine, den Expression Evaluator und die
Activity-Abstraktion.

## Projekt-Setup

- **Projekt:** `Workflow.Engine`
- **SDK:** `Microsoft.NET.Sdk`
- **Framework:** `net10.0`
- **NuGet:** `System.Text.Json` (für Serialisierung)
- **Keine** ASP.NET-Abhängigkeit

## Datenmodell

### WorkflowDefinition

```csharp
namespace Workflow.Engine.Models;

public sealed class WorkflowDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Version { get; set; } = 1;
    public List<ActivityNode> Activities { get; set; } = [];
    public List<Connection> Connections { get; set; } = [];
    public Dictionary<string, object?> Variables { get; set; } = [];
}
```

### ActivityNode

```csharp
public sealed class ActivityNode
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public Dictionary<string, object?> Properties { get; set; } = [];
    public double X { get; set; }  // Visuelle Position für Frontend
    public double Y { get; set; }
}
```

### Connection

```csharp
public sealed class Connection
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SourceActivityId { get; set; } = string.Empty;
    public string TargetActivityId { get; set; } = string.Empty;
    public string? Condition { get; set; }  // Optional: Expression
}
```

### WorkflowInstance

```csharp
public sealed class WorkflowInstance
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string WorkflowDefinitionId { get; set; } = string.Empty;
    public int WorkflowVersion { get; set; }
    public WorkflowStatus Status { get; set; } = WorkflowStatus.Created;
    public Dictionary<string, object?> Variables { get; set; } = [];
    public Dictionary<string, ActivityState> ActivityStates { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string? Error { get; set; }
}
```

### Enums

```csharp
public enum WorkflowStatus
{
    Created, Running, Suspended, Completed, Faulted, Cancelled
}

public enum ActivityExecutionStatus
{
    Pending, Running, Completed, Faulted, Skipped, Suspended
}
```

### ActivityState

```csharp
public sealed class ActivityState
{
    public string ActivityId { get; set; } = string.Empty;
    public ActivityExecutionStatus Status { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Dictionary<string, object?> Output { get; set; } = [];
    public string? Error { get; set; }
}
```

## Activity-Abstraktion

### ActivityBase

```csharp
namespace Workflow.Engine.Activities;

public abstract class ActivityBase
{
    public abstract string Type { get; }
    public abstract Task<ActivityResult> ExecuteAsync(
        ActivityContext context,
        CancellationToken cancellationToken = default);
}
```

### ActivityContext

Stellt Properties, Variablen und ServiceProvider bereit:

- `ActivityId`: ID der ausgeführten Activity
- `Properties`: Konfiguration aus der WorkflowDefinition
- `Variables`: Aktuelle Workflow-Variablen (read/write)
- `ServiceProvider`: DI-Container für externe Abhängigkeiten
- `GetProperty<T>(name)`: Typisierter Zugriff auf Properties
- `GetVariable<T>(name)`: Typisierter Zugriff auf Variablen
- `SetVariable(name, value)`: Variable setzen

### ActivityResult

Factory Methods:

- `ActivityResult.Completed(output?)`: Erfolgreich
- `ActivityResult.Faulted(error)`: Fehler
- `ActivityResult.SuspendExecution(reason)`: Workflow pausieren

### ActivityRegistry

- `Register<T>()`: Activity-Typ registrieren (parameterloser Konstruktor)
- `Register<T>(Func<IServiceProvider, T>)`: Activity mit Factory registrieren
- `Resolve(type, serviceProvider)`: Activity-Instanz erzeugen
- `GetRegisteredTypes()`: Alle registrierten Typen auflisten

## DAG-Validierung (DagValidator)

Algorithmus: Kahn's Algorithmus für topologische Sortierung.

**Validierungen:**
1. Zyklen-Erkennung: Graph darf keine Zyklen enthalten
2. Start-Knoten: Mindestens ein Knoten ohne eingehende Kanten
3. Erreichbarkeit: Alle Knoten müssen von einem Start-Knoten erreichbar sein
4. Referenzielle Integrität: Connections verweisen auf existierende Activity-IDs

**Rückgabe:** `DagValidationResult` mit `IsValid`, `Errors`, `TopologicalOrder`, `StartActivityIds`

## DAG-Traversierung (DagTraverser)

**Methode:** `GetExecutableActivities(definition, instance, expressionEvaluator)`

**Logik:**
1. Alle Activities deren Vorgänger `Completed` sind
2. Die Activity selbst ist `Pending`
3. Alle Connections zum Knoten haben erfüllte Bedingungen (oder keine Bedingung)
4. Mindestens eine eingehende Connection ist erfüllt (OR-Semantik für bedingte Pfade)

**Parallele Branches:** Wenn mehrere Activities gleichzeitig ausführbar sind (z.B. nach einem Fork), werden alle zurückgegeben.

## Expression Evaluator

### Interface

```csharp
namespace Workflow.Engine.Expressions;

public interface IExpressionEvaluator
{
    Task<object?> EvaluateAsync(string expression, Dictionary<string, object?> variables);
    Task<bool> EvaluateConditionAsync(string condition, Dictionary<string, object?> variables);
}
```

### SimpleExpressionEvaluator

Unterstützte Syntax:
- Variable: `${variableName}` → Wert aus Variables Dictionary
- Vergleich (==): `${status} == "approved"`
- Vergleich (!=): `${status} != "rejected"`
- Numerisch (>, <, >=, <=): `${count} > 5`
- Boolean: `${isActive}` (truthy check)

**Kein Roslyn** im Core - bewusst leichtgewichtig für den Spike.

## Execution Engine

### WorkflowExecutionEngine

```csharp
namespace Workflow.Engine.Execution;

public sealed class WorkflowExecutionEngine
{
    public async Task<WorkflowInstance> StartAsync(
        WorkflowDefinition definition,
        Dictionary<string, object?>? inputVariables = null,
        CancellationToken cancellationToken = default);

    public async Task<WorkflowInstance> ResumeAsync(
        WorkflowDefinition definition,
        WorkflowInstance instance,
        string? resumeActivityId = null,
        Dictionary<string, object?>? resumeData = null,
        CancellationToken cancellationToken = default);
}
```

### Algorithmus

```
StartAsync:
  1. WorkflowInstance erstellen (Status = Running)
  2. Input-Variablen mit Definition-Variablen mergen
  3. DAG validieren
  4. ExecuteLoop aufrufen

ResumeAsync:
  1. Suspended Activity auf Completed setzen
  2. Resume-Daten in Variablen übernehmen
  3. Status = Running
  4. ExecuteLoop aufrufen

ExecuteLoop:
  1. executables = DagTraverser.GetExecutableActivities()
  2. Wenn leer UND alle completed → Status = Completed, RETURN
  3. Wenn leer UND nicht alle completed → Status = Faulted, RETURN
  4. Für jede executable Activity parallel:
     a. Activity aus Registry auflösen
     b. ActivityContext erstellen
     c. ExecuteAsync aufrufen
     d. Ergebnis verarbeiten:
        - Completed: ActivityState = Completed, Output speichern
        - Faulted: ActivityState = Faulted, Workflow = Faulted, RETURN
        - Suspend: ActivityState = Suspended, Workflow = Suspended, RETURN
  5. GOTO 1
```

### IWorkflowInstanceStore

```csharp
namespace Workflow.Engine.Execution;

public interface IWorkflowInstanceStore
{
    Task SaveAsync(WorkflowInstance instance, CancellationToken ct = default);
    Task<WorkflowInstance?> GetAsync(string instanceId, CancellationToken ct = default);
    Task<IReadOnlyList<WorkflowInstance>> GetByStatusAsync(WorkflowStatus status, CancellationToken ct = default);
}
```

Implementation erfolgt in Phase 3 (EF Core). Für Tests wird ein InMemory-Store verwendet.

## Dateistruktur

```
Workflow.Engine/
├── Workflow.Engine.csproj
├── Models/
│   ├── WorkflowDefinition.cs
│   └── WorkflowInstance.cs
├── Activities/
│   ├── ActivityBase.cs
│   ├── ActivityContext.cs
│   ├── ActivityResult.cs
│   └── ActivityRegistry.cs
├── Graph/
│   ├── DagValidator.cs
│   └── DagTraverser.cs
├── Execution/
│   ├── WorkflowExecutionEngine.cs
│   └── IWorkflowInstanceStore.cs
├── Expressions/
│   ├── IExpressionEvaluator.cs
│   └── SimpleExpressionEvaluator.cs
└── Serialization/
    └── WorkflowJsonConverter.cs
```

## Tests (MSTest)

| Testklasse | Szenarien |
|-----------|-----------|
| `DagValidatorTests` | Gültiger DAG, Zyklus erkannt, fehlender Start-Knoten, ungültige Referenz |
| `DagTraverserTests` | Linear, parallele Branches, bedingte Pfade, Join-Punkt |
| `SimpleExpressionEvaluatorTests` | Variable Substitution, String-Vergleich, numerischer Vergleich, Boolean |
| `WorkflowExecutionEngineTests` | Linear A→B→C, Parallel A→[B,C]→D, Bedingt, Suspend/Resume, Faulted |
| `ActivityRegistryTests` | Register, Resolve, unbekannter Typ → Exception |
| `WorkflowSerializationTests` | JSON Round-Trip für WorkflowDefinition |

## Akzeptanzkriterien

- [ ] WorkflowDefinition lässt sich als JSON serialisieren/deserialisieren
- [ ] DAG-Validierung erkennt Zyklen zuverlässig
- [ ] Lineare Workflows werden korrekt ausgeführt
- [ ] Parallele Branches werden gleichzeitig ausgeführt
- [ ] Bedingte Pfade werden korrekt evaluiert
- [ ] Suspend/Resume funktioniert für einzelne Activities
- [ ] Alle Unit Tests sind grün
