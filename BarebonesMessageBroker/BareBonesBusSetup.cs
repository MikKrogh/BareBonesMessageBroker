

using System.Collections.Frozen;

namespace BarebonesMessageBroker;

internal class BareBonesBusSetup
{

    public FrozenDictionary<string, Type[]>? ScanForListeners()
    {
        var assemblies = GetAssemblies();
        List<Type> listeners = GetEventListeners(assemblies);

        Dictionary<string, List<Type>> subscriptionsToMap = new();
        foreach (var type in listeners)
        {
            var listenerInterface = type.GetInterfaces().FirstOrDefault(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(Listener<>)
            );

            var eventType = listenerInterface?.GetGenericArguments().FirstOrDefault();
            var eventNameProp = eventType?.GetProperty("EventName");
            if (eventNameProp?.PropertyType == typeof(string) && eventType is not null)
            {
                var eventInstance = Activator.CreateInstance(eventType);
                var eventName = eventNameProp.GetValue(eventInstance) as string;
                if (!string.IsNullOrEmpty(eventName))
                {
                    if (!subscriptionsToMap.ContainsKey(eventName))
                    {
                        subscriptionsToMap[eventName] = new List<Type>();
                    }
                    subscriptionsToMap[eventName].Add(type);
                }
            }
        }
        var result = subscriptionsToMap
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray())
            .ToFrozenDictionary();
        return result;

    }

    private static List<Type> GetEventListeners(List<System.Reflection.Assembly> assemblies)
    {

        Type listenerInterfaceType = typeof(Listener<>);
        return assemblies.SelectMany(x => x.GetTypes())
            .Where(x =>
                x.IsClass &&
                !x.IsAbstract &&
                x.GetInterfaces().Any(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == listenerInterfaceType))
            .ToList();
    }

    private static List<System.Reflection.Assembly> GetAssemblies()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var trimmedAssemblies = assemblies.Where(x =>
            !x.FullName.StartsWith("microsoft", StringComparison.InvariantCultureIgnoreCase) &&
            !x.FullName.StartsWith("xunit", StringComparison.InvariantCultureIgnoreCase) &&
            !x.FullName.StartsWith("System")
        ).ToList();
        return trimmedAssemblies;
    }
}
