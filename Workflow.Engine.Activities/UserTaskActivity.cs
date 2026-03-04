namespace Workflow.Engine.Activities;

public sealed class UserTaskActivity : ActivityBase
{
    public override string Type => "UserTask";

    public override Task<ActivityResult> ExecuteAsync(ActivityContext context, CancellationToken cancellationToken = default)
    {
        var title = context.GetProperty<string>("title") ?? "Untitled Task";
        var description = context.GetProperty<string>("description");
        var assignedTo = context.GetProperty<string>("assignedTo");

        return Task.FromResult(ActivityResult.SuspendExecution(
            $"Waiting for user task: {title}",
            new Dictionary<string, object?>
            {
                ["title"] = title,
                ["description"] = description,
                ["assignedTo"] = assignedTo
            }));
    }
}
