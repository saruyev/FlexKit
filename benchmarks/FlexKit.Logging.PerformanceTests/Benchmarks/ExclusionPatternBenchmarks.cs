using BenchmarkDotNet.Attributes;
using FlexKit.Logging.PerformanceTests.Services;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable NullableWarningSuppressionIsUsed

namespace FlexKit.Logging.PerformanceTests.Benchmarks;

[MemoryDiagnoser]
public class ExclusionPatternBenchmarks : FlexKitBenchmarkBase
{
    private IExactExclusionService _exactExclusionService = null!;
    private IPrefixExclusionService _prefixExclusionService = null!;
    private ISuffixExclusionService _suffixExclusionService = null!;
    private IMixedExclusionService _mixedExclusionService = null!;
    private INoExclusionService _noExclusionService = null!;

    [GlobalSetup]
    public override void Setup()
    {
        base.Setup();
        _exactExclusionService = FlexKitServices.GetService<IExactExclusionService>()!;
        _prefixExclusionService = FlexKitServices.GetService<IPrefixExclusionService>()!;
        _suffixExclusionService = FlexKitServices.GetService<ISuffixExclusionService>()!;
        _mixedExclusionService = FlexKitServices.GetService<IMixedExclusionService>()!;
        _noExclusionService = FlexKitServices.GetService<INoExclusionService>()!;
    }

    [Benchmark(Baseline = true)]
    public string No_Exclusion_Patterns()
    {
        return _noExclusionService.ProcessData("test");
    }

    [Benchmark]
    public string Exact_Method_Exclusion()
    {
        return _exactExclusionService.ProcessData("test"); // Should be logged
    }

    [Benchmark]
    public string Exact_Method_Excluded()
    {
        return _exactExclusionService.ToString(); // Should be excluded
    }

    [Benchmark]
    public string Prefix_Pattern_Excluded()
    {
        return _prefixExclusionService.GetUserName(123); // Should be excluded (Get*)
    }

    [Benchmark]
    public string Suffix_Pattern_Excluded()
    {
        return _suffixExclusionService.ProcessInternal("data"); // Should be excluded (*Internal)
    }

    [Benchmark]
    public string Mixed_Patterns_Complex()
    {
        return _mixedExclusionService.ProcessMainData("test"); // Should be logged
    }
}