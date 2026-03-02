# Spezifikation 04: Frontend Workflow Designer

## Überblick

Das `Workflow.Web` Projekt wird von Blazor Server auf Blazor WASM umgestellt.
Es enthält einen visuellen Node-Editor für Workflow-Definitionen, Instanz-Monitoring
und eine User Task Inbox.

## Projekt-Setup

- **Projekt:** `Workflow.Web` (bestehendes Projekt umbauen)
- **SDK:** `Microsoft.NET.Sdk.BlazorWebAssembly`
- **Framework:** `net10.0`
- **NuGet-Pakete:**
  - `Microsoft.AspNetCore.Components.WebAssembly`
  - `Microsoft.AspNetCore.Components.WebAssembly.DevServer` (nur Dev)
  - `Z.Blazor.Diagrams` (Node-Editor, falls .NET 10 kompatibel, sonst Custom SVG)
- **CSS:** Bootstrap 5 (bestehend) + Custom Styles für Designer

## Blazor WASM Umstellung

### Änderungen an Program.cs

```csharp
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Workflow.Web;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// API HttpClient
builder.Services.AddScoped(sp =>
    new HttpClient { BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress) });

builder.Services.AddScoped<WorkflowApiClient>();
builder.Services.AddScoped<DesignerStateService>();

await builder.Build().RunAsync();
```

### wwwroot/index.html

Neuer HTML-Host (ersetzt Server-Side App.razor):

```html
<!DOCTYPE html>
<html lang="de">
<head>
    <meta charset="utf-8" />
    <title>Workflow Designer</title>
    <base href="/" />
    <link rel="stylesheet" href="lib/bootstrap/dist/css/bootstrap.min.css" />
    <link rel="stylesheet" href="css/app.css" />
    <link rel="stylesheet" href="css/workflow-designer.css" />
    <link rel="stylesheet" href="Workflow.Web.styles.css" />
</head>
<body>
    <div id="app">Loading...</div>
    <script src="_framework/blazor.webassembly.js"></script>
</body>
</html>
```

### Zu entfernende Dateien

- `Components/App.razor` → ersetzt durch `wwwroot/index.html` + `App.razor` (Root Component)
- `WeatherApiClient.cs` → ersetzt durch `WorkflowApiClient.cs`
- `Components/Pages/Counter.razor` → entfernen
- `Components/Pages/Weather.razor` → entfernen

## Seiten

### Index.razor (Dashboard)

Route: `/`

Zeigt Übersicht:
- Anzahl Workflow-Definitionen
- Aktive Instanzen (Running/Suspended)
- Offene User Tasks
- Letzte Aktivitäten

### WorkflowList.razor

Route: `/workflows`

Tabelle aller Workflow-Definitionen mit:
- Name, Version, Status (Draft/Published), Erstellt/Aktualisiert
- Aktionen: Bearbeiten (→ Designer), Löschen, Veröffentlichen, Starten
- "Neuen Workflow erstellen" Button

### WorkflowDesigner.razor

Route: `/workflows/{id}/edit` und `/workflows/new`

Hauptkomponente des Designers. Layout:

```
┌──────────────────────────────────────────────────────────────┐
│  Toolbar: [Speichern] [Rückgängig] [Wiederholen] [Starten]  │
├──────────────┬───────────────────────────┬───────────────────┤
│  Activity    │                           │  Property Panel   │
│  Palette     │     Workflow Canvas       │                   │
│              │                           │  (Formular für    │
│  Drag & Drop │     Node-Graph            │   selektierten    │
│  Liste       │     SVG/Canvas            │   Node)           │
│              │                           │                   │
│  ○ Log       │                           │  Name: [_____]    │
│  ○ Email     │                           │  Prop1: [_____]   │
│  ○ HTTP      │                           │  Prop2: [_____]   │
│  ○ Delay     │                           │                   │
│  ○ UserTask  │                           │                   │
│  ...         │                           │                   │
├──────────────┴───────────────────────────┴───────────────────┤
│  JSON Preview (collapsible)                                   │
└──────────────────────────────────────────────────────────────┘
```

