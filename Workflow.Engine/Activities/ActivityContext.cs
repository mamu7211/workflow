using System.Text.Json;

namespace Workflow.Engine.Activities;

public sealed class ActivityContext
{
    public string ActivityId { get; }
    public Dictionary<string, object?> Properties { get; }
    public Dictionary<string, object?> Variables { get; }
    public IServiceProvider ServiceProvider { get; }

    public ActivityContext(
        string activityId,
        Dictionary<string, object?> properties,
        Dictionary<string, object?> variables,
        IServiceProvider serviceProvider)
    {
        ActivityId = activityId;
        Properties = properties;
        Variables = variables;
        ServiceProvider = serviceProvider;
    }

    public T? GetProperty<T>(string name)
    {
        if (!Properties.TryGetValue(name, out var value) || value is null)
            return default;

        if (value is T typed)
            return typed;

        if (value is JsonElement element)
            return element.Deserialize<T>();

        return (T)Convert.ChangeType(value, typeof(T));
    }

    public T? GetVariable<T>(string name)
    {
        if (!Variables.TryGetValue(name, out var value) || value is null)
            return default;

        if (value is T typed)
            return typed;

        if (value is JsonElement element)
            return element.Deserialize<T>();

        return (T)Convert.ChangeType(value, typeof(T));
    }

    public void SetVariable(string name, object? value)
    {
        Variables[name] = value;
    }
}
