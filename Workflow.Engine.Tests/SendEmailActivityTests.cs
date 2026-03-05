using MailKit;
using MimeKit;
using NSubstitute;
using Workflow.Engine.Activities;

namespace Workflow.Engine.Tests;

[TestClass]
public sealed class SendEmailActivityTests
{
    [TestMethod]
    public async Task SendEmail_ValidProperties_SendsCorrectMessage()
    {
        var transport = Substitute.For<IMailTransport>();
        transport.SendAsync(Arg.Any<MimeMessage>(), Arg.Any<CancellationToken>(), Arg.Any<ITransferProgress>())
            .Returns("test-message-id");

        var context = CreateContext(
            new Dictionary<string, object?>
            {
                ["to"] = "recipient@example.com",
                ["subject"] = "Test Subject",
                ["body"] = "Hello World",
                ["isHtml"] = false
            },
            transport);

        var activity = new SendEmailActivity();
        var result = await activity.ExecuteAsync(context);

        Assert.AreEqual(ActivityResultType.Completed, result.ResultType);
        Assert.AreEqual("test-message-id", result.Output!["messageId"]);

        await transport.Received(1).SendAsync(
            Arg.Is<MimeMessage>(m =>
                m.To.Mailboxes.First().Address == "recipient@example.com" &&
                m.Subject == "Test Subject"),
            Arg.Any<CancellationToken>(),
            Arg.Any<ITransferProgress>());
    }

    [TestMethod]
    public async Task SendEmail_HtmlBody_SetsHtmlContentType()
    {
        var transport = Substitute.For<IMailTransport>();
        transport.SendAsync(Arg.Any<MimeMessage>(), Arg.Any<CancellationToken>(), Arg.Any<ITransferProgress>())
            .Returns("html-message-id");

        var context = CreateContext(
            new Dictionary<string, object?>
            {
                ["to"] = "user@example.com",
                ["subject"] = "HTML Test",
                ["body"] = "<h1>Hello</h1>",
                ["isHtml"] = true
            },
            transport);

        var activity = new SendEmailActivity();
        var result = await activity.ExecuteAsync(context);

        Assert.AreEqual(ActivityResultType.Completed, result.ResultType);

        await transport.Received(1).SendAsync(
            Arg.Is<MimeMessage>(m =>
                m.Body != null && m.Body.ContentType.MimeType == "text/html"),
            Arg.Any<CancellationToken>(),
            Arg.Any<ITransferProgress>());
    }

    [TestMethod]
    public async Task SendEmail_MissingTo_ReturnsFaulted()
    {
        var context = CreateContext(
            new Dictionary<string, object?>
            {
                ["subject"] = "Test",
                ["body"] = "Body"
            });

        var activity = new SendEmailActivity();
        var result = await activity.ExecuteAsync(context);

        Assert.AreEqual(ActivityResultType.Faulted, result.ResultType);
        Assert.IsTrue(result.Error!.Contains("'to'"));
    }

    [TestMethod]
    public async Task SendEmail_ExpressionResolved_UsesEvaluatedValue()
    {
        var transport = Substitute.For<IMailTransport>();
        transport.SendAsync(Arg.Any<MimeMessage>(), Arg.Any<CancellationToken>(), Arg.Any<ITransferProgress>())
            .Returns("expr-message-id");

        // Expressions are already evaluated by the engine before reaching the activity
        var context = CreateContext(
            new Dictionary<string, object?>
            {
                ["to"] = "resolved@example.com",
                ["subject"] = "Resolved Subject",
                ["body"] = "Resolved Body"
            },
            transport);

        var activity = new SendEmailActivity();
        var result = await activity.ExecuteAsync(context);

        Assert.AreEqual(ActivityResultType.Completed, result.ResultType);
        await transport.Received(1).SendAsync(
            Arg.Is<MimeMessage>(m =>
                m.To.Mailboxes.First().Address == "resolved@example.com" &&
                m.Subject == "Resolved Subject"),
            Arg.Any<CancellationToken>(),
            Arg.Any<ITransferProgress>());
    }

    private static ActivityContext CreateContext(Dictionary<string, object?> properties, IMailTransport? transport = null)
    {
        return new ActivityContext(
            "test-sendemail",
            properties,
            new Dictionary<string, object?>(),
            new TestServiceProvider(transport));
    }

    private sealed class TestServiceProvider(IMailTransport? transport) : IServiceProvider
    {
        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(IMailTransport)) return transport;
            return null;
        }
    }
}
