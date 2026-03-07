# Spezifikation 06: MudBlazor Integration

## Überblick

Das bestehende `Workflow.Web` Frontend (Blazor WASM) wird von Bootstrap 5 auf **MudBlazor 9.x** (Material Design) umgestellt. Alle Seiten und Komponenten werden auf MudBlazor-Komponenten migriert. Die Kernlogik (WorkflowApiClient, DesignerStateService, SVG-Canvas) bleibt unverändert.

## Motivation

- Einheitliches, professionelles Material Design
- Fertige Komponenten für Dialoge, Datentabellen, Formulare (kein Custom-CSS nötig)
- `MudDataGrid` mit Sorting/Filtering out-of-the-box
- `IDialogService` ersetzt manuell gebaute Bootstrap-Modals
- `ISnackbar` für Feedback-Meldungen (Speichern, Fehler)

## Projekt-Setup

### NuGet-Pakete

**Workflow.Web.csproj:**
```xml
<PackageReference Include="MudBlazor" Version="9.*" />
```

**Workflow.Web.Tests.csproj:**
```xml
<PackageReference Include="MudBlazor" Version="9.*" />
```

### wwwroot/index.html

Bootstrap-CSS entfernen, MudBlazor-Ressourcen hinzufügen:

```html
<!DOCTYPE html>
<html lang="de">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Workflow Designer</title>
    <base href="/" />
    <!-- MudBlazor 9 -->
    <link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />
    <!-- App CSS (nur Designer-spezifische SVG-Styles) -->
    <link rel="stylesheet" href="css/workflow-designer.css" />
    <link rel="stylesheet" href="Workflow.Web.styles.css" />
</head>
<body>
    <div id="app">Loading...</div>
    <script src="_framework/blazor.webassembly.js"></script>
    <script src="_content/MudBlazor/MudBlazor.min.js"></script>
</body>
</html>
```

> **Hinweis:** MudBlazor 9 bündelt Roboto-Font direkt – kein Google Fonts CDN-Link nötig.

### Program.cs

```csharp
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Workflow.Web;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp =>
    new HttpClient { BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress) });

builder.Services.AddScoped<WorkflowApiClient>();
builder.Services.AddScoped<DesignerStateService>();

// MudBlazor
builder.Services.AddMudServices();

await builder.Build().RunAsync();
```

### _Imports.razor

MudBlazor-Namespace hinzufügen:
```razor
@using MudBlazor
```

## Layout-Migration

### MainLayout.razor

Bootstrap-Layout → MudLayout. In MudBlazor 9 sind zusätzlich `MudPopoverProvider` erforderlich:

```razor
@inherits LayoutComponentBase

<MudThemeProvider />
<MudPopoverProvider />
<MudDialogProvider />
<MudSnackbarProvider />

<MudLayout>
    <MudAppBar Elevation="1" Color="Color.Primary">
        <MudIconButton Icon="@Icons.Material.Filled.Menu" Color="Color.Inherit" Edge="Edge.Start"
                       OnClick="@ToggleDrawer" />
        <MudText Typo="Typo.h6">Workflow Designer</MudText>
    </MudAppBar>

    <MudDrawer @bind-Open="_drawerOpen" Elevation="2" Variant="DrawerVariant.Mini">
        <NavMenu />
    </MudDrawer>

    <MudMainContent>
        <MudContainer MaxWidth="MaxWidth.False" Class="pa-4">
            @Body
        </MudContainer>
    </MudMainContent>
</MudLayout>

@code {
    private bool _drawerOpen = true;
    private void ToggleDrawer() => _drawerOpen = !_drawerOpen;
}
```

### NavMenu.razor

