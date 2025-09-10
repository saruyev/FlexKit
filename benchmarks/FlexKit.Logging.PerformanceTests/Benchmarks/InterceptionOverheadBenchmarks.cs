using BenchmarkDotNet.Attributes;
using FlexKit.Logging.PerformanceTests.Services;
using Microsoft.Extensions.DependencyInjection;
// ReSharper disable NullableWarningSuppressionIsUsed

namespace FlexKit.Logging.PerformanceTests.Benchmarks;

[MemoryDiagnoser]
public class InterceptionOverheadBenchmarks : FlexKitBenchmarkBase
{
    private INativeService _nativeService = null!;
    private IManualService _manualService = null!;
    private INoLogService _noLogService = null!;
    private ILogInputService _logInputService = null!;
    private ILogBothService _logBothService = null!;
    private IAutoService _autoService = null!;

    [GlobalSetup]
    public override void Setup()
    {
        base.Setup();
        _nativeService = FlexKitServices.GetService<INativeService>()!;
        _manualService = FlexKitServices.GetService<IManualService>()!;
        _noLogService = FlexKitServices.GetService<INoLogService>()!;
        _logInputService = FlexKitServices.GetService<ILogInputService>()!;
        _logBothService = FlexKitServices.GetService<ILogBothService>()!;
        _autoService = FlexKitServices.GetService<IAutoService>()!;
    }

    [Benchmark(Baseline = true)]
    public string Native_NoFramework()
    {
        return _nativeService.ProcessData("test");
    }

    [Benchmark]
    public string Manual_IFlexKitLogger()
    {
        return _manualService.ProcessData("test");
    }

    [Benchmark]
    public string NoLog_Attribute()
    {
        return _noLogService.ProcessData("test");
    }

    [Benchmark]
    public string LogInput_Attribute()
    {
        return _logInputService.ProcessData("test");
    }

    [Benchmark]
    public string LogBoth_Attribute()
    {
        return _logBothService.ProcessData("test");
    }

    [Benchmark]
    public string Auto_Detection()
    {
        return _autoService.ProcessData("test");
    }
}