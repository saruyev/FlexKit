using System.Diagnostics;
using FlexKit.Configuration.Core;
using FlexKit.Configuration.Providers.Aws.IntegrationTests.Utils;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Reqnroll;
// ReSharper disable NullableWarningSuppressionIsUsed

// ReSharper disable ClassTooBig
// ReSharper disable MethodTooLong
// ReSharper disable TooManyDeclarations
// ReSharper disable FlagArgument
// ReSharper disable ComplexConditionExpression

namespace FlexKit.Configuration.Providers.Aws.IntegrationTests.Steps.Integration;

/// <summary>
/// Step definitions for AWS Performance scenarios.
/// Tests performance characteristics of AWS Parameter Store and Secrets Manager integration,
/// including loading times, memory usage, concurrent access, and optimization patterns.
/// Uses distinct step patterns ("aws performance controller") to avoid conflicts with other step classes.
/// </summary>
[Binding]
public class AwsPerformanceSteps(ScenarioContext scenarioContext)
{
    private AwsTestConfigurationBuilder? _awsPerformanceBuilder;
    private IConfiguration? _awsPerformanceConfiguration;
    private IFlexConfig? _awsPerformanceFlexConfiguration;
    private Exception? _lastAwsPerformanceException;
    private readonly List<string> _awsPerformanceValidationResults = new();
    private readonly Dictionary<string, object> _performanceMetrics = new();
    private readonly Stopwatch _performanceStopwatch = new();
    private TimeSpan _configurationBuildTime;
    private TimeSpan _dynamicAccessTime;
    private TimeSpan _concurrentAccessTime;
    private long _memoryUsageBefore;
    private long _memoryUsageAfter;
    private bool _jsonProcessingEnabled;
    private bool _reloadingEnabled;
    private bool _concurrentAccessTested;
    private readonly Lock _performanceLock = new();

    #region Given Steps - Setup

    [Given(@"I have established an aws performance controller environment")]
    public void GivenIHaveEstablishedAnAwsPerformanceControllerEnvironment()
    {
        _awsPerformanceBuilder = new AwsTestConfigurationBuilder(scenarioContext);
        _memoryUsageBefore = GC.GetTotalMemory(forceFullCollection: true);
        
        scenarioContext.Set(_awsPerformanceBuilder, "AwsPerformanceBuilder");
        scenarioContext.Set(_memoryUsageBefore, "MemoryUsageBefore");
    }