### WorkflowInstances.razor

Route: `/instances`

Tabelle aller Workflow-Instanzen mit:
- Workflow-Name, Status (farbcodiert), Gestartet, Abgeschlossen
- Filter nach Status
- Aktionen: Details anzeigen, Abbrechen (wenn Running/Suspended)

Route: `/instances/{id}` (Detail-Ansicht)
- Graph-Visualisierung mit farbcodierten Knoten (Completed=grün, Running=blau, etc.)
- Execution Log als Timeline
- Variablen-Tabelle

### UserTasks.razor

Route: `/tasks`

Inbox für offene Genehmigungen:
- Titel, Workflow-Name, Zugewiesen an, Erstellt
- Aktionen: Genehmigen, Ablehnen (mit optionalem Kommentar)

## Designer-Komponenten

### ActivityPalette.razor

Linke Sidebar mit allen verfügbaren Activity-Typen:
- Gruppiert nach Kategorie (Utility, Integration, Messaging, etc.)
- Drag & Drop auf den Canvas
- Icon + Label pro Typ
- Wird über GET `/api/activities/types` befüllt

### WorkflowCanvas.razor

Zentrale Graph-Darstellung:
- SVG-basiert oder via Blazor.Diagrams
- Nodes als Rechtecke mit Icon, Label, Status-Indikator
- Connections als Bezier-Kurven (SVG path)
- Zoom & Pan (Mouse Wheel / Drag auf leerem Bereich)
- Selektion durch Klick
- Multi-Selektion durch Ctrl+Klick oder Rahmen
- Neue Connection: Drag von Output-Port zu Input-Port

### NodeComponent.razor

Einzelner Knoten im Graph:
- Rechteck mit abgerundeten Ecken
- Activity-Typ Icon (oben)
- Display Name (Mitte)
- Input-Port (links) und Output-Port (rechts)
- Farbcodierung je nach Activity-Kategorie
- Selektions-Highlight

### ConnectionLine.razor

Verbindungslinie zwischen Nodes:
- SVG `<path>` mit Bezier-Kurve
- Pfeil am Ziel
- Optionale Bedingung als Label auf der Linie
- Hover-Highlight
- Klick zum Auswählen (Bedingung editieren)

### PropertyPanel.razor

Rechte Sidebar - zeigt Formular für den selektierten Node:

- **Gemeinsame Felder:** DisplayName, Beschreibung
- **Typ-spezifische Felder** (dynamisch basierend auf Activity-Type):

| Activity | Formularfelder |
|----------|---------------|
| Log | Message (Textarea), Level (Dropdown) |
| SetVariable | VariableName (Text), Value (Text) |
| Delay | Duration (TimeSpan-Input) |
| SendEmail | To (Email), Subject (Text), Body (Textarea), IsHtml (Checkbox) |
| HttpRequest | URL (Text), Method (Dropdown), Headers (Key-Value Editor), Body (Textarea) |
| UserTask | Title (Text), Description (Textarea), AssignedTo (Text) |
| RabbitMqPublish | Exchange (Text), RoutingKey (Text), Message (Textarea) |
| RabbitMqSubscribe | QueueName (Text), Timeout (TimeSpan-Input) |
| DatabaseQuery | Query (SQL Textarea), Parameters (Key-Value Editor) |
| ScriptExecution | Script (Code Textarea), Timeout (Number) |
| WebhookTrigger | Path (Text, auto-generiert), ExpectedMethod (Dropdown) |

## Services

### WorkflowApiClient

Typisierter HttpClient-Wrapper:

