using Workflow.Engine.Activities;

namespace Workflow.Engine.Tests;

[TestClass]
public sealed class RabbitMqSubscribeActivityTests
{
    [TestMethod]
    public async Task RabbitMqSubscribe_ReturnsSuspendWithQueueName()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            ["queueName"] = "my-queue",
            ["timeout"] = "00:30:00"
        });

        var activity = new RabbitMqSubscribeActivity();
        var result = await activity.ExecuteAsync(context);

        Assert.AreEqual(ActivityResultType.Suspended, result.ResultType);
        Assert.IsTrue(result.SuspendReason!.Contains("my-queue"));
        Assert.AreEqual("my-queue", result.Output!["queueName"]);
        Assert.AreEqual("00:30:00", result.Output["timeout"]);
    }

    [TestMethod]
    public async Task RabbitMqSubscribe_MissingQueueName_ReturnsFaulted()
    {
        var context = CreateContext(new Dictionary<string, object?>());

        var activity = new RabbitMqSubscribeActivity();
        var result = await activity.ExecuteAsync(context);

        Assert.AreEqual(ActivityResultType.Faulted, result.ResultType);
        Assert.IsTrue(result.Error!.Contains("queueName"));
    }

    [TestMethod]
    public async Task RabbitMqSubscribe_NoTimeout_OutputContainsNull()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            ["queueName"] = "events"
        });

        var activity = new RabbitMqSubscribeActivity();
        var result = await activity.ExecuteAsync(context);

        Assert.AreEqual(ActivityResultType.Suspended, result.ResultType);
        Assert.AreEqual("events", result.Output!["queueName"]);
        Assert.IsNull(result.Output["timeout"]);
    }

    private static ActivityContext CreateContext(Dictionary<string, object?> properties)
    {
        return new ActivityContext(
            "test-rabbitmq-subscribe",
            properties,
            new Dictionary<string, object?>(),
            new EmptyServiceProvider());
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
