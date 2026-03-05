# Implementierungsfortschritt

## Legende

| Symbol | Bedeutung     |
|--------|---------------|
| ⬜      | Offen         |
| 🚧     | In Arbeit     |
| ✅      | Abgeschlossen |
| ❌      | Blockiert     |

---

## Phase 0: Dokumentation und Projektstruktur

| Task                                                  | Status | Notizen                                     |
|-------------------------------------------------------|--------|---------------------------------------------|
| CLAUDE.md erstellen                                   | ✅      |                                             |
| docs/design.md erstellen                              | ✅      |                                             |
| docs/specs/01-engine-core.md                          | ✅      |                                             |
| docs/specs/02-activities.md                           | ✅      |                                             |
| docs/specs/03-api-service.md                          | ✅      |                                             |
| docs/specs/04-frontend-designer.md                    | ✅      |                                             |
| docs/specs/05-infrastructure.md                       | ✅      |                                             |
| docs/specs/progress.md                                | ✅      | Diese Datei                                 |
| Workflow.Engine Projekt anlegen                       | ✅      | net10.0 Class Library                       |
| Workflow.Engine.Activities Projekt anlegen            | ✅      | net10.0 Class Library, Ref → Engine         |
| Workflow.Engine.Tests Projekt anlegen (MSTest)        | ✅      | MSTest 4.0.1, Ref → Engine + Activities     |
| Workflow.Web.Tests Projekt anlegen (bUnit)            | ✅      | MSTest 4.0.1, Ref → Web                     |
| Workflow.Tests auf MSTest migrieren                   | ✅      | xUnit → MSTest 4.0.1                        |
| Aspire Host erweitern (PostgreSQL, RabbitMQ, MailHog) | ✅      | Aspire.Hosting.PostgreSQL + RabbitMQ 13.1.1 |
| Bruno API Projekt-Struktur                            | ✅      | 16 .bru Dateien in 5 Ordnern                |

## Phase 1: Engine Core

| Task                              | Status | Notizen                 |
|-----------------------------------|--------|-------------------------|
| WorkflowDefinition Modell         | ✅      | Spec: 01-engine-core.md |
| WorkflowInstance Modell           | ✅      |                         |
| ActivityBase Abstraktion          | ✅      |                         |
| ActivityContext                   | ✅      |                         |
| ActivityResult                    | ✅      |                         |
| ActivityRegistry                  | ✅      |                         |
| DagValidator (Kahn's Algorithmus) | ✅      |                         |
| DagTraverser                      | ✅      |                         |
| IExpressionEvaluator Interface    | ✅      |                         |
| SimpleExpressionEvaluator         | ✅      |                         |
| WorkflowExecutionEngine           | ✅      |                         |
| IWorkflowInstanceStore Interface  | ✅      |                         |
| WorkflowJsonConverter             | ✅      |                         |
| DagValidatorTests                 | ✅      |                         |
| DagTraverserTests                 | ✅      |                         |
| ExpressionEvaluatorTests          | ✅      |                         |
| ExecutionEngineTests              | ✅      |                         |
| ActivityRegistryTests             | ✅      |                         |
| SerializationTests                | ✅      |                         |

## Phase 2: Built-in Activities

| Task                        | Status | Notizen |
|-----------------------------|--------|---------|
| LogActivity                 | ✅      |         |
| SetVariableActivity         | ✅      |         |
| DelayActivity               | ✅      | Suspend |
| SendEmailActivity           | ✅      | MailKit |
| HttpRequestActivity         | ✅      |         |
| UserTaskActivity            | ✅      | Suspend |
| RabbitMqPublishActivity     | ✅      |         |
| RabbitMqSubscribeActivity   | ✅      | Suspend |
| DatabaseQueryActivity       | ✅      | Npgsql  |
| ScriptExecutionActivity     | ✅      | Roslyn  |
| WebhookTriggerActivity      | ✅      | Suspend |
| ServiceCollectionExtensions | ✅      |         |
| Activity Unit Tests         | ✅      |         |

## Phase 3: API Service und Persistierung

| Task                         | Status | Notizen                                  |
|------------------------------|--------|------------------------------------------|
| WorkflowDbContext + Entities | ✅      | 4 Entities, JSONB columns                |
| EF Core Migrations           | ✅      | EnsureCreatedAsync on startup            |
| EfWorkflowInstanceStore      | ✅      | Upsert-basiert                           |
| Workflow Endpoints (CRUD)    | ✅      | GET/POST/PUT/DELETE + Publish + Start    |
| Instance Endpoints           | ✅      | List, Get, Cancel, Resume, Log           |
| UserTask Endpoints           | ✅      | List (Pending), Get, Complete mit Resume |
| Webhook Endpoints            | ✅      | CorrelationId-basiertes Resume           |
| Activity Metadata Endpoints  | ✅      | Registered types aus ActivityRegistry    |
| OpenAPI Konfiguration        | ✅      | .WithOpenApi() auf allen Endpoints       |
| DelayResumeService           | ✅      | 10s Intervall, expired timer check       |
| RabbitMqListenerService      | ✅      | Queue consumer per suspended subscription|
| API Tests                    | ✅      | 25 Tests (Store + 3 Endpoint-Klassen)    |

## Phase 4: Frontend (Blazor WASM)

| Task                       | Status | Notizen |
|----------------------------|--------|---------|
| Blazor WASM Umstellung     | ⬜      |         |
| WorkflowApiClient          | ⬜      |         |
| DesignerStateService       | ⬜      |         |
| MainLayout + NavMenu       | ⬜      |         |
| Dashboard (Index)          | ⬜      |         |
| WorkflowList Seite         | ⬜      |         |
| WorkflowDesigner Seite     | ⬜      |         |
| ActivityPalette Komponente | ⬜      |         |
| WorkflowCanvas Komponente  | ⬜      |         |
| NodeComponent              | ⬜      |         |
| ConnectionLine             | ⬜      |         |
| PropertyPanel              | ⬜      |         |
| WorkflowInstances Seite    | ⬜      |         |
| UserTasks Seite            | ⬜      |         |
| bUnit Tests                | ⬜      |         |

## Phase 5: Integration und End-to-End

| Task                        | Status | Notizen |
|-----------------------------|--------|---------|
| Demo-Workflows (Seed Data)  | ⬜      |         |
| Bruno Collection befüllen   | ⬜      |         |
| Integration Tests erweitern | ⬜      |         |
| MailHog-Verifikation        | ⬜      |         |
| End-to-End Test             | ⬜      |         |

---

## Änderungslog

| Datum      | Änderung                                                                      |
|------------|-------------------------------------------------------------------------------|
| 2026-03-02 | Initiale Erstellung, Phase 0 gestartet                                        |
| 2026-03-02 | Phase 0 abgeschlossen: Dokumentation, Projekte, Aspire Host, Bruno Collection |
| 2026-03-02 | Phase 1 abgeschlossen: Engine Core (DAG, Execution, Expressions)               |
| 2026-03-02 | Phase 2 abgeschlossen: 11 Built-in Activities mit Tests                         |
| 2026-03-05 | Phase 3 abgeschlossen: API Service, Persistierung, Background Services          |
