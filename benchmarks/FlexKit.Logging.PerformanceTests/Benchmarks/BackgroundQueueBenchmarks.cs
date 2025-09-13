using BenchmarkDotNet.Attributes;
using FlexKit.Logging.Core;
using FlexKit.Logging.Models;
using Microsoft.Extensions.DependencyInjection;
// ReSharper disable NullableWarningSuppressionIsUsed

namespace FlexKit.Logging.PerformanceTests.Benchmarks;

public class BackgroundQueueBenchmarks : FlexKitBenchmarkBase
{
    private IBackgroundLog _backgroundLog = null!;
    private LogEntry _testEntry;

    [GlobalSetup]
    public override void Setup()
    {
        base.Setup();
        _backgroundLog = FlexKitServices.GetService<IBackgroundLog>()!;
        _testEntry = LogEntry.CreateStart("TestMethod", "TestClass")
            .WithInput("test data");
    }

    [Benchmark]
    [Arguments(1)]
    [Arguments(100)]
    [Arguments(1000)]
    public void Enqueue_Operations(int count)
    {
        for (int i = 0; i < count; i++)
        {
            _backgroundLog.TryEnqueue(_testEntry);
        }
    }

    [Benchmark]
    public bool Single_Enqueue_Latency()
    {
        return _backgroundLog.TryEnqueue(_testEntry);
    }
}