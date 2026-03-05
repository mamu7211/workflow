---
name: create-bug
description: Erstellt einen Bug-Report. Fragt interaktiv nach Details zum Bug, erstellt ein Dokument in docs/bugs/ und schlaegt einen Fix-Plan vor.
---

# Bug-Report erstellen

Erstelle einen strukturierten Bug-Report in `docs/bugs/`.

## Ablauf

### 1. Naechste Bug-Nummer ermitteln

Lies die vorhandenen Dateien in `docs/bugs/` (ohne `bug-template.md`) und ermittle die naechste freie Nummer (dreistellig, z.B. `001`, `002`, ...).

### 2. Bug-Details erfragen

Frage den User nacheinander:

1. **Titel**: Kurzer, beschreibender Titel fuer den Bug (wird auch im Dateinamen verwendet).
2. **Reproduktion**: Was wurde getan, um den Bug auszuloesen? Welcher Teil des Codes ist betroffen? Welcher Input hat den Fehler verursacht?
3. **Beobachtetes Verhalten**: Was ist schiefgelaufen? Was passiert aktuell falsch?
4. **Erwartetes Verhalten**: Was haette stattdessen passieren sollen? Was muss verbessert werden?

Frage so lange nach, bis klar ist, was schiefgelaufen ist und was gefixt werden muss. Stelle Rueckfragen wenn die Beschreibung unklar oder unvollstaendig ist.

### 3. Code untersuchen und Fix-Plan erstellen

- Untersuche den betroffenen Code anhand der Beschreibung des Users.
- Erstelle einen groben Plan, wie der Bug gefixt werden kann.
- Bespreche den Fix-Plan kurz mit dem User.

### 4. Bug-Dokument erstellen

Erstelle die Datei `docs/bugs/<nummer>-<titel-kebab-case>-open.md` basierend auf dem Template `docs/bugs/bug-template.md`:

```markdown
# <Titel>

## Status

Status: open
Detected: <aktuelles ISO-Datum, YYYY-MM-DD>

## Steps to reproduce the bug

<Reproduktionsschritte vom User>

## Observed behavior

<Beobachtetes Verhalten vom User>

## Expected outcome

<Erwartetes Verhalten vom User>

## Fix

<Fix-Plan>
```

### 5. Zusammenfassung

Zeige dem User:
- Den Dateinamen des erstellten Bug-Reports
- Eine kurze Zusammenfassung des Bugs und des geplanten Fixes
