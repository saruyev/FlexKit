using BenchmarkDotNet.Attributes;
using FlexKit.Configuration.Core;
using FlexKit.Configuration.Conversion;
using Microsoft.Extensions.Configuration;

namespace FlexKit.Configuration.PerformanceTests.Benchmarks;

/// <summary>
/// Benchmarks for large, realistic configuration files with 580+ keys and 5 levels of nesting.
/// Tests how FlexKit.Configuration performs with enterprise-scale configuration complexity.
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class LargeConfigurationBenchmarks
{
    private IConfiguration _standardConfig = null!;
    private FlexConfiguration _flexConfig = null!;
    private string _largeConfigJson = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Load the large configuration JSON file
        _largeConfigJson = LoadLargeConfigJson();
        
        // Setup standard configuration
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(_largeConfigJson)));
        _standardConfig = configBuilder.Build();
        
        // Setup FlexConfiguration
        _flexConfig = new FlexConfiguration(_standardConfig);
    }

    private string LoadLargeConfigJson()
    {
        // Load the actual large-config.json file with 580+ keys
        var configPath = Path.Combine("TestData", "large-config.json");
        
        if (!File.Exists(configPath))
        {
            throw new FileNotFoundException($"Large configuration file not found at: {configPath}. " +
                "Please ensure large-config.json is copied to the TestData folder.");
        }
        
        return File.ReadAllText(configPath);
    }

    // === Configuration Loading Benchmarks ===

    [Benchmark(Baseline = true)]
    public IConfiguration LoadStandardConfiguration()
    {
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(_largeConfigJson)));
        return configBuilder.Build();
    }

    [Benchmark]
    public FlexConfiguration LoadFlexConfiguration()
    {
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(_largeConfigJson)));
        var standardConfig = configBuilder.Build();
        return new FlexConfiguration(standardConfig);
    }

    // === Shallow Access Benchmarks (Level 1-2) ===

    [Benchmark]
    public string? StandardConfigShallowAccess()
    {
        return _standardConfig["Application:Name"];
    }

    [Benchmark]
    public string? FlexConfigIndexerShallowAccess()
    {
        return _flexConfig["Application:Name"];
    }

    [Benchmark]
    public string? FlexConfigDynamicShallowAccess()
    {
        dynamic config = _flexConfig;
        return ((string?)config.Application?.Configuration["Name"]);
    }

    // === Medium Depth Access Benchmarks (Level 3) ===

    [Benchmark]
    public string? StandardConfigMediumAccess()
    {
        return _standardConfig["Database:Primary:ConnectionString"];
    }

    [Benchmark]
    public string? FlexConfigIndexerMediumAccess()
    {
        return _flexConfig["Database:Primary:ConnectionString"];
    }

    [Benchmark]
    public string? FlexConfigDynamicMediumAccess()
    {
        dynamic config = _flexConfig;
        var database = config.Database;
        var primary = database?.Configuration.CurrentConfig("Primary");
        return ((string?)primary?.Configuration["ConnectionString"]);
    }

    // === Deep Access Benchmarks (Level 4-5) ===

    [Benchmark]
    public string? StandardConfigDeepAccess()
    {
        return _standardConfig["Regional:Regions:EU:DataResidencyRules:RequireEuDataStorage"];
    }

    [Benchmark]
    public string? FlexConfigIndexerDeepAccess()
    {
        return _flexConfig["Regional:Regions:EU:DataResidencyRules:RequireEuDataStorage"];
    }

    [Benchmark]
    public string? FlexConfigDynamicDeepAccess()
    {
        dynamic config = _flexConfig;
        var regional = config.Regional;
        var regions = regional?.Configuration.CurrentConfig("Regions");
        var eu = regions?.Configuration.CurrentConfig("EU");
        var dataRules = eu?.Configuration.CurrentConfig("DataResidencyRules");
        return ((string?)dataRules?.Configuration["RequireEuDataStorage"]);
    }

    // === Type Conversion with Large Config ===

    [Benchmark]
    public int StandardConfigTypeConversion()
    {
        var value = _standardConfig["Database:Primary:CommandTimeout"];
        return int.Parse(value ?? "0");
    }

    [Benchmark]
    public int FlexConfigTypeConversion()
    {
        var value = _flexConfig["Database:Primary:CommandTimeout"];
        return value.ToType<int>();
    }

    [Benchmark]
    public bool FlexConfigComplexTypeConversion()
    {
        var value = _flexConfig["Regional:Regions:EU:DataResidencyRules:RequireEuDataStorage"];
        return value.ToType<bool>();
    }

    // === Multiple Value Access Patterns ===

    [Benchmark]
    public object StandardConfigMultipleAccess()
    {
        return new
        {
            AppName = _standardConfig["Application:Name"],
            Version = _standardConfig["Application:Version"],
            Environment = _standardConfig["Application:Environment"],
            DatabaseConnection = _standardConfig["Database:Primary:ConnectionString"],
            DatabaseTimeout = int.Parse(_standardConfig["Database:Primary:CommandTimeout"] ?? "30"),
            CacheConnection = _standardConfig["Database:Cache:ConnectionString"],
            PaymentUrl = _standardConfig["ExternalServices:PaymentProcessor:Primary:BaseUrl"],
            JwtIssuer = _standardConfig["Security:Authentication:JwtSettings:Issuer"],
            DefaultRegion = _standardConfig["Regional:DefaultRegion"],
            EnableTracking = bool.Parse(_standardConfig["Features:Analytics:EnableUserTracking"] ?? "false")
        };
    }

    [Benchmark]
    public object FlexConfigIndexerMultipleAccess()
    {
        return new
        {
            AppName = _flexConfig["Application:Name"],
            Version = _flexConfig["Application:Version"],
            Environment = _flexConfig["Application:Environment"],
            DatabaseConnection = _flexConfig["Database:Primary:ConnectionString"],
            DatabaseTimeout = _flexConfig["Database:Primary:CommandTimeout"].ToType<int>(),
            CacheConnection = _flexConfig["Database:Cache:ConnectionString"],
            PaymentUrl = _flexConfig["ExternalServices:PaymentProcessor:Primary:BaseUrl"],
            JwtIssuer = _flexConfig["Security:Authentication:JwtSettings:Issuer"],
            DefaultRegion = _flexConfig["Regional:DefaultRegion"],
            EnableTracking = _flexConfig["Features:Analytics:EnableUserTracking"].ToType<bool>()
        };
    }

    [Benchmark]
    public object FlexConfigDynamicMultipleAccess()
    {
        dynamic config = _flexConfig;
        
        var app = config.Application;
        var database = config.Database;
        var primary = database?.Configuration.CurrentConfig("Primary");
        var cache = database?.Configuration.CurrentConfig("Cache");
        var external = config.ExternalServices;
        var payment = external?.Configuration.CurrentConfig("PaymentProcessor")?.Configuration.CurrentConfig("Primary");
        var security = config.Security;
        var auth = security?.Configuration.CurrentConfig("Authentication");
        var jwt = auth?.Configuration.CurrentConfig("JwtSettings");
        var regional = config.Regional;
        var features = config.Features;
        var analytics = features?.Configuration.CurrentConfig("Analytics");
        
        return new
        {
            AppName = (string?)app?.Configuration["Name"],
            Version = (string?)app?.Configuration["Version"],
            Environment = (string?)app?.Configuration["Environment"],
            DatabaseConnection = (string?)primary?.Configuration["ConnectionString"],
            DatabaseTimeout = ((string?)primary?.Configuration["CommandTimeout"])?.ToType<int>() ?? 30,
            CacheConnection = (string?)cache?.Configuration["ConnectionString"],
            PaymentUrl = (string?)payment?.Configuration["BaseUrl"],
            JwtIssuer = (string?)jwt?.Configuration["Issuer"],
            DefaultRegion = (string?)regional?.Configuration["DefaultRegion"],
            EnableTracking = ((string?)analytics?.Configuration["EnableUserTracking"])?.ToType<bool>() ?? false
        };
    }

    // === Section Navigation with Large Config ===

    [Benchmark]
    public object StandardConfigSectionNavigation()
    {
        var databaseSection = _standardConfig.GetSection("Database");
        var primarySection = databaseSection.GetSection("Primary");
        
        return new
        {
            ConnectionString = primarySection["ConnectionString"],
            CommandTimeout = int.Parse(primarySection["CommandTimeout"] ?? "30"),
            MaxRetryCount = int.Parse(primarySection["MaxRetryCount"] ?? "3"),
            EnableLogging = bool.Parse(primarySection["EnableLogging"] ?? "false")
        };
    }

    [Benchmark]
    public object FlexConfigSectionNavigation()
    {
        var databaseSection = _flexConfig.Configuration.CurrentConfig("Database");
        var primarySection = databaseSection?.Configuration.CurrentConfig("Primary");
        
        return new
        {
            ConnectionString = primarySection?.Configuration["ConnectionString"],
            CommandTimeout = primarySection?.Configuration["CommandTimeout"]?.ToType<int>() ?? 30,
            MaxRetryCount = primarySection?.Configuration["MaxRetryCount"]?.ToType<int>() ?? 3,
            EnableLogging = primarySection?.Configuration["EnableLogging"]?.ToType<bool>() ?? false
        };
    }

    // === Array/Collection Access Patterns ===

    [Benchmark]
    public object StandardConfigArrayAccess()
    {
        var whitelistedIps = _standardConfig.GetSection("Security:ApiSecurity:RateLimiting:WhitelistedIps")
            .GetChildren()
            .Select(c => c.Value)
            .ToArray();
            
        var supportedCurrencies = _standardConfig.GetSection("ExternalServices:PaymentProcessor:Primary:SupportedCurrencies")
            .GetChildren()
            .Select(c => c.Value)
            .ToArray();
            
        return new
        {
            WhitelistedIpCount = whitelistedIps.Length,
            FirstWhitelistedIp = whitelistedIps.FirstOrDefault(),
            SupportedCurrencyCount = supportedCurrencies.Length,
            FirstCurrency = supportedCurrencies.FirstOrDefault()
        };
    }

    [Benchmark]
    public object FlexConfigArrayAccess()
    {
        var whitelistedIps = _flexConfig.Configuration.GetSection("Security:ApiSecurity:RateLimiting:WhitelistedIps")
            .GetChildren()
            .Select(c => c.Value)
            .ToArray();
            
        var supportedCurrencies = _flexConfig.Configuration.GetSection("ExternalServices:PaymentProcessor:Primary:SupportedCurrencies")
            .GetChildren()
            .Select(c => c.Value)
            .ToArray();
            
        return new
        {
            WhitelistedIpCount = whitelistedIps.Length,
            FirstWhitelistedIp = whitelistedIps.FirstOrDefault(),
            SupportedCurrencyCount = supportedCurrencies.Length,
            FirstCurrency = supportedCurrencies.FirstOrDefault()
        };
    }

    // === Configuration Search/Enumeration Patterns ===

    [Benchmark]
    public int StandardConfigEnumerateAllKeys()
    {
        return CountAllConfigurationKeys(_standardConfig);
    }

    [Benchmark]
    public int FlexConfigEnumerateAllKeys()
    {
        return CountAllConfigurationKeys(_flexConfig.Configuration);
    }

    private int CountAllConfigurationKeys(IConfiguration configuration)
    {
        int count = 0;
        
        foreach (var section in configuration.GetChildren())
        {
            if (section.Value != null)
            {
                count++;
            }
            count += CountAllConfigurationKeys(section);
        }
        
        return count;
    }

    // === Complex Object Navigation ===

    [Benchmark]
    public object StandardConfigComplexNavigation()
    {
        var results = new List<object>();
        
        // Navigate through different sections of the large config
        var regions = _standardConfig.GetSection("Regional:Regions").GetChildren();
        foreach (var region in regions)
        {
            var taxRates = region.GetSection("TaxRates").GetChildren();
            foreach (var taxRate in taxRates)
            {
                results.Add(new
                {
                    Region = region.Key,
                    Location = taxRate.Key,
                    Rate = double.Parse(taxRate.Value ?? "0")
                });
            }
        }
        
        return results;
    }

    [Benchmark]
    public object FlexConfigComplexNavigation()
    {
        var results = new List<object>();
        
        // Navigate through different sections using FlexConfig
        var regions = _flexConfig.Configuration.GetSection("Regional:Regions").GetChildren();
        foreach (var region in regions)
        {
            var taxRates = region.GetSection("TaxRates").GetChildren();
            foreach (var taxRate in taxRates)
            {
                results.Add(new
                {
                    Region = region.Key,
                    Location = taxRate.Key,
                    Rate = taxRate.Value?.ToType<double>() ?? 0.0
                });
            }
        }
        
        return results;
    }
}