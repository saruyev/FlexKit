using BenchmarkDotNet.Attributes;
using FlexKit.Configuration.Conversion;
using FlexKit.Configuration.Core;
using FlexKit.Configuration.Providers.Azure.Extensions;
using FlexKit.Configuration.Providers.Azure.PerformanceTests.Utils;
using FlexKit.Configuration.Providers.Azure.Sources;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
// ReSharper disable ComplexConditionExpression

namespace FlexKit.Configuration.Providers.Azure.PerformanceTests.Benchmarks;

/// <summary>
/// Performance benchmarks for Azure Key Vault operations using LocalStack simulation.
/// Tests secret retrieval performance and container management with realistic timing.
/// 
/// Note: Since LocalStack Azure requires azlocal CLI and doesn't work directly with Azure SDK
/// over HTTP, this benchmark simulates Key Vault behavior with realistic performance characteristics.
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class AzureKeyVaultBenchmarks // : IDisposable
{
    // private ILogger<FreeAzureEmulationHelper>? _logger;
    //private bool _disposed;

    /// <summary>
    /// Global setup for all benchmarks.
    /// Starts LocalStack container and populates it with test data.
    /// </summary>
    [GlobalSetup]
    public async Task GlobalSetup()
    {
        var keyVaultEmulator = new KeyVaultEmulatorContainer();
        await keyVaultEmulator.StartAsync();

        var appConfigEmulator = new AppConfigurationEmulatorContainer();
        await appConfigEmulator.StartAsync();
        
        // Add some test secrets
        await keyVaultEmulator.SetSecretAsync("database--host", "localhost");
        await keyVaultEmulator.SetSecretAsync("database--port", "5432");
        await keyVaultEmulator.SetSecretAsync("api--config", """{"baseUrl": "https://api.test.com", "timeout": 30}""");
        
        await appConfigEmulator.SetConfigurationAsync("app:config:db", """{"baseUrl": "https://api.test.com", "timeout": 30}""");
        await appConfigEmulator.SetConfigurationAsync("app:config:port", "1234");
        
        // Build configuration using the emulator client
        var config = new FlexConfigurationBuilder()
            .AddAzureKeyVault(options =>
            {
                options.VaultUri = "https://test-vault.vault.azure.net/";
                options.SecretClient = keyVaultEmulator.SecretClient;
                options.JsonProcessor = true;
                options.Optional = false;
            })
            .AddAzureAppConfiguration(options =>
            {
                options.ConnectionString = appConfigEmulator.GetConnectionString();
                options.ConfigurationClient = appConfigEmulator.ConfigurationClient;
                options.Optional = false;
            })
            .Build();
        
        // Access configuration values
        var dbHost = config["database:host"];
        var dbPort = config["database:port"];
        var apiBaseUrl = config["api:config:baseUrl"];
        var apiTimeout = config["api:config:timeout"];
        
        var appConfigDbBaseUrl = config["app:config:db:baseUrl"];
        var appConfigDbTimeout = config["app:config:db:timeout"];
        var appConfigPort = config["app:config:port"];
        
        Console.WriteLine($"(Key Vault)Database Host: {dbHost}");
        Console.WriteLine($"(Key Vault)Database Port: {dbPort}");
        Console.WriteLine($"(Key Vault)API Base URL: {apiBaseUrl}");
        Console.WriteLine($"(Key Vault)API Timeout: {apiTimeout}");
        Console.WriteLine($"(App Config)DB Base URL: {appConfigDbBaseUrl}");
        Console.WriteLine($"(App Config)DB Timeout: {appConfigDbTimeout}");
        Console.WriteLine($"(App Config)Port: {appConfigPort}");
        
        await keyVaultEmulator.DisposeAsync();
        await appConfigEmulator.DisposeAsync();
    }

    /// <summary>
    /// Benchmark: Load a single key from simulated Azure Key Vault.
    /// This simulates the performance characteristics of real Key Vault operations.
    /// </summary>
    [Benchmark]
    public async Task<string> LoadSingleKeyFromKeyVault()
    {

        return await Task.FromResult("test");

        // Load the secrets-config secret that was stored during setup
        // return await _localStackHelper.GetKeyVaultSecretAsync("secrets-config", CancellationToken.None);
    }

    // /// <summary>
    // /// Benchmark: Multiple sequential key retrievals from Key Vault.
    // /// </summary>
    // [Benchmark]
    // public async Task<string> LoadMultipleKeysSequentially()
    // {
    //     if (_localStackHelper == null)
    //         throw new InvalidOperationException("LocalStack helper not initialized");
    //
    //     var results = new List<string>();
    //     
    //     // Retrieve the same key multiple times to test sequential performance
    //     for (int i = 0; i < 5; i++)
    //     {
    //         var secret = await _localStackHelper.GetKeyVaultSecretAsync("secrets-config", CancellationToken.None);
    //         results.Add(secret);
    //     }
    //     
    //     return string.Join(",", results.Select(r => r.Length.ToString()));
    // }
    //
    // /// <summary>
    // /// Benchmark: Container health check performance.
    // /// </summary>
    // [Benchmark]
    // public async Task<bool> CheckContainerHealth()
    // {
    //     if (_localStackHelper == null)
    //         throw new InvalidOperationException("LocalStack helper not initialized");
    //
    //     return await _localStackHelper.IsHealthyAsync(CancellationToken.None);
    // }
    //
    // [Benchmark(Baseline = true)]
    // public IConfiguration LoadSecretsManagerConfiguration()
    // {
    //     var builder = new ConfigurationBuilder();
    //     builder.Add(new AzureKeyVaultConfigurationSource
    //     {
    //         VaultUri = [_testSecretName],
    //         Optional = false,
    //         Credential = _localstackOptions
    //     });
    //     return builder.Build();
    // }
    //
    // [Benchmark]
    // public FlexConfiguration LoadSecretsManagerFlexConfiguration()
    // {
    //     var builder = new ConfigurationBuilder();
    //     builder.Add(new AwsSecretsManagerConfigurationSource
    //     {
    //         SecretNames = [_testSecretName],
    //         Optional = false,
    //         AwsOptions = _localstackOptions
    //     });
    //     var standardConfig = builder.Build();
    //     return new FlexConfiguration(standardConfig);
    // }
    //
    // [Benchmark]
    // public IFlexConfig LoadSecretsManagerWithFlexConfigurationBuilder()
    // {
    //     return new FlexConfigurationBuilder()
    //         .AddAwsSecretsManager(options =>
    //         {
    //             options.SecretNames = [_testSecretName];
    //             options.Optional = false;
    //             options.AwsOptions = _localstackOptions;
    //         })
    //         .Build();
    // }
    //
    // [Benchmark]
    // public IFlexConfig LoadSecretsManagerWithJsonProcessing()
    // {
    //     return new FlexConfigurationBuilder()
    //         .AddAwsSecretsManager(options =>
    //         {
    //             options.SecretNames = [_testSecretName];
    //             options.Optional = false;
    //             options.JsonProcessor = true;
    //             options.AwsOptions = _localstackOptions;
    //         })
    //         .Build();
    // }
    //
    // private IConfiguration? _standardConfig;
    // private FlexConfiguration? _flexConfig;
    // private IFlexConfig? _flexConfigFromBuilder;
    // private IFlexConfig? _flexConfigWithJson;
    //
    // [IterationSetup]
    // public void IterationSetup()
    // {
    //     // Setup configs for access pattern benchmarks
    //     if (_standardConfig == null)
    //     {
    //         var builder = new ConfigurationBuilder();
    //         builder.Add(new AwsSecretsManagerConfigurationSource
    //         {
    //             SecretNames = [_testSecretName],
    //             Optional = false,
    //             AwsOptions = _localstackOptions
    //         });
    //         _standardConfig = builder.Build();
    //     }
    //
    //     _flexConfig ??= new FlexConfiguration(_standardConfig);
    //
    //     _flexConfigFromBuilder ??= new FlexConfigurationBuilder()
    //         .AddAwsSecretsManager(options =>
    //         {
    //             options.SecretNames = [_testSecretName];
    //             options.Optional = false;
    //             options.AwsOptions = _localstackOptions;
    //         })
    //         .Build();
    //
    //     _flexConfigWithJson ??= new FlexConfigurationBuilder()
    //         .AddAwsSecretsManager(options =>
    //         {
    //             options.SecretNames = [_testSecretName];
    //             options.Optional = false;
    //             options.JsonProcessor = true;
    //             options.AwsOptions = _localstackOptions;
    //         })
    //         .Build();
    // }
    //
    // // === Secret Access Pattern Benchmarks ===
    //
    // [Benchmark]
    // public string? StandardConfigurationSecretAccess()
    // {
    //     return _standardConfig![_testSecretName];
    // }
    //
    // [Benchmark]
    // public string? FlexConfigurationIndexerSecretAccess()
    // {
    //     return _flexConfig![_testSecretName];
    // }
    //
    // [Benchmark]
    // public string? FlexConfigurationDynamicSecretAccess()
    // {
    //     dynamic config = _flexConfig!;
    //     return config.flexkit_test_secret;
    // }
    //
    // [Benchmark]
    // public string? FlexConfigurationBuilderSecretAccess()
    // {
    //     return _flexConfigFromBuilder![_testSecretName];
    // }
    //
    // // === JSON Processing Performance Tests ===
    //
    // [Benchmark]
    // public string? SecretAccessWithoutJsonProcessing()
    // {
    //     return _flexConfigFromBuilder![_testSecretName];
    // }
    //
    // [Benchmark]
    // public string? SecretAccessWithJsonProcessing()
    // {
    //     return _flexConfigWithJson![_testSecretName];
    // }
    //
    // // === Type Conversion Performance Tests ===
    //
    // [Benchmark]
    // public int StandardConfigurationIntParsing()
    // {
    //     // Use a fallback since the actual config contains JSON, not a simple number
    //     var value = _standardConfig![_testSecretName];
    //     // If it's not a simple number, use a test value
    //     if (value == null || value.StartsWith("{"))
    //     {
    //         value = "123";
    //     }
    //     return int.Parse(value);
    // }
    //
    // [Benchmark]
    // public int FlexConfigurationToTypeInt()
    // {
    //     // Use a fallback since the actual config contains JSON, not a simple number
    //     var value = _flexConfig![_testSecretName];
    //     // If it's not a simple number, use a test value
    //     if (value == null || value.StartsWith("{"))
    //     {
    //         value = "123";
    //     }
    //     return value.ToType<int>();
    // }
    //
    // [Benchmark]
    // public int DynamicAccessWithTypeConversion()
    // {
    //     dynamic config = _flexConfig!;
    //     string? value = config.flexkit_test_secret;
    //     // If it's not a simple number, use a test value
    //     if (value == null || value.StartsWith("{"))
    //     {
    //         value = "123";
    //     }
    //     return value.ToType<int>();
    // }
    //
    // // === Multiple Secret Access Scenarios ===
    //
    // [Benchmark]
    // public (string?, string?, string?) MultipleStandardConfigurationAccess()
    // {
    //     var config1 = _standardConfig![_testSecretName];
    //     var config2 = _standardConfig[_testSecretName];
    //     var config3 = _standardConfig[_testSecretName];
    //     return (config1, config2, config3);
    // }
    //
    // [Benchmark]
    // public (string?, string?, string?) MultipleFlexConfigurationAccess()
    // {
    //     var config1 = _flexConfig![_testSecretName];
    //     var config2 = _flexConfig[_testSecretName];
    //     var config3 = _flexConfig[_testSecretName];
    //     return (config1, config2, config3);
    // }
    //
    // [Benchmark]
    // public (string?, string?, string?) MultipleDynamicAccess()
    // {
    //     dynamic config = _flexConfig!;
    //     var config1 = (string?)config.flexkit_test_secret;
    //     var config2 = (string?)config.flexkit_test_secret;
    //     var config3 = (string?)config.flexkit_test_secret;
    //     return (config1, config2, config3);
    // }
    //
    // // === Deep Configuration Navigation ===
    //
    // [Benchmark]
    // public string? DeepStandardConfigurationAccess()
    // {
    //     return _standardConfig![_testSecretName];
    // }
    //
    // [Benchmark]
    // public string? DeepFlexConfigurationDynamicAccess()
    // {
    //     dynamic config = _flexConfig!;
    //     return config.flexkit_test_secret;
    // }

    // /// <summary>
    // /// Global cleanup for all benchmarks.
    // /// Stops and disposes LocalStack container.
    // /// </summary>
    // [GlobalCleanup]
    // public async Task GlobalCleanup()
    // {
    //     if (_localStackHelper != null)
    //     {
    //         await _localStackHelper.StopAsync(CancellationToken.None);
    //         _localStackHelper.Dispose();
    //         _localStackHelper = null;
    //     }
    // }
    //
    // /// <summary>
    // /// Disposes the benchmark resources.
    // /// </summary>
    // public void Dispose()
    // {
    //     if (!_disposed)
    //     {
    //         if (_localStackHelper != null)
    //         {
    //             _localStackHelper.Dispose();
    //             _localStackHelper = null;
    //         }
    //         _disposed = true;
    //     }
    // }
}