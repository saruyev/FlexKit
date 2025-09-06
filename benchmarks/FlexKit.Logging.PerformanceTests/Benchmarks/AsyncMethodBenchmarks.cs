using BenchmarkDotNet.Attributes;
using FlexKit.Logging.PerformanceTests.Services;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable NullableWarningSuppressionIsUsed

namespace FlexKit.Logging.PerformanceTests.Benchmarks;

// Additional benchmark classes for FlexKit.Logging.PerformanceTests.Core
// These complement the existing InterceptionOverheadBenchmarks, FormatterBenchmarks, and BackgroundQueueBenchmarks

// 4. AsyncMethodBenchmarks
[MemoryDiagnoser]
public class AsyncMethodBenchmarks : FlexKitBenchmarkBase
{
    private INativeService _nativeService = null!;
    private IAsyncLogBothService _flexKitAsyncService = null!;
    private IAsyncManualService _manualAsyncService = null!;

    [GlobalSetup]
    public override void Setup()
    {
        base.Setup();
        _nativeService = FlexKitServices.GetService<INativeService>()!;
        _flexKitAsyncService = FlexKitServices.GetService<IAsyncLogBothService>()!;
        _manualAsyncService = FlexKitServices.GetService<IAsyncManualService>()!;
    }

    [Benchmark(Baseline = true)]
    public async Task<string> Native_Async_Method()
    {
        _nativeService.ProcessData("test");
        await Task.Delay(1); // Simulate async work
        return "native async result";
    }

    [Benchmark]
    public async Task<string> FlexKit_Async_LogBoth()
    {
        return await _flexKitAsyncService.ProcessDataAsync("test");
    }

    [Benchmark]
    public async Task<string> FlexKit_Manual_Async()
    {
        return await _manualAsyncService.ProcessDataAsync("test");
    }

    [Benchmark]
    public async Task FlexKit_Async_Void()
    {
        await _flexKitAsyncService.ProcessVoidAsync("test");
    }
}
