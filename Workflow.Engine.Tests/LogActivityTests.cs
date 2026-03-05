using Microsoft.Extensions.Logging;
using Workflow.Engine.Activities;

namespace Workflow.Engine.Tests;

[TestClass]
public sealed class LogActivityTests
{
    [TestMethod]
    public async Task Log_InfoLevel_LogsCorrectMessage()
    {
        var loggerFactory = new TestLoggerFactory();
        var context = CreateContext(
            new Dictionary<string, object?> { ["message"] = "Hello World", ["level"] = "Info" },
            loggerFactory);

        var activity = new LogActivity();
        var result = await activity.ExecuteAsync(context);

        Assert.AreEqual(ActivityResultType.Completed, result.ResultType);
        Assert.AreEqual(1, loggerFactory.Logger.Logs.Count);
        Assert.AreEqual(LogLevel.Information, loggerFactory.Logger.Logs[0].Level);
        Assert.AreEqual("Hello World", loggerFactory.Logger.Logs[0].Message);
    }

    [TestMethod]
    public async Task Log_WarningLevel_LogsAtWarning()
    {
        var loggerFactory = new TestLoggerFactory();
        var context = CreateContext(
            new Dictionary<string, object?> { ["message"] = "Watch out", ["level"] = "Warning" },
            loggerFactory);

        var activity = new LogActivity();
        var result = await activity.ExecuteAsync(context);

        Assert.AreEqual(ActivityResultType.Completed, result.ResultType);
        Assert.AreEqual(LogLevel.Warning, loggerFactory.Logger.Logs[0].Level);
    }

    [TestMethod]
    public async Task Log_ErrorLevel_LogsAtError()
    {
        var loggerFactory = new TestLoggerFactory();
        var context = CreateContext(
            new Dictionary<string, object?> { ["message"] = "Something broke", ["level"] = "Error" },
            loggerFactory);

        var activity = new LogActivity();
        var result = await activity.ExecuteAsync(context);

        Assert.AreEqual(ActivityResultType.Completed, result.ResultType);
        Assert.AreEqual(LogLevel.Error, loggerFactory.Logger.Logs[0].Level);
        Assert.AreEqual("Something broke", loggerFactory.Logger.Logs[0].Message);
    }

    [TestMethod]
    public async Task Log_DefaultLevel_UsesInformation()
    {
        var loggerFactory = new TestLoggerFactory();
        var context = CreateContext(
            new Dictionary<string, object?> { ["message"] = "Default level" },
            loggerFactory);

        var activity = new LogActivity();
        var result = await activity.ExecuteAsync(context);

        Assert.AreEqual(ActivityResultType.Completed, result.ResultType);
        Assert.AreEqual(LogLevel.Information, loggerFactory.Logger.Logs[0].Level);
    }

    [TestMethod]
    public async Task Log_NoLoggerFactory_CompletesWithoutError()
    {
        var context = CreateContext(
            new Dictionary<string, object?> { ["message"] = "No logger" });

        var activity = new LogActivity();
        var result = await activity.ExecuteAsync(context);

        Assert.AreEqual(ActivityResultType.Completed, result.ResultType);
    }

    private static ActivityContext CreateContext(Dictionary<string, object?> properties, ILoggerFactory? loggerFactory = null)
    {
        return new ActivityContext(
            "test-log",
            properties,
            new Dictionary<string, object?>(),
            new TestServiceProvider(loggerFactory));
    }

    private sealed class TestServiceProvider(ILoggerFactory? loggerFactory) : IServiceProvider
    {
        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(ILoggerFactory)) return loggerFactory;
            return null;
        }
    }

    private sealed class TestLoggerFactory : ILoggerFactory
    {
        public TestLogger Logger { get; } = new();
        public void AddProvider(ILoggerProvider provider) { }
        public ILogger CreateLogger(string categoryName) => Logger;
        public void Dispose() { }
    }

    private sealed class TestLogger : ILogger
    {
        public List<(LogLevel Level, string Message)> Logs { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Logs.Add((logLevel, formatter(state, exception)));
        }
    }
}
