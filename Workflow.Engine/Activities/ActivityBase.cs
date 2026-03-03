namespace Workflow.Engine.Activities;

public abstract class ActivityBase
{
    public abstract string Type { get; }

    public abstract Task<ActivityResult> ExecuteAsync(
        ActivityContext context,
        CancellationToken cancellationToken = default);
}
