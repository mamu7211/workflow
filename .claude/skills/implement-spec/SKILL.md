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

1. **Git-Status prüfen**: Führe `git status --short` aus.
   - Wenn uncommittete Änderungen vorhanden sind: **Stoppe sofort** und weise den Benutzer darauf hin. Frage, ob die Änderungen committed, gestasht oder verworfen werden sollen, bevor du weitermachst.
   - Wenn der Working Tree sauber ist: Fahre fort.

2. **Feature-Branch erstellen**: Bestimme den Branch-Namen aus der Spec-Nummer (z.B. Spec `01-engine-core.md` → Branch `feature/01-engine-core`).
   - Führe aus: `git checkout master && git checkout -b feature/<spec-name>`
   - Wenn der Branch bereits existiert: `git checkout feature/<spec-name>` und informiere den Benutzer, dass der Branch bereits existiert (ggf. Fortsetzung einer früheren Session).

3. **Spec lesen**: Lies die Spezifikation aus `docs/specs/$0`. Wenn nur eine Nummer angegeben wurde, finde die passende Datei (z.B. `1` → `01-engine-core.md`).

4. **CLAUDE.md lesen**: Lies `CLAUDE.md` für Projektkonventionen.

5. **Design lesen**: Lies `docs/design.md` für Architekturkontext.

6. **Progress prüfen**: Lies `docs/specs/progress.md` um den aktuellen Stand zu verstehen und zu wissen, welche Abhängigkeiten bereits implementiert sind.

7. **Bestehenden Code prüfen**: Lies die relevanten existierenden Dateien, um Konflikte zu vermeiden und bestehende Patterns wiederzuverwenden.

8. **Implementieren**: Setze die Spezifikation Schritt für Schritt um:
   - Erstelle alle in der Spec genannten Dateien
   - Halte dich an die Dateistruktur aus der Spec
   - Verwende die Code-Beispiele als Vorlage, nicht als Copy-Paste
   - Beachte die Konventionen aus CLAUDE.md

9. **Tests schreiben**: Implementiere alle in der Spec genannten Tests:
   - Verwende MSTest (`[TestClass]`, `[TestMethod]`)
   - Teste alle in der Spec genannten Szenarien
   - Tests müssen grün sein

10. **Build verifizieren**: Führe `dotnet build Workflow.sln` aus und behebe alle Fehler.

11. **Tests ausführen**: Führe `dotnet test` aus und behebe alle fehlschlagenden Tests.

## Regeln

- Implementiere NUR was in der Spezifikation steht
- Weiche nicht von der Spec ab ohne triftigen Grund
- Wenn du von der Spec abweichen musst, dokumentiere den Grund
- Erstelle keine Dateien, die nicht in der Spec genannt sind
- Aktualisiere NICHT die progress.md (das macht der /update-progress Skill)
- Committe NICHT automatisch (der Benutzer entscheidet wann)
