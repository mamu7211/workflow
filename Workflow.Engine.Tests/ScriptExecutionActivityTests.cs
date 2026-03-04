using Workflow.Engine.Activities;

namespace Workflow.Engine.Tests;

[TestClass]
public sealed class ScriptExecutionActivityTests
{
    [TestMethod]
    public async Task ScriptExecution_SimpleScript_ReturnsResult()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            ["script"] = "1 + 2"
        });

        var activity = new ScriptExecutionActivity();
        var result = await activity.ExecuteAsync(context);

        Assert.AreEqual(ActivityResultType.Completed, result.ResultType);
        Assert.AreEqual(3, result.Output!["result"]);
    }

    [TestMethod]
    public async Task ScriptExecution_AccessVariables_ReturnsResult()
    {
        var variables = new Dictionary<string, object?>
        {
            ["counter"] = 10
        };
        var context = CreateContext(
            new Dictionary<string, object?> { ["script"] = "(int)Variables[\"counter\"] * 2" },
            variables);

        var activity = new ScriptExecutionActivity();
        var result = await activity.ExecuteAsync(context);

        Assert.AreEqual(ActivityResultType.Completed, result.ResultType);
        Assert.AreEqual(20, result.Output!["result"]);
    }

    [TestMethod]
    public async Task ScriptExecution_StringScript_ReturnsString()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            ["script"] = "\"Hello\" + \" \" + \"World\""
        });

        var activity = new ScriptExecutionActivity();
        var result = await activity.ExecuteAsync(context);

        Assert.AreEqual(ActivityResultType.Completed, result.ResultType);
        Assert.AreEqual("Hello World", result.Output!["result"]);
    }

    [TestMethod]
    public async Task ScriptExecution_CompilationError_ReturnsFaulted()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            ["script"] = "this is not valid C#!!!"
        });

        var activity = new ScriptExecutionActivity();
        var result = await activity.ExecuteAsync(context);

        Assert.AreEqual(ActivityResultType.Faulted, result.ResultType);
        Assert.IsTrue(result.Error!.Contains("Script compilation error") || result.Error.Contains("Script execution failed"));
    }

    [TestMethod]
    public async Task ScriptExecution_Timeout_ReturnsFaulted()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            ["script"] = "while(true) { }",
            ["timeout"] = 1
        });

        var activity = new ScriptExecutionActivity();
        var result = await activity.ExecuteAsync(context);

        Assert.AreEqual(ActivityResultType.Faulted, result.ResultType);
        Assert.IsTrue(result.Error!.Contains("timed out"));
    }

    [TestMethod]
    public async Task ScriptExecution_MissingScript_ReturnsFaulted()
    {
        var context = CreateContext(new Dictionary<string, object?>());

        var activity = new ScriptExecutionActivity();
        var result = await activity.ExecuteAsync(context);

        Assert.AreEqual(ActivityResultType.Faulted, result.ResultType);
        Assert.IsTrue(result.Error!.Contains("script"));
    }

    [TestMethod]
    public async Task ScriptExecution_LinqUsage_Works()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            ["script"] = "new[] { 1, 2, 3, 4, 5 }.Where(x => x > 3).Sum()"
        });

        var activity = new ScriptExecutionActivity();
        var result = await activity.ExecuteAsync(context);

        Assert.AreEqual(ActivityResultType.Completed, result.ResultType);
        Assert.AreEqual(9, result.Output!["result"]);
    }

    private static ActivityContext CreateContext(Dictionary<string, object?> properties, Dictionary<string, object?>? variables = null)
    {
        return new ActivityContext(
            "test-script",
            properties,
            variables ?? new Dictionary<string, object?>(),
            new EmptyServiceProvider());
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
