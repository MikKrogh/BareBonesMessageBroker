
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
    private Dictionary<Type, Type> _cachedEventsForListeners = new Dictionary<Type, Type>();
    private Dictionary<Type, Type[]> _cachedListenerDependencies = new Dictionary<Type, Type[]>();
    private Dictionary<Type, PropertyInfo[]> _cachedEventProperties = new Dictionary<Type, PropertyInfo[]>();
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
    //| PublishSomeEvent | 4.029 us | 0.0802 us | 0.1777 us | 0.2747 |    2.3 KB | + cahed props on event type
    //| PublishSomeEvent | 4.966 us | 0.0981 us | 0.1241 us | 0.3204 |   2.62 KB | + events for listener cached
    //| PublishSomeEvent | 5.407 us | 0.1051 us | 0.1329 us | 0.3738 |   3.09 KB | cache of listener services
    //| PublishSomeEvent | 6.082 us | 0.1195 us | 0.1713 us | 0.4730 |   3.87 KB |  no cache

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

    private  Type GetEventType(Type listenerType)
    {
        if (!_cachedEventsForListeners.ContainsKey(listenerType))
        {
            var listenerInterface = listenerType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(Listener<>));
            if (listenerInterface == null) throw new NoNullAllowedException($"Type {listenerType.FullName} does not implement Listener<TEvent>.");

            Type eventType = listenerInterface.GetGenericArguments()[0];
            _cachedEventsForListeners.Add(listenerType, eventType);
        }

        return _cachedEventsForListeners[listenerType];
    }

    private object? CreateInstance(Type listenerType)
    {
        if (!_cachedListenerDependencies.ContainsKey(listenerType))
        {
            var ctor = listenerType.GetConstructors().OrderByDescending(c => c.GetParameters().Length).First();
            var dependencies = ctor.GetParameters().Select(p => p.ParameterType);
            _cachedListenerDependencies.Add(listenerType, dependencies.ToArray());
        }
        var services = new List<object>();
        foreach (var dependency in _cachedListenerDependencies[listenerType])
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
        if (!_cachedEventProperties.ContainsKey(returnedInstance))
        {
            var instanceProps = returnedInstance.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            instanceProps = instanceProps.Where(p => p.CanWrite).ToArray();
            _cachedEventProperties.Add(returnedInstance, instanceProps);
        }

        var eventInstance = Activator.CreateInstance(returnedInstance);
        if (eventInstance is null)
            throw new InvalidOperationException($"Could not create instance of event type {returnedInstance.FullName}.");

        foreach (var property in _cachedEventProperties[returnedInstance])
        {
            var value = message.GetType().GetProperty(property.Name)?.GetValue(message);
            property.SetValue(eventInstance, value);            
        }

        return (Tevent?)eventInstance;
    }
}
