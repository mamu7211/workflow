using Workflow.Engine.Activities;

namespace Workflow.Engine.Tests;

[TestClass]
public sealed class WebhookTriggerActivityTests
{
    [TestMethod]
    public async Task WebhookTrigger_ReturnsSuspendWithCorrelationId()
    {
        var context = CreateContext(new Dictionary<string, object?>());

        var activity = new WebhookTriggerActivity();
        var result = await activity.ExecuteAsync(context);

        Assert.AreEqual(ActivityResultType.Suspended, result.ResultType);
        Assert.IsNotNull(result.Output!["correlationId"]);
        Assert.AreEqual("POST", result.Output["expectedMethod"]);

        // Auto-generated correlation ID should be a valid GUID
        Assert.IsTrue(Guid.TryParse(result.Output["correlationId"]!.ToString(), out _));
    }

    [TestMethod]
    public async Task WebhookTrigger_WithPath_UsesPathAsCorrelationId()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            ["path"] = "my-webhook-endpoint"
        });

        var activity = new WebhookTriggerActivity();
        var result = await activity.ExecuteAsync(context);

        Assert.AreEqual(ActivityResultType.Suspended, result.ResultType);
        Assert.AreEqual("my-webhook-endpoint", result.Output!["correlationId"]);
    }

    [TestMethod]
    public async Task WebhookTrigger_CustomMethod_ReturnsExpectedMethod()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            ["expectedMethod"] = "PUT"
        });

        var activity = new WebhookTriggerActivity();
        var result = await activity.ExecuteAsync(context);

        Assert.AreEqual(ActivityResultType.Suspended, result.ResultType);
        Assert.AreEqual("PUT", result.Output!["expectedMethod"]);
    }

    [TestMethod]
    public async Task WebhookTrigger_SuspendReason_ContainsCorrelationId()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            ["path"] = "order-webhook"
        });

        var activity = new WebhookTriggerActivity();
        var result = await activity.ExecuteAsync(context);

        Assert.IsTrue(result.SuspendReason!.Contains("order-webhook"));
    }

    private static ActivityContext CreateContext(Dictionary<string, object?> properties)
    {
        return new ActivityContext(
            "test-webhook",
            properties,
            new Dictionary<string, object?>(),
            new EmptyServiceProvider());
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
