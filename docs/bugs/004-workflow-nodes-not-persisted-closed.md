# Workflow Nodes werden nicht gespeichert

## Status

Status: closed
Detected: 2026-03-05
Closed: 2026-03-05

## Steps to reproduce the bug

1. Blazor Designer oeffnen
2. Neuen Workflow anlegen (Name, Description)
3. Nodes im visuellen Editor hinzufuegen
4. Connections zwischen Nodes ziehen
5. Workflow speichern (Save-Button)
6. Workflow erneut laden (aus der Liste oeffnen oder Seite neu laden)
7. Nodes und Connections sind verschwunden, nur Name/Description sind erhalten

## Observed behavior

Der Workflow wird gespeichert, aber Activities, Connections und Variables gehen verloren. Beim erneuten Laden ist der Workflow leer (keine Nodes, keine Connections). Es werden keine Fehlermeldungen angezeigt - das Speichern scheint erfolgreich.

### Root Cause

JSON Property-Naming Mismatch zwischen Frontend und Backend:

- **Frontend** (`WorkflowDesigner.razor:107`) serialisiert mit `JsonSerializer.Serialize()` (default) und erzeugt **PascalCase**: `"Activities": [...]`
- **Backend** (`WorkflowEndpoints.cs:57`) deserialisiert mit `WorkflowJsonConverter.CreateOptions()` welches `PropertyNamingPolicy = JsonNamingPolicy.CamelCase` setzt und **camelCase** erwartet: `"activities": [...]`
- System.Text.Json matcht mit CamelCase-Policy case-sensitiv: `"Activities"` wird nicht als `"activities"` erkannt
- Resultat: `Activities` und `Connections` bleiben leere Listen und werden so in die Datenbank geschrieben

## Expected outcome

Nach dem Speichern und erneuten Laden eines Workflows muessen alle Nodes, Connections und Variables erhalten bleiben.

## Fix

Frontend soll beim Serialisieren die gleichen `JsonSerializerOptions` verwenden wie das Backend:

1. **`Workflow.Web/Pages/WorkflowDesigner.razor` (Zeile ~107):** `JsonSerializer.Serialize()` mit `WorkflowJsonConverter.CreateOptions()` aufrufen statt default Options
2. **`Workflow.Web/Services/DesignerStateService.cs`:** Pruefen ob dort ebenfalls Serialisierung mit default Options stattfindet und ggf. anpassen
3. **Test hinzufuegen:** Cross-Serialisierung testen (Frontend-Style serialize → Backend-Style deserialize → Nodes erhalten)
