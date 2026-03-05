using System.Text.Json;

namespace Workflow.ApiService.Dtos;

public sealed record WorkflowDefinitionDto(
    string Id,
    string Name,
    string? Description,
    int Version,
    JsonElement Definition,
    bool IsPublished,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record CreateWorkflowDto(
    string Name,
    string? Description,
    JsonElement Definition);

public sealed record UpdateWorkflowDto(
    string Name,
    string? Description,
    JsonElement Definition);
