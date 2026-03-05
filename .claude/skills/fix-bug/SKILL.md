---
name: fix-bug
description: Fixt einen offenen Bug aus docs/bugs/. Liest das Bug-Dokument, implementiert den Fix gemaess dem Fix-Plan, aktualisiert den Bug-Status und benennt die Datei um.
---

# Bug fixen

Fixe einen Bug basierend auf seinem Bug-Dokument in `docs/bugs/`.

## Argumente

- `$0`: Bug-Nummer (z.B. `001` oder `1`)

## Ablauf

### 1. Bug-Dokument finden

Suche in `docs/bugs/` nach einer Datei, deren Name mit der angegebenen Nummer beginnt (z.B. `1` → `001-*`). Die Nummer wird auf drei Stellen mit fuehrenden Nullen aufgefuellt. Wenn keine passende Datei gefunden wird, informiere den User.

### 2. Bug-Dokument lesen und pruefen

- Lies das Bug-Dokument.
- Pruefe den Status: Nur Bugs mit Status `open` oder `blocked` koennen gefixt werden.
- Wenn der Status `closed` oder `nofix` ist, informiere den User und brich ab.

### 3. Betroffenen Code untersuchen

- Lies den im Bug-Dokument referenzierten Code.
- Verstehe den Fix-Plan aus der `## Fix` Sektion.
- Lies angrenzenden Code, um den Kontext zu verstehen und Seiteneffekte zu vermeiden.

### 4. Fix implementieren

- Setze den Fix-Plan Schritt fuer Schritt um.
- Halte dich an die bestehenden Code-Konventionen (siehe `CLAUDE.md`).
- Aendere nur was noetig ist - keine unnoetige Refactorings.

### 5. Build und Tests

- Fuehre `dotnet build Workflow.sln` aus und behebe alle Build-Fehler.
- Fuehre `dotnet test` aus und behebe alle fehlschlagenden Tests.
- Wenn der Fix neue Testfaelle erfordert, schreibe diese.

### 6. Bug-Status aktualisieren

Wenn der Fix erfolgreich ist:

1. **Status im Dokument aendern:** Aendere `Status: open` zu `Status: closed` und fuege eine Zeile `Closed: <aktuelles ISO-Datum, YYYY-MM-DDThh-mm-ssZ>` hinzu.
2. **Datei umbenennen:** Benenne die Datei um, sodass der Status-Suffix im Dateinamen dem neuen Status entspricht:
   - `001-titel-open.md` → `001-titel-closed.md`
   - Das Muster ist: `<nummer>-<titel>-<status>.md`
3. Verwende `git mv` fuer die Umbenennung, damit git die Aenderung trackt.

### 7. Zusammenfassung

Zeige dem User:
- Welche Dateien geaendert wurden
- Was der Fix bewirkt
- Den neuen Dateinamen des Bug-Dokuments

## Status-Uebergaenge

Folgende Status-Werte sind gueltig: `open`, `closed`, `nofix`, `blocked`.

Beim Aendern des Status wird IMMER:
1. Der `Status:`-Wert im Dokument aktualisiert
2. Der Status-Suffix im Dateinamen synchronisiert (via `git mv`)

## Regeln

- Committe NICHT automatisch - der User entscheidet wann
- Fixe NUR den beschriebenen Bug, keine anderen Aenderungen
- Wenn der Fix-Plan unklar ist, frage den User bevor du loslegst
- Wenn der Fix nicht moeglich ist (z.B. wegen fehlender Abhaengigkeiten), setze den Status auf `blocked` statt `closed` und erklaere warum
