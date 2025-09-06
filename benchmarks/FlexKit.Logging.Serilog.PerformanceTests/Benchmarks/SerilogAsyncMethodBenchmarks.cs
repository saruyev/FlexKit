using BenchmarkDotNet.Attributes;
using FlexKit.Logging.PerformanceTests.Benchmarks;
using FlexKit.Logging.PerformanceTests.Services;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
// ReSharper disable NullableWarningSuppressionIsUsed

namespace FlexKit.Logging.Serilog.PerformanceTests.Benchmarks;

[MemoryDiagnoser]
public class SerilogProviderComparisonBenchmarks : FlexKitBenchmarkBase
{
    private ILogger _nativeSerilogLogger = null!;
    private ILogBothService _flexKitSerilogService = null!;

    [GlobalSetup]
    public override void Setup()
    {
        // Pure Serilog setup
        _nativeSerilogLogger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();

        // FlexKit + Serilog setup (SerilogLoggingModule auto-detected)
        base.Setup(); // This creates FlexKitHost with Serilog provider
        _flexKitSerilogService = FlexKitServices.GetService<ILogBothService>()!;
    }

    [Benchmark(Baseline = true)]
    public void Native_Serilog_Manual_Logging()
    {
        _nativeSerilogLogger.Information("Processing {Input} with result {Result}", "test", "native result");
    }

    [Benchmark]
    public void FlexKit_Serilog_Auto_Instrumentation()
    {
        _flexKitSerilogService.ProcessData("test");
    }
}