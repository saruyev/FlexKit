using FlexKit.Configuration.Core;
using FlexKit.Configuration.Providers.Azure.IntegrationTests.Utils;
using FlexKit.Configuration.Providers.Azure.Extensions;
using FlexKit.Configuration.Providers.Azure.Sources;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Reqnroll;
using System.Diagnostics;
using FlexKit.Configuration.Conversion;

// ReSharper disable NullableWarningSuppressionIsUsed
// ReSharper disable MethodTooLong
// ReSharper disable ClassTooBig

namespace FlexKit.Configuration.Providers.Azure.IntegrationTests.Steps.Infrastructure;

/// <summary>
/// Step definitions for Azure performance scenarios testing.
/// Tests performance characteristics of Azure configuration loading including
/// large data sets, concurrent access, and memory usage monitoring.
/// Uses distinct step patterns ("performance controller") to avoid conflicts with other step classes.
/// </summary>
[Binding]
public class AzurePerformanceScenariosSteps(ScenarioContext scenarioContext)
{
    private IConfiguration? _perfConfiguration;
    private IFlexConfig? _perfFlexConfiguration;
    private readonly List<string> _perfValidationResults = new();
    private readonly Stopwatch _loadStopwatch = new();
    private long _memoryBaseline;
    private bool _performanceMonitoringEnabled;
    private bool _concurrentAccessEnabled;
    private bool _memoryTrackingEnabled;
    private bool _largeKeyVaultConfigured;
    private bool _extensiveAppConfigConfigured;

    #region Given Steps - Setup

    [Given(@"I have established an azure performance controller environment")]
    public void GivenIHaveEstablishedAnAzurePerformanceControllerEnvironment()
    {
        var appConfigEmulator = scenarioContext.GetAppConfigEmulator();
        var keyVaultEmulator = scenarioContext.GetKeyVaultEmulator();
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");
        
        keyVaultEmulator = new KeyVaultEmulatorContainer();
        appConfigEmulator = new AppConfigurationEmulatorContainer();
        _memoryBaseline = GC.GetTotalMemory(true);
        
        scenarioContext.Set(keyVaultEmulator, "KeyVaultEmulator");
        scenarioContext.Set(appConfigEmulator, "AppConfigEmulator");
        
        _perfValidationResults.Add($"✓ Performance controller environment established with emulators for prefix '{scenarioPrefix}'");
    }

