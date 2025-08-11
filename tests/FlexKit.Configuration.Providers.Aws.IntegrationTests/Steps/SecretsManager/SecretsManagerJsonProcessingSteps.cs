using FlexKit.Configuration.Core;
using FlexKit.Configuration.Providers.Aws.IntegrationTests.Utils;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Reqnroll;
// ReSharper disable ComplexConditionExpression
// ReSharper disable NullableWarningSuppressionIsUsed

namespace FlexKit.Configuration.Providers.Aws.IntegrationTests.Steps.SecretsManager;

/// <summary>
/// Step definitions for Secrets Manager JSON processing scenarios.
/// Tests automatic JSON secret processing, hierarchical key flattening,
/// complex object navigation, and dynamic access to JSON-processed configuration data.
/// Uses distinct step patterns ("secrets JSON processor") to avoid conflicts with other step classes.
/// </summary>
[Binding]
public class SecretsManagerJsonProcessingSteps(ScenarioContext scenarioContext)
{
    private AwsTestConfigurationBuilder? _secretsJsonBuilder;
    private IConfiguration? _secretsJsonConfiguration;
    private IFlexConfig? _secretsJsonFlexConfiguration;
    private Exception? _lastSecretsJsonException;
    private readonly List<string> _secretsJsonValidationResults = new();
    private bool _jsonProcessingEnabled;

    #region Given Steps - Setup

    [Given(@"I have established a secrets json processor environment")]
    public void GivenIHaveEstablishedASecretsJsonProcessorEnvironment()
    {
        _secretsJsonBuilder = new AwsTestConfigurationBuilder(scenarioContext);
        scenarioContext.Set(_secretsJsonBuilder, "SecretsJsonBuilder");
    }

