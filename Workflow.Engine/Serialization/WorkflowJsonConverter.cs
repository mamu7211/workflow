using System.Text.Json;
using System.Text.Json.Serialization;
using Workflow.Engine.Models;

namespace Workflow.Engine.Serialization;

public static class WorkflowJsonConverter
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

    public static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }

    public static string Serialize(WorkflowDefinition definition)
        => JsonSerializer.Serialize(definition, Options);

    public static WorkflowDefinition? DeserializeDefinition(string json)
        => JsonSerializer.Deserialize<WorkflowDefinition>(json, Options);

    public static string Serialize(WorkflowInstance instance)
        => JsonSerializer.Serialize(instance, Options);

    public static WorkflowInstance? DeserializeInstance(string json)
        => JsonSerializer.Deserialize<WorkflowInstance>(json, Options);
}
