using BenchmarkDotNet.Attributes;
using FlexKit.Logging.PerformanceTests.Services;
using Microsoft.Extensions.DependencyInjection;
// ReSharper disable NullableWarningSuppressionIsUsed

namespace FlexKit.Logging.PerformanceTests.Benchmarks;

[MemoryDiagnoser]
public class MethodFrequencyBenchmarks : FlexKitBenchmarkBase
{
    private INativeService _nativeService = null!;
    private INoLogService _noLogService = null!;
    private ILogInputService _logInputService = null!;
    private ILogBothService _logBothService = null!;

    [GlobalSetup]
    public override void Setup()
    {
        base.Setup();
        _nativeService = FlexKitServices.GetService<INativeService>()!;
        _noLogService = FlexKitServices.GetService<INoLogService>()!;
        _logInputService = FlexKitServices.GetService<ILogInputService>()!;
        _logBothService = FlexKitServices.GetService<ILogBothService>()!;
    }

    [Benchmark(Baseline = true)]
    [Arguments(1000)]
    [Arguments(10000)]
    public void Native_High_Frequency(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            _nativeService.ProcessData($"test_{i}");
        }
    }

    [Benchmark]
    [Arguments(1000)]
    [Arguments(10000)]
    public void NoLog_High_Frequency(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            _noLogService.ProcessData($"test_{i}");
        }
    }

    [Benchmark]
    [Arguments(1000)]
    [Arguments(10000)]
    public void LogInput_High_Frequency(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            _logInputService.ProcessData($"test_{i}");
        }
    }

    [Benchmark]
    [Arguments(1000)]
    [Arguments(10000)]
    public void LogBoth_High_Frequency(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            _logBothService.ProcessData($"test_{i}");
        }
    }
}