    [Given(@"I have secrets json processor configuration with JSON processing from ""(.*)""")]
    public void GivenIHaveSecretsJsonProcessorConfigurationWithJsonProcessingFrom(string testDataPath)
    {
        _secretsJsonBuilder.Should().NotBeNull("Secrets json processor builder should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        _secretsJsonBuilder!.AddSecretsManagerFromTestData(fullPath, optional: false, jsonProcessor: true);
        _jsonProcessingEnabled = true;
        
        scenarioContext.Set(_secretsJsonBuilder, "SecretsJsonBuilder");
    }

    [Given(@"I have secrets json processor configuration with optional JSON processing from ""(.*)""")]
    public void GivenIHaveSecretsJsonProcessorConfigurationWithOptionalJsonProcessingFrom(string testDataPath)
    {
        _secretsJsonBuilder.Should().NotBeNull("Secrets json processor builder should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        _secretsJsonBuilder!.AddSecretsManagerFromTestData(fullPath, optional: true, jsonProcessor: true);
        _jsonProcessingEnabled = true;
        
        scenarioContext.Set(_secretsJsonBuilder, "SecretsJsonBuilder");
    }

    [Given(@"I have secrets json processor configuration with mixed secret types from ""(.*)""")]
    public void GivenIHaveSecretsJsonProcessorConfigurationWithMixedSecretTypesFrom(string testDataPath)
    {
        _secretsJsonBuilder.Should().NotBeNull("Secrets json processor builder should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        _secretsJsonBuilder!.AddSecretsManagerFromTestData(fullPath, optional: false, jsonProcessor: true);
        _jsonProcessingEnabled = true;
        
        scenarioContext.Set(_secretsJsonBuilder, "SecretsJsonBuilder");
        scenarioContext.Set("mixed_types_processing", "MixedTypesProcessing");
    }

    [Given(@"I have secrets json processor configuration with deep nesting from ""(.*)""")]
    public void GivenIHaveSecretsJsonProcessorConfigurationWithDeepNestingFrom(string testDataPath)
    {
        _secretsJsonBuilder.Should().NotBeNull("Secrets json processor builder should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        _secretsJsonBuilder!.AddSecretsManagerFromTestData(fullPath, optional: false, jsonProcessor: true);
        _jsonProcessingEnabled = true;
        
        scenarioContext.Set(_secretsJsonBuilder, "SecretsJsonBuilder");
        scenarioContext.Set("deep_nesting_processing", "DeepNestingProcessing");
    }

    [Given(@"I have secrets json processor configuration with invalid JSON tolerance from ""(.*)""")]
    public void GivenIHaveSecretsJsonProcessorConfigurationWithInvalidJsonToleranceFrom(string testDataPath)
    {
        _secretsJsonBuilder.Should().NotBeNull("Secrets json processor builder should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        _secretsJsonBuilder!.AddSecretsManagerFromTestData(fullPath, optional: true, jsonProcessor: true);
        _jsonProcessingEnabled = true;
        
        scenarioContext.Set(_secretsJsonBuilder, "SecretsJsonBuilder");
        scenarioContext.Set("error_tolerance_enabled", "ErrorToleranceEnabled");
    }

    #endregion

    #region When Steps - Configuration Processing

    [When(@"I configure secrets json processor by building the configuration")]
    public void WhenIConfigureSecretsJsonProcessorByBuildingTheConfiguration()
    {
        _secretsJsonBuilder.Should().NotBeNull("Secrets json processor builder should be established");

        try
        {
            _secretsJsonConfiguration = _secretsJsonBuilder!.Build();
            scenarioContext.Set(_secretsJsonConfiguration, "SecretsJsonConfiguration");
        }
        catch (Exception ex)
        {
            _lastSecretsJsonException = ex;
            scenarioContext.Set(ex, "LastSecretsJsonException");
            throw;
        }
    }

    [When(@"I create secrets json processor FlexConfig from the configuration")]
    public void WhenICreateSecretsJsonProcessorFlexConfigFromTheConfiguration()
    {
        _secretsJsonConfiguration.Should().NotBeNull("Secrets json processor configuration should be built");

        try
        {
            _secretsJsonFlexConfiguration = _secretsJsonBuilder!.BuildFlexConfig();
            scenarioContext.Set(_secretsJsonFlexConfiguration, "SecretsJsonFlexConfiguration");
        }
        catch (Exception ex)
        {
            _lastSecretsJsonException = ex;
            scenarioContext.Set(ex, "LastSecretsJsonFlexConfiguration");
            throw;
        }
    }

    [When(@"I verify secrets json processor JSON flattening capabilities")]
    public void WhenIVerifySecretsJsonProcessorJsonFlatteningCapabilities()
    {
        _secretsJsonConfiguration.Should().NotBeNull("Secrets json processor configuration should be built");

        try
        {
            var jsonTestKeys = new[]
            {
                "infrastructure-module:app:config:database:host",
                "infrastructure-module:app:config:cache:cluster:nodes:0:host",
                "infrastructure-module:services:config:microservices:0:name"
            };

            foreach (var key in jsonTestKeys)
            {
                try
                {
                    var value = _secretsJsonConfiguration![key];
                    _secretsJsonValidationResults.Add($"Successfully accessed '{key}': {value}");
                }
                catch (Exception ex)
                {
                    _secretsJsonValidationResults.Add($"Error accessing '{key}': {ex.Message}");
                }
            }
            
            scenarioContext.Set(_secretsJsonValidationResults, "SecretsJsonValidationResults");
        }
        catch (Exception ex)
        {
            _lastSecretsJsonException = ex;
            scenarioContext.Set(ex, "LastSecretsJsonException");
            throw;
        }
    }

    [When(@"I verify secrets json processor dynamic access patterns")]
    public void WhenIVerifySecretsJsonProcessorDynamicAccessPatterns()
    {
        _secretsJsonFlexConfiguration.Should().NotBeNull("Secrets json processor FlexConfig should be created");

        try
        {
            var dynamicTestPatterns = new[]
            {
                ("infrastructure_module.app.config.database.host", "dynamic property access"),
                ("infrastructure-module:app:config:database:port", "indexer access"),
                ("infrastructure_module.nonexistent.key", "missing key handling")
            };

            foreach (var (pattern, description) in dynamicTestPatterns)
            {
                try
                {
                    var value = AwsTestConfigurationBuilder.GetDynamicProperty(_secretsJsonFlexConfiguration!, pattern);
                    _secretsJsonValidationResults.Add($"Successfully tested {description} for '{pattern}': {value}");
                }
                catch (Exception ex)
                {
                    _secretsJsonValidationResults.Add($"Error in {description} for '{pattern}': {ex.Message}");
                }
            }
            
            scenarioContext.Set(_secretsJsonValidationResults, "SecretsJsonValidationResults");
        }
        catch (Exception ex)
        {
            _lastSecretsJsonException = ex;
            scenarioContext.Set(ex, "LastSecretsJsonException");
            throw;
        }
    }

    #endregion

    #region Then Steps - Verification

    [Then(@"the secrets json processor configuration should contain ""(.*)"" with value ""(.*)""")]
    public void ThenTheSecretsJsonProcessorConfigurationShouldContainWithValue(string configKey, string expectedValue)
    {
        _secretsJsonConfiguration.Should().NotBeNull("Secrets json processor configuration should be built");

        var actualValue = _secretsJsonConfiguration![configKey];
        actualValue.Should().Be(expectedValue, $"Configuration key '{configKey}' should have expected value");
    }

    [Then(@"the secrets json processor should process JSON secrets into hierarchical keys")]
    public void ThenTheSecretsJsonProcessorShouldProcessJsonSecretsIntoHierarchicalKeys()
    {
        _secretsJsonConfiguration.Should().NotBeNull("Secrets json processor configuration should be built");
        _jsonProcessingEnabled.Should().BeTrue("JSON processing should be enabled");

        // Verify that JSON secrets are flattened into hierarchical keys
        var hierarchicalKeys = _secretsJsonConfiguration!
            .AsEnumerable()
            .Where(kvp => kvp.Key.Contains(':'))
            .Select(kvp => kvp.Key)
            .ToList();

        hierarchicalKeys.Should().NotBeEmpty("JSON secrets should be processed into hierarchical keys");
        hierarchicalKeys.Should().Contain(key => key.Contains("infrastructure-module") && 
                                                 (key.Contains("config") || key.Contains("credentials") || key.Contains("api-keys")),
            "JSON secrets should contain infrastructure module hierarchical structure");
    }

    [Then(@"the secrets json processor should keep non-JSON secrets as plain string values")]
    public void ThenTheSecretsJsonProcessorShouldKeepNonJsonSecretsAsPlainStringValues()
    {
        _secretsJsonConfiguration.Should().NotBeNull("Secrets json processor configuration should be built");

        // Verify that non-JSON secrets remain as plain string values
        var plainStringKeys = _secretsJsonConfiguration!
            .AsEnumerable()
            .Where(kvp => !kvp.Key.Contains(':') || kvp.Value?.StartsWith("{") == false)
            .ToList();

        // At least some plain string values should exist
        plainStringKeys.Should().NotBeEmpty("Non-JSON secrets should be preserved as plain strings");
    }

    [Then(@"the secrets json processor FlexConfig should support dynamic property access for ""(.*)""")]
    public void ThenTheSecretsJsonProcessorFlexConfigShouldSupportDynamicPropertyAccessFor(string dynamicPath)
    {
        _secretsJsonFlexConfiguration.Should().NotBeNull("Secrets json processor FlexConfig should be created");

        var value = AwsTestConfigurationBuilder.GetDynamicProperty(_secretsJsonFlexConfiguration!, dynamicPath);
        value.Should().NotBeNull($"Dynamic property access should work for '{dynamicPath}'");
    }

    [Then(@"the secrets json processor FlexConfig should support indexer access for ""(.*)""")]
    public void ThenTheSecretsJsonProcessorFlexConfigShouldSupportIndexerAccessFor(string configKey)
    {
        _secretsJsonFlexConfiguration.Should().NotBeNull("Secrets json processor FlexConfig should be created");

        var value = _secretsJsonFlexConfiguration![configKey];
        value.Should().NotBeNull($"Indexer access should work for '{configKey}'");
    }

    [Then(@"the secrets json processor FlexConfig should handle missing secrets gracefully with default values")]
    public void ThenTheSecretsJsonProcessorFlexConfigShouldHandleMissingSecretsGracefullyWithDefaultValues()
    {
        _secretsJsonFlexConfiguration.Should().NotBeNull("Secrets json processor FlexConfig should be created");

        // Test access to a non-existent key
        var action = () => _secretsJsonFlexConfiguration!["nonexistent:secret:key"];
        action.Should().NotThrow("Missing secrets should be handled gracefully");

        var missingValue = _secretsJsonFlexConfiguration!["nonexistent:secret:key"];
        missingValue.Should().BeNull("Missing secrets should return null");
    }

    [Then(@"the secrets json processor FlexConfig should provide typed access to JSON-processed secret values")]
    public void ThenTheSecretsJsonProcessorFlexConfigShouldProvideTypedAccessToJsonProcessedSecretValues()
    {
        _secretsJsonFlexConfiguration.Should().NotBeNull("Secrets json processor FlexConfig should be created");

        // Test typed access for different data types that were JSON-processed
        var boolValue = _secretsJsonConfiguration!.GetValue<bool>("infrastructure-module:app:config:database:ssl");
        boolValue.Should().BeTrue("Boolean values should be accessible with typed access");

        var intValue = _secretsJsonConfiguration!.GetValue<int>("infrastructure-module:app:config:database:port");
        intValue.Should().Be(5432, "Integer values should be accessible with typed access");
    }

    [Then(@"the secrets json processor should complete loading without errors")]
    public void ThenTheSecretsJsonProcessorShouldCompleteLoadingWithoutErrors()
    {
        _secretsJsonConfiguration.Should().NotBeNull("Secrets json processor configuration should be built");
        _lastSecretsJsonException.Should().BeNull("No exceptions should occur during configuration loading");
    }

    [Then(@"the secrets json processor should apply JSON processing where enabled")]
    public void ThenTheSecretsJsonProcessorShouldApplyJsonProcessingWhereEnabled()
    {
        _secretsJsonConfiguration.Should().NotBeNull("Secrets json processor configuration should be built");
        _jsonProcessingEnabled.Should().BeTrue("JSON processing should be enabled");

        // Verify that JSON processing was applied by checking for hierarchical keys
        var processedKeys = _secretsJsonConfiguration!
            .AsEnumerable()
            .Where(kvp => kvp.Key.Contains(':') && (kvp.Key.Contains("config") || kvp.Key.Contains("credentials")))
            .ToList();

        processedKeys.Should().NotBeEmpty("JSON processing should create hierarchical keys for config data");
    }

    [Then(@"the secrets json processor should skip JSON processing for excluded secrets")]
    public void ThenTheSecretsJsonProcessorShouldSkipJsonProcessingForExcludedSecrets()
    {
        _secretsJsonConfiguration.Should().NotBeNull("Secrets json processor configuration should be built");

        // Verify that some secrets remain as plain strings (not JSON processed)
        var plainSecrets = _secretsJsonConfiguration!
            .AsEnumerable()
            .Where(kvp => !kvp.Key.Contains(':') && kvp.Value?.Length > 0)
            .ToList();

        // Should have at least some non-processed secrets
        plainSecrets.Should().NotBeEmpty("Some secrets should remain unprocessed as plain strings");
    }

    [Then(@"the secrets json processor configuration should provide consistent access patterns")]
    public void ThenTheSecretsJsonProcessorConfigurationShouldProvideConsistentAccessPatterns()
    {
        _secretsJsonConfiguration.Should().NotBeNull("Secrets json processor configuration should be built");

        // Get the first available hierarchical key from the actual configuration
        var availableKeys = _secretsJsonConfiguration!
            .AsEnumerable()
            .Where(kvp => kvp.Key.Contains(':') && !string.IsNullOrEmpty(kvp.Value))
            .ToList();

        availableKeys.Should().NotBeEmpty("Configuration should contain hierarchical keys");

        // Test with the first available key
        var testKey = availableKeys.First().Key;
    
        var directValue = _secretsJsonConfiguration![testKey];
        directValue.Should().NotBeNull($"Direct access should work for key '{testKey}'");

        // Extract the section and property parts for section-based access
        var keyParts = testKey.Split(':');
        if (keyParts.Length >= 2)
        {
            var sectionKey = string.Join(":", keyParts.Take(keyParts.Length - 1));
            var propertyKey = keyParts.Last();
        
            var sectionValue = _secretsJsonConfiguration!.GetSection(sectionKey)[propertyKey];
            sectionValue.Should().Be(directValue, "Section-based access should return the same value as direct access");
        }
    }

    [Then(@"the secrets json processor configuration should handle deep nesting correctly")]
    public void ThenTheSecretsJsonProcessorConfigurationShouldHandleDeepNestingCorrectly()
    {
        _secretsJsonConfiguration.Should().NotBeNull("Secrets json processor configuration should be built");

        // Verify deep nesting by checking for keys with multiple levels
        var deepKeys = _secretsJsonConfiguration!
            .AsEnumerable()
            .Where(kvp => kvp.Key.Split(':').Length >= 6) // At least 6 levels deep
            .ToList();

        deepKeys.Should().NotBeEmpty("Deep nesting should be handled correctly");
    }

    [Then(@"the secrets json processor configuration should flatten complex structures into accessible keys")]
    public void ThenTheSecretsJsonProcessorConfigurationShouldFlattenComplexStructuresIntoAccessibleKeys()
    {
        _secretsJsonConfiguration.Should().NotBeNull("Secrets json processor configuration should be built");

        // Test that complex nested structures are flattened and accessible
        var complexKeys = new[]
        {
            "infrastructure-module:app:config:database:pool:max",
            "infrastructure-module:app:config:database:pool:timeout"
        };

        foreach (var key in complexKeys)
        {
            var value = _secretsJsonConfiguration![key];
            value.Should().NotBeNull($"Complex nested key '{key}' should be accessible");
        }
    }

    [Then(@"the secrets json processor configuration should contain valid configuration data")]
    public void ThenTheSecretsJsonProcessorConfigurationShouldContainValidConfigurationData()
    {
        _secretsJsonConfiguration.Should().NotBeNull("Secrets json processor configuration should be built");

        // Verify that configuration contains some data
        var allKeys = _secretsJsonConfiguration!.AsEnumerable().ToList();
        allKeys.Should().NotBeEmpty("Configuration should contain some data");
        
        // Verify at least some keys have values
        var keysWithValues = allKeys.Where(kvp => !string.IsNullOrEmpty(kvp.Value)).ToList();
        keysWithValues.Should().NotBeEmpty("Configuration should contain keys with values");
    }

    #endregion
}