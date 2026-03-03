using Workflow.Engine.Models;

namespace Workflow.Engine.Execution;

public interface IWorkflowInstanceStore
{
    Task SaveAsync(WorkflowInstance instance, CancellationToken ct = default);
    Task<WorkflowInstance?> GetAsync(string instanceId, CancellationToken ct = default);
    Task<IReadOnlyList<WorkflowInstance>> GetByStatusAsync(WorkflowStatus status, CancellationToken ct = default);
}
