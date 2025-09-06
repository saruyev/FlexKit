using BenchmarkDotNet.Attributes;
using FlexKit.Logging.PerformanceTests.Benchmarks;
using FlexKit.Logging.PerformanceTests.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZLogger;

// ReSharper disable NullableWarningSuppressionIsUsed

namespace FlexKit.Logging.ZLogger.PerformanceTests.Benchmarks;

[MemoryDiagnoser]
[MaxIterationCount(16)]
[InvocationCount(1_000)]
public class ZLoggerProviderComparisonBenchmarks : FlexKitBenchmarkBase
{
    private ILogger<ZLoggerProviderComparisonBenchmarks> _nativeZLogger = null!;
    private ILogBothService _flexKitZLoggerService = null!;
    private IHost _nativeZLoggerHost = null!;

    [GlobalSetup]
    public override void Setup()
    {
        // Pure ZLogger setup
        _nativeZLoggerHost = Host.CreateDefaultBuilder()
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddZLoggerConsole();
            })
            .Build();
        _nativeZLogger = _nativeZLoggerHost.Services.GetService<ILogger<ZLoggerProviderComparisonBenchmarks>>()!;

        // FlexKit + ZLogger setup (ZLoggerLoggingModule auto-detected if exists)
        base.Setup();
        _flexKitZLoggerService = FlexKitServices.GetService<ILogBothService>()!;
    }

    [Benchmark(Baseline = true)]
    public void Native_ZLogger_Manual_Logging()
    {
        var input = new { Name = "input", Type = "String", Value = "test" };
        var result = "native result";
        _nativeZLogger.ZLogInformation($"Processing {input:json} with result {result}");
    }

    [Benchmark]
    public void FlexKit_ZLogger_Auto_Instrumentation()
    {
        _flexKitZLoggerService.ProcessData("test");
    }
}