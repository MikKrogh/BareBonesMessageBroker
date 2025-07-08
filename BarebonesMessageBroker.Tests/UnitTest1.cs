namespace BarebonesMessageBroker.Tests;

public class BusTests
{
    [Fact]
    public async Task WhenOneListenerExists_WhenEventIsPublished_ThenListenerGetsCorrectlyMappedProperties()
    {
        ServiceCollection serviceCollection = new ServiceCollection();
        var bus = new BareBonesBus(serviceCollection);

        object sentEvent = new
        {
            Id = "id:" + Guid.NewGuid().ToString(),
            StringValue = "hello",
            IntValue = 123,
            LongValue = 456789L,
        };

        await bus.Publish(sentEvent, Constants.EventName);
        await bus.Publish(sentEvent, Constants.EventName);
    }
    // design a test setut, have unique listener and event for each test or somehow share one and clutter it?
    // test for dependency injection,
    // missing properties should/shouldnot throw?,
    // what should happen if something throws?,
    // monitoring setup with opentelemetry,

}


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