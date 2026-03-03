using Workflow.Engine.Activities;

namespace Workflow.Engine.Tests;

[TestClass]
public class ActivityRegistryTests
{
    private sealed class TestActivity : ActivityBase
    {
        public override string Type => "Test";

        public override Task<ActivityResult> ExecuteAsync(ActivityContext context, CancellationToken cancellationToken = default)
            => Task.FromResult(ActivityResult.Completed());
    }

    private sealed class AnotherActivity : ActivityBase
    {
        public override string Type => "Another";

        public override Task<ActivityResult> ExecuteAsync(ActivityContext context, CancellationToken cancellationToken = default)
            => Task.FromResult(ActivityResult.Completed());
    }

    [TestMethod]
    public void Register_AndResolve_ReturnsActivity()
    {
        var registry = new ActivityRegistry();
        registry.Register<TestActivity>();

        var activity = registry.Resolve("Test", null!);

        Assert.IsNotNull(activity);
        Assert.AreEqual("Test", activity.Type);
        Assert.IsInstanceOfType<TestActivity>(activity);
    }

    [TestMethod]
    public void Resolve_UnknownType_ThrowsException()
    {
        var registry = new ActivityRegistry();

        Assert.ThrowsExactly<InvalidOperationException>(
            () => registry.Resolve("Unknown", null!));
    }

    [TestMethod]
    public void GetRegisteredTypes_ReturnsAllTypes()
    {
        var registry = new ActivityRegistry();
        registry.Register<TestActivity>();
        registry.Register<AnotherActivity>();

        var types = registry.GetRegisteredTypes();

        Assert.AreEqual(2, types.Count);
        CollectionAssert.Contains(types.ToList(), "Test");
        CollectionAssert.Contains(types.ToList(), "Another");
    }

    [TestMethod]
    public void Register_WithFactory_ResolvesCorrectly()
    {
        var registry = new ActivityRegistry();
        registry.Register(sp => new TestActivity());

        var activity = registry.Resolve("Test", null!);

        Assert.IsNotNull(activity);
        Assert.AreEqual("Test", activity.Type);
    }

    [TestMethod]
    public void Resolve_IsCaseInsensitive()
    {
        var registry = new ActivityRegistry();
        registry.Register<TestActivity>();

        var activity = registry.Resolve("test", null!);

        Assert.IsNotNull(activity);
        Assert.AreEqual("Test", activity.Type);
    }
}
