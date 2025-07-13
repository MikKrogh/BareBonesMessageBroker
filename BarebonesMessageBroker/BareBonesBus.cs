
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Frozen;
using System.Data;
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("BarebonesMessageBroker.Tests")]
namespace BarebonesMessageBroker;

public class BareBonesBus : IBus
{
    private FrozenDictionary<string, Type[]> eventListerners = new Dictionary<string, Type[]>().ToFrozenDictionary();
    private Dictionary<Type, Type> _cachedEventsForListeners = new Dictionary<Type, Type>();
    private Dictionary<Type, Type[]> _cachedListenerDependencies = new Dictionary<Type, Type[]>();
    private Dictionary<Type, PropertyInfo[]> _cachedEventProperties = new Dictionary<Type, PropertyInfo[]>();
    
    private readonly IServiceScopeFactory _scopeFactory;

    public BareBonesBus(IServiceScopeFactory scopeFactory)
    {
        var setup = new BareBonesBusSetup();
        var listeners = setup.ScanForListeners();
        if (listeners?.Any() == true)
        {
            eventListerners = listeners;
        }
        this._scopeFactory = scopeFactory;
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
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    {
                        object? typeInstance = CreateInstance(listenerType, scope);
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
                catch (Exception e)
                {
                    throw;
                }
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

    private object? CreateInstance(Type listenerType, IServiceScope scope)
    {
        try
        {
            if (!_cachedListenerDependencies.ContainsKey(listenerType))
            {
                var ctor = listenerType.GetConstructors().OrderByDescending(c => c.GetParameters().Length).First();
                var dependencies = ctor.GetParameters().Select(p => p.ParameterType);
                _cachedListenerDependencies.Add(listenerType, dependencies.ToArray());
            }
            var scopedProvider = scope.ServiceProvider;
            var services = new List<object>();
            foreach (var dependency in _cachedListenerDependencies[listenerType])
            {
                
                        var service = scopedProvider.GetService(dependency);
                        if(service is null)
                        {
                            throw new NoNullAllowedException($"Could not resolve service of type {dependency.FullName} for listener {listenerType.FullName}.");
                        }
                        services.Add(service);                
                
            }
            var TypeInstance = Activator.CreateInstance(listenerType, services.ToArray());

            return TypeInstance;
        }
        catch (Exception e)
        {
            throw;
        }
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
