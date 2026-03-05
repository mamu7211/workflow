using System.Net;
using System.Text.Json;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Workflow.Web;
using Workflow.Web.Pages;

namespace Workflow.Web.Tests;

[TestClass]
public class WorkflowListTests
{
    private static WorkflowDefinitionDto CreateTestDto(string name = "Test Workflow", bool published = false) =>
        new(
            Id: Guid.NewGuid().ToString(),
            Name: name,
            Description: null,
            Version: 1,
            Definition: JsonDocument.Parse("{}").RootElement,
            IsPublished: published,
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow);

    [TestMethod]
    public void ShowsWorkflowTable_WhenWorkflowsExist()
    {
        using var ctx = new BunitContext();
        var handler = new MockHttpHandler();
        handler.SetResponse("/api/workflows",
            JsonSerializer.Serialize(new[] { CreateTestDto("My Workflow") }));

        ctx.Services.AddScoped(_ =>
            new WorkflowApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") }));

        var cut = ctx.Render<WorkflowList>();
        cut.WaitForState(() => cut.Markup.Contains("My Workflow"));

        Assert.IsTrue(cut.Markup.Contains("My Workflow"));
        Assert.IsTrue(cut.Markup.Contains("v1"));
    }

    [TestMethod]
    public void ShowsEmptyMessage_WhenNoWorkflows()
    {
        using var ctx = new BunitContext();
        var handler = new MockHttpHandler();
        handler.SetResponse("/api/workflows", "[]");

        ctx.Services.AddScoped(_ =>
            new WorkflowApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") }));

        var cut = ctx.Render<WorkflowList>();
        cut.WaitForState(() => cut.Markup.Contains("Keine Workflows"));

        Assert.IsTrue(cut.Markup.Contains("Keine Workflows vorhanden"));
    }

    [TestMethod]
    public void ShowsDraftBadge_ForUnpublishedWorkflow()
    {
        using var ctx = new BunitContext();
        var handler = new MockHttpHandler();
        handler.SetResponse("/api/workflows",
            JsonSerializer.Serialize(new[] { CreateTestDto(published: false) }));

        ctx.Services.AddScoped(_ =>
            new WorkflowApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") }));

        var cut = ctx.Render<WorkflowList>();
        cut.WaitForState(() => cut.Markup.Contains("Draft"));

        Assert.IsTrue(cut.Markup.Contains("Draft"));
    }

    [TestMethod]
    public void ShowsPublishedBadge_ForPublishedWorkflow()
    {
        using var ctx = new BunitContext();
        var handler = new MockHttpHandler();
        handler.SetResponse("/api/workflows",
            JsonSerializer.Serialize(new[] { CreateTestDto(published: true) }));

        ctx.Services.AddScoped(_ =>
            new WorkflowApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") }));

        var cut = ctx.Render<WorkflowList>();
        cut.WaitForState(() => cut.Markup.Contains("Published"));

        Assert.IsTrue(cut.Markup.Contains("Published"));
    }

    [TestMethod]
    public void ShowsDeleteConfirmation_OnDeleteClick()
    {
        using var ctx = new BunitContext();
        var handler = new MockHttpHandler();
        var dto = CreateTestDto("Delete Me");
        handler.SetResponse("/api/workflows", JsonSerializer.Serialize(new[] { dto }));

        ctx.Services.AddScoped(_ =>
            new WorkflowApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://test") }));

        var cut = ctx.Render<WorkflowList>();
        cut.WaitForState(() => cut.Markup.Contains("Delete Me"));

        var deleteBtn = cut.Find(".btn-outline-danger");
        deleteBtn.Click();

        Assert.IsTrue(cut.Markup.Contains("Möchten Sie den Workflow"));
        Assert.IsTrue(cut.Markup.Contains("Delete Me"));
        Assert.IsTrue(cut.Markup.Contains("wirklich löschen"));
    }
}

/// <summary>
/// Simple mock HTTP handler for testing WorkflowApiClient.
/// </summary>
public class MockHttpHandler : HttpMessageHandler
{
    private readonly Dictionary<string, string> _responses = new();

    public void SetResponse(string path, string json)
    {
        _responses[path] = json;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.AbsolutePath ?? "";

        if (_responses.TryGetValue(path, out var json))
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}
