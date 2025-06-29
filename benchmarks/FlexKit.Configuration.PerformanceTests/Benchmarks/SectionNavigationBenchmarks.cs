using BenchmarkDotNet.Attributes;
using FlexKit.Configuration.Core;
using Microsoft.Extensions.Configuration;

namespace FlexKit.Configuration.PerformanceTests.Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
public class SectionNavigationBenchmarks
{
    private IConfiguration _standardConfig = null!;
    private FlexConfiguration _flexConfig = null!;

    [GlobalSetup]
    public void Setup()
    {
        var configData = new Dictionary<string, string?>
        {
            ["Level1:Level2:Level3:Property"] = "DeepValue",
            ["Level1:Level2:OtherProperty"] = "MidValue",
            ["Level1:DirectProperty"] = "TopValue",
            ["Database:ConnectionStrings:Primary"] = "primary-connection",
            ["Database:ConnectionStrings:Secondary"] = "secondary-connection",
            ["Database:Settings:Timeout"] = "30",
            ["Database:Settings:Retries"] = "3",
            ["Api:Endpoints:Users"] = "/api/users",
            ["Api:Endpoints:Orders"] = "/api/orders",
            ["Api:Authentication:Key"] = "secret-key",
            ["Api:Authentication:Timeout"] = "3600"
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(configData);
        _standardConfig = builder.Build();
        _flexConfig = new FlexConfiguration(_standardConfig);
    }

    [Benchmark(Baseline = true)]
    public string? StandardConfigurationDeepAccess()
    {
        return _standardConfig["Level1:Level2:Level3:Property"];
    }

    [Benchmark]
    public string? FlexConfigurationIndexerDeepAccess()
    {
        return _flexConfig["Level1:Level2:Level3:Property"];
    }

    [Benchmark]
    public string? FlexConfigurationDynamicDeepAccess()
    {
        dynamic config = _flexConfig;
        var level1 = config.Level1;
        var level2 = ((IConfiguration)level1!.Configuration).CurrentConfig("Level2");
        var level3 = level2!.Configuration.CurrentConfig("Level3");
        return level3?.Configuration["Property"];
    }

    [Benchmark]
    public IFlexConfig? StandardSectionNavigation()
    {
        var section = _standardConfig.GetSection("Database");
        return section.Exists() ? section.GetFlexConfiguration() : null;
    }

    [Benchmark]
    public IFlexConfig? FlexConfigurationSectionNavigation()
    {
        return _flexConfig.Configuration.CurrentConfig("Database");
    }

    [Benchmark]
    public string MultipleSectionAccess()
    {
        // Simulate accessing multiple related configuration values
        var dbPrimary = _flexConfig["Database:ConnectionStrings:Primary"];
        var dbTimeout = _flexConfig["Database:Settings:Timeout"];
        var apiKey = _flexConfig["Api:Authentication:Key"];
        
        // Return a concatenated result to ensure all work is done
        return $"{dbPrimary}-{dbTimeout}-{apiKey}";
    }
}