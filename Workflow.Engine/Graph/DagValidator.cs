using Workflow.Engine.Models;

namespace Workflow.Engine.Graph;

public sealed class DagValidationResult
{
    public bool IsValid { get; init; }
    public List<string> Errors { get; init; } = [];
    public List<string> TopologicalOrder { get; init; } = [];
    public List<string> StartActivityIds { get; init; } = [];
}

public static class DagValidator
{
    public static DagValidationResult Validate(WorkflowDefinition definition)
    {
        var errors = new List<string>();
        var activityIds = new HashSet<string>(definition.Activities.Select(a => a.Id));

        // 1. Referential integrity: connections reference existing activity IDs
        foreach (var connection in definition.Connections)
        {
            if (!activityIds.Contains(connection.SourceActivityId))
                errors.Add($"Connection '{connection.Id}' references non-existent source activity '{connection.SourceActivityId}'.");
            if (!activityIds.Contains(connection.TargetActivityId))
                errors.Add($"Connection '{connection.Id}' references non-existent target activity '{connection.TargetActivityId}'.");
        }

        if (errors.Count > 0)
        {
            return new DagValidationResult { IsValid = false, Errors = errors };
        }

        // Build adjacency and in-degree maps
        var inDegree = new Dictionary<string, int>();
        var adjacency = new Dictionary<string, List<string>>();

        foreach (var activity in definition.Activities)
        {
            inDegree[activity.Id] = 0;
            adjacency[activity.Id] = [];
        }

        foreach (var connection in definition.Connections)
        {
            adjacency[connection.SourceActivityId].Add(connection.TargetActivityId);
            inDegree[connection.TargetActivityId]++;
        }

        // 2. Start nodes: at least one node with no incoming edges
        var startNodes = inDegree.Where(kv => kv.Value == 0).Select(kv => kv.Key).ToList();
        if (startNodes.Count == 0)
        {
            errors.Add("No start activities found (all activities have incoming connections).");
            return new DagValidationResult { IsValid = false, Errors = errors };
        }

        // 3. Kahn's algorithm for topological sort + cycle detection
        var queue = new Queue<string>(startNodes);
        var topologicalOrder = new List<string>();
        var tempInDegree = new Dictionary<string, int>(inDegree);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            topologicalOrder.Add(current);

            foreach (var neighbor in adjacency[current])
            {
                tempInDegree[neighbor]--;
                if (tempInDegree[neighbor] == 0)
                    queue.Enqueue(neighbor);
            }
        }

        if (topologicalOrder.Count != definition.Activities.Count)
        {
            errors.Add("Workflow contains a cycle.");
            return new DagValidationResult { IsValid = false, Errors = errors };
        }

        // 4. Reachability: all nodes must be reachable from a start node
        var reachable = new HashSet<string>();
        var reachQueue = new Queue<string>(startNodes);
        while (reachQueue.Count > 0)
        {
            var current = reachQueue.Dequeue();
            if (!reachable.Add(current)) continue;
            foreach (var neighbor in adjacency[current])
                reachQueue.Enqueue(neighbor);
        }

        var unreachable = activityIds.Except(reachable).ToList();
        if (unreachable.Count > 0)
        {
            foreach (var id in unreachable)
                errors.Add($"Activity '{id}' is not reachable from any start activity.");
            return new DagValidationResult { IsValid = false, Errors = errors };
        }

        return new DagValidationResult
        {
            IsValid = true,
            TopologicalOrder = topologicalOrder,
            StartActivityIds = startNodes
        };
    }
}
