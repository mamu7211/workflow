using Workflow.Engine.Activities;
using Workflow.Engine.Expressions;
using Workflow.Engine.Graph;
using Workflow.Engine.Models;

namespace Workflow.Engine.Execution;

public sealed class WorkflowExecutionEngine
{
    private readonly ActivityRegistry _registry;
    private readonly IExpressionEvaluator _expressionEvaluator;
    private readonly IServiceProvider _serviceProvider;

    public WorkflowExecutionEngine(
        ActivityRegistry registry,
        IExpressionEvaluator expressionEvaluator,
        IServiceProvider serviceProvider)
    {
        _registry = registry;
        _expressionEvaluator = expressionEvaluator;
        _serviceProvider = serviceProvider;
    }

    public async Task<WorkflowInstance> StartAsync(
        WorkflowDefinition definition,
        Dictionary<string, object?>? inputVariables = null,
        CancellationToken cancellationToken = default)
    {
        // 1. Validate DAG
        var validation = DagValidator.Validate(definition);
        if (!validation.IsValid)
            throw new InvalidOperationException(
                $"Invalid workflow definition: {string.Join("; ", validation.Errors)}");

        // 2. Create instance
        var instance = new WorkflowInstance
        {
            WorkflowDefinitionId = definition.Id,
            WorkflowVersion = definition.Version,
            Status = WorkflowStatus.Running,
            Variables = new Dictionary<string, object?>(definition.Variables)
        };

        // Merge input variables (override definition defaults)
        if (inputVariables is not null)
        {
            foreach (var (key, value) in inputVariables)
                instance.Variables[key] = value;
        }

        // Initialize activity states
        foreach (var activity in definition.Activities)
        {
            instance.ActivityStates[activity.Id] = new ActivityState
            {
                ActivityId = activity.Id,
                Status = ActivityExecutionStatus.Pending
            };
        }

        // 3. Execute loop
        await ExecuteLoopAsync(definition, instance, cancellationToken);

        return instance;
    }

    public async Task<WorkflowInstance> ResumeAsync(
        WorkflowDefinition definition,
        WorkflowInstance instance,
        string? resumeActivityId = null,
        Dictionary<string, object?>? resumeData = null,
        CancellationToken cancellationToken = default)
    {
        // 1. Set suspended activity to Completed
        if (resumeActivityId is not null && instance.ActivityStates.TryGetValue(resumeActivityId, out var actState))
        {
            actState.Status = ActivityExecutionStatus.Completed;
            actState.CompletedAt = DateTime.UtcNow;
        }

        // 2. Merge resume data into variables
        if (resumeData is not null)
        {
            foreach (var (key, value) in resumeData)
                instance.Variables[key] = value;
        }

        // 3. Set status to Running
        instance.Status = WorkflowStatus.Running;

        // 4. Execute loop
        await ExecuteLoopAsync(definition, instance, cancellationToken);

        return instance;
    }

    private async Task ExecuteLoopAsync(
        WorkflowDefinition definition,
        WorkflowInstance instance,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // 1. Get executable activities
            var executables = await DagTraverser.GetExecutableActivitiesAsync(
                definition, instance, _expressionEvaluator);

            if (executables.Count == 0)
            {
                // Check if all activities are completed
                var allCompleted = instance.ActivityStates.Values
                    .All(s => s.Status is ActivityExecutionStatus.Completed or ActivityExecutionStatus.Skipped);

                if (allCompleted)
                {
                    instance.Status = WorkflowStatus.Completed;
                    instance.CompletedAt = DateTime.UtcNow;
                }
                else if (instance.Status != WorkflowStatus.Suspended)
                {
                    // Some activities are not completed and none are executable
                    // This can happen with conditional branches where no condition is met
                    var hasSuspended = instance.ActivityStates.Values
                        .Any(s => s.Status == ActivityExecutionStatus.Suspended);

                    if (!hasSuspended)
                    {
                        // Skip unreachable activities due to unmet conditions and complete
                        var pendingIds = instance.ActivityStates.Values
                            .Where(s => s.Status == ActivityExecutionStatus.Pending)
                            .Select(s => s.ActivityId)
                            .ToList();

                        foreach (var id in pendingIds)
                            instance.ActivityStates[id].Status = ActivityExecutionStatus.Skipped;

                        instance.Status = WorkflowStatus.Completed;
                        instance.CompletedAt = DateTime.UtcNow;
                    }
                }

                return;
            }

            // 2. Execute all executable activities in parallel
            var activityLookup = definition.Activities.ToDictionary(a => a.Id);
            var tasks = executables.Select(activityId =>
                ExecuteActivityAsync(activityId, activityLookup[activityId], instance, cancellationToken));

            var results = await Task.WhenAll(tasks);

            // 3. Process results
            foreach (var (activityId, result) in results)
            {
                switch (result.ResultType)
                {
                    case ActivityResultType.Completed:
                        instance.ActivityStates[activityId].Status = ActivityExecutionStatus.Completed;
                        instance.ActivityStates[activityId].CompletedAt = DateTime.UtcNow;
                        if (result.Output is not null)
                            instance.ActivityStates[activityId].Output = result.Output;
                        break;

                    case ActivityResultType.Faulted:
                        instance.ActivityStates[activityId].Status = ActivityExecutionStatus.Faulted;
                        instance.ActivityStates[activityId].Error = result.Error;
                        instance.Status = WorkflowStatus.Faulted;
                        instance.Error = $"Activity '{activityId}' faulted: {result.Error}";
                        return;

                    case ActivityResultType.Suspended:
                        instance.ActivityStates[activityId].Status = ActivityExecutionStatus.Suspended;
                        instance.Status = WorkflowStatus.Suspended;
                        return;
                }
            }

            // 4. Continue loop
        }
    }

    private async Task<(string ActivityId, ActivityResult Result)> ExecuteActivityAsync(
        string activityId,
        ActivityNode activityNode,
        WorkflowInstance instance,
        CancellationToken cancellationToken)
    {
        instance.ActivityStates[activityId].Status = ActivityExecutionStatus.Running;
        instance.ActivityStates[activityId].StartedAt = DateTime.UtcNow;

        try
        {
            var activity = _registry.Resolve(activityNode.Type, _serviceProvider);

            // Evaluate expressions in properties
            var evaluatedProperties = await EvaluatePropertiesAsync(activityNode.Properties, instance.Variables);

            var context = new ActivityContext(
                activityId,
                evaluatedProperties,
                instance.Variables,
                _serviceProvider);

            var result = await activity.ExecuteAsync(context, cancellationToken);
            return (activityId, result);
        }
        catch (Exception ex)
        {
            return (activityId, ActivityResult.Faulted(ex.Message));
        }
    }

    private async Task<Dictionary<string, object?>> EvaluatePropertiesAsync(
        Dictionary<string, object?> properties,
        Dictionary<string, object?> variables)
    {
        var evaluated = new Dictionary<string, object?>();

        foreach (var (key, value) in properties)
        {
            if (value is string strValue && strValue.Contains("${"))
            {
                evaluated[key] = await _expressionEvaluator.EvaluateAsync(strValue, variables);
            }
            else
            {
                evaluated[key] = value;
            }
        }

        return evaluated;
    }
}
