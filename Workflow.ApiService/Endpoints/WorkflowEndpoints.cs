using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Workflow.ApiService.Data;
using Workflow.ApiService.Data.Entities;
using Workflow.ApiService.Dtos;
using Workflow.Engine.Execution;
using Workflow.Engine.Models;
using Workflow.Engine.Serialization;

namespace Workflow.ApiService.Endpoints;

public static class WorkflowEndpoints
{
    public static void MapWorkflowEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/workflows").WithOpenApi();

        group.MapGet("/", GetAll);
        group.MapGet("/{id}", GetById);
        group.MapPost("/", Create);
        group.MapPut("/{id}", Update);
        group.MapDelete("/{id}", Delete);
        group.MapPost("/{id}/publish", Publish);
        group.MapPost("/{id}/start", Start);
    }

    internal static async Task<IResult> GetAll(WorkflowDbContext db)
    {
        var entities = await db.WorkflowDefinitions
            .AsNoTracking()
            .OrderByDescending(e => e.UpdatedAt)
            .ToListAsync();

        return Results.Ok(entities.Select(ToDto));
    }

    internal static async Task<IResult> GetById(string id, WorkflowDbContext db)
    {
        var entity = await db.WorkflowDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);

        return entity is null ? Results.NotFound() : Results.Ok(ToDto(entity));
    }

    internal static async Task<IResult> Create(CreateWorkflowDto dto, WorkflowDbContext db)
    {
        var definition = new WorkflowDefinition
        {
            Name = dto.Name,
            Description = dto.Description
        };

        // Deserialize the provided definition JSON to merge properties
        if (dto.Definition.ValueKind != JsonValueKind.Undefined)
        {
            var parsed = dto.Definition.Deserialize<WorkflowDefinition>(WorkflowJsonConverter.CreateOptions());
            if (parsed is not null)
            {
                definition.Activities = parsed.Activities;
                definition.Connections = parsed.Connections;
                definition.Variables = parsed.Variables;
            }
        }

        var entity = new WorkflowDefinitionEntity
        {
            Id = definition.Id,
            Name = dto.Name,
            Description = dto.Description,
            Version = definition.Version,
            DefinitionJson = WorkflowJsonConverter.Serialize(definition)
        };

        db.WorkflowDefinitions.Add(entity);
        await db.SaveChangesAsync();

        return Results.Created($"/api/workflows/{entity.Id}", ToDto(entity));
    }

    internal static async Task<IResult> Update(string id, UpdateWorkflowDto dto, WorkflowDbContext db)
    {
        var entity = await db.WorkflowDefinitions.FirstOrDefaultAsync(e => e.Id == id);
        if (entity is null)
            return Results.NotFound();

        var definition = WorkflowJsonConverter.DeserializeDefinition(entity.DefinitionJson);
        if (definition is null)
            return Results.Problem("Corrupted workflow definition.");

        definition.Name = dto.Name;
        definition.Description = dto.Description;

        if (dto.Definition.ValueKind != JsonValueKind.Undefined)
        {
            var parsed = dto.Definition.Deserialize<WorkflowDefinition>(WorkflowJsonConverter.CreateOptions());
            if (parsed is not null)
            {
                definition.Activities = parsed.Activities;
                definition.Connections = parsed.Connections;
                definition.Variables = parsed.Variables;
            }
        }

        definition.Version++;
        entity.Name = dto.Name;
        entity.Description = dto.Description;
        entity.Version = definition.Version;
        entity.DefinitionJson = WorkflowJsonConverter.Serialize(definition);
        entity.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Results.Ok(ToDto(entity));
    }

    internal static async Task<IResult> Delete(string id, WorkflowDbContext db)
    {
        var entity = await db.WorkflowDefinitions.FirstOrDefaultAsync(e => e.Id == id);
        if (entity is null)
            return Results.NotFound();

        db.WorkflowDefinitions.Remove(entity);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    internal static async Task<IResult> Publish(string id, WorkflowDbContext db)
    {
        var entity = await db.WorkflowDefinitions.FirstOrDefaultAsync(e => e.Id == id);
        if (entity is null)
            return Results.NotFound();

        entity.IsPublished = true;
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Results.Ok(ToDto(entity));
    }

    internal static async Task<IResult> Start(
        string id,
        StartWorkflowDto? dto,
        WorkflowDbContext db,
        WorkflowExecutionEngine engine,
        IWorkflowInstanceStore store)
    {
        var entity = await db.WorkflowDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);

        if (entity is null)
            return Results.NotFound();

        var definition = WorkflowJsonConverter.DeserializeDefinition(entity.DefinitionJson);
        if (definition is null)
            return Results.Problem("Corrupted workflow definition.");

        var instance = await engine.StartAsync(definition, dto?.Variables);
        await store.SaveAsync(instance);

        // Log activity executions
        await LogActivityExecutions(db, instance, definition);

        return Results.Created($"/api/instances/{instance.Id}", ToInstanceDto(instance));
    }

    internal static async Task LogActivityExecutions(
        WorkflowDbContext db,
        WorkflowInstance instance,
        WorkflowDefinition definition)
    {
        var activityLookup = definition.Activities.ToDictionary(a => a.Id);

        foreach (var (activityId, state) in instance.ActivityStates)
        {
            if (state.Status is ActivityExecutionStatus.Pending)
                continue;

            var activityType = activityLookup.TryGetValue(activityId, out var node) ? node.Type : "Unknown";

            db.ActivityExecutionLogs.Add(new ActivityExecutionLogEntity
            {
                WorkflowInstanceId = instance.Id,
                ActivityId = activityId,
                ActivityType = activityType,
                Status = state.Status.ToString(),
                OutputJson = state.Output.Count > 0
                    ? JsonSerializer.Serialize(state.Output)
                    : null,
                Error = state.Error,
                StartedAt = state.StartedAt ?? instance.CreatedAt,
                CompletedAt = state.CompletedAt
            });
        }

        await db.SaveChangesAsync();
    }

    internal static WorkflowDefinitionDto ToDto(WorkflowDefinitionEntity entity) =>
        new(entity.Id,
            entity.Name,
            entity.Description,
            entity.Version,
            JsonDocument.Parse(entity.DefinitionJson).RootElement,
            entity.IsPublished,
            entity.CreatedAt,
            entity.UpdatedAt);

    internal static WorkflowInstanceDto ToInstanceDto(WorkflowInstance instance) =>
        new(instance.Id,
            instance.WorkflowDefinitionId,
            instance.WorkflowVersion,
            instance.Status.ToString(),
            JsonDocument.Parse(WorkflowJsonConverter.Serialize(instance)).RootElement,
            instance.CreatedAt,
            instance.CompletedAt,
            instance.Error);
}
