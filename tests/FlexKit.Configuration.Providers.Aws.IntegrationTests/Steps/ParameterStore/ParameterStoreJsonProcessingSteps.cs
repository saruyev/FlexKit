using FlexKit.Configuration.Core;
using FlexKit.Configuration.Providers.Aws.IntegrationTests.Utils;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Reqnroll;

namespace FlexKit.Configuration.Providers.Aws.IntegrationTests.Steps.ParameterStore;

/// <summary>
/// Step definitions for Parameter Store JSON processing scenarios.
/// Tests automatic JSON parameter processing, hierarchical key flattening,
/// complex object navigation, and dynamic access to JSON-processed configuration data.
/// Uses distinct step patterns ("parameters JSON module") to avoid conflicts with other step classes.
/// </summary>
[Binding]
public class ParameterStoreJsonProcessingSteps(ScenarioContext scenarioContext)
{
    private AwsTestConfigurationBuilder? _parametersJsonBuilder;
    private IConfiguration? _parametersJsonConfiguration;
    private IFlexConfig? _parametersJsonFlexConfiguration;
    private Exception? _lastParametersJsonException;
    private readonly List<string> _parametersJsonValidationResults = new();
    private bool _jsonProcessingEnabled;
    private bool _errorToleranceEnabled;

    #region Given Steps - Setup

    [Given(@"I have established a parameters json module environment")]
    public void GivenIHaveEstablishedAParametersJsonModuleEnvironment()
    {
        _parametersJsonBuilder = new AwsTestConfigurationBuilder(scenarioContext);
        scenarioContext.Set(_parametersJsonBuilder, "ParametersJsonBuilder");
    }

