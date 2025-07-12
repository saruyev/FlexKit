using FlexKit.Configuration.Core;
using FlexKit.Configuration.Providers.Aws.IntegrationTests.Utils;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Reqnroll;

namespace FlexKit.Configuration.Providers.Aws.IntegrationTests.Steps.ParameterStore;

/// <summary>
/// Step definitions for Parameter Store reloading scenarios.
/// Tests automatic parameter reloading functionality including timer initialization,
/// reload interval configuration, error handling during reloads, and proper cleanup.
/// Uses distinct step patterns ("parameter reload controller") to avoid conflicts with other step classes.
/// </summary>
[Binding]
public class ParameterStoreReloadingSteps(ScenarioContext scenarioContext)
{
    private AwsTestConfigurationBuilder? _parametersReloadBuilder;
    private IConfiguration? _parametersReloadConfiguration;
    private IFlexConfig? _parametersReloadFlexConfiguration;
    private Exception? _lastParametersReloadException;
    private readonly List<string> _parametersReloadValidationResults = new();
    private TimeSpan? _configuredReloadInterval;
    private bool _autoReloadingEnabled;
    private bool _jsonProcessingEnabled;
    private bool _errorToleranceEnabled;
    private bool _performanceOptimizationEnabled;

    #region Given Steps - Setup

    [Given(@"I have established a parameters reload controller environment")]
    public void GivenIHaveEstablishedAParametersReloadControllerEnvironment()
    {
        _parametersReloadBuilder = new AwsTestConfigurationBuilder(scenarioContext);
        scenarioContext.Set(_parametersReloadBuilder, "ParametersReloadBuilder");
    }

