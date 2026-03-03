using System.Text.Json;
using System.Text.RegularExpressions;

namespace Workflow.Engine.Expressions;

public sealed partial class SimpleExpressionEvaluator : IExpressionEvaluator
{
    [GeneratedRegex(@"\$\{([^}]+)\}")]
    private static partial Regex VariablePattern();

    public Task<object?> EvaluateAsync(string expression, Dictionary<string, object?> variables)
    {
        var result = SubstituteVariables(expression, variables);
        return Task.FromResult<object?>(result);
    }

    public Task<bool> EvaluateConditionAsync(string condition, Dictionary<string, object?> variables)
    {
        var trimmed = condition.Trim();

        // Comparison operators: ==, !=, >, <, >=, <=
        if (TryEvaluateComparison(trimmed, "==", variables, out var eqResult))
            return Task.FromResult(eqResult);
        if (TryEvaluateComparison(trimmed, "!=", variables, out var neqResult))
            return Task.FromResult(neqResult);
        if (TryEvaluateComparison(trimmed, ">=", variables, out var gteResult))
            return Task.FromResult(gteResult);
        if (TryEvaluateComparison(trimmed, "<=", variables, out var lteResult))
            return Task.FromResult(lteResult);
        if (TryEvaluateComparison(trimmed, ">", variables, out var gtResult))
            return Task.FromResult(gtResult);
        if (TryEvaluateComparison(trimmed, "<", variables, out var ltResult))
            return Task.FromResult(ltResult);

        // Boolean truthy check: ${isActive}
        var value = SubstituteVariables(trimmed, variables);
        return Task.FromResult(IsTruthy(value));
    }

    private string SubstituteVariables(string expression, Dictionary<string, object?> variables)
    {
        return VariablePattern().Replace(expression, match =>
        {
            var varName = match.Groups[1].Value;
            var value = ResolveVariable(varName, variables);
            return value?.ToString() ?? string.Empty;
        });
    }

    private static object? ResolveVariable(string path, Dictionary<string, object?> variables)
    {
        var parts = path.Split('.');
        object? current = null;

        if (!variables.TryGetValue(parts[0], out current))
            return null;

        for (var i = 1; i < parts.Length; i++)
        {
            if (current is null) return null;

            if (current is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.Object && jsonElement.TryGetProperty(parts[i], out var prop))
                    current = prop;
                else
                    return null;
            }
            else if (current is Dictionary<string, object?> dict)
            {
                if (!dict.TryGetValue(parts[i], out current))
                    return null;
            }
            else
            {
                return null;
            }
        }

        return UnwrapJsonElement(current);
    }

    private static object? UnwrapJsonElement(object? value)
    {
        if (value is not JsonElement element) return value;

        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.GetRawText()
        };
    }

    private bool TryEvaluateComparison(string condition, string op, Dictionary<string, object?> variables, out bool result)
    {
        result = false;

        // For >= and <=, don't match single > or <
        var index = op.Length == 2
            ? condition.IndexOf(op, StringComparison.Ordinal)
            : FindSingleCharOperator(condition, op[0]);

        if (index < 0) return false;

        var left = SubstituteVariables(condition[..index].Trim(), variables);
        var right = StripQuotes(SubstituteVariables(condition[(index + op.Length)..].Trim(), variables));

        result = op switch
        {
            "==" => string.Equals(left, right, StringComparison.Ordinal),
            "!=" => !string.Equals(left, right, StringComparison.Ordinal),
            ">" => ToDouble(left) > ToDouble(right),
            "<" => ToDouble(left) < ToDouble(right),
            ">=" => ToDouble(left) >= ToDouble(right),
            "<=" => ToDouble(left) <= ToDouble(right),
            _ => false
        };

        return true;
    }

    private static int FindSingleCharOperator(string condition, char op)
    {
        for (var i = 0; i < condition.Length; i++)
        {
            if (condition[i] == op)
            {
                // Skip if part of ==, !=, >=, <=
                if (i > 0 && (condition[i - 1] == '!' || condition[i - 1] == '>' || condition[i - 1] == '<'))
                    continue;
                if (i + 1 < condition.Length && condition[i + 1] == '=')
                    continue;
                // Skip if inside ${...}
                if (IsInsideVariable(condition, i))
                    continue;
                return i;
            }
        }
        return -1;
    }

    private static bool IsInsideVariable(string text, int position)
    {
        var lastOpen = text.LastIndexOf("${", position, StringComparison.Ordinal);
        if (lastOpen < 0) return false;
        var closeAfterOpen = text.IndexOf('}', lastOpen);
        return closeAfterOpen > position;
    }

    private static string StripQuotes(string value)
    {
        if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
            return value[1..^1];
        return value;
    }

    private static double ToDouble(string value)
    {
        return double.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : 0;
    }

    private static bool IsTruthy(object? value)
    {
        return value switch
        {
            null => false,
            bool b => b,
            string s => !string.IsNullOrEmpty(s) && !s.Equals("false", StringComparison.OrdinalIgnoreCase),
            int i => i != 0,
            long l => l != 0,
            double d => d != 0,
            _ => true
        };
    }
}