```razor
<MudNavMenu>
    <MudNavLink Href="/" Match="NavLinkMatch.All"
                Icon="@Icons.Material.Filled.Dashboard">Dashboard</MudNavLink>
    <MudNavLink Href="/workflows"
                Icon="@Icons.Material.Filled.AccountTree">Workflows</MudNavLink>
    <MudNavLink Href="/instances"
                Icon="@Icons.Material.Filled.PlayCircle">Instanzen</MudNavLink>
    <MudNavLink Href="/tasks"
                Icon="@Icons.Material.Filled.Assignment">Aufgaben</MudNavLink>
</MudNavMenu>
```

## Seiten-Migration

### Index.razor (Dashboard)

Bootstrap-Cards → MudPaper + MudGrid + MudTable:

```razor
@page "/"
@inject WorkflowApiClient Api

<PageTitle>Dashboard</PageTitle>
<MudText Typo="Typo.h4" GutterBottom="true">Dashboard</MudText>

@if (_loading)
{
    <MudProgressCircular Indeterminate="true" />
}
else
{
    <MudGrid>
        <MudItem xs="12" sm="6" md="3">
            <MudPaper Class="pa-4" Elevation="2">
                <MudText Typo="Typo.subtitle2" Color="Color.Primary">Workflows</MudText>
                <MudText Typo="Typo.h3">@_workflowCount</MudText>
            </MudPaper>
        </MudItem>
        <MudItem xs="12" sm="6" md="3">
            <MudPaper Class="pa-4" Elevation="2">
                <MudText Typo="Typo.subtitle2" Color="Color.Success">Aktive Instanzen</MudText>
                <MudText Typo="Typo.h3">@_activeInstances</MudText>
            </MudPaper>
        </MudItem>
        <MudItem xs="12" sm="6" md="3">
            <MudPaper Class="pa-4" Elevation="2">
                <MudText Typo="Typo.subtitle2" Color="Color.Warning">Offene Aufgaben</MudText>
                <MudText Typo="Typo.h3">@_openTasks</MudText>
            </MudPaper>
        </MudItem>
        <MudItem xs="12" sm="6" md="3">
            <MudPaper Class="pa-4" Elevation="2">
                <MudText Typo="Typo.subtitle2" Color="Color.Info">Abgeschlossen</MudText>
                <MudText Typo="Typo.h3">@_completedInstances</MudText>
            </MudPaper>
        </MudItem>
    </MudGrid>

    @if (_recentInstances.Length > 0)
    {
        <MudText Typo="Typo.h6" Class="mt-6 mb-2">Letzte Aktivitäten</MudText>
        <MudTable Items="_recentInstances" Dense="true" Hover="true" Elevation="1">
            <HeaderContent>
                <MudTh>Workflow</MudTh>
                <MudTh>Status</MudTh>
                <MudTh>Gestartet</MudTh>
            </HeaderContent>
            <RowTemplate>
                <MudTd>@context.WorkflowDefinitionId</MudTd>
                <MudTd>
                    <MudChip T="string" Color="@GetStatusColor(context.Status)" Size="Size.Small">
                        @context.Status
                    </MudChip>
                </MudTd>
                <MudTd>@context.CreatedAt.ToString("g")</MudTd>
            </RowTemplate>
        </MudTable>
    }
}
```

Status-Farben via `Color`-Enum (kein CSS-Klassen-String):

```csharp
private static Color GetStatusColor(string status) => status switch
{
    "Running"   => Color.Primary,
    "Suspended" => Color.Warning,
    "Completed" => Color.Success,
    "Faulted"   => Color.Error,
    "Cancelled" => Color.Default,
    _           => Color.Info
};
```

### WorkflowList.razor

Bootstrap-Tabelle + manuelles Modal → MudDataGrid + IDialogService:

