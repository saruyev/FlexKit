using FlexKit.Configuration.Core;
using FlexKit.Configuration.Providers.Aws.IntegrationTests.Utils;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Reqnroll;
// ReSharper disable NullableWarningSuppressionIsUsed

namespace FlexKit.Configuration.Providers.Aws.IntegrationTests.Steps.ParameterStore;

/// <summary>
/// Step definitions for Parameter Store basic loading scenarios.
/// Tests fundamental Parameter Store configuration loading including string parameters,
/// JSON processing, StringList parameters, SecureString parameters, and error handling.
/// Uses distinct step patterns ("parameters basic module") to avoid conflicts with other step classes.
/// </summary>
[Binding]
public class ParameterStoreBasicLoadingSteps(ScenarioContext scenarioContext)
{
    private AwsTestConfigurationBuilder? _parametersBasicBuilder;
    private IConfiguration? _parametersBasicConfiguration;
    private IFlexConfig? _parametersBasicFlexConfiguration;
    private Exception? _lastParametersBasicException;

    #region Given Steps - Setup

    [Given(@"I have established a parameters basic module environment")]
    public void GivenIHaveEstablishedAParametersBasicModuleEnvironment()
    {
        _parametersBasicBuilder = new AwsTestConfigurationBuilder(scenarioContext);
        scenarioContext.Set(_parametersBasicBuilder, "ParametersBasicBuilder");
    }

