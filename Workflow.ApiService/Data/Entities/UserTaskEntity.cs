namespace Workflow.ApiService.Data.Entities;

public sealed class UserTaskEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string WorkflowInstanceId { get; set; } = string.Empty;
    public string ActivityId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? AssignedTo { get; set; }
    public string Status { get; set; } = "Pending";
    public string? ResponseJson { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public WorkflowInstanceEntity? WorkflowInstance { get; set; }
}
