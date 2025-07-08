using BarebonesMessageBroker;

public class BenchmarkListener : Listener<BenchmarkEvent>
{
    public BenchmarkListener()
    {
        
    }
    public Task Handle(BenchmarkEvent t)
    {
        return Task.CompletedTask;
    }
}
