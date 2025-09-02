using BenchmarkDotNet.Attributes;
using FlexKit.Logging.PerformanceTests.Benchmarks;
using FlexKit.Logging.PerformanceTests.Services;
using Microsoft.Extensions.DependencyInjection;
// ReSharper disable NullableWarningSuppressionIsUsed

namespace FlexKit.Logging.Log4Net.PerformanceTests.Benchmarks;

[MemoryDiagnoser]
public class Log4NetProviderComparisonBenchmarks : FlexKitBenchmarkBase
{
    private log4net.ILog _nativeLog4NetLogger = null!;
    private ILogBothService _flexKitLog4NetService = null!;

    [GlobalSetup]
    public override void Setup()
    {
        // Pure Log4Net setup
        log4net.Config.BasicConfigurator.Configure();
        _nativeLog4NetLogger = log4net.LogManager.GetLogger(typeof(Log4NetProviderComparisonBenchmarks));

        // FlexKit + Log4Net setup (Log4NetLoggingModule auto-detected)
        base.Setup();
        _flexKitLog4NetService = FlexKitServices.GetService<ILogBothService>()!;
    }

    [Benchmark(Baseline = true)]
    public void Native_Log4Net_Manual_Logging()
    {
        _nativeLog4NetLogger.InfoFormat("Processing {0} with result {1}", "test", "native result");
    }

    [Benchmark]
    public void FlexKit_Log4Net_Auto_Instrumentation()
    {
        _flexKitLog4NetService.ProcessData("test");
    }
}