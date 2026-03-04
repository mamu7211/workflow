using Workflow.Engine.Activities;

namespace Workflow.Engine.Tests;

[TestClass]
public sealed class UserTaskActivityTests
{
    [TestMethod]
    public async Task UserTask_ReturnsSuspendWithTitle()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            ["title"] = "Approve Request",
            ["description"] = "Please review and approve the request",
            ["assignedTo"] = "manager@example.com"
        });

        var activity = new UserTaskActivity();
        var result = await activity.ExecuteAsync(context);

        Assert.AreEqual(ActivityResultType.Suspended, result.ResultType);
        Assert.IsTrue(result.SuspendReason!.Contains("Approve Request"));
        Assert.AreEqual("Approve Request", result.Output!["title"]);
        Assert.AreEqual("Please review and approve the request", result.Output["description"]);
        Assert.AreEqual("manager@example.com", result.Output["assignedTo"]);
    }

    [TestMethod]
    public async Task UserTask_OptionalFields_DefaultValues()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            ["title"] = "Simple Task"
        });

        var activity = new UserTaskActivity();
        var result = await activity.ExecuteAsync(context);

        Assert.AreEqual(ActivityResultType.Suspended, result.ResultType);
        Assert.AreEqual("Simple Task", result.Output!["title"]);
        Assert.IsNull(result.Output["description"]);
        Assert.IsNull(result.Output["assignedTo"]);
    }

    [TestMethod]
    public async Task UserTask_NoTitle_UsesDefault()
    {
        var context = CreateContext(new Dictionary<string, object?>());

        var activity = new UserTaskActivity();
        var result = await activity.ExecuteAsync(context);

        Assert.AreEqual(ActivityResultType.Suspended, result.ResultType);
        Assert.AreEqual("Untitled Task", result.Output!["title"]);
    }

    private static ActivityContext CreateContext(Dictionary<string, object?> properties)
    {
        return new ActivityContext(
            "test-usertask",
            properties,
            new Dictionary<string, object?>(),
            new EmptyServiceProvider());
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
