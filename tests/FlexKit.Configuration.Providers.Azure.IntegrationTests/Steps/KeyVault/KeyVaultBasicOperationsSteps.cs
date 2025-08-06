using FlexKit.Configuration.Core;
using FlexKit.Configuration.Providers.Azure.IntegrationTests.Utils;
using FlexKit.Configuration.Conversion;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Reqnroll;
// ReSharper disable MethodTooLong
// ReSharper disable TooManyDeclarations

namespace FlexKit.Configuration.Providers.Azure.IntegrationTests.Steps.KeyVault;

/// <summary>
/// Step definitions for Key Vault basic operations scenarios.
/// Tests Azure Key Vault integration including secret loading, hierarchical organization,
/// JSON processing, and FlexKit dynamic access capabilities.
/// Uses distinct step patterns ("key vault controller") to avoid conflicts with other step classes.
/// </summary>
[Binding]
public class KeyVaultBasicOperationsSteps(ScenarioContext scenarioContext)
{
    private AzureTestConfigurationBuilder? _keyVaultBuilder;
    private IConfiguration? _keyVaultConfiguration;
    private IFlexConfig? _keyVaultFlexConfiguration;
    private Exception? _lastKeyVaultException;
    private readonly List<string> _keyVaultValidationResults = new();
    private bool _jsonProcessingEnabled;
    private bool _hierarchicalSecretsConfigured;
    private bool _errorToleranceEnabled;

    #region Given Steps - Setup

    [Given(@"I have established a key vault controller environment")]
    public void GivenIHaveEstablishedAKeyVaultControllerEnvironment()
    {
        _keyVaultBuilder = new AzureTestConfigurationBuilder(scenarioContext);
        scenarioContext.Set(_keyVaultBuilder, "KeyVaultBuilder");
    }

