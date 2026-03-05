using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Workflow.Engine.Activities;

public sealed class LogActivity : ActivityBase
{
    public override string Type => "Log";

    public override Task<ActivityResult> ExecuteAsync(ActivityContext context, CancellationToken cancellationToken = default)
    {
        var message = context.GetProperty<string>("message") ?? string.Empty;
        var level = context.GetProperty<string>("level") ?? "Info";

        var loggerFactory = context.ServiceProvider.GetService<ILoggerFactory>();
        var logger = loggerFactory?.CreateLogger<LogActivity>();

        var logLevel = level.ToLowerInvariant() switch
        {
            "warning" or "warn" => LogLevel.Warning,
            "error" => LogLevel.Error,
            "debug" => LogLevel.Debug,
            "trace" => LogLevel.Trace,
            "critical" => LogLevel.Critical,
            _ => LogLevel.Information
        };

        logger?.Log(logLevel, "{Message}", message);

        return Task.FromResult(ActivityResult.Completed());
    }
}
