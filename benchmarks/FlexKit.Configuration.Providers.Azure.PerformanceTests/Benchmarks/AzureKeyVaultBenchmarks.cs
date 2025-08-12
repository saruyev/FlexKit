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
/// Benchmarks for Azure Key Vault integration with FlexKit Configuration.
/// Tests the performance of loading secrets from Key Vault emulator with different
/// data sizes and processing options, including JSON flattening.
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class AzureKeyVaultBenchmarks
{
    private KeyVaultEmulatorContainer _keyVaultEmulator = null!;
    private IFlexConfig _simpleConfig = null!;
    private IFlexConfig _complexConfig = null!;
    private IFlexConfig _largeConfig = null!;
    private IFlexConfig _jsonProcessingConfig = null!;

    [GlobalSetup]
    public async Task Setup()
    {
        _keyVaultEmulator = new KeyVaultEmulatorContainer();
        await _keyVaultEmulator.StartAsync();

        // Setup simple configuration
        await _keyVaultEmulator.CreateTestDataAsync("./TestData/simple-config.json");
        
        var simpleConfigBuilder = new FlexConfigurationBuilder()
            .AddAzureKeyVault(options =>
            {
                options.VaultUri = "https://test-vault.vault.azure.net/";
                options.SecretClient = _keyVaultEmulator.SecretClient;
                options.JsonProcessor = false;
                options.Optional = false;
            });
        _simpleConfig = simpleConfigBuilder.Build();

        // Setup complex configuration
        await _keyVaultEmulator.CreateTestDataAsync("./TestData/complex-secret.json");
        
        var complexConfigBuilder = new FlexConfigurationBuilder()
            .AddAzureKeyVault(options =>
            {
                options.VaultUri = "https://test-vault.vault.azure.net/";
                options.SecretClient = _keyVaultEmulator.SecretClient;
                options.JsonProcessor = false;
                options.Optional = false;
            });
        _complexConfig = complexConfigBuilder.Build();

        // Setup large configuration
        await _keyVaultEmulator.CreateTestDataAsync("./TestData/large-secret.json");
        
        var largeConfigBuilder = new FlexConfigurationBuilder()
            .AddAzureKeyVault(options =>
            {
                options.VaultUri = "https://test-vault.vault.azure.net/";
                options.SecretClient = _keyVaultEmulator.SecretClient;
                options.JsonProcessor = false;
                options.Optional = false;
            });
        _largeConfig = largeConfigBuilder.Build();

        // Setup JSON processing configuration
        var jsonConfigBuilder = new FlexConfigurationBuilder()
            .AddAzureKeyVault(options =>
            {
                options.VaultUri = "https://test-vault.vault.azure.net/";
                options.SecretClient = _keyVaultEmulator.SecretClient;
                options.JsonProcessor = true;
                options.Optional = false;
            });
        _jsonProcessingConfig = jsonConfigBuilder.Build();
    }

    [GlobalCleanup]
    public async Task Cleanup()
    {
        await _keyVaultEmulator.DisposeAsync();
    }

    // === Configuration Building Benchmarks ===

    [Benchmark(Baseline = true)]
    public IFlexConfig BuildSimpleKeyVaultConfiguration()
    {
        return new FlexConfigurationBuilder()
            .AddAzureKeyVault(options =>
            {
                options.VaultUri = "https://test-vault.vault.azure.net/";
                options.SecretClient = _keyVaultEmulator.SecretClient;
                options.JsonProcessor = false;
                options.Optional = false;
            })
            .Build();
    }

    [Benchmark]
    public IFlexConfig BuildComplexKeyVaultConfiguration()
    {
        return new FlexConfigurationBuilder()
            .AddAzureKeyVault(options =>
            {
                options.VaultUri = "https://test-vault.vault.azure.net/";
                options.SecretClient = _keyVaultEmulator.SecretClient;
                options.JsonProcessor = false;
                options.Optional = false;
            })
            .Build();
    }

    [Benchmark]
    public IFlexConfig BuildLargeKeyVaultConfiguration()
    {
        return new FlexConfigurationBuilder()
            .AddAzureKeyVault(options =>
            {
                options.VaultUri = "https://test-vault.vault.azure.net/";
                options.SecretClient = _keyVaultEmulator.SecretClient;
                options.JsonProcessor = false;
                options.Optional = false;
            })
            .Build();
    }

    [Benchmark]
    public IFlexConfig BuildKeyVaultWithJsonProcessing()
    {
        return new FlexConfigurationBuilder()
            .AddAzureKeyVault(options =>
            {
                options.VaultUri = "https://test-vault.vault.azure.net/";
                options.SecretClient = _keyVaultEmulator.SecretClient;
                options.JsonProcessor = true;
                options.Optional = false;
            })
            .Build();
    }

    // === Secret Access Benchmarks ===

    [Benchmark]
    public string? SimpleSecretAccess()
    {
        return _simpleConfig["Application:Name"];
    }

    [Benchmark]
    public string? ComplexSecretAccess()
    {
        return _complexConfig["database:primary:host"];
    }

    [Benchmark]
    public string? LargeConfigurationAccess()
    {
        return _largeConfig["microservices:userService:database:host"];
    }

    [Benchmark]
    public string? JsonProcessedSecretAccess()
    {
        return _jsonProcessingConfig["Application:Name"];
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
    public int TimeoutConversion()
    {
        var timeoutValue = _complexConfig["database:primary:connectionTimeout"];
        return timeoutValue.ToType<int>();
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
    public string? CertificateAccess()
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
}