    [Given(@"I have parameters json module configuration with JSON processing from ""(.*)""")]
    public void GivenIHaveParametersJsonModuleConfigurationWithJsonProcessingFrom(string testDataPath)
    {
        _parametersJsonBuilder.Should().NotBeNull("Parameters json builder should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        _parametersJsonBuilder!.AddParameterStoreFromTestData(fullPath, optional: false, jsonProcessor: true);
        _jsonProcessingEnabled = true;
        
        scenarioContext.Set(_parametersJsonBuilder, "ParametersJsonBuilder");
    }

    [Given(@"I have parameters json module configuration with optional JSON processing from ""(.*)""")]
    public void GivenIHaveParametersJsonModuleConfigurationWithOptionalJsonProcessingFrom(string testDataPath)
    {
        _parametersJsonBuilder.Should().NotBeNull("Parameters json builder should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        _parametersJsonBuilder!.AddParameterStoreFromTestData(fullPath, optional: true, jsonProcessor: true);
        _jsonProcessingEnabled = true;
        
        scenarioContext.Set(_parametersJsonBuilder, "ParametersJsonBuilder");
    }

    [Given(@"I have parameters json module configuration with invalid JSON processing from ""(.*)""")]
    public void GivenIHaveParametersJsonModuleConfigurationWithInvalidJsonProcessingFrom(string testDataPath)
    {
        _parametersJsonBuilder.Should().NotBeNull("Parameters json builder should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        
        // Add some configuration data with a mix of valid and invalid JSON
        var configData = new Dictionary<string, string?>
        {
            ["infrastructure-module:app:config:database:host"] = "complex-db.example.com",
            ["infrastructure-module:app:config:database:port"] = "5432",
            ["infrastructure-module:invalid:json"] = "{invalid json structure",
            ["infrastructure-module:valid:setting"] = "normal-value"
        };
        
        _parametersJsonBuilder!.AddInMemoryCollection(configData);
        _parametersJsonBuilder.AddParameterStoreFromTestData(fullPath, optional: true, jsonProcessor: true);
        _jsonProcessingEnabled = true;
        
        scenarioContext.Set(_parametersJsonBuilder, "ParametersJsonBuilder");
    }

    #endregion

    #region When Steps - Actions

    [When(@"I configure parameters json module by building the configuration")]
    public void WhenIConfigureParametersJsonModuleByBuildingTheConfiguration()
    {
        _parametersJsonBuilder.Should().NotBeNull("Parameters json builder should be established");

        try
        {
            _parametersJsonFlexConfiguration = _parametersJsonBuilder!.BuildFlexConfig();
            _parametersJsonConfiguration = _parametersJsonFlexConfiguration.Configuration;

            // Debug: Log some configuration keys that were loaded to verify JSON processing
            var allKeys = _parametersJsonConfiguration.AsEnumerable()
                .Where(kvp => kvp.Value != null)
                .Take(15) // Log more keys for debugging JSON processing
                .Select(kvp => $"{kvp.Key} = {kvp.Value}")
                .ToList();
            
            foreach (var debugKey in allKeys)
            {
                System.Diagnostics.Debug.WriteLine($"Config loaded: {debugKey}");
            }

            scenarioContext.Set(_parametersJsonConfiguration, "ParametersJsonConfiguration");
            scenarioContext.Set(_parametersJsonFlexConfiguration, "ParametersJsonFlexConfiguration");
        }
        catch (Exception ex)
        {
            _lastParametersJsonException = ex;
            scenarioContext.Set(ex, "ParametersJsonException");
        }
    }

    [When(@"I configure parameters json module by building the configuration with error tolerance")]
    public void WhenIConfigureParametersJsonModuleByBuildingTheConfigurationWithErrorTolerance()
    {
        _parametersJsonBuilder.Should().NotBeNull("Parameters json builder should be established");
        _errorToleranceEnabled = true;

        try
        {
            _parametersJsonFlexConfiguration = _parametersJsonBuilder!.BuildFlexConfig();
            _parametersJsonConfiguration = _parametersJsonFlexConfiguration.Configuration;

            scenarioContext.Set(_parametersJsonConfiguration, "ParametersJsonConfiguration");
            scenarioContext.Set(_parametersJsonFlexConfiguration, "ParametersJsonFlexConfiguration");
        }
        catch (Exception ex)
        {
            _lastParametersJsonException = ex;
            scenarioContext.Set(ex, "ParametersJsonException");
            
            // In error tolerance mode, we don't fail - we capture the exception
            System.Diagnostics.Debug.WriteLine($"Exception captured with error tolerance: {ex.Message}");
        }
    }

    [When(@"I verify parameters json module dynamic access capabilities")]
    public void WhenIVerifyParametersJsonModuleDynamicAccessCapabilities()
    {
        _parametersJsonFlexConfiguration.Should().NotBeNull("Parameters json FlexConfiguration should be built");

        // Test dynamic access to JSON-processed configuration values
        dynamic config = _parametersJsonFlexConfiguration!;
        
        try
        {
            // Test accessing nested database configuration
            var dbHost = config.infrastructure_module.app.config.database.host;
            _parametersJsonValidationResults.Add($"Dynamic access to database.host: {dbHost}");
            
            // Test accessing boolean values
            var sslEnabled = config.infrastructure_module.app.config.database.ssl;
            _parametersJsonValidationResults.Add($"Dynamic access to database.ssl: {sslEnabled}");
            
            // Test accessing cache configuration
            var cacheType = config.infrastructure_module.app.config.cache.type;
            _parametersJsonValidationResults.Add($"Dynamic access to cache.type: {cacheType}");
            
            scenarioContext.Set(_parametersJsonValidationResults, "ParametersJsonValidationResults");
        }
        catch (Exception ex)
        {
            _lastParametersJsonException = ex;
            scenarioContext.Set(ex, "ParametersJsonDynamicException");
        }
    }

    #endregion

    #region Then Steps - Verification

    [Then(@"the parameters json module configuration should contain ""(.*)"" with value ""(.*)""")]
    public void ThenTheParametersJsonModuleConfigurationShouldContainWithValue(string configKey, string expectedValue)
    {
        _parametersJsonConfiguration.Should().NotBeNull("Parameters json configuration should be built");

        var actualValue = _parametersJsonConfiguration![configKey];
        actualValue.Should().Be(expectedValue, $"Configuration key '{configKey}' should have value '{expectedValue}'");
        
        _parametersJsonValidationResults.Add($"✓ {configKey} = {actualValue}");
    }

    [Then(@"the parameters json module configuration should be built successfully")]
    public void ThenTheParametersJsonModuleConfigurationShouldBeBuiltSuccessfully()
    {
        if (_lastParametersJsonException != null && !_errorToleranceEnabled)
        {
            throw new Exception($"Configuration building failed with exception: {_lastParametersJsonException.Message}");
        }

        _parametersJsonConfiguration.Should().NotBeNull("Parameters json configuration should be built successfully");
        
        if (_parametersJsonFlexConfiguration != null)
        {
            _parametersJsonFlexConfiguration.Should().NotBeNull("Parameters json FlexConfiguration should be built successfully");
        }
    }

    [Then(@"the parameters json module FlexConfig should provide dynamic access to ""(.*)""")]
    public void ThenTheParametersJsonModuleFlexConfigShouldProvideDynamicAccessTo(string configPath)
    {
        _parametersJsonFlexConfiguration.Should().NotBeNull("Parameters json FlexConfiguration should be built");

        dynamic config = _parametersJsonFlexConfiguration!;

        string current = AwsTestConfigurationBuilder.GetDynamicProperty(config, configPath);
        
        current.Should().NotBeNull($"Dynamic access to '{configPath}' should return a value");
        _parametersJsonValidationResults.Add($"✓ Dynamic access to {configPath} succeeded");
    }

    [Then(@"the parameters json module configuration should have JSON processing enabled")]
    public void ThenTheParametersJsonModuleConfigurationShouldHaveJsonProcessingEnabled()
    {
        _jsonProcessingEnabled.Should().BeTrue("JSON processing should be enabled");
        
        // Verify that JSON processing actually occurred by checking for flattened keys
        _parametersJsonConfiguration.Should().NotBeNull("Configuration should be built");
        
        var flattenedKeys = _parametersJsonConfiguration!.AsEnumerable()
            .Where(kvp => kvp.Key.Contains(':') && kvp.Key.Split(':').Length > 3)
            .Take(5)
            .ToList();
            
        flattenedKeys.Should().NotBeEmpty("Should have flattened configuration keys from JSON processing");
        
        foreach (var key in flattenedKeys)
        {
            _parametersJsonValidationResults.Add($"✓ JSON flattened key: {key.Key}");
        }
    }

    [Then(@"the parameters json module configuration should contain processed JSON data")]
    public void ThenTheParametersJsonModuleConfigurationShouldContainProcessedJsonData()
    {
        _parametersJsonConfiguration.Should().NotBeNull("Configuration should be built");
        
        // Look for evidence of JSON processing - hierarchical keys that would only exist if JSON was processed
        var jsonProcessedKeys = _parametersJsonConfiguration!.AsEnumerable()
            .Where(kvp => kvp.Key.Contains("infrastructure-module:app:config") || 
                         kvp.Key.Contains("infrastructure-module:services:config"))
            .ToList();
            
        jsonProcessedKeys.Should().NotBeEmpty("Should contain keys that indicate JSON processing occurred");
        
        foreach (var key in jsonProcessedKeys.Take(10))
        {
            _parametersJsonValidationResults.Add($"✓ JSON processed key: {key.Key} = {key.Value}");
        }
    }

    [Then(@"the parameters json module should handle JSON processing errors gracefully")]
    public void ThenTheParametersJsonModuleShouldHandleJsonProcessingErrorsGracefully()
    {
        // In error tolerance mode, configuration should still be built even with some invalid JSON
        _parametersJsonConfiguration.Should().NotBeNull("Configuration should be built despite JSON errors");
        
        // Should contain valid configuration data
        var validKeys = _parametersJsonConfiguration!.AsEnumerable()
            .Where(kvp => kvp.Value != null && !kvp.Key.Contains("invalid"))
            .ToList();
            
        validKeys.Should().NotBeEmpty("Should contain valid configuration data despite JSON errors");
        
        _parametersJsonValidationResults.Add($"✓ Gracefully handled errors, {validKeys.Count} valid keys loaded");
    }

    #endregion
}