using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Workflow.ApiService.Data;
using Workflow.ApiService.Dtos;
using Workflow.Engine.Execution;
using Workflow.Engine.Models;
using Workflow.Engine.Serialization;

namespace Workflow.ApiService.Endpoints;

public static class InstanceEndpoints
{
    public static void MapInstanceEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/instances").WithOpenApi();

        group.MapGet("/", GetAll);
        group.MapGet("/{id}", GetById);
        group.MapPost("/{id}/cancel", Cancel);
        group.MapPost("/{id}/resume", Resume);
        group.MapGet("/{id}/log", GetLog);
    }

    internal static async Task<IResult> GetAll(WorkflowDbContext db)
    {
        var entities = await db.WorkflowInstances
            .AsNoTracking()
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        return Results.Ok(entities.Select(e => ToDto(e)));
    }

    internal static async Task<IResult> GetById(string id, WorkflowDbContext db)
    {
        var entity = await db.WorkflowInstances
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);

        return entity is null ? Results.NotFound() : Results.Ok(ToDto(entity));
    }

    internal static async Task<IResult> Cancel(string id, WorkflowDbContext db)
    {
        var entity = await db.WorkflowInstances.FirstOrDefaultAsync(e => e.Id == id);
        if (entity is null)
            return Results.NotFound();

        var instance = WorkflowJsonConverter.DeserializeInstance(entity.StateJson);
        if (instance is null)
            return Results.Problem("Corrupted workflow instance.");

        instance.Status = WorkflowStatus.Cancelled;
        instance.CompletedAt = DateTime.UtcNow;

        entity.Status = instance.Status.ToString();
        entity.StateJson = WorkflowJsonConverter.Serialize(instance);
        entity.CompletedAt = instance.CompletedAt;

        await db.SaveChangesAsync();
        return Results.Ok(ToDto(entity));
    }

    internal static async Task<IResult> Resume(
        string id,
        ResumeWorkflowDto? dto,
        WorkflowDbContext db,
        WorkflowExecutionEngine engine,
        IWorkflowInstanceStore store)
    {
        var instanceEntity = await db.WorkflowInstances.FirstOrDefaultAsync(e => e.Id == id);
        if (instanceEntity is null)
            return Results.NotFound();

        var instance = WorkflowJsonConverter.DeserializeInstance(instanceEntity.StateJson);
        if (instance is null)
            return Results.Problem("Corrupted workflow instance.");

        if (instance.Status != WorkflowStatus.Suspended)
            return Results.BadRequest("Workflow is not suspended.");

        var defEntity = await db.WorkflowDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == instance.WorkflowDefinitionId);

        if (defEntity is null)
            return Results.Problem("Workflow definition not found.");

        var definition = WorkflowJsonConverter.DeserializeDefinition(defEntity.DefinitionJson);
        if (definition is null)
            return Results.Problem("Corrupted workflow definition.");

        instance = await engine.ResumeAsync(
            definition, instance, dto?.ActivityId, dto?.Variables);

        await store.SaveAsync(instance);
        await WorkflowEndpoints.LogActivityExecutions(db, instance, definition);

        return Results.Ok(WorkflowEndpoints.ToInstanceDto(instance));
    }

    internal static async Task<IResult> GetLog(string id, WorkflowDbContext db)
    {
        var logs = await db.ActivityExecutionLogs
            .AsNoTracking()
            .Where(e => e.WorkflowInstanceId == id)
            .OrderBy(e => e.StartedAt)
            .ToListAsync();

        return Results.Ok(logs.Select(e => new ActivityExecutionLogDto(
            e.Id,
            e.ActivityId,
            e.ActivityType,
            e.Status,
            e.InputJson is not null ? JsonDocument.Parse(e.InputJson).RootElement : null,
            e.OutputJson is not null ? JsonDocument.Parse(e.OutputJson).RootElement : null,
            e.Error,
            e.StartedAt,
            e.CompletedAt)));
    }

    internal static WorkflowInstanceDto ToDto(Data.Entities.WorkflowInstanceEntity entity) =>
        new(entity.Id,
            entity.WorkflowDefinitionId,
            entity.WorkflowVersion,
            entity.Status,
            JsonDocument.Parse(entity.StateJson).RootElement,
            entity.CreatedAt,
            entity.CompletedAt,
            entity.Error);
}
