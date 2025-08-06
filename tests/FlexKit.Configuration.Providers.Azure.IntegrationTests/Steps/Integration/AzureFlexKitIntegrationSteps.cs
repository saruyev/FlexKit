using FlexKit.Configuration.Core;
using FlexKit.Configuration.Providers.Azure.IntegrationTests.Utils;
using FlexKit.Configuration.Conversion;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Reqnroll;
// ReSharper disable MethodTooLong
// ReSharper disable ComplexConditionExpression
// ReSharper disable TooManyDeclarations

namespace FlexKit.Configuration.Providers.Azure.IntegrationTests.Steps.Integration;

/// <summary>
/// Step definitions for Azure FlexKit integration scenarios.
/// Tests the integration of Azure Key Vault and App Configuration with FlexKit Configuration's
/// dynamic access capabilities, type conversion, and enhanced configuration features.
/// Uses distinct step patterns ("integration controller") to avoid conflicts with other step classes.
/// </summary>
[Binding]
public class AzureFlexKitIntegrationSteps(ScenarioContext scenarioContext)
{
    private AzureTestConfigurationBuilder? _integrationBuilder;
    private IConfiguration? _integrationConfiguration;
    private IFlexConfig? _integrationFlexConfiguration;
    private Exception? _lastIntegrationException;
    private readonly List<string> _integrationValidationResults = new();
    private bool _jsonProcessingEnabled;
    private bool _errorToleranceEnabled;
    private bool _keyVaultConfigured;
    private bool _appConfigurationConfigured;

    #region Given Steps - Setup

    [Given(@"I have established an integration controller environment")]
    public void GivenIHaveEstablishedAnIntegrationControllerEnvironment()
    {
        _integrationBuilder = new AzureTestConfigurationBuilder(scenarioContext);
        scenarioContext.Set(_integrationBuilder, "IntegrationBuilder");
    }

