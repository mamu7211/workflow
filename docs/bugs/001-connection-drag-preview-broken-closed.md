# Connection-Drag Vorschau-Linie funktioniert nicht korrekt

## Status

Status: closed
Detected: 2026-03-05T17-32-00Z
Closed: 2026-03-05T18-00-00Z

## Steps to reproduce the bug

1. Workflow Designer oeffnen mit mindestens zwei Nodes
2. Vom Output-Port einer Node eine neue Connection ziehen (Mousedown auf Output-Port)
3. Maus bewegen in Richtung Input-Port einer anderen Node
4. Beobachten: Die gestrichelte Vorschau-Linie folgt der Maus nicht
5. Mit der Maus auf den Input-Port einer anderen Node klicken
6. Beobachten: Die Ziel-Node geht in den Drag-Modus ueber, statt die Connection zu erstellen

Betroffener Code: `Workflow.Web/Components/Designer/WorkflowCanvas.razor`

## Observed behavior

- Die gestrichelte Vorschau-Linie (dashed line) bleibt an der initialen Position stehen und folgt der Mausbewegung nicht
- Beim Klick auf den Input-Port einer anderen Node wird statt der Connection die Node in den Drag-Modus versetzt
- Neue Connections koennen dadurch nicht per Drag erstellt werden

## Expected outcome

- Die gestrichelte Vorschau-Linie soll der Mausposition folgen waehrend des Drags
- Beim Loslassen ueber einem Input-Port soll die Connection erstellt werden
- Im Connection-Modus darf kein Node-Drag ausgeloest werden

## Fix

1. **Vorschau-Linie aktualisieren:** In `OnCanvasMouseMove` pruefen ob `_connecting == true` und dann `_connectTargetX`/`_connectTargetY` mit der aktuellen Mausposition aktualisieren (unter Beruecksichtigung des SVG-Koordinatensystems)
2. **Node-Drag im Connection-Modus verhindern:** In der Node-Drag-Logik (`OnNodeMouseDown` oder aehnlich) pruefen ob `_connecting == true` und in dem Fall keinen Node-Drag starten
3. **Connection-State bei Mouse-Up zuruecksetzen:** In `OnCanvasMouseUp` den Connection-State (`_connecting = false`, `_connectSourceNode = null`) zuruecksetzen, falls kein gueltiges Target getroffen wurde (Abbruch des Connection-Drags)
