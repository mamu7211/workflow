# Spezifikation 05: Infrastruktur und Integration

## Überblick

Konfiguration der Aspire-Infrastruktur (PostgreSQL, RabbitMQ, MailHog),
End-to-End Integration Tests, Demo-Workflows und Bruno API Collection.

## Aspire AppHost

### AppHost.cs

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// --- Infrastruktur ---

var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin()
    .AddDatabase("workflowdb");

var rabbitmq = builder.AddRabbitMQ("messaging")
    .WithManagementPlugin();

var mailhog = builder.AddContainer("mailhog", "mailhog/mailhog", "latest")
    .WithHttpEndpoint(port: 8025, targetPort: 8025, name: "mailhog-ui")
    .WithEndpoint(port: 1025, targetPort: 1025, name: "mailhog-smtp");

// --- Services ---

var apiService = builder.AddProject<Projects.Workflow_ApiService>("apiservice")
    .WithReference(postgres)
    .WithReference(rabbitmq)
    .WithEnvironment("Smtp__Host", mailhog.GetEndpoint("mailhog-smtp"))
    .WithHttpHealthCheck("/health")
    .WaitFor(postgres)
    .WaitFor(rabbitmq);

builder.AddProject<Projects.Workflow_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
```

### AppHost NuGet-Pakete

```xml
<PackageReference Include="Aspire.Hosting.PostgreSQL" Version="13.1.1" />
<PackageReference Include="Aspire.Hosting.RabbitMQ" Version="13.1.1" />
```

## Service-Konfiguration

### ApiService - PostgreSQL

```csharp
// In Program.cs
builder.AddNpgsqlDbContext<WorkflowDbContext>("workflowdb");
```

Aspire injiziert automatisch den Connection String über Service Discovery.

### ApiService - RabbitMQ

```csharp
// In Program.cs
builder.AddRabbitMQClient("messaging");
```

### ApiService - SMTP (MailHog)

Konfiguration über Umgebungsvariablen:

```csharp
// In appsettings.json
{
  "Smtp": {
    "Host": "localhost",
    "Port": 1025
  }
}
```

Aspire überschreibt Host/Port dynamisch.

## Demo-Workflows (Seed Data)

### 1. Genehmigungsprozess

```
Start (Log) → Genehmigung (UserTask)
  ├── [approved] → Bestätigung (SendEmail) → Ende (Log)
  └── [rejected] → Ablehnung (Log)
```

**Variables:** `applicantEmail`, `applicantName`, `approval_result`

### 2. HTTP-Datensammler

```
Webhook (WebhookTrigger) → API-Aufruf (HttpRequest) → Daten speichern (SetVariable)
  → DB-Eintrag (DatabaseQuery) → Protokoll (Log)
```

**Variables:** `webhookData`, `apiResponse`, `dbResult`

### 3. Timer-basierter Benachrichtigungsprozess

```
Start (Log) → Warten (Delay, 5min) → Email (SendEmail) → Nachricht (RabbitMqPublish)
```

**Variables:** `recipientEmail`, `notificationSubject`

### Seed-Daten Implementierung

```csharp
// Workflow.ApiService/Data/SeedData.cs
public static class SeedData
{
    public static async Task SeedAsync(WorkflowDbContext context)
    {
        if (await context.WorkflowDefinitions.AnyAsync()) return;

        context.WorkflowDefinitions.AddRange(
            CreateApprovalWorkflow(),
            CreateDataCollectorWorkflow(),
            CreateTimerNotificationWorkflow()
        );

        await context.SaveChangesAsync();
    }
}
```

## Bruno API Collection

### Verzeichnisstruktur

```
bruno/
├── bruno.json
├── environments/
│   └── local.bru
├── workflows/
│   ├── get-all-workflows.bru
│   ├── create-workflow.bru
│   ├── get-workflow.bru
│   ├── update-workflow.bru
│   ├── delete-workflow.bru
│   ├── publish-workflow.bru
│   └── start-workflow.bru
├── instances/
│   ├── get-all-instances.bru
│   ├── get-instance.bru
│   ├── cancel-instance.bru
│   ├── resume-instance.bru
│   └── get-instance-log.bru
├── tasks/
│   ├── get-all-tasks.bru
│   ├── get-task.bru
│   └── complete-task.bru
├── webhooks/
│   └── trigger-webhook.bru
└── activities/
    └── get-activity-types.bru
