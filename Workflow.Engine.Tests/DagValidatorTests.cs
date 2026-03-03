using Workflow.Engine.Graph;
using Workflow.Engine.Models;

namespace Workflow.Engine.Tests;

[TestClass]
public class DagValidatorTests
{
    [TestMethod]
    public void Validate_ValidLinearDag_ReturnsValid()
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

        var result = DagValidator.Validate(definition);

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Count);
        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, result.TopologicalOrder);
        CollectionAssert.AreEqual(new[] { "a" }, result.StartActivityIds);
    }

    [TestMethod]
    public void Validate_ValidParallelDag_ReturnsValid()
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

        var result = DagValidator.Validate(definition);

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(4, result.TopologicalOrder.Count);
        Assert.AreEqual("a", result.TopologicalOrder[0]);
        Assert.AreEqual("d", result.TopologicalOrder[3]);
    }

    [TestMethod]
    public void Validate_CycleDetected_ReturnsInvalid()
    {
        // A is a valid start node, but B→C→B forms a cycle
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
                new Connection { SourceActivityId = "b", TargetActivityId = "c" },
                new Connection { SourceActivityId = "c", TargetActivityId = "b" }
            ]
        };

        var result = DagValidator.Validate(definition);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.Contains("cycle", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void Validate_NoStartNode_ReturnsInvalid()
    {
        // Every node has incoming edges (a cycle of 2)
        var definition = new WorkflowDefinition
        {
            Activities =
            [
                new ActivityNode { Id = "a", Type = "Log" },
                new ActivityNode { Id = "b", Type = "Log" }
            ],
            Connections =
            [
                new Connection { SourceActivityId = "a", TargetActivityId = "b" },
                new Connection { SourceActivityId = "b", TargetActivityId = "a" }
            ]
        };

        var result = DagValidator.Validate(definition);

        Assert.IsFalse(result.IsValid);
    }

    [TestMethod]
    public void Validate_InvalidReference_ReturnsInvalid()
    {
        var definition = new WorkflowDefinition
        {
            Activities = [new ActivityNode { Id = "a", Type = "Log" }],
            Connections =
            [
                new Connection { SourceActivityId = "a", TargetActivityId = "nonexistent" }
            ]
        };

        var result = DagValidator.Validate(definition);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.Contains("nonexistent")));
    }

    [TestMethod]
    public void Validate_SingleActivity_ReturnsValid()
    {
        var definition = new WorkflowDefinition
        {
            Activities = [new ActivityNode { Id = "a", Type = "Log" }],
            Connections = []
        };

        var result = DagValidator.Validate(definition);

        Assert.IsTrue(result.IsValid);
        CollectionAssert.AreEqual(new[] { "a" }, result.TopologicalOrder);
        CollectionAssert.AreEqual(new[] { "a" }, result.StartActivityIds);
    }
}
