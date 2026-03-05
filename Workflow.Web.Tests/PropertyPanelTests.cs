using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Workflow.Engine.Models;
using Workflow.Web;
using Workflow.Web.Components.Designer;

namespace Workflow.Web.Tests;

[TestClass]
public class PropertyPanelTests
{
    private static (BunitContext ctx, DesignerStateService designer) SetupContext()
    {
        var ctx = new BunitContext();
        var designer = new DesignerStateService();
        ctx.Services.AddSingleton(designer);
        return (ctx, designer);
    }

    [TestMethod]
    public void ShowsPlaceholder_WhenNothingSelected()
    {
        var (ctx, designer) = SetupContext();
        designer.LoadWorkflow(new WorkflowDefinition());

        var cut = ctx.Render<PropertyPanel>();

        Assert.IsTrue(cut.Markup.Contains("Wählen Sie einen Knoten"));
    }

    [TestMethod]
    public void ShowsDisplayName_WhenNodeSelected()
    {
        var (ctx, designer) = SetupContext();
        designer.LoadWorkflow(new WorkflowDefinition());

        var node = new ActivityNode { Id = "n1", Type = "Log", DisplayName = "My Log" };
        designer.AddNode(node);
        designer.SelectNode("n1");

        var cut = ctx.Render<PropertyPanel>();

        Assert.IsTrue(cut.Markup.Contains("Display Name"));
        Assert.IsTrue(cut.Markup.Contains("My Log"));
    }

    [TestMethod]
    public void ShowsLogFields_ForLogActivity()
    {
        var (ctx, designer) = SetupContext();
        designer.LoadWorkflow(new WorkflowDefinition());

        var node = new ActivityNode { Id = "n1", Type = "Log", DisplayName = "Logger" };
        designer.AddNode(node);
        designer.SelectNode("n1");

        var cut = ctx.Render<PropertyPanel>();

        Assert.IsTrue(cut.Markup.Contains("Message"));
        Assert.IsTrue(cut.Markup.Contains("Level"));
    }

    [TestMethod]
    public void ShowsEmailFields_ForSendEmailActivity()
    {
        var (ctx, designer) = SetupContext();
        designer.LoadWorkflow(new WorkflowDefinition());

        var node = new ActivityNode { Id = "n1", Type = "SendEmail", DisplayName = "Mail" };
        designer.AddNode(node);
        designer.SelectNode("n1");

        var cut = ctx.Render<PropertyPanel>();

        Assert.IsTrue(cut.Markup.Contains("To"));
        Assert.IsTrue(cut.Markup.Contains("Subject"));
        Assert.IsTrue(cut.Markup.Contains("Body"));
        Assert.IsTrue(cut.Markup.Contains("Is HTML"));
    }

    [TestMethod]
    public void ShowsHttpFields_ForHttpRequestActivity()
    {
        var (ctx, designer) = SetupContext();
        designer.LoadWorkflow(new WorkflowDefinition());

        var node = new ActivityNode { Id = "n1", Type = "HttpRequest", DisplayName = "HTTP" };
        designer.AddNode(node);
        designer.SelectNode("n1");

        var cut = ctx.Render<PropertyPanel>();

        Assert.IsTrue(cut.Markup.Contains("URL"));
        Assert.IsTrue(cut.Markup.Contains("Method"));
        Assert.IsTrue(cut.Markup.Contains("Body"));
    }

    [TestMethod]
    public void ShowsUserTaskFields()
    {
        var (ctx, designer) = SetupContext();
        designer.LoadWorkflow(new WorkflowDefinition());

        var node = new ActivityNode { Id = "n1", Type = "UserTask", DisplayName = "Task" };
        designer.AddNode(node);
        designer.SelectNode("n1");

        var cut = ctx.Render<PropertyPanel>();

        Assert.IsTrue(cut.Markup.Contains("Title"));
        Assert.IsTrue(cut.Markup.Contains("Description"));
        Assert.IsTrue(cut.Markup.Contains("Assigned To"));
    }

    [TestMethod]
    public void ShowsConnectionCondition_WhenConnectionSelected()
    {
        var (ctx, designer) = SetupContext();
        var workflow = new WorkflowDefinition();
        workflow.Connections.Add(new Connection { Id = "c1", SourceActivityId = "n1", TargetActivityId = "n2", Condition = "x == 1" });
        designer.LoadWorkflow(workflow);
        designer.SelectConnection("c1");

        var cut = ctx.Render<PropertyPanel>();

        Assert.IsTrue(cut.Markup.Contains("Bedingung"));
        Assert.IsTrue(cut.Markup.Contains("x == 1"));
    }

    [TestMethod]
    public void ShowsDeleteButton_WhenNodeSelected()
    {
        var (ctx, designer) = SetupContext();
        designer.LoadWorkflow(new WorkflowDefinition());

        var node = new ActivityNode { Id = "n1", Type = "Log" };
        designer.AddNode(node);
        designer.SelectNode("n1");

        var cut = ctx.Render<PropertyPanel>();

        Assert.IsTrue(cut.Markup.Contains("Knoten löschen"));
    }
}
