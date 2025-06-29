using BenchmarkDotNet.Attributes;
using FlexKit.Configuration.Core;
using Microsoft.Extensions.Configuration;

namespace FlexKit.Configuration.PerformanceTests.Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
public class DynamicAccessBenchmarks
{
    private IConfiguration _standardConfig = null!;
    private FlexConfiguration _flexConfig = null!;

    [GlobalSetup]
    public void Setup()
    {
        var configData = new Dictionary<string, string?>
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

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(configData);
        _standardConfig = builder.Build();
        _flexConfig = new FlexConfiguration(_standardConfig);
    }

    [Benchmark(Baseline = true)]
    public string? StandardConfigurationAccess()
    {
        return _standardConfig["Database:ConnectionString"];
    }

    [Benchmark]
    public string? FlexConfigurationIndexerAccess()
    {
        return _flexConfig["Database:ConnectionString"];
    }

    [Benchmark]
    public string? FlexConfigurationDynamicAccess()
    {
        dynamic config = _flexConfig;
        var database = config.Database;
        var configuration = (IConfiguration)database!.Configuration;
        return configuration.CurrentConfig("ConnectionString")?.ToString();
    }

    [Benchmark]
    public string? FlexConfigurationChainedDynamicAccess()
    {
        dynamic config = _flexConfig;
        // Simulate more realistic dynamic access pattern
        var dbSection = config.Database;
        if (dbSection != null)
        {
            return dbSection.Configuration["ConnectionString"];
        }
        return null;
    }
}