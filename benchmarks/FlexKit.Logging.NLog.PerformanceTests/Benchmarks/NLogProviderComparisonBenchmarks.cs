using BenchmarkDotNet.Attributes;
using FlexKit.Logging.PerformanceTests.Benchmarks;
using FlexKit.Logging.PerformanceTests.Services;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using NLog.Config;
using NLog.Targets;
// ReSharper disable NullableWarningSuppressionIsUsed

namespace FlexKit.Logging.NLog.PerformanceTests.Benchmarks;

[MemoryDiagnoser]
public class NLogProviderComparisonBenchmarks : FlexKitBenchmarkBase
{
    private ILogger _nativeNLogLogger = null!;
    private ILogBothService _flexKitNLogService = null!;

    [GlobalSetup]
    public override void Setup()
    {
        // Pure NLog setup
        var config = new LoggingConfiguration();
        var consoleTarget = new ConsoleTarget("console");
        config.AddRule(LogLevel.Debug, LogLevel.Fatal, consoleTarget);
        LogManager.Configuration = config;
        _nativeNLogLogger = LogManager.GetCurrentClassLogger();

        // FlexKit + NLog setup (NLogLoggingModule auto-detected)
        base.Setup();
        _flexKitNLogService = FlexKitServices.GetService<ILogBothService>()!;
    }

    [Benchmark(Baseline = true)]
    public void Native_NLog_Manual_Logging()
    {
        _nativeNLogLogger.Info("Processing {Input} with result {Result}", "test", "native result");
    }

    [Benchmark]
    public void FlexKit_NLog_Auto_Instrumentation()
    {
        _flexKitNLogService.ProcessData("test");
    }
}