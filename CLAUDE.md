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

## Branching & Workflow

### Regeln

- **Hauptbranch:** `master`
- **Branch-Namenskonvention:** `feature/<spec-name>` (z.B. `feature/01-engine-core`)
- **Feature-Branches IMMER von `master` erstellen:** `git checkout master && git checkout -b feature/<spec-name>`
- **Merge-Strategie:** `git merge feature/<spec-name> --no-ff` (immer Merge-Commit)
- **Nie direkt auf `master` committen** - immer Feature-Branch nutzen
- **Session-Start:** `git branch` prüfen, in den passenden Feature-Branch wechseln

### Workflow pro Phase

1. `git checkout master && git checkout -b feature/<spec-name>`
2. Implementierung gemäß Spec
3. Tests grün: `dotnet test`
4. `progress.md` aktualisieren
5. Commit: `Phase X: Kurzbeschreibung`
6. `git checkout master && git merge feature/<spec-name> --no-ff -m "Merge feature/<spec-name>: Beschreibung"`

### Commit-Message-Konvention

- **Implementation:** `Phase X: Kurzbeschreibung` (z.B. `Phase 3: API Service - REST API, EF Core persistence`)
- **Merge:** `Merge feature/<spec-name>: Beschreibung`

### Phase-Branch-Zuordnung

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
