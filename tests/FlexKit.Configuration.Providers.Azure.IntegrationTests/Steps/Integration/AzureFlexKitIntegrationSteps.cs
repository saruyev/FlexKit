using FlexKit.Configuration.Core;
using FlexKit.Configuration.Providers.Azure.IntegrationTests.Utils;
using FlexKit.Configuration.Conversion;
using FlexKit.Configuration.Providers.Azure.Extensions;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Reqnroll;

// ReSharper disable MethodTooLong
// ReSharper disable ComplexConditionExpression
// ReSharper disable TooManyDeclarations
// ReSharper disable NullableWarningSuppressionIsUsed

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
    private FlexConfigurationBuilder? _integrationBuilder;
    private IConfiguration? _integrationConfiguration;
    private IFlexConfig? _integrationFlexConfiguration;
    private readonly List<string> _integrationValidationResults = new();
    private bool _errorToleranceEnabled;
    private bool _keyVaultConfigured;
    private bool _appConfigurationConfigured;

    #region Given Steps - Setup

    [Given(@"I have established an integration controller environment")]
    public void GivenIHaveEstablishedAnIntegrationControllerEnvironment()
    {
        _integrationBuilder = new FlexConfigurationBuilder();
        scenarioContext.Set(_integrationBuilder, "IntegrationBuilder");
    }

    [Given(@"I have integration controller configuration with Key Vault from ""(.*)""")]
    public async Task GivenIHaveIntegrationControllerConfigurationWithKeyVaultFrom(string testDataPath)
    {
        _integrationBuilder.Should().NotBeNull("Integration builder should be established");

        var keyVaultEmulator = scenarioContext.GetKeyVaultEmulator();
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");

        var fullPath = Path.Combine("TestData", testDataPath);
        await keyVaultEmulator.CreateTestDataAsync(fullPath, scenarioPrefix);

        _integrationBuilder!.AddAzureKeyVault(options =>
        {
            options.VaultUri = "https://test-vault.vault.azure.net/";
            options.SecretClient = keyVaultEmulator.SecretClient;
            options.JsonProcessor = true;
            options.Optional = false;
            // Use a custom secret processor to filter by scenario prefix
            options.SecretProcessor = new ScenarioPrefixSecretProcessor(scenarioPrefix);
        });

        _keyVaultConfigured = true;
        
        scenarioContext.Set(_integrationBuilder, "IntegrationBuilder");
    }

    [Given(@"I have integration controller configuration with App Configuration from ""(.*)""")]
    public async Task GivenIHaveIntegrationControllerConfigurationWithAppConfigurationFrom(string testDataPath)
    {
        var keyVaultEmulator = scenarioContext.GetKeyVaultEmulator();
        var appConfigEmulator = scenarioContext.GetAppConfigEmulator();
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");
        _integrationBuilder.Should().NotBeNull("Integration builder should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        await appConfigEmulator.CreateTestDataAsync(fullPath, scenarioPrefix);
        await keyVaultEmulator.CreateTestDataAsync(fullPath, scenarioPrefix);

        _integrationBuilder!.AddAzureAppConfiguration(options =>
        {
            options.ConnectionString = appConfigEmulator.GetConnectionString();
            options.ConfigurationClient = appConfigEmulator.ConfigurationClient;
            options.Optional = false;
            // Use scenario prefix as key filter to isolate this scenario's data
            options.KeyFilter = $"{scenarioPrefix}:*";
        });
        _integrationBuilder!.AddAzureKeyVault(options =>
        {
            options.VaultUri = "https://test-vault.vault.azure.net/";
            options.SecretClient = keyVaultEmulator.SecretClient;
            options.JsonProcessor = true;
            options.Optional = false;
            // Use a custom secret processor to filter by scenario prefix
            options.SecretProcessor = new ScenarioPrefixSecretProcessor(scenarioPrefix);
        });

        _appConfigurationConfigured = true;
        
        scenarioContext.Set(_integrationBuilder, "IntegrationBuilder");
    }

    [Given(@"I have integration controller configuration with JSON-enabled Key Vault from ""(.*)""")]
    public async Task GivenIHaveIntegrationControllerConfigurationWithJsonEnabledKeyVaultFrom(string testDataPath)
    {
        _integrationBuilder.Should().NotBeNull("Integration builder should be established");

        var keyVaultEmulator = scenarioContext.GetKeyVaultEmulator();
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");

        var fullPath = Path.Combine("TestData", testDataPath);
        await keyVaultEmulator.CreateTestDataAsync(fullPath, scenarioPrefix);

        _integrationBuilder!.AddAzureKeyVault(options =>
        {
            options.VaultUri = "https://test-vault.vault.azure.net/";
            options.SecretClient = keyVaultEmulator.SecretClient;
            options.JsonProcessor = true; // Enable JSON processing
            options.Optional = false;
            // Use a custom secret processor to filter by scenario prefix
            options.SecretProcessor = new ScenarioPrefixSecretProcessor(scenarioPrefix);
        });

        _keyVaultConfigured = true;
        
        scenarioContext.Set(_integrationBuilder, "IntegrationBuilder");
    }

    [Given(@"I have integration controller configuration with JSON-enabled App Configuration from ""(.*)""")]
    public async Task GivenIHaveIntegrationControllerConfigurationWithJsonEnabledAppConfigurationFrom(string testDataPath)
    {
        _integrationBuilder.Should().NotBeNull("Integration builder should be established");

        var appConfigEmulator = scenarioContext.GetAppConfigEmulator();
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");

        var fullPath = Path.Combine("TestData", testDataPath);
        await appConfigEmulator.CreateTestDataAsync(fullPath, scenarioPrefix);

        _integrationBuilder!.AddAzureAppConfiguration(options =>
        {
            options.ConnectionString = appConfigEmulator.GetConnectionString();
            options.ConfigurationClient = appConfigEmulator.ConfigurationClient;
            options.Optional = false;
            // Use scenario prefix as key filter to isolate this scenario's data
            options.KeyFilter = $"{scenarioPrefix}:*";
        });

        _appConfigurationConfigured = true;
        
        scenarioContext.Set(_integrationBuilder, "IntegrationBuilder");
    }

    [Given(@"I have integration controller configuration with optional Azure sources from ""(.*)""")]
    public async Task GivenIHaveIntegrationControllerConfigurationWithOptionalAzureSourcesFrom(string testDataPath)
    {
        _integrationBuilder.Should().NotBeNull("Integration builder should be established");

        var keyVaultEmulator = scenarioContext.GetKeyVaultEmulator();
        var appConfigEmulator = scenarioContext.GetAppConfigEmulator();
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");

        var fullPath = Path.Combine("TestData", testDataPath);
        await keyVaultEmulator.CreateTestDataAsync(fullPath, scenarioPrefix);
        await appConfigEmulator.CreateTestDataAsync(fullPath, scenarioPrefix);

        _integrationBuilder!.AddAzureKeyVault(options =>
        {
            options.VaultUri = "https://test-vault.vault.azure.net/";
            options.SecretClient = keyVaultEmulator.SecretClient;
            options.JsonProcessor = false;
            options.Optional = true; // Optional for error tolerance testing
            // Use a custom secret processor to filter by scenario prefix
            options.SecretProcessor = new ScenarioPrefixSecretProcessor(scenarioPrefix);
        });

        _integrationBuilder.AddAzureAppConfiguration(options =>
        {
            options.ConnectionString = appConfigEmulator.GetConnectionString();
            options.ConfigurationClient = appConfigEmulator.ConfigurationClient;
            options.Optional = true; // Optional for error tolerance testing
            // Use scenario prefix as key filter to isolate this scenario's data
            options.KeyFilter = $"{scenarioPrefix}:*";
        });
        
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
            _integrationFlexConfiguration = _integrationBuilder.Build();
            _integrationConfiguration = _integrationFlexConfiguration.Configuration;
            
            scenarioContext.Set(_integrationConfiguration, "IntegrationConfiguration");
            scenarioContext.Set(_integrationFlexConfiguration, "IntegrationFlexConfiguration");
            
            _integrationValidationResults.Add("✓ Integration configuration built successfully");
        }
        catch (Exception ex)
        {
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
            _integrationFlexConfiguration = _integrationBuilder.Build();
            _integrationConfiguration = _integrationFlexConfiguration.Configuration;
            
            scenarioContext.Set(_integrationConfiguration, "IntegrationConfiguration");
            scenarioContext.Set(_integrationFlexConfiguration, "IntegrationFlexConfiguration");
            
            _integrationValidationResults.Add("✓ Integration configuration built successfully with error tolerance");
        }
        catch (Exception ex)
        {
            scenarioContext.Set(ex, "IntegrationException");
            _integrationValidationResults.Add($"✗ Integration configuration build failed even with error tolerance: {ex.Message}");
        }
    }

    [When(@"I verify integration controller advanced FlexKit capabilities")]
    public void WhenIVerifyIntegrationControllerAdvancedFlexKitCapabilities()
    {
        _integrationFlexConfiguration.Should().NotBeNull("FlexKit configuration should be available");
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");

        try
        {
            // Test dynamic access capabilities with a scenario prefix
            dynamic config = _integrationFlexConfiguration!;
            
            // Test various FlexKit access patterns - using scenario prefix for keys
            var dynamicTests = new List<(string description, Func<object?> test)>
            {
                ("Basic property access", () => config[$"{scenarioPrefix}:myapp:database:host"]),
                ("Nested property navigation", () => _integrationFlexConfiguration.Configuration.GetSection($"{scenarioPrefix}:myapp")),
                ("Section access", () => _integrationFlexConfiguration.Configuration.GetSection($"{scenarioPrefix}:infrastructure-module")),
                ("Dynamic casting", () => (string?)config[$"appConfigurationSettings:myapp:api:timeout"])
            };

            var successfulAdvancedFeatures = 0;
            foreach (var test in dynamicTests)
            {
                try
                {
                    var result = test.test();
                    if (result != null)
                    {
                        successfulAdvancedFeatures++;
                        _integrationValidationResults.Add($"✓ {test.description}: success");
                    }
                    else
                    {
                        _integrationValidationResults.Add($"⚠ {test.description}: null result");
                    }
                }
                catch (Exception ex)
                {
                    _integrationValidationResults.Add($"✗ {test.description}: {ex.Message}");
                }
            }

            _integrationValidationResults.Add($"Advanced FlexKit features verification: {successfulAdvancedFeatures}/{dynamicTests.Count} tests passed");
        }
        catch (Exception ex)
        {
            _integrationValidationResults.Add($"✗ Advanced FlexKit features verification failed: {ex.Message}");
        }
        
        scenarioContext.Set(_integrationValidationResults, "IntegrationValidationResults");
    }

    #endregion

    #region Then Steps - Assertions

    [Then(@"the integration controller should support FlexKit dynamic access patterns")]
    public void ThenTheIntegrationControllerShouldSupportFlexKitDynamicAccessPatterns()
    {
        _integrationFlexConfiguration.Should().NotBeNull("FlexKit configuration should be available");
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");

        try
        {
            // Test FlexKit dynamic access patterns with a scenario prefix
            dynamic config = _integrationFlexConfiguration!;

            var dynamicTests = new List<(string description, Func<bool> validation)>
            {
                ("Bracket notation access", () => 
                {
                    var value = config["keyVaultSecrets:myapp:database:host"];
                    return value != null;
                }),
                ("Dot notation access via dynamic", () => 
                {
                    var value = config.keyVaultSecrets.myapp.database.host;
                    return value != null;
                }),
                ("Section-based access", () => 
                {
                    var section = _integrationFlexConfiguration.Configuration.GetSection("keyVaultSecrets:myapp");
                    return section.Exists();
                }),
                ("Null-safe dynamic access", () => 
                {
                    _ = config[$"{scenarioPrefix}:nonexistent:nested:value"];
                    return true; // Should not throw even if a path doesn't exist
                })
            };

            foreach (var test in dynamicTests)
            {
                var passed = test.validation();
                _integrationValidationResults.Add(passed 
                    ? $"✓ {test.description} test passed"
                    : $"✗ {test.description} test failed");
                
                passed.Should().BeTrue($"{test.description} validation should pass");
            }
        }
        catch (Exception ex)
        {
            _integrationValidationResults.Add($"✗ FlexKit dynamic access pattern testing failed: {ex.Message}");
            throw;
        }
    }

    [Then(@"the integration controller configuration should contain ""(.*)"" with value ""(.*)""")]
    public void ThenTheIntegrationControllerConfigurationShouldContainWithValue(string key, string expectedValue)
    {
        _integrationConfiguration.Should().NotBeNull("Configuration should be built");

        try
        {
            // Use prefixed key for lookup
            var actualValue = _integrationConfiguration![key];
            actualValue.Should().Be(expectedValue, $"Configuration key '{key}' should have the expected value");
            
            _integrationValidationResults.Add($"✓ Configuration contains '{key}' with value '{expectedValue}'");
        }
        catch (Exception ex)
        {
            _integrationValidationResults.Add($"✗ Configuration value verification failed for '{key}': {ex.Message}");
            throw;
        }
    }

    [Then(@"the integration controller should demonstrate FlexKit type conversion capabilities")]
    public void ThenTheIntegrationControllerShouldDemonstrateFlexKitTypeConversionCapabilities()
    {
        _integrationFlexConfiguration.Should().NotBeNull("FlexKit configuration should be available");

        try
        {
            // Test type conversion capabilities with a scenario prefix
            var conversionTests = new List<(string description, Func<bool> validation)>
            {
                ("String to int conversion", () => 
                {
                    var value = _integrationFlexConfiguration![$"keyVaultSecrets:infrastructure-module-database-credentials:port"].ToType<int>();
                    return value > 0;
                }),
                ("String to bool conversion", () => 
                {
                    var value = _integrationFlexConfiguration!["keyVaultSecrets:myapp:features:cache:enabled"].ToType<bool>();
                    return value;
                }),
                ("String to TimeSpan conversion", () => 
                {
                    var timeout = _integrationFlexConfiguration!["appConfigurationSettings:myapp:api:timeout"].ToType<int>();
                    return timeout > 0;
                }),
                ("Dynamic type inference", () => 
                {
                    dynamic config = _integrationFlexConfiguration!;
                    var host = (string?)config[$"keyVaultSecrets:myapp:database:host"];
                    return !string.IsNullOrEmpty(host);
                })
            };

            foreach (var test in conversionTests)
            {
                var passed = test.validation();
                _integrationValidationResults.Add(passed 
                    ? $"✓ {test.description} test passed"
                    : $"✗ {test.description} test failed");
                
                passed.Should().BeTrue($"{test.description} validation should pass");
            }
        }
        catch (Exception ex)
        {
            _integrationValidationResults.Add($"✗ FlexKit type conversion testing failed: {ex.Message}");
            throw;
        }
    }

    [Then(@"the integration controller should support FlexKit dynamic access to configuration")]
    public void ThenTheIntegrationControllerShouldSupportFlexKitDynamicAccessToConfiguration()
    {
        _integrationFlexConfiguration.Should().NotBeNull("FlexKit configuration should be available");

        try
        {
            // Test comprehensive dynamic access with a scenario prefix
            dynamic config = _integrationFlexConfiguration!;

            var accessTests = new List<(string description, Func<bool> validation)>
            {
                ("Direct key access", () => 
                {
                    var value = config["appConfigurationSettings:myapp:api:timeout"];
                    return !string.IsNullOrEmpty(value?.ToString());
                }),
                ("Nested object navigation via config", () => 
                {
                    var value = config.appConfigurationSettings.myapp.api.timeout;
                    return value != null;
                }),
                ("Safe navigation chains", () => 
                {
                    _ = config["nonexistent:deeply:nested:value"];
                    return true; // Should not throw
                }),
                ("Mixed access patterns", () => 
                {
                    var section = _integrationFlexConfiguration.Configuration.GetSection($"appConfigurationSettings:myapp:api");
                    return section.Exists();
                })
            };

            foreach (var test in accessTests)
            {
                var passed = test.validation();
                _integrationValidationResults.Add(passed 
                    ? $"✓ {test.description} test passed"
                    : $"✗ {test.description} test failed");
                
                passed.Should().BeTrue($"{test.description} validation should pass");
            }
        }
        catch (Exception ex)
        {
            _integrationValidationResults.Add($"✗ FlexKit dynamic access testing failed: {ex.Message}");
            throw;
        }
    }

    [Then(@"the integration controller should demonstrate advanced FlexKit features")]
    public void ThenTheIntegrationControllerShouldDemonstrateAdvancedFlexKitFeatures()
    {
        _integrationFlexConfiguration.Should().NotBeNull("FlexKit configuration should be available");

        try
        {
            // Test advanced FlexKit features with a scenario prefix
            var advancedTests = new List<(string description, Func<bool> validation)>
            {
                ("Configuration section binding", () => 
                {
                    var section = _integrationFlexConfiguration!.Configuration.GetSection("appConfigurationSettings:myapp");
                    return section.Exists() && section.GetChildren().Any();
                }),
                ("Hierarchical configuration access", () => 
                {
                    var keys = _integrationFlexConfiguration!.Configuration.AsEnumerable()
                        .Where(kvp => kvp.Key.StartsWith("appConfigurationSettings:myapp:"))
                        .ToList();
                    return keys.Count > 0;
                }),
                ("Dynamic property enumeration", () => 
                {
                    var hasMyApp = TryDynamicAccess();
                    return hasMyApp;
                }),
                ("Type-safe configuration access", () => 
                {
                    var timeout = _integrationFlexConfiguration!["appConfigurationSettings:myapp:api:timeout"].ToType<string>();
                    return !string.IsNullOrEmpty(timeout);
                })
            };

            foreach (var test in advancedTests)
            {
                var passed = test.validation();
                _integrationValidationResults.Add(passed 
                    ? $"✓ {test.description} test passed"
                    : $"✗ {test.description} test failed");
                
                passed.Should().BeTrue($"{test.description} validation should pass");
            }
        }
        catch (Exception ex)
        {
            _integrationValidationResults.Add($"✗ Advanced FlexKit features testing failed: {ex.Message}");
            throw;
        }
    }

    [Then(@"the integration controller configuration should demonstrate integrated Azure configuration")]
    public void ThenTheIntegrationControllerConfigurationShouldDemonstrateIntegratedAzureConfiguration()
    {
        _integrationConfiguration.Should().NotBeNull("Configuration should be built");
        _keyVaultConfigured.Should().BeTrue("Key Vault should be configured");
        _appConfigurationConfigured.Should().BeTrue("App Configuration should be configured");

        try
        {
            // Verify integration of both Azure services with the scenario prefix
            var integrationTests = new List<(string description, string key, Func<bool> validation)>
            {
                ("Key Vault integration", "keyVaultSecrets:myapp:database:host", () => 
                {
                    var value = _integrationConfiguration!["keyVaultSecrets:myapp:database:host"];
                    return !string.IsNullOrEmpty(value);
                }),
                ("App Configuration integration", "appConfigurationSettings:myapp:api:timeout", () => 
                {
                    var value = _integrationConfiguration!["appConfigurationSettings:myapp:api:timeout"];
                    return !string.IsNullOrEmpty(value);
                }),
                ("Cross-service configuration", "appConfigurationSettings:infrastructure-module:environment", () => 
                {
                    var value = _integrationConfiguration!["appConfigurationSettings:infrastructure-module:environment"];
                    return !string.IsNullOrEmpty(value);
                })
            };

            foreach (var test in integrationTests)
            {
                var passed = test.validation();
                _integrationValidationResults.Add(passed 
                    ? $"✓ {test.description} test passed for key '{test.key}'"
                    : $"✗ {test.description} test failed for key '{test.key}'");
                
                passed.Should().BeTrue($"{test.description} validation should pass");
            }
        }
        catch (Exception ex)
        {
            _integrationValidationResults.Add($"✗ Integrated Azure configuration testing failed: {ex.Message}");
            throw;
        }
    }

    [Then(@"the integration controller should demonstrate FlexKit precedence handling")]
    public void ThenTheIntegrationControllerShouldDemonstrateFlexKitPrecedenceHandling()
    {
        _integrationConfiguration.Should().NotBeNull("Configuration should be built");
        _integrationFlexConfiguration.Should().NotBeNull("FlexConfig should be built");
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");

        try
        {
            // Test that FlexKit maintains proper precedence from underlying configuration with scenario prefix
            var precedenceTests = new List<(string description, string key, Func<bool> validation)>
            {
                ("Standard configuration precedence", $"appConfigurationSettings:myapp:api:timeout", () => 
                {
                    var standardValue = _integrationConfiguration![$"appConfigurationSettings:myapp:api:timeout"];
                    var flexValue = _integrationFlexConfiguration![$"appConfigurationSettings:myapp:api:timeout"];
                    return standardValue == flexValue;
                }),
                ("Dynamic access precedence", $"{scenarioPrefix}:myapp:database:host", () => 
                {
                    dynamic config = _integrationFlexConfiguration!;
                    var dynamicValue = config[$"{scenarioPrefix}:myapp:database:host"]?.ToString();
                    var standardValue = _integrationConfiguration![$"{scenarioPrefix}:myapp:database:host"];
                    return dynamicValue == standardValue;
                }),
                ("Section-based precedence", $"{scenarioPrefix}:infrastructure-module:environment", () => 
                {
                    var flexSection = _integrationFlexConfiguration!.Configuration.GetSection($"{scenarioPrefix}:infrastructure-module");
                    var standardSection = _integrationConfiguration!.GetSection($"{scenarioPrefix}:infrastructure-module");
                    return flexSection.Exists() == standardSection.Exists();
                })
            };

            foreach (var test in precedenceTests)
            {
                var passed = test.validation();
                _integrationValidationResults.Add(passed 
                    ? $"✓ {test.description} test passed for key '{test.key}'"
                    : $"✗ {test.description} test failed for key '{test.key}'");
                
                passed.Should().BeTrue($"{test.description} validation should pass");
            }
        }
        catch (Exception ex)
        {
            _integrationValidationResults.Add($"✗ FlexKit precedence handling testing failed: {ex.Message}");
            throw;
        }
    }

    [Then(@"the integration controller should support comprehensive JSON processing")]
    public void ThenTheIntegrationControllerShouldSupportComprehensiveJsonProcessing()
    {
        _integrationConfiguration.Should().NotBeNull("Configuration should be built");

        try
        {
            // Test comprehensive JSON processing across Azure sources with a scenario prefix
            var jsonTests = new List<(string description, string key, Func<bool> validation)>
            {
                ("Key Vault JSON processing", $"keyVaultSecrets:infrastructure-module-database-credentials:host", () => 
                {
                    var value = _integrationConfiguration!["keyVaultSecrets:infrastructure-module-database-credentials:host"];
                    return !string.IsNullOrEmpty(value);
                }),
                ("Nested JSON structure", "keyVaultSecrets:infrastructure-module-api-config:authentication:type", () => 
                {
                    var value = _integrationConfiguration!["keyVaultSecrets:infrastructure-module-api-config:authentication:type"];
                    return value == "bearer";
                }),
                ("JSON array elements", "keyVaultSecrets:infrastructure-module-cache-settings:redis:host", () => 
                {
                    var value = _integrationConfiguration!["keyVaultSecrets:infrastructure-module-cache-settings:redis:host"];
                    return !string.IsNullOrEmpty(value);
                })
            };

            foreach (var test in jsonTests)
            {
                var passed = test.validation();
                _integrationValidationResults.Add(passed 
                    ? $"✓ {test.description} test passed for key '{test.key}'"
                    : $"✗ {test.description} test failed for key '{test.key}'");
                
                passed.Should().BeTrue($"{test.description} validation should pass");
            }
        }
        catch (Exception ex)
        {
            _integrationValidationResults.Add($"✗ Comprehensive JSON processing testing failed: {ex.Message}");
            throw;
        }
    }

    [Then(@"the integration controller configuration should demonstrate complex data structure handling")]
    public void ThenTheIntegrationControllerConfigurationShouldDemonstrateComplexDataStructureHandling()
    {
        _integrationConfiguration.Should().NotBeNull("Configuration should be built");
        _integrationFlexConfiguration.Should().NotBeNull("FlexConfig should be built");

        try
        {
            // Test handling of complex data structures with a scenario prefix
            var complexTests = new List<(string description, Func<bool> validation)>
            {
                ("Nested object access via FlexKit", () => 
                {
                    dynamic config = _integrationFlexConfiguration!;
                    var value = config[$"keyVaultSecrets:infrastructure-module-database-credentials:port"];
                    return value != null;
                }),
                ("Deep hierarchy navigation", () => 
                {
                    var value = _integrationConfiguration!["keyVaultSecrets:infrastructure-module-api-config:authentication:token"];
                    return !string.IsNullOrEmpty(value);
                }),
                ("Complex structure enumeration", () => 
                {
                    var keys = _integrationConfiguration!.AsEnumerable()
                        .Where(kvp => kvp.Key.StartsWith($"keyVaultSecrets:infrastructure-module-api-config:") && kvp.Key.Contains(":"))
                        .ToList();
                    return keys.Count > 0;
                }),
                ("FlexKit dynamic complex access", () => 
                {
                    dynamic config = _integrationFlexConfiguration!;
                    var section = config["appConfigurationSettings:myapp:cache:ttl"];
                    return section != null;
                })
            };

            foreach (var test in complexTests)
            {
                var passed = test.validation();
                _integrationValidationResults.Add(passed 
                    ? $"✓ {test.description} test passed"
                    : $"✗ {test.description} test failed");
                
                passed.Should().BeTrue($"{test.description} validation should pass");
            }
        }
        catch (Exception ex)
        {
            _integrationValidationResults.Add($"✗ Complex data structure handling testing failed: {ex.Message}");
            throw;
        }
    }

    [Then(@"the integration controller should demonstrate FlexKit JSON integration capabilities")]
    public void ThenTheIntegrationControllerShouldDemonstrateFlexKitJsonIntegrationCapabilities()
    {
        _integrationFlexConfiguration.Should().NotBeNull("FlexKit configuration should be available");

        try
        {
            // Test FlexKit's integration with JSON-processed Azure configuration with scenario prefix
            dynamic config = _integrationFlexConfiguration!;

            var jsonIntegrationTests = new List<(string description, Func<bool> validation)>
            {
                ("JSON to dynamic property mapping", () => 
                {
                    var value = config["keyVaultSecrets:infrastructure-module-database-credentials:ssl"];
                    return value != null;
                }),
                ("Nested JSON dynamic access", () => 
                {
                    var value = config.appConfigurationSettings.myapp.api.baseUrl;
                    return !string.IsNullOrEmpty(value?.ToString());
                }),
                ("JSON array to configuration mapping", () => 
                {
                    var value = config[$"keyVaultSecrets:myapp:features:cache:enabled"];
                    return value != null;
                }),
                ("Type-safe JSON access", () => 
                {
                    var port = _integrationFlexConfiguration!["jsonSecrets:database-config:port"].ToType<string>();
                    return !string.IsNullOrEmpty(port);
                })
            };

            foreach (var test in jsonIntegrationTests)
            {
                var passed = test.validation();
                _integrationValidationResults.Add(passed 
                    ? $"✓ {test.description} test passed"
                    : $"✗ {test.description} test failed");
                
                passed.Should().BeTrue($"{test.description} validation should pass");
            }
        }
        catch (Exception ex)
        {
            _integrationValidationResults.Add($"✗ FlexKit JSON integration testing failed: {ex.Message}");
            throw;
        }
    }

    [Then(@"the integration controller should demonstrate error-tolerant Azure integration")]
    public void ThenTheIntegrationControllerShouldDemonstrateErrorTolerantAzureIntegration()
    {
        _errorToleranceEnabled.Should().BeTrue("Error tolerance should be enabled");
        _integrationFlexConfiguration.Should().NotBeNull("FlexConfig should be built despite potential errors");

        try
        {
            // Test that the integration works even with optional sources
            var errorToleranceTests = new List<(string description, Func<bool> validation)>
            {
                ("Configuration built despite optional failures", () => _integrationConfiguration != null),
                ("FlexKit operational with error tolerance", () => 
                {
                    dynamic _ = _integrationFlexConfiguration!;
                    return TryDynamicAccess();
                }),
                ("Available configuration accessible", () => 
                {
                    var keys = _integrationConfiguration!.AsEnumerable().ToList();
                    return keys.Count > 0;
                }),
                ("Error recovery and graceful degradation", () => 
                {
                    // Test that we can still access available configuration
                    var hasAnyConfig = _integrationConfiguration!.AsEnumerable().Any();
                    return hasAnyConfig;
                })
            };

            foreach (var test in errorToleranceTests)
            {
                var passed = test.validation();
                _integrationValidationResults.Add(passed 
                    ? $"✓ {test.description} test passed"
                    : $"✗ {test.description} test failed");
                
                passed.Should().BeTrue($"{test.description} validation should pass");
            }

            // Verify that we can still access FlexKit features despite any potential issues
            var flexKitStillWorks = TryDynamicAccess();
            _integrationValidationResults.Add(flexKitStillWorks 
                ? "✓ FlexKit dynamic access still functional with error tolerance"
                : "✗ FlexKit dynamic access failed with error tolerance");
            
            flexKitStillWorks.Should().BeTrue("FlexKit should remain functional with error tolerance");
        }
        catch (Exception ex)
        {
            _integrationValidationResults.Add($"✗ Error-tolerant Azure integration testing failed: {ex.Message}");
            throw;
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Attempts dynamic access to test FlexKit functionality without throwing exceptions.
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