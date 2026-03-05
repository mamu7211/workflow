namespace Workflow.ApiService.Data.Entities;

public sealed class WorkflowInstanceEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string WorkflowDefinitionId { get; set; } = string.Empty;
    public int WorkflowVersion { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StateJson { get; set; } = "{}";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string? Error { get; set; }

    public WorkflowDefinitionEntity? WorkflowDefinition { get; set; }
    public ICollection<ActivityExecutionLogEntity> ActivityExecutionLogs { get; set; } = [];
    public ICollection<UserTaskEntity> UserTasks { get; set; } = [];
}
