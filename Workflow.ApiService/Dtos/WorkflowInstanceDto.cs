using System.Text.Json;

namespace Workflow.ApiService.Dtos;

public sealed record WorkflowInstanceDto(
    string Id,
    string WorkflowDefinitionId,
    int WorkflowVersion,
    string Status,
    JsonElement State,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    string? Error);

public sealed record StartWorkflowDto(
    Dictionary<string, object?>? Variables);

public sealed record ResumeWorkflowDto(
    string? ActivityId,
    Dictionary<string, object?>? Variables);
