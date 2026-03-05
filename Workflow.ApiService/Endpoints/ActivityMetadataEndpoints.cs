using Workflow.ApiService.Dtos;
using Workflow.Engine.Activities;

namespace Workflow.ApiService.Endpoints;

public static class ActivityMetadataEndpoints
{
    public static void MapActivityMetadataEndpoints(this WebApplication app)
    {
        app.MapGet("/api/activities/types", GetTypes).WithOpenApi();
    }

    private static IResult GetTypes(ActivityRegistry registry)
    {
        var types = registry.GetRegisteredTypes()
            .Select(t => new ActivityTypeDto(t))
            .ToList();

        return Results.Ok(types);
    }
}
