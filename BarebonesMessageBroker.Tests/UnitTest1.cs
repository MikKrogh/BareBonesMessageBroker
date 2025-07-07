namespace BarebonesMessageBroker.Tests;

public class BusTests
{
    [Fact]
    public async Task WhenOneListenerExists_WhenEventIsPublished_ThenListenerReacts()
    {
        ServiceCollection serviceCollection = new ServiceCollection();
        var monitor = new ListernerMonitor();
        var randomService = new RandomService();
        serviceCollection.AddService(monitor);
        serviceCollection.AddService(randomService);
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
}


internal class ListernerMonitor
{
    public List<object> RecievedEvents { get; set; } = new List<object>();
}
internal class RandomService
{

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