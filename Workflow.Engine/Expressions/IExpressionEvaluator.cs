namespace Workflow.Engine.Expressions;

public interface IExpressionEvaluator
{
    Task<object?> EvaluateAsync(string expression, Dictionary<string, object?> variables);
    Task<bool> EvaluateConditionAsync(string condition, Dictionary<string, object?> variables);
}
