# Workflow Engine - Architektur-Design

## 1. Überblick

Die Workflow Engine implementiert ein DAG-basiertes (Directed Acyclic Graph) Ausführungsmodell.
Workflows werden als gerichtete azyklische Graphen definiert, in denen Knoten (Activities)
durch Kanten (Connections) verbunden sind. Das System unterstützt parallele Branches,
bedingte Pfade und langlebige Workflows mit Suspend/Resume-Mechanismus.

```
┌─────────────────────────────────────────────────────────────────┐
│                        Blazor WASM Frontend                     │
│  ┌──────────────┐  ┌──────────────┐  ┌───────────────────────┐ │
│  │ Node-Editor  │  │  Monitoring  │  │    User Task Inbox    │ │
│  └──────┬───────┘  └──────┬───────┘  └───────────┬───────────┘ │
└─────────┼─────────────────┼──────────────────────┼─────────────┘
          │                 │                      │
          ▼                 ▼                      ▼
┌─────────────────────────────────────────────────────────────────┐
│                        REST API (ApiService)                     │
│  ┌──────────────┐  ┌──────────────┐  ┌───────────────────────┐ │
│  │  Workflow API │  │ Instance API │  │    Task/Webhook API   │ │
│  └──────┬───────┘  └──────┬───────┘  └───────────┬───────────┘ │
│         │                 │                      │              │
│  ┌──────┴─────────────────┴──────────────────────┴───────────┐ │
│  │              Workflow Execution Engine                      │ │
│  │  ┌────────────┐  ┌────────────┐  ┌──────────────────────┐ │ │
│  │  │ DAG Valid. │  │ DAG Trav.  │  │  Expression Eval.    │ │ │
│  │  └────────────┘  └────────────┘  └──────────────────────┘ │ │
│  └────────────────────────────────────────────────────────────┘ │
│         │                                                       │
│  ┌──────┴─────────────────────────────────────────────────────┐ │
│  │                   Activity Registry                         │ │
│  │  SendEmail │ HttpRequest │ Delay │ UserTask │ RabbitMQ │...│ │
│  └────────────────────────────────────────────────────────────┘ │
│                                                                  │
│  ┌──────────────┐  ┌──────────────┐                             │
│  │ Delay Resume │  │ RabbitMQ     │  (Background Services)      │
│  │ Service      │  │ Listener     │                             │
│  └──────────────┘  └──────────────┘                             │
└─────────┬──────────────────┬──────────────────┬─────────────────┘
          │                  │                  │
          ▼                  ▼                  ▼
   ┌──────────────┐  ┌──────────────┐  ┌──────────────┐
   │  PostgreSQL   │  │   RabbitMQ   │  │   MailHog    │
   │  (Workflow DB)│  │  (Messaging) │  │   (SMTP)     │
   └──────────────┘  └──────────────┘  └──────────────┘
```

## 2. Workflow-Modell (DAG)

### 2.1 WorkflowDefinition

Eine WorkflowDefinition beschreibt den vollständigen Graphen als JSON-Dokument:

```json
{
  "id": "wf-001",
  "name": "Genehmigungsprozess",
  "version": 1,
  "activities": [
    { "id": "start", "type": "Log", "displayName": "Start", "properties": { "message": "Workflow gestartet" }, "x": 100, "y": 200 },
    { "id": "approval", "type": "UserTask", "displayName": "Genehmigung", "properties": { "title": "Antrag prüfen" }, "x": 300, "y": 200 },
    { "id": "send-email", "type": "SendEmail", "displayName": "Bestätigung", "properties": { "to": "${applicantEmail}", "subject": "Genehmigt" }, "x": 500, "y": 100 },
    { "id": "reject-log", "type": "Log", "displayName": "Abgelehnt", "properties": { "message": "Antrag abgelehnt" }, "x": 500, "y": 300 }
  ],
  "connections": [
    { "sourceActivityId": "start", "targetActivityId": "approval" },
    { "sourceActivityId": "approval", "targetActivityId": "send-email", "condition": "${approval_result} == \"approved\"" },
    { "sourceActivityId": "approval", "targetActivityId": "reject-log", "condition": "${approval_result} == \"rejected\"" }
  ],
  "variables": {
    "applicantEmail": "user@example.com",
    "approval_result": null
  }
}
```

### 2.2 DAG-Ausführung

