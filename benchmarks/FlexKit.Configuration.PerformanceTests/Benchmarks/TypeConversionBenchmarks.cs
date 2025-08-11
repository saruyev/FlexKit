using System.Globalization;
using BenchmarkDotNet.Attributes;
using FlexKit.Configuration.Core;
using FlexKit.Configuration.Conversion;
using Microsoft.Extensions.Configuration;
// ReSharper disable NullableWarningSuppressionIsUsed

namespace FlexKit.Configuration.PerformanceTests.Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
public class TypeConversionBenchmarks
{
    private FlexConfiguration _flexConfig = null!;
    private IConfiguration _standardConfig = null!;

    [GlobalSetup]
    public void Setup()
    {
        var configData = new Dictionary<string, string?>
        {
            ["Server:Port"] = "8080",
            ["Server:Timeout"] = "30.5",
            ["Server:IsSecure"] = "true",
            ["Database:MaxConnections"] = "100",
            ["Api:RetryCount"] = "5",
            ["Features:IsEnabled"] = "false"
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(configData);
        _standardConfig = builder.Build();
        _flexConfig = new FlexConfiguration(_standardConfig);
    }

    [Benchmark(Baseline = true)]
    public int StandardConfigurationIntParsing()
    {
        var value = _standardConfig["Server:Port"];
        return int.Parse(value ?? "0");
    }

    [Benchmark]
    public int FlexConfigurationToTypeInt()
    {
        var value = _flexConfig["Server:Port"];
        return value.ToType<int>();
    }

    [Benchmark]
    public bool StandardConfigurationBoolParsing()
    {
        var value = _standardConfig["Server:IsSecure"];
        return bool.Parse(value ?? "false");
    }

    [Benchmark]
    public bool FlexConfigurationToTypeBool()
    {
        var value = _flexConfig["Server:IsSecure"];
        return value.ToType<bool>();
    }

    [Benchmark]
    public double StandardConfigurationDoubleParsing()
    {
        var value = _standardConfig["Server:Timeout"];
        return double.Parse(value ?? "0.0", CultureInfo.InvariantCulture);
    }

    [Benchmark]
    public double FlexConfigurationToTypeDouble()
    {
        var value = _flexConfig["Server:Timeout"];
        return value.ToType<double>();
    }

    [Benchmark]
    public int DynamicAccessWithTypeConversion()
    {
        dynamic config = _flexConfig;
        var serverSection = config.Server;
        var portValue = serverSection?.Configuration["Port"];
        return ((string?)portValue!).ToType<int>();
    }
}