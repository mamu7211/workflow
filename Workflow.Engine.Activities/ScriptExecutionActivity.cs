using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace Workflow.Engine.Activities;

public sealed class ScriptExecutionActivity : ActivityBase
{
    public override string Type => "ScriptExecution";

    public override async Task<ActivityResult> ExecuteAsync(ActivityContext context, CancellationToken cancellationToken = default)
    {
        var script = context.GetProperty<string>("script");
        var timeoutSeconds = context.GetProperty<int>("timeout");
        if (timeoutSeconds <= 0) timeoutSeconds = 5;

        if (string.IsNullOrEmpty(script))
            return ActivityResult.Faulted("Property 'script' is required.");

        try
        {
            var options = ScriptOptions.Default
                .WithReferences(typeof(object).Assembly, typeof(Enumerable).Assembly)
                .WithImports("System", "System.Collections.Generic", "System.Linq");

            var globals = new ScriptGlobals { Variables = context.Variables };

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            var result = await CSharpScript.EvaluateAsync<object?>(
                script,
                options,
                globals,
                typeof(ScriptGlobals),
                cts.Token);

            return ActivityResult.Completed(new Dictionary<string, object?>
            {
                ["result"] = result
            });
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ActivityResult.Faulted($"Script execution timed out after {timeoutSeconds} seconds.");
        }
        catch (CompilationErrorException ex)
        {
            return ActivityResult.Faulted($"Script compilation error: {string.Join("; ", ex.Diagnostics)}");
        }
        catch (Exception ex)
        {
            return ActivityResult.Faulted($"Script execution failed: {ex.Message}");
        }
    }
}

public sealed class ScriptGlobals
{
    public Dictionary<string, object?> Variables { get; set; } = [];
}
