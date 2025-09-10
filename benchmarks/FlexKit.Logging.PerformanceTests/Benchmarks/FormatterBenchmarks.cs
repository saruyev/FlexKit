using BenchmarkDotNet.Attributes;
using FlexKit.Logging.PerformanceTests.Services;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable NullableWarningSuppressionIsUsed

namespace FlexKit.Logging.PerformanceTests.Benchmarks;

[MemoryDiagnoser]
public class FormatterBenchmarks : FlexKitBenchmarkBase
{
    private IFormatterAttributeService _attributeService = null!;
    private IFormatterManualService _manualService = null!;
    private ComplexTestData _testData = null!;

    [UsedImplicitly]
    [ParamsSource(nameof(FormatterTypes))] public string FormatterType { get; set; } = null!;

    public static IEnumerable<string> FormatterTypes =>
    [
        "Json",
        "Hybrid",
        "CustomTemplate",
        "StandardStructured",
        "SuccessError"
    ];

    [GlobalSetup]
    public override void Setup()
    {
        base.Setup();
        _attributeService = FlexKitServices.GetService<IFormatterAttributeService>()!;
        _manualService = FlexKitServices.GetService<IFormatterManualService>()!;

        _testData = new ComplexTestData("formatter test", 42)
        {
            Properties = new() { { "key1", "value1" }, { "key2", 123 } }
        };
    }

    [Benchmark(Baseline = true)]
    public string Attribute_Based_Formatter()
    {
        return _attributeService.ProcessWithFormatter(FormatterType, _testData);
    }

    [Benchmark]
    public string Manual_LogEntry_Formatter()
    {
        return _manualService.ProcessWithFormatter(FormatterType, _testData);
    }
}