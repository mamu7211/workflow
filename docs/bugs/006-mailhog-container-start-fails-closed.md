# MailHog Container startet nicht (Podman short-name)

## Status

Status: closed
Detected: 2026-03-05
Closed: 2026-03-06T00:00:00Z

## Steps to reproduce the bug

1. `dotnet run --project Workflow.AppHost` ausfuehren
2. MailHog-Container wird gestartet mit Image `mailhog/mailhog:latest`
3. Podman versucht das Image zu pullen

## Observed behavior

Container-Start schlaegt fehl mit:

```
Error: short-name "mailhog/mailhog:latest" did not resolve to an alias and no unqualified-search registries are defined in "/etc/containers/registries.conf"
```

Podman kann den Image-Namen `mailhog/mailhog` nicht aufloesen, weil keine `unqualified-search-registries` in der Podman-Konfiguration definiert sind. Docker loest solche Short-Names automatisch gegen `docker.io` auf, Podman nicht.

## Expected outcome

Der MailHog-Container startet fehlerfrei und ist auf Port 8025 (UI) und 1025 (SMTP) erreichbar.

## Fix

In `Workflow.AppHost/AppHost.cs` Zeile 12 den Image-Namen vollqualifizieren:

```csharp
// Vorher:
var mailhog = builder.AddContainer("mailhog", "mailhog/mailhog", "latest")

// Nachher:
var mailhog = builder.AddContainer("mailhog", "docker.io/mailhog/mailhog", "latest")
```

Damit wird die Docker Hub Registry explizit angegeben und Podman kann das Image korrekt pullen.
