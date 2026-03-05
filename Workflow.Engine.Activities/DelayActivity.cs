namespace Workflow.Engine.Activities;

public sealed class DelayActivity : ActivityBase
{
    public override string Type => "Delay";

    public override Task<ActivityResult> ExecuteAsync(ActivityContext context, CancellationToken cancellationToken = default)
    {
        var durationStr = context.GetProperty<string>("duration");

        if (string.IsNullOrEmpty(durationStr) || !TimeSpan.TryParse(durationStr, out var duration))
            return Task.FromResult(ActivityResult.Faulted(
                "Property 'duration' is required and must be a valid TimeSpan format (e.g., '00:05:00')."));

        var resumeAt = DateTime.UtcNow.Add(duration);

        return Task.FromResult(ActivityResult.SuspendExecution(
            $"Delay until {resumeAt:O}",
            new Dictionary<string, object?> { ["resumeAt"] = resumeAt }));
    }
}
