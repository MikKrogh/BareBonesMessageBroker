public class ServiceCollection : IServiceProvider
{
    private readonly Dictionary<Type, object> _services = new();
    public void AddService<TService>(TService service)
    {
        _services[typeof(TService)] = service;
    }
    public TService GetService<TService>()
    {
        return (TService)_services[typeof(TService)];
    }
    public object GetService(Type serviceType)
    {
        return _services[serviceType];
    }
}