```razor
@page "/workflows"
@inject WorkflowApiClient Api
@inject NavigationManager Navigation
@inject IDialogService DialogService
@inject ISnackbar Snackbar

<MudStack Row="true" AlignItems="AlignItems.Center" Class="mb-4">
    <MudText Typo="Typo.h4">Workflows</MudText>
    <MudSpacer />
    <MudButton Variant="Variant.Filled" Color="Color.Primary"
               StartIcon="@Icons.Material.Filled.Add"
               OnClick="CreateNew">Neuen Workflow erstellen</MudButton>
</MudStack>

@if (_loading)
{
    <MudProgressCircular Indeterminate="true" />
}
else
{
    <MudDataGrid T="WorkflowDefinitionDto" Items="_workflows"
                 Hover="true" Dense="true" Elevation="1"
                 QuickFilter="@_quickFilter">
        <ToolBarContent>
            <MudSpacer />
            <MudTextField @bind-Value="_searchText" Placeholder="Suchen..."
                          Adornment="Adornment.Start"
                          AdornmentIcon="@Icons.Material.Filled.Search"
                          IconSize="Size.Medium" Class="mt-0" Immediate="true" />
        </ToolBarContent>
        <Columns>
            <PropertyColumn Property="x => x.Name" Title="Name" />
            <PropertyColumn Property="x => x.Version" Title="Version" />
            <TemplateColumn Title="Status">
                <CellTemplate>
                    <MudChip T="string"
                             Color="@(context.Item.IsPublished ? Color.Success : Color.Default)"
                             Size="Size.Small">
                        @(context.Item.IsPublished ? "Published" : "Draft")
                    </MudChip>
                </CellTemplate>
            </TemplateColumn>
            <PropertyColumn Property="x => x.CreatedAt" Title="Erstellt" Format="g" />
            <PropertyColumn Property="x => x.UpdatedAt" Title="Aktualisiert" Format="g" />
            <TemplateColumn Title="Aktionen">
                <CellTemplate>
                    <MudStack Row="true" Spacing="1">
                        <MudIconButton Icon="@Icons.Material.Filled.Edit"
                                       Size="Size.Small" Color="Color.Primary"
                                       OnClick="@(() => Edit(context.Item.Id))" />
                        @if (!context.Item.IsPublished)
                        {
                            <MudIconButton Icon="@Icons.Material.Filled.Publish"
                                           Size="Size.Small" Color="Color.Success"
                                           OnClick="@(() => Publish(context.Item.Id))" />
                        }
                        <MudIconButton Icon="@Icons.Material.Filled.PlayArrow"
                                       Size="Size.Small" Color="Color.Info"
                                       OnClick="@(() => Start(context.Item.Id))" />
                        <MudIconButton Icon="@Icons.Material.Filled.Delete"
                                       Size="Size.Small" Color="Color.Error"
                                       OnClick="@(() => Delete(context.Item))" />
                    </MudStack>
                </CellTemplate>
            </TemplateColumn>
        </Columns>
        <NoRecordsContent>
            <MudText>Keine Workflows vorhanden. Erstellen Sie einen neuen Workflow.</MudText>
        </NoRecordsContent>
    </MudDataGrid>
}
```

Delete-Bestätigung via `IDialogService.ShowMessageBox`:

```csharp
private async Task Delete(WorkflowDefinitionDto wf)
{
    var result = await DialogService.ShowMessageBox(
        "Workflow löschen",
        $"Möchten Sie den Workflow \"{wf.Name}\" wirklich löschen?",
        yesText: "Löschen", cancelText: "Abbrechen");

    if (result == true)
    {
        await Api.DeleteWorkflowAsync(wf.Id);
        Snackbar.Add($"Workflow \"{wf.Name}\" gelöscht.", Severity.Success);
        await LoadWorkflows();
    }
}

private Func<WorkflowDefinitionDto, bool> _quickFilter =>
    wf => string.IsNullOrWhiteSpace(_searchText)
       || wf.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase);
```

### WorkflowInstances.razor

Gleiche Struktur wie WorkflowList:
- MudDataGrid mit Status-Filter via `QuickFilter`
- MudChip für farbcodierten Status
- Detail-Route `/instances/{id}`: MudExpansionPanel für Execution Log, MudTable für Variables
- Cancel-Bestätigung via `IDialogService.ShowMessageBox`

### UserTasks.razor