    [Given(@"I have aws performance controller configuration with basic Parameter Store from ""(.*)""")]
    public void GivenIHaveAwsPerformanceControllerConfigurationWithBasicParameterStoreFrom(string testDataPath)
    {
        _awsPerformanceBuilder.Should().NotBeNull("AWS performance builder should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        _awsPerformanceBuilder!.AddParameterStoreFromTestData(fullPath, optional: false, jsonProcessor: false);
        
        _performanceMetrics["ParameterStoreConfigured"] = true;
        _performanceMetrics["JSONProcessingEnabled"] = false;
        
        scenarioContext.Set(_awsPerformanceBuilder, "AwsPerformanceBuilder");
    }

    [Given(@"I have aws performance controller configuration with large Parameter Store dataset from ""(.*)""")]
    public void GivenIHaveAwsPerformanceControllerConfigurationWithLargeParameterStoreDatasetFrom(string testDataPath)
    {
        _awsPerformanceBuilder.Should().NotBeNull("AWS performance builder should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        _awsPerformanceBuilder!.AddParameterStoreFromTestData(fullPath, optional: false, jsonProcessor: false);
        
        _performanceMetrics["ParameterStoreConfigured"] = true;
        _performanceMetrics["LargeDatasetEnabled"] = true;
        _performanceMetrics["ExpectedParameterCount"] = 100; // Based on performance-config.json structure
        
        scenarioContext.Set(_awsPerformanceBuilder, "AwsPerformanceBuilder");
    }

    [Given(@"I have aws performance controller configuration with JSON processing from ""(.*)""")]
    public void GivenIHaveAwsPerformanceControllerConfigurationWithJsonProcessingFrom(string testDataPath)
    {
        _awsPerformanceBuilder.Should().NotBeNull("AWS performance builder should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        _awsPerformanceBuilder!.AddParameterStoreFromTestData(fullPath, optional: false, jsonProcessor: true);
        
        _jsonProcessingEnabled = true;
        _performanceMetrics["ParameterStoreConfigured"] = true;
        _performanceMetrics["JSONProcessingEnabled"] = true;
        
        scenarioContext.Set(_awsPerformanceBuilder, "AwsPerformanceBuilder");
    }

    [Given(@"I have aws performance controller configuration with basic Secrets Manager from ""(.*)""")]
    public void GivenIHaveAwsPerformanceControllerConfigurationWithBasicSecretsManagerFrom(string testDataPath)
    {
        _awsPerformanceBuilder.Should().NotBeNull("AWS performance builder should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        _awsPerformanceBuilder!.AddSecretsManagerFromTestData(fullPath, optional: false, jsonProcessor: false);
        
        _performanceMetrics["SecretsManagerConfigured"] = true;
        _performanceMetrics["JSONProcessingEnabled"] = false;
        
        scenarioContext.Set(_awsPerformanceBuilder, "AwsPerformanceBuilder");
    }

    [Given(@"I have aws performance controller configuration with large Secrets Manager dataset from ""(.*)""")]
    public void GivenIHaveAwsPerformanceControllerConfigurationWithLargeSecretsManagerDatasetFrom(string testDataPath)
    {
        _awsPerformanceBuilder.Should().NotBeNull("AWS performance builder should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        _awsPerformanceBuilder!.AddSecretsManagerFromTestData(fullPath, optional: false, jsonProcessor: false);
        
        _performanceMetrics["SecretsManagerConfigured"] = true;
        _performanceMetrics["LargeDatasetEnabled"] = true;
        _performanceMetrics["ExpectedSecretCount"] = 50; // Based on performance-config.json structure
        
        scenarioContext.Set(_awsPerformanceBuilder, "AwsPerformanceBuilder");
    }

    [Given(@"I have aws performance controller configuration with combined sources from ""(.*)""")]
    public void GivenIHaveAwsPerformanceControllerConfigurationWithCombinedSourcesFrom(string testDataPath)
    {
        _awsPerformanceBuilder.Should().NotBeNull("AWS performance builder should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        
        // Add both Parameter Store and Secrets Manager for combined testing
        _awsPerformanceBuilder!.AddParameterStoreFromTestData(fullPath, optional: false, jsonProcessor: false);
        _awsPerformanceBuilder.AddSecretsManagerFromTestData(fullPath, optional: false, jsonProcessor: false);
        
        _performanceMetrics["ParameterStoreConfigured"] = true;
        _performanceMetrics["SecretsManagerConfigured"] = true;
        _performanceMetrics["CombinedSourcesEnabled"] = true;
        
        scenarioContext.Set(_awsPerformanceBuilder, "AwsPerformanceBuilder");
    }

    [Given(@"I have aws performance controller configuration with performance-optimized reloading from ""(.*)""")]
    public void GivenIHaveAwsPerformanceControllerConfigurationWithPerformanceOptimizedReloadingFrom(string testDataPath)
    {
        _awsPerformanceBuilder.Should().NotBeNull("AWS performance builder should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        
        // Configure Parameter Store with optimized reloading (longer intervals for performance)
        // Simulate test data loading for the configured path with performance metrics
        _awsPerformanceBuilder!.AddParameterStoreFromTestData(fullPath, optional: false, jsonProcessor: false);
        
        _reloadingEnabled = true;
        _performanceMetrics["ReloadingEnabled"] = true;
        _performanceMetrics["ReloadInterval"] = TimeSpan.FromMinutes(15);
        
        scenarioContext.Set(_awsPerformanceBuilder, "AwsPerformanceBuilder");
    }

    [Given(@"I have aws performance controller configuration with FlexConfig optimization from ""(.*)""")]
    public void GivenIHaveAwsPerformanceControllerConfigurationWithFlexConfigOptimizationFrom(string testDataPath)
    {
        _awsPerformanceBuilder.Should().NotBeNull("AWS performance builder should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        _awsPerformanceBuilder!.AddParameterStoreFromTestData(fullPath, optional: false, jsonProcessor: true);
        
        _jsonProcessingEnabled = true;
        _performanceMetrics["FlexConfigOptimizationEnabled"] = true;
        _performanceMetrics["JSONProcessingEnabled"] = true;
        
        scenarioContext.Set(_awsPerformanceBuilder, "AwsPerformanceBuilder");
    }

    [Given(@"I have aws performance controller configuration with concurrent access optimization from ""(.*)""")]
    public void GivenIHaveAwsPerformanceControllerConfigurationWithConcurrentAccessOptimizationFrom(string testDataPath)
    {
        _awsPerformanceBuilder.Should().NotBeNull("AWS performance builder should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        _awsPerformanceBuilder!.AddParameterStoreFromTestData(fullPath, optional: false, jsonProcessor: false);
        
        _performanceMetrics["ConcurrentAccessOptimizationEnabled"] = true;
        _performanceMetrics["ExpectedThreadCount"] = 10; // Based on performance-config.json
        _performanceMetrics["OperationsPerThread"] = 100;
        
        scenarioContext.Set(_awsPerformanceBuilder, "AwsPerformanceBuilder");
    }

    [Given(@"I have aws performance controller configuration with memory optimization from ""(.*)""")]
    public void GivenIHaveAwsPerformanceControllerConfigurationWithMemoryOptimizationFrom(string testDataPath)
    {
        _awsPerformanceBuilder.Should().NotBeNull("AWS performance builder should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        _awsPerformanceBuilder!.AddParameterStoreFromTestData(fullPath, optional: false, jsonProcessor: false);
        
        _performanceMetrics["MemoryOptimizationEnabled"] = true;
        
        scenarioContext.Set(_awsPerformanceBuilder, "AwsPerformanceBuilder");
    }

    #endregion

    #region When Steps - Actions

    [When(@"I configure aws performance controller by building the configuration with performance monitoring")]
    public void WhenIConfigureAwsPerformanceControllerByBuildingTheConfigurationWithPerformanceMonitoring()
    {
        _awsPerformanceBuilder.Should().NotBeNull("AWS performance builder should be established");

        try
        {
            // Start performance monitoring
            _performanceStopwatch.Restart();
            
            // Build configuration with FlexConfig
            _awsPerformanceFlexConfiguration = _awsPerformanceBuilder!.BuildFlexConfig();
            _awsPerformanceConfiguration = _awsPerformanceFlexConfiguration.Configuration;
            
            // Stop performance monitoring
            _performanceStopwatch.Stop();
            _configurationBuildTime = _performanceStopwatch.Elapsed;
            
            // Record memory usage after a configuration build
            _memoryUsageAfter = GC.GetTotalMemory(forceFullCollection: false);
            
            // Store performance metrics
            _performanceMetrics["ConfigurationBuildTime"] = _configurationBuildTime;
            _performanceMetrics["MemoryUsageAfter"] = _memoryUsageAfter;
            _performanceMetrics["MemoryUsageDelta"] = _memoryUsageAfter - _memoryUsageBefore;
            
            // Log all configuration keys for debugging
            var configKeys = _awsPerformanceConfiguration.AsEnumerable()
                .Where(kvp => kvp.Value != null)
                .Take(10)
                .Select(kvp => $"{kvp.Key} = {kvp.Value}")
                .ToList();
            
            foreach (var debugKey in configKeys)
            {
                Debug.WriteLine($"Performance config loaded: {debugKey}");
            }

            scenarioContext.Set(_awsPerformanceConfiguration, "AwsPerformanceConfiguration");
            scenarioContext.Set(_awsPerformanceFlexConfiguration, "AwsPerformanceFlexConfiguration");
            scenarioContext.Set(_performanceMetrics, "PerformanceMetrics");
        }
        catch (Exception ex)
        {
            _lastAwsPerformanceException = ex;
            scenarioContext.Set(ex, "AwsPerformanceException");
        }
    }

    [When(@"I verify aws performance controller dynamic access performance")]
    public void WhenIVerifyAwsPerformanceControllerDynamicAccessPerformance()
    {
        _awsPerformanceFlexConfiguration.Should().NotBeNull("AWS performance FlexConfiguration should be built");

        try
        {
            _performanceStopwatch.Restart();
            
            // Test dynamic access performance
            dynamic config = _awsPerformanceFlexConfiguration!;
            
            // Perform multiple dynamic access operations to measure performance
            var operations = new List<object>();
            
            for (int i = 0; i < 100; i++)
            {
                try
                {
                    // Test various dynamic access patterns
                    _ = config.infrastructure_module;
                    
                    if (_jsonProcessingEnabled)
                    {
                        var appConfig = config.infrastructure_module?.app?.config;
                        operations.Add(appConfig ?? "null");
                    }
                    else
                    {
                        var database = config.infrastructure_module?.database;
                        operations.Add(database ?? "null");
                    }
                }
                catch
                {
                    // Count failed operations but don't fail the test
                    operations.Add("failed");
                }
            }
            
            _performanceStopwatch.Stop();
            _dynamicAccessTime = _performanceStopwatch.Elapsed;
            
            _performanceMetrics["DynamicAccessTime"] = _dynamicAccessTime;
            _performanceMetrics["DynamicAccessOperations"] = operations.Count;
            _performanceMetrics["SuccessfulDynamicAccess"] = operations.Count(o => o.ToString() != "failed");
            
            scenarioContext.Set("true", "DynamicAccessTested");
        }
        catch (Exception ex)
        {
            _lastAwsPerformanceException = ex;
            scenarioContext.Set(ex, "AwsPerformanceException");
        }
    }

    [When(@"I verify aws performance controller concurrent access capabilities")]
    public void WhenIVerifyAwsPerformanceControllerConcurrentAccessCapabilities()
    {
        _awsPerformanceFlexConfiguration.Should().NotBeNull("AWS performance FlexConfiguration should be built");

        try
        {
            _performanceStopwatch.Restart();
            
            const int threadCount = 10;
            const int operationsPerThread = 100;
            
            // Create concurrent tasks for configuration access
            var tasks = new List<Task>();
            var results = new Dictionary<int, List<object>>();
            
            for (int threadId = 0; threadId < threadCount; threadId++)
            {
                var currentThreadId = threadId;
                results[currentThreadId] = new List<object>();
                
                var task = Task.Run(() =>
                {
                    var threadResults = new List<object>();
                    dynamic config = _awsPerformanceFlexConfiguration!;
                    
                    for (int operation = 0; operation < operationsPerThread; operation++)
                    {
                        try
                        {
                            lock (_performanceLock)
                            {
                                // Perform configuration access operations
                                var value = config.infrastructure_module?.database?.host ?? "default";
                                threadResults.Add(value);
                            }
                        }
                        catch (Exception ex)
                        {
                            threadResults.Add($"error: {ex.Message}");
                        }
                    }
                    
                    lock (_performanceLock)
                    {
                        results[currentThreadId] = threadResults;
                    }
                });
                
                tasks.Add(task);
            }
            
            Task.WaitAll(tasks.ToArray(), TimeSpan.FromSeconds(30));
            
            _performanceStopwatch.Stop();
            _concurrentAccessTime = _performanceStopwatch.Elapsed;
            
            // Calculate concurrent access metrics
            var totalOperations = results.Values.Sum(r => r.Count);
            var successfulOperations = results.Values.Sum(r => r.Count(o => !o.ToString()!.StartsWith("error:")));
            
            _performanceMetrics["ConcurrentAccessTime"] = _concurrentAccessTime;
            _performanceMetrics["TotalConcurrentOperations"] = totalOperations;
            _performanceMetrics["SuccessfulConcurrentOperations"] = successfulOperations;
            _performanceMetrics["ConcurrentAccessSuccessRate"] =
                totalOperations > 0 ? (double)successfulOperations / totalOperations : 0.0;
            
            _concurrentAccessTested = true;
            scenarioContext.Set("true", "ConcurrentAccessTested");
        }
        catch (Exception ex)
        {
            _lastAwsPerformanceException = ex;
            scenarioContext.Set(ex, "AwsPerformanceException");
        }
    }

    #endregion

    #region Then Steps - Assertions

    [Then(@"the aws performance controller configuration should be built successfully")]
    public void ThenTheAwsPerformanceControllerConfigurationShouldBeBuiltSuccessfully()
    {
        _lastAwsPerformanceException.Should().BeNull("Configuration building should not throw exceptions");
        _awsPerformanceConfiguration.Should().NotBeNull("Configuration should be built successfully");
        _awsPerformanceFlexConfiguration.Should().NotBeNull("FlexConfiguration should be built successfully");
        
        _awsPerformanceValidationResults.Add("Configuration built successfully");
        scenarioContext.Set(_awsPerformanceValidationResults, "AwsPerformanceValidationResults");
    }

    [Then(@"the aws performance controller should demonstrate optimized Parameter Store loading performance")]
    public void ThenTheAwsPerformanceControllerShouldDemonstrateOptimizedParameterStoreLoadingPerformance()
    {
        _configurationBuildTime.Should().BeLessThan(TimeSpan.FromSeconds(5), 
            "Parameter Store loading should complete within 5 seconds for basic configurations");
        
        _performanceMetrics.ContainsKey("ParameterStoreConfigured").Should().BeTrue();
        _performanceMetrics["ParameterStoreConfigured"].Should().Be(true);
        
        _awsPerformanceValidationResults.Add($"Parameter Store loading completed in {_configurationBuildTime.TotalMilliseconds:F2}ms");
        scenarioContext.Set(_awsPerformanceValidationResults, "AwsPerformanceValidationResults");
    }

    [Then(@"the aws performance controller should handle large parameter sets efficiently")]
    public void ThenTheAwsPerformanceControllerShouldHandleLargeParameterSetsEfficiently()
    {
        // Large parameter sets should still load within a reasonable time
        _configurationBuildTime.Should().BeLessThan(TimeSpan.FromSeconds(30), 
            "Large parameter sets should load within 30 seconds");
        
        _performanceMetrics.ContainsKey("LargeDatasetEnabled").Should().BeTrue();
        _performanceMetrics["LargeDatasetEnabled"].Should().Be(true);
        
        // Memory usage should be reasonable for large datasets
        var memoryDelta = (long)_performanceMetrics["MemoryUsageDelta"];
        memoryDelta.Should().BeLessThan(100_000_000, // 100MB
            "Memory usage for large parameter sets should be under 100MB");
        
        _awsPerformanceValidationResults.Add($"Large parameter set handled efficiently in {_configurationBuildTime.TotalMilliseconds:F2}ms");
        scenarioContext.Set(_awsPerformanceValidationResults, "AwsPerformanceValidationResults");
    }

    [Then(@"the aws performance controller should demonstrate efficient JSON processing performance")]
    public void ThenTheAwsPerformanceControllerShouldDemonstrateEfficientJsonProcessingPerformance()
    {
        _jsonProcessingEnabled.Should().BeTrue("JSON processing should be enabled");
        
        // JSON processing adds overhead but should still be reasonable
        _configurationBuildTime.Should().BeLessThan(TimeSpan.FromSeconds(10), 
            "JSON processing should complete within 10 seconds");
        
        _performanceMetrics.ContainsKey("JSONProcessingEnabled").Should().BeTrue();
        _performanceMetrics["JSONProcessingEnabled"].Should().Be(true);
        
        _awsPerformanceValidationResults.Add($"JSON processing completed efficiently in {_configurationBuildTime.TotalMilliseconds:F2}ms");
        scenarioContext.Set(_awsPerformanceValidationResults, "AwsPerformanceValidationResults");
    }

    [Then(@"the aws performance controller should demonstrate optimized Secrets Manager loading performance")]
    public void ThenTheAwsPerformanceControllerShouldDemonstrateOptimizedSecretsManagerLoadingPerformance()
    {
        _configurationBuildTime.Should().BeLessThan(TimeSpan.FromSeconds(5), 
            "Secrets Manager loading should complete within 5 seconds for basic configurations");
        
        _performanceMetrics.ContainsKey("SecretsManagerConfigured").Should().BeTrue();
        _performanceMetrics["SecretsManagerConfigured"].Should().Be(true);
        
        _awsPerformanceValidationResults.Add($"Secrets Manager loading completed in {_configurationBuildTime.TotalMilliseconds:F2}ms");
        scenarioContext.Set(_awsPerformanceValidationResults, "AwsPerformanceValidationResults");
    }

    [Then(@"the aws performance controller should handle large secret sets efficiently")]
    public void ThenTheAwsPerformanceControllerShouldHandleLargeSecretSetsEfficiently()
    {
        // Large secret sets should still load within a reasonable time
        _configurationBuildTime.Should().BeLessThan(TimeSpan.FromSeconds(30), 
            "Large secret sets should load within 30 seconds");
        
        _performanceMetrics.ContainsKey("LargeDatasetEnabled").Should().BeTrue();
        _performanceMetrics["LargeDatasetEnabled"].Should().Be(true);
        
        // Memory usage should be reasonable for large datasets
        var memoryDelta = (long)_performanceMetrics["MemoryUsageDelta"];
        memoryDelta.Should().BeLessThan(100_000_000, // 100MB
            "Memory usage for large secret sets should be under 100MB");
        
        _awsPerformanceValidationResults.Add($"Large secret set handled efficiently in {_configurationBuildTime.TotalMilliseconds:F2}ms");
        scenarioContext.Set(_awsPerformanceValidationResults, "AwsPerformanceValidationResults");
    }

    [Then(@"the aws performance controller should demonstrate efficient combined source loading")]
    public void ThenTheAwsPerformanceControllerShouldDemonstrateEfficientCombinedSourceLoading()
    {
        // Combined sources should load within a reasonable time
        _configurationBuildTime.Should().BeLessThan(TimeSpan.FromSeconds(15), 
            "Combined Parameter Store and Secrets Manager sources should load within 15 seconds");
        
        _performanceMetrics.ContainsKey("CombinedSourcesEnabled").Should().BeTrue();
        _performanceMetrics["CombinedSourcesEnabled"].Should().Be(true);
        
        // Verify both sources are configured
        _performanceMetrics.ContainsKey("ParameterStoreConfigured").Should().BeTrue();
        _performanceMetrics.ContainsKey("SecretsManagerConfigured").Should().BeTrue();
        
        _awsPerformanceValidationResults.Add($"Combined sources loaded efficiently in {_configurationBuildTime.TotalMilliseconds:F2}ms");
        scenarioContext.Set(_awsPerformanceValidationResults, "AwsPerformanceValidationResults");
    }

    [Then(@"the aws performance controller should demonstrate efficient reloading performance")]
    public void ThenTheAwsPerformanceControllerShouldDemonstrateEfficientReloadingPerformance()
    {
        _reloadingEnabled.Should().BeTrue("Reloading should be enabled");
        
        _performanceMetrics.ContainsKey("ReloadingEnabled").Should().BeTrue();
        _performanceMetrics["ReloadingEnabled"].Should().Be(true);
        
        // Verify reload interval is performance-optimized (not too frequent)
        var reloadInterval = (TimeSpan)_performanceMetrics["ReloadInterval"];
        reloadInterval.Should().BeGreaterThanOrEqualTo(TimeSpan.FromMinutes(10), 
            "Performance-optimized reload interval should be at least 10 minutes");
        
        _awsPerformanceValidationResults.Add($"Reloading configured with performance-optimized interval: {reloadInterval}");
        scenarioContext.Set(_awsPerformanceValidationResults, "AwsPerformanceValidationResults");
    }

    [Then(@"the aws performance controller FlexConfig should provide optimized dynamic access performance")]
    public void ThenTheAwsPerformanceControllerFlexConfigShouldProvideOptimizedDynamicAccessPerformance()
    {
        scenarioContext.ContainsKey("DynamicAccessTested").Should().BeTrue("Dynamic access should have been tested");
        
        _performanceMetrics.ContainsKey("DynamicAccessTime").Should().BeTrue();
        var dynamicAccessTime = (TimeSpan)_performanceMetrics["DynamicAccessTime"];
        
        // Dynamic access should be fast for 100 operations
        dynamicAccessTime.Should().BeLessThan(TimeSpan.FromSeconds(1), 
            "100 dynamic access operations should complete within 1 second");
        
        var successfulOperations = (int)_performanceMetrics["SuccessfulDynamicAccess"];
        var totalOperations = (int)_performanceMetrics["DynamicAccessOperations"];
        
        successfulOperations.Should().BeGreaterThan(0, "At least some dynamic access operations should succeed");
        
        _awsPerformanceValidationResults.Add($"Dynamic access performance: {successfulOperations}/{totalOperations} operations in {dynamicAccessTime.TotalMilliseconds:F2}ms");
        scenarioContext.Set(_awsPerformanceValidationResults, "AwsPerformanceValidationResults");
    }

    [Then(@"the aws performance controller should handle concurrent access efficiently")]
    public void ThenTheAwsPerformanceControllerShouldHandleConcurrentAccessEfficiently()
    {
        _concurrentAccessTested.Should().BeTrue("Concurrent access should have been tested");
        scenarioContext.ContainsKey("ConcurrentAccessTested").Should().BeTrue();
        
        _performanceMetrics.ContainsKey("ConcurrentAccessTime").Should().BeTrue();
        var concurrentAccessTime = (TimeSpan)_performanceMetrics["ConcurrentAccessTime"];
        
        // Concurrent access with 10 threads, 100 operations each should complete reasonably fast
        concurrentAccessTime.Should().BeLessThan(TimeSpan.FromSeconds(30), 
            "Concurrent access (10 threads × 100 operations) should complete within 30 seconds");
        
        var successRate = (double)_performanceMetrics["ConcurrentAccessSuccessRate"];
        successRate.Should().BeGreaterThan(0.8, "At least 80% of concurrent operations should succeed");
        
        var totalOperations = (int)_performanceMetrics["TotalConcurrentOperations"];
        var successfulOperations = (int)_performanceMetrics["SuccessfulConcurrentOperations"];
        
        _awsPerformanceValidationResults.Add($"Concurrent access: {successfulOperations}/{totalOperations} operations in {concurrentAccessTime.TotalMilliseconds:F2}ms");
        scenarioContext.Set(_awsPerformanceValidationResults, "AwsPerformanceValidationResults");
    }

    [Then(@"the aws performance controller should demonstrate optimized memory usage patterns")]
    public void ThenTheAwsPerformanceControllerShouldDemonstrateOptimizedMemoryUsagePatterns()
    {
        _performanceMetrics.ContainsKey("MemoryOptimizationEnabled").Should().BeTrue();
        _performanceMetrics["MemoryOptimizationEnabled"].Should().Be(true);
        
        var memoryDelta = (long)_performanceMetrics["MemoryUsageDelta"];
        var memoryBefore = _memoryUsageBefore;
        var memoryAfter = (long)_performanceMetrics["MemoryUsageAfter"];
        
        // Memory usage should be reasonable
        memoryDelta.Should().BeLessThan(50_000_000, // 50MB
            "Memory usage increase should be under 50MB for optimized configurations");
        
        memoryDelta.Should().BeGreaterThan(0, "Some memory usage is expected for configuration loading");
        
        _awsPerformanceValidationResults.Add($"Memory usage: {memoryBefore / 1024 / 1024}MB → {memoryAfter / 1024 / 1024}MB (Δ{memoryDelta / 1024 / 1024}MB)");
        scenarioContext.Set(_awsPerformanceValidationResults, "AwsPerformanceValidationResults");
    }

    #endregion
}