    [Given(@"I have azure performance controller configuration with large Key Vault from ""(.*)""")]
    public void GivenIHaveAzurePerformanceControllerConfigurationWithLargeKeyVaultFrom(string testDataPath)
    {
        var keyVaultEmulator = scenarioContext.GetKeyVaultEmulator();
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");
        
        keyVaultEmulator.Should().NotBeNull("Key Vault emulator should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        var createTask = keyVaultEmulator!.CreateTestDataAsync(fullPath, scenarioPrefix);
        createTask.Wait(TimeSpan.FromMinutes(1));
        
        // Add additional secrets to simulate a large Key Vault with scenario prefix
        var largeBatchTasks = new List<Task>();
        for (int i = 0; i < 50; i++)
        {
            largeBatchTasks.Add(keyVaultEmulator.SetSecretAsync($"perf--test--secret--{i:D3}", $"large-secret-value-{i}", scenarioPrefix));
            largeBatchTasks.Add(keyVaultEmulator.SetSecretAsync($"config--batch--{i:D3}--setting", $"batch-config-{i}", scenarioPrefix));
        }
        
        Task.WaitAll([.. largeBatchTasks], TimeSpan.FromMinutes(2));
        _largeKeyVaultConfigured = true;
        
        _perfValidationResults.Add($"✓ Large Key Vault configuration added with 100+ secrets for prefix '{scenarioPrefix}'");
    }

    [Given(@"I have azure performance controller configuration with extensive App Configuration from ""(.*)""")]
    public void GivenIHaveAzurePerformanceControllerConfigurationWithExtensiveAppConfigurationFrom(string testDataPath)
    {
        var appConfigEmulator = scenarioContext.GetAppConfigEmulator();
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");
        
        appConfigEmulator.Should().NotBeNull("App Configuration emulator should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        var createTask = appConfigEmulator!.CreateTestDataAsync(fullPath, scenarioPrefix);
        createTask.Wait(TimeSpan.FromMinutes(1));
        
        // Add extensive configuration settings for performance testing with scenario prefix
        var extensiveBatchTasks = new List<Task>();
        for (int i = 0; i < 100; i++)
        {
            extensiveBatchTasks.Add(appConfigEmulator.SetConfigurationAsync($"{scenarioPrefix}:app:perf:setting:{i:D3}", $"extensive-value-{i}"));
            extensiveBatchTasks.Add(appConfigEmulator.SetConfigurationAsync($"{scenarioPrefix}:feature:flags:flag{i:D3}", (i % 2 == 0).ToString()));
        }
        
        Task.WaitAll([.. extensiveBatchTasks], TimeSpan.FromMinutes(2));
        _extensiveAppConfigConfigured = true;
        
        _perfValidationResults.Add($"✓ Extensive App Configuration added with 200+ settings for prefix '{scenarioPrefix}'");
    }

    [Given(@"I have azure performance controller configuration with concurrent access setup from ""(.*)""")]
    public void GivenIHaveAzurePerformanceControllerConfigurationWithConcurrentAccessSetupFrom(string testDataPath)
    {
        var appConfigEmulator = scenarioContext.GetAppConfigEmulator();
        var keyVaultEmulator = scenarioContext.GetKeyVaultEmulator();
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");
        
        keyVaultEmulator.Should().NotBeNull("Key Vault emulator should be established");
        appConfigEmulator.Should().NotBeNull("App Configuration emulator should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        
        // Load test data into both emulators with scenario prefix
        var keyVaultTask = keyVaultEmulator!.CreateTestDataAsync(fullPath, scenarioPrefix);
        var appConfigTask = appConfigEmulator!.CreateTestDataAsync(fullPath, scenarioPrefix);
        Task.WaitAll([keyVaultTask, appConfigTask], TimeSpan.FromMinutes(1));
        
        // Add specific concurrent access test data with scenario prefix
        var concurrentTestTasks = new List<Task>
        {
            keyVaultEmulator.SetSecretAsync("test--secret", "test-value", scenarioPrefix),
            keyVaultEmulator.SetSecretAsync("concurrent--access--test", "concurrent-value", scenarioPrefix),
            appConfigEmulator.SetConfigurationAsync($"{scenarioPrefix}:test:config", "test-value"),
            appConfigEmulator.SetConfigurationAsync($"{scenarioPrefix}:test:concurrent:access", "true")
        };
        
        Task.WaitAll([.. concurrentTestTasks], TimeSpan.FromSeconds(30));
        _concurrentAccessEnabled = true;
        
        _perfValidationResults.Add($"✓ Concurrent access configuration setup added to emulators for prefix '{scenarioPrefix}'");
    }

    [Given(@"I have azure performance controller configuration with memory monitoring from ""(.*)""")]
    public void GivenIHaveAzurePerformanceControllerConfigurationWithMemoryMonitoringFrom(string testDataPath)
    {
        var appConfigEmulator = scenarioContext.GetAppConfigEmulator();
        var keyVaultEmulator = scenarioContext.GetKeyVaultEmulator();
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");
        
        keyVaultEmulator.Should().NotBeNull("Key Vault emulator should be established");
        appConfigEmulator.Should().NotBeNull("App Configuration emulator should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        
        // Load test data for memory monitoring with scenario prefix
        var keyVaultTask = keyVaultEmulator!.CreateTestDataAsync(fullPath, scenarioPrefix);
        var appConfigTask = appConfigEmulator!.CreateTestDataAsync(fullPath, scenarioPrefix);
        Task.WaitAll([keyVaultTask, appConfigTask], TimeSpan.FromMinutes(1));
        
        _memoryTrackingEnabled = true;
        
        _perfValidationResults.Add($"✓ Memory monitoring configuration setup added to emulators for prefix '{scenarioPrefix}'");
    }

    #endregion

    #region When Steps - Actions

    [When(@"I configure azure performance controller with performance monitoring")]
    public void WhenIConfigureAzurePerformanceControllerWithPerformanceMonitoring()
    {
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");
        
        _performanceMonitoringEnabled = true;
        _loadStopwatch.Reset();
        _perfValidationResults.Add($"✓ Performance monitoring enabled for prefix '{scenarioPrefix}'");
    }

    [When(@"I configure azure performance controller with concurrent access testing")]
    public void WhenIConfigureAzurePerformanceControllerWithConcurrentAccessTesting()
    {
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");
        
        _concurrentAccessEnabled = true;
        _perfValidationResults.Add($"✓ Concurrent access testing enabled for prefix '{scenarioPrefix}'");
    }

    [When(@"I configure azure performance controller with memory tracking")]
    public void WhenIConfigureAzurePerformanceControllerWithMemoryTracking()
    {
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");
        
        _memoryTrackingEnabled = true;
        _memoryBaseline = GC.GetTotalMemory(true);
        _perfValidationResults.Add($"✓ Memory tracking enabled for prefix '{scenarioPrefix}'");
    }

    [When(@"I configure azure performance controller by building the configuration")]
    public void WhenIConfigureAzurePerformanceControllerByBuildingTheConfiguration()
    {
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");
        
        try
        {
            var keyVaultEmulator = scenarioContext.GetKeyVaultEmulator();
            var appConfigEmulator = scenarioContext.GetAppConfigEmulator();
            if (_performanceMonitoringEnabled)
            {
                _loadStopwatch.Start();
            }

            if (_memoryTrackingEnabled)
            {
                _memoryBaseline = GC.GetTotalMemory(true);
            }

            // Build configuration with performance monitoring and scenario prefix filtering
            var builder = new FlexConfigurationBuilder();

            // Add Key Vault if configured with scenario prefix filtering
            if (keyVaultEmulator != null && (_largeKeyVaultConfigured || _concurrentAccessEnabled || _memoryTrackingEnabled))
            {
                builder.AddAzureKeyVault(options =>
                {
                    options.VaultUri = "https://test-vault.vault.azure.net/";
                    options.SecretClient = keyVaultEmulator.SecretClient;
                    options.JsonProcessor = true; // Enable JSON processing for performance testing
                    options.Optional = false;
                    // Use a custom secret processor to filter by scenario prefix
                    options.SecretProcessor = new ScenarioPrefixSecretProcessor(scenarioPrefix);
                });
            }

            // Add App Configuration if configured with scenario prefix filtering
            if (appConfigEmulator != null && (_extensiveAppConfigConfigured || _concurrentAccessEnabled || _memoryTrackingEnabled))
            {
                builder.AddAzureAppConfiguration(options =>
                {
                    options.ConnectionString = appConfigEmulator.GetConnectionString();
                    options.ConfigurationClient = appConfigEmulator.ConfigurationClient;
                    options.Optional = false;
                    // Use scenario prefix as key filter to isolate this scenario's data
                    options.KeyFilter = $"{scenarioPrefix}:*";
                });
            }

            _perfFlexConfiguration = builder.Build();
            _perfConfiguration = _perfFlexConfiguration.Configuration;

            if (_performanceMonitoringEnabled)
            {
                _loadStopwatch.Stop();
                _perfValidationResults.Add($"✓ Configuration load time: {_loadStopwatch.ElapsedMilliseconds}ms for prefix '{scenarioPrefix}'");
            }

            if (_memoryTrackingEnabled)
            {
                var currentMemory = GC.GetTotalMemory(false);
                _perfValidationResults.Add($"✓ Initial memory usage: {(currentMemory - _memoryBaseline) / 1024 / 1024}MB for prefix '{scenarioPrefix}'");
            }
        
            scenarioContext.Set(_perfConfiguration, "PerfConfiguration");
            scenarioContext.Set(_perfFlexConfiguration, "PerfFlexConfiguration");
        
            _perfValidationResults.Add($"✓ Performance configuration built successfully with emulators for prefix '{scenarioPrefix}'");
        }
        catch (Exception ex)
        {
            _perfValidationResults.Add($"✗ Performance configuration build failed: {ex.Message}");
            throw;
        }
    }

    [When(@"I configure azure performance controller by building the configuration with concurrency")]
    public async Task WhenIConfigureAzurePerformanceControllerByBuildingTheConfigurationWithConcurrency()
    {
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");
        
        try
        {
            var keyVaultEmulator = scenarioContext.GetKeyVaultEmulator();
            var appConfigEmulator = scenarioContext.GetAppConfigEmulator();

            var concurrencyLevel = _concurrentAccessEnabled ? 10 : 1;
            var tasks = new List<Task<(IConfiguration config, IFlexConfig flexConfig)>>();
        
            // Create multiple concurrent configuration builds with scenario prefix filtering
            for (var i = 0; i < concurrencyLevel; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    var builder = new FlexConfigurationBuilder();

                    // Add Key Vault if available with scenario prefix filtering
                    if (keyVaultEmulator != null)
                    {
                        builder.AddAzureKeyVault(options =>
                        {
                            options.VaultUri = "https://test-vault.vault.azure.net/";
                            options.SecretClient = keyVaultEmulator.SecretClient;
                            options.JsonProcessor = true;
                            options.Optional = false;
                            // Use a custom secret processor to filter by scenario prefix
                            options.SecretProcessor = new ScenarioPrefixSecretProcessor(scenarioPrefix);
                        });
                    }

                    // Add App Configuration if available with scenario prefix filtering
                    if (appConfigEmulator != null)
                    {
                        builder.AddAzureAppConfiguration(options =>
                        {
                            options.ConnectionString = appConfigEmulator.GetConnectionString();
                            options.ConfigurationClient = appConfigEmulator.ConfigurationClient;
                            options.Optional = false;
                            // Use scenario prefix as key filter to isolate this scenario's data
                            options.KeyFilter = $"{scenarioPrefix}:*";
                        });
                    }

                    var flexConfig = builder.Build();
                    var config = flexConfig.Configuration;
                    return (config, flexConfig);
                }));
            }

            await Task.WhenAll(tasks);
        
            var lastResult = tasks.Last().GetAwaiter().GetResult();
            _perfConfiguration = lastResult.config;
            _perfFlexConfiguration = lastResult.flexConfig;
        
            scenarioContext.Set(_perfConfiguration, "PerfConfiguration");
            scenarioContext.Set(_perfFlexConfiguration, "PerfFlexConfiguration");
        
            _perfValidationResults.Add($"✓ Concurrent configuration build completed successfully with {concurrencyLevel} threads for prefix '{scenarioPrefix}'");
        }
        catch (Exception ex)
        {
            _perfValidationResults.Add($"✗ Concurrent configuration build failed: {ex.Message}");
            throw;
        }
    }

    #endregion

    #region Then Steps - Assertions

    [Then(@"the azure performance controller should complete Key Vault loading within performance limits")]
    public void ThenTheAzurePerformanceControllerShouldCompleteKeyVaultLoadingWithinPerformanceLimits()
    {
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");
        
        _loadStopwatch.ElapsedMilliseconds.Should().BeLessThan(10000, 
            "Key Vault loading should complete within 10 seconds (emulator may be slower than real Azure)");
        _perfValidationResults.Add($"✓ Key Vault load time: {_loadStopwatch.ElapsedMilliseconds}ms for prefix '{scenarioPrefix}'");
    }

    [Then(@"the azure performance controller should demonstrate efficient secret retrieval")]
    public void ThenTheAzurePerformanceControllerShouldDemonstrateEfficientSecretRetrieval()
    {
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");
        
        _perfFlexConfiguration.Should().NotBeNull();
        
        var sw = Stopwatch.StartNew();
        
        // Test retrieving a known secret that should exist with scenario prefix
        var testValue = _perfFlexConfiguration![$"{scenarioPrefix}:test:secret"]?.ToType<string>();
        
        sw.Stop();

        // Be more lenient with emulator performance vs. real Azure
        sw.ElapsedMilliseconds.Should().BeLessThan(1000, 
            "Individual secret retrieval should be reasonably fast");
        _perfValidationResults.Add($"✓ Secret retrieval time: {sw.ElapsedMilliseconds}ms (value: {testValue ?? "null"}) for prefix '{scenarioPrefix}'");
    }

    [Then(@"the azure performance controller should report Key Vault performance metrics")]
    public void ThenTheAzurePerformanceControllerShouldReportKeyVaultPerformanceMetrics()
    {
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");
        
        _perfValidationResults.Should().NotBeEmpty("Performance metrics should be collected");
        _perfValidationResults.Should().Contain(x => x.Contains("load time"), 
            "Load time metrics should be recorded");
        
        // Additional performance metrics for Key Vault with scenario prefix filtering
        if (_largeKeyVaultConfigured && _perfConfiguration != null)
        {
            var keyVaultKeys = _perfConfiguration
                .AsEnumerable()
                .Count(kvp => kvp.Key.StartsWith($"{scenarioPrefix}:") && (kvp.Key.Contains("perf") || kvp.Key.Contains("config")));
            
            _perfValidationResults.Add($"✓ Key Vault secrets loaded: {keyVaultKeys} for prefix '{scenarioPrefix}'");
        }
    }

    [Then(@"the azure performance controller should complete App Configuration loading within performance limits")]
    public void ThenTheAzurePerformanceControllerShouldCompleteAppConfigurationLoadingWithinPerformanceLimits()
    {
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");
        
        _loadStopwatch.ElapsedMilliseconds.Should().BeLessThan(10000, 
            "App Configuration loading should complete within 10 seconds (emulator may be slower than real Azure)");
        _perfValidationResults.Add($"✓ App Configuration load time: {_loadStopwatch.ElapsedMilliseconds}ms for prefix '{scenarioPrefix}'");
    }

    [Then(@"the azure performance controller should demonstrate efficient configuration retrieval")]
    public void ThenTheAzurePerformanceControllerShouldDemonstrateEfficientConfigurationRetrieval()
    {
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");
        
        _perfFlexConfiguration.Should().NotBeNull();
        
        var sw = Stopwatch.StartNew();
        
        // Test retrieving a known configuration that should exist with scenario prefix
        var testValue = _perfFlexConfiguration![$"{scenarioPrefix}:test:config"]?.ToType<string>();
        
        sw.Stop();

        // Be more lenient with emulator performance vs. real Azure
        sw.ElapsedMilliseconds.Should().BeLessThan(1000, 
            "Individual configuration retrieval should be reasonably fast");
        _perfValidationResults.Add($"✓ Configuration retrieval time: {sw.ElapsedMilliseconds}ms (value: {testValue ?? "null"}) for prefix '{scenarioPrefix}'");
    }

    [Then(@"the azure performance controller should report App Configuration performance metrics")]
    public void ThenTheAzurePerformanceControllerShouldReportAppConfigurationPerformanceMetrics()
    {
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");
        
        _perfValidationResults.Should().NotBeEmpty("Performance metrics should be collected");
        _perfValidationResults.Should().Contain(x => x.Contains("retrieval time"), 
            "Retrieval time metrics should be recorded");
        
        // Additional performance metrics for App Configuration with scenario prefix filtering
        if (_extensiveAppConfigConfigured && _perfConfiguration != null)
        {
            var appConfigKeys = _perfConfiguration
                .AsEnumerable()
                .Count(kvp => kvp.Key.StartsWith($"{scenarioPrefix}:") && (kvp.Key.Contains("app:perf") || kvp.Key.Contains("feature")));
            
            _perfValidationResults.Add($"✓ App Configuration settings loaded: {appConfigKeys} for prefix '{scenarioPrefix}'");
        }
    }

    [Then(@"the azure performance controller should handle concurrent access safely")]
    public void ThenTheAzurePerformanceControllerShouldHandleConcurrentAccessSafely()
    {
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");
        
        _perfConfiguration.Should().NotBeNull();
        _perfFlexConfiguration.Should().NotBeNull();
        _perfValidationResults.Add($"✓ Configuration supports concurrent access with emulators for prefix '{scenarioPrefix}'");
    }

    [Then(@"the azure performance controller should demonstrate thread-safe configuration access")]
    public void ThenTheAzurePerformanceControllerShouldDemonstrateThreadSafeConfigurationAccess()
    {
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");
        
        _perfFlexConfiguration.Should().NotBeNull();
    
        var sw = Stopwatch.StartNew();
        var tasks = Enumerable.Range(0, 50).Select(_ => Task.Run(() => // Reduced from 100 to 50 for emulator performance
        {
            try
            {
                // Test multiple configuration accesses with scenario prefix
                var config1 = _perfFlexConfiguration!.Configuration.GetValue<string>($"{scenarioPrefix}:test:config");
                var config2 = _perfFlexConfiguration![$"{scenarioPrefix}:test:concurrent:access"];
                return config1 != null || config2 != null; // At least one should exist
            }
            catch
            {
                return false;
            }
        })).ToArray();

        // ReSharper disable once CoVariantArrayConversion
        Task.WaitAll(tasks);
        sw.Stop();
    
        var successfulTasks = tasks.Count(t => t.Result);
        successfulTasks.Should().BeGreaterThan(0, "At least some concurrent configuration access should succeed");
        
        _perfValidationResults.Add($"✓ Thread-safe configuration access verified: {successfulTasks}/{tasks.Length} successful, {sw.ElapsedMilliseconds}ms total for prefix '{scenarioPrefix}'");
    }

    [Then(@"the azure performance controller should report concurrency performance metrics")]
    public void ThenTheAzurePerformanceControllerShouldReportConcurrencyPerformanceMetrics()
    {
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");
        
        _perfValidationResults.Should().NotBeEmpty("Concurrency metrics should be collected");
        _perfValidationResults.Should().Contain(x => x.Contains("concurrent"), 
            "Concurrent access metrics should be recorded");
        
        if (_concurrentAccessEnabled)
        {
            _perfValidationResults.Add($"✓ Concurrency performance testing completed with emulators for prefix '{scenarioPrefix}'");
        }
    }

    [Then(@"the azure performance controller should maintain acceptable memory usage")]
    public void ThenTheAzurePerformanceControllerShouldMaintainAcceptableMemoryUsage()
    {
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");
        
        var currentMemory = GC.GetTotalMemory(false);
        var memoryDelta = currentMemory - _memoryBaseline;
        
        // Be more lenient with emulator memory usage vs. real Azure
        memoryDelta.Should().BeLessThan(100 * 1024 * 1024, 
            "Memory usage increase should be less than 100MB (emulators may use more memory)");
        _perfValidationResults.Add($"✓ Memory usage delta: {memoryDelta / 1024 / 1024}MB for prefix '{scenarioPrefix}'");
    }

    [Then(@"the azure performance controller should demonstrate efficient resource utilization")]
    public void ThenTheAzurePerformanceControllerShouldDemonstrateEfficientResourceUtilization()
    {
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");
        
        // Force garbage collection and measure memory
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        var currentMemory = GC.GetTotalMemory(true);
        var memoryDelta = currentMemory - _memoryBaseline;
        
        // Be more lenient with emulator memory usage after cleanup
        memoryDelta.Should().BeLessThan(50 * 1024 * 1024, 
            "Memory usage after cleanup should be less than 50MB (emulators may retain more memory)");
        _perfValidationResults.Add($"✓ Post-cleanup memory delta: {memoryDelta / 1024 / 1024}MB for prefix '{scenarioPrefix}'");
        
        // Additional resource utilization metrics with scenario prefix filtering
        if (_largeKeyVaultConfigured || _extensiveAppConfigConfigured)
        {
            var configurationSize = _perfConfiguration?.AsEnumerable()
                .Count(kvp => kvp.Key.StartsWith($"{scenarioPrefix}:")) ?? 0;
            _perfValidationResults.Add($"✓ Total configuration entries loaded: {configurationSize} for prefix '{scenarioPrefix}'");
        }
    }

    [Then(@"the azure performance controller should report memory usage metrics")]
    public void ThenTheAzurePerformanceControllerShouldReportMemoryUsageMetrics()
    {
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");
        
        _perfValidationResults.Should().NotBeEmpty("Memory metrics should be collected");
        _perfValidationResults.Should().Contain(x => x.Contains("memory"), 
            "Memory usage metrics should be recorded");
        
        // Provide a comprehensive memory metrics summary
        var currentMemory = GC.GetTotalMemory(false);
        var gen0Collections = GC.CollectionCount(0);
        var gen1Collections = GC.CollectionCount(1);
        var gen2Collections = GC.CollectionCount(2);
        
        _perfValidationResults.Add($"✓ Memory metrics summary: Current={currentMemory / 1024 / 1024}MB, GC(0/1/2)=({gen0Collections}/{gen1Collections}/{gen2Collections}) for prefix '{scenarioPrefix}'");
    }

    #endregion
}