```
1. Graph validieren (Zyklen-Erkennung via Kahn's Algorithmus)
2. Start-Knoten ermitteln (Knoten ohne eingehende Kanten)
3. LOOP:
   a. Ausführbare Knoten bestimmen (alle Vorgänger completed + Bedingungen geprüft)
   b. Alle ausführbaren Knoten parallel ausführen (Task.WhenAll)
   c. Ergebnisse verarbeiten:
      - Completed → nächste Knoten prüfen
      - Suspended → Workflow pausieren, State persistieren
      - Faulted → Workflow als fehlerhaft markieren
   d. Wenn alle Knoten completed → Workflow abgeschlossen
   e. Wenn keine Knoten ausführbar aber nicht alle completed → Suspended oder Fehler
```

### 2.3 Parallele Branches

```
        ┌──── B ────┐
A ──────┤            ├──── D
        └──── C ────┘
```

Wenn A abgeschlossen ist, werden B und C gleichzeitig gestartet.
D wird erst gestartet, wenn sowohl B als auch C abgeschlossen sind.

### 2.4 Bedingte Pfade

Connections können eine `condition` Property haben. Die Bedingung wird über den
Expression Evaluator ausgewertet. Nur Connections mit erfüllter (oder leerer) Bedingung
werden verfolgt.

## 3. Activity-System

### 3.1 Abstraktion

Jede Activity erbt von `ActivityBase` und implementiert `ExecuteAsync`:

```csharp
public abstract class ActivityBase
{
    public abstract string Type { get; }
    public abstract Task<ActivityResult> ExecuteAsync(ActivityContext context, CancellationToken ct);
}
```

### 3.2 Ergebnis-Typen

| Ergebnis | Bedeutung |
|----------|-----------|
| `ActivityResult.Completed(output)` | Erfolgreich abgeschlossen, optionale Output-Daten |
| `ActivityResult.Faulted(error)` | Fehler aufgetreten |
| `ActivityResult.SuspendExecution(reason)` | Workflow pausieren (UserTask, Delay, Webhook) |

### 3.3 Suspend/Resume-Mechanismus

Activities wie UserTask, Delay, WebhookTrigger und RabbitMqSubscribe können den
Workflow suspendieren. Der gesamte Workflow-State wird persistiert. Das Resumieren
erfolgt durch:

- **Delay:** Background-Service prüft periodisch abgelaufene Timer
- **UserTask:** Manueller API-Aufruf (POST /api/tasks/{id}/complete)
- **WebhookTrigger:** Eingehender HTTP-Request (POST /api/webhooks/{correlationId})
- **RabbitMqSubscribe:** Background-Consumer empfängt Nachricht

### 3.4 Verfügbare Activity-Typen

| Type | Kategorie | Suspend? | Beschreibung |
|------|-----------|----------|--------------|
| `Log` | Utility | Nein | Nachricht loggen |
| `SetVariable` | Utility | Nein | Workflow-Variable setzen |
| `Delay` | Control Flow | Ja | Warten für Zeitspanne |
| `SendEmail` | Integration | Nein | Email via SMTP senden |
| `HttpRequest` | Integration | Nein | HTTP-Anfrage ausführen |
| `UserTask` | Human | Ja | Manuelle Genehmigung |
| `RabbitMqPublish` | Messaging | Nein | Nachricht publizieren |
| `RabbitMqSubscribe` | Messaging | Ja | Auf Nachricht warten |
| `DatabaseQuery` | Data | Nein | SQL-Abfrage ausführen |
| `ScriptExecution` | Scripting | Nein | C#-Script ausführen |
| `WebhookTrigger` | Integration | Ja | Auf HTTP-Aufruf warten |

## 4. Persistierung

### 4.1 Datenbankschema (PostgreSQL via EF Core)

