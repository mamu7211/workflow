# Workflow Engine Spike

## Projektüberblick

DAG-basierte Workflow Engine als .NET 10.0 Aspire-Anwendung.
Selbstimplementiert (kein Elsa Core / Workflow Core).

## Architektur

| Projekt | Beschreibung |
|---------|-------------|
| `Workflow.Engine` | Pure C# Library - DAG-Modell, Execution Engine, Expression Evaluator |
| `Workflow.Engine.Activities` | Built-in Activity-Implementierungen (Email, HTTP, RabbitMQ, DB, etc.) |
| `Workflow.ApiService` | REST API für Workflow-Management, PostgreSQL via EF Core |
| `Workflow.Web` | Blazor WASM Frontend mit visuellem Node-Editor |
| `Workflow.AppHost` | Aspire Orchestrierung (PostgreSQL, RabbitMQ, MailHog) |
| `Workflow.ServiceDefaults` | Shared Service-Konfiguration (OpenTelemetry, Health Checks, Resilience) |
| `Workflow.Engine.Tests` | MSTest Unit Tests für Engine + Activities |
| `Workflow.Web.Tests` | bUnit Component Tests für Blazor Frontend |
| `Workflow.Tests` | MSTest Aspire Integration Tests |

## Build & Run

```bash
# Gesamte Solution bauen
dotnet build Workflow.sln

# Alle Tests ausführen
dotnet test

# Aspire starten (PostgreSQL, RabbitMQ, MailHog, API, Frontend)
dotnet run --project Workflow.AppHost
```

## Infrastruktur (via Aspire)

| Service | Zweck | Zugriff |
|---------|-------|---------|
| PostgreSQL | Workflow-Datenbank | Connection via Aspire Service Discovery |
| PgAdmin | DB-Management UI | Automatisch via Aspire |
| RabbitMQ | Message Queue für async Activities | Management UI via Aspire |
| MailHog | SMTP-Testing | UI auf Port 8025, SMTP auf Port 1025 |

## Branching

- Hauptbranch: `master`
- Jede Spec-Phase wird in einem eigenen Feature-Branch implementiert
- **Branch-Namenskonvention:** `feature/<spec-name>` (z.B. `feature/01-engine-core`, `feature/02-activities`)
- Nach Fertigstellung wird der Branch in `master` gemergt
- **WICHTIG:** Zu Beginn jeder Session den aktuellen Branch prüfen (`git branch`). Arbeite immer im passenden Feature-Branch für die jeweilige Phase. Wechsle ggf. mit `git checkout feature/<spec-name>` oder erstelle den Branch mit `git checkout -b feature/<spec-name>` von `master` aus.

| Phase | Spec | Branch |
|-------|------|--------|
| 1 | `docs/specs/01-engine-core.md` | `feature/01-engine-core` |
| 2 | `docs/specs/02-activities.md` | `feature/02-activities` |
| 3 | `docs/specs/03-api-service.md` | `feature/03-api-service` |
| 4 | `docs/specs/04-frontend-designer.md` | `feature/04-frontend-designer` |
| 5 | `docs/specs/05-infrastructure.md` | `feature/05-infrastructure` |

## Konventionen

- **Framework:** .NET 10.0 durchgängig
- **Nullable reference types:** aktiviert
- **Implicit usings:** aktiviert
- **Serialisierung:** System.Text.Json (kein Newtonsoft)
- **Datenbank:** PostgreSQL via EF Core (Code-First Migrations)
- **Async/Await:** durchgängig, alle I/O-Operationen async
- **Test-Framework:** MSTest (`[TestClass]` / `[TestMethod]`)
- **Frontend-Tests:** bUnit für Blazor Component Tests
- **API-Testing:** Bruno (dateibasierte Collection in `bruno/`)
- **API-Dokumentation:** OpenAPI via `/openapi/v1.json`

## Dokumentation

- `docs/design.md` - Architektur-Übersicht
- `docs/specs/` - Spezifikationen pro Entwicklungsphase
- `docs/specs/progress.md` - Implementierungsfortschritt
