
namespace BarebonesMessageBroker
{
    public interface Event
    {
        public string Id { get; }
        public string EventName { get; }
        public DateTime Timestamp { get; }

    }
    public interface Listener<TEvent> where TEvent : Event
    {
        Task Handle(TEvent t);
    }
    public interface IBus
    {
        Task Publish(object t, string EventType);
        void Configure(Action<BusConfig> configure);
        public class BusConfig
        {
        }
    }


}




