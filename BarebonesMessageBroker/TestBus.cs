


namespace BarebonesMessageBroker;

public class TestBus : IBus
{
    private readonly List<PublishedEvent> _publishedEvents = new();

    public Task Publish(object message, string eventType)
    {
        _publishedEvents.Add(new PublishedEvent
        {
            EventName = eventType,
            Message = message
        });

        return Task.CompletedTask;
    }

    public IReadOnlyList<PublishedEvent> PublishedEvents => _publishedEvents.AsReadOnly();

    public class PublishedEvent : Event
    {
        public object Message { get; set; }

        public string Id { get; init; }

        public string EventName { get; init; }
        public DateTime Timestamp => throw new NotImplementedException();
    }

    public void Clear() => _publishedEvents.Clear();
    public bool WasPublished<T>(string eventName, Func<T, bool>? predicate = null)
    {
        return _publishedEvents
            .Where(e => e.EventName == eventName && e.Message is T)
            .Cast<PublishedEvent>()
            .Any(e => predicate == null || predicate((T)e.Message));
    }
}
