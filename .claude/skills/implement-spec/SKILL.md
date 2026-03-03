---
name: implement-spec
description: Implementiert eine Spezifikation aus docs/specs/. Liest die Spec-Datei, implementiert den Code gemäß der Vorgaben und schreibt Tests. Verwende diesen Skill wenn du eine Phase des Projekts umsetzen willst.
disable-model-invocation: true
---

# Spezifikation implementieren

Implementiere die angegebene Spezifikation aus dem `docs/specs/` Ordner.

## Argumente

- `$0`: Spec-Dateiname oder Phasennummer (z.B. `01-engine-core.md` oder `01` oder `1`)

## Ablauf

1. **Spec lesen**: Lies die Spezifikation aus `docs/specs/$0`. Wenn nur eine Nummer angegeben wurde, finde die passende Datei (z.B. `1` → `01-engine-core.md`).

2. **CLAUDE.md lesen**: Lies `CLAUDE.md` für Projektkonventionen.

3. **Design lesen**: Lies `docs/design.md` für Architekturkontext.

4. **Progress prüfen**: Lies `docs/specs/progress.md` um den aktuellen Stand zu verstehen und zu wissen, welche Abhängigkeiten bereits implementiert sind.

5. **Bestehenden Code prüfen**: Lies die relevanten existierenden Dateien, um Konflikte zu vermeiden und bestehende Patterns wiederzuverwenden.

6. **Implementieren**: Setze die Spezifikation Schritt für Schritt um:
   - Erstelle alle in der Spec genannten Dateien
   - Halte dich an die Dateistruktur aus der Spec
   - Verwende die Code-Beispiele als Vorlage, nicht als Copy-Paste
   - Beachte die Konventionen aus CLAUDE.md

7. **Tests schreiben**: Implementiere alle in der Spec genannten Tests:
   - Verwende MSTest (`[TestClass]`, `[TestMethod]`)
   - Teste alle in der Spec genannten Szenarien
   - Tests müssen grün sein

8. **Build verifizieren**: Führe `dotnet build Workflow.sln` aus und behebe alle Fehler.

9. **Tests ausführen**: Führe `dotnet test` aus und behebe alle fehlschlagenden Tests.

## Regeln

- Implementiere NUR was in der Spezifikation steht
- Weiche nicht von der Spec ab ohne triftigen Grund
- Wenn du von der Spec abweichen musst, dokumentiere den Grund
- Erstelle keine Dateien, die nicht in der Spec genannt sind
- Aktualisiere NICHT die progress.md (das macht der /update-progress Skill)
- Committe NICHT automatisch (der Benutzer entscheidet wann)
