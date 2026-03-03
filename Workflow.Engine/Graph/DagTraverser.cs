using Workflow.Engine.Expressions;
using Workflow.Engine.Models;

namespace Workflow.Engine.Graph;

public static class DagTraverser
{
    public static async Task<List<string>> GetExecutableActivitiesAsync(
        WorkflowDefinition definition,
        WorkflowInstance instance,
        IExpressionEvaluator expressionEvaluator)
    {
        var executable = new List<string>();

        // Build lookup for incoming connections per activity
        var incomingConnections = new Dictionary<string, List<Connection>>();
        foreach (var activity in definition.Activities)
            incomingConnections[activity.Id] = [];
        foreach (var connection in definition.Connections)
            incomingConnections[connection.TargetActivityId].Add(connection);

        foreach (var activity in definition.Activities)
        {
            // Activity must be Pending (or not yet tracked)
            var state = GetActivityState(instance, activity.Id);
            if (state != ActivityExecutionStatus.Pending)
                continue;

            var incoming = incomingConnections[activity.Id];

            // Start nodes (no incoming connections) are immediately executable
            if (incoming.Count == 0)
            {
                executable.Add(activity.Id);
                continue;
            }

            // Check if all predecessor activities are completed
            var allPredecessorsCompleted = true;
            var hasAnyFulfilledConnection = false;

            foreach (var connection in incoming)
            {
                var predecessorState = GetActivityState(instance, connection.SourceActivityId);
                if (predecessorState != ActivityExecutionStatus.Completed)
                {
                    allPredecessorsCompleted = false;
                    break;
                }

                // Check connection condition
                if (string.IsNullOrWhiteSpace(connection.Condition))
                {
                    hasAnyFulfilledConnection = true;
                }
                else
                {
                    var conditionMet = await expressionEvaluator.EvaluateConditionAsync(
                        connection.Condition, instance.Variables);
                    if (conditionMet)
                        hasAnyFulfilledConnection = true;
                }
            }

            // All predecessors must be completed AND at least one incoming connection fulfilled
            if (allPredecessorsCompleted && hasAnyFulfilledConnection)
                executable.Add(activity.Id);
        }

        return executable;
    }

    private static ActivityExecutionStatus GetActivityState(WorkflowInstance instance, string activityId)
    {
        return instance.ActivityStates.TryGetValue(activityId, out var state)
            ? state.Status
            : ActivityExecutionStatus.Pending;
    }
}
