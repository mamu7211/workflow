namespace Workflow.Engine.Activities;

public sealed class SetVariableActivity : ActivityBase
{
    public override string Type => "SetVariable";

    public override Task<ActivityResult> ExecuteAsync(ActivityContext context, CancellationToken cancellationToken = default)
    {
        var variableName = context.GetProperty<string>("variableName");

        if (string.IsNullOrEmpty(variableName))
            return Task.FromResult(ActivityResult.Faulted("Property 'variableName' is required."));

        var value = context.Properties.GetValueOrDefault("value");
        context.SetVariable(variableName, value);

        return Task.FromResult(ActivityResult.Completed());
    }
}
