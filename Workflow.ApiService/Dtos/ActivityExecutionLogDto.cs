using System.Text.Json;

namespace Workflow.ApiService.Dtos;

public sealed record ActivityExecutionLogDto(
    long Id,
    string ActivityId,
    string ActivityType,
    string Status,
    JsonElement? Input,
    JsonElement? Output,
    string? Error,
    DateTime StartedAt,
    DateTime? CompletedAt);
