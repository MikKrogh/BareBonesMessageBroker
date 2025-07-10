using BarebonesMessageBroker;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

//| PublishSomeEvent | 4.029 us | 0.0802 us | 0.1777 us | 0.2747 |    2.3 KB | + cahed props on event type
//| PublishSomeEvent | 4.966 us | 0.0981 us | 0.1241 us | 0.3204 |   2.62 KB | + events for listener cached
//| PublishSomeEvent | 5.407 us | 0.1051 us | 0.1329 us | 0.3738 |   3.09 KB | cache of listener services
//| PublishSomeEvent | 6.082 us | 0.1195 us | 0.1713 us | 0.4730 |   3.87 KB |  no cache

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
            StringValue = "hello",
            IntValue = 123,
            LongValue = 456789L,
        };
        _bus = new BareBonesBus(new ScopeFactory());
    }

    [Benchmark]
    public async Task PublishSomeEvent()
    {
        await _bus.Publish(_message, "benchmarkAssembly.BenchmarkEvent");
        await _bus.Publish(_message, "benchmarkAssembly.BenchmarkEvent");
        await _bus.Publish(_message, "benchmarkAssembly.BenchmarkEvent");
        await _bus.Publish(_message, "benchmarkAssembly.BenchmarkEvent");
        await _bus.Publish(_message, "benchmarkAssembly.BenchmarkEvent");
    }
}
