namespace Workflow.Engine.Activities;

public sealed class ActivityRegistry
{
    private readonly Dictionary<string, Func<IServiceProvider, ActivityBase>> _factories = new(StringComparer.OrdinalIgnoreCase);

    public void Register<T>() where T : ActivityBase, new()
    {
        var instance = new T();
        _factories[instance.Type] = _ => new T();
    }

    public void Register<T>(Func<IServiceProvider, T> factory) where T : ActivityBase, new()
    {
        var temp = new T();
        _factories[temp.Type] = sp => factory(sp);
    }

    public void Register(string type, Func<IServiceProvider, ActivityBase> factory)
    {
        _factories[type] = factory;
    }

    public ActivityBase Resolve(string type, IServiceProvider serviceProvider)
    {
        if (!_factories.TryGetValue(type, out var factory))
            throw new InvalidOperationException($"Activity type '{type}' is not registered.");

        return factory(serviceProvider);
    }

    public IReadOnlyList<string> GetRegisteredTypes()
        => _factories.Keys.ToList().AsReadOnly();
}
