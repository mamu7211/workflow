using Workflow.Engine.Activities;

namespace Workflow.Engine.Tests;

[TestClass]
public sealed class SetVariableActivityTests
{
    [TestMethod]
    public async Task SetVariable_SetsVariableInContext()
    {
        var variables = new Dictionary<string, object?>();
        var context = CreateContext(
            new Dictionary<string, object?> { ["variableName"] = "myVar", ["value"] = "hello" },
            variables);

        var activity = new SetVariableActivity();
        var result = await activity.ExecuteAsync(context);

        Assert.AreEqual(ActivityResultType.Completed, result.ResultType);
        Assert.AreEqual("hello", variables["myVar"]);
    }

    [TestMethod]
    public async Task SetVariable_OverwritesExistingVariable()
    {
        var variables = new Dictionary<string, object?> { ["myVar"] = "old" };
        var context = CreateContext(
            new Dictionary<string, object?> { ["variableName"] = "myVar", ["value"] = "new" },
            variables);

        var activity = new SetVariableActivity();
        var result = await activity.ExecuteAsync(context);

        Assert.AreEqual(ActivityResultType.Completed, result.ResultType);
        Assert.AreEqual("new", variables["myVar"]);
    }

    [TestMethod]
    public async Task SetVariable_NullValue_SetsNull()
    {
        var variables = new Dictionary<string, object?>();
        var context = CreateContext(
            new Dictionary<string, object?> { ["variableName"] = "myVar" },
            variables);

        var activity = new SetVariableActivity();
        var result = await activity.ExecuteAsync(context);

        Assert.AreEqual(ActivityResultType.Completed, result.ResultType);
        Assert.IsTrue(variables.ContainsKey("myVar"));
        Assert.IsNull(variables["myVar"]);
    }

    [TestMethod]
    public async Task SetVariable_MissingVariableName_ReturnsFaulted()
    {
        var context = CreateContext(new Dictionary<string, object?>());

        var activity = new SetVariableActivity();
        var result = await activity.ExecuteAsync(context);

        Assert.AreEqual(ActivityResultType.Faulted, result.ResultType);
        Assert.IsTrue(result.Error!.Contains("variableName"));
    }

    private static ActivityContext CreateContext(Dictionary<string, object?> properties, Dictionary<string, object?>? variables = null)
    {
        return new ActivityContext(
            "test-setvariable",
            properties,
            variables ?? new Dictionary<string, object?>(),
            new EmptyServiceProvider());
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
