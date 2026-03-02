# Spezifikation 03: API Service und Persistierung

## Überblick

Der `Workflow.ApiService` stellt eine REST API für Workflow-Management bereit,
persistiert Daten in PostgreSQL via EF Core und betreibt Background Services
für Timer- und Message-basiertes Resumieren.

## Projekt-Setup

- **Projekt:** `Workflow.ApiService` (bestehendes Projekt erweitern)
- **Framework:** `net10.0`
- **Referenzen:** `Workflow.Engine`, `Workflow.Engine.Activities`, `Workflow.ServiceDefaults`
- **Neue NuGet-Pakete:**
  - `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL` (Aspire-integriertes EF Core)
  - `Aspire.RabbitMQ.Client` (Aspire-integriertes RabbitMQ)
  - `Microsoft.EntityFrameworkCore.Design` (für Migrations)

## Datenbank (EF Core / PostgreSQL)

### DbContext

```csharp
namespace Workflow.ApiService.Data;

public class WorkflowDbContext : DbContext
{
    public DbSet<WorkflowDefinitionEntity> WorkflowDefinitions => Set<WorkflowDefinitionEntity>();
    public DbSet<WorkflowInstanceEntity> WorkflowInstances => Set<WorkflowInstanceEntity>();
    public DbSet<ActivityExecutionLogEntity> ActivityExecutionLogs => Set<ActivityExecutionLogEntity>();
    public DbSet<UserTaskEntity> UserTasks => Set<UserTaskEntity>();
}
```

### Entities

#### WorkflowDefinitionEntity

| Spalte | Typ | Beschreibung |
|--------|-----|-------------|
| Id | string (PK) | GUID |
| Name | string | Workflow-Name |
| Description | string? | Beschreibung |
| Version | int | Versionsnummer |
| DefinitionJson | string (JSONB) | Gesamte WorkflowDefinition als JSON |
| IsPublished | bool | Veröffentlicht? |
| CreatedAt | DateTime | Erstellzeitpunkt |
| UpdatedAt | DateTime | Letztes Update |

#### WorkflowInstanceEntity

| Spalte | Typ | Beschreibung |
|--------|-----|-------------|
| Id | string (PK) | GUID |
| WorkflowDefinitionId | string (FK) | Referenz auf Definition |
| WorkflowVersion | int | Version bei Start |
| Status | string | WorkflowStatus als String |
| StateJson | string (JSONB) | Serialisierter WorkflowInstance |
| CreatedAt | DateTime | Start-Zeitpunkt |
| CompletedAt | DateTime? | Abschluss-Zeitpunkt |
| Error | string? | Fehlermeldung |

#### ActivityExecutionLogEntity

| Spalte | Typ | Beschreibung |
|--------|-----|-------------|
| Id | long (PK, auto) | Auto-Increment |
| WorkflowInstanceId | string (FK) | Referenz auf Instance |
| ActivityId | string | Activity-ID im Graph |
| ActivityType | string | z.B. "SendEmail" |
| Status | string | Execution-Status |
| InputJson | string? | Input-Properties als JSON |
| OutputJson | string? | Output als JSON |
| Error | string? | Fehlermeldung |
| StartedAt | DateTime | Startzeit |
| CompletedAt | DateTime? | Endzeit |

#### UserTaskEntity

| Spalte | Typ | Beschreibung |
|--------|-----|-------------|
| Id | string (PK) | GUID |
| WorkflowInstanceId | string (FK) | Referenz auf Instance |
| ActivityId | string | Activity-ID im Graph |
| Title | string | Aufgaben-Titel |
| Description | string? | Beschreibung |
| AssignedTo | string? | Zugewiesene Person |
| Status | string | Pending / Approved / Rejected |
| ResponseJson | string? | Antwort-Daten als JSON |
| CreatedAt | DateTime | Erstellzeitpunkt |
| CompletedAt | DateTime? | Abschluss-Zeitpunkt |

### IWorkflowInstanceStore Implementation

`EfWorkflowInstanceStore` implementiert das Interface aus `Workflow.Engine`:

- `SaveAsync`: Upsert (Insert oder Update basierend auf ID)
- `GetAsync`: Laden einer Instanz
- `GetByStatusAsync`: Alle Instanzen mit bestimmtem Status
- Serialisierung/Deserialisierung zwischen Engine-Modell und DB-Entity

## REST API Endpoints

### Workflow Definitions

| Method | Pfad | Beschreibung | Request Body | Response |
|--------|------|-------------|-------------|----------|
| GET | `/api/workflows` | Alle Definitionen | - | `WorkflowDefinitionDto[]` |
| GET | `/api/workflows/{id}` | Einzelne Definition | - | `WorkflowDefinitionDto` |
| POST | `/api/workflows` | Neue Definition | `CreateWorkflowDto` | `WorkflowDefinitionDto` (201) |
| PUT | `/api/workflows/{id}` | Definition aktualisieren | `UpdateWorkflowDto` | `WorkflowDefinitionDto` |
| DELETE | `/api/workflows/{id}` | Definition löschen | - | 204 No Content |
| POST | `/api/workflows/{id}/publish` | Veröffentlichen | - | `WorkflowDefinitionDto` |
| POST | `/api/workflows/{id}/start` | Instanz starten | `StartWorkflowDto?` | `WorkflowInstanceDto` (201) |

