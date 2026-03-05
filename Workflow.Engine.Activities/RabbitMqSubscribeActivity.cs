namespace Workflow.Engine.Activities;

public sealed class RabbitMqSubscribeActivity : ActivityBase
{
    public override string Type => "RabbitMqSubscribe";

    public override Task<ActivityResult> ExecuteAsync(ActivityContext context, CancellationToken cancellationToken = default)
    {
        var queueName = context.GetProperty<string>("queueName");

        if (string.IsNullOrEmpty(queueName))
            return Task.FromResult(ActivityResult.Faulted("Property 'queueName' is required."));

        var timeout = context.GetProperty<string>("timeout");

        return Task.FromResult(ActivityResult.SuspendExecution(
            $"Waiting for message on queue: {queueName}",
            new Dictionary<string, object?>
            {
                ["queueName"] = queueName,
                ["timeout"] = timeout
            }));
    }
}
