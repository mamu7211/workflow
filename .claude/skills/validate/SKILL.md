---
name: validate
description: Baut die Solution, führt alle Tests aus und gibt einen Statusbericht. Verwende diesen Skill um den Gesamtzustand des Projekts zu prüfen.
disable-model-invocation: true
---

# Projekt validieren

Baue die Solution und führe alle Tests aus.

## Ablauf

1. **Build**: `dotnet build Workflow.sln`
   - Fehler auflisten und kategorisieren
   - Warnings auflisten

2. **Tests**: `dotnet test --no-build --verbosity normal`
   - Ergebnisse zusammenfassen: Bestanden / Fehlgeschlagen / Übersprungen
   - Fehlgeschlagene Tests mit Fehlermeldung auflisten

3. **Statusbericht**: Gib eine übersichtliche Zusammenfassung:
   ```
   Build:    ✓ Erfolgreich (0 Errors, X Warnings)
   Tests:    ✓ X bestanden, 0 fehlgeschlagen
   Projekte: 9 in Solution
   ```

4. **Bei Fehlern**: Schlage konkrete Fixes vor, implementiere sie aber NICHT automatisch.

## Regeln

- Nur lesen und ausführen, nichts ändern
- Gib eine klare, knappe Zusammenfassung
- Bei vielen Warnings: gruppiere sie nach Kategorie
