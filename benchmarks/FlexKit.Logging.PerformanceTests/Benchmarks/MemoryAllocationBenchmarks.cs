using BenchmarkDotNet.Attributes;
using FlexKit.Logging.Core;
using FlexKit.Logging.Models;
using FlexKit.Logging.PerformanceTests.Services;
using Microsoft.Extensions.DependencyInjection;
// ReSharper disable NullableWarningSuppressionIsUsed

namespace FlexKit.Logging.PerformanceTests.Benchmarks;

[MemoryDiagnoser]
public class MemoryAllocationBenchmarks : FlexKitBenchmarkBase
{
    private ILogBothService _logBothService = null!;
    private IBackgroundLog _backgroundLog = null!;
    private ComplexTestData _testData = null!;

    [GlobalSetup]
    public override void Setup()
    {
        base.Setup();
        _logBothService = FlexKitServices.GetService<ILogBothService>()!;
        _backgroundLog = FlexKitServices.GetService<IBackgroundLog>()!;
        _testData = new ComplexTestData("memory test", 42)
        {
            Properties = new() { { "key1", "value1" }, { "key2", 123 } }
        };
    }

    [Benchmark(Baseline = true)]
    public LogEntry LogEntry_Creation()
    {
        return LogEntry.CreateStart("TestMethod", "TestClass")
            .WithInput("test data")
            .WithTarget("Console");
    }

    [Benchmark]
    public LogEntry LogEntry_With_Complex_Data()
    {
        return LogEntry.CreateStart("TestMethod", "TestClass")
            .WithInput(_testData)
            .WithOutput("result")
            .WithTarget("Console");
    }

    [Benchmark]
    public string Full_Method_Interception_Memory()
    {
        return _logBothService.ProcessComplexData(_testData);
    }

    [Benchmark]
    public bool Background_Queue_Memory()
    {
        var entry = LogEntry.CreateStart("TestMethod", "TestClass")
            .WithInput(_testData);
        return _backgroundLog.TryEnqueue(entry);
    }

    [Benchmark]
    public void Sustained_Allocation_Pattern()
    {
        // Simulate realistic usage pattern
        for (int i = 0; i < 100; i++)
        {
            var entry = LogEntry.CreateStart($"Method_{i}", "TestClass")
                .WithInput($"data_{i}")
                .WithTarget("Console");
            _backgroundLog.TryEnqueue(entry);
        }
    }
}