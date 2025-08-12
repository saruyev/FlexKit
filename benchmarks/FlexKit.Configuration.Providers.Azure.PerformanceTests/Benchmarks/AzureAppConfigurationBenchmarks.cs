using BenchmarkDotNet.Attributes;
using FlexKit.Configuration.Core;
using FlexKit.Configuration.Providers.Azure.Extensions;
using FlexKit.Configuration.Providers.Azure.PerformanceTests.Utils;
using FlexKit.Configuration.Conversion;
// ReSharper disable ClassTooBig
// ReSharper disable ComplexConditionExpression
// ReSharper disable MethodTooLong
// ReSharper disable NullableWarningSuppressionIsUsed

namespace FlexKit.Configuration.Providers.Azure.PerformanceTests.Benchmarks;

/// <summary>
/// Benchmarks for Azure App Configuration integration with FlexKit Configuration.
/// Tests the performance of loading configuration from App Configuration emulator with different
/// data sizes, key filtering, labels, and JSON processing options.
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class AzureAppConfigurationBenchmarks
{
    private AppConfigurationEmulatorContainer _appConfigEmulator = null!;
    private IFlexConfig _simpleConfig = null!;
    private IFlexConfig _complexConfig = null!;
    private IFlexConfig _largeConfig = null!;
    private IFlexConfig _jsonProcessingConfig = null!;
    private IFlexConfig _filteredConfig = null!;
    private IFlexConfig _labeledConfig = null!;

    [GlobalSetup]
    public async Task Setup()
    {
        _appConfigEmulator = new AppConfigurationEmulatorContainer();
        await _appConfigEmulator.StartAsync();

        // Setup simple configuration
        await _appConfigEmulator.CreateTestDataAsync("./TestData/simple-config.json");
        
        var simpleConfigBuilder = new FlexConfigurationBuilder()
            .AddAzureAppConfiguration(options =>
            {
                options.ConnectionString = _appConfigEmulator.GetConnectionString();
                options.ConfigurationClient = _appConfigEmulator.ConfigurationClient;
                options.JsonProcessor = false;
                options.Optional = false;
            });
        _simpleConfig = simpleConfigBuilder.Build();

        // Setup complex configuration
        await _appConfigEmulator.CreateTestDataAsync("./TestData/complex-secret.json");
        
        var complexConfigBuilder = new FlexConfigurationBuilder()
            .AddAzureAppConfiguration(options =>
            {
                options.ConnectionString = _appConfigEmulator.GetConnectionString();
                options.ConfigurationClient = _appConfigEmulator.ConfigurationClient;
                options.JsonProcessor = false;
                options.Optional = false;
            });
        _complexConfig = complexConfigBuilder.Build();

        // Setup large configuration
        await _appConfigEmulator.CreateTestDataAsync("./TestData/large-secret.json");
        
        var largeConfigBuilder = new FlexConfigurationBuilder()
            .AddAzureAppConfiguration(options =>
            {
                options.ConnectionString = _appConfigEmulator.GetConnectionString();
                options.ConfigurationClient = _appConfigEmulator.ConfigurationClient;
                options.JsonProcessor = false;
                options.Optional = false;
            });
        _largeConfig = largeConfigBuilder.Build();

        // Setup JSON processing configuration
        var jsonConfigBuilder = new FlexConfigurationBuilder()
            .AddAzureAppConfiguration(options =>
            {
                options.ConnectionString = _appConfigEmulator.GetConnectionString();
                options.ConfigurationClient = _appConfigEmulator.ConfigurationClient;
                options.JsonProcessor = true;
                options.Optional = false;
            });
        _jsonProcessingConfig = jsonConfigBuilder.Build();

        // Setup filtered configuration (database keys only)
        var filteredConfigBuilder = new FlexConfigurationBuilder()
            .AddAzureAppConfiguration(options =>
            {
                options.ConnectionString = _appConfigEmulator.GetConnectionString();
                options.ConfigurationClient = _appConfigEmulator.ConfigurationClient;
                options.KeyFilter = "database*";
                options.JsonProcessor = false;
                options.Optional = false;
            });
        _filteredConfig = filteredConfigBuilder.Build();

        // Setup labeled configuration with production label
        // Add some labeled configuration data
        await _appConfigEmulator.SetConfigurationAsync("app:environment", "production", "prod");
        await _appConfigEmulator.SetConfigurationAsync("app:database:host", "prod-db.example.com", "prod");
        await _appConfigEmulator.SetConfigurationAsync("app:cache:enabled", "true", "prod");
        
        var labeledConfigBuilder = new FlexConfigurationBuilder()
            .AddAzureAppConfiguration(options =>
            {
                options.ConnectionString = _appConfigEmulator.GetConnectionString();
                options.ConfigurationClient = _appConfigEmulator.ConfigurationClient;
                options.Label = "prod";
                options.JsonProcessor = false;
                options.Optional = false;
            });
        _labeledConfig = labeledConfigBuilder.Build();
    }

    [GlobalCleanup]
    public async Task Cleanup()
    {
        await _appConfigEmulator.DisposeAsync();
    }

    // === Configuration Building Benchmarks ===

    [Benchmark(Baseline = true)]
    public IFlexConfig BuildSimpleAppConfigConfiguration()
    {
        return new FlexConfigurationBuilder()
            .AddAzureAppConfiguration(options =>
            {
                options.ConnectionString = _appConfigEmulator.GetConnectionString();
                options.ConfigurationClient = _appConfigEmulator.ConfigurationClient;
                options.JsonProcessor = false;
                options.Optional = false;
            })
            .Build();
    }

    [Benchmark]
    public IFlexConfig BuildComplexAppConfigConfiguration()
    {
        return new FlexConfigurationBuilder()
            .AddAzureAppConfiguration(options =>
            {
                options.ConnectionString = _appConfigEmulator.GetConnectionString();
                options.ConfigurationClient = _appConfigEmulator.ConfigurationClient;
                options.JsonProcessor = false;
                options.Optional = false;
            })
            .Build();
    }

    [Benchmark]
    public IFlexConfig BuildLargeAppConfigConfiguration()
    {
        return new FlexConfigurationBuilder()
            .AddAzureAppConfiguration(options =>
            {
                options.ConnectionString = _appConfigEmulator.GetConnectionString();
                options.ConfigurationClient = _appConfigEmulator.ConfigurationClient;
                options.JsonProcessor = false;
                options.Optional = false;
            })
            .Build();
    }

    [Benchmark]
    public IFlexConfig BuildAppConfigWithJsonProcessing()
    {
        return new FlexConfigurationBuilder()
            .AddAzureAppConfiguration(options =>
            {
                options.ConnectionString = _appConfigEmulator.GetConnectionString();
                options.ConfigurationClient = _appConfigEmulator.ConfigurationClient;
                options.JsonProcessor = true;
                options.Optional = false;
            })
            .Build();
    }

    [Benchmark]
    public IFlexConfig BuildAppConfigWithKeyFilter()
    {
        return new FlexConfigurationBuilder()
            .AddAzureAppConfiguration(options =>
            {
                options.ConnectionString = _appConfigEmulator.GetConnectionString();
                options.ConfigurationClient = _appConfigEmulator.ConfigurationClient;
                options.KeyFilter = "database*";
                options.JsonProcessor = false;
                options.Optional = false;
            })
            .Build();
    }

    [Benchmark]
    public IFlexConfig BuildAppConfigWithLabel()
    {
        return new FlexConfigurationBuilder()
            .AddAzureAppConfiguration(options =>
            {
                options.ConnectionString = _appConfigEmulator.GetConnectionString();
                options.ConfigurationClient = _appConfigEmulator.ConfigurationClient;
                options.Label = "prod";
                options.JsonProcessor = false;
                options.Optional = false;
            })
            .Build();
    }

    // === Configuration Access Benchmarks ===

    [Benchmark]
    public string? SimpleConfigurationAccess()
    {
        return _simpleConfig["Application:Name"];
    }

    [Benchmark]
    public string? ComplexConfigurationAccess()
    {
        return _complexConfig["database:primary:host"];
    }

    [Benchmark]
    public string? LargeConfigurationAccess()
    {
        return _largeConfig["microservices:userService:database:host"];
    }

    [Benchmark]
    public string? JsonProcessedConfigurationAccess()
    {
        return _jsonProcessingConfig["Application:Name"];
    }

    [Benchmark]
    public string? FilteredConfigurationAccess()
    {
        return _filteredConfig["database:primary:host"];
    }

    [Benchmark]
    public string? LabeledConfigurationAccess()
    {
        return _labeledConfig["app:environment"];
    }

    // === Dynamic Access Benchmarks ===

    [Benchmark]
    public string? SimpleDynamicAccess()
    {
        dynamic config = _simpleConfig;
        return config.Application?.Name;
    }

    [Benchmark]
    public string? ComplexDynamicAccess()
    {
        dynamic config = _complexConfig;
        return config.database?.primary?.host;
    }

    [Benchmark]
    public string? LargeDynamicAccess()
    {
        dynamic config = _largeConfig;
        return config.microservices?.userService?.database?.host;
    }

    [Benchmark]
    public string? JsonProcessedDynamicAccess()
    {
        dynamic config = _jsonProcessingConfig;
        return config.Application?.Name;
    }

    [Benchmark]
    public string? FilteredDynamicAccess()
    {
        dynamic config = _filteredConfig;
        return config.database?.primary?.host;
    }

    [Benchmark]
    public string? LabeledDynamicAccess()
    {
        dynamic config = _labeledConfig;
        return config.app?.environment;
    }

    // === Type Conversion Benchmarks ===

    [Benchmark]
    public int DatabasePortConversion()
    {
        var portValue = _complexConfig["database:primary:port"];
        return portValue.ToType<int>();
    }

    [Benchmark]
    public bool SslEnabledConversion()
    {
        var sslValue = _complexConfig["database:primary:ssl"];
        return sslValue.ToType<bool>();
    }

    [Benchmark]
    public int MaxConnectionsConversion()
    {
        var maxConnValue = _complexConfig["database:primary:maxConnections"];
        return maxConnValue.ToType<int>();
    }

    [Benchmark]
    public bool CacheEnabledConversion()
    {
        var cacheValue = _labeledConfig["app:cache:enabled"];
        return cacheValue.ToType<bool>();
    }

    // === Deep Hierarchy Access Benchmarks ===

    [Benchmark]
    public string? DeepLargeConfigAccess()
    {
        return _largeConfig["microservices:paymentService:external:stripeKey"];
    }

    [Benchmark]
    public string? VeryDeepConfigAccess()
    {
        return _largeConfig["infrastructure:monitoring:datadog:apiKey"];
    }

    [Benchmark]
    public string? CertificateConfigAccess()
    {
        return _largeConfig["certificates:ssl:certificate"];
    }

    // === Multiple Values Access Benchmark ===

    [Benchmark]
    public (string?, string?, string?, bool?) MultipleValuesAccess()
    {
        var dbHost = _complexConfig["database:primary:host"];
        var dbPort = _complexConfig["database:primary:port"];
        var dbUser = _complexConfig["database:primary:username"];
        var sslEnabled = _complexConfig["database:primary:ssl"].ToType<bool>();
        
        return (dbHost, dbPort, dbUser, sslEnabled);
    }

    // === JSON Processing Comparison Benchmarks ===

    [Benchmark]
    public string? WithoutJsonProcessing()
    {
        return _complexConfig["apis:external:paymentService:baseUrl"];
    }

    [Benchmark]
    public string? WithJsonProcessing()
    {
        return _jsonProcessingConfig["apis:external:paymentService:baseUrl"];
    }

    // === Key Filtering Performance ===

    [Benchmark]
    public string? UnfilteredKeyAccess()
    {
        return _complexConfig["database:primary:host"];
    }

    [Benchmark]
    public string? FilteredKeyAccess()
    {
        return _filteredConfig["database:primary:host"];
    }

    // === Label-based Access Performance ===

    [Benchmark]
    public string? DefaultLabelAccess()
    {
        return _simpleConfig["Application:Name"];
    }

    [Benchmark]
    public string? ProductionLabelAccess()
    {
        return _labeledConfig["app:environment"];
    }

    // === Configuration Section Access ===

    [Benchmark]
    public int DatabaseSectionAccessCount()
    {
        var section = _complexConfig.Configuration.GetSection("database:primary");
        return section.GetChildren().Count();
    }

    [Benchmark]
    public int LargeSectionAccessCount()
    {
        var section = _largeConfig.Configuration.GetSection("microservices");
        return section.GetChildren().Count();
    }

    [Benchmark]
    public int FilteredSectionAccessCount()
    {
        var section = _filteredConfig.Configuration.GetSection("database");
        return section.GetChildren().Count();
    }

    // === Performance Comparison: Different Connection Approaches ===

    [Benchmark]
    public IFlexConfig BuildWithConnectionString()
    {
        return new FlexConfigurationBuilder()
            .AddAzureAppConfiguration(_appConfigEmulator.GetConnectionString())
            .Build();
    }

    [Benchmark]
    public IFlexConfig BuildWithOptionsAndClient()
    {
        return new FlexConfigurationBuilder()
            .AddAzureAppConfiguration(options =>
            {
                options.ConnectionString = _appConfigEmulator.GetConnectionString();
                options.ConfigurationClient = _appConfigEmulator.ConfigurationClient;
            })
            .Build();
    }
}