```csharp
public class WorkflowApiClient(HttpClient http)
{
    // Definitions
    public Task<WorkflowDefinitionDto[]> GetWorkflowsAsync();
    public Task<WorkflowDefinitionDto> GetWorkflowAsync(string id);
    public Task<WorkflowDefinitionDto> CreateWorkflowAsync(CreateWorkflowDto dto);
    public Task<WorkflowDefinitionDto> UpdateWorkflowAsync(string id, UpdateWorkflowDto dto);
    public Task DeleteWorkflowAsync(string id);
    public Task<WorkflowDefinitionDto> PublishWorkflowAsync(string id);
    public Task<WorkflowInstanceDto> StartWorkflowAsync(string id, StartWorkflowDto? dto = null);

    // Instances
    public Task<WorkflowInstanceDto[]> GetInstancesAsync();
    public Task<WorkflowInstanceDto> GetInstanceAsync(string id);
    public Task<WorkflowInstanceDto> CancelInstanceAsync(string id);
    public Task<ActivityExecutionLogDto[]> GetInstanceLogAsync(string id);

    // Tasks
    public Task<UserTaskDto[]> GetTasksAsync();
    public Task<UserTaskDto> CompleteTaskAsync(string id, CompleteTaskDto dto);

    // Metadata
    public Task<ActivityTypeDto[]> GetActivityTypesAsync();
}
```

### DesignerStateService

Lokaler State-Manager für den Editor:

```csharp
public class DesignerStateService
{
    public WorkflowDefinition? CurrentWorkflow { get; }
    public string? SelectedNodeId { get; }
    public string? SelectedConnectionId { get; }

    public event Action? OnChange;

    public void LoadWorkflow(WorkflowDefinition definition);
    public void AddNode(ActivityNode node);
    public void RemoveNode(string nodeId);
    public void UpdateNode(ActivityNode node);
    public void AddConnection(Connection connection);
    public void RemoveConnection(string connectionId);
    public void SelectNode(string? nodeId);
    public void SelectConnection(string? connectionId);

    // Undo/Redo
    public void Undo();
    public void Redo();
    public bool CanUndo { get; }
    public bool CanRedo { get; }
}
```

## Dateistruktur

```
Workflow.Web/
├── Workflow.Web.csproj
├── Program.cs
├── App.razor
├── _Imports.razor
├── wwwroot/
│   ├── index.html
│   ├── css/
│   │   ├── app.css
│   │   └── workflow-designer.css
│   ├── favicon.png
│   └── lib/bootstrap/
├── Layout/
│   ├── MainLayout.razor
│   ├── MainLayout.razor.css
│   ├── NavMenu.razor
│   └── NavMenu.razor.css
├── Pages/
│   ├── Index.razor
│   ├── WorkflowList.razor
│   ├── WorkflowDesigner.razor
│   ├── WorkflowInstances.razor
│   └── UserTasks.razor
├── Components/
│   └── Designer/
│       ├── WorkflowCanvas.razor
│       ├── NodeComponent.razor
│       ├── ConnectionLine.razor
│       ├── PropertyPanel.razor
│       └── ActivityPalette.razor
└── Services/
    ├── WorkflowApiClient.cs
    └── DesignerStateService.cs
```

## Tests (bUnit / MSTest)

- **Projekt:** `Workflow.Web.Tests`
- **NuGet:** `bunit`, `bunit.web`, `MSTest.TestFramework`, `MSTest.TestAdapter`

| Testklasse | Szenarien |
|-----------|-----------|
| `ActivityPaletteTests` | Zeigt alle Activity-Typen, Drag initiiert Event |
| `PropertyPanelTests` | Zeigt korrekte Felder pro Activity-Typ, Änderungen propagiert |
| `NodeComponentTests` | Rendert Name/Icon, Selektion-Highlight |
| `WorkflowListTests` | Lädt und zeigt Workflows, Delete-Bestätigung |
| `DesignerStateServiceTests` | AddNode, RemoveNode, Undo/Redo |

## Akzeptanzkriterien

- [ ] Blazor WASM läuft standalone im Browser
- [ ] Node-Editor zeigt Workflow-Graph
- [ ] Nodes können per Drag & Drop platziert werden
- [ ] Nodes können verbunden werden
- [ ] Property Panel zeigt typ-spezifische Formulare
- [ ] JSON-Definition wird live aktualisiert
- [ ] Workflows können gespeichert und geladen werden
- [ ] Instanz-Status wird korrekt visualisiert
- [ ] User Tasks können genehmigt/abgelehnt werden
- [ ] bUnit Tests grün
