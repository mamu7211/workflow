using System.Text.Json;

namespace Workflow.ApiService.Dtos;

public sealed record UserTaskDto(
    string Id,
    string WorkflowInstanceId,
    string ActivityId,
    string Title,
    string? Description,
    string? AssignedTo,
    string Status,
    JsonElement? Response,
    DateTime CreatedAt,
    DateTime? CompletedAt);

public sealed record CompleteTaskDto(
    string Status,
    Dictionary<string, object?>? Data);