```

### bruno.json

```json
{
  "version": "1",
  "name": "Workflow Engine API",
  "type": "collection"
}
```

### environments/local.bru

```
vars {
  baseUrl: http://localhost:5452
  workflowId:
  instanceId:
  taskId:
  correlationId:
}
```

### Beispiel-Request (create-workflow.bru)

```
meta {
  name: Create Workflow
  type: http
  seq: 2
}

post {
  url: {{baseUrl}}/api/workflows
  body: json
  auth: none
}

body:json {
  {
    "name": "Test Workflow",
    "description": "Ein einfacher Test-Workflow",
    "activities": [
      {
        "id": "start",
        "type": "Log",
        "displayName": "Start",
        "properties": { "message": "Workflow gestartet", "level": "Info" },
        "x": 100,
        "y": 200
      },
      {
        "id": "end",
        "type": "Log",
        "displayName": "Ende",
        "properties": { "message": "Workflow beendet", "level": "Info" },
        "x": 400,
        "y": 200
      }
    ],
    "connections": [
      {
        "sourceActivityId": "start",
        "targetActivityId": "end"
      }
    ],
    "variables": {}
  }
}
```

## Integration Tests (MSTest)

### Erweiterte Workflow.Tests

```csharp
[TestClass]
public class WorkflowIntegrationTests
{
    [TestMethod]
    public async Task StartWorkflow_LinearFlow_CompletesSuccessfully() { }

    [TestMethod]
    public async Task StartWorkflow_WithUserTask_SuspendsAndResumes() { }

    [TestMethod]
    public async Task SendEmailActivity_DeliversToMailHog() { }

    [TestMethod]
    public async Task WebhookTrigger_ResumesOnIncomingRequest() { }

    [TestMethod]
    public async Task WorkflowDefinition_CRUD_WorksCorrectly() { }
}
```

### MailHog Verifikation

MailHog bietet eine REST-API für Test-Verifikation:

```csharp
// GET http://localhost:8025/api/v2/messages
var messages = await httpClient.GetFromJsonAsync<MailHogResponse>(
    "http://localhost:8025/api/v2/messages");
Assert.IsTrue(messages.Items.Any(m => m.Content.Headers.Subject.Contains("Genehmigt")));
```

## Externe Zugriffs-URLs (Entwicklung)

| Service | URL | Beschreibung |
|---------|-----|-------------|
| Aspire Dashboard | https://localhost:17059 | Orchestrierungs-Übersicht |
| API Service | http://localhost:5452 | REST API |
| Web Frontend | http://localhost:5034 | Blazor WASM App |
| PgAdmin | Dynamischer Port via Aspire | PostgreSQL Management |
| RabbitMQ Management | Dynamischer Port via Aspire | Queue Management |
| MailHog UI | http://localhost:8025 | Email-Inbox |

## Akzeptanzkriterien

- [ ] `dotnet run --project Workflow.AppHost` startet alle Services erfolgreich
- [ ] PostgreSQL-Datenbank wird automatisch erstellt
- [ ] RabbitMQ ist erreichbar und Management UI funktioniert
- [ ] MailHog empfängt Emails
- [ ] Demo-Workflows sind nach Start verfügbar
- [ ] Bruno Collection kann alle Endpoints erfolgreich aufrufen
- [ ] Integration Tests laufen grün
- [ ] Aspire Dashboard zeigt alle Services gesund
