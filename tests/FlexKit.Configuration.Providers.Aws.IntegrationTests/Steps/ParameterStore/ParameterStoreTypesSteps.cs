using FlexKit.Configuration.Core;
using FlexKit.Configuration.Providers.Aws.IntegrationTests.Utils;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Reqnroll;
// ReSharper disable ClassTooBig

namespace FlexKit.Configuration.Providers.Aws.IntegrationTests.Steps.ParameterStore;

/// <summary>
/// Step definitions for Parameter Store parameter types scenarios.
/// Tests different AWS Parameter Store parameter types, including String, StringList, and SecureString,
/// with support for JSON processing, dynamic access, and mixed type configurations.
/// Uses distinct step patterns ("parameter types module") to avoid conflicts with other step classes.
/// </summary>
[Binding]
public class ParameterStoreTypesSteps(ScenarioContext scenarioContext)
{
    private AwsTestConfigurationBuilder? _parametersTypesBuilder;
    private IConfiguration? _parametersTypesConfiguration;
    private IFlexConfig? _parametersTypesFlexConfiguration;
    private Exception? _lastParametersTypesException;
    private readonly List<string> _parametersTypesValidationResults = new();
    private bool _jsonProcessingEnabled;

    #region Given Steps - Setup

    [Given(@"I have established a parameters types module environment")]
    public void GivenIHaveEstablishedAParametersTypesModuleEnvironment()
    {
        _parametersTypesBuilder = new AwsTestConfigurationBuilder(scenarioContext);
        scenarioContext.Set(_parametersTypesBuilder, "ParametersTypesBuilder");
    }

