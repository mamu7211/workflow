using Workflow.Engine.Activities;

namespace Workflow.Engine.Tests;

[TestClass]
public sealed class DelayActivityTests
{
    [TestMethod]
    public async Task Delay_ValidDuration_ReturnsSuspendWithResumeAt()
    {
        var before = DateTime.UtcNow;
        var context = CreateContext(new Dictionary<string, object?> { ["duration"] = "00:05:00" });

        var activity = new DelayActivity();
        var result = await activity.ExecuteAsync(context);

        Assert.AreEqual(ActivityResultType.Suspended, result.ResultType);
        Assert.IsNotNull(result.Output);
        Assert.IsTrue(result.Output!.ContainsKey("resumeAt"));

        var resumeAt = (DateTime)result.Output["resumeAt"]!;
        var expectedMin = before.AddMinutes(5);
        Assert.IsTrue(resumeAt >= expectedMin.AddSeconds(-1), $"ResumeAt {resumeAt} should be approximately 5 minutes from now");
        Assert.IsTrue(resumeAt <= expectedMin.AddSeconds(5), $"ResumeAt {resumeAt} should not be too far in the future");
    }

    [TestMethod]
    public async Task Delay_ShortDuration_ReturnsSuspend()
    {
        var context = CreateContext(new Dictionary<string, object?> { ["duration"] = "00:00:30" });

        var activity = new DelayActivity();
        var result = await activity.ExecuteAsync(context);

        Assert.AreEqual(ActivityResultType.Suspended, result.ResultType);
        Assert.IsNotNull(result.SuspendReason);
        Assert.IsTrue(result.SuspendReason!.Contains("Delay until"));
    }

    [TestMethod]
    public async Task Delay_MissingDuration_ReturnsFaulted()
    {
        var context = CreateContext(new Dictionary<string, object?>());

        var activity = new DelayActivity();
        var result = await activity.ExecuteAsync(context);

        Assert.AreEqual(ActivityResultType.Faulted, result.ResultType);
        Assert.IsTrue(result.Error!.Contains("duration"));
    }

    [TestMethod]
    public async Task Delay_InvalidDuration_ReturnsFaulted()
    {
        var context = CreateContext(new Dictionary<string, object?> { ["duration"] = "not-a-timespan" });

        var activity = new DelayActivity();
        var result = await activity.ExecuteAsync(context);

        Assert.AreEqual(ActivityResultType.Faulted, result.ResultType);
    }

    private static ActivityContext CreateContext(Dictionary<string, object?> properties)
    {
        return new ActivityContext(
            "test-delay",
            properties,
            new Dictionary<string, object?>(),
            new EmptyServiceProvider());
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