### Workflow Instances

| Method | Pfad | Beschreibung | Request Body | Response |
|--------|------|-------------|-------------|----------|
| GET | `/api/instances` | Alle Instanzen | - | `WorkflowInstanceDto[]` |
| GET | `/api/instances/{id}` | Instanz-Details | - | `WorkflowInstanceDto` |
| POST | `/api/instances/{id}/cancel` | Instanz abbrechen | - | `WorkflowInstanceDto` |
| POST | `/api/instances/{id}/resume` | Resumieren | `ResumeWorkflowDto?` | `WorkflowInstanceDto` |
| GET | `/api/instances/{id}/log` | Execution Log | - | `ActivityExecutionLogDto[]` |

### User Tasks

| Method | Pfad | Beschreibung | Request Body | Response |
|--------|------|-------------|-------------|----------|
| GET | `/api/tasks` | Offene Tasks | - | `UserTaskDto[]` |
| GET | `/api/tasks/{id}` | Task-Details | - | `UserTaskDto` |
| POST | `/api/tasks/{id}/complete` | Task abschließen | `CompleteTaskDto` | `UserTaskDto` |

### Webhooks

| Method | Pfad | Beschreibung | Request Body | Response |
|--------|------|-------------|-------------|----------|
| POST | `/api/webhooks/{correlationId}` | Webhook empfangen | Any JSON | 200 OK |

### Activity Metadata

| Method | Pfad | Beschreibung | Response |
|--------|------|-------------|----------|
| GET | `/api/activities/types` | Verfügbare Typen | `ActivityTypeDto[]` |

Alle Endpoints mit `.WithOpenApi()` annotiert. OpenAPI-Dokument unter `/openapi/v1.json`.

## Background Services

### DelayResumeService

- `BackgroundService` mit 10-Sekunden-Intervall
- Lädt alle suspendierten Instanzen mit abgelaufenem Delay-Timer
- Resumed den Workflow über die Execution Engine
- Logging bei jedem Resume

### RabbitMqListenerService

- `BackgroundService` der RabbitMQ-Queues überwacht
- Bei suspendiertem RabbitMqSubscribe: Consumer auf der konfigurierten Queue
- Eingehende Nachricht → Workflow resumed mit Message-Daten

## DI-Registrierung (Program.cs)

```csharp
// Engine
builder.Services.AddSingleton<ActivityRegistry>(sp =>
{
    var registry = new ActivityRegistry();
    registry.AddBuiltInActivities();
    return registry;
});
builder.Services.AddScoped<IExpressionEvaluator, SimpleExpressionEvaluator>();
builder.Services.AddScoped<WorkflowExecutionEngine>();
builder.Services.AddScoped<IWorkflowInstanceStore, EfWorkflowInstanceStore>();

// Datenbank
builder.AddNpgsqlDbContext<WorkflowDbContext>("workflowdb");

// RabbitMQ
builder.AddRabbitMQClient("messaging");

// Background Services
builder.Services.AddHostedService<DelayResumeService>();
builder.Services.AddHostedService<RabbitMqListenerService>();
```

## Dateistruktur

```
Workflow.ApiService/
├── Program.cs                          (überarbeitet)
├── Workflow.ApiService.csproj          (erweitert)
├── Data/
│   ├── WorkflowDbContext.cs
│   ├── EfWorkflowInstanceStore.cs
│   └── Entities/
│       ├── WorkflowDefinitionEntity.cs
│       ├── WorkflowInstanceEntity.cs
│       ├── ActivityExecutionLogEntity.cs
│       └── UserTaskEntity.cs
├── Endpoints/
│   ├── WorkflowEndpoints.cs
│   ├── InstanceEndpoints.cs
│   ├── UserTaskEndpoints.cs
│   ├── WebhookEndpoints.cs
│   └── ActivityMetadataEndpoints.cs
├── Services/
│   ├── DelayResumeService.cs
│   └── RabbitMqListenerService.cs
└── Dtos/
    ├── WorkflowDefinitionDto.cs
    ├── WorkflowInstanceDto.cs
    ├── UserTaskDto.cs
    ├── ActivityExecutionLogDto.cs
    └── ActivityTypeDto.cs
```

## Tests (MSTest)

| Testklasse | Ansatz | Szenarien |
|-----------|--------|-----------|
| `WorkflowEndpointsTests` | Mock Store | CRUD Operationen, Validierung |
| `InstanceEndpointsTests` | Mock Store + Engine | Start, Cancel, Resume |
| `UserTaskEndpointsTests` | Mock Store | Complete mit Daten |
| `EfWorkflowInstanceStoreTests` | In-Memory DB | Save, Get, GetByStatus |

## Akzeptanzkriterien

- [ ] EF Core Migrations laufen erfolgreich
- [ ] CRUD für Workflow-Definitionen funktioniert
- [ ] Workflow kann über API gestartet werden
- [ ] Instanz-Status ist über API abfragbar
- [ ] UserTask kann über API completed werden
- [ ] Webhook-Endpoint nimmt Requests entgegen
- [ ] OpenAPI-Dokument ist verfügbar unter `/openapi/v1.json`
- [ ] DelayResumeService resumed abgelaufene Timer
- [ ] Alle Tests grün
