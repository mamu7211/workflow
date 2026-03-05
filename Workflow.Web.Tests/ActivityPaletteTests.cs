using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Workflow.Engine.Models;
using Workflow.Web;
using Workflow.Web.Components.Designer;

namespace Workflow.Web.Tests;

[TestClass]
public class ActivityPaletteTests
{
    [TestMethod]
    public void ShowsAllActivityTypes()
    {
        using var ctx = new BunitContext();
        ctx.Services.AddScoped<WorkflowApiClient>(_ =>
            new WorkflowApiClient(new HttpClient()));

        var cut = ctx.Render<ActivityPalette>(parameters => parameters
            .Add(p => p.OnActivityAdded, (ActivityNode _) => { }));

        // Falls back to hardcoded types when API is unavailable
        var paletteItems = cut.FindAll(".palette-item");
        Assert.IsTrue(paletteItems.Count >= 11, $"Expected at least 11 activity types, got {paletteItems.Count}");
    }

    [TestMethod]
    public void ShowsActivityTypeNames()
    {
        using var ctx = new BunitContext();
        ctx.Services.AddScoped<WorkflowApiClient>(_ =>
            new WorkflowApiClient(new HttpClient()));

        var cut = ctx.Render<ActivityPalette>(parameters => parameters
            .Add(p => p.OnActivityAdded, (ActivityNode _) => { }));

        var markup = cut.Markup;
        Assert.IsTrue(markup.Contains("Log"));
        Assert.IsTrue(markup.Contains("SendEmail"));
        Assert.IsTrue(markup.Contains("HttpRequest"));
        Assert.IsTrue(markup.Contains("UserTask"));
    }

    [TestMethod]
    public void ClickOnActivityType_FiresEvent()
    {
        using var ctx = new BunitContext();
        ctx.Services.AddScoped<WorkflowApiClient>(_ =>
            new WorkflowApiClient(new HttpClient()));

        ActivityNode? addedNode = null;
        var cut = ctx.Render<ActivityPalette>(parameters => parameters
            .Add(p => p.OnActivityAdded, (ActivityNode node) => addedNode = node));

        var firstItem = cut.Find(".palette-item");
        firstItem.Click();

        Assert.IsNotNull(addedNode);
        Assert.IsFalse(string.IsNullOrEmpty(addedNode.Type));
    }

    [TestMethod]
    public void PaletteItems_HaveDraggableAttribute()
    {
        using var ctx = new BunitContext();
        ctx.Services.AddScoped<WorkflowApiClient>(_ =>
            new WorkflowApiClient(new HttpClient()));

        var cut = ctx.Render<ActivityPalette>(parameters => parameters
            .Add(p => p.OnActivityAdded, (ActivityNode _) => { }));

        var items = cut.FindAll(".palette-item[draggable='true']");
        Assert.IsTrue(items.Count >= 11);
    }
}
