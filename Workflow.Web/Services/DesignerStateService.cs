using System.Text.Json;
using Workflow.Engine.Models;

namespace Workflow.Web;

public class DesignerStateService
{
    private readonly Stack<string> _undoStack = new();
    private readonly Stack<string> _redoStack = new();

    public WorkflowDefinition? CurrentWorkflow { get; private set; }
    public string? SelectedNodeId { get; private set; }
    public string? SelectedConnectionId { get; private set; }

    public event Action? OnChange;

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public void LoadWorkflow(WorkflowDefinition definition)
    {
        _undoStack.Clear();
        _redoStack.Clear();
        CurrentWorkflow = definition;
        SelectedNodeId = null;
        SelectedConnectionId = null;
        NotifyStateChanged();
    }

    public void AddNode(ActivityNode node)
    {
        if (CurrentWorkflow is null) return;
        SaveState();
        CurrentWorkflow.Activities.Add(node);
        SelectedNodeId = node.Id;
        SelectedConnectionId = null;
        NotifyStateChanged();
    }

    public void RemoveNode(string nodeId)
    {
        if (CurrentWorkflow is null) return;
        SaveState();
        CurrentWorkflow.Activities.RemoveAll(a => a.Id == nodeId);
        CurrentWorkflow.Connections.RemoveAll(c =>
            c.SourceActivityId == nodeId || c.TargetActivityId == nodeId);
        if (SelectedNodeId == nodeId) SelectedNodeId = null;
        NotifyStateChanged();
    }

    public void UpdateNode(ActivityNode node)
    {
        if (CurrentWorkflow is null) return;
        SaveState();
        var index = CurrentWorkflow.Activities.FindIndex(a => a.Id == node.Id);
        if (index >= 0)
        {
            CurrentWorkflow.Activities[index] = node;
        }
        NotifyStateChanged();
    }

    public void AddConnection(Connection connection)
    {
        if (CurrentWorkflow is null) return;
        SaveState();
        CurrentWorkflow.Connections.Add(connection);
        SelectedConnectionId = connection.Id;
        SelectedNodeId = null;
        NotifyStateChanged();
    }

    public void RemoveConnection(string connectionId)
    {
        if (CurrentWorkflow is null) return;
        SaveState();
        CurrentWorkflow.Connections.RemoveAll(c => c.Id == connectionId);
        if (SelectedConnectionId == connectionId) SelectedConnectionId = null;
        NotifyStateChanged();
    }

    public void SelectNode(string? nodeId)
    {
        SelectedNodeId = nodeId;
        SelectedConnectionId = null;
        NotifyStateChanged();
    }

    public void SelectConnection(string? connectionId)
    {
        SelectedConnectionId = connectionId;
        SelectedNodeId = null;
        NotifyStateChanged();
    }

    public void Undo()
    {
        if (!CanUndo || CurrentWorkflow is null) return;
        _redoStack.Push(SerializeWorkflow(CurrentWorkflow));
        var state = _undoStack.Pop();
        CurrentWorkflow = DeserializeWorkflow(state);
        SelectedNodeId = null;
        SelectedConnectionId = null;
        NotifyStateChanged();
    }

    public void Redo()
    {
        if (!CanRedo || CurrentWorkflow is null) return;
        _undoStack.Push(SerializeWorkflow(CurrentWorkflow));
        var state = _redoStack.Pop();
        CurrentWorkflow = DeserializeWorkflow(state);
        SelectedNodeId = null;
        SelectedConnectionId = null;
        NotifyStateChanged();
    }

    private void SaveState()
    {
        if (CurrentWorkflow is null) return;
        _undoStack.Push(SerializeWorkflow(CurrentWorkflow));
        _redoStack.Clear();
    }

    private static string SerializeWorkflow(WorkflowDefinition workflow) =>
        JsonSerializer.Serialize(workflow);

    private static WorkflowDefinition DeserializeWorkflow(string json) =>
        JsonSerializer.Deserialize<WorkflowDefinition>(json)!;

    private void NotifyStateChanged() => OnChange?.Invoke();
}
