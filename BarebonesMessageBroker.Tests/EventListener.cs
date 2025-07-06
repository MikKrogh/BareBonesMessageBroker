namespace BarebonesMessageBroker.Tests;

internal static class Constants
{
    internal const string EventName = "TestAssembly.SpecificEvent";
}

internal class EventListener : Listener<SpecificEventForListener>
{
    public SpecificEventForListener? RecievedEvent = null;
    internal readonly ListernerMonitor _someService;
    private readonly RandomService randomService;

    public EventListener(ListernerMonitor someService, RandomService randomService)
    {
        _someService = someService ?? throw new ArgumentNullException(nameof(someService));
        this.randomService = randomService;
    }
    public EventListener()
    {
        
    }
    public Task Handle(SpecificEventForListener t)
    {
        RecievedEvent = t;
        return Task.CompletedTask;
    }    
}


internal class SpecificEventForListener : Event
{
    public string Id {get;init; } = string.Empty;

    public string EventName => Constants.EventName;

    public DateTime Timestamp => DateTime.UtcNow.AddDays(5);
    public string StringValue { get; init; } = string.Empty;
    public int IntValue { get; set; } = 0;
    public long LongValue { get; init; } = 0;
    public double DoubleValue { get; init; } = 0.0;
    public bool BoolValue { get; init; } = false;
    public object? ObjectValue { get; init; } = null;
    public SpecificEventForListener()
    {
        
    }
}