MudDataGrid für Task-Liste. Approve/Reject via eigene Dialog-Komponente:

**Components/Dialogs/CompleteTaskDialog.razor** (neu):

```razor
@* MudBlazor 9: CascadingParameter ist IMudDialogInstance *@
<MudDialog>
    <TitleContent>
        <MudText Typo="Typo.h6">@(Approve ? "Genehmigen" : "Ablehnen")</MudText>
    </TitleContent>
    <DialogContent>
        <MudTextField @bind-Value="Comment" Label="Kommentar (optional)"
                      Lines="3" Variant="Variant.Outlined" />
    </DialogContent>
    <DialogActions>
        <MudButton OnClick="Cancel">Abbrechen</MudButton>
        <MudButton Color="@(Approve ? Color.Success : Color.Error)"
                   Variant="Variant.Filled"
                   OnClick="Submit">@(Approve ? "Genehmigen" : "Ablehnen")</MudButton>
    </DialogActions>
</MudDialog>

@code {
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = default!;
    [Parameter] public bool Approve { get; set; }
    public string Comment { get; set; } = "";

    void Cancel() => MudDialog.Cancel();
    void Submit() => MudDialog.Close(DialogResult.Ok(Comment));
}
```

Aufruf aus UserTasks.razor:

```csharp
private async Task OpenCompleteDialog(UserTaskDto task, bool approve)
{
    var parameters = new DialogParameters<CompleteTaskDialog>
    {
        { x => x.Approve, approve }
    };
    var dialog = await DialogService.ShowAsync<CompleteTaskDialog>(
        approve ? "Genehmigen" : "Ablehnen", parameters);
    var result = await dialog.Result;

    if (!result.Canceled && result.Data is string comment)
    {
        await Api.CompleteTaskAsync(task.Id, new CompleteTaskDto(approve ? "Approved" : "Rejected", comment));
        Snackbar.Add($"Aufgabe {(approve ? "genehmigt" : "abgelehnt")}.", Severity.Success);
        await LoadTasks();
    }
}
```

### WorkflowDesigner.razor

MudToolBar + MudGrid als Dreispalten-Layout:

```razor
<MudPaper Elevation="1" Class="mb-2">
    <MudToolBar Dense="true">
        <MudText Typo="Typo.h6" Class="mr-4">@(_workflowName ?? "Neuer Workflow")</MudText>
        <MudSpacer />
        <MudTooltip Text="Rückgängig">
            <MudIconButton Icon="@Icons.Material.Filled.Undo"
                           Disabled="@(!State.CanUndo)" OnClick="@State.Undo" />
        </MudTooltip>
        <MudTooltip Text="Wiederholen">
            <MudIconButton Icon="@Icons.Material.Filled.Redo"
                           Disabled="@(!State.CanRedo)" OnClick="@State.Redo" />
        </MudTooltip>
        <MudDivider Vertical="true" FlexItem="true" Class="mx-2" />
        <MudButton Variant="Variant.Filled" Color="Color.Primary"
                   StartIcon="@Icons.Material.Filled.Save"
                   OnClick="Save">Speichern</MudButton>
        <MudButton Variant="Variant.Filled" Color="Color.Success"
                   StartIcon="@Icons.Material.Filled.PlayArrow"
                   Class="ml-2" OnClick="StartWorkflow">Starten</MudButton>
    </MudToolBar>
</MudPaper>

<MudGrid Spacing="0" Style="height: calc(100vh - 200px)">
    <MudItem xs="2">
        <MudPaper Elevation="1" Style="height:100%; overflow-y:auto;">
            <ActivityPalette OnActivityDropped="@AddActivity" />
        </MudPaper>
    </MudItem>
    <MudItem xs="7">
        <MudPaper Elevation="0" Style="height:100%;">
            <WorkflowCanvas />
        </MudPaper>
    </MudItem>
    <MudItem xs="3">
        <MudPaper Elevation="1" Style="height:100%; overflow-y:auto;">
            <PropertyPanel />
        </MudPaper>
    </MudItem>
</MudGrid>

<MudExpansionPanels Class="mt-2">
    <MudExpansionPanel Text="JSON Preview">
        <pre style="font-size:0.75rem; max-height:200px; overflow:auto;">@_json</pre>
    </MudExpansionPanel>
</MudExpansionPanels>
```

