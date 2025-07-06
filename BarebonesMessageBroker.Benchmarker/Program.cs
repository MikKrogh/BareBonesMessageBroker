using BarebonesMessageBroker;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

BenchmarkRunner.Run<BusPublishBenchmark>();

[MemoryDiagnoser]
public class BusPublishBenchmark
{
    private BareBonesBus _bus;
    private object _message;
    [GlobalSetup]
    public void Setup()
    {
         _message = new
        {
            Id = "id:" + Guid.NewGuid().ToString(),
            StringValues = "hello",
            IntValues = 123,
            LongValues = 456789L,
        };
        _bus = new BareBonesBus(new ServiceCollection());
    }

    [Benchmark]
    public async Task PublishSomeEvent()
    {

        await _bus.Publish(_message, "benchmarkAssembly.SomeEvent");
    }
}
