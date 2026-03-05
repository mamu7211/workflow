using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Workflow.ApiService.Data;
using Workflow.ApiService.Endpoints;
using Workflow.Engine.Activities;
using Workflow.Engine.Execution;
using Workflow.Engine.Expressions;
using Workflow.Engine.Models;
using Workflow.Engine.Serialization;

namespace Workflow.ApiService.Services;

public sealed class RabbitMqListenerService(
    IServiceScopeFactory scopeFactory,
    IConnection connection,
    ILogger<RabbitMqListenerService> logger) : BackgroundService
{
    private readonly HashSet<string> _activeQueues = [];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckForNewSubscriptionsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error checking RabbitMQ subscriptions");
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }

    private async Task CheckForNewSubscriptionsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WorkflowDbContext>();

        var suspendedEntities = await db.WorkflowInstances
            .AsNoTracking()
            .Where(e => e.Status == "Suspended")
            .ToListAsync(ct);

        foreach (var entity in suspendedEntities)
        {
            var instance = WorkflowJsonConverter.DeserializeInstance(entity.StateJson);
            if (instance is null)
                continue;

            var subscribeActivity = instance.ActivityStates.Values
                .FirstOrDefault(s => s.Status == ActivityExecutionStatus.Suspended
                    && s.Output.TryGetValue("queue", out var q)
                    && q is not null);

            if (subscribeActivity is null)
                continue;

            var queueName = subscribeActivity.Output["queue"]?.ToString();
            if (string.IsNullOrEmpty(queueName) || _activeQueues.Contains($"{instance.Id}:{queueName}"))
                continue;

            _activeQueues.Add($"{instance.Id}:{queueName}");

            _ = StartConsumerAsync(
                instance.Id,
                instance.WorkflowDefinitionId,
                subscribeActivity.ActivityId,
                queueName,
                ct);
        }
    }

    private async Task StartConsumerAsync(
        string instanceId,
        string definitionId,
        string activityId,
        string queueName,
        CancellationToken ct)
    {
        try
        {
            var channel = await connection.CreateChannelAsync(cancellationToken: ct);
            await channel.QueueDeclareAsync(queueName, durable: true, exclusive: false,
                autoDelete: false, cancellationToken: ct);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                try
                {
                    var messageBody = Encoding.UTF8.GetString(ea.Body.ToArray());

                    logger.LogInformation(
                        "RabbitMQ message received on queue {Queue} for workflow {InstanceId}",
                        queueName, instanceId);

                    using var scope = scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<WorkflowDbContext>();

                    var instanceEntity = await db.WorkflowInstances
                        .FirstOrDefaultAsync(e => e.Id == instanceId);

                    if (instanceEntity is null)
                        return;

                    var instance = WorkflowJsonConverter.DeserializeInstance(instanceEntity.StateJson);
                    if (instance is null)
                        return;

                    var defEntity = await db.WorkflowDefinitions
                        .AsNoTracking()
                        .FirstOrDefaultAsync(e => e.Id == definitionId);

                    if (defEntity is null)
                        return;

                    var definition = WorkflowJsonConverter.DeserializeDefinition(defEntity.DefinitionJson);
                    if (definition is null)
                        return;

                    var resumeData = new Dictionary<string, object?>
                    {
                        ["rabbitMqMessage"] = messageBody
                    };

                    var registry = scope.ServiceProvider.GetRequiredService<ActivityRegistry>();
                    var expressionEvaluator = scope.ServiceProvider.GetRequiredService<IExpressionEvaluator>();
                    var engine = new WorkflowExecutionEngine(registry, expressionEvaluator, scope.ServiceProvider);

                    instance = await engine.ResumeAsync(
                        definition, instance, activityId, resumeData);

                    var store = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceStore>();
                    await store.SaveAsync(instance);
                    await WorkflowEndpoints.LogActivityExecutions(db, instance, definition);

                    await channel.BasicAckAsync(ea.DeliveryTag, false);
                    _activeQueues.Remove($"{instanceId}:{queueName}");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing RabbitMQ message for workflow {InstanceId}", instanceId);
                }
            };

            await channel.BasicConsumeAsync(queueName, autoAck: false, consumer, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error starting consumer on queue {Queue}", queueName);
            _activeQueues.Remove($"{instanceId}:{queueName}");
        }
    }
}
