# Implementierungsfortschritt

## Legende

| Symbol | Bedeutung |
|--------|-----------|
| :white_large_square: | Offen |
| :construction: | In Arbeit |
| :white_check_mark: | Abgeschlossen |
| :x: | Blockiert |

---

## Phase 0: Dokumentation und Projektstruktur

| Task | Status | Notizen |
|------|--------|---------|
| CLAUDE.md erstellen | :white_check_mark: | |
| docs/design.md erstellen | :white_check_mark: | |
| docs/specs/01-engine-core.md | :white_check_mark: | |
| docs/specs/02-activities.md | :white_check_mark: | |
| docs/specs/03-api-service.md | :white_check_mark: | |
| docs/specs/04-frontend-designer.md | :white_check_mark: | |
| docs/specs/05-infrastructure.md | :white_check_mark: | |
| docs/specs/progress.md | :white_check_mark: | Diese Datei |
| Workflow.Engine Projekt anlegen | :white_check_mark: | net10.0 Class Library |
| Workflow.Engine.Activities Projekt anlegen | :white_check_mark: | net10.0 Class Library, Ref → Engine |
| Workflow.Engine.Tests Projekt anlegen (MSTest) | :white_check_mark: | MSTest 4.0.1, Ref → Engine + Activities |
| Workflow.Web.Tests Projekt anlegen (bUnit) | :white_check_mark: | MSTest 4.0.1, Ref → Web |
| Workflow.Tests auf MSTest migrieren | :white_check_mark: | xUnit → MSTest 4.0.1 |
| Aspire Host erweitern (PostgreSQL, RabbitMQ, MailHog) | :white_check_mark: | Aspire.Hosting.PostgreSQL + RabbitMQ 13.1.1 |
| Bruno API Projekt-Struktur | :white_check_mark: | 16 .bru Dateien in 5 Ordnern |

## Phase 1: Engine Core

| Task | Status | Notizen |
|------|--------|---------|
| WorkflowDefinition Modell | :white_large_square: | Spec: 01-engine-core.md |
| WorkflowInstance Modell | :white_large_square: | |
| ActivityBase Abstraktion | :white_large_square: | |
| ActivityContext | :white_large_square: | |
| ActivityResult | :white_large_square: | |
| ActivityRegistry | :white_large_square: | |
| DagValidator (Kahn's Algorithmus) | :white_large_square: | |
| DagTraverser | :white_large_square: | |
| IExpressionEvaluator Interface | :white_large_square: | |
| SimpleExpressionEvaluator | :white_large_square: | |
| WorkflowExecutionEngine | :white_large_square: | |
| IWorkflowInstanceStore Interface | :white_large_square: | |
| WorkflowJsonConverter | :white_large_square: | |
| DagValidatorTests | :white_large_square: | |
| DagTraverserTests | :white_large_square: | |
| ExpressionEvaluatorTests | :white_large_square: | |
| ExecutionEngineTests | :white_large_square: | |
| ActivityRegistryTests | :white_large_square: | |
| SerializationTests | :white_large_square: | |

## Phase 2: Built-in Activities

| Task | Status | Notizen |
|------|--------|---------|
| LogActivity | :white_large_square: | |
| SetVariableActivity | :white_large_square: | |
| DelayActivity | :white_large_square: | Suspend |
| SendEmailActivity | :white_large_square: | MailKit |
| HttpRequestActivity | :white_large_square: | |
| UserTaskActivity | :white_large_square: | Suspend |
| RabbitMqPublishActivity | :white_large_square: | |
| RabbitMqSubscribeActivity | :white_large_square: | Suspend |
| DatabaseQueryActivity | :white_large_square: | Npgsql |
| ScriptExecutionActivity | :white_large_square: | Roslyn |
| WebhookTriggerActivity | :white_large_square: | Suspend |
| ServiceCollectionExtensions | :white_large_square: | |
| Activity Unit Tests | :white_large_square: | |

## Phase 3: API Service und Persistierung

| Task | Status | Notizen |
|------|--------|---------|
| WorkflowDbContext + Entities | :white_large_square: | |
| EF Core Migrations | :white_large_square: | |
| EfWorkflowInstanceStore | :white_large_square: | |
| Workflow Endpoints (CRUD) | :white_large_square: | |
| Instance Endpoints | :white_large_square: | |
| UserTask Endpoints | :white_large_square: | |
| Webhook Endpoints | :white_large_square: | |
| Activity Metadata Endpoints | :white_large_square: | |
| OpenAPI Konfiguration | :white_large_square: | |
| DelayResumeService | :white_large_square: | |
| RabbitMqListenerService | :white_large_square: | |
| API Tests | :white_large_square: | |

## Phase 4: Frontend (Blazor WASM)

| Task | Status | Notizen |
|------|--------|---------|
| Blazor WASM Umstellung | :white_large_square: | |
| WorkflowApiClient | :white_large_square: | |
| DesignerStateService | :white_large_square: | |
| MainLayout + NavMenu | :white_large_square: | |
| Dashboard (Index) | :white_large_square: | |
| WorkflowList Seite | :white_large_square: | |
| WorkflowDesigner Seite | :white_large_square: | |
| ActivityPalette Komponente | :white_large_square: | |
| WorkflowCanvas Komponente | :white_large_square: | |
| NodeComponent | :white_large_square: | |
| ConnectionLine | :white_large_square: | |
| PropertyPanel | :white_large_square: | |
| WorkflowInstances Seite | :white_large_square: | |
| UserTasks Seite | :white_large_square: | |
| bUnit Tests | :white_large_square: | |

## Phase 5: Integration und End-to-End

| Task | Status | Notizen |
|------|--------|---------|
| Demo-Workflows (Seed Data) | :white_large_square: | |
| Bruno Collection befüllen | :white_large_square: | |
| Integration Tests erweitern | :white_large_square: | |
| MailHog-Verifikation | :white_large_square: | |
| End-to-End Test | :white_large_square: | |

---

## Änderungslog

| Datum | Änderung |
|-------|---------|
| 2026-03-02 | Initiale Erstellung, Phase 0 gestartet |
| 2026-03-02 | Phase 0 abgeschlossen: Dokumentation, Projekte, Aspire Host, Bruno Collection |
