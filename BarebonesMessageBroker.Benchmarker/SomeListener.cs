using BarebonesMessageBroker;

public class SomeListener : Listener<SomeEvent>
{
    public SomeListener()
    {
        
    }
    public Task Handle(SomeEvent t)
    {
        return Task.CompletedTask;
    }
}