    [Given(@"I have parameters basic module configuration from ""(.*)""")]
    public void GivenIHaveParametersBasicModuleConfigurationFrom(string testDataPath)
    {
        _parametersBasicBuilder.Should().NotBeNull("Parameters basic builder should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        _parametersBasicBuilder!.AddParameterStoreFromTestData(fullPath, optional: false, jsonProcessor: false);
        
        scenarioContext.Set(_parametersBasicBuilder, "ParametersBasicBuilder");
    }

    [Given(@"I have parameters basic module configuration with JSON processing from ""(.*)""")]
    public void GivenIHaveParametersBasicModuleConfigurationWithJsonProcessingFrom(string testDataPath)
    {
        _parametersBasicBuilder.Should().NotBeNull("Parameters basic builder should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        _parametersBasicBuilder!.AddParameterStoreFromTestData(fullPath, optional: false, jsonProcessor: true);
        
        scenarioContext.Set(_parametersBasicBuilder, "ParametersBasicBuilder");
    }

    [Given(@"I have parameters basic module configuration with StringList from ""(.*)""")]
    public void GivenIHaveParametersBasicModuleConfigurationWithStringListFrom(string testDataPath)
    {
        _parametersBasicBuilder.Should().NotBeNull("Parameters basic builder should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        _parametersBasicBuilder!.AddParameterStoreFromTestData(fullPath, optional: false, jsonProcessor: false);
        
        scenarioContext.Set(_parametersBasicBuilder, "ParametersBasicBuilder");
    }

    [Given(@"I have parameters basic module configuration with missing path as optional from ""(.*)""")]
    public void GivenIHaveParametersBasicModuleConfigurationWithMissingPathAsOptionalFrom(string testDataPath)
    {
        _parametersBasicBuilder.Should().NotBeNull("Parameters basic builder should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        _parametersBasicBuilder!.AddParameterStoreFromTestData(fullPath, optional: true, jsonProcessor: false);
        
        scenarioContext.Set(_parametersBasicBuilder, "ParametersBasicBuilder");
    }

    [Given(@"I have parameters basic module configuration with missing path as required from ""(.*)""")]
    public void GivenIHaveParametersBasicModuleConfigurationWithMissingPathAsRequiredFrom(string testDataPath)
    {
        _parametersBasicBuilder.Should().NotBeNull("Parameters basic builder should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        _parametersBasicBuilder!.AddParameterStoreFromTestData(fullPath, optional: false, jsonProcessor: false);
        
        scenarioContext.Set(_parametersBasicBuilder, "ParametersBasicBuilder");
    }

    #endregion

    #region When Steps - Actions

    [When(@"I configure parameters basic module by building the configuration")]
    public void WhenIConfigureParametersBasicModuleByBuildingTheConfiguration()
    {
        _parametersBasicBuilder.Should().NotBeNull("Parameters basic builder should be established");

        try
        {
            // _parametersBasicConfiguration = _parametersBasicBuilder!.Build();
            _parametersBasicFlexConfiguration = _parametersBasicBuilder!.BuildFlexConfig();
            _parametersBasicConfiguration = _parametersBasicFlexConfiguration.Configuration;

            // Debug: Log all configuration keys that were loaded
            var allKeys = _parametersBasicConfiguration.AsEnumerable()
                .Where(kvp => kvp.Value != null)
                .Take(10) // Log the first 10 keys for debugging
                .Select(kvp => $"{kvp.Key} = {kvp.Value}")
                .ToList();
            
            foreach (var debugKey in allKeys)
            {
                System.Diagnostics.Debug.WriteLine($"Config loaded: {debugKey}");
            }

            scenarioContext.Set(_parametersBasicConfiguration, "ParametersBasicConfiguration");
            scenarioContext.Set(_parametersBasicFlexConfiguration, "ParametersBasicFlexConfiguration");
        }
        catch (Exception ex)
        {
            _lastParametersBasicException = ex;
            scenarioContext.Set(ex, "ParametersBasicException");
        }
    }

    [When(@"I verify parameters basic module dynamic access capabilities")]
    public void WhenIVerifyParametersBasicModuleDynamicAccessCapabilities()
    {
        _parametersBasicFlexConfiguration.Should().NotBeNull("Parameters basic FlexConfiguration should be built");

        // Test dynamic access to configuration values
        dynamic config = _parametersBasicFlexConfiguration!;
        
        try
        {
            // Verify dynamic access works by accessing a known configuration value
            string dynamicHost = AwsTestConfigurationBuilder.GetDynamicProperty(config, "infrastructure-module.database.host");
            dynamicHost.Should().NotBeNull("Dynamic access to infrastructure-module.database.host should return a value");
        }
        catch (Exception ex)
        {
            throw new Exception($"Dynamic access error: {ex.Message}");
        }
    }

    #endregion

    #region Then Steps - Assertions

    [Then(@"the parameters basic module configuration should contain ""(.*)"" with value ""(.*)""")]
    public void ThenTheParametersBasicModuleConfigurationShouldContainWithValue(string key, string expectedValue)
    {
        _parametersBasicConfiguration.Should().NotBeNull("Parameters basic configuration should be built successfully");

        var actualValue = _parametersBasicConfiguration![key];
        actualValue.Should().Be(expectedValue, $"Configuration key '{key}' should have value '{expectedValue}'");
    }

    [Then(@"the parameters basic module configuration should be built successfully")]
    public void ThenTheParametersBasicModuleConfigurationShouldBeBuiltSuccessfully()
    {
        _parametersBasicConfiguration.Should().NotBeNull("Parameters basic configuration should be built without errors");
        _lastParametersBasicException.Should().BeNull("No exception should have occurred during configuration building");
    }

    [Then(@"the parameters basic module configuration should not contain ""(.*)""")]
    public void ThenTheParametersBasicModuleConfigurationShouldNotContain(string key)
    {
        _parametersBasicConfiguration.Should().NotBeNull("Parameters basic configuration should be built");

        var actualValue = _parametersBasicConfiguration![key];
        actualValue.Should().BeNull($"Configuration key '{key}' should not exist");
    }

    [Then(@"the parameters basic module configuration should fail to build")]
    public void ThenTheParametersBasicModuleConfigurationShouldFailToBuild()
    {
        _lastParametersBasicException.Should().NotBeNull("An exception should have occurred during configuration building");
        _parametersBasicConfiguration.Should().BeNull("Configuration should not be built when required parameters are missing");
    }

    [Then(@"the parameters basic module should have configuration exception")]
    public void ThenTheParametersBasicModuleShouldHaveConfigurationException()
    {
        _lastParametersBasicException.Should().NotBeNull("Configuration exception should have occurred");
        _lastParametersBasicException.Should().BeAssignableTo<Exception>("Exception should be a configuration-related exception");
    }

    [Then(@"the parameters basic module FlexConfig should provide dynamic access to ""(.*)""")]
    public void ThenTheParametersBasicModuleFlexConfigShouldProvideDynamicAccessTo(string propertyPath)
    {
        _parametersBasicFlexConfiguration.Should().NotBeNull("Parameters basic FlexConfiguration should be available");

        // First, verify that we can access the configuration as dynamic
        dynamic config = _parametersBasicFlexConfiguration!;
        
        var dynamicValue = AwsTestConfigurationBuilder.GetDynamicProperty(config, propertyPath);
        string stringValue = dynamicValue?.ToString() ?? string.Empty;
        stringValue.Should().NotBeNull($"Dynamic property '{propertyPath}' should be accessible and have a value");
        
        // Also verify that the value matches what we expect from the configuration
        var configKey = propertyPath.Replace('.', ':');
        var configValue = _parametersBasicConfiguration![configKey];
        configValue.Should().NotBeNull($"Configuration value for key '{configKey}' should exist");
        
        stringValue.Should().Be(configValue, $"Dynamic access value should match direct configuration access for '{propertyPath}'");
    }

    #endregion
}