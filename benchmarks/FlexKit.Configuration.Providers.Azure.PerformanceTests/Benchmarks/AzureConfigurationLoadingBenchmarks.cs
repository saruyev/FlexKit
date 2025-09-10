using BenchmarkDotNet.Attributes;
using FlexKit.Configuration.Core;
using FlexKit.Configuration.Providers.Azure.Extensions;
using FlexKit.Configuration.Providers.Azure.PerformanceTests.Utils;
using FlexKit.Configuration.Conversion;
using Microsoft.Extensions.Configuration;

// ReSharper disable ClassTooBig
// ReSharper disable ComplexConditionExpression
// ReSharper disable MethodTooLong
// ReSharper disable NullableWarningSuppressionIsUsed

namespace FlexKit.Configuration.Providers.Azure.PerformanceTests.Benchmarks;

/// <summary>
/// Benchmarks for combined Azure Key Vault and App Configuration integration with FlexKit Configuration.
/// Tests realistic scenarios where both services are used together - secrets in Key Vault, 
/// non-sensitive configuration in App Configuration, with different loading strategies and precedence.
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class AzureConfigurationLoadingBenchmarks
{
    private KeyVaultEmulatorContainer _keyVaultEmulator = null!;
    private AppConfigurationEmulatorContainer _appConfigEmulator = null!;
    private IFlexConfig _keyVaultOnlyConfig = null!;
    private IFlexConfig _appConfigOnlyConfig = null!;
    private IFlexConfig _combinedConfig = null!;
    private IFlexConfig _combinedJsonConfig = null!;
    private IFlexConfig _layeredConfig = null!;
    private IFlexConfig _productionConfig = null!;

    [GlobalSetup]
    public async Task Setup()
    {
        // Start both emulators
        _keyVaultEmulator = new KeyVaultEmulatorContainer();
        await _keyVaultEmulator.StartAsync();

        _appConfigEmulator = new AppConfigurationEmulatorContainer();
        await _appConfigEmulator.StartAsync();

        // Setup Key Vault data (secrets and sensitive configuration)
        await _keyVaultEmulator.CreateTestDataAsync("./TestData/complex-secret.json");
        await _keyVaultEmulator.SetSecretAsync("database--credentials--password", "super-secret-password");
        await _keyVaultEmulator.SetSecretAsync("api--keys--payment", "pk_live_secret_key_12345");
        await _keyVaultEmulator.SetSecretAsync("certificates--ssl--private-key", "-----BEGIN PRIVATE KEY-----...");

        // Setup App Configuration data (non-sensitive configuration)
        await _appConfigEmulator.CreateTestDataAsync("./TestData/simple-config.json");
        await _appConfigEmulator.SetConfigurationAsync("app:features:caching", "true");
        await _appConfigEmulator.SetConfigurationAsync("app:features:logging", "verbose");
        await _appConfigEmulator.SetConfigurationAsync("app:timeout:default", "30");
        await _appConfigEmulator.SetConfigurationAsync("app:retry:maxAttempts", "3");

        // Setup production labeled configuration
        await _appConfigEmulator.SetConfigurationAsync("app:environment", "production", "prod");
        await _appConfigEmulator.SetConfigurationAsync("app:database:host", "prod-db.example.com", "prod");
        await _appConfigEmulator.SetConfigurationAsync("app:cache:timeout", "300", "prod");

        // === Key Vault Only Configuration ===
        var keyVaultOnlyBuilder = new FlexConfigurationBuilder()
            .AddAzureKeyVault(options =>
            {
                options.VaultUri = "https://test-vault.vault.azure.net/";
                options.SecretClient = _keyVaultEmulator.SecretClient;
                options.JsonProcessor = false;
                options.Optional = false;
            });
        _keyVaultOnlyConfig = keyVaultOnlyBuilder.Build();

        // === App Configuration Only ===
        var appConfigOnlyBuilder = new FlexConfigurationBuilder()
            .AddAzureAppConfiguration(options =>
            {
                options.ConnectionString = _appConfigEmulator.GetConnectionString();
                options.ConfigurationClient = _appConfigEmulator.ConfigurationClient;
                options.JsonProcessor = false;
                options.Optional = false;
            });
        _appConfigOnlyConfig = appConfigOnlyBuilder.Build();

        // === Combined Configuration (Basic) ===
        var combinedBuilder = new FlexConfigurationBuilder()
            .AddAzureAppConfiguration(options =>
            {
                options.ConnectionString = _appConfigEmulator.GetConnectionString();
                options.ConfigurationClient = _appConfigEmulator.ConfigurationClient;
                options.JsonProcessor = false;
                options.Optional = false;
            })
            .AddAzureKeyVault(options =>
            {
                options.VaultUri = "https://test-vault.vault.azure.net/";
                options.SecretClient = _keyVaultEmulator.SecretClient;
                options.JsonProcessor = false;
                options.Optional = false;
            });
        _combinedConfig = combinedBuilder.Build();

        // === Combined Configuration with JSON Processing ===
        var combinedJsonBuilder = new FlexConfigurationBuilder()
            .AddAzureAppConfiguration(options =>
            {
                options.ConnectionString = _appConfigEmulator.GetConnectionString();
                options.ConfigurationClient = _appConfigEmulator.ConfigurationClient;
                options.JsonProcessor = true;
                options.Optional = false;
            })
            .AddAzureKeyVault(options =>
            {
                options.VaultUri = "https://test-vault.vault.azure.net/";
                options.SecretClient = _keyVaultEmulator.SecretClient;
                options.JsonProcessor = true;
                options.Optional = false;
            });
        _combinedJsonConfig = combinedJsonBuilder.Build();

        // === Layered Configuration (Realistic Production Setup) ===
        var layeredBuilder = new FlexConfigurationBuilder()
            // Base non-sensitive configuration from App Configuration
            .AddAzureAppConfiguration(options =>
            {
                options.ConnectionString = _appConfigEmulator.GetConnectionString();
                options.ConfigurationClient = _appConfigEmulator.ConfigurationClient;
                options.KeyFilter = "app:*";
                options.JsonProcessor = false;
                options.Optional = false;
            })
            // Sensitive secrets from Key Vault (higher precedence)
            .AddAzureKeyVault(options =>
            {
                options.VaultUri = "https://test-vault.vault.azure.net/";
                options.SecretClient = _keyVaultEmulator.SecretClient;
                options.JsonProcessor = true;
                options.Optional = false;
            });
        _layeredConfig = layeredBuilder.Build();

        // === Production Configuration with Labels ===
        var productionBuilder = new FlexConfigurationBuilder()
            // Base configuration
            .AddAzureAppConfiguration(options =>
            {
                options.ConnectionString = _appConfigEmulator.GetConnectionString();
                options.ConfigurationClient = _appConfigEmulator.ConfigurationClient;
                options.JsonProcessor = false;
                options.Optional = false;
            })
            // Production-specific configuration
            .AddAzureAppConfiguration(options =>
            {
                options.ConnectionString = _appConfigEmulator.GetConnectionString();
                options.ConfigurationClient = _appConfigEmulator.ConfigurationClient;
                options.Label = "prod";
                options.KeyFilter = "app:*";
                options.JsonProcessor = false;
                options.Optional = false;
            })
            // Secrets (the highest precedence)
            .AddAzureKeyVault(options =>
            {
                options.VaultUri = "https://test-vault.vault.azure.net/";
                options.SecretClient = _keyVaultEmulator.SecretClient;
                options.JsonProcessor = true;
                options.Optional = false;
            });
        _productionConfig = productionBuilder.Build();
    }

    [GlobalCleanup]
    public async Task Cleanup()
    {
        await _keyVaultEmulator.DisposeAsync();
        await _appConfigEmulator.DisposeAsync();
    }

    // === Configuration Building Benchmarks ===

    [Benchmark(Baseline = true)]
    public IFlexConfig BuildKeyVaultOnlyConfiguration()
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
    public IFlexConfig BuildAppConfigOnlyConfiguration()
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
    public IFlexConfig BuildCombinedConfiguration()
    {
        return new FlexConfigurationBuilder()
            .AddAzureAppConfiguration(options =>
            {
                options.ConnectionString = _appConfigEmulator.GetConnectionString();
                options.ConfigurationClient = _appConfigEmulator.ConfigurationClient;
                options.JsonProcessor = false;
                options.Optional = false;
            })
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
    public IFlexConfig BuildCombinedWithJsonProcessing()
    {
        return new FlexConfigurationBuilder()
            .AddAzureAppConfiguration(options =>
            {
                options.ConnectionString = _appConfigEmulator.GetConnectionString();
                options.ConfigurationClient = _appConfigEmulator.ConfigurationClient;
                options.JsonProcessor = true;
                options.Optional = false;
            })
            .AddAzureKeyVault(options =>
            {
                options.VaultUri = "https://test-vault.vault.azure.net/";
                options.SecretClient = _keyVaultEmulator.SecretClient;
                options.JsonProcessor = true;
                options.Optional = false;
            })
            .Build();
    }

    [Benchmark]
    public IFlexConfig BuildLayeredConfiguration()
    {
        return new FlexConfigurationBuilder()
            .AddAzureAppConfiguration(options =>
            {
                options.ConnectionString = _appConfigEmulator.GetConnectionString();
                options.ConfigurationClient = _appConfigEmulator.ConfigurationClient;
                options.KeyFilter = "app:*";
                options.JsonProcessor = false;
                options.Optional = false;
            })
            .AddAzureKeyVault(options =>
            {
                options.VaultUri = "https://test-vault.vault.azure.net/";
                options.SecretClient = _keyVaultEmulator.SecretClient;
                options.JsonProcessor = true;
                options.Optional = false;
            })
            .Build();
    }

    [Benchmark]
    public IFlexConfig BuildProductionConfiguration()
    {
        return new FlexConfigurationBuilder()
            .AddAzureAppConfiguration(options =>
            {
                options.ConnectionString = _appConfigEmulator.GetConnectionString();
                options.ConfigurationClient = _appConfigEmulator.ConfigurationClient;
                options.JsonProcessor = false;
                options.Optional = false;
            })
            .AddAzureAppConfiguration(options =>
            {
                options.ConnectionString = _appConfigEmulator.GetConnectionString();
                options.ConfigurationClient = _appConfigEmulator.ConfigurationClient;
                options.Label = "prod";
                options.KeyFilter = "app:*";
                options.JsonProcessor = false;
                options.Optional = false;
            })
            .AddAzureKeyVault(options =>
            {
                options.VaultUri = "https://test-vault.vault.azure.net/";
                options.SecretClient = _keyVaultEmulator.SecretClient;
                options.JsonProcessor = true;
                options.Optional = false;
            })
            .Build();
    }

    // === Single Source Access Benchmarks ===

    [Benchmark]
    public string? KeyVaultOnlyAccess()
    {
        return _keyVaultOnlyConfig["database:credentials:password"];
    }

    [Benchmark]
    public string? AppConfigOnlyAccess()
    {
        return _appConfigOnlyConfig["app:features:caching"];
    }

    // === Combined Source Access Benchmarks ===

    [Benchmark]
    public string? CombinedSecretAccess()
    {
        return _combinedConfig["database:credentials:password"];
    }

    [Benchmark]
    public string? CombinedConfigAccess()
    {
        return _combinedConfig["app:features:caching"];
    }

    [Benchmark]
    public string? CombinedJsonSecretAccess()
    {
        return _combinedJsonConfig["database:primary:host"];
    }

    [Benchmark]
    public string? LayeredSecretAccess()
    {
        return _layeredConfig["api:keys:payment"];
    }

    [Benchmark]
    public string? LayeredConfigAccess()
    {
        return _layeredConfig["app:timeout:default"];
    }

    [Benchmark]
    public string? ProductionConfigAccess()
    {
        return _productionConfig["app:environment"];
    }

    // === Dynamic Access Benchmarks ===

    [Benchmark]
    public string? CombinedDynamicSecretAccess()
    {
        dynamic config = _combinedConfig;
        return config.database?.credentials?.password;
    }

    [Benchmark]
    public string? CombinedDynamicConfigAccess()
    {
        dynamic config = _combinedConfig;
        return config.app?.features?.caching;
    }

    [Benchmark]
    public string? LayeredDynamicAccess()
    {
        dynamic config = _layeredConfig;
        return config.app?.timeout?.@default;
    }

    [Benchmark]
    public string? ProductionDynamicAccess()
    {
        dynamic config = _productionConfig;
        return config.app?.environment;
    }

    // === Configuration Precedence Benchmarks ===

    [Benchmark]
    public string? TestConfigurationPrecedence()
    {
        // Key Vault should override App Configuration
        return _combinedConfig["Application:Name"];
    }

    [Benchmark]
    public string? TestProductionPrecedence()
    {
        // Production label should override base configuration
        return _productionConfig["app:database:host"];
    }

    // === Type Conversion with Mixed Sources ===

    [Benchmark]
    public bool FeatureFlagConversion()
    {
        var flagValue = _combinedConfig["app:features:caching"];
        return flagValue.ToType<bool>();
    }

    [Benchmark]
    public int TimeoutConversion()
    {
        var timeoutValue = _layeredConfig["app:timeout:default"];
        return timeoutValue.ToType<int>();
    }

    [Benchmark]
    public int RetryCountConversion()
    {
        var retryValue = _productionConfig["app:retry:maxAttempts"];
        return retryValue.ToType<int>();
    }

    [Benchmark]
    public int CacheTimeoutConversion()
    {
        var cacheValue = _productionConfig["app:cache:timeout"];
        return cacheValue.ToType<int>();
    }

    // === Multi-Value Access Scenarios ===

    [Benchmark]
    public (string?, string?, bool?, int?) DatabaseConfigurationSet()
    {
        var host = _combinedConfig["database:primary:host"];
        var password = _combinedConfig["database:credentials:password"];
        var caching = _combinedConfig["app:features:caching"].ToType<bool>();
        var timeout = _combinedConfig["app:timeout:default"].ToType<int>();

        return (host, password, caching, timeout);
    }

    [Benchmark]
    public (string?, string?, string?, int?) ProductionConfigurationSet()
    {
        var environment = _productionConfig["app:environment"];
        var dbHost = _productionConfig["app:database:host"];
        var apiKey = _productionConfig["api:keys:payment"];
        var cacheTimeout = _productionConfig["app:cache:timeout"].ToType<int>();

        return (environment, dbHost, apiKey, cacheTimeout);
    }

    // === JSON Processing Performance Comparison ===

    [Benchmark]
    public string? WithoutJsonProcessingCombined()
    {
        return _combinedConfig["apis:external:paymentService:baseUrl"];
    }

    [Benchmark]
    public string? WithJsonProcessingCombined()
    {
        return _combinedJsonConfig["apis:external:paymentService:baseUrl"];
    }

    // === Section Access with Multiple Sources ===

    [Benchmark]
    public int CombinedDatabaseSectionCount()
    {
        var section = _combinedConfig.Configuration.GetSection("database");
        return section.GetChildren().Count();
    }

    [Benchmark]
    public int LayeredAppSectionCount()
    {
        var section = _layeredConfig.Configuration.GetSection("app");
        return section.GetChildren().Count();
    }

    [Benchmark]
    public int ProductionSectionCount()
    {
        var section = _productionConfig.Configuration.GetSection("app");
        return section.GetChildren().Count();
    }

    // === Configuration Enumeration Performance ===

    [Benchmark]
    public int CountAllCombinedKeys()
    {
        return _combinedConfig.Configuration.AsEnumerable().Count();
    }

    [Benchmark]
    public int CountAllLayeredKeys()
    {
        return _layeredConfig.Configuration.AsEnumerable().Count();
    }

    [Benchmark]
    public int CountAllProductionKeys()
    {
        return _productionConfig.Configuration.AsEnumerable().Count();
    }

    // === Realistic Application Startup Scenario ===

    [Benchmark]
    public (string?, string?, string?, bool?, int?, int?) ApplicationStartupConfiguration()
    {
        // Simulate typical application startup configuration loading
        var dbHost = _productionConfig["app:database:host"];
        var dbPassword = _productionConfig["database:credentials:password"];
        var apiKey = _productionConfig["api:keys:payment"];
        var cachingEnabled = _productionConfig["app:features:caching"].ToType<bool>();
        var timeout = _productionConfig["app:timeout:default"].ToType<int>();
        var retries = _productionConfig["app:retry:maxAttempts"].ToType<int>();

        return (dbHost, dbPassword, apiKey, cachingEnabled, timeout, retries);
    }
}