```
┌─────────────────────────┐     ┌─────────────────────────────┐
│ WorkflowDefinitions     │     │ WorkflowInstances           │
├─────────────────────────┤     ├─────────────────────────────┤
│ Id (PK)                 │────<│ WorkflowDefinitionId (FK)   │
│ Name                    │     │ Id (PK)                     │
│ Description             │     │ WorkflowVersion             │
│ Version                 │     │ Status                      │
│ DefinitionJson          │     │ StateJson                   │
│ IsPublished             │     │ CreatedAt                   │
│ CreatedAt               │     │ CompletedAt                 │
│ UpdatedAt               │     │ Error                       │
└─────────────────────────┘     └──────────────┬──────────────┘
                                               │
                                ┌──────────────┴──────────────┐
                                │                             │
                    ┌───────────┴───────────┐  ┌──────────────┴──────────┐
                    │ ActivityExecutionLogs  │  │ UserTasks               │
                    ├───────────────────────┤  ├─────────────────────────┤
                    │ Id (PK, auto)         │  │ Id (PK)                 │
                    │ WorkflowInstanceId(FK)│  │ WorkflowInstanceId (FK) │
                    │ ActivityId            │  │ ActivityId              │
                    │ ActivityType          │  │ Title                   │
                    │ Status                │  │ Description             │
                    │ InputJson             │  │ AssignedTo              │
                    │ OutputJson            │  │ Status                  │
                    │ Error                 │  │ ResponseJson            │
                    │ StartedAt             │  │ CreatedAt               │
                    │ CompletedAt           │  │ CompletedAt             │
                    └───────────────────────┘  └─────────────────────────┘
```

### 4.2 JSON-Speicherung

WorkflowDefinition und WorkflowInstance werden als JSON in einer Spalte gespeichert.
Vorteile: Flexibel, keine Migration bei Schema-Änderungen des Workflow-Modells.
PostgreSQL unterstützt JSONB für effiziente Abfragen.

## 5. Expression Evaluator

Einfacher Evaluator für den Spike:

- **Variable Substitution:** `${variableName}` wird durch den Wert ersetzt
- **Bedingungen:** `${status} == "approved"`, `${count} > 5`
- **Verschachtelt:** `${outputs.httpResult.statusCode} == 200`

Kein Roslyn Scripting im Expression Evaluator - dafür gibt es die ScriptExecution Activity.

## 6. Frontend (Blazor WASM)

### 6.1 Node-Editor

Visueller Graph-Editor basierend auf JSON-Definitionen:

```
┌──────────────┬────────────────────────────────┬──────────────────┐
│  Activity    │                                │  Property Panel  │
│  Palette     │       Workflow Canvas           │                  │
│              │                                │  [Activity-Typ]  │
│  ○ Log       │    ┌───┐     ┌───┐            │                  │
│  ○ Email     │    │ A │────>│ B │            │  Name: [____]    │
│  ○ HTTP      │    └───┘     └─┬─┘            │  Prop1: [____]   │
│  ○ Delay     │                │              │  Prop2: [____]   │
│  ○ UserTask  │              ┌─┴─┐            │                  │
│  ○ RabbitMQ  │              │ C │            │  [Save]          │
│  ○ DB Query  │              └───┘            │                  │
│  ○ Script    │                                │                  │
│  ○ Webhook   │                                │                  │
└──────────────┴────────────────────────────────┴──────────────────┘
```

### 6.2 Workflow

1. Activity aus Palette auf Canvas ziehen (Drag & Drop)
2. Node auf Canvas platziert
3. Nodes verbinden: Output-Port → Input-Port ziehen
4. Node anklicken → Property Panel zeigt formularbasierte Eingaben
5. Properties bearbeiten (je nach Activity-Typ spezifische Felder)
6. JSON-Definition wird live aktualisiert
7. Speichern → POST/PUT an API

## 7. Infrastruktur (Aspire)

### 7.1 Orchestrierung

```csharp
// AppHost.cs
var postgres = builder.AddPostgres("postgres").WithPgAdmin().AddDatabase("workflowdb");
var rabbitmq = builder.AddRabbitMQ("messaging").WithManagementPlugin();
var mailhog  = builder.AddContainer("mailhog", "mailhog/mailhog", "latest")
                      .WithHttpEndpoint(8025, 8025, "ui")
                      .WithEndpoint(1025, 1025, "smtp");

var api = builder.AddProject<Projects.Workflow_ApiService>("apiservice")
                 .WithReference(postgres).WithReference(rabbitmq)
                 .WaitFor(postgres).WaitFor(rabbitmq);

builder.AddProject<Projects.Workflow_Web>("webfrontend")
       .WithExternalHttpEndpoints().WithReference(api).WaitFor(api);
```

### 7.2 Service Discovery

Aspire verwaltet automatisch die Verbindungs-Strings für PostgreSQL und RabbitMQ.
Im ApiService werden diese via `builder.AddNpgsqlDbContext<WorkflowDbContext>("workflowdb")`
und `builder.AddRabbitMQClient("messaging")` aufgelöst.
