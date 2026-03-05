using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Workflow.ApiService.Data;
using Workflow.ApiService.Endpoints;
using Workflow.Engine.Activities;
using Workflow.Engine.Execution;
using Workflow.Engine.Expressions;
using Workflow.Engine.Models;
using Workflow.Engine.Serialization;

namespace Workflow.ApiService.Services;

public sealed class DelayResumeService(
    IServiceScopeFactory scopeFactory,
    ILogger<DelayResumeService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndResumeDelaysAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error checking delayed workflows");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task CheckAndResumeDelaysAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WorkflowDbContext>();

        var suspendedEntities = await db.WorkflowInstances
            .Where(e => e.Status == "Suspended")
            .ToListAsync(ct);

        foreach (var entity in suspendedEntities)
        {
            var instance = WorkflowJsonConverter.DeserializeInstance(entity.StateJson);
            if (instance is null)
                continue;

            // Find suspended Delay activities with expired timers
            var delayActivity = instance.ActivityStates.Values
                .FirstOrDefault(s => s.Status == ActivityExecutionStatus.Suspended
                    && s.Output.TryGetValue("resumeAt", out var resumeAtObj)
                    && TryGetResumeTime(resumeAtObj, out var resumeAt)
                    && resumeAt <= DateTime.UtcNow);

            if (delayActivity is null)
                continue;

            logger.LogInformation(
                "Resuming delayed workflow {InstanceId}, activity {ActivityId}",
                instance.Id, delayActivity.ActivityId);

            var defEntity = await db.WorkflowDefinitions
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == instance.WorkflowDefinitionId, ct);

            if (defEntity is null)
                continue;

            var definition = WorkflowJsonConverter.DeserializeDefinition(defEntity.DefinitionJson);
            if (definition is null)
                continue;

            var registry = scope.ServiceProvider.GetRequiredService<ActivityRegistry>();
            var expressionEvaluator = scope.ServiceProvider.GetRequiredService<IExpressionEvaluator>();
            var engine = new WorkflowExecutionEngine(registry, expressionEvaluator, scope.ServiceProvider);

            instance = await engine.ResumeAsync(
                definition, instance, delayActivity.ActivityId, cancellationToken: ct);

            var store = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceStore>();
            await store.SaveAsync(instance, ct);
            await WorkflowEndpoints.LogActivityExecutions(db, instance, definition);
        }
    }

    private static bool TryGetResumeTime(object? value, out DateTime resumeAt)
    {
        resumeAt = default;

        if (value is DateTime dt)
        {
            resumeAt = dt;
            return true;
        }

        if (value is JsonElement element)
        {
            if (element.TryGetDateTime(out dt))
            {
                resumeAt = dt;
                return true;
            }
        }

        if (value is string str && DateTime.TryParse(str, out dt))
        {
            resumeAt = dt;
            return true;
        }

        return false;
    }
}
