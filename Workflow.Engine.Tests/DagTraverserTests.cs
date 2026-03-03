using Workflow.Engine.Expressions;
using Workflow.Engine.Graph;
using Workflow.Engine.Models;

namespace Workflow.Engine.Tests;

[TestClass]
public class DagTraverserTests
{
    private readonly IExpressionEvaluator _evaluator = new SimpleExpressionEvaluator();

    [TestMethod]
    public async Task GetExecutableActivities_LinearWorkflow_ReturnsStartNode()
    {
        var definition = new WorkflowDefinition
        {
            Activities =
            [
                new ActivityNode { Id = "a", Type = "Log" },
                new ActivityNode { Id = "b", Type = "Log" },
                new ActivityNode { Id = "c", Type = "Log" }
            ],
            Connections =
            [
                new Connection { SourceActivityId = "a", TargetActivityId = "b" },
                new Connection { SourceActivityId = "b", TargetActivityId = "c" }
            ]
        };

        var instance = CreateInstance(definition);

        var executable = await DagTraverser.GetExecutableActivitiesAsync(definition, instance, _evaluator);

        CollectionAssert.AreEqual(new[] { "a" }, executable);
    }

    [TestMethod]
    public async Task GetExecutableActivities_LinearAfterFirstCompleted_ReturnsSecond()
    {
        var definition = new WorkflowDefinition
        {
            Activities =
            [
                new ActivityNode { Id = "a", Type = "Log" },
                new ActivityNode { Id = "b", Type = "Log" },
                new ActivityNode { Id = "c", Type = "Log" }
            ],
            Connections =
            [
                new Connection { SourceActivityId = "a", TargetActivityId = "b" },
                new Connection { SourceActivityId = "b", TargetActivityId = "c" }
            ]
        };

        var instance = CreateInstance(definition);
        instance.ActivityStates["a"].Status = ActivityExecutionStatus.Completed;

        var executable = await DagTraverser.GetExecutableActivitiesAsync(definition, instance, _evaluator);

        CollectionAssert.AreEqual(new[] { "b" }, executable);
    }

    [TestMethod]
    public async Task GetExecutableActivities_ParallelBranches_ReturnsBothBranches()
    {
        // A → [B, C] → D
        var definition = new WorkflowDefinition
        {
            Activities =
            [
                new ActivityNode { Id = "a", Type = "Log" },
                new ActivityNode { Id = "b", Type = "Log" },
                new ActivityNode { Id = "c", Type = "Log" },
                new ActivityNode { Id = "d", Type = "Log" }
            ],
            Connections =
            [
                new Connection { SourceActivityId = "a", TargetActivityId = "b" },
                new Connection { SourceActivityId = "a", TargetActivityId = "c" },
                new Connection { SourceActivityId = "b", TargetActivityId = "d" },
                new Connection { SourceActivityId = "c", TargetActivityId = "d" }
            ]
        };

        var instance = CreateInstance(definition);
        instance.ActivityStates["a"].Status = ActivityExecutionStatus.Completed;

        var executable = await DagTraverser.GetExecutableActivitiesAsync(definition, instance, _evaluator);

        Assert.AreEqual(2, executable.Count);
        CollectionAssert.Contains(executable, "b");
        CollectionAssert.Contains(executable, "c");
    }

    [TestMethod]
    public async Task GetExecutableActivities_JoinPoint_WaitsForAllBranches()
    {
        // A → [B, C] → D
        var definition = new WorkflowDefinition
        {
            Activities =
            [
                new ActivityNode { Id = "a", Type = "Log" },
                new ActivityNode { Id = "b", Type = "Log" },
                new ActivityNode { Id = "c", Type = "Log" },
                new ActivityNode { Id = "d", Type = "Log" }
            ],
            Connections =
            [
                new Connection { SourceActivityId = "a", TargetActivityId = "b" },
                new Connection { SourceActivityId = "a", TargetActivityId = "c" },
                new Connection { SourceActivityId = "b", TargetActivityId = "d" },
                new Connection { SourceActivityId = "c", TargetActivityId = "d" }
            ]
        };

        var instance = CreateInstance(definition);
        instance.ActivityStates["a"].Status = ActivityExecutionStatus.Completed;
        instance.ActivityStates["b"].Status = ActivityExecutionStatus.Completed;
        // C is still pending → D should NOT be executable

        var executable = await DagTraverser.GetExecutableActivitiesAsync(definition, instance, _evaluator);

        CollectionAssert.DoesNotContain(executable, "d");
        CollectionAssert.Contains(executable, "c");
    }

    [TestMethod]
    public async Task GetExecutableActivities_ConditionalPath_OnlyFulfilledCondition()
    {
        var definition = new WorkflowDefinition
        {
            Activities =
            [
                new ActivityNode { Id = "a", Type = "Log" },
                new ActivityNode { Id = "b", Type = "Log" },
                new ActivityNode { Id = "c", Type = "Log" }
            ],
            Connections =
            [
                new Connection { SourceActivityId = "a", TargetActivityId = "b", Condition = "${status} == \"approved\"" },
                new Connection { SourceActivityId = "a", TargetActivityId = "c", Condition = "${status} == \"rejected\"" }
            ]
        };

        var instance = CreateInstance(definition);
        instance.ActivityStates["a"].Status = ActivityExecutionStatus.Completed;
        instance.Variables["status"] = "approved";

        var executable = await DagTraverser.GetExecutableActivitiesAsync(definition, instance, _evaluator);

        CollectionAssert.Contains(executable, "b");
        CollectionAssert.DoesNotContain(executable, "c");
    }

    private static WorkflowInstance CreateInstance(WorkflowDefinition definition)
    {
        var instance = new WorkflowInstance
        {
            WorkflowDefinitionId = definition.Id,
            Status = WorkflowStatus.Running,
            Variables = new Dictionary<string, object?>(definition.Variables)
        };

        foreach (var activity in definition.Activities)
        {
            instance.ActivityStates[activity.Id] = new ActivityState
            {
                ActivityId = activity.Id,
                Status = ActivityExecutionStatus.Pending
            };
        }

        return instance;
    }
}
