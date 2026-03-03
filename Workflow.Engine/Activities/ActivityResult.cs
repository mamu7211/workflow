namespace Workflow.Engine.Activities;

public sealed class ActivityResult
{
    public ActivityResultType ResultType { get; }
    public Dictionary<string, object?>? Output { get; }
    public string? Error { get; }
    public string? SuspendReason { get; }

    private ActivityResult(ActivityResultType resultType, Dictionary<string, object?>? output = null, string? error = null, string? suspendReason = null)
    {
        ResultType = resultType;
        Output = output;
        Error = error;
        SuspendReason = suspendReason;
    }

    public static ActivityResult Completed(Dictionary<string, object?>? output = null)
        => new(ActivityResultType.Completed, output: output);

    public static ActivityResult Faulted(string error)
        => new(ActivityResultType.Faulted, error: error);

    public static ActivityResult SuspendExecution(string reason)
        => new(ActivityResultType.Suspended, suspendReason: reason);
}

public enum ActivityResultType
{
    Completed,
    Faulted,
    Suspended
}
