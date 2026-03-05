namespace Workflow.Engine.Activities;

public sealed class WebhookTriggerActivity : ActivityBase
{
    public override string Type => "WebhookTrigger";

    public override Task<ActivityResult> ExecuteAsync(ActivityContext context, CancellationToken cancellationToken = default)
    {
        var path = context.GetProperty<string>("path");
        var expectedMethod = context.GetProperty<string>("expectedMethod") ?? "POST";

        // Generate correlation ID if path not provided
        var correlationId = !string.IsNullOrEmpty(path) ? path : Guid.NewGuid().ToString();

        return Task.FromResult(ActivityResult.SuspendExecution(
            $"Waiting for webhook: {correlationId}",
            new Dictionary<string, object?>
            {
                ["correlationId"] = correlationId,
                ["expectedMethod"] = expectedMethod
            }));
    }
}