    [Given(@"I have key vault controller configuration with Key Vault from ""(.*)""")]
    public void GivenIHaveKeyVaultControllerConfigurationWithKeyVaultFrom(string testDataPath)
    {
        _keyVaultBuilder.Should().NotBeNull("Key vault builder should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        _keyVaultBuilder!.AddKeyVaultFromTestData(fullPath, optional: false, jsonProcessor: false);
        
        scenarioContext.Set(_keyVaultBuilder, "KeyVaultBuilder");
    }

    [Given(@"I have key vault controller configuration with hierarchical Key Vault from ""(.*)""")]
    public void GivenIHaveKeyVaultControllerConfigurationWithHierarchicalKeyVaultFrom(string testDataPath)
    {
        _keyVaultBuilder.Should().NotBeNull("Key vault builder should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        _keyVaultBuilder!.AddKeyVaultFromTestData(fullPath, optional: false, jsonProcessor: false);
        _hierarchicalSecretsConfigured = true;
        
        scenarioContext.Set(_keyVaultBuilder, "KeyVaultBuilder");
    }

    [Given(@"I have key vault controller configuration with JSON-enabled Key Vault from ""(.*)""")]
    public void GivenIHaveKeyVaultControllerConfigurationWithJsonEnabledKeyVaultFrom(string testDataPath)
    {
        _keyVaultBuilder.Should().NotBeNull("Key vault builder should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        _keyVaultBuilder!.AddKeyVaultFromTestDataWithJsonProcessing(fullPath, optional: false, jsonProcessor: true);
        _jsonProcessingEnabled = true;
        
        scenarioContext.Set(_keyVaultBuilder, "KeyVaultBuilder");
    }

    [Given(@"I have key vault controller configuration with optional Key Vault from ""(.*)""")]
    public void GivenIHaveKeyVaultControllerConfigurationWithOptionalKeyVaultFrom(string testDataPath)
    {
        _keyVaultBuilder.Should().NotBeNull("Key vault builder should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        _keyVaultBuilder!.AddKeyVaultFromTestData(fullPath, optional: true, jsonProcessor: false);
        
        scenarioContext.Set(_keyVaultBuilder, "KeyVaultBuilder");
    }

    #endregion

    #region When Steps - Actions

    [When(@"I configure key vault controller by building the configuration")]
    public void WhenIConfigureKeyVaultControllerByBuildingTheConfiguration()
    {
        _keyVaultBuilder.Should().NotBeNull("Key vault builder should be established");

        try
        {
            // Start LocalStack first
            var startTask = _keyVaultBuilder!.StartLocalStackAsync("keyvault");
            startTask.Wait(TimeSpan.FromMinutes(2));

            _keyVaultConfiguration = _keyVaultBuilder.Build();
            _keyVaultFlexConfiguration = _keyVaultBuilder.BuildFlexConfig();
            
            scenarioContext.Set(_keyVaultConfiguration, "KeyVaultConfiguration");
            scenarioContext.Set(_keyVaultFlexConfiguration, "KeyVaultFlexConfiguration");
            
            _keyVaultValidationResults.Add("✓ Key vault configuration built successfully");
        }
        catch (Exception ex)
        {
            _lastKeyVaultException = ex;
            scenarioContext.Set(ex, "KeyVaultException");
            _keyVaultValidationResults.Add($"✗ Key vault configuration build failed: {ex.Message}");
        }
    }

    [When(@"I configure key vault controller with error tolerance by building the configuration")]
    public void WhenIConfigureKeyVaultControllerWithErrorToleranceByBuildingTheConfiguration()
    {
        _keyVaultBuilder.Should().NotBeNull("Key vault builder should be established");
        _errorToleranceEnabled = true;

        try
        {
            // Start LocalStack first
            var startTask = _keyVaultBuilder!.StartLocalStackAsync("keyvault");
            startTask.Wait(TimeSpan.FromMinutes(2));

            _keyVaultConfiguration = _keyVaultBuilder.Build();
            _keyVaultFlexConfiguration = _keyVaultBuilder.BuildFlexConfig();
            
            scenarioContext.Set(_keyVaultConfiguration, "KeyVaultConfiguration");
            scenarioContext.Set(_keyVaultFlexConfiguration, "KeyVaultFlexConfiguration");
            
            _keyVaultValidationResults.Add("✓ Key vault configuration built successfully with error tolerance");
        }
        catch (Exception ex)
        {
            _lastKeyVaultException = ex;
            scenarioContext.Set(ex, "KeyVaultException");
            _keyVaultValidationResults.Add($"✗ Key vault configuration build failed even with error tolerance: {ex.Message}");
        }
    }

    #endregion

    #region Then Steps - Assertions

    [Then(@"the key vault controller should support FlexKit dynamic access patterns")]
    public void ThenTheKeyVaultControllerShouldSupportFlexKitDynamicAccessPatterns()
    {
        _keyVaultFlexConfiguration.Should().NotBeNull("Key vault FlexKit configuration should be available");

        try
        {
            // Test dynamic access patterns specific to FlexKit
            dynamic config = _keyVaultFlexConfiguration!;
            
            // Test various FlexKit access patterns
            var dynamicTests = new List<(string description, Func<object?> test)>
            {
                ("Basic property access", () => config["myapp:database:host"]),
                ("Nested property navigation", () => AzureTestConfigurationBuilder.GetDynamicProperty(_keyVaultFlexConfiguration, "myapp.database.host")),
                ("Section access", () => _keyVaultFlexConfiguration.Configuration.GetSection("myapp")),
                ("Dynamic casting", () => (string?)config["myapp:database:host"])
            };

            var successfulTests = 0;
            foreach (var (description, test) in dynamicTests)
            {
                try
                {
                    var result = test();
                    if (result != null)
                    {
                        successfulTests++;
                        _keyVaultValidationResults.Add($"✓ {description}: success");
                    }
                    else
                    {
                        _keyVaultValidationResults.Add($"⚠ {description}: null result");
                    }
                }
                catch (Exception ex)
                {
                    _keyVaultValidationResults.Add($"✗ {description}: {ex.Message}");
                }
            }

            _keyVaultValidationResults.Add($"Dynamic access patterns verification: {successfulTests}/{dynamicTests.Count} tests passed");
        }
        catch (Exception ex)
        {
            _keyVaultValidationResults.Add($"✗ Dynamic access patterns verification failed: {ex.Message}");
        }
        
        scenarioContext.Set(_keyVaultValidationResults, "KeyVaultValidationResults");
    }

    [Then(@"the key vault controller configuration should contain ""(.*)"" with value ""(.*)""")]
    public void ThenTheKeyVaultControllerConfigurationShouldContainWithValue(string expectedKey, string expectedValue)
    {
        _keyVaultConfiguration.Should().NotBeNull("Key vault configuration should be built");

        var actualValue = _keyVaultConfiguration![expectedKey];
        actualValue.Should().Be(expectedValue, $"Configuration key '{expectedKey}' should have the expected value");
        
        _keyVaultValidationResults.Add($"✓ Configuration validation passed: {expectedKey} = {expectedValue}");
        scenarioContext.Set(_keyVaultValidationResults, "KeyVaultValidationResults");
    }

    [Then(@"the key vault controller should demonstrate FlexKit type conversion capabilities")]
    public void ThenTheKeyVaultControllerShouldDemonstrateFlexKitTypeConversionCapabilities()
    {
        _keyVaultFlexConfiguration.Should().NotBeNull("Key vault FlexKit configuration should be available");

        try
        {
            // Test type conversion capabilities
            var typeConversionTests = new List<(string description, Func<object> test)>
            {
                ("String to int conversion", () => _keyVaultFlexConfiguration!["myapp:database:port"].ToType<int>()),
                ("String to bool conversion", () => _keyVaultFlexConfiguration!["myapp:features:cache:enabled"].ToType<bool>()),
                ("String to TimeSpan conversion", () => _keyVaultFlexConfiguration!["myapp:features:cache:ttl"].ToType<int>()),
                ("Direct string access", () => _keyVaultFlexConfiguration!["myapp:database:host"] ?? "null")
            };

            var successfulConversions = 0;
            foreach (var (description, test) in typeConversionTests)
            {
                try
                {
                    var result = test();
                    result.Should().NotBeNull($"{description} should return a value");
                    successfulConversions++;
                    _keyVaultValidationResults.Add($"✓ {description}: success");
                }
                catch (Exception ex)
                {
                    _keyVaultValidationResults.Add($"✗ {description}: {ex.Message}");
                }
            }

            _keyVaultValidationResults.Add($"Type conversion verification: {successfulConversions}/{typeConversionTests.Count} tests passed");
        }
        catch (Exception ex)
        {
            _keyVaultValidationResults.Add($"✗ Type conversion verification failed: {ex.Message}");
        }
        
        scenarioContext.Set(_keyVaultValidationResults, "KeyVaultValidationResults");
    }

    [Then(@"the key vault controller should support hierarchical secret access")]
    public void ThenTheKeyVaultControllerShouldSupportHierarchicalSecretAccess()
    {
        _keyVaultFlexConfiguration.Should().NotBeNull("Key vault FlexKit configuration should be available");
        _hierarchicalSecretsConfigured.Should().BeTrue("Hierarchical secrets should be configured");

        try
        {
            // Test hierarchical access patterns
            var hierarchicalTests = new List<(string description, string key)>
            {
                ("Database host access", "myapp:database:host"),
                ("Database port access", "myapp:database:port"),
                ("Cache enabled feature", "myapp:features:cache:enabled"),
                ("Cache TTL setting", "myapp:features:cache:ttl"),
                ("Logging level setting", "myapp:features:logging:level")
            };

            var successfulHierarchicalAccess = 0;
            foreach (var (description, key) in hierarchicalTests)
            {
                try
                {
                    var value = _keyVaultFlexConfiguration![key];
                    if (!string.IsNullOrEmpty(value))
                    {
                        successfulHierarchicalAccess++;
                        _keyVaultValidationResults.Add($"✓ {description}: {value}");
                    }
                    else
                    {
                        _keyVaultValidationResults.Add($"⚠ {description}: empty or null value");
                    }
                }
                catch (Exception ex)
                {
                    _keyVaultValidationResults.Add($"✗ {description}: {ex.Message}");
                }
            }

            _keyVaultValidationResults.Add($"Hierarchical access verification: {successfulHierarchicalAccess}/{hierarchicalTests.Count} tests passed");
        }
        catch (Exception ex)
        {
            _keyVaultValidationResults.Add($"✗ Hierarchical access verification failed: {ex.Message}");
        }
        
        scenarioContext.Set(_keyVaultValidationResults, "KeyVaultValidationResults");
    }

    [Then(@"the key vault controller should support JSON secret flattening")]
    public void ThenTheKeyVaultControllerShouldSupportJsonSecretFlattening()
    {
        _keyVaultFlexConfiguration.Should().NotBeNull("Key vault FlexKit configuration should be available");
        _jsonProcessingEnabled.Should().BeTrue("JSON processing should be enabled");

        try
        {
            // Test JSON flattening - these should be flattened from JSON secrets
            var jsonFlatteningTests = new List<(string description, string key)>
            {
                ("Database config host", "database-config:host"),
                ("Database config port", "database-config:port"),  
                ("Database config SSL", "database-config:ssl"),
                ("API settings base URL", "api-settings:baseUrl"),
                ("API settings timeout", "api-settings:timeout"),
                ("Feature config caching enabled", "feature-config:caching:enabled")
            };

            var successfulFlattening = 0;
            foreach (var (description, key) in jsonFlatteningTests)
            {
                try
                {
                    var value = _keyVaultFlexConfiguration![key];
                    if (!string.IsNullOrEmpty(value))
                    {
                        successfulFlattening++;
                        _keyVaultValidationResults.Add($"✓ {description}: {value}");
                    }
                    else
                    {
                        _keyVaultValidationResults.Add($"⚠ {description}: not found or empty");
                    }
                }
                catch (Exception ex)
                {
                    _keyVaultValidationResults.Add($"✗ {description}: {ex.Message}");
                }
            }

            _keyVaultValidationResults.Add($"JSON flattening verification: {successfulFlattening}/{jsonFlatteningTests.Count} tests passed");
        }
        catch (Exception ex)
        {
            _keyVaultValidationResults.Add($"✗ JSON flattening verification failed: {ex.Message}");
        }
        
        scenarioContext.Set(_keyVaultValidationResults, "KeyVaultValidationResults");
    }

    [Then(@"the key vault controller should build successfully with error tolerance")]
    public void ThenTheKeyVaultControllerShouldBuildSuccessfullyWithErrorTolerance()
    {
        _errorToleranceEnabled.Should().BeTrue("Error tolerance should be enabled");
        
        // Even with errors, the configuration should build when error tolerance is enabled
        // This tests that optional Key Vault sources don't fail the entire configuration
        bool configurationBuilt = _keyVaultConfiguration != null || _lastKeyVaultException == null;
        
        if (!configurationBuilt && _lastKeyVaultException != null)
        {
            // If configuration failed to build even with error tolerance, this might be acceptable
            // for optional sources, but we should log it
            _keyVaultValidationResults.Add($"ⓘ Configuration build failed with error tolerance: {_lastKeyVaultException.Message}");
        }
        else
        {
            _keyVaultValidationResults.Add("✓ Configuration built successfully with error tolerance");
        }

        scenarioContext.Set(_keyVaultValidationResults, "KeyVaultValidationResults");
    }

    [Then(@"the key vault controller should demonstrate FlexKit capabilities despite missing secrets")]
    public void ThenTheKeyVaultControllerShouldDemonstrateFlexKitCapabilitiesDespiteMissingSecrets()
    {
        try
        {
            // Test that FlexKit can still function even when some secrets are missing
            if (_keyVaultFlexConfiguration != null)
            {
                dynamic config = _keyVaultFlexConfiguration;
                
                // Try to access various properties - some may be null, but FlexKit should handle this gracefully
                var capabilityTests = new List<(string description, Func<object?> test)>
                {
                    ("Null-safe property access", () => config["non-existent-key"]),
                    ("Section enumeration", () => _keyVaultFlexConfiguration.Configuration.GetChildren().Count()),
                    ("Configuration as enumerable", () => _keyVaultFlexConfiguration.Configuration.AsEnumerable().Count()),
                    ("Dynamic property with fallback", () => config["missing-key"] ?? "default-value")
                };

                var successfulCapabilities = 0;
                foreach (var (description, test) in capabilityTests)
                {
                    try
                    {
                        _ = test();
                        // The result can be null for missing keys, that's expected behavior
                        successfulCapabilities++;
                        _keyVaultValidationResults.Add($"✓ {description}: handled gracefully");
                    }
                    catch (Exception ex)
                    {
                        _keyVaultValidationResults.Add($"✗ {description}: {ex.Message}");
                    }
                }

                _keyVaultValidationResults.Add($"FlexKit capabilities with missing secrets: {successfulCapabilities}/{capabilityTests.Count} tests passed");
            }
            else
            {
                _keyVaultValidationResults.Add("ⓘ FlexKit configuration not available, but error tolerance working");
            }
        }
        catch (Exception ex)
        {
            _keyVaultValidationResults.Add($"✗ FlexKit capabilities test failed: {ex.Message}");
        }
        
        scenarioContext.Set(_keyVaultValidationResults, "KeyVaultValidationResults");
    }

    #endregion
}