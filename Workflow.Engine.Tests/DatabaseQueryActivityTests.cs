using Workflow.Engine.Activities;

namespace Workflow.Engine.Tests;

[TestClass]
public sealed class DatabaseQueryActivityTests
{
    [TestMethod]
    public async Task DatabaseQuery_NonSelectQuery_ReturnsFaulted()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            ["connectionString"] = "Host=localhost;Database=test",
            ["query"] = "DELETE FROM users WHERE id = 1"
        });

        var activity = new DatabaseQueryActivity();
        var result = await activity.ExecuteAsync(context);

        Assert.AreEqual(ActivityResultType.Faulted, result.ResultType);
        Assert.IsTrue(result.Error!.Contains("Only SELECT"));
    }

    [TestMethod]
    public async Task DatabaseQuery_InsertQuery_ReturnsFaulted()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            ["connectionString"] = "Host=localhost;Database=test",
            ["query"] = "INSERT INTO users (name) VALUES ('test')"
        });

        var activity = new DatabaseQueryActivity();
        var result = await activity.ExecuteAsync(context);

        Assert.AreEqual(ActivityResultType.Faulted, result.ResultType);
        Assert.IsTrue(result.Error!.Contains("Only SELECT"));
    }

    [TestMethod]
    public async Task DatabaseQuery_UpdateQuery_ReturnsFaulted()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            ["connectionString"] = "Host=localhost;Database=test",
            ["query"] = "UPDATE users SET name = 'x' WHERE id = 1"
        });

        var activity = new DatabaseQueryActivity();
        var result = await activity.ExecuteAsync(context);

        Assert.AreEqual(ActivityResultType.Faulted, result.ResultType);
        Assert.IsTrue(result.Error!.Contains("Only SELECT"));
    }

    [TestMethod]
    public async Task DatabaseQuery_DropQuery_ReturnsFaulted()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            ["connectionString"] = "Host=localhost;Database=test",
            ["query"] = "DROP TABLE users"
        });

        var activity = new DatabaseQueryActivity();
        var result = await activity.ExecuteAsync(context);

        Assert.AreEqual(ActivityResultType.Faulted, result.ResultType);
        Assert.IsTrue(result.Error!.Contains("Only SELECT"));
    }

    [TestMethod]
    public async Task DatabaseQuery_MissingConnectionString_ReturnsFaulted()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            ["query"] = "SELECT 1"
        });

        var activity = new DatabaseQueryActivity();
        var result = await activity.ExecuteAsync(context);

        Assert.AreEqual(ActivityResultType.Faulted, result.ResultType);
        Assert.IsTrue(result.Error!.Contains("connectionString"));
    }

    [TestMethod]
    public async Task DatabaseQuery_MissingQuery_ReturnsFaulted()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            ["connectionString"] = "Host=localhost;Database=test"
        });

        var activity = new DatabaseQueryActivity();
        var result = await activity.ExecuteAsync(context);

        Assert.AreEqual(ActivityResultType.Faulted, result.ResultType);
        Assert.IsTrue(result.Error!.Contains("query"));
    }

    [TestMethod]
    public async Task DatabaseQuery_SelectQuery_PassesValidation()
    {
        // This will fail at connection level (no real DB), but should pass validation
        var context = CreateContext(new Dictionary<string, object?>
        {
            ["connectionString"] = "Host=invalid-host;Database=test",
            ["query"] = "SELECT * FROM users WHERE id = @id",
            ["parameters"] = new Dictionary<string, object?> { ["id"] = 1 }
        });

        var activity = new DatabaseQueryActivity();
        var result = await activity.ExecuteAsync(context);

        // Should fail at connection level, not validation level
        Assert.AreEqual(ActivityResultType.Faulted, result.ResultType);
        Assert.IsTrue(result.Error!.Contains("Database query failed"));
        Assert.IsFalse(result.Error.Contains("Only SELECT"));
    }

    [TestMethod]
    public async Task DatabaseQuery_SelectWithWhitespace_PassesValidation()
    {
        var context = CreateContext(new Dictionary<string, object?>
        {
            ["connectionString"] = "Host=invalid-host;Database=test",
            ["query"] = "  SELECT id, name FROM users"
        });

        var activity = new DatabaseQueryActivity();
        var result = await activity.ExecuteAsync(context);

        // Should pass validation (SELECT with leading whitespace)
        Assert.AreEqual(ActivityResultType.Faulted, result.ResultType);
        Assert.IsFalse(result.Error!.Contains("Only SELECT"));
    }

    private static ActivityContext CreateContext(Dictionary<string, object?> properties)
    {
        return new ActivityContext(
            "test-dbquery",
            properties,
            new Dictionary<string, object?>(),
            new EmptyServiceProvider());
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