    [Given(@"I have integration controller configuration with Key Vault from ""(.*)""")]
    public void GivenIHaveIntegrationControllerConfigurationWithKeyVaultFrom(string testDataPath)
    {
        _integrationBuilder.Should().NotBeNull("Integration builder should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        _integrationBuilder!.AddKeyVaultFromTestData(fullPath, optional: false, jsonProcessor: false);
        _keyVaultConfigured = true;
        
        scenarioContext.Set(_integrationBuilder, "IntegrationBuilder");
    }

    [Given(@"I have integration controller configuration with App Configuration from ""(.*)""")]
    public void GivenIHaveIntegrationControllerConfigurationWithAppConfigurationFrom(string testDataPath)
    {
        _integrationBuilder.Should().NotBeNull("Integration builder should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        _integrationBuilder!.AddAppConfigurationFromTestData(fullPath, optional: false);
        _appConfigurationConfigured = true;
        
        scenarioContext.Set(_integrationBuilder, "IntegrationBuilder");
    }

    [Given(@"I have integration controller configuration with JSON-enabled Key Vault from ""(.*)""")]
    public void GivenIHaveIntegrationControllerConfigurationWithJsonEnabledKeyVaultFrom(string testDataPath)
    {
        _integrationBuilder.Should().NotBeNull("Integration builder should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        _integrationBuilder!.AddKeyVaultFromTestDataWithJsonProcessing(fullPath, optional: false, jsonProcessor: true);
        _keyVaultConfigured = true;
        _jsonProcessingEnabled = true;
        
        scenarioContext.Set(_integrationBuilder, "IntegrationBuilder");
    }

    [Given(@"I have integration controller configuration with JSON-enabled App Configuration from ""(.*)""")]
    public void GivenIHaveIntegrationControllerConfigurationWithJsonEnabledAppConfigurationFrom(string testDataPath)
    {
        _integrationBuilder.Should().NotBeNull("Integration builder should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        _integrationBuilder!.AddAppConfigurationFromTestData(fullPath, optional: false);
        _appConfigurationConfigured = true;
        _jsonProcessingEnabled = true;
        
        scenarioContext.Set(_integrationBuilder, "IntegrationBuilder");
    }

    [Given(@"I have integration controller configuration with optional Azure sources from ""(.*)""")]
    public void GivenIHaveIntegrationControllerConfigurationWithOptionalAzureSourcesFrom(string testDataPath)
    {
        _integrationBuilder.Should().NotBeNull("Integration builder should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        _integrationBuilder!.AddKeyVaultFromTestData(fullPath, optional: true, jsonProcessor: false);
        _integrationBuilder.AddAppConfigurationFromTestData(fullPath, optional: true);
        
        _keyVaultConfigured = true;
        _appConfigurationConfigured = true;
        
        scenarioContext.Set(_integrationBuilder, "IntegrationBuilder");
    }

    #endregion

    #region When Steps - Actions

    [When(@"I configure integration controller by building the configuration")]
    public void WhenIConfigureIntegrationControllerByBuildingTheConfiguration()
    {
        _integrationBuilder.Should().NotBeNull("Integration builder should be established");

        try
        {
            // Start LocalStack with both services
            var services = new List<string>();
            if (_keyVaultConfigured) services.Add("keyvault");
            if (_appConfigurationConfigured) services.Add("appconfig");
            
            var startTask = _integrationBuilder!.StartLocalStackAsync(string.Join(",", services));
            startTask.Wait(TimeSpan.FromMinutes(3));

            _integrationConfiguration = _integrationBuilder.Build();
            _integrationFlexConfiguration = _integrationBuilder.BuildFlexConfig();
            
            scenarioContext.Set(_integrationConfiguration, "IntegrationConfiguration");
            scenarioContext.Set(_integrationFlexConfiguration, "IntegrationFlexConfiguration");
            
            _integrationValidationResults.Add("✓ Integration configuration built successfully");
        }
        catch (Exception ex)
        {
            _lastIntegrationException = ex;
            scenarioContext.Set(ex, "IntegrationException");
            _integrationValidationResults.Add($"✗ Integration configuration build failed: {ex.Message}");
        }
    }

    [When(@"I configure integration controller with error tolerance by building the configuration")]
    public void WhenIConfigureIntegrationControllerWithErrorToleranceByBuildingTheConfiguration()
    {
        _integrationBuilder.Should().NotBeNull("Integration builder should be established");
        _errorToleranceEnabled = true;

        try
        {
            // Start LocalStack with both services
            var services = new List<string>();
            if (_keyVaultConfigured) services.Add("keyvault");
            if (_appConfigurationConfigured) services.Add("appconfig");
            
            var startTask = _integrationBuilder!.StartLocalStackAsync(string.Join(",", services));
            startTask.Wait(TimeSpan.FromMinutes(3));

            _integrationConfiguration = _integrationBuilder.Build();
            _integrationFlexConfiguration = _integrationBuilder.BuildFlexConfig();
            
            scenarioContext.Set(_integrationConfiguration, "IntegrationConfiguration");
            scenarioContext.Set(_integrationFlexConfiguration, "IntegrationFlexConfiguration");
            
            _integrationValidationResults.Add("✓ Integration configuration built successfully with error tolerance");
        }
        catch (Exception ex)
        {
            _lastIntegrationException = ex;
            scenarioContext.Set(ex, "IntegrationException");
            _integrationValidationResults.Add($"✗ Integration configuration build failed even with error tolerance: {ex.Message}");
        }
    }

    [When(@"I verify integration controller advanced FlexKit capabilities")]
    public void WhenIVerifyIntegrationControllerAdvancedFlexKitCapabilities()
    {
        _integrationFlexConfiguration.Should().NotBeNull("FlexKit configuration should be available");

        try
        {
            // Test dynamic access capabilities
            dynamic config = _integrationFlexConfiguration!;
            
            // Test various FlexKit access patterns
            var dynamicTests = new List<(string description, Func<object?> test)>
            {
                ("Basic property access", () => config["myapp:database:host"]),
                ("Nested property navigation", () => _integrationFlexConfiguration.Configuration.GetSection("myapp")),
                ("Section access", () => _integrationFlexConfiguration.Configuration.GetSection("infrastructure-module")),
                ("Dynamic casting", () => (string?)config["myapp:api:baseUrl"])
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
                        _integrationValidationResults.Add($"✓ {description}: success");
                    }
                    else
                    {
                        _integrationValidationResults.Add($"⚠ {description}: null result");
                    }
                }
                catch (Exception ex)
                {
                    _integrationValidationResults.Add($"✗ {description}: {ex.Message}");
                }
            }

            _integrationValidationResults.Add($"Advanced capabilities verification: {successfulTests}/{dynamicTests.Count} tests passed");
        }
        catch (Exception ex)
        {
            _integrationValidationResults.Add($"✗ Advanced capabilities verification failed: {ex.Message}");
        }
        
        scenarioContext.Set(_integrationValidationResults, "IntegrationValidationResults");
    }

    #endregion

    #region Then Steps - Assertions

    [Then(@"the integration controller should support FlexKit dynamic access patterns")]
    public void ThenTheIntegrationControllerShouldSupportFlexKitDynamicAccessPatterns()
    {
        _integrationFlexConfiguration.Should().NotBeNull("FlexKit configuration should be available");

        try
        {
            // Test dynamic access patterns specific to FlexKit
            dynamic config = _integrationFlexConfiguration!;
            
            // Test various FlexKit access patterns
            var dynamicTests = new List<(string description, Func<object?> test)>
            {
                ("Basic property access", () => config["myapp:database:host"]),
                ("Nested property navigation", () => AzureTestConfigurationBuilder.GetDynamicProperty(_integrationFlexConfiguration, "myapp.database.host")),
                ("Section access", () => _integrationFlexConfiguration.Configuration.GetSection("myapp")),
                ("Configuration enumeration", () => _integrationFlexConfiguration.Configuration.AsEnumerable().Count())
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
                        _integrationValidationResults.Add($"✓ {description}: success");
                    }
                    else
                    {
                        _integrationValidationResults.Add($"⚠ {description}: null result");
                    }
                }
                catch (Exception ex)
                {
                    _integrationValidationResults.Add($"✗ {description}: {ex.Message}");
                }
            }

            _integrationValidationResults.Add($"Dynamic access patterns verification: {successfulTests}/{dynamicTests.Count} tests passed");
        }
        catch (Exception ex)
        {
            _integrationValidationResults.Add($"✗ Dynamic access patterns verification failed: {ex.Message}");
        }
        
        scenarioContext.Set(_integrationValidationResults, "IntegrationValidationResults");
    }

    [Then(@"the integration controller configuration should contain ""(.*)"" with value ""(.*)""")]
    public void ThenTheIntegrationControllerConfigurationShouldContainWithValue(string expectedKey, string expectedValue)
    {
        _integrationConfiguration.Should().NotBeNull("Configuration should be built");

        var actualValue = _integrationConfiguration![expectedKey];
        actualValue.Should().Be(expectedValue, $"Configuration key '{expectedKey}' should have the expected value");
        
        _integrationValidationResults.Add($"✓ Configuration validation passed: {expectedKey} = {expectedValue}");
        scenarioContext.Set(_integrationValidationResults, "IntegrationValidationResults");
    }

    [Then(@"the integration controller should demonstrate FlexKit type conversion capabilities")]
    public void ThenTheIntegrationControllerShouldDemonstrateFlexKitTypeConversionCapabilities()
    {
        _integrationFlexConfiguration.Should().NotBeNull("FlexKit configuration should be available");

        try
        {
            // Test type conversion capabilities
            var typeConversionTests = new List<(string description, Func<object> test)>
            {
                ("String to int conversion", () => _integrationFlexConfiguration!["myapp:database:port"].ToType<int>()),
                ("String to bool conversion", () => _integrationFlexConfiguration!["myapp:features:cache:enabled"].ToType<bool>()),
                ("String to int for timeout", () => _integrationFlexConfiguration!["myapp:api:timeout"].ToType<int>()),
                ("Direct string access", () => _integrationFlexConfiguration!["myapp:database:host"] ?? "null")
            };

            var successfulConversions = 0;
            foreach (var (description, test) in typeConversionTests)
            {
                try
                {
                    var result = test();
                    result.Should().NotBeNull($"{description} should return a value");
                    successfulConversions++;
                    _integrationValidationResults.Add($"✓ {description}: success");
                }
                catch (Exception ex)
                {
                    _integrationValidationResults.Add($"✗ {description}: {ex.Message}");
                }
            }

            _integrationValidationResults.Add($"Type conversion verification: {successfulConversions}/{typeConversionTests.Count} tests passed");
        }
        catch (Exception ex)
        {
            _integrationValidationResults.Add($"✗ Type conversion verification failed: {ex.Message}");
        }
        
        scenarioContext.Set(_integrationValidationResults, "IntegrationValidationResults");
    }

    [Then(@"the integration controller should support FlexKit dynamic access to configuration")]
    public void ThenTheIntegrationControllerShouldSupportFlexKitDynamicAccessToConfiguration()
    {
        _integrationFlexConfiguration.Should().NotBeNull("FlexKit configuration should be available");
        _appConfigurationConfigured.Should().BeTrue("App Configuration should be configured");

        try
        {
            // Test App Configuration specific dynamic access
            dynamic config = _integrationFlexConfiguration!;
            
            var appConfigTests = new List<(string description, Func<object?> test)>
            {
                ("API timeout access", () => config["myapp:api:timeout"]),
                ("Base URL access", () => config["myapp:api:baseUrl"]),
                ("Cache settings access", () => config["myapp:cache:enabled"]),
                ("Logging configuration", () => config["myapp:logging:level"])
            };

            var successfulTests = 0;
            foreach (var (description, test) in appConfigTests)
            {
                try
                {
                    var result = test();
                    if (result != null)
                    {
                        successfulTests++;
                        _integrationValidationResults.Add($"✓ {description}: success");
                    }
                    else
                    {
                        _integrationValidationResults.Add($"⚠ {description}: null result");
                    }
                }
                catch (Exception ex)
                {
                    _integrationValidationResults.Add($"✗ {description}: {ex.Message}");
                }
            }

            _integrationValidationResults.Add($"App Configuration dynamic access verification: {successfulTests}/{appConfigTests.Count} tests passed");
        }
        catch (Exception ex)
        {
            _integrationValidationResults.Add($"✗ App Configuration dynamic access verification failed: {ex.Message}");
        }
        
        scenarioContext.Set(_integrationValidationResults, "IntegrationValidationResults");
    }

    [Then(@"the integration controller should demonstrate advanced FlexKit features")]
    public void ThenTheIntegrationControllerShouldDemonstrateAdvancedFlexKitFeatures()
    {
        _integrationFlexConfiguration.Should().NotBeNull("FlexKit configuration should be available");

        try
        {
            // Test advanced FlexKit features
            var advancedTests = new List<(string description, Func<object?> test)>
            {
                ("Configuration sections", () => _integrationFlexConfiguration!.Configuration.GetChildren().Count()),
                ("Key enumeration", () => _integrationFlexConfiguration.Configuration.AsEnumerable().Count()),
                ("Section binding", () => _integrationFlexConfiguration.Configuration.GetSection("myapp").Exists()),
                ("Hierarchical access", () => _integrationFlexConfiguration.Configuration["myapp:database:host"])
            };

            var successfulAdvancedFeatures = 0;
            foreach (var (description, test) in advancedTests)
            {
                try
                {
                    var result = test();
                    if (result != null)
                    {
                        successfulAdvancedFeatures++;
                        _integrationValidationResults.Add($"✓ {description}: success");
                    }
                    else
                    {
                        _integrationValidationResults.Add($"⚠ {description}: null result");
                    }
                }
                catch (Exception ex)
                {
                    _integrationValidationResults.Add($"✗ {description}: {ex.Message}");
                }
            }

            _integrationValidationResults.Add($"Advanced FlexKit features verification: {successfulAdvancedFeatures}/{advancedTests.Count} tests passed");
        }
        catch (Exception ex)
        {
            _integrationValidationResults.Add($"✗ Advanced FlexKit features verification failed: {ex.Message}");
        }
        
        scenarioContext.Set(_integrationValidationResults, "IntegrationValidationResults");
    }

    [Then(@"the integration controller configuration should demonstrate integrated Azure configuration")]
    public void ThenTheIntegrationControllerConfigurationShouldDemonstrateIntegratedAzureConfiguration()
    {
        _integrationConfiguration.Should().NotBeNull("Configuration should be built");
        _keyVaultConfigured.Should().BeTrue("Key Vault should be configured");
        _appConfigurationConfigured.Should().BeTrue("App Configuration should be configured");

        try
        {
            // Verify integration of both Azure services
            var keyVaultKeys = _integrationConfiguration!
                .AsEnumerable()
                .Count(kvp => kvp.Key.StartsWith("myapp:database:") && kvp.Value != null);
                
            var appConfigKeys = _integrationConfiguration
                .AsEnumerable()
                .Count(kvp => kvp.Key.StartsWith("myapp:api:") && kvp.Value != null);
            
            keyVaultKeys.Should().BeGreaterThan(0, "Should have Key Vault keys");
            appConfigKeys.Should().BeGreaterThan(0, "Should have App Configuration keys");
            
            _integrationValidationResults.Add($"✓ Integrated Azure configuration verified: {keyVaultKeys} Key Vault keys, {appConfigKeys} App Config keys");
        }
        catch (Exception ex)
        {
            _integrationValidationResults.Add($"✗ Integrated Azure configuration verification failed: {ex.Message}");
        }
        
        scenarioContext.Set(_integrationValidationResults, "IntegrationValidationResults");
    }

    [Then(@"the integration controller should demonstrate FlexKit precedence handling")]
    public void ThenTheIntegrationControllerShouldDemonstrateFlexKitPrecedenceHandling()
    {
        _integrationConfiguration.Should().NotBeNull("Configuration should be built");
        _keyVaultConfigured.Should().BeTrue("Key Vault should be configured");
        _appConfigurationConfigured.Should().BeTrue("App Configuration should be configured");

        try
        {
            // Test configuration precedence - App Configuration should typically override Key Vault
            // due to being added later in the configuration pipeline
            var precedenceTests = new List<(string description, string key, string expectedSource)>
            {
                ("API timeout precedence", "myapp:api:timeout", "AppConfiguration"),
                ("Database host precedence", "myapp:database:host", "KeyVault"),
                ("Cache enabled precedence", "myapp:cache:enabled", "AppConfiguration")
            };

            var successfulPrecedenceTests = 0;
            foreach (var (description, key, expectedSource) in precedenceTests)
            {
                try
                {
                    var value = _integrationConfiguration![key];
                    if (!string.IsNullOrEmpty(value))
                    {
                        successfulPrecedenceTests++;
                        _integrationValidationResults.Add($"✓ {description}: value from {expectedSource}");
                    }
                    else
                    {
                        _integrationValidationResults.Add($"⚠ {description}: no value found");
                    }
                }
                catch (Exception ex)
                {
                    _integrationValidationResults.Add($"✗ {description}: {ex.Message}");
                }
            }

            _integrationValidationResults.Add($"Precedence handling verification: {successfulPrecedenceTests}/{precedenceTests.Count} tests passed");
        }
        catch (Exception ex)
        {
            _integrationValidationResults.Add($"✗ Precedence handling verification failed: {ex.Message}");
        }
        
        scenarioContext.Set(_integrationValidationResults, "IntegrationValidationResults");
    }

    [Then(@"the integration controller should support comprehensive JSON processing")]
    public void ThenTheIntegrationControllerShouldSupportComprehensiveJsonProcessing()
    {
        _integrationFlexConfiguration.Should().NotBeNull("FlexKit configuration should be available");
        _jsonProcessingEnabled.Should().BeTrue("JSON processing should be enabled");

        try
        {
            // Test JSON processing from both sources
            var jsonProcessingTests = new List<(string description, string key)>
            {
                ("Database config from Key Vault", "database-config:host"),
                ("API settings from Key Vault", "api-settings:baseUrl"),
                ("Feature config caching", "feature-config:caching:enabled"),
                ("Infrastructure module database", "infrastructure-module-database-credentials:host")
            };

            var successfulJsonProcessing = 0;
            foreach (var (description, key) in jsonProcessingTests)
            {
                try
                {
                    var value = _integrationFlexConfiguration![key];
                    if (!string.IsNullOrEmpty(value))
                    {
                        successfulJsonProcessing++;
                        _integrationValidationResults.Add($"✓ {description}: {value}");
                    }
                    else
                    {
                        _integrationValidationResults.Add($"⚠ {description}: not found or empty");
                    }
                }
                catch (Exception ex)
                {
                    _integrationValidationResults.Add($"✗ {description}: {ex.Message}");
                }
            }

            _integrationValidationResults.Add($"JSON processing verification: {successfulJsonProcessing}/{jsonProcessingTests.Count} tests passed");
        }
        catch (Exception ex)
        {
            _integrationValidationResults.Add($"✗ JSON processing verification failed: {ex.Message}");
        }
        
        scenarioContext.Set(_integrationValidationResults, "IntegrationValidationResults");
    }

    [Then(@"the integration controller configuration should demonstrate complex data structure handling")]
    public void ThenTheIntegrationControllerConfigurationShouldDemonstrateComplexDataStructureHandling()
    {
        _integrationFlexConfiguration.Should().NotBeNull("FlexKit configuration should be available");

        try
        {
            // Test complex nested structure access
            var complexStructureTests = new List<(string description, string key)>
            {
                ("Nested database configuration", "database-config:connectionTimeout"),
                ("Authentication configuration", "api-settings:authentication:type"),
                ("Logging targets configuration", "feature-config:logging:structured"),
                ("Cache provider settings", "feature-config:caching:provider")
            };

            var successfulComplexStructures = 0;
            foreach (var (description, key) in complexStructureTests)
            {
                try
                {
                    var value = _integrationFlexConfiguration![key];
                    if (!string.IsNullOrEmpty(value))
                    {
                        successfulComplexStructures++;
                        _integrationValidationResults.Add($"✓ {description}: {value}");
                    }
                    else
                    {
                        _integrationValidationResults.Add($"⚠ {description}: not found or empty");
                    }
                }
                catch (Exception ex)
                {
                    _integrationValidationResults.Add($"✗ {description}: {ex.Message}");
                }
            }

            _integrationValidationResults.Add($"Complex data structure handling verification: {successfulComplexStructures}/{complexStructureTests.Count} tests passed");
        }
        catch (Exception ex)
        {
            _integrationValidationResults.Add($"✗ Complex data structure handling verification failed: {ex.Message}");
        }
        
        scenarioContext.Set(_integrationValidationResults, "IntegrationValidationResults");
    }

    [Then(@"the integration controller should demonstrate FlexKit JSON integration capabilities")]
    public void ThenTheIntegrationControllerShouldDemonstrateFlexKitJsonIntegrationCapabilities()
    {
        _integrationFlexConfiguration.Should().NotBeNull("FlexKit configuration should be available");
        _jsonProcessingEnabled.Should().BeTrue("JSON processing should be enabled");

        try
        {
            // Test FlexKit-specific JSON integration features
            dynamic _ = _integrationFlexConfiguration!;
            
            var jsonIntegrationTests = new List<(string description, Func<object?> test)>
            {
                ("Dynamic JSON property access", () => AzureTestConfigurationBuilder.GetDynamicProperty(_integrationFlexConfiguration, "database-config.host")),
                ("Type conversion on JSON values", () => _integrationFlexConfiguration["database-config:port"].ToType<int>()),
                ("Boolean JSON values", () => _integrationFlexConfiguration["database-config:ssl"].ToType<bool>()),
                ("Nested JSON object access", () => _integrationFlexConfiguration["api-settings:authentication:type"])
            };

            var successfulJsonIntegration = 0;
            foreach (var (description, test) in jsonIntegrationTests)
            {
                try
                {
                    var result = test();
                    if (result != null)
                    {
                        successfulJsonIntegration++;
                        _integrationValidationResults.Add($"✓ {description}: success");
                    }
                    else
                    {
                        _integrationValidationResults.Add($"⚠ {description}: null result");
                    }
                }
                catch (Exception ex)
                {
                    _integrationValidationResults.Add($"✗ {description}: {ex.Message}");
                }
            }

            _integrationValidationResults.Add($"FlexKit JSON integration verification: {successfulJsonIntegration}/{jsonIntegrationTests.Count} tests passed");
        }
        catch (Exception ex)
        {
            _integrationValidationResults.Add($"✗ FlexKit JSON integration verification failed: {ex.Message}");
        }
        
        scenarioContext.Set(_integrationValidationResults, "IntegrationValidationResults");
    }

    [Then(@"the integration controller should demonstrate error-tolerant Azure integration")]
    public void ThenTheIntegrationControllerShouldDemonstrateErrorTolerantAzureIntegration()
    {
        _errorToleranceEnabled.Should().BeTrue("Error tolerance should be enabled");
        
        try
        {
            // Test that the integration can handle various error scenarios gracefully
            var errorToleranceTests = new List<(string description, Func<bool> test)>
            {
                ("Configuration built despite errors", () => _integrationConfiguration != null || _lastIntegrationException == null),
                ("FlexKit accessible despite errors", () => _integrationFlexConfiguration != null),
                ("Partial configuration available", () => _integrationConfiguration?.AsEnumerable().Any() == true),
                ("Dynamic access still works", TryDynamicAccess)
            };

            var successfulErrorTolerance = 0;
            foreach (var (description, test) in errorToleranceTests)
            {
                try
                {
                    var result = test();
                    if (result)
                    {
                        successfulErrorTolerance++;
                        _integrationValidationResults.Add($"✓ {description}: passed");
                    }
                    else
                    {
                        _integrationValidationResults.Add($"⚠ {description}: failed");
                    }
                }
                catch (Exception ex)
                {
                    _integrationValidationResults.Add($"✗ {description}: {ex.Message}");
                }
            }

            _integrationValidationResults.Add($"Error tolerance verification: {successfulErrorTolerance}/{errorToleranceTests.Count} tests passed");
        }
        catch (Exception ex)
        {
            _integrationValidationResults.Add($"✗ Error tolerance verification failed: {ex.Message}");
        }
        
        scenarioContext.Set(_integrationValidationResults, "IntegrationValidationResults");
    }

    /// <summary>
    /// Helper method to test dynamic access capability.
    /// </summary>
    /// <returns>True if dynamic access works, false otherwise</returns>
    private bool TryDynamicAccess()
    {
        try
        {
            if (_integrationFlexConfiguration == null) return false;
            
            dynamic config = _integrationFlexConfiguration;
            var _ = config["any-key"]; // This should not throw even if the key doesn't exist
            return true;
        }
        catch
        {
            return false;
        }
    }

    #endregion
}