    [Given(@"I have parameters types module configuration with String parameters from ""(.*)""")]
    public void GivenIHaveParametersTypesModuleConfigurationWithStringParametersFrom(string testDataPath)
    {
        _parametersTypesBuilder.Should().NotBeNull("Parameters types builder should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        _parametersTypesBuilder!.AddParameterStoreFromTestData(fullPath, optional: false, jsonProcessor: false);
        scenarioContext.Set(_parametersTypesBuilder, "ParametersTypesBuilder");
    }

    [Given(@"I have parameters types module configuration with StringList parameters from ""(.*)""")]
    public void GivenIHaveParametersTypesModuleConfigurationWithStringListParametersFrom(string testDataPath)
    {
        _parametersTypesBuilder.Should().NotBeNull("Parameters types builder should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        _parametersTypesBuilder!.AddParameterStoreFromTestData(fullPath, optional: false, jsonProcessor: false);
        
        scenarioContext.Set(_parametersTypesBuilder, "ParametersTypesBuilder");
    }

    [Given(@"I have parameters types module configuration with SecureString parameters from ""(.*)""")]
    public void GivenIHaveParametersTypesModuleConfigurationWithSecureStringParametersFrom(string testDataPath)
    {
        _parametersTypesBuilder.Should().NotBeNull("Parameters types builder should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        _parametersTypesBuilder!.AddParameterStoreFromTestData(fullPath, optional: false, jsonProcessor: false);
        
        scenarioContext.Set(_parametersTypesBuilder, "ParametersTypesBuilder");
    }

    [Given(@"I have parameters types module configuration with SecureString JSON processing from ""(.*)""")]
    public void GivenIHaveParametersTypesModuleConfigurationWithSecureStringJsonProcessingFrom(string testDataPath)
    {
        _parametersTypesBuilder.Should().NotBeNull("Parameters types builder should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        _parametersTypesBuilder!.AddParameterStoreFromTestData(fullPath, optional: false, jsonProcessor: true);
        
        _jsonProcessingEnabled = true;
        
        scenarioContext.Set(_parametersTypesBuilder, "ParametersTypesBuilder");
    }

    [Given(@"I have parameters types module configuration with mixed parameter types from ""(.*)""")]
    public void GivenIHaveParametersTypesModuleConfigurationWithMixedParameterTypesFrom(string testDataPath)
    {
        _parametersTypesBuilder.Should().NotBeNull("Parameters types builder should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        _parametersTypesBuilder!.AddParameterStoreFromTestData(fullPath, optional: false, jsonProcessor: false);
        
        scenarioContext.Set(_parametersTypesBuilder, "ParametersTypesBuilder");
    }

    [Given(@"I have parameters types module configuration with mixed types and JSON processing from ""(.*)""")]
    public void GivenIHaveParametersTypesModuleConfigurationWithMixedTypesAndJsonProcessingFrom(string testDataPath)
    {
        _parametersTypesBuilder.Should().NotBeNull("Parameters types builder should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        _parametersTypesBuilder!.AddParameterStoreFromTestData(fullPath, optional: false, jsonProcessor: true);
        
        _jsonProcessingEnabled = true;
        
        scenarioContext.Set(_parametersTypesBuilder, "ParametersTypesBuilder");
    }

    [Given(@"I have parameters types module configuration with optional parameter types from ""(.*)""")]
    public void GivenIHaveParametersTypesModuleConfigurationWithOptionalParameterTypesFrom(string testDataPath)
    {
        _parametersTypesBuilder.Should().NotBeNull("Parameters types builder should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        _parametersTypesBuilder!.AddParameterStoreFromTestData(fullPath, optional: true, jsonProcessor: false);
        
        scenarioContext.Set(_parametersTypesBuilder, "ParametersTypesBuilder");
    }

    #endregion

    #region When Steps - Actions

    [When(@"I configure parameters types module by building the configuration")]
    public void WhenIConfigureParametersTypesModuleByBuildingTheConfiguration()
    {
        _parametersTypesBuilder.Should().NotBeNull("Parameters types builder should be established");

        try
        {
            _parametersTypesConfiguration = _parametersTypesBuilder!.Build();
            _parametersTypesFlexConfiguration = _parametersTypesBuilder.BuildFlexConfig();
            
            _parametersTypesValidationResults.Add("✓ Configuration built successfully");
        }
        catch (Exception ex)
        {
            _lastParametersTypesException = ex;
            _parametersTypesValidationResults.Add($"✗ Configuration build failed: {ex.Message}");
            throw;
        }
    }

    [When(@"I verify parameters types module dynamic access capabilities")]
    public void WhenIVerifyParametersTypesModuleDynamicAccessCapabilities()
    {
        _parametersTypesFlexConfiguration.Should().NotBeNull("FlexConfig should be built");

        dynamic config = _parametersTypesFlexConfiguration!;

        try
        {
            // Test dynamic access to different parameter types
            var stringValue = AwsTestConfigurationBuilder.GetDynamicProperty(config, "infrastructure-module.database.host");
            _parametersTypesValidationResults.Add($"✓ Dynamic access to String parameter: {stringValue}");

            var listValue = AwsTestConfigurationBuilder.GetDynamicProperty(config, "infrastructure-module.api.allowed-origins.0");
            _parametersTypesValidationResults.Add($"✓ Dynamic access to StringList parameter: {listValue}");
        }
        catch (Exception ex)
        {
            _parametersTypesValidationResults.Add($"✗ Dynamic access verification failed: {ex.Message}");
            throw;
        }
    }

    #endregion

    #region Then Steps - Verification

    [Then(@"the parameters types module configuration should contain ""(.*)"" with value ""(.*)""")]
    public void ThenTheParametersTypesModuleConfigurationShouldContainWithValue(string configKey, string expectedValue)
    {
        _parametersTypesConfiguration.Should().NotBeNull("Parameters types configuration should be built");

        var actualValue = _parametersTypesConfiguration![configKey];
        actualValue.Should().Be(expectedValue, $"Configuration key '{configKey}' should have value '{expectedValue}'");
        
        _parametersTypesValidationResults.Add($"✓ {configKey} = {actualValue}");
    }
    
    [Then(@"the parameters types module configuration should contain ""(.*)"" with value '(.*)'")]
    public void ThenTheParametersTypesModuleConfigurationShouldContainWithValueSingleQuotes(string configKey, string expectedValue)
    {
        ThenTheParametersTypesModuleConfigurationShouldContainWithValue(configKey, expectedValue);
    }

    [Then(@"the parameters types module should handle String parameters correctly")]
    public void ThenTheParametersTypesModuleShouldHandleStringParametersCorrectly()
    {
        _parametersTypesConfiguration.Should().NotBeNull("Configuration should be built");
        
        // Verify that String parameters are loaded as simple key-value pairs
        var stringParams = _parametersTypesConfiguration!.AsEnumerable()
            .Where(kvp => kvp.Key.Contains("database:host") || kvp.Key.Contains("database:port"))
            .ToList();
            
        stringParams.Should().NotBeEmpty("Should have String parameters loaded");
        
        foreach (var param in stringParams)
        {
            param.Value.Should().NotBeNull($"String parameter '{param.Key}' should have a value");
            _parametersTypesValidationResults.Add($"✓ String parameter handled: {param.Key}");
        }
    }

    [Then(@"the parameters types module should handle StringList parameters correctly")]
    public void ThenTheParametersTypesModuleShouldHandleStringListParametersCorrectly()
    {
        _parametersTypesConfiguration.Should().NotBeNull("Configuration should be built");
        
        // Verify that StringList parameters are converted to the indexed format
        var stringListParams = _parametersTypesConfiguration!.AsEnumerable()
            .Where(kvp => kvp.Key.Contains("allowed-origins:"))
            .ToList();
            
        stringListParams.Should().NotBeEmpty("Should have StringList parameters loaded");
        stringListParams.Should().HaveCountGreaterThanOrEqualTo(2, "Should have multiple StringList items");
        
        // Verify indexed format (key:0, key:1, etc.)
        var indexedKeys = stringListParams.Where(kvp => 
            kvp.Key.EndsWith(":0") || kvp.Key.EndsWith(":1") || kvp.Key.EndsWith(":2")).ToList();
            
        indexedKeys.Should().NotBeEmpty("StringList should be converted to indexed format");
        
        foreach (var param in indexedKeys)
        {
            param.Value.Should().NotBeNull($"StringList item '{param.Key}' should have a value");
            _parametersTypesValidationResults.Add($"✓ StringList item handled: {param.Key}");
        }
    }

    [Then(@"the parameters types module should handle SecureString parameters correctly")]
    public void ThenTheParametersTypesModuleShouldHandleSecureStringParametersCorrectly()
    {
        _parametersTypesConfiguration.Should().NotBeNull("Configuration should be built");
        
        // Verify that SecureString parameters are loaded (decrypted values should be accessible)
        var secureStringParams = _parametersTypesConfiguration!.AsEnumerable()
            .Where(kvp => kvp.Key.Contains("credentials"))
            .ToList();
            
        secureStringParams.Should().NotBeEmpty("Should have SecureString parameters loaded");
        
        foreach (var param in secureStringParams)
        {
            param.Value.Should().NotBeNull($"SecureString parameter '{param.Key}' should have a decrypted value");
            
            // If JSON processing is enabled, verify the value is JSON
            if (_jsonProcessingEnabled && param.Key.EndsWith("credentials"))
            {
                param.Value.Should().StartWith("{", "SecureString with JSON should contain JSON structure");
            }
            
            _parametersTypesValidationResults.Add($"✓ SecureString parameter handled: {param.Key}");
        }
    }

    [Then(@"the parameters types module should handle SecureString JSON processing correctly")]
    public void ThenTheParametersTypesModuleShouldHandleSecureStringJsonProcessingCorrectly()
    {
        _parametersTypesConfiguration.Should().NotBeNull("Configuration should be built");
        _jsonProcessingEnabled.Should().BeTrue("JSON processing should be enabled");
        
        // Verify that SecureString parameters with JSON are flattened
        var jsonProcessedParams = _parametersTypesConfiguration!.AsEnumerable()
            .Where(kvp => kvp.Key.Contains("credentials:"))
            .ToList();
            
        jsonProcessedParams.Should().NotBeEmpty("Should have JSON-processed SecureString parameters");
        
        // Should have keys like "infrastructure-module:database:credentials:username"
        var usernameKey = jsonProcessedParams.FirstOrDefault(kvp => kvp.Key.EndsWith(":username"));
        var passwordKey = jsonProcessedParams.FirstOrDefault(kvp => kvp.Key.EndsWith(":password"));
        
        usernameKey.Should().NotBeNull("Should have flattened username from JSON");
        passwordKey.Should().NotBeNull("Should have flattened password from JSON");
        
        _parametersTypesValidationResults.Add("✓ SecureString JSON processing handled correctly");
    }

    [Then(@"the parameters types module should handle mixed parameter types correctly")]
    public void ThenTheParametersTypesModuleShouldHandleMixedParameterTypesCorrectly()
    {
        _parametersTypesConfiguration.Should().NotBeNull("Configuration should be built");
        
        // Verify that all different parameter types are present
        var allParams = _parametersTypesConfiguration!.AsEnumerable().ToList();
        
        // Should have String parameters (simple key-value)
        var stringParams = allParams.Where(kvp => 
            kvp.Key.Contains("database:host") || kvp.Key.Contains("database:port")).ToList();
        stringParams.Should().NotBeEmpty("Should have String parameters");
        
        // Should have StringList parameters (indexed format)
        var stringListParams = allParams.Where(kvp => kvp.Key.Contains("allowed-origins:")).ToList();
        stringListParams.Should().NotBeEmpty("Should have StringList parameters");
        
        // Should have SecureString parameters
        var secureStringParams = allParams.Where(kvp => kvp.Key.Contains("credentials")).ToList();
        secureStringParams.Should().NotBeEmpty("Should have SecureString parameters");
        
        _parametersTypesValidationResults.Add($"✓ Mixed types handled: {stringParams.Count} String, {stringListParams.Count} StringList items, {secureStringParams.Count} SecureString");
    }

    [Then(@"the parameters types module should handle mixed types with JSON processing correctly")]
    public void ThenTheParametersTypesModuleShouldHandleMixedTypesWithJsonProcessingCorrectly()
    {
        _parametersTypesConfiguration.Should().NotBeNull("Configuration should be built");
        _jsonProcessingEnabled.Should().BeTrue("JSON processing should be enabled");
        
        var allParams = _parametersTypesConfiguration!.AsEnumerable().ToList();
        
        // String parameters should remain simple (unless they contain JSON)
        var stringParams = allParams.Where(kvp => 
            kvp.Key.Contains("database:host") || kvp.Key.Contains("database:port")).ToList();
        stringParams.Should().NotBeEmpty("Should have String parameters");
        
        // StringList parameters should be indexed (not affected by JSON processing)
        var stringListParams = allParams.Where(kvp => kvp.Key.Contains("allowed-origins:")).ToList();
        stringListParams.Should().NotBeEmpty("Should have StringList parameters in indexed format");
        
        // SecureString parameters with JSON should be flattened
        var flattenedSecureParams = allParams.Where(kvp => 
            kvp.Key.Contains("credentials:username") || kvp.Key.Contains("credentials:password")).ToList();
        flattenedSecureParams.Should().NotBeEmpty("Should have JSON-flattened SecureString parameters");
        
        _parametersTypesValidationResults.Add("✓ Mixed types with JSON processing handled correctly");
    }

    [Then(@"the parameters types module FlexConfig should provide dynamic access to ""(.*)""")]
    public void ThenTheParametersTypesModuleFlexConfigShouldProvideDynamicAccessTo(string configPath)
    {
        _parametersTypesFlexConfiguration.Should().NotBeNull("Parameters types FlexConfiguration should be built");

        dynamic config = _parametersTypesFlexConfiguration!;

        string value = AwsTestConfigurationBuilder.GetDynamicProperty(config, configPath);
        
        value.Should().NotBeNull($"Dynamic access to '{configPath}' should return a value");
        _parametersTypesValidationResults.Add($"✓ Dynamic access to {configPath} succeeded: {value}");
    }

    [Then(@"the parameters types module should provide dynamic access to different types")]
    public void ThenTheParametersTypesModuleShouldProvideDynamicAccessToDifferentTypes()
    {
        var dynamicAccessResults = _parametersTypesValidationResults.Where(result => 
            result.Contains("Dynamic access to")).ToList();
            
        dynamicAccessResults.Should().NotBeEmpty("Should have successful dynamic access results");
        dynamicAccessResults.Should().HaveCountGreaterThanOrEqualTo(2, "Should access multiple different parameter types");
        
        _parametersTypesValidationResults.Add("✓ Dynamic access to different parameter types verified");
    }

    [Then(@"the parameters types module configuration should be built successfully")]
    public void ThenTheParametersTypesModuleConfigurationShouldBeBuiltSuccessfully()
    {
        if (_lastParametersTypesException != null)
        {
            throw new Exception($"Configuration building failed with exception: {_lastParametersTypesException.Message}");
        }

        _parametersTypesConfiguration.Should().NotBeNull("Parameters types configuration should be built successfully");
        
        if (_parametersTypesFlexConfiguration != null)
        {
            _parametersTypesFlexConfiguration.Should().NotBeNull("Parameters types FlexConfiguration should be built successfully");
        }
    }

    [Then(@"the parameters types module should handle optional parameters gracefully")]
    public void ThenTheParametersTypesModuleShouldHandleOptionalParametersGracefully()
    {
        // Even with optional configuration, if test data is available, it should be loaded
        _parametersTypesConfiguration.Should().NotBeNull("Configuration should be built even when optional");
        
        var paramCount = _parametersTypesConfiguration!.AsEnumerable().Count();
        _parametersTypesValidationResults.Add($"✓ Optional parameter loading handled gracefully, loaded {paramCount} parameters");
    }

    #endregion
}