namespace Workflow.ApiService.Data.Entities;

public sealed class WorkflowDefinitionEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Version { get; set; } = 1;
    public string DefinitionJson { get; set; } = "{}";
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<WorkflowInstanceEntity> Instances { get; set; } = [];
}
