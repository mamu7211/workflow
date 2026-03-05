using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Workflow.ApiService.Data.Entities;
using Workflow.Engine.Execution;
using Workflow.Engine.Models;
using Workflow.Engine.Serialization;

namespace Workflow.ApiService.Data;

public sealed class EfWorkflowInstanceStore(WorkflowDbContext dbContext) : IWorkflowInstanceStore
{
    public async Task SaveAsync(WorkflowInstance instance, CancellationToken ct = default)
    {
        var existing = await dbContext.WorkflowInstances
            .FirstOrDefaultAsync(e => e.Id == instance.Id, ct);

        if (existing is null)
        {
            var entity = new WorkflowInstanceEntity
            {
                Id = instance.Id,
                WorkflowDefinitionId = instance.WorkflowDefinitionId,
                WorkflowVersion = instance.WorkflowVersion,
                Status = instance.Status.ToString(),
                StateJson = WorkflowJsonConverter.Serialize(instance),
                CreatedAt = instance.CreatedAt,
                CompletedAt = instance.CompletedAt,
                Error = instance.Error
            };
            dbContext.WorkflowInstances.Add(entity);
        }
        else
        {
            existing.Status = instance.Status.ToString();
            existing.StateJson = WorkflowJsonConverter.Serialize(instance);
            existing.CompletedAt = instance.CompletedAt;
            existing.Error = instance.Error;
        }

        await dbContext.SaveChangesAsync(ct);
    }

    public async Task<WorkflowInstance?> GetAsync(string instanceId, CancellationToken ct = default)
    {
        var entity = await dbContext.WorkflowInstances
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == instanceId, ct);

        return entity is null ? null : WorkflowJsonConverter.DeserializeInstance(entity.StateJson);
    }

    public async Task<IReadOnlyList<WorkflowInstance>> GetByStatusAsync(WorkflowStatus status, CancellationToken ct = default)
    {
        var statusStr = status.ToString();
        var entities = await dbContext.WorkflowInstances
            .AsNoTracking()
            .Where(e => e.Status == statusStr)
            .ToListAsync(ct);

        var instances = new List<WorkflowInstance>();
        foreach (var entity in entities)
        {
            var instance = WorkflowJsonConverter.DeserializeInstance(entity.StateJson);
            if (instance is not null)
                instances.Add(instance);
        }

        return instances;
    }
}
