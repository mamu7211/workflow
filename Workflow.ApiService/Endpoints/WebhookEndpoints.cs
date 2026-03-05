using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Workflow.ApiService.Data;
using Workflow.Engine.Execution;
using Workflow.Engine.Models;
using Workflow.Engine.Serialization;

namespace Workflow.ApiService.Endpoints;

public static class WebhookEndpoints
{
    public static void MapWebhookEndpoints(this WebApplication app)
    {
        app.MapPost("/api/webhooks/{correlationId}", HandleWebhook).WithOpenApi();
    }

    private static async Task<IResult> HandleWebhook(
        string correlationId,
        JsonElement body,
        WorkflowDbContext db,
        WorkflowExecutionEngine engine,
        IWorkflowInstanceStore store)
    {
        // Find suspended instances with a WebhookTrigger activity waiting for this correlationId
        var suspendedEntities = await db.WorkflowInstances
            .Where(e => e.Status == "Suspended")
            .ToListAsync();

        foreach (var entity in suspendedEntities)
        {
            var instance = WorkflowJsonConverter.DeserializeInstance(entity.StateJson);
            if (instance is null)
                continue;

            // Find suspended WebhookTrigger activity with matching correlationId
            var webhookActivity = instance.ActivityStates.Values
                .FirstOrDefault(s => s.Status == ActivityExecutionStatus.Suspended
                    && s.Output.TryGetValue("correlationId", out var cid)
                    && cid?.ToString() == correlationId);

            if (webhookActivity is null)
                continue;

            var defEntity = await db.WorkflowDefinitions
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == instance.WorkflowDefinitionId);

            if (defEntity is null)
                continue;

            var definition = WorkflowJsonConverter.DeserializeDefinition(defEntity.DefinitionJson);
            if (definition is null)
                continue;

            var resumeData = new Dictionary<string, object?>
            {
                ["webhookPayload"] = body
            };

            instance = await engine.ResumeAsync(
                definition, instance, webhookActivity.ActivityId, resumeData);

            await store.SaveAsync(instance);
            await WorkflowEndpoints.LogActivityExecutions(db, instance, definition);

            return Results.Ok();
        }

        return Results.NotFound();
    }
}
