using System.Text;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace Workflow.Engine.Activities;

public sealed class RabbitMqPublishActivity : ActivityBase
{
    public override string Type => "RabbitMqPublish";

    public override async Task<ActivityResult> ExecuteAsync(ActivityContext context, CancellationToken cancellationToken = default)
    {
        var exchange = context.GetProperty<string>("exchange") ?? string.Empty;
        var routingKey = context.GetProperty<string>("routingKey") ?? string.Empty;
        var message = context.GetProperty<string>("message") ?? string.Empty;

        try
        {
            var connection = context.ServiceProvider.GetRequiredService<IConnection>();
            await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

            var body = Encoding.UTF8.GetBytes(message);
            await channel.BasicPublishAsync(exchange, routingKey, body, cancellationToken);

            return ActivityResult.Completed();
        }
        catch (Exception ex)
        {
            return ActivityResult.Faulted($"Failed to publish message: {ex.Message}");
        }
    }
}
