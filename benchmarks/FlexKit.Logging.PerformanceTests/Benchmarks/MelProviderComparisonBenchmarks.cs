using BenchmarkDotNet.Attributes;
using FlexKit.Configuration.Core;
using FlexKit.Logging.PerformanceTests.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
// ReSharper disable NullableWarningSuppressionIsUsed

namespace FlexKit.Logging.PerformanceTests.Benchmarks;

public class MelProviderComparisonBenchmarks
{
    private IServiceProvider _nativeMelServices = null!;
    private IServiceProvider _flexKitMelServices = null!;
    private ILogger<MelProviderComparisonBenchmarks> _nativeMelLogger = null!;
    private ILogBothService _flexKitService = null!;
    private ComplexTestData _complexTestData = null!;
    private IAsyncLogBothService _flexKitAsyncService = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Pure MEL setup
        var nativeHost = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddLogging(builder => builder.AddConsole());
            })
            .Build();
        _nativeMelServices = nativeHost.Services;
        _nativeMelLogger = _nativeMelServices.GetService<ILogger<MelProviderComparisonBenchmarks>>()!;


        // FlexKit + MEL setup (no external providers, falls back to MEL)
        var flexKitHost = Host.CreateDefaultBuilder()
            .AddFlexConfig()
            .Build();
        _flexKitMelServices = flexKitHost.Services;
        _flexKitService = _flexKitMelServices.GetService<ILogBothService>()!;
        _flexKitAsyncService = _flexKitMelServices.GetService<IAsyncLogBothService>()!;
        _complexTestData = new ComplexTestData("test complex", 42)
        {
            Properties = new() { { "key1", "value1" }, { "key2", 123 } }
        };
    }

    [Benchmark(Baseline = true)]
    public void Native_MEL_Manual_Logging()
    {
        _nativeMelLogger.LogInformation("Processing {Input} with result {Result}", "test", "native result");
    }

    [Benchmark]
    public void FlexKit_MEL_Auto_Instrumentation()
    {
        _flexKitService.ProcessData("test");
    }

    [Benchmark]
    public void Native_MEL_Complex_Object()
    {
        _nativeMelLogger.LogInformation("Processing {@ComplexData}", _complexTestData);
    }

    [Benchmark]
    public async Task Native_MEL_Async_Pattern()
    {
        using var scope = _nativeMelLogger.BeginScope("AsyncOperation");
        _nativeMelLogger.LogInformation("Starting async operation");
        await Task.Delay(1);
        _nativeMelLogger.LogInformation("Completed async operation");
    }

    [Benchmark]
    public void Native_MEL_Exception_Scenario()
    {
        try
        {
            throw new InvalidOperationException("Test exception");
        }
        catch (Exception ex)
        {
            _nativeMelLogger.LogError(ex, "Operation failed with {Input}", "test");
        }
    }

    // FlexKit comparisons
    [Benchmark]
    public string FlexKit_MEL_Auto_Simple()
    {
        return _flexKitService.ProcessData("test");
    }

    [Benchmark]
    public string FlexKit_MEL_Auto_Complex()
    {
        return _flexKitService.ProcessComplexData(_complexTestData);
    }

    [Benchmark]
    public async Task<string> FlexKit_MEL_Auto_Async()
    {
        return await _flexKitAsyncService.ProcessDataAsync("test");
    }
}