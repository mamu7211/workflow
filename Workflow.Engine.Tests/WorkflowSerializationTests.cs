using Workflow.Engine.Models;
using Workflow.Engine.Serialization;

namespace Workflow.Engine.Tests;

[TestClass]
public class WorkflowSerializationTests
{
    [TestMethod]
    public void Serialize_Deserialize_WorkflowDefinition_RoundTrip()
    {
        var definition = new WorkflowDefinition
        {
            Id = "wf-001",
            Name = "Test Workflow",
            Description = "A test workflow",
            Version = 2,
            Activities =
            [
                new ActivityNode
                {
                    Id = "start",
                    Type = "Log",
                    DisplayName = "Start",
                    Properties = new() { ["message"] = "Hello" },
                    X = 100,
                    Y = 200
                },
                new ActivityNode
                {
                    Id = "end",
                    Type = "Log",
                    DisplayName = "End",
                    Properties = new() { ["message"] = "${result}" },
                    X = 300,
                    Y = 200
                }
            ],
            Connections =
            [
                new Connection
                {
                    Id = "conn-1",
                    SourceActivityId = "start",
                    TargetActivityId = "end",
                    Condition = "${status} == \"done\""
                }
            ],
            Variables = new()
            {
                ["status"] = "pending",
                ["count"] = 42,
                ["flag"] = true
            }
        };

        var json = WorkflowJsonConverter.Serialize(definition);
        var deserialized = WorkflowJsonConverter.DeserializeDefinition(json);

        Assert.IsNotNull(deserialized);
        Assert.AreEqual(definition.Id, deserialized.Id);
        Assert.AreEqual(definition.Name, deserialized.Name);
        Assert.AreEqual(definition.Description, deserialized.Description);
        Assert.AreEqual(definition.Version, deserialized.Version);
        Assert.AreEqual(2, deserialized.Activities.Count);
        Assert.AreEqual("start", deserialized.Activities[0].Id);
        Assert.AreEqual("Log", deserialized.Activities[0].Type);
        Assert.AreEqual(100, deserialized.Activities[0].X);
        Assert.AreEqual(1, deserialized.Connections.Count);
        Assert.AreEqual("conn-1", deserialized.Connections[0].Id);
        Assert.AreEqual(3, deserialized.Variables.Count);
    }

    [TestMethod]
    public void Serialize_Deserialize_WorkflowInstance_RoundTrip()
    {
        var instance = new WorkflowInstance
        {
            Id = "inst-001",
            WorkflowDefinitionId = "wf-001",
            WorkflowVersion = 1,
            Status = WorkflowStatus.Suspended,
            Variables = new() { ["key"] = "value" },
            ActivityStates = new()
            {
                ["a"] = new ActivityState
                {
                    ActivityId = "a",
                    Status = ActivityExecutionStatus.Completed,
                    StartedAt = DateTime.UtcNow.AddMinutes(-5),
                    CompletedAt = DateTime.UtcNow,
                    Output = new() { ["result"] = "ok" }
                },
                ["b"] = new ActivityState
                {
                    ActivityId = "b",
                    Status = ActivityExecutionStatus.Suspended
                }
            },
            CreatedAt = DateTime.UtcNow.AddMinutes(-10),
            Error = null
        };

        var json = WorkflowJsonConverter.Serialize(instance);
        var deserialized = WorkflowJsonConverter.DeserializeInstance(json);

        Assert.IsNotNull(deserialized);
        Assert.AreEqual(instance.Id, deserialized.Id);
        Assert.AreEqual(instance.WorkflowDefinitionId, deserialized.WorkflowDefinitionId);
        Assert.AreEqual(WorkflowStatus.Suspended, deserialized.Status);
        Assert.AreEqual(2, deserialized.ActivityStates.Count);
        Assert.AreEqual(ActivityExecutionStatus.Completed, deserialized.ActivityStates["a"].Status);
        Assert.AreEqual(ActivityExecutionStatus.Suspended, deserialized.ActivityStates["b"].Status);
    }

    [TestMethod]
    public void Serialize_ProducesValidJson()
    {
        var definition = new WorkflowDefinition
        {
            Id = "wf-test",
            Name = "Simple",
            Activities = [new ActivityNode { Id = "a", Type = "Log" }]
        };

        var json = WorkflowJsonConverter.Serialize(definition);

        Assert.IsTrue(json.Contains("\"id\""));
        Assert.IsTrue(json.Contains("\"name\""));
        Assert.IsTrue(json.Contains("\"activities\""));
        Assert.IsTrue(json.Contains("wf-test"));
    }

    [TestMethod]
    public void Serialize_CamelCase_Deserialize_WithOptions_PreservesNodes()
    {
        // Regression test for Bug #004: Frontend serialized with camelCase,
        // Backend deserialized with WorkflowJsonConverter - nodes must survive round-trip
        var definition = new WorkflowDefinition
        {
            Id = "wf-roundtrip",
            Name = "Roundtrip Test",
            Activities =
            [
                new ActivityNode { Id = "node-1", Type = "Log", DisplayName = "First", X = 10, Y = 20 },
                new ActivityNode { Id = "node-2", Type = "Http", DisplayName = "Second", X = 100, Y = 200 }
            ],
            Connections =
            [
                new Connection { Id = "conn-1", SourceActivityId = "node-1", TargetActivityId = "node-2" }
            ],
            Variables = new() { ["key"] = "value" }
        };

        // Simulate: serialize with WorkflowJsonConverter (camelCase) as frontend now does
        var json = WorkflowJsonConverter.Serialize(definition);

        // Simulate: Backend deserializes with same options
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<WorkflowDefinition>(
            json, WorkflowJsonConverter.CreateOptions());

        Assert.IsNotNull(deserialized);
        Assert.AreEqual(2, deserialized.Activities.Count, "Activities must be preserved");
        Assert.AreEqual("node-1", deserialized.Activities[0].Id);
        Assert.AreEqual("node-2", deserialized.Activities[1].Id);
        Assert.AreEqual(1, deserialized.Connections.Count, "Connections must be preserved");
        Assert.AreEqual("conn-1", deserialized.Connections[0].Id);
        Assert.AreEqual(1, deserialized.Variables.Count, "Variables must be preserved");
    }

    [TestMethod]
    public void Serialize_EnumsAsStrings()
    {
        var instance = new WorkflowInstance
        {
            Status = WorkflowStatus.Running,
            ActivityStates = new()
            {
                ["a"] = new ActivityState
                {
                    ActivityId = "a",
                    Status = ActivityExecutionStatus.Completed
                }
            }
        };

        var json = WorkflowJsonConverter.Serialize(instance);

        Assert.IsTrue(json.Contains("\"running\""));
        Assert.IsTrue(json.Contains("\"completed\""));
    }
}
