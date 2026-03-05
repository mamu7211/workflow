using System.Net.Http.Json;
using System.Text.Json;

namespace Workflow.Web;

public class WorkflowApiClient(HttpClient http)
{
    // Definitions
    public async Task<WorkflowDefinitionDto[]> GetWorkflowsAsync() =>
        await http.GetFromJsonAsync<WorkflowDefinitionDto[]>("/api/workflows") ?? [];

    public async Task<WorkflowDefinitionDto?> GetWorkflowAsync(string id) =>
        await http.GetFromJsonAsync<WorkflowDefinitionDto>($"/api/workflows/{id}");

    public async Task<WorkflowDefinitionDto?> CreateWorkflowAsync(CreateWorkflowDto dto)
    {
        var response = await http.PostAsJsonAsync("/api/workflows", dto);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<WorkflowDefinitionDto>();
    }

    public async Task<WorkflowDefinitionDto?> UpdateWorkflowAsync(string id, UpdateWorkflowDto dto)
    {
        var response = await http.PutAsJsonAsync($"/api/workflows/{id}", dto);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<WorkflowDefinitionDto>();
    }

    public async Task DeleteWorkflowAsync(string id)
    {
        var response = await http.DeleteAsync($"/api/workflows/{id}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<WorkflowDefinitionDto?> PublishWorkflowAsync(string id)
    {
        var response = await http.PostAsync($"/api/workflows/{id}/publish", null);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<WorkflowDefinitionDto>();
    }

    public async Task<WorkflowInstanceDto?> StartWorkflowAsync(string id, StartWorkflowDto? dto = null)
    {
        var response = await http.PostAsJsonAsync($"/api/workflows/{id}/start", dto ?? new StartWorkflowDto(null));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<WorkflowInstanceDto>();
    }

    // Instances
    public async Task<WorkflowInstanceDto[]> GetInstancesAsync() =>
        await http.GetFromJsonAsync<WorkflowInstanceDto[]>("/api/instances") ?? [];

    public async Task<WorkflowInstanceDto?> GetInstanceAsync(string id) =>
        await http.GetFromJsonAsync<WorkflowInstanceDto>($"/api/instances/{id}");

    public async Task<WorkflowInstanceDto?> CancelInstanceAsync(string id)
    {
        var response = await http.PostAsync($"/api/instances/{id}/cancel", null);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<WorkflowInstanceDto>();
    }

    public async Task<ActivityExecutionLogDto[]> GetInstanceLogAsync(string id) =>
        await http.GetFromJsonAsync<ActivityExecutionLogDto[]>($"/api/instances/{id}/log") ?? [];

    // Tasks
    public async Task<UserTaskDto[]> GetTasksAsync() =>
        await http.GetFromJsonAsync<UserTaskDto[]>("/api/tasks") ?? [];

    public async Task<UserTaskDto?> CompleteTaskAsync(string id, CompleteTaskDto dto)
    {
        var response = await http.PostAsJsonAsync($"/api/tasks/{id}/complete", dto);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserTaskDto>();
    }

    // Metadata
    public async Task<ActivityTypeDto[]> GetActivityTypesAsync() =>
        await http.GetFromJsonAsync<ActivityTypeDto[]>("/api/activities/types") ?? [];
}

// DTOs matching the API responses
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

public sealed record ActivityTypeDto(string Type);

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
