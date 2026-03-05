namespace Workflow.ApiService.Data.Entities;

public sealed class ActivityExecutionLogEntity
{
    public long Id { get; set; }
    public string WorkflowInstanceId { get; set; } = string.Empty;
    public string ActivityId { get; set; } = string.Empty;
    public string ActivityType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? InputJson { get; set; }
    public string? OutputJson { get; set; }
    public string? Error { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public WorkflowInstanceEntity? WorkflowInstance { get; set; }
}
