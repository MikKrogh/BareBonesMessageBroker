
using System.Collections.Frozen;
using System.Data;
using System.Reflection;
using System.Runtime.CompilerServices;
using static BarebonesMessageBroker.IBus;

[assembly: InternalsVisibleTo("BarebonesMessageBroker.Tests")]
namespace BarebonesMessageBroker;

public class BareBonesBus : IBus
{
    private FrozenDictionary<string, Type[]> eventListerners = new Dictionary<string, Type[]>().ToFrozenDictionary();
    private readonly IServiceProvider _services;
    public BareBonesBus(IServiceProvider services)
    {
        var setup = new BareBonesBusSetup();
        var listeners = setup.ScanForListeners();
        if (listeners?.Any() == true)
        {
            eventListerners = listeners;
        }
        _services = services;
    }
    public void Configure(Action<BusConfig> configure)
    {
        throw new NotImplementedException();
    }

    //| PublishSomeEvent | 1.477 us | 0.0327 us | 0.0939 us | 1.449 us | 0.1221 |   1.02 KB |
    //| PublishSomeEvent | 1.286 us | 0.0257 us | 0.0414 us | 0.0916 |     792 B |  No catching or perf implementations
    public async Task Publish(object message, string EventType)
    {
        if (eventListerners.ContainsKey(EventType))
        {
            foreach (var listenerType in eventListerners[EventType])
            {
                object? typeInstance = CreateInstance(listenerType);
                if (typeInstance is null) throw new NoNullAllowedException($"Could not create instance of listener type {listenerType.FullName}.");

                Type eventType = GetEventType(listenerType);
                var listenerEvent = DeserializeMessage<Event>(message, eventType);
                if (listenerEvent is null) 
                    throw new NoNullAllowedException($"Could not deserialize message to event type {eventType.FullName}.");

                Type listenerInterface = typeof(Listener<>).MakeGenericType(eventType);
                var handleMethod = listenerType.GetMethod("Handle");
                Task task = (Task?)handleMethod?.Invoke(typeInstance,  new[] { listenerEvent } );
                await task;

            }
        }
    }

    private static Type GetEventType(Type listenerType)
    {
        var listenerInterface = listenerType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType &&
            i.GetGenericTypeDefinition() == typeof(Listener<>));
        if (listenerInterface == null) throw new NoNullAllowedException($"Type {listenerType.FullName} does not implement Listener<TEvent>.");

        Type eventType = listenerInterface.GetGenericArguments()[0];
        return eventType;
    }

    private object? CreateInstance(Type listenerType)
    {
        var ctor = listenerType.GetConstructors().OrderByDescending(c => c.GetParameters().Length).First();
        var listernerDependencies = ctor.GetParameters().Select(p => p.ParameterType);

        var services = new List<object>();
        foreach (var dependency in listernerDependencies)
        {
            var service = _services.GetService(dependency);
            if (service is null)
                throw new InvalidOperationException($"Service of type {dependency.FullName} not registered.");
            services.Add(service);
        }

        var TypeInstance = Activator.CreateInstance(listenerType, services.ToArray());
        return TypeInstance;
    }

    private Tevent? DeserializeMessage<Tevent>(object message, Type returnedInstance) where Tevent : Event
    {
        if (message == null) 
            return default;
        var messageProps = message.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var instanceProps = returnedInstance.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var eventInstance = Activator.CreateInstance(returnedInstance);
        if (eventInstance is null)
            throw new InvalidOperationException($"Could not create instance of event type {returnedInstance.FullName}.");
        try
        {
            foreach (var property in instanceProps)
            {
                if (property.CanWrite)
                {
                    var value = message.GetType().GetProperty(property.Name)?.GetValue(message);
                    property.SetValue(eventInstance, value);
                }
            }
        }
        catch (Exception e)
        {
        }
        return (Tevent?)eventInstance;
    }
}
