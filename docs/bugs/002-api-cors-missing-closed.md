# API CORS nicht konfiguriert - 405 Method Not Allowed

## Status

Status: closed
Detected: 2026-03-05
Closed: 2026-03-05T00:00:00Z

## Steps to reproduce the bug

1. Aspire-Anwendung starten (`dotnet run --project Workflow.AppHost`)
2. Workflow Designer im Browser oeffnen
3. Neuen Workflow erstellen und auf "Speichern" klicken
4. Browser-Konsole zeigt: `:7057/api/workflows` - `405 Method Not Allowed`

Das Blazor WASM-Frontend laeuft im Browser auf einem anderen Port/Origin als die API (`:7057`). Bei POST/PUT/DELETE-Requests sendet der Browser einen CORS-Preflight (OPTIONS-Request). Die API hat keine CORS-Middleware konfiguriert und antwortet mit 405.

## Observed behavior

Der Browser sendet einen OPTIONS-Preflight-Request an die API. Da keine CORS-Policy konfiguriert ist, gibt die API `405 Method Not Allowed` zurueck. Der eigentliche POST-Request wird vom Browser blockiert.

Fehlermeldung in der Browser-Konsole:
```
Failed to load resource: the server responded with a status of 405 ()
System.Net.Http.HttpRequestException: net_http_message_not_success_statuscode_reason, 405, Method Not Allowed
   at Workflow.Web.WorkflowApiClient.CreateWorkflowAsync(CreateWorkflowDto dto)
```

## Expected outcome

Die API sollte CORS-Preflight-Requests korrekt beantworten, sodass das Blazor WASM-Frontend cross-origin API-Calls ausfuehren kann.

## Fix

1. In `Workflow.ApiService/Program.cs` CORS-Services und Middleware hinzufuegen:
   - `builder.Services.AddCors()` mit einer Policy, die den Frontend-Origin erlaubt
   - `app.UseCors()` vor den Endpoint-Mappings
   - Erlaubte Methods: GET, POST, PUT, DELETE
   - Erlaubte Headers: Content-Type, Authorization
2. Fuer Development: Alle Origins erlauben (`AllowAnyOrigin()`)
3. Pruefen ob Aspire die CORS-Konfiguration ueber Service Discovery automatisieren kann
