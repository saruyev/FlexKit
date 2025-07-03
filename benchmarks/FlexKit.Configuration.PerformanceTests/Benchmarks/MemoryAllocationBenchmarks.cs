using BenchmarkDotNet.Attributes;
using FlexKit.Configuration.Core;
using FlexKit.Configuration.Conversion;
using Microsoft.Extensions.Configuration;
// ReSharper disable ComplexConditionExpression

namespace FlexKit.Configuration.PerformanceTests.Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
public class MemoryAllocationBenchmarks
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
            ["Server:Port"] = "8080",
            ["Server:IsSecure"] = "true",
            ["Api:Key"] = "abc123def456",
            ["Features:NewFeature"] = "false"
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(configData);
        _standardConfig = builder.Build();
        _flexConfig = new FlexConfiguration(_standardConfig);
    }

    [Benchmark(Baseline = true)]
    public object StandardConfigurationAllocations()
    {
        // Simulate typical configuration access pattern
        var results = new List<object>
        {
            _standardConfig["Database:ConnectionString"] ?? "",
            int.Parse(_standardConfig["Database:Timeout"] ?? "0"),
            int.Parse(_standardConfig["Server:Port"] ?? "0"),
            bool.Parse(_standardConfig["Server:IsSecure"] ?? "false"),
            _standardConfig["Api:Key"] ?? "",
            bool.Parse(_standardConfig["Features:NewFeature"] ?? "false"),
        };

        return results;
    }

    [Benchmark]
    public object FlexConfigurationIndexerAllocations()
    {
        var results = new List<object>
        {
            _flexConfig["Database:ConnectionString"] ?? "",
            _flexConfig["Database:Timeout"].ToType<int>(),
            _flexConfig["Server:Port"].ToType<int>(),
            _flexConfig["Server:IsSecure"].ToType<bool>(),
            _flexConfig["Api:Key"] ?? "",
            _flexConfig["Features:NewFeature"].ToType<bool>(),
        };

        return results;
    }

    [Benchmark]
    public object FlexConfigurationDynamicAllocations()
    {
        var results = new List<object>();
        dynamic config = _flexConfig;
        
        var database = config.Database;
        var server = config.Server;
        var api = config.Api;
        var features = config.Features;
        
        results.Add(database?.Configuration["ConnectionString"] ?? "");
        results.Add(((string?)database?.Configuration["Timeout"])?.ToType<int>() ?? 0);
        results.Add(((string?)server?.Configuration["Port"])?.ToType<int>() ?? 0);
        results.Add(((string?)server?.Configuration["IsSecure"])?.ToType<bool>() ?? false);
        results.Add(((string?)features?.Configuration["NewFeature"])?.ToType<bool>() ?? false);
        results.Add(api?.Configuration["Key"] ?? "");
        
        return results;
    }

    [Benchmark]
    public object RepeatedDynamicAccess()
    {
        // Test allocation overhead of repeated dynamic access to the same configuration
        var results = new List<object>();
        
        for (int i = 0; i < 10; i++)
        {
            dynamic config = _flexConfig;
            var database = config.Database;
            results.Add(database?.Configuration["ConnectionString"] ?? "");
        }
        
        return results;
    }

    [Benchmark]
    public object CachedSectionAccess()
    {
        // Test pattern where sections are cached vs. repeatedly accessed
        var results = new List<object>();
        var databaseSection = _flexConfig.Configuration.CurrentConfig("Database");
        var serverSection = _flexConfig.Configuration.CurrentConfig("Server");
        
        for (int i = 0; i < 10; i++)
        {
            results.Add(databaseSection?.Configuration["ConnectionString"] ?? "");
            results.Add(serverSection?.Configuration["Port"]?.ToType<int>() ?? 0);
        }
        
        return results;
    }
}