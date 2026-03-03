---
name: update-progress
description: Aktualisiert docs/specs/progress.md basierend auf dem aktuellen Implementierungsstand. Prüft welche Dateien existieren und welche Tests grün sind.
disable-model-invocation: true
---

# Fortschritt aktualisieren

Aktualisiere `docs/specs/progress.md` basierend auf dem tatsächlichen Implementierungsstand.

## Ablauf

1. **Progress lesen**: Lies die aktuelle `docs/specs/progress.md`.

2. **Implementierungsstand prüfen**: Für jede Phase und jeden Task:
   - Prüfe ob die genannten Dateien existieren (via Glob)
   - Prüfe ob die Dateien nicht leer sind (via Read)
   - Prüfe ob relevante Tests existieren

3. **Build ausführen**: `dotnet build Workflow.sln` - Merke ob es Fehler gibt.

4. **Tests ausführen**: `dotnet test --no-build` - Merke welche Tests grün/rot sind.

5. **Status aktualisieren**: Aktualisiere die Status-Spalte in progress.md:
   - `:white_check_mark:` - Datei existiert, nicht leer, Tests grün
   - `:construction:` - Datei existiert, aber Tests fehlen oder sind rot
   - `:white_large_square:` - Datei existiert noch nicht
   - `:x:` - Blockiert (Build-Fehler, fehlende Abhängigkeit)

6. **Notizen ergänzen**: Füge hilfreiche Notizen hinzu (z.B. Testanzahl, bekannte Issues).

7. **Änderungslog**: Füge einen Eintrag im Änderungslog am Ende der Datei hinzu mit dem heutigen Datum.

## Regeln

- Ändere NUR `docs/specs/progress.md`
- Ändere keine anderen Dateien
- Sei ehrlich über den Status - markiere nichts als abgeschlossen, das nicht vollständig funktioniert
- Committe NICHT automatisch
