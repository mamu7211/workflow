# Designer Save ohne Fehlerbehandlung

## Status

Status: open
Detected: 2026-03-05

## Steps to reproduce the bug

1. Workflow Designer oeffnen
2. Einen Workflow erstellen/bearbeiten
3. Auf "Speichern" klicken, waehrend die API nicht erreichbar ist (oder einen Fehler zurueckgibt)
4. Die Blazor-Komponente crasht mit einer unbehandelten Exception

## Observed behavior

Die `Save()`-Methode in `WorkflowDesigner.razor` (Zeile 112) hat keinen try/catch fuer `HttpRequestException`. Bei einem API-Fehler (z.B. Connection Refused, 405, 500) wird die Exception nicht gefangen und crasht die gesamte Komponente:

```
Unhandled exception rendering component: TypeError: Failed to fetch
System.Net.Http.HttpRequestException: TypeError: Failed to fetch
   at Workflow.Web.WorkflowApiClient.CreateWorkflowAsync(CreateWorkflowDto dto)
   at Workflow.Web.Pages.WorkflowDesigner.Save()
```

## Expected outcome

Bei einem API-Fehler sollte eine benutzerfreundliche Fehlermeldung im UI angezeigt werden (z.B. als Alert/Toast), statt die Komponente zum Absturz zu bringen.

## Fix

1. In `Workflow.Web/Pages/WorkflowDesigner.razor` die `Save()`-Methode um einen try/catch fuer `HttpRequestException` erweitern
2. Eine Fehlermeldung als Variable speichern und im UI anzeigen (z.B. `_errorMessage`)
3. Die Fehlermeldung nach erneutem erfolgreichen Speichern wieder ausblenden
