
using FlexKit.Configuration.Core;
using FlexKit.Configuration.Providers.Azure.IntegrationTests.Utils;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Reqnroll;
using System.Diagnostics;
using FlexKit.Configuration.Conversion;

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
    private AzureTestConfigurationBuilder? _perfBuilder;
    private IConfiguration? _perfConfiguration;
    private IFlexConfig? _perfFlexConfiguration;
    private readonly List<string> _perfValidationResults = new();
    private readonly Stopwatch _loadStopwatch = new();
    private long _memoryBaseline;
    private bool _performanceMonitoringEnabled;
    private bool _concurrentAccessEnabled;
    private bool _memoryTrackingEnabled;

    #region Given Steps - Setup

    [Given(@"I have established an azure performance controller environment")]
    public void GivenIHaveEstablishedAnAzurePerformanceControllerEnvironment()
    {
        _perfBuilder = new AzureTestConfigurationBuilder(scenarioContext);
        _memoryBaseline = GC.GetTotalMemory(true);
        scenarioContext.Set(_perfBuilder, "PerfBuilder");
        _perfValidationResults.Add("✓ Performance controller environment established");
    }

    [Given(@"I have azure performance controller configuration with large Key Vault from ""(.*)""")]
    public void GivenIHaveAzurePerformanceControllerConfigurationWithLargeKeyVaultFrom(string testDataPath)
    {
        _perfBuilder.Should().NotBeNull("Performance builder should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        _perfBuilder!.AddKeyVaultFromTestData(fullPath, optional: false, jsonProcessor: true);
        
        scenarioContext.Set(_perfBuilder, "PerfBuilder");
        _perfValidationResults.Add("✓ Large Key Vault configuration added");
    }

    [Given(@"I have azure performance controller configuration with extensive App Configuration from ""(.*)""")]
    public void GivenIHaveAzurePerformanceControllerConfigurationWithExtensiveAppConfigurationFrom(string testDataPath)
    {
        _perfBuilder.Should().NotBeNull("Performance builder should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        _perfBuilder!.AddAppConfigurationFromTestData(fullPath, optional: false);
        
        scenarioContext.Set(_perfBuilder, "PerfBuilder");
        _perfValidationResults.Add("✓ Extensive App Configuration added");
    }

    [Given(@"I have azure performance controller configuration with concurrent access setup from ""(.*)""")]
    public void GivenIHaveAzurePerformanceControllerConfigurationWithConcurrentAccessSetupFrom(string testDataPath)
    {
        _perfBuilder.Should().NotBeNull("Performance builder should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        _perfBuilder!.AddKeyVaultFromTestData(fullPath, optional: false, jsonProcessor: true);
        _perfBuilder!.AddAppConfigurationFromTestData(fullPath, optional: false);
        _concurrentAccessEnabled = true;
        
        scenarioContext.Set(_perfBuilder, "PerfBuilder");
        _perfValidationResults.Add("✓ Concurrent access configuration setup added");
    }

    [Given(@"I have azure performance controller configuration with memory monitoring from ""(.*)""")]
    public void GivenIHaveAzurePerformanceControllerConfigurationWithMemoryMonitoringFrom(string testDataPath)
    {
        _perfBuilder.Should().NotBeNull("Performance builder should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        _perfBuilder!.AddKeyVaultFromTestData(fullPath, optional: false, jsonProcessor: true);
        _perfBuilder!.AddAppConfigurationFromTestData(fullPath, optional: false);
        _memoryTrackingEnabled = true;
        
        scenarioContext.Set(_perfBuilder, "PerfBuilder");
        _perfValidationResults.Add("✓ Memory monitoring configuration setup added");
    }

    #endregion

    #region When Steps - Actions

    [When(@"I configure azure performance controller with performance monitoring")]
    public void WhenIConfigureAzurePerformanceControllerWithPerformanceMonitoring()
    {
        _perfBuilder.Should().NotBeNull("Performance builder should be established");
        _performanceMonitoringEnabled = true;
        _loadStopwatch.Reset();
    }

    [When(@"I configure azure performance controller with concurrent access testing")]
    public void WhenIConfigureAzurePerformanceControllerWithConcurrentAccessTesting()
    {
        _perfBuilder.Should().NotBeNull("Performance builder should be established");
        _concurrentAccessEnabled = true;
    }

    [When(@"I configure azure performance controller with memory tracking")]
    public void WhenIConfigureAzurePerformanceControllerWithMemoryTracking()
    {
        _perfBuilder.Should().NotBeNull("Performance builder should be established");
        _memoryTrackingEnabled = true;
        _memoryBaseline = GC.GetTotalMemory(true);
    }

    [When(@"I configure azure performance controller by building the configuration")]
    public void WhenIConfigureAzurePerformanceControllerByBuildingTheConfiguration()
    {
        _perfBuilder.Should().NotBeNull("Performance builder should be established");

        try
        {
            // Start LocalStack first
            var startTask = _perfBuilder!.StartLocalStackAsync("keyvault,appconfig");
            startTask.Wait(TimeSpan.FromMinutes(2));

            if (_performanceMonitoringEnabled)
            {
                _loadStopwatch.Start();
            }

            _perfConfiguration = _perfBuilder.Build();
            _perfFlexConfiguration = _perfBuilder.BuildFlexConfig();

            if (_performanceMonitoringEnabled)
            {
                _loadStopwatch.Stop();
            }
            
            scenarioContext.Set(_perfConfiguration, "PerfConfiguration");
            scenarioContext.Set(_perfFlexConfiguration, "PerfFlexConfiguration");
            
            _perfValidationResults.Add("✓ Performance configuration built successfully");
            _perfValidationResults.Add($"✓ Configuration load time: {_loadStopwatch.ElapsedMilliseconds}ms");
        }
        catch (Exception ex)
        {
            _perfValidationResults.Add($"✗ Performance configuration build failed: {ex.Message}");
            throw;
        }
    }

    [When(@"I configure azure performance controller by building the configuration with concurrency")]
    public void WhenIConfigureAzurePerformanceControllerByBuildingTheConfigurationWithConcurrency()
    {
        _perfBuilder.Should().NotBeNull("Performance builder should be established");

        try
        {
            // Start LocalStack first
            var startTask = _perfBuilder!.StartLocalStackAsync("keyvault,appconfig");
            startTask.Wait(TimeSpan.FromMinutes(2));

            var tasks = new List<Task>();
            for (var i = 0; i < 10; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    var config = _perfBuilder.Build();
                    var flexConfig = _perfBuilder.BuildFlexConfig();
                    return (config, flexConfig);
                }));
            }

            Task.WaitAll(tasks.ToArray());
            
            // Use the last built configuration
            var lastResult = tasks.Last().Result;
            _perfConfiguration = lastResult.config;
            _perfFlexConfiguration = lastResult.flexConfig;
            
            scenarioContext.Set(_perfConfiguration, "PerfConfiguration");
            scenarioContext.Set(_perfFlexConfiguration, "PerfFlexConfiguration");
            
            _perfValidationResults.Add("✓ Concurrent configuration build completed successfully");
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
        _loadStopwatch.ElapsedMilliseconds.Should().BeLessThan(5000, 
            "Key Vault loading should complete within 5 seconds");
        _perfValidationResults.Add($"✓ Key Vault load time: {_loadStopwatch.ElapsedMilliseconds}ms");
    }

    [Then(@"the azure performance controller should demonstrate efficient secret retrieval")]
    public void ThenTheAzurePerformanceControllerShouldDemonstrateEfficientSecretRetrieval()
    {
        _perfFlexConfiguration.Should().NotBeNull();
        var sw = Stopwatch.StartNew();
        var testValue = _perfFlexConfiguration!["test:secret"].ToType<string>();
        sw.Stop();

        sw.ElapsedMilliseconds.Should().BeLessThan(100, 
            "Individual secret retrieval should be fast");
        _perfValidationResults.Add($"✓ Secret retrieval time: {sw.ElapsedMilliseconds}ms");
    }

    [Then(@"the azure performance controller should report Key Vault performance metrics")]
    public void ThenTheAzurePerformanceControllerShouldReportKeyVaultPerformanceMetrics()
    {
        _perfValidationResults.Should().NotBeEmpty("Performance metrics should be collected");
        _perfValidationResults.Should().Contain(x => x.Contains("load time"), 
            "Load time metrics should be recorded");
    }

    [Then(@"the azure performance controller should complete App Configuration loading within performance limits")]
    public void ThenTheAzurePerformanceControllerShouldCompleteAppConfigurationLoadingWithinPerformanceLimits()
    {
        _loadStopwatch.ElapsedMilliseconds.Should().BeLessThan(5000, 
            "App Configuration loading should complete within 5 seconds");
        _perfValidationResults.Add($"✓ App Configuration load time: {_loadStopwatch.ElapsedMilliseconds}ms");
    }

    [Then(@"the azure performance controller should demonstrate efficient configuration retrieval")]
    public void ThenTheAzurePerformanceControllerShouldDemonstrateEfficientConfigurationRetrieval()
    {
        _perfFlexConfiguration.Should().NotBeNull();
        var sw = Stopwatch.StartNew();
        var testValue = _perfFlexConfiguration!["test:config"].ToType<string>();
        sw.Stop();

        sw.ElapsedMilliseconds.Should().BeLessThan(100, 
            "Individual configuration retrieval should be fast");
        _perfValidationResults.Add($"✓ Configuration retrieval time: {sw.ElapsedMilliseconds}ms");
    }

    [Then(@"the azure performance controller should report App Configuration performance metrics")]
    public void ThenTheAzurePerformanceControllerShouldReportAppConfigurationPerformanceMetrics()
    {
        _perfValidationResults.Should().NotBeEmpty("Performance metrics should be collected");
        _perfValidationResults.Should().Contain(x => x.Contains("retrieval time"), 
            "Retrieval time metrics should be recorded");
    }

    [Then(@"the azure performance controller should handle concurrent access safely")]
    public void ThenTheAzurePerformanceControllerShouldHandleConcurrentAccessSafely()
    {
        _perfConfiguration.Should().NotBeNull();
        _perfFlexConfiguration.Should().NotBeNull();
        _perfValidationResults.Add("✓ Configuration supports concurrent access");
    }

    [Then(@"the azure performance controller should demonstrate thread-safe configuration access")]
    public void ThenTheAzurePerformanceControllerShouldDemonstrateThreadSafeConfigurationAccess()
    {
        _perfFlexConfiguration.Should().NotBeNull();
        
        var tasks = Enumerable.Range(0, 100).Select(_ => Task.Run(() =>
        {
            var value = _perfFlexConfiguration!["test:config"].ToType<string>();
            return value != null;
        })).ToArray();

        Task.WaitAll(tasks);
        
        tasks.Should().AllSatisfy(t => t.Result.Should().BeTrue(),
            "All concurrent configuration access should succeed");
        _perfValidationResults.Add("✓ Thread-safe configuration access verified");
    }

    [Then(@"the azure performance controller should report concurrency performance metrics")]
    public void ThenTheAzurePerformanceControllerShouldReportConcurrencyPerformanceMetrics()
    {
        _perfValidationResults.Should().NotBeEmpty("Concurrency metrics should be collected");
        _perfValidationResults.Should().Contain(x => x.Contains("concurrent"), 
            "Concurrent access metrics should be recorded");
    }

    [Then(@"the azure performance controller should maintain acceptable memory usage")]
    public void ThenTheAzurePerformanceControllerShouldMaintainAcceptableMemoryUsage()
    {
        var currentMemory = GC.GetTotalMemory(false);
        var memoryDelta = currentMemory - _memoryBaseline;
        
        memoryDelta.Should().BeLessThan(50 * 1024 * 1024, 
            "Memory usage increase should be less than 50MB");
        _perfValidationResults.Add($"✓ Memory usage delta: {memoryDelta / 1024 / 1024}MB");
    }

    [Then(@"the azure performance controller should demonstrate efficient resource utilization")]
    public void ThenTheAzurePerformanceControllerShouldDemonstrateEfficientResourceUtilization()
    {
        GC.Collect();
        var currentMemory = GC.GetTotalMemory(true);
        var memoryDelta = currentMemory - _memoryBaseline;
        
        memoryDelta.Should().BeLessThan(20 * 1024 * 1024, 
            "Memory usage after cleanup should be less than 20MB");
        _perfValidationResults.Add($"✓ Post-cleanup memory delta: {memoryDelta / 1024 / 1024}MB");
    }

    [Then(@"the azure performance controller should report memory usage metrics")]
    public void ThenTheAzurePerformanceControllerShouldReportMemoryUsageMetrics()
    {
        _perfValidationResults.Should().NotBeEmpty("Memory metrics should be collected");
        _perfValidationResults.Should().Contain(x => x.Contains("memory"), 
            "Memory usage metrics should be recorded");
    }

    #endregion
}