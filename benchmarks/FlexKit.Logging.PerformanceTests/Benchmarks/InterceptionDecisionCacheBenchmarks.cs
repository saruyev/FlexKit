using System.Reflection;
using BenchmarkDotNet.Attributes;
using FlexKit.Logging.Interception;
using FlexKit.Logging.PerformanceTests.Services;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable NullableWarningSuppressionIsUsed

namespace FlexKit.Logging.PerformanceTests.Benchmarks;

[MemoryDiagnoser]
public class InterceptionDecisionCacheBenchmarks : FlexKitBenchmarkBase
{
    private InterceptionDecisionCache _cache = null!;
    private MethodInfo _cachedMethod = null!;
    private MethodInfo _uncachedMethod = null!;
    private MethodInfo[] _multipleMethods = null!;

    [GlobalSetup]
    public override void Setup()
    {
        base.Setup();
        _cache = FlexKitServices.GetService<InterceptionDecisionCache>()!;

        // Get methods for testing
        _cachedMethod = typeof(ILogBothService).GetMethod(nameof(ILogBothService.ProcessData))!;
        _uncachedMethod = typeof(string).GetMethod(nameof(string.ToString), Type.EmptyTypes)!;

        _multipleMethods = [.. typeof(ILogBothService).GetMethods().Take(10)];
    }

    [Benchmark(Baseline = true)]
    public InterceptionDecision? Cache_Hit_Lookup()
    {
        return _cache.GetInterceptionDecision(_cachedMethod);
    }

    [Benchmark]
    public InterceptionDecision? Cache_Miss_Lookup()
    {
        return _cache.GetInterceptionDecision(_uncachedMethod);
    }

    [Benchmark]
    public void Multiple_Cache_Lookups()
    {
        foreach (var method in _multipleMethods)
        {
            _cache.GetInterceptionDecision(method);
        }
    }

    [Benchmark]
    public void Concurrent_Cache_Access()
    {
        Parallel.For(0, 100, _ =>
        {
            _cache.GetInterceptionDecision(_cachedMethod);
        });
    }
}