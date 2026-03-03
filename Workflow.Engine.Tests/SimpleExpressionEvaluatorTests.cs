using Workflow.Engine.Expressions;

namespace Workflow.Engine.Tests;

[TestClass]
public class SimpleExpressionEvaluatorTests
{
    private readonly SimpleExpressionEvaluator _evaluator = new();

    [TestMethod]
    public async Task EvaluateAsync_VariableSubstitution_ReplacesVariable()
    {
        var variables = new Dictionary<string, object?> { ["name"] = "World" };

        var result = await _evaluator.EvaluateAsync("Hello ${name}!", variables);

        Assert.AreEqual("Hello World!", result);
    }

    [TestMethod]
    public async Task EvaluateAsync_MultipleVariables_ReplacesAll()
    {
        var variables = new Dictionary<string, object?>
        {
            ["first"] = "John",
            ["last"] = "Doe"
        };

        var result = await _evaluator.EvaluateAsync("${first} ${last}", variables);

        Assert.AreEqual("John Doe", result);
    }

    [TestMethod]
    public async Task EvaluateAsync_MissingVariable_ReplacesWithEmpty()
    {
        var variables = new Dictionary<string, object?>();

        var result = await _evaluator.EvaluateAsync("Hello ${missing}!", variables);

        Assert.AreEqual("Hello !", result);
    }

    [TestMethod]
    public async Task EvaluateConditionAsync_StringEquals_ReturnsTrue()
    {
        var variables = new Dictionary<string, object?> { ["status"] = "approved" };

        var result = await _evaluator.EvaluateConditionAsync("${status} == \"approved\"", variables);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task EvaluateConditionAsync_StringEquals_ReturnsFalse()
    {
        var variables = new Dictionary<string, object?> { ["status"] = "rejected" };

        var result = await _evaluator.EvaluateConditionAsync("${status} == \"approved\"", variables);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task EvaluateConditionAsync_StringNotEquals_ReturnsTrue()
    {
        var variables = new Dictionary<string, object?> { ["status"] = "pending" };

        var result = await _evaluator.EvaluateConditionAsync("${status} != \"rejected\"", variables);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task EvaluateConditionAsync_NumericGreaterThan_ReturnsTrue()
    {
        var variables = new Dictionary<string, object?> { ["count"] = 10 };

        var result = await _evaluator.EvaluateConditionAsync("${count} > 5", variables);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task EvaluateConditionAsync_NumericLessThan_ReturnsFalse()
    {
        var variables = new Dictionary<string, object?> { ["count"] = 3 };

        var result = await _evaluator.EvaluateConditionAsync("${count} > 5", variables);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task EvaluateConditionAsync_NumericGreaterOrEqual_ReturnsTrue()
    {
        var variables = new Dictionary<string, object?> { ["count"] = 5 };

        var result = await _evaluator.EvaluateConditionAsync("${count} >= 5", variables);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task EvaluateConditionAsync_NumericLessOrEqual_ReturnsTrue()
    {
        var variables = new Dictionary<string, object?> { ["count"] = 5 };

        var result = await _evaluator.EvaluateConditionAsync("${count} <= 5", variables);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task EvaluateConditionAsync_BooleanTruthy_ReturnsTrue()
    {
        var variables = new Dictionary<string, object?> { ["isActive"] = true };

        var result = await _evaluator.EvaluateConditionAsync("${isActive}", variables);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task EvaluateConditionAsync_BooleanFalsy_ReturnsFalse()
    {
        var variables = new Dictionary<string, object?> { ["isActive"] = false };

        var result = await _evaluator.EvaluateConditionAsync("${isActive}", variables);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task EvaluateConditionAsync_NullVariable_ReturnsFalse()
    {
        var variables = new Dictionary<string, object?> { ["value"] = null };

        var result = await _evaluator.EvaluateConditionAsync("${value}", variables);

        Assert.IsFalse(result);
    }
}
