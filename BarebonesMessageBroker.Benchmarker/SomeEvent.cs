using BarebonesMessageBroker;

public class SomeEvent : Event
{
    public string Id { get; init; } = string.Empty;
    public string EventName => "benchmarkAssembly.SomeEvent";
    public DateTime Timestamp => throw new NotImplementedException();
    public int IntValue { get; init; } = 0;
    public long LongValue { get; init; } = 0;
}
