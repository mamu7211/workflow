# Spezifikation 02: Built-in Activities

## Überblick

Das `Workflow.Engine.Activities` Projekt implementiert alle 11 Activity-Typen.
Es referenziert `Workflow.Engine` und nutzt externe NuGet-Pakete für Email, RabbitMQ,
Datenbank und Scripting.

## Projekt-Setup

- **Projekt:** `Workflow.Engine.Activities`
- **Framework:** `net10.0`
- **Referenz:** `Workflow.Engine`
- **NuGet-Pakete:**
  - `MailKit` (SMTP)
  - `RabbitMQ.Client` (Messaging)
  - `Npgsql` (PostgreSQL)
  - `Microsoft.CodeAnalysis.CSharp.Scripting` (C# Scripts)

## Activity-Implementierungen

### 1. LogActivity

| Eigenschaft | Wert |
|-------------|------|
| Type | `Log` |
| Suspend | Nein |
| Properties | `message` (string), `level` (string: Info/Warning/Error) |
| Output | Keine |
| DI | `ILogger<LogActivity>` |

Schreibt eine Nachricht mit konfigurierbarem Level in den Logger.
`message` unterstützt Expression-Syntax: `${variableName}`.

### 2. SetVariableActivity

| Eigenschaft | Wert |
|-------------|------|
| Type | `SetVariable` |
| Suspend | Nein |
| Properties | `variableName` (string), `value` (object) |
| Output | Keine |

Setzt eine Variable im Workflow-Kontext. `value` kann eine Expression sein.

### 3. DelayActivity

| Eigenschaft | Wert |
|-------------|------|
| Type | `Delay` |
| Suspend | Ja |
| Properties | `duration` (string, TimeSpan-Format z.B. "00:05:00") |
| Output | `resumeAt` (DateTime) |

Gibt `ActivityResult.SuspendExecution` zurück mit der berechneten Resume-Zeit.
Ein Background-Service im ApiService prüft periodisch abgelaufene Timer.

**Kein `Thread.Sleep` oder `Task.Delay`** - der Workflow wird persistiert und später resumed.

### 4. SendEmailActivity

| Eigenschaft | Wert |
|-------------|------|
| Type | `SendEmail` |
| Suspend | Nein |
| Properties | `to` (string), `subject` (string), `body` (string), `isHtml` (bool, default: false) |
| Output | `messageId` (string) |
| DI | SMTP-Konfiguration (Host, Port) |

Sendet Email via MailKit/SMTP. Im Aspire-Setup zeigt der SMTP-Host auf MailHog.
Alle Properties unterstützen Expression-Syntax.

### 5. HttpRequestActivity

| Eigenschaft | Wert |
|-------------|------|
| Type | `HttpRequest` |
| Suspend | Nein |
| Properties | `url` (string), `method` (string: GET/POST/PUT/DELETE), `headers` (Dictionary), `body` (string?) |
| Output | `statusCode` (int), `responseBody` (string), `responseHeaders` (Dictionary) |
| DI | `IHttpClientFactory` |

Führt einen HTTP-Request aus. URL und Body unterstützen Expression-Syntax.

### 6. UserTaskActivity

| Eigenschaft | Wert |
|-------------|------|
| Type | `UserTask` |
| Suspend | Ja |
| Properties | `title` (string), `description` (string?), `assignedTo` (string?) |
| Output | `response` (Dictionary - Daten vom User beim Complete) |

Gibt `ActivityResult.SuspendExecution` zurück. Erstellt einen Eintrag in der
UserTasks-Tabelle. Wird manuell über die API resumed (POST /api/tasks/{id}/complete).

### 7. RabbitMqPublishActivity

| Eigenschaft | Wert |
|-------------|------|
| Type | `RabbitMqPublish` |
| Suspend | Nein |
| Properties | `exchange` (string), `routingKey` (string), `message` (string) |
| Output | Keine |
| DI | RabbitMQ `IConnection` |

Publiziert eine Nachricht auf den konfigurierten Exchange/RoutingKey.
Fire-and-Forget - kein Suspend.

### 8. RabbitMqSubscribeActivity

| Eigenschaft | Wert |
|-------------|------|
| Type | `RabbitMqSubscribe` |
| Suspend | Ja |
| Properties | `queueName` (string), `timeout` (string?, TimeSpan-Format) |
| Output | `message` (string), `routingKey` (string) |
| DI | RabbitMQ `IConnection` |

Gibt `ActivityResult.SuspendExecution` zurück. Ein Background-Service im ApiService
überwacht die Queue und resumed den Workflow bei eingehender Nachricht.

### 9. DatabaseQueryActivity

| Eigenschaft | Wert |
|-------------|------|
| Type | `DatabaseQuery` |
| Suspend | Nein |
| Properties | `connectionString` (string), `query` (string), `parameters` (Dictionary?) |
| Output | `rows` (List<Dictionary>), `rowCount` (int) |
| DI | Keine (erstellt eigene Npgsql-Connection) |

Führt eine parametrisierte SQL-Abfrage aus. **Nur SELECT erlaubt** (Sicherheit).
Query unterstützt Expression-Syntax für Parameter.

### 10. ScriptExecutionActivity

| Eigenschaft | Wert |
|-------------|------|
| Type | `ScriptExecution` |
| Suspend | Nein |
| Properties | `script` (string), `timeout` (int, Sekunden, default: 5) |
| Output | `result` (object?) |

Führt ein C#-Script via Roslyn Scripting aus. Das Script hat Zugriff auf
Workflow-Variablen über ein `Variables`-Dictionary.

**Sicherheitsmaßnahmen:**
- Timeout (default 5 Sekunden)
- Eingeschränkte Referenzen (kein System.IO, kein System.Net)
- CancellationToken für Abbruch

### 11. WebhookTriggerActivity

| Eigenschaft | Wert |
|-------------|------|
| Type | `WebhookTrigger` |
| Suspend | Ja |
| Properties | `path` (string?, auto-generiert wenn leer), `expectedMethod` (string: POST/PUT, default: POST) |
| Output | `requestBody` (string), `requestHeaders` (Dictionary), `method` (string) |

Gibt `ActivityResult.SuspendExecution` zurück mit einer generierten Correlation-ID.
Ein dedizierter Endpoint im ApiService (POST /api/webhooks/{correlationId}) empfängt
den externen Aufruf und resumed den Workflow.

## Registrierung

```csharp
namespace Workflow.Engine.Activities;

public static class ServiceCollectionExtensions
{
    public static ActivityRegistry AddBuiltInActivities(this ActivityRegistry registry)
    {
        registry.Register<LogActivity>();
        registry.Register<SetVariableActivity>();
        registry.Register<DelayActivity>();
        registry.Register<SendEmailActivity>();
        registry.Register<HttpRequestActivity>();
        registry.Register<UserTaskActivity>();
        registry.Register<RabbitMqPublishActivity>();
        registry.Register<RabbitMqSubscribeActivity>();
        registry.Register<DatabaseQueryActivity>();
        registry.Register<ScriptExecutionActivity>();
        registry.Register<WebhookTriggerActivity>();
        return registry;
    }
}
```

## Dateistruktur

```
Workflow.Engine.Activities/
├── Workflow.Engine.Activities.csproj
├── LogActivity.cs
├── SetVariableActivity.cs
├── DelayActivity.cs
├── SendEmailActivity.cs
├── HttpRequestActivity.cs
├── UserTaskActivity.cs
├── RabbitMqPublishActivity.cs
├── RabbitMqSubscribeActivity.cs
├── DatabaseQueryActivity.cs
├── ScriptExecutionActivity.cs
├── WebhookTriggerActivity.cs
└── ServiceCollectionExtensions.cs
```

## Tests (MSTest)

Alle Activities werden mit Mocks getestet (kein Infrastruktur-Zugriff in Unit Tests).

| Testklasse | Mock/Setup | Szenarien |
|-----------|------------|-----------|
| `LogActivityTests` | Mock ILogger | Korrekte Message + Level |
| `SetVariableActivityTests` | - | Variable wird gesetzt |
| `DelayActivityTests` | - | Suspend mit korrekter ResumeAt |
| `SendEmailActivityTests` | Mock SmtpClient | Korrekte To/Subject/Body, Expression-Auflösung |
| `HttpRequestActivityTests` | MockHttpMessageHandler | GET/POST, Headers, StatusCode in Output |
| `UserTaskActivityTests` | - | Suspend, Title/Description korrekt |
| `RabbitMqPublishActivityTests` | Mock IConnection | Message auf korrektem Exchange |
| `RabbitMqSubscribeActivityTests` | - | Suspend mit QueueName |
| `DatabaseQueryActivityTests` | Mock NpgsqlConnection | Parametrisierte Query, Ergebnis-Mapping |
| `ScriptExecutionActivityTests` | - | Einfaches Script, Timeout-Test |
| `WebhookTriggerActivityTests` | - | Suspend mit generierter CorrelationId |

## Akzeptanzkriterien

- [ ] Alle 11 Activity-Typen implementiert und registrierbar
- [ ] Expression-Substitution in Properties funktioniert
- [ ] Suspend-Activities geben korrekt SuspendExecution zurück
- [ ] ScriptExecution respektiert Timeout
- [ ] DatabaseQuery blockiert nicht-SELECT Queries
- [ ] Alle Unit Tests grün