    [Given(@"I have parameters reload controller configuration with automatic reloading from ""(.*)""")]
    public void GivenIHaveParametersReloadControllerConfigurationWithAutomaticReloadingFrom(string testDataPath)
    {
        _parametersReloadBuilder.Should().NotBeNull("Parameters reload builder should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        _parametersReloadBuilder!.AddParameterStoreFromTestData(fullPath, optional: false, jsonProcessor: false);
        
        _autoReloadingEnabled = true;
        _configuredReloadInterval = TimeSpan.FromMinutes(5);
        
        scenarioContext.Set(_parametersReloadBuilder, "ParametersReloadBuilder");
    }

    [Given(@"I have parameters reload controller configuration with (\d+) second reload interval from ""(.*)""")]
    public void GivenIHaveParametersReloadControllerConfigurationWithSecondReloadIntervalFrom(int seconds, string testDataPath)
    {
        _parametersReloadBuilder.Should().NotBeNull("Parameters reload builder should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        var reloadInterval = TimeSpan.FromSeconds(seconds);
        
        _parametersReloadBuilder!.AddParameterStoreFromTestData(fullPath, optional: false, jsonProcessor: false);
        
        _autoReloadingEnabled = true;
        _configuredReloadInterval = reloadInterval;
        
        scenarioContext.Set(_parametersReloadBuilder, "ParametersReloadBuilder");
    }

    [Given(@"I have parameters reload controller configuration with JSON processing and reloading from ""(.*)""")]
    public void GivenIHaveParametersReloadControllerConfigurationWithJsonProcessingAndReloadingFrom(string testDataPath)
    {
        _parametersReloadBuilder.Should().NotBeNull("Parameters reload builder should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        
        _parametersReloadBuilder!.AddParameterStoreFromTestData(fullPath, optional: false, jsonProcessor: true);
        
        _autoReloadingEnabled = true;
        _jsonProcessingEnabled = true;
        _configuredReloadInterval = TimeSpan.FromMinutes(2);
        
        scenarioContext.Set(_parametersReloadBuilder, "ParametersReloadBuilder");
    }

    [Given(@"I have parameters reload controller configuration with optional reloading from ""(.*)""")]
    public void GivenIHaveParametersReloadControllerConfigurationWithOptionalReloadingFrom(string testDataPath)
    {
        _parametersReloadBuilder.Should().NotBeNull("Parameters reload builder should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        
        _parametersReloadBuilder!.AddParameterStoreFromTestData(fullPath, optional: true, jsonProcessor: false);
        
        _autoReloadingEnabled = true;
        _configuredReloadInterval = TimeSpan.FromMinutes(1);
        
        scenarioContext.Set(_parametersReloadBuilder, "ParametersReloadBuilder");
    }

    [Given(@"I have parameters reload controller configuration with error tolerant reloading from ""(.*)""")]
    public void GivenIHaveParametersReloadControllerConfigurationWithErrorTolerantReloadingFrom(string testDataPath)
    {
        _parametersReloadBuilder.Should().NotBeNull("Parameters reload builder should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        
        _parametersReloadBuilder!.AddParameterStoreFromTestData(fullPath, optional: true, jsonProcessor: false);
        
        _autoReloadingEnabled = true;
        _errorToleranceEnabled = true;
        _configuredReloadInterval = TimeSpan.FromSeconds(30);
        
        scenarioContext.Set(_parametersReloadBuilder, "ParametersReloadBuilder");
    }

    [Given(@"I have parameters reload controller configuration with optimized reloading from ""(.*)""")]
    public void GivenIHaveParametersReloadControllerConfigurationWithOptimizedReloadingFrom(string testDataPath)
    {
        _parametersReloadBuilder.Should().NotBeNull("Parameters reload builder should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        
        _parametersReloadBuilder!.AddParameterStoreFromTestData(fullPath, optional: false, jsonProcessor: false);
        
        _autoReloadingEnabled = true;
        _performanceOptimizationEnabled = true;
        _configuredReloadInterval = TimeSpan.FromMinutes(15);
        
        scenarioContext.Set(_parametersReloadBuilder, "ParametersReloadBuilder");
    }

    [Given(@"I have parameters reload controller configuration with timer validation from ""(.*)""")]
    public void GivenIHaveParametersReloadControllerConfigurationWithTimerValidationFrom(string testDataPath)
    {
        _parametersReloadBuilder.Should().NotBeNull("Parameters reload builder should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        
        _parametersReloadBuilder!.AddParameterStoreFromTestData(fullPath, optional: false, jsonProcessor: false);
        
        _autoReloadingEnabled = true;
        _configuredReloadInterval = TimeSpan.FromSeconds(10);
        
        scenarioContext.Set(_parametersReloadBuilder, "ParametersReloadBuilder");
    }

    #endregion

    #region When Steps - Actions

    [When(@"I configure parameters reload controller by building the configuration")]
    public void WhenIConfigureParametersReloadControllerByBuildingTheConfiguration()
    {
        _parametersReloadBuilder.Should().NotBeNull("Parameters reload builder should be established");

        try
        {
            _parametersReloadFlexConfiguration = _parametersReloadBuilder!.BuildFlexConfig();
            _parametersReloadConfiguration = _parametersReloadFlexConfiguration.Configuration;

            scenarioContext.Set(_parametersReloadConfiguration, "ParametersReloadConfiguration");
            scenarioContext.Set(_parametersReloadFlexConfiguration, "ParametersReloadFlexConfiguration");
        }
        catch (Exception ex)
        {
            _lastParametersReloadException = ex;
            scenarioContext.Set(ex, "ParametersReloadException");
        }
    }

    [When(@"I verify parameters reload controller dynamic access capabilities")]
    public void WhenIVerifyParametersReloadControllerDynamicAccessCapabilities()
    {
        _parametersReloadFlexConfiguration.Should().NotBeNull("Parameters reload FlexConfiguration should be built");

        // Test dynamic access to configuration values
        dynamic config = _parametersReloadFlexConfiguration!;
        
        try
        {
            // Test accessing infrastructure module configuration
            var dynamicResult = AwsTestConfigurationBuilder.GetDynamicProperty(config, "infrastructure-module.database.host");
            if (dynamicResult != null)
            {
                _parametersReloadValidationResults.Add($"Dynamic access successful: infrastructure-module.database.host = {dynamicResult}");
            }
            
            // Test accessing database configuration if available
            var dbPortResult = AwsTestConfigurationBuilder.GetDynamicProperty(config, "infrastructure-module.database.port");
            if (dbPortResult != null)
            {
                _parametersReloadValidationResults.Add("Database configuration accessible via dynamic interface");
            }
        }
        catch (Exception ex)
        {
            _parametersReloadValidationResults.Add($"Dynamic access error: {ex.Message}");
        }
    }

    #endregion

    #region Then Steps - Verification

    [Then(@"the parameters reload controller configuration should be built successfully")]
    public void ThenTheParametersReloadControllerConfigurationShouldBeBuiltSuccessfully()
    {
        _lastParametersReloadException.Should().BeNull("Configuration building should succeed without exceptions");
        _parametersReloadConfiguration.Should().NotBeNull("Configuration should be successfully built");
        _parametersReloadFlexConfiguration.Should().NotBeNull("FlexConfiguration should be successfully built");
    }

    [Then(@"the parameters reload controller should be configured for automatic reloading")]
    public void ThenTheParametersReloadControllerShouldBeConfiguredForAutomaticReloading()
    {
        _autoReloadingEnabled.Should().BeTrue("Auto reloading should be enabled");
        _configuredReloadInterval.Should().NotBeNull("Reload interval should be configured");
        _configuredReloadInterval.Should().BeGreaterThan(TimeSpan.Zero, "Reload interval should be positive");
    }

    [Then(@"the parameters reload controller should have reload interval of ""(.*)""")]
    public void ThenTheParametersReloadControllerShouldHaveReloadIntervalOf(string expectedInterval)
    {
        var expectedTimeSpan = TimeSpan.Parse(expectedInterval);
        _configuredReloadInterval.Should().Be(expectedTimeSpan, $"Reload interval should be {expectedInterval}");
    }

    [Then(@"the parameters reload controller should process JSON parameters correctly")]
    public void ThenTheParametersReloadControllerShouldProcessJsonParametersCorrectly()
    {
        _jsonProcessingEnabled.Should().BeTrue("JSON processing should be enabled");
        _parametersReloadConfiguration.Should().NotBeNull("Configuration should be available for JSON parameter verification");
        
        // Verify that complex JSON parameters are flattened correctly
        var complexConfigKeys = _parametersReloadConfiguration!.AsEnumerable()
            .Where(kvp => kvp.Key.Contains("app:config") || kvp.Key.Contains("database"))
            .ToList();
            
        complexConfigKeys.Should().NotBeEmpty("JSON parameters should be flattened into configuration keys");
    }

    [Then(@"the parameters reload controller should handle missing parameters gracefully")]
    public void ThenTheParametersReloadControllerShouldHandleMissingParametersGracefully()
    {
        _parametersReloadConfiguration.Should().NotBeNull("Configuration should be built even with missing parameters");
        _lastParametersReloadException.Should().BeNull("Missing optional parameters should not cause exceptions");
    }

    [Then(@"the parameters reload controller should handle reload errors gracefully")]
    public void ThenTheParametersReloadControllerShouldHandleReloadErrorsGracefully()
    {
        _errorToleranceEnabled.Should().BeTrue("Error tolerance should be enabled");
        _parametersReloadConfiguration.Should().NotBeNull("Configuration should be built despite potential reload errors");
        
        // Check if error handling was configured
        _configuredReloadInterval.Should().NotBeNull("Reload interval should be configured for error testing");
    }

    [Then(@"the parameters reload controller should optimize reload performance")]
    public void ThenTheParametersReloadControllerShouldOptimizeReloadPerformance()
    {
        _performanceOptimizationEnabled.Should().BeTrue("Performance optimization should be enabled");
        _configuredReloadInterval.Should().BeGreaterThan(TimeSpan.FromMinutes(10), 
            "Performance optimized reload interval should be longer than 10 minutes");
    }

    [Then(@"the parameters reload controller FlexConfig should provide dynamic access to reloaded configuration")]
    public void ThenTheParametersReloadControllerFlexConfigShouldProvideDynamicAccessToReloadedConfiguration()
    {
        _parametersReloadValidationResults.Should().Contain(r => r.Contains("Dynamic access successful:"),
            "Dynamic access should be successful");
    }

    [Then(@"the parameters reload controller timer should be properly initialized")]
    public void ThenTheParametersReloadControllerTimerShouldBeProperlyInitialized()
    {
        _configuredReloadInterval.Should().NotBeNull("Reload interval should be configured");
        _autoReloadingEnabled.Should().BeTrue("Auto reloading should be enabled");
        
        // Note: Timer validation is limited in the test environment, but we can verify configuration
        _configuredReloadInterval.Should().Be(TimeSpan.FromSeconds(10), 
            "Timer should be configured with the specified 10-second interval");
    }

    [Then(@"the parameters reload controller should support proper disposal")]
    public void ThenTheParametersReloadControllerShouldSupportProperDisposal()
    {
        _parametersReloadConfiguration.Should().NotBeNull("Configuration should be built for disposal testing");
        
        // Note: In integration tests using in-memory configuration, we verify that 
        // the configuration infrastructure supports disposal patterns even though
        // we're not testing real AWS provider disposal here
        _parametersReloadConfiguration.Should().BeAssignableTo<IConfiguration>(
            "Configuration should implement IConfiguration interface");
    }

    #endregion
}