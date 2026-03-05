# Connection-Rendering: Preview-Linie gerade + Pfeilspitze im Connector

## Status

Status: closed
Detected: 2026-03-05
Closed: 2026-03-05

## Steps to reproduce the bug

1. Workflow Designer oeffnen mit mindestens zwei Nodes
2. Vom Output-Port einer Node eine neue Connection ziehen (Drag) -> Preview-Linie beobachten
3. Eine bestehende Connection zwischen zwei Nodes betrachten -> Pfeilspitze am Target-Connector beobachten

## Observed behavior

Zwei Darstellungsfehler bei Connections im Designer:

1. **Preview-Linie ist gerade:** Beim Ziehen einer neuen Connection wird eine gerade gestrichelte Linie (`<line>`) gerendert statt einer Bezier-Kurve. Die Linie endet ausserdem nicht exakt am Maus-Cursor, sondern an einer festen Offset-Position (`source.X + 200, source.Y + 30`), die per Delta-Tracking verschoben wird.

2. **Pfeilspitze verschwindet im Connector:** Bei bestehenden Connections endet der Bezier-Pfad direkt am Input-Connector (`tx = TargetX, ty = TargetY + 30`). Die Pfeilspitze (SVG `marker-end`, 10px breit) wird dadurch vom Connector-Kreis verdeckt, statt sichtbar davor zu enden.

## Expected outcome

1. Die Preview-Linie soll als gestrichelte Bezier-Kurve dargestellt werden (identische Kurvenform wie bestehende Connections) und exakt am Maus-Cursor enden.
2. Die Pfeilspitze bestehender Connections soll sichtbar vor dem Input-Connector enden, nicht dahinter verschwinden.

## Fix

### Fix 1: Preview-Linie als Bezier-Kurve

In `WorkflowCanvas.razor` (Zeilen 33-38): Das `<line>`-Element durch ein `<path>`-Element mit derselben Bezier-Formel wie in `ConnectionLine.razor` ersetzen:

- Source-Punkt: `(sourceNode.X + 140, sourceNode.Y + 30)` (Output-Port)
- Target-Punkt: `(_connectTargetX, _connectTargetY)` (Maus-Position)
- Kubische Bezier-Kontrollpunkte: `dx = Math.Abs(tx - sx) * 0.5`
- `stroke-dasharray="5,5"` beibehalten fuer gestrichelte Darstellung

### Fix 2: Pfeilspitze vor dem Connector

In `ConnectionLine.razor` (Zeile 10): Den Pfad-Endpunkt um die Marker-Breite (10px) verkuerzen, damit die Pfeilspitze vor dem Connector-Kreis sichtbar bleibt:

- Endpunkt von `tx` auf `tx + 10` aendern (10px Abstand zum Connector)
- Alternativ: `refX` im Marker anpassen, damit der Pfeil relativ zum Pfadende korrekt positioniert wird
