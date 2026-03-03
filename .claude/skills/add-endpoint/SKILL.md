---
name: add-endpoint
description: Erstellt einen neuen REST API Endpoint im ApiService. Scaffolding für Minimal API Route, DTOs, Bruno-Request und OpenAPI-Annotation.
disable-model-invocation: true
---

# Neuen API-Endpoint erstellen

Erstelle einen neuen REST API Endpoint im `Workflow.ApiService`.

## Argumente

- `$0`: HTTP-Methode und Pfad (z.B. `GET /api/reports`, `POST /api/workflows/{id}/clone`)
- `$1`: (optional) Kurzbeschreibung

## Ablauf

1. **Bestehende Endpoints lesen**: Lies die existierenden Endpoints in `Workflow.ApiService/Endpoints/` als Referenz.

2. **Endpoint erstellen oder erweitern**:
   - Wenn eine passende Endpoint-Datei existiert (z.B. `WorkflowEndpoints.cs` für `/api/workflows/*`), dort hinzufügen
   - Sonst neue Datei in `Workflow.ApiService/Endpoints/` erstellen
   - Minimal API Pattern: `app.MapGet/MapPost/...`
   - `.WithOpenApi()` annotieren
   - Passende Status-Codes zurückgeben (200, 201, 204, 404, etc.)

3. **DTOs erstellen** (falls nötig):
   - Request-DTO in `Workflow.ApiService/Dtos/`
   - Response-DTO in `Workflow.ApiService/Dtos/`
   - Records verwenden wo möglich

4. **Endpoint registrieren**: In `Program.cs` die Endpoint-Mapping-Extension aufrufen (falls neue Datei).

5. **Bruno-Request erstellen**: Passenden `.bru` Request in `bruno/` anlegen:
   - Richtiger Ordner (workflows/, instances/, tasks/, etc.)
   - URL mit `{{baseUrl}}` Variable
   - Beispiel-Body bei POST/PUT

6. **Build**: `dotnet build Workflow.sln`

## Konventionen

- Endpoint-Dateien: `{Bereich}Endpoints.cs` (z.B. `WorkflowEndpoints.cs`)
- Extension Methods: `Map{Bereich}Endpoints(this WebApplication app)`
- DTOs: Records mit `required` Properties wo nötig
- Alle Endpoints mit `.WithOpenApi()` annotieren
- Async Handler mit CancellationToken
