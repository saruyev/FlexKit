using BenchmarkDotNet.Attributes;
using FlexKit.Configuration.Core;
using Microsoft.Extensions.Configuration;
// ReSharper disable NullableWarningSuppressionIsUsed

namespace FlexKit.Configuration.PerformanceTests.Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
public class ConfigurationBuildingBenchmarks
{
    private Dictionary<string, string?> _configData = null!;

    [GlobalSetup]
    public void Setup()
    {
        _configData = new Dictionary<string, string?>
        {
            ["Database:ConnectionString"] = "Server=localhost;Database=TestDb;User=test;Password=test123",
            ["Database:Timeout"] = "30",
            ["Database:Retries"] = "3",
            ["Api:BaseUrl"] = "https://api.example.com",
            ["Api:Key"] = "abc123def456",
            ["Api:Timeout"] = "60",
            ["Server:Host"] = "localhost",
            ["Server:Port"] = "8080",
            ["Server:IsSecure"] = "true",
            ["Logging:Level"] = "Information",
            ["Logging:Console"] = "true",
            ["Features:NewFeature"] = "false",
            ["Features:BetaFeature"] = "true"
        };
    }

    [Benchmark(Baseline = true)]
    public IConfiguration BuildStandardConfiguration()
    {
        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(_configData);
        return builder.Build();
    }

    [Benchmark]
    public FlexConfiguration BuildFlexConfiguration()
    {
        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(_configData);
        var standardConfig = builder.Build();
        return new FlexConfiguration(standardConfig);
    }

    [Benchmark]
    public FlexConfiguration BuildFlexConfigurationWithExtension()
    {
        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(_configData);
        var standardConfig = builder.Build();
        return standardConfig.GetFlexConfiguration();
    }
}