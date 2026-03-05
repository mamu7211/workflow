using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Workflow.ApiService.Data;
using Workflow.ApiService.Dtos;
using Workflow.Engine.Execution;
using Workflow.Engine.Models;
using Workflow.Engine.Serialization;

namespace Workflow.ApiService.Endpoints;

public static class UserTaskEndpoints
{
    public static void MapUserTaskEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/tasks").WithOpenApi();

        group.MapGet("/", GetAll);
        group.MapGet("/{id}", GetById);
        group.MapPost("/{id}/complete", Complete);
    }

    internal static async Task<IResult> GetAll(WorkflowDbContext db)
    {
        var entities = await db.UserTasks
            .AsNoTracking()
            .Where(e => e.Status == "Pending")
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        return Results.Ok(entities.Select(ToDto));
    }

    internal static async Task<IResult> GetById(string id, WorkflowDbContext db)
    {
        var entity = await db.UserTasks
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);

        return entity is null ? Results.NotFound() : Results.Ok(ToDto(entity));
    }

    internal static async Task<IResult> Complete(
        string id,
        CompleteTaskDto dto,
        WorkflowDbContext db,
        WorkflowExecutionEngine engine,
        IWorkflowInstanceStore store)
    {
        var task = await db.UserTasks.FirstOrDefaultAsync(e => e.Id == id);
        if (task is null)
            return Results.NotFound();

        if (task.Status != "Pending")
            return Results.BadRequest("Task is already completed.");

        task.Status = dto.Status;
        task.ResponseJson = dto.Data is not null
            ? JsonSerializer.Serialize(dto.Data)
            : null;
        task.CompletedAt = DateTime.UtcNow;

        // Resume the workflow
        var instanceEntity = await db.WorkflowInstances
            .FirstOrDefaultAsync(e => e.Id == task.WorkflowInstanceId);

        if (instanceEntity is not null)
        {
            var instance = WorkflowJsonConverter.DeserializeInstance(instanceEntity.StateJson);
            var defEntity = await db.WorkflowDefinitions
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == instance!.WorkflowDefinitionId);

            if (instance is not null && defEntity is not null)
            {
                var definition = WorkflowJsonConverter.DeserializeDefinition(defEntity.DefinitionJson);
                if (definition is not null)
                {
                    var resumeData = new Dictionary<string, object?>
                    {
                        [$"{task.ActivityId}_result"] = dto.Status
                    };

                    if (dto.Data is not null)
                    {
                        foreach (var (key, value) in dto.Data)
                            resumeData[key] = value;
                    }

                    instance = await engine.ResumeAsync(
                        definition, instance, task.ActivityId, resumeData);

                    await store.SaveAsync(instance);
                    await WorkflowEndpoints.LogActivityExecutions(db, instance, definition);
                }
            }
        }

        await db.SaveChangesAsync();
        return Results.Ok(ToDto(task));
    }

    internal static UserTaskDto ToDto(Data.Entities.UserTaskEntity entity) =>
        new(entity.Id,
            entity.WorkflowInstanceId,
            entity.ActivityId,
            entity.Title,
            entity.Description,
            entity.AssignedTo,
            entity.Status,
            entity.ResponseJson is not null
                ? JsonDocument.Parse(entity.ResponseJson).RootElement
                : null,
            entity.CreatedAt,
            entity.CompletedAt);
}
