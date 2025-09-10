using BenchmarkDotNet.Attributes;
using FlexKit.Logging.PerformanceTests.Services;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable NullableWarningSuppressionIsUsed

namespace FlexKit.Logging.PerformanceTests.Benchmarks;

[MemoryDiagnoser]
public class ComplexParameterBenchmarks : FlexKitBenchmarkBase
{
    private IExtendedLogBothService _extendedLogBothService = null!;
    private SimpleData _simpleData = null!;
    private ComplexTestData _complexData = null!;
    private VeryComplexData _veryComplexData = null!;

    [GlobalSetup]
    public override void Setup()
    {
        base.Setup();
        _extendedLogBothService = FlexKitServices.GetService<IExtendedLogBothService>()!;

        _simpleData = new SimpleData("test", 42);
        _complexData = new ComplexTestData("complex", 100)
        {
            Properties = new() { { "key1", "value1" }, { "key2", 123 } }
        };
        _veryComplexData = new VeryComplexData
        {
            Id = Guid.NewGuid(),
            Data = Enumerable.Range(1, 100).ToDictionary(i => $"key_{i}", i => (object)$"value_{i}"),
            NestedObjects = Enumerable.Range(1, 10).Select(i => new ComplexTestData($"nested_{i}", i)).ToList(),
            LargeArray = Enumerable.Range(1, 1000).ToArray()
        };
    }

    [Benchmark(Baseline = true)]
    public string Simple_Parameters()
    {
        return _extendedLogBothService.ProcessSimpleData(_simpleData);
    }

    [Benchmark]
    public string Complex_Parameters()
    {
        return _extendedLogBothService.ProcessComplexData(_complexData);
    }

    [Benchmark]
    public string Very_Complex_Parameters()
    {
        return _extendedLogBothService.ProcessVeryComplexData(_veryComplexData);
    }

    [Benchmark]
    public string Multiple_Parameters()
    {
        return _extendedLogBothService.ProcessMultipleParameters("string", 42, _simpleData, [1, 2, 3]);
    }
}