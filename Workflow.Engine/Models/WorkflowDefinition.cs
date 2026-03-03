namespace Workflow.Engine.Models;

public sealed class WorkflowDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Version { get; set; } = 1;
    public List<ActivityNode> Activities { get; set; } = [];
    public List<Connection> Connections { get; set; } = [];
    public Dictionary<string, object?> Variables { get; set; } = [];
}

public sealed class ActivityNode
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public Dictionary<string, object?> Properties { get; set; } = [];
    public double X { get; set; }
    public double Y { get; set; }
}

public sealed class Connection
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SourceActivityId { get; set; } = string.Empty;
    public string TargetActivityId { get; set; } = string.Empty;
    public string? Condition { get; set; }
}
