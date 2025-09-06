using BenchmarkDotNet.Attributes;
using FlexKit.Logging.PerformanceTests.Services;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable NullableWarningSuppressionIsUsed

namespace FlexKit.Logging.PerformanceTests.Benchmarks;

[MemoryDiagnoser]
public class ConfigurationOverheadBenchmarks : FlexKitBenchmarkBase
{
    private IExactMatchService _exactMatchService = null!;
    private IWildcardMatchService _wildcardMatchService = null!;
    private IAttributeOverrideService _attributeOverrideService = null!;
    private INoConfigService _noConfigService = null!;

    [GlobalSetup]
    public override void Setup()
    {
        base.Setup();
        _exactMatchService = FlexKitServices.GetService<IExactMatchService>()!;
        _wildcardMatchService = FlexKitServices.GetService<IWildcardMatchService>()!;
        _attributeOverrideService = FlexKitServices.GetService<IAttributeOverrideService>()!;
        _noConfigService = FlexKitServices.GetService<INoConfigService>()!;
    }

    [Benchmark(Baseline = true)]
    public string No_Configuration_Auto()
    {
        return _noConfigService.ProcessData("test");
    }

    [Benchmark]
    public string Exact_Match_Configuration()
    {
        return _exactMatchService.ProcessData("test");
    }

    [Benchmark]
    public string Wildcard_Pattern_Match()
    {
        return _wildcardMatchService.ProcessData("test");
    }

    [Benchmark]
    public string Attribute_Overrides_Config()
    {
        return _attributeOverrideService.ProcessData("test");
    }
}