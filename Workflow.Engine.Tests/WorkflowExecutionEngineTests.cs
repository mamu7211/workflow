using Workflow.Engine.Activities;
using Workflow.Engine.Execution;
using Workflow.Engine.Expressions;
using Workflow.Engine.Models;

namespace Workflow.Engine.Tests;

[TestClass]
public class WorkflowExecutionEngineTests
{
    private sealed class CompletingActivity : ActivityBase
    {
        public override string Type => "Complete";

        public override Task<ActivityResult> ExecuteAsync(ActivityContext context, CancellationToken cancellationToken = default)
        {
            var output = new Dictionary<string, object?> { ["executed"] = true };
            return Task.FromResult(ActivityResult.Completed(output));
        }
    }

    private sealed class FaultingActivity : ActivityBase
    {
        public override string Type => "Fault";

        public override Task<ActivityResult> ExecuteAsync(ActivityContext context, CancellationToken cancellationToken = default)
            => Task.FromResult(ActivityResult.Faulted("Something went wrong"));
    }

    private sealed class SuspendingActivity : ActivityBase
    {
        public override string Type => "Suspend";

        public override Task<ActivityResult> ExecuteAsync(ActivityContext context, CancellationToken cancellationToken = default)
            => Task.FromResult(ActivityResult.SuspendExecution("Waiting for input"));
    }

    private sealed class SetVariableActivity : ActivityBase
    {
        public override string Type => "SetVar";

        public override Task<ActivityResult> ExecuteAsync(ActivityContext context, CancellationToken cancellationToken = default)
        {
            context.SetVariable("result", "done");
            return Task.FromResult(ActivityResult.Completed());
        }
    }

    private static WorkflowExecutionEngine CreateEngine(params ActivityBase[] activities)
    {
        var registry = new ActivityRegistry();
        foreach (var activity in activities)
        {
            var type = activity.Type;
            registry.Register(type, _ => activity);
        }

        return new WorkflowExecutionEngine(
            registry,
            new SimpleExpressionEvaluator(),
            null!);
    }

    [TestMethod]
    public async Task StartAsync_LinearWorkflow_CompletesAllActivities()
    {
        var engine = CreateEngine(new CompletingActivity());

        var definition = new WorkflowDefinition
        {
            Activities =
            [
                new ActivityNode { Id = "a", Type = "Complete" },
                new ActivityNode { Id = "b", Type = "Complete" },
                new ActivityNode { Id = "c", Type = "Complete" }
            ],
            Connections =
            [
                new Connection { SourceActivityId = "a", TargetActivityId = "b" },
                new Connection { SourceActivityId = "b", TargetActivityId = "c" }
            ]
        };

        var instance = await engine.StartAsync(definition);

        Assert.AreEqual(WorkflowStatus.Completed, instance.Status);
        Assert.IsTrue(instance.ActivityStates.Values.All(s => s.Status == ActivityExecutionStatus.Completed));
        Assert.IsNotNull(instance.CompletedAt);
    }

    [TestMethod]
    public async Task StartAsync_ParallelBranches_CompletesAll()
    {
        var engine = CreateEngine(new CompletingActivity());

        // A → [B, C] → D
        var definition = new WorkflowDefinition
        {
            Activities =
            [
                new ActivityNode { Id = "a", Type = "Complete" },
                new ActivityNode { Id = "b", Type = "Complete" },
                new ActivityNode { Id = "c", Type = "Complete" },
                new ActivityNode { Id = "d", Type = "Complete" }
            ],
            Connections =
            [
                new Connection { SourceActivityId = "a", TargetActivityId = "b" },
                new Connection { SourceActivityId = "a", TargetActivityId = "c" },
                new Connection { SourceActivityId = "b", TargetActivityId = "d" },
                new Connection { SourceActivityId = "c", TargetActivityId = "d" }
            ]
        };

        var instance = await engine.StartAsync(definition);

        Assert.AreEqual(WorkflowStatus.Completed, instance.Status);
        Assert.IsTrue(instance.ActivityStates.Values.All(s => s.Status == ActivityExecutionStatus.Completed));
    }

    [TestMethod]
    public async Task StartAsync_ConditionalPath_ExecutesCorrectBranch()
    {
        var engine = CreateEngine(new CompletingActivity());

        var definition = new WorkflowDefinition
        {
            Activities =
            [
                new ActivityNode { Id = "a", Type = "Complete" },
                new ActivityNode { Id = "approved", Type = "Complete" },
                new ActivityNode { Id = "rejected", Type = "Complete" }
            ],
            Connections =
            [
                new Connection { SourceActivityId = "a", TargetActivityId = "approved", Condition = "${status} == \"yes\"" },
                new Connection { SourceActivityId = "a", TargetActivityId = "rejected", Condition = "${status} == \"no\"" }
            ],
            Variables = new() { ["status"] = null }
        };

        var instance = await engine.StartAsync(definition, new() { ["status"] = "yes" });

        Assert.AreEqual(WorkflowStatus.Completed, instance.Status);
        Assert.AreEqual(ActivityExecutionStatus.Completed, instance.ActivityStates["approved"].Status);
        Assert.AreEqual(ActivityExecutionStatus.Skipped, instance.ActivityStates["rejected"].Status);
    }

    [TestMethod]
    public async Task StartAsync_FaultingActivity_SetsFaulted()
    {
        var engine = CreateEngine(new CompletingActivity(), new FaultingActivity());

        var definition = new WorkflowDefinition
        {
            Activities =
            [
                new ActivityNode { Id = "a", Type = "Complete" },
                new ActivityNode { Id = "b", Type = "Fault" }
            ],
            Connections =
            [
                new Connection { SourceActivityId = "a", TargetActivityId = "b" }
            ]
        };

        var instance = await engine.StartAsync(definition);

        Assert.AreEqual(WorkflowStatus.Faulted, instance.Status);
        Assert.AreEqual(ActivityExecutionStatus.Faulted, instance.ActivityStates["b"].Status);
        Assert.IsNotNull(instance.Error);
    }

    [TestMethod]
    public async Task StartAsync_SuspendingActivity_SetsSuspended()
    {
        var engine = CreateEngine(new CompletingActivity(), new SuspendingActivity());

        var definition = new WorkflowDefinition
        {
            Activities =
            [
                new ActivityNode { Id = "a", Type = "Complete" },
                new ActivityNode { Id = "b", Type = "Suspend" },
                new ActivityNode { Id = "c", Type = "Complete" }
            ],
            Connections =
            [
                new Connection { SourceActivityId = "a", TargetActivityId = "b" },
                new Connection { SourceActivityId = "b", TargetActivityId = "c" }
            ]
        };

        var instance = await engine.StartAsync(definition);

        Assert.AreEqual(WorkflowStatus.Suspended, instance.Status);
        Assert.AreEqual(ActivityExecutionStatus.Suspended, instance.ActivityStates["b"].Status);
        Assert.AreEqual(ActivityExecutionStatus.Pending, instance.ActivityStates["c"].Status);
    }

    [TestMethod]
    public async Task ResumeAsync_AfterSuspend_ContinuesExecution()
    {
        var engine = CreateEngine(new CompletingActivity(), new SuspendingActivity());

        var definition = new WorkflowDefinition
        {
            Activities =
            [
                new ActivityNode { Id = "a", Type = "Complete" },
                new ActivityNode { Id = "b", Type = "Suspend" },
                new ActivityNode { Id = "c", Type = "Complete" }
            ],
            Connections =
            [
                new Connection { SourceActivityId = "a", TargetActivityId = "b" },
                new Connection { SourceActivityId = "b", TargetActivityId = "c" }
            ]
        };

        var instance = await engine.StartAsync(definition);
        Assert.AreEqual(WorkflowStatus.Suspended, instance.Status);

        instance = await engine.ResumeAsync(definition, instance, "b", new() { ["input"] = "data" });

        Assert.AreEqual(WorkflowStatus.Completed, instance.Status);
        Assert.AreEqual(ActivityExecutionStatus.Completed, instance.ActivityStates["c"].Status);
        Assert.AreEqual("data", instance.Variables["input"]);
    }

    [TestMethod]
    public async Task StartAsync_InvalidDag_ThrowsException()
    {
        var engine = CreateEngine(new CompletingActivity());

        var definition = new WorkflowDefinition
        {
            Activities =
            [
                new ActivityNode { Id = "a", Type = "Complete" },
                new ActivityNode { Id = "b", Type = "Complete" }
            ],
            Connections =
            [
                new Connection { SourceActivityId = "a", TargetActivityId = "b" },
                new Connection { SourceActivityId = "b", TargetActivityId = "a" }
            ]
        };

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => engine.StartAsync(definition));
    }

    [TestMethod]
    public async Task StartAsync_InputVariables_OverrideDefaults()
    {
        var engine = CreateEngine(new SetVariableActivity());

        var definition = new WorkflowDefinition
        {
            Activities = [new ActivityNode { Id = "a", Type = "SetVar" }],
            Variables = new() { ["key"] = "default" }
        };

        var instance = await engine.StartAsync(definition, new() { ["key"] = "override" });

        Assert.AreEqual("override", instance.Variables["key"]);
    }
}
