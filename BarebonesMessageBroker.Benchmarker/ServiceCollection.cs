using Microsoft.Extensions.DependencyInjection;

internal class ServiceCollection : IServiceProvider
{
    private readonly Dictionary<Type, object> _services = new();
    public object? GetService(Type serviceType)
    {
        if (_services.TryGetValue(serviceType, out var service))
        {
            return service;
        }
        throw new InvalidOperationException($"Service of type {serviceType.Name} not found.");
    }
    public void AddService<TService>(TService service)
    {
        _services[typeof(TService)] = service;
    }
}

internal class ScopeFactory : IServiceScopeFactory
{
    public IServiceScope CreateScope()
    {
        return new scoper();

    }
}

internal class scoper : IServiceScope
{
    public IServiceProvider ServiceProvider { get; private set; }
    public scoper()
    {
        ServiceProvider = new ServiceCollection();
    }
    public void Dispose()
    {
        ServiceProvider = null!;
    }
}