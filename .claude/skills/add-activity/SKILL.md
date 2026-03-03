---
name: add-activity
description: Erstellt eine neue Activity für die Workflow Engine. Scaffolding für Activity-Klasse, Registrierung, Tests, Bruno-Request und Property-Editor-Felder.
disable-model-invocation: true
---

# Neue Activity erstellen

Erstelle eine neue Activity für die Workflow Engine.

## Argumente

- `$0`: Activity-Name (z.B. `SendSlackMessage`, `FileUpload`)
- `$1`: (optional) Kurzbeschreibung der Activity

## Ablauf

1. **Bestehende Activities lesen**: Lies eine existierende Activity aus `Workflow.Engine.Activities/` als Referenz für das Pattern.

2. **Activity-Klasse erstellen**: `Workflow.Engine.Activities/{$0}Activity.cs`
   - Erbt von `ActivityBase`
   - `Type` Property gibt den Namen zurück
   - `ExecuteAsync` implementiert die Logik
   - Properties über `ActivityContext.GetProperty<T>()` lesen
   - Ergebnis über `ActivityResult.Completed()` / `.Faulted()` / `.SuspendExecution()` zurückgeben

3. **Registrierung**: In `ServiceCollectionExtensions.cs` die neue Activity im `AddBuiltInActivities()` registrieren.

4. **Tests erstellen**: `Workflow.Engine.Tests/Activities/{$0}ActivityTests.cs`
   - MSTest mit `[TestClass]` / `[TestMethod]`
   - Mindestens: Erfolgsfall, Fehlerfall, Property-Validierung

5. **Bruno-Request** (wenn die Activity über einen Endpoint getriggert wird):
   - Passenden Request in `bruno/` anlegen oder bestehenden erweitern

6. **Spec aktualisieren**: Ergänze die Activity in `docs/specs/02-activities.md` in der Activity-Tabelle.

7. **Build + Test**: `dotnet build Workflow.sln && dotnet test`

## Konventionen

- Activity-Klasse: `{Name}Activity.cs`
- Type-String: `{Name}` (ohne "Activity" Suffix)
- Namespace: `Workflow.Engine.Activities`
- Properties als Dictionary<string, object?> aus dem ActivityContext lesen
- Externe Abhängigkeiten über IServiceProvider aus dem ActivityContext
