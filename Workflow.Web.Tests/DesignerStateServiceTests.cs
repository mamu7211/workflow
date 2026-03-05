using Workflow.Engine.Models;
using Workflow.Web;

namespace Workflow.Web.Tests;

[TestClass]
public class DesignerStateServiceTests
{
    private DesignerStateService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _service = new DesignerStateService();
        _service.LoadWorkflow(new WorkflowDefinition { Name = "Test" });
    }

    [TestMethod]
    public void LoadWorkflow_SetsCurrentWorkflow()
    {
        Assert.IsNotNull(_service.CurrentWorkflow);
        Assert.AreEqual("Test", _service.CurrentWorkflow.Name);
    }

    [TestMethod]
    public void LoadWorkflow_ClearsSelection()
    {
        _service.SelectNode("some-id");
        _service.LoadWorkflow(new WorkflowDefinition());

        Assert.IsNull(_service.SelectedNodeId);
        Assert.IsNull(_service.SelectedConnectionId);
    }

    [TestMethod]
    public void AddNode_AddsToActivities()
    {
        var node = new ActivityNode { Id = "n1", Type = "Log" };
        _service.AddNode(node);

        Assert.AreEqual(1, _service.CurrentWorkflow!.Activities.Count);
        Assert.AreEqual("n1", _service.CurrentWorkflow.Activities[0].Id);
    }

    [TestMethod]
    public void AddNode_SelectsNewNode()
    {
        var node = new ActivityNode { Id = "n1", Type = "Log" };
        _service.AddNode(node);

        Assert.AreEqual("n1", _service.SelectedNodeId);
    }

    [TestMethod]
    public void RemoveNode_RemovesFromActivities()
    {
        var node = new ActivityNode { Id = "n1", Type = "Log" };
        _service.AddNode(node);
        _service.RemoveNode("n1");

        Assert.AreEqual(0, _service.CurrentWorkflow!.Activities.Count);
    }

    [TestMethod]
    public void RemoveNode_RemovesRelatedConnections()
    {
        var n1 = new ActivityNode { Id = "n1", Type = "Log" };
        var n2 = new ActivityNode { Id = "n2", Type = "Log" };
        _service.AddNode(n1);
        _service.AddNode(n2);
        _service.AddConnection(new Connection { Id = "c1", SourceActivityId = "n1", TargetActivityId = "n2" });

        _service.RemoveNode("n1");

        Assert.AreEqual(0, _service.CurrentWorkflow!.Connections.Count);
    }

    [TestMethod]
    public void RemoveNode_ClearsSelectionIfRemoved()
    {
        var node = new ActivityNode { Id = "n1", Type = "Log" };
        _service.AddNode(node);
        _service.SelectNode("n1");

        _service.RemoveNode("n1");

        Assert.IsNull(_service.SelectedNodeId);
    }

    [TestMethod]
    public void UpdateNode_ReplacesNodeInList()
    {
        var node = new ActivityNode { Id = "n1", Type = "Log", DisplayName = "Old" };
        _service.AddNode(node);

        var updated = new ActivityNode { Id = "n1", Type = "Log", DisplayName = "New" };
        _service.UpdateNode(updated);

        Assert.AreEqual("New", _service.CurrentWorkflow!.Activities[0].DisplayName);
    }

    [TestMethod]
    public void AddConnection_AddsToConnections()
    {
        var conn = new Connection { Id = "c1", SourceActivityId = "n1", TargetActivityId = "n2" };
        _service.AddConnection(conn);

        Assert.AreEqual(1, _service.CurrentWorkflow!.Connections.Count);
    }

    [TestMethod]
    public void RemoveConnection_RemovesFromConnections()
    {
        var conn = new Connection { Id = "c1", SourceActivityId = "n1", TargetActivityId = "n2" };
        _service.AddConnection(conn);
        _service.RemoveConnection("c1");

        Assert.AreEqual(0, _service.CurrentWorkflow!.Connections.Count);
    }

    [TestMethod]
    public void SelectNode_ClearsConnectionSelection()
    {
        _service.SelectConnection("c1");
        _service.SelectNode("n1");

        Assert.AreEqual("n1", _service.SelectedNodeId);
        Assert.IsNull(_service.SelectedConnectionId);
    }

    [TestMethod]
    public void SelectConnection_ClearsNodeSelection()
    {
        _service.SelectNode("n1");
        _service.SelectConnection("c1");

        Assert.AreEqual("c1", _service.SelectedConnectionId);
        Assert.IsNull(_service.SelectedNodeId);
    }

    [TestMethod]
    public void Undo_RestoresPreviousState()
    {
        var n1 = new ActivityNode { Id = "n1", Type = "Log" };
        _service.AddNode(n1);

        var n2 = new ActivityNode { Id = "n2", Type = "Delay" };
        _service.AddNode(n2);

        Assert.AreEqual(2, _service.CurrentWorkflow!.Activities.Count);

        _service.Undo();

        Assert.AreEqual(1, _service.CurrentWorkflow!.Activities.Count);
        Assert.AreEqual("n1", _service.CurrentWorkflow.Activities[0].Id);
    }

    [TestMethod]
    public void Redo_RestoresUndoneState()
    {
        var n1 = new ActivityNode { Id = "n1", Type = "Log" };
        _service.AddNode(n1);

        _service.Undo();
        Assert.AreEqual(0, _service.CurrentWorkflow!.Activities.Count);

        _service.Redo();
        Assert.AreEqual(1, _service.CurrentWorkflow!.Activities.Count);
    }

    [TestMethod]
    public void CanUndo_FalseInitially()
    {
        Assert.IsFalse(_service.CanUndo);
    }

    [TestMethod]
    public void CanUndo_TrueAfterModification()
    {
        _service.AddNode(new ActivityNode { Id = "n1", Type = "Log" });
        Assert.IsTrue(_service.CanUndo);
    }

    [TestMethod]
    public void CanRedo_TrueAfterUndo()
    {
        _service.AddNode(new ActivityNode { Id = "n1", Type = "Log" });
        _service.Undo();
        Assert.IsTrue(_service.CanRedo);
    }

    [TestMethod]
    public void NewChange_ClearsRedoStack()
    {
        _service.AddNode(new ActivityNode { Id = "n1", Type = "Log" });
        _service.Undo();
        Assert.IsTrue(_service.CanRedo);

        _service.AddNode(new ActivityNode { Id = "n2", Type = "Delay" });
        Assert.IsFalse(_service.CanRedo);
    }

    [TestMethod]
    public void OnChange_FiresOnModifications()
    {
        var fired = 0;
        _service.OnChange += () => fired++;

        _service.AddNode(new ActivityNode { Id = "n1", Type = "Log" });
        _service.RemoveNode("n1");
        _service.SelectNode("x");

        Assert.AreEqual(3, fired);
    }
}
