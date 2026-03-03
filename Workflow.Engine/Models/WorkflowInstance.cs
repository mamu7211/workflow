namespace Workflow.Engine.Models;

public sealed class WorkflowInstance
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string WorkflowDefinitionId { get; set; } = string.Empty;
    public int WorkflowVersion { get; set; }
    public WorkflowStatus Status { get; set; } = WorkflowStatus.Created;
    public Dictionary<string, object?> Variables { get; set; } = [];
    public Dictionary<string, ActivityState> ActivityStates { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string? Error { get; set; }
}

public enum WorkflowStatus
{
    Created, Running, Suspended, Completed, Faulted, Cancelled
}

public enum ActivityExecutionStatus
{
    Pending, Running, Completed, Faulted, Skipped, Suspended
}

public sealed class ActivityState
{
    public string ActivityId { get; set; } = string.Empty;
    public ActivityExecutionStatus Status { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Dictionary<string, object?> Output { get; set; } = [];
    public string? Error { get; set; }
}
