using NSubstitute;
using RabbitMQ.Client;
using Workflow.Engine.Activities;

namespace Workflow.Engine.Tests;

[TestClass]
public sealed class RabbitMqPublishActivityTests
{
    [TestMethod]
    public async Task RabbitMqPublish_PublishesMessageToCorrectExchange()
    {
        var channel = Substitute.For<IChannel>();
        var connection = Substitute.For<IConnection>();
        connection.CreateChannelAsync(Arg.Any<CreateChannelOptions>(), Arg.Any<CancellationToken>())
            .Returns(channel);

        var context = CreateContext(
            new Dictionary<string, object?>
            {
                ["exchange"] = "my-exchange",
                ["routingKey"] = "my-key",
                ["message"] = "Hello RabbitMQ"
            },
            connection);

        var activity = new RabbitMqPublishActivity();
        var result = await activity.ExecuteAsync(context);

        Assert.AreEqual(ActivityResultType.Completed, result.ResultType);

        // Verify a channel was created from the connection to publish the message
        await connection.Received(1).CreateChannelAsync(
            Arg.Any<CreateChannelOptions>(),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task RabbitMqPublish_NoConnection_ReturnsFaulted()
    {
        var context = CreateContext(
            new Dictionary<string, object?>
            {
                ["exchange"] = "test",
                ["routingKey"] = "key",
                ["message"] = "msg"
            },
            null);

        var activity = new RabbitMqPublishActivity();
        var result = await activity.ExecuteAsync(context);

        Assert.AreEqual(ActivityResultType.Faulted, result.ResultType);
        Assert.IsTrue(result.Error!.Contains("Failed to publish"));
    }

    [TestMethod]
    public async Task RabbitMqPublish_EmptyExchange_UsesDefault()
    {
        var channel = Substitute.For<IChannel>();
        var connection = Substitute.For<IConnection>();
        connection.CreateChannelAsync(Arg.Any<CreateChannelOptions>(), Arg.Any<CancellationToken>())
            .Returns(channel);

        var context = CreateContext(
            new Dictionary<string, object?>
            {
                ["routingKey"] = "direct-key",
                ["message"] = "Direct message"
            },
            connection);

        var activity = new RabbitMqPublishActivity();
        var result = await activity.ExecuteAsync(context);

        Assert.AreEqual(ActivityResultType.Completed, result.ResultType);
    }

    private static ActivityContext CreateContext(Dictionary<string, object?> properties, IConnection? connection)
    {
        return new ActivityContext(
            "test-rabbitmq-publish",
            properties,
            new Dictionary<string, object?>(),
            new TestServiceProvider(connection));
    }

    private sealed class TestServiceProvider(IConnection? connection) : IServiceProvider
    {
        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(IConnection)) return connection;
            return null;
        }
    }
}