### ActivityPalette.razor

Bootstrap-Listengruppen → MudList mit MudListSubheader:

```razor
<MudList T="string" Dense="true">
    @foreach (var group in _groupedTypes)
    {
        <MudListSubheader>@group.Key</MudListSubheader>
        @foreach (var type in group.Value)
        {
            <MudListItem T="string"
                         Icon="@GetIcon(type.Type)"
                         draggable="true"
                         @ondragstart="@(() => OnDragStart(type))">
                @type.DisplayName
            </MudListItem>
        }
    }
</MudList>
```

### PropertyPanel.razor

Bootstrap-Formulare → MudForm mit einheitlichem `Variant.Outlined`:

```razor
<MudForm>
    <MudTextField Label="Name" @bind-Value="_displayName"
                  Variant="Variant.Outlined" Margin="Margin.Dense" Class="mb-2" />
    <MudTextField Label="Beschreibung" @bind-Value="_description"
                  Variant="Variant.Outlined" Margin="Margin.Dense" Lines="2" Class="mb-2" />
    <!-- Typ-spezifische Felder: MudTextField / MudSelect / MudCheckBox / MudNumericField -->
</MudForm>
```

Felder pro Activity-Typ:

| Activity | MudBlazor-Komponenten |
|----------|----------------------|
| Log | MudTextField (Message), MudSelect (Level) |
| SetVariable | MudTextField (Name, Value) |
| Delay | MudTextField (Duration, Pattern `hh:mm:ss`) |
| SendEmail | MudTextField (To, Subject, Body), MudCheckBox (IsHtml) |
| HttpRequest | MudTextField (URL, Body), MudSelect (Method), Key-Value-Editor |
| UserTask | MudTextField (Title, Description, AssignedTo) |
| RabbitMqPublish | MudTextField (Exchange, RoutingKey, Message) |
| RabbitMqSubscribe | MudTextField (QueueName, Timeout) |
| DatabaseQuery | MudTextField (Query, multiline), Key-Value-Editor |
| ScriptExecution | MudTextField (Script, multiline), MudNumericField (Timeout) |
| WebhookTrigger | MudTextField (Path, readonly), MudSelect (Method) |

## Entfernbare Dateien

Nach der Migration:
- `wwwroot/css/app.css` (Bootstrap-spezifische Styles)
- `wwwroot/lib/bootstrap/` (gesamtes Verzeichnis)
- `Layout/MainLayout.razor.css`
- `Layout/NavMenu.razor.css`

`wwwroot/css/workflow-designer.css` **bleibt** (SVG-Canvas-Styles).

## Tests (bUnit / MSTest)

MudBlazor 9 in bUnit-Context:

```csharp
using var ctx = new BunitContext();
ctx.JSInterop.Mode = JSRuntimeMode.Loose; // MudBlazor braucht JS-Interop
ctx.Services.AddMudServices();
// ... weitere Services
```

### Anzupassende Testklassen

| Testklasse | Anpassung |
|-----------|-----------|
| `WorkflowListTests` | `AddMudServices()` + `JSInterop.Loose`; CSS-Selektoren von `.btn-outline-danger` auf Icon-Button oder `data-testid`; Delete-Dialog via `ShowMessageBox` testen |
| `ActivityPaletteTests` | `AddMudServices()` + `JSInterop.Loose`; MudListItem-Selektoren |
| `PropertyPanelTests` | `AddMudServices()` + `JSInterop.Loose`; MudTextField-Selektoren (`mud-input-slot`) |
| `NodeComponentTests` | Unverändert (SVG, keine MudBlazor-Abhängigkeit) |
| `DesignerStateServiceTests` | Unverändert (keine UI-Abhängigkeit) |

