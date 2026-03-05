namespace Workflow.Engine.Activities;

public static class ServiceCollectionExtensions
{
    public static ActivityRegistry AddBuiltInActivities(this ActivityRegistry registry)
    {
        registry.Register<LogActivity>();
        registry.Register<SetVariableActivity>();
        registry.Register<DelayActivity>();
        registry.Register<SendEmailActivity>();
        registry.Register<HttpRequestActivity>();
        registry.Register<UserTaskActivity>();
        registry.Register<RabbitMqPublishActivity>();
        registry.Register<RabbitMqSubscribeActivity>();
        registry.Register<DatabaseQueryActivity>();
        registry.Register<ScriptExecutionActivity>();
        registry.Register<WebhookTriggerActivity>();
        return registry;
    }
}
