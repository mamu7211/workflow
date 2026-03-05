using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Workflow.Engine.Models;
using Workflow.Web.Components.Designer;

namespace Workflow.Web.Tests;

[TestClass]
public class NodeComponentTests
{
    [TestMethod]
    public void RendersDisplayName()
    {
        using var ctx = new BunitContext();
        var node = new ActivityNode { Id = "n1", Type = "Log", DisplayName = "My Logger" };

        var cut = ctx.Render<NodeComponent>(parameters => parameters
            .Add(p => p.Node, node)
            .Add(p => p.IsSelected, false));

        Assert.IsTrue(cut.Markup.Contains("My Logger"));
    }

    [TestMethod]
    public void RendersActivityType()
    {
        using var ctx = new BunitContext();
        var node = new ActivityNode { Id = "n1", Type = "SendEmail", DisplayName = "Mail" };

        var cut = ctx.Render<NodeComponent>(parameters => parameters
            .Add(p => p.Node, node)
            .Add(p => p.IsSelected, false));

        Assert.IsTrue(cut.Markup.Contains("SendEmail"));
    }

    [TestMethod]
    public void AppliesSelectedClass_WhenSelected()
    {
        using var ctx = new BunitContext();
        var node = new ActivityNode { Id = "n1", Type = "Log", DisplayName = "Test" };

        var cut = ctx.Render<NodeComponent>(parameters => parameters
            .Add(p => p.Node, node)
            .Add(p => p.IsSelected, true));

        var nodeDiv = cut.Find(".workflow-node");
        Assert.IsTrue(nodeDiv.ClassList.Contains("selected"));
    }

    [TestMethod]
    public void NoSelectedClass_WhenNotSelected()
    {
        using var ctx = new BunitContext();
        var node = new ActivityNode { Id = "n1", Type = "Log", DisplayName = "Test" };

        var cut = ctx.Render<NodeComponent>(parameters => parameters
            .Add(p => p.Node, node)
            .Add(p => p.IsSelected, false));

        var nodeDiv = cut.Find(".workflow-node");
        Assert.IsFalse(nodeDiv.ClassList.Contains("selected"));
    }

    [TestMethod]
    public void AppliesCategoryClass()
    {
        using var ctx = new BunitContext();
        var node = new ActivityNode { Id = "n1", Type = "SendEmail", DisplayName = "Mail" };

        var cut = ctx.Render<NodeComponent>(parameters => parameters
            .Add(p => p.Node, node)
            .Add(p => p.IsSelected, false));

        var nodeDiv = cut.Find(".workflow-node");
        Assert.IsTrue(nodeDiv.ClassList.Contains("category-integration"));
    }

    [TestMethod]
    public void HasInputAndOutputPorts()
    {
        using var ctx = new BunitContext();
        var node = new ActivityNode { Id = "n1", Type = "Log", DisplayName = "Test" };

        var cut = ctx.Render<NodeComponent>(parameters => parameters
            .Add(p => p.Node, node)
            .Add(p => p.IsSelected, false));

        Assert.IsNotNull(cut.Find(".node-port-in"));
        Assert.IsNotNull(cut.Find(".node-port-out"));
    }

    [TestMethod]
    public void PositionedViaStyle()
    {
        using var ctx = new BunitContext();
        var node = new ActivityNode { Id = "n1", Type = "Log", DisplayName = "Test", X = 150, Y = 200 };

        var cut = ctx.Render<NodeComponent>(parameters => parameters
            .Add(p => p.Node, node)
            .Add(p => p.IsSelected, false));

        var nodeDiv = cut.Find(".workflow-node");
        var style = nodeDiv.GetAttribute("style");
        Assert.IsTrue(style?.Contains("left: 150px"));
        Assert.IsTrue(style?.Contains("top: 200px"));
    }

    [TestMethod]
    public void FiresDragStart_OnMouseDown()
    {
        using var ctx = new BunitContext();
        var node = new ActivityNode { Id = "n1", Type = "Log", DisplayName = "Test" };
        MouseEventArgs? receivedArgs = null;

        var cut = ctx.Render<NodeComponent>(parameters => parameters
            .Add(p => p.Node, node)
            .Add(p => p.IsSelected, false)
            .Add(p => p.OnDragStart, (MouseEventArgs e) => { receivedArgs = e; }));

        cut.Find(".workflow-node").MouseDown(clientX: 100, clientY: 200);

        Assert.IsNotNull(receivedArgs);
        Assert.AreEqual(100, receivedArgs.ClientX);
        Assert.AreEqual(200, receivedArgs.ClientY);
    }
}