### Delete-Bestätigung testen

```csharp
[TestMethod]
public async Task ShowsDeleteConfirmation_OnDeleteClick()
{
    using var ctx = new BunitContext();
    ctx.JSInterop.Mode = JSRuntimeMode.Loose;
    ctx.Services.AddMudServices();
    var handler = new MockHttpHandler();
    handler.SetResponse("/api/workflows",
        JsonSerializer.Serialize(new[] { CreateTestDto("Delete Me") }));
    ctx.Services.AddScoped(_ =>
        new WorkflowApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") }));

    var cut = ctx.Render<WorkflowList>();
    cut.WaitForState(() => cut.Markup.Contains("Delete Me"));

    var deleteBtn = cut.FindAll("button").First(b => b.InnerHtml.Contains("delete", StringComparison.OrdinalIgnoreCase));
    await cut.InvokeAsync(() => deleteBtn.Click());

    Assert.IsTrue(cut.Markup.Contains("Workflow löschen"));
}
```

## Dateistruktur (Änderungen)

```
Workflow.Web/
├── Components/
│   ├── Designer/
│   │   ├── WorkflowCanvas.razor      (unveraendert - SVG)
│   │   ├── NodeComponent.razor       (unveraendert - SVG)
│   │   ├── ConnectionLine.razor      (unveraendert - SVG)
│   │   ├── ActivityPalette.razor     (MudList)
│   │   └── PropertyPanel.razor       (MudForm/MudTextField)
│   └── Dialogs/                      (NEU)
│       └── CompleteTaskDialog.razor  (NEU)
├── Layout/
│   ├── MainLayout.razor              (MudLayout + MudPopoverProvider)
│   └── NavMenu.razor                 (MudNavMenu)
├── Pages/
│   ├── Index.razor                   (MudGrid + MudTable)
│   ├── WorkflowList.razor            (MudDataGrid + IDialogService)
│   ├── WorkflowDesigner.razor        (MudToolBar + MudGrid)
│   ├── WorkflowInstances.razor       (MudDataGrid + MudChip)
│   └── UserTasks.razor               (MudDataGrid + CompleteTaskDialog)
├── _Imports.razor                    (@using MudBlazor hinzugefuegt)
└── wwwroot/
    ├── index.html                    (MudBlazor CSS/JS, kein Bootstrap)
    └── css/
        └── workflow-designer.css     (bleibt)
```

## Branch

```
feature/06-mudblazor-integration
```

## Akzeptanzkriterien

- [ ] MudBlazor 9.x NuGet installiert, Bootstrap-Abhängigkeit entfernt
- [ ] `MudThemeProvider`, `MudPopoverProvider`, `MudDialogProvider`, `MudSnackbarProvider` im Root-Layout
- [ ] MainLayout mit MudLayout / MudAppBar / MudDrawer
- [ ] NavMenu mit MudNavMenu / MudNavLink
- [ ] Dashboard mit MudGrid-Kacheln und MudTable
- [ ] WorkflowList mit MudDataGrid (Suche/Sortierung) + IDialogService Delete-Bestätigung
- [ ] WorkflowInstances mit MudDataGrid + MudChip Status-Farben
- [ ] UserTasks mit MudDataGrid + CompleteTaskDialog
- [ ] WorkflowDesigner Toolbar mit MudToolBar + Dreispalten-Layout via MudGrid
- [ ] ActivityPalette mit MudList / MudListSubheader
- [ ] PropertyPanel mit MudForm / MudTextField / MudSelect / MudCheckBox
- [ ] SVG-Canvas-Komponenten (NodeComponent, ConnectionLine, WorkflowCanvas) unverändert
- [ ] Alle bUnit Tests gruen (nach Anpassung auf MudBlazor-Selektoren)
- [ ] `dotnet build` fehlerfrei
