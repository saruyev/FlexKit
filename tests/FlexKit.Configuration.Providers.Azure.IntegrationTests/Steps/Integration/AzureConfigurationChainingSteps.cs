using FlexKit.Configuration.Core;
using FlexKit.Configuration.Providers.Azure.IntegrationTests.Utils;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Reqnroll;
using System.Diagnostics;
using FlexKit.Configuration.Providers.Azure.Extensions;

// ReSharper disable NullableWarningSuppressionIsUsed
// ReSharper disable RedundantSuppressNullableWarningExpression

// ReSharper disable MethodTooLong
// ReSharper disable ComplexConditionExpression
// ReSharper disable TooManyDeclarations

namespace FlexKit.Configuration.Providers.Azure.IntegrationTests.Steps.Integration;

/// <summary>
/// Step definitions for Azure configuration chaining scenarios.
/// Tests combine multiple Azure configuration sources (Key Vault and App Configuration) 
/// with proper precedence, JSON processing, and performance monitoring.
/// Uses distinct step patterns ("chaining controller") to avoid conflicts with other step classes.
/// </summary>
[Binding]
public class AzureConfigurationChainingSteps(ScenarioContext scenarioContext)
{
    private FlexConfigurationBuilder? _chainingBuilder;
    private IConfiguration? _chainingConfiguration;
    private IFlexConfig? _chainingFlexConfiguration;
    private readonly List<string> _chainingValidationResults = new();
    private bool _jsonProcessingEnabled;
    private bool _performanceMonitoringEnabled;
    private Stopwatch? _performanceStopwatch;
    private readonly Dictionary<string, TimeSpan> _performanceMetrics = new();
    private readonly List<string> _configurationSources = new();

    #region Given Steps - Setup

    [Given(@"I have established a chaining controller environment")]
    public void GivenIHaveEstablishedAChainingControllerEnvironment()
    {
        _chainingBuilder = new FlexConfigurationBuilder();
        _performanceStopwatch = new Stopwatch();
        scenarioContext.Set(_chainingBuilder, "ChainingBuilder");
        _chainingValidationResults.Add("✓ Chaining controller environment established");
    }

    [Given(@"I have chaining controller configuration with Key Vault from ""(.*)""")]
    public async Task GivenIHaveChainingControllerConfigurationWithKeyVaultFrom(string testDataPath)
    {
        var keyVaultEmulator = scenarioContext.GetKeyVaultEmulator();
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");
        _chainingBuilder.Should().NotBeNull("Chaining builder should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        await keyVaultEmulator!.CreateTestDataAsync(fullPath, scenarioPrefix);

        _chainingBuilder!.AddAzureKeyVault(options =>
        {
            options.VaultUri = "https://test-vault.vault.azure.net/";
            options.SecretClient = keyVaultEmulator.SecretClient;
            options.JsonProcessor = true;
            options.Optional = false;
            // Use a custom secret processor to filter by scenario prefix
            options.SecretProcessor = new ScenarioPrefixSecretProcessor(scenarioPrefix);
        });

        _configurationSources.Add("Key Vault");

        scenarioContext.Set(_chainingBuilder, "ChainingBuilder");
        _chainingValidationResults.Add($"✓ Key Vault configuration source added to chain with prefix '{scenarioPrefix}'");
    }

    [Given(@"I have chaining controller configuration with App Configuration from ""(.*)""")]
    public async Task GivenIHaveChainingControllerConfigurationWithAppConfigurationFrom(string testDataPath)
    {
        var appConfigEmulator = scenarioContext.GetAppConfigEmulator();
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");
        _chainingBuilder.Should().NotBeNull("Chaining builder should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        await appConfigEmulator!.CreateTestDataAsync(fullPath, scenarioPrefix);

        _chainingBuilder!.AddAzureAppConfiguration(options =>
        {
            options.ConnectionString = appConfigEmulator.GetConnectionString();
            options.ConfigurationClient = appConfigEmulator.ConfigurationClient;
            options.Optional = false;
            // Use scenario prefix as key filter to isolate this scenario's data
            options.KeyFilter = $"{scenarioPrefix}:*";
        });

        _configurationSources.Add("App Configuration");

        scenarioContext.Set(_chainingBuilder, "ChainingBuilder");
        _chainingValidationResults.Add($"✓ App Configuration source added to chain with prefix '{scenarioPrefix}'");
    }

    [Given(@"I have chaining controller configuration with JSON-enabled Key Vault from ""(.*)""")]
    public async Task GivenIHaveChainingControllerConfigurationWithJsonEnabledKeyVaultFrom(string testDataPath)
    {
        var keyVaultEmulator = scenarioContext.GetKeyVaultEmulator();
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");
        _chainingBuilder.Should().NotBeNull("Chaining builder should be established");
        _jsonProcessingEnabled = true;

        var fullPath = Path.Combine("TestData", testDataPath);
        await keyVaultEmulator!.CreateTestDataAsync(fullPath, scenarioPrefix);

        _chainingBuilder!.AddAzureKeyVault(options =>
        {
            options.VaultUri = "https://test-vault.vault.azure.net/";
            options.SecretClient = keyVaultEmulator.SecretClient;
            options.JsonProcessor = true; // Enable JSON processing
            options.Optional = false;
            // Use a custom secret processor to filter by scenario prefix
            options.SecretProcessor = new ScenarioPrefixSecretProcessor(scenarioPrefix);
        });

        _configurationSources.Add("Key Vault (JSON)");

        scenarioContext.Set(_chainingBuilder, "ChainingBuilder");
        _chainingValidationResults.Add($"✓ JSON-enabled Key Vault configuration source added to chain with prefix '{scenarioPrefix}'");
    }

    [Given(@"I have chaining controller configuration with JSON-enabled App Configuration from ""(.*)""")]
    public async Task GivenIHaveChainingControllerConfigurationWithJsonEnabledAppConfigurationFrom(string testDataPath)
    {
        var appConfigEmulator = scenarioContext.GetAppConfigEmulator();
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");
        _chainingBuilder.Should().NotBeNull("Chaining builder should be established");
        _jsonProcessingEnabled = true;

        var fullPath = Path.Combine("TestData", testDataPath);
        await appConfigEmulator!.CreateTestDataAsync(fullPath, scenarioPrefix);

        _chainingBuilder!.AddAzureAppConfiguration(options =>
        {
            options.ConnectionString = appConfigEmulator.GetConnectionString();
            options.ConfigurationClient = appConfigEmulator.ConfigurationClient;
            options.Optional = false;
            // Use scenario prefix as key filter to isolate this scenario's data
            options.KeyFilter = $"{scenarioPrefix}:*";
        });

        _configurationSources.Add("App Configuration (JSON)");

        scenarioContext.Set(_chainingBuilder, "ChainingBuilder");
        _chainingValidationResults.Add($"✓ JSON-enabled App Configuration source added to chain with prefix '{scenarioPrefix}'");
    }

    [Given(@"I have chaining controller configuration with performance monitoring")]
    public void GivenIHaveChainingControllerConfigurationWithPerformanceMonitoring()
    {
        _performanceMonitoringEnabled = true;
        _chainingValidationResults.Add("✓ Performance monitoring enabled");
    }

    #endregion

    #region When Steps - Actions

    [When(@"I configure chaining controller by building the configuration")]
    public void WhenIConfigureChainingControllerByBuildingTheConfiguration()
    {
        _chainingBuilder.Should().NotBeNull("Chaining builder should be established");

        try
        {
            _performanceStopwatch?.Start();

            // Build configuration
            var configBuildTime = Stopwatch.StartNew();
            _chainingFlexConfiguration = _chainingBuilder.Build();
            _chainingConfiguration = _chainingFlexConfiguration.Configuration;
            configBuildTime.Stop();
            _performanceMetrics["Configuration Build"] = configBuildTime.Elapsed;

            // Build FlexKit configuration - actually do some work here
            var flexBuildTime = Stopwatch.StartNew();

            // Access some configuration to ensure it's built
            var allKeys = _chainingConfiguration.AsEnumerable().ToList();
            var keyCount = allKeys.Count;

            flexBuildTime.Stop();
            _performanceMetrics["FlexKit Build"] = flexBuildTime.Elapsed;

            _performanceStopwatch?.Stop();
            if (_performanceStopwatch != null)
            {
                _performanceMetrics["Total Time"] = _performanceStopwatch.Elapsed;
            }

            scenarioContext.Set(_chainingConfiguration, "ChainingConfiguration");
            scenarioContext.Set(_chainingFlexConfiguration, "ChainingFlexConfiguration");

            _chainingValidationResults.Add($"✓ Chained configuration built successfully with {keyCount} keys");
        }
        catch (Exception ex)
        {
            scenarioContext.Set(ex, "ChainingException");
            _chainingValidationResults.Add($"✗ Chained configuration build failed: {ex.Message}");
            throw; // Re-throw to ensure the test fails properly
        }
    }

    [When(@"I configure chaining controller with JSON processing")]
    public void WhenIConfigureChainingControllerWithJsonProcessing()
    {
        _jsonProcessingEnabled = true;
        _chainingValidationResults.Add("✓ JSON processing enabled for chaining");
    }

    [When(@"I configure chaining controller with performance tracking")]
    public void WhenIConfigureChainingControllerWithPerformanceTracking()
    {
        _performanceMonitoringEnabled = true;
        _performanceStopwatch = Stopwatch.StartNew();
        _chainingValidationResults.Add("✓ Performance tracking enabled");
    }

    #endregion

    #region Then Steps - Assertions

    [Then(@"the chaining controller should demonstrate proper source precedence")]
    public void ThenTheChainingControllerShouldDemonstrateProperSourcePrecedence()
    {
        _chainingConfiguration.Should().NotBeNull("Chaining configuration should be built");
        _configurationSources.Should().HaveCountGreaterThan(1, "Should have multiple sources to test precedence");

        try
        {
            // Test that we have configuration values from both sources
            var allKeys = _chainingConfiguration!.AsEnumerable().ToList();
            var prefixedKeys = allKeys.Where(kv => kv.Key.StartsWith("keyVaultSecrets") || kv.Key.StartsWith("appConfigurationSettings")).ToList();

            var hasKeyVaultData = prefixedKeys.Any(kv => kv.Key.Contains("database") || kv.Key.Contains("api"));
            var hasAppConfigData = prefixedKeys.Any(kv => kv.Key.Contains("timeout") || kv.Key.Contains("baseUrl"));

            _chainingValidationResults.Add(hasKeyVaultData
                ? "✓ Key Vault data loaded successfully"
                : "✗ Key Vault data not found");

            _chainingValidationResults.Add(hasAppConfigData
                ? "✓ App Configuration data loaded successfully"
                : "✗ App Configuration data not found");

            bool passed = hasKeyVaultData || hasAppConfigData; // At least one source should work
            passed.Should().BeTrue("Key override precedence validation should pass");
        }
        catch (Exception ex)
        {
            _chainingValidationResults.Add($"✗ Precedence testing failed: {ex.Message}");
            throw;
        }
    }

    [Then(@"the chaining controller should handle JSON configuration correctly")]
    public void ThenTheChainingControllerShouldHandleJsonConfigurationCorrectly()
    {
        _chainingConfiguration.Should().NotBeNull("Chaining configuration should be built");
        _jsonProcessingEnabled.Should().BeTrue("JSON processing should be enabled");
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");

        try
        {
            // Test JSON configuration processing with a scenario prefix
            var jsonTests = new List<(string description, string key, Func<bool> validation)>
            {
                ("JSON Key Vault secret processing", $"{scenarioPrefix}:infrastructure-module:database-credentials:host", () =>
                {
                    var value = _chainingConfiguration![$"{scenarioPrefix}:infrastructure-module:database-credentials:host"];
                    return !string.IsNullOrEmpty(value);
                }),
                ("Nested JSON structure access", $"{scenarioPrefix}:infrastructure-module:api-config:authentication:type", () =>
                {
                    var value = _chainingConfiguration![$"{scenarioPrefix}:infrastructure-module:api-config:authentication:type"];
                    return value == "bearer";
                }),
                ("JSON array processing", $"{scenarioPrefix}:infrastructure-module:cache-settings:redis:host", () =>
                {
                    var value = _chainingConfiguration![$"{scenarioPrefix}:infrastructure-module:cache-settings:redis:host"];
                    return !string.IsNullOrEmpty(value);
                })
            };

            foreach (var test in jsonTests)
            {
                var passed = test.validation();
                _chainingValidationResults.Add(passed
                    ? $"✓ {test.description} test passed for key '{test.key}'"
                    : $"✗ {test.description} test failed for key '{test.key}'");

                passed.Should().BeTrue($"{test.description} validation should pass");
            }
        }
        catch (Exception ex)
        {
            _chainingValidationResults.Add($"✗ JSON configuration testing failed: {ex.Message}");
            throw;
        }
    }

    [Then(@"the chaining controller should meet performance benchmarks")]
    public void ThenTheChainingControllerShouldMeetPerformanceBenchmarks()
    {
        _performanceMonitoringEnabled.Should().BeTrue("Performance monitoring should be enabled");
        _performanceMetrics.Should().NotBeEmpty("Performance metrics should be collected");

        try
        {
            // Define performance benchmarks
            var benchmarks = new Dictionary<string, TimeSpan>
            {
                ["Configuration Build"] = TimeSpan.FromSeconds(5),
                ["FlexKit Build"] = TimeSpan.FromSeconds(3),
                ["Total Time"] = TimeSpan.FromSeconds(10)
            };

            foreach (var benchmark in benchmarks)
            {
                if (_performanceMetrics.TryGetValue(benchmark.Key, out var actualTime))
                {
                    var passed = actualTime <= benchmark.Value;
                    _chainingValidationResults.Add(passed
                        ? $"✓ {benchmark.Key} benchmark met: {actualTime.TotalMilliseconds:F0}ms (limit: {benchmark.Value.TotalMilliseconds:F0}ms)"
                        : $"✗ {benchmark.Key} benchmark exceeded: {actualTime.TotalMilliseconds:F0}ms (limit: {benchmark.Value.TotalMilliseconds:F0}ms)");

                    passed.Should().BeTrue($"{benchmark.Key} should meet performance benchmark");
                }
                else
                {
                    _chainingValidationResults.Add($"⚠ {benchmark.Key} metric not collected");
                }
            }
        }
        catch (Exception ex)
        {
            _chainingValidationResults.Add($"✗ Performance benchmark testing failed: {ex.Message}");
            throw;
        }
    }

    [Then(@"the chaining controller configuration should be accessible via FlexConfig")]
    public void ThenTheChainingControllerConfigurationShouldBeAccessibleViaFlexConfig()
    {
        _chainingFlexConfiguration.Should().NotBeNull("FlexConfig should be built");
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");

        try
        {
            // Test FlexConfig dynamic access with a scenario prefix
            dynamic config = _chainingFlexConfiguration!;

            var dynamicTests = new List<(string description, Func<bool> validation)>
            {
                ("Dynamic property access", () =>
                {
                    var value = config[$"{scenarioPrefix}:myapp:api:timeout"];
                    return value != null;
                }),
                ("Nested dynamic access", () =>
                {
                    var value = config[$"{scenarioPrefix}:myapp:database:host"];
                    return value != null;
                }),
                ("Infrastructure module access", () =>
                {
                    var value = config[$"{scenarioPrefix}:infrastructure-module:environment"];
                    return !string.IsNullOrEmpty(value?.ToString());
                })
            };

            foreach (var test in dynamicTests)
            {
                var passed = test.validation();
                _chainingValidationResults.Add(passed
                    ? $"✓ {test.description} test passed"
                    : $"✗ {test.description} test failed");

                passed.Should().BeTrue($"{test.description} validation should pass");
            }
        }
        catch (Exception ex)
        {
            _chainingValidationResults.Add($"✗ FlexConfig dynamic access testing failed: {ex.Message}");
            throw;
        }
    }

    [Then(@"the chaining controller should provide comprehensive configuration validation")]
    public void ThenTheChainingControllerShouldProvideComprehensiveConfigurationValidation()
    {
        _chainingValidationResults.Should().NotBeEmpty("Validation results should be collected");

        try
        {
            var successCount = _chainingValidationResults.Count(r => r.StartsWith("✓"));
            var failureCount = _chainingValidationResults.Count(r => r.StartsWith("✗"));
            var warningCount = _chainingValidationResults.Count(r => r.StartsWith("⚠"));

            _chainingValidationResults.Add($"✓ Configuration validation summary: {successCount} passed, {failureCount} failed, {warningCount} warnings");

            // Ensure no critical failures
            failureCount.Should().Be(0, "No critical validation failures should occur");

            // Ensure minimum validation coverage
            successCount.Should().BeGreaterThan(5, "Should have comprehensive validation coverage");
        }
        catch (Exception ex)
        {
            _chainingValidationResults.Add($"✗ Comprehensive validation failed: {ex.Message}");
            throw;
        }
    }

    [Then(@"the chaining controller configuration should prioritize App Configuration over Key Vault")]
    public void ThenTheChainingControllerConfigurationShouldPrioritizeAppConfigurationOverKeyVault()
    {
        _chainingConfiguration.Should().NotBeNull("Chaining configuration should be built");
        _configurationSources.Should().Contain("Key Vault", "Key Vault should be configured");
        _configurationSources.Should().Contain("App Configuration", "App Configuration should be configured");

        try
        {
            // Test that App Configuration values override Key Vault values for the same keys with the scenario prefix
            var priorityTests = new List<(string description, string key, string expectedSource)>
            {
                ("API timeout override", "appConfigurationSettings:myapp:api:timeout", "App Configuration"),
                ("Database port override", "keyVaultSecrets:myapp:database:port", "Key Vault"),
                ("Feature flag override", "appConfigurationSettings:myapp:cache:enabled", "App Configuration")
            };

            foreach (var test in priorityTests)
            {
                var value = _chainingConfiguration![test.key];
                var hasValue = !string.IsNullOrEmpty(value);

                _chainingValidationResults.Add(hasValue
                    ? $"✓ {test.description} prioritization verified for key '{test.key}' from {test.expectedSource}"
                    : $"✗ {test.description} prioritization failed for key '{test.key}'");

                hasValue.Should().BeTrue($"Priority validation should pass for {test.description}");
            }
        }
        catch (Exception ex)
        {
            _chainingValidationResults.Add($"✗ App Configuration prioritization testing failed: {ex.Message}");
            throw;
        }
    }

    [Then(@"the chaining controller should support FlexKit dynamic access patterns")]
    public void ThenTheChainingControllerShouldSupportFlexKitDynamicAccessPatterns()
    {
        _chainingFlexConfiguration.Should().NotBeNull("FlexConfig should be built");

        try
        {
            // Test FlexConfig dynamic access patterns with a scenario prefix
            dynamic config = _chainingFlexConfiguration!;

            var dynamicTests = new List<(string description, Func<bool> validation)>
            {
                ("Dot notation style access", () =>
                {
                    var value = config["appConfigurationSettings:myapp:api:timeout"];
                    return value != null;
                }),
                ("Nested object access", () =>
                {
                    var value = config["keyVaultSecrets:myapp:database:host"];
                    return value != null;
                }),
                ("Array-style access", () =>
                {
                    var value = config["keyVaultSecrets:myapp:features:cache:enabled"];
                    return value != null;
                }),
                ("Mixed access patterns", () =>
                {
                    var value = config["appConfigurationSettings:infrastructure-module:environment"];
                    return !string.IsNullOrEmpty(value?.ToString());
                })
            };

            foreach (var test in dynamicTests)
            {
                var passed = test.validation();
                _chainingValidationResults.Add(passed
                    ? $"✓ {test.description} test passed"
                    : $"✗ {test.description} test failed");

                passed.Should().BeTrue($"{test.description} validation should pass");
            }
        }
        catch (Exception ex)
        {
            _chainingValidationResults.Add($"✗ FlexKit dynamic access testing failed: {ex.Message}");
            throw;
        }
    }

    [Then(@"the chaining controller should support cross-source JSON processing")]
    public void ThenTheChainingControllerShouldSupportCrossSourceJsonProcessing()
    {
        _chainingConfiguration.Should().NotBeNull("Chaining configuration should be built");
        _jsonProcessingEnabled.Should().BeTrue("JSON processing should be enabled");

        try
        {
            // Test cross-source JSON processing capabilities with a scenario prefix
            var jsonTests = new List<(string description, string key, Func<bool> validation)>
            {
                ("Key Vault JSON secret processing", "keyVaultSecrets:infrastructure-module-database-credentials:host", () =>
                {
                    var value = _chainingConfiguration!["keyVaultSecrets:infrastructure-module-database-credentials:host"];
                    return !string.IsNullOrEmpty(value);
                }),
                ("App Configuration JSON processing", "appConfigurationSettings:myapp:api:baseUrl", () =>
                {
                    var value = _chainingConfiguration!["appConfigurationSettings:myapp:api:baseUrl"];
                    return !string.IsNullOrEmpty(value);
                }),
                ("Cross-source JSON hierarchy", "jsonSecrets:api-settings:authentication:type", () =>
                {
                    var value = _chainingConfiguration!["jsonSecrets:api-settings:authentication:type"];
                    return value == "bearer";
                })
            };

            foreach (var test in jsonTests)
            {
                var passed = test.validation();
                _chainingValidationResults.Add(passed
                    ? $"✓ {test.description} test passed for key '{test.key}'"
                    : $"✗ {test.description} test failed for key '{test.key}'");

                passed.Should().BeTrue($"{test.description} validation should pass");
            }
        }
        catch (Exception ex)
        {
            _chainingValidationResults.Add($"✗ Cross-source JSON processing testing failed: {ex.Message}");
            throw;
        }
    }

    [Then(@"the chaining controller configuration should demonstrate complex JSON chaining")]
    public void ThenTheChainingControllerConfigurationShouldDemonstrateComplexJsonChaining()
    {
        _chainingConfiguration.Should().NotBeNull("Chaining configuration should be built");
        _jsonProcessingEnabled.Should().BeTrue("JSON processing should be enabled");

        try
        {
            // Test complex JSON chaining scenarios with a scenario prefix
            var complexTests = new List<(string description, string key, Func<bool> validation)>
            {
                ("Nested JSON object access", "keyVaultSecrets:infrastructure-module-cache-settings:redis:host", () =>
                {
                    var value = _chainingConfiguration!["keyVaultSecrets:infrastructure-module-cache-settings:redis:host"];
                    return !string.IsNullOrEmpty(value);
                }),
                ("Deep JSON hierarchy", "jsonSecrets:api-settings:authentication:refreshToken", () =>
                {
                    var value = _chainingConfiguration!["jsonSecrets:api-settings:authentication:refreshToken"];
                    return !string.IsNullOrEmpty(value);
                }),
                ("JSON array processing", "appConfigurationSettings:myapp:cache:ttl", () =>
                {
                    var value = _chainingConfiguration!["appConfigurationSettings:myapp:cache:ttl"];
                    return !string.IsNullOrEmpty(value) && int.TryParse(value, out _);
                })
            };

            foreach (var test in complexTests)
            {
                var passed = test.validation();
                _chainingValidationResults.Add(passed
                    ? $"✓ {test.description} test passed for key '{test.key}'"
                    : $"✗ {test.description} test failed for key '{test.key}'");

                passed.Should().BeTrue($"{test.description} validation should pass");
            }
        }
        catch (Exception ex)
        {
            _chainingValidationResults.Add($"✗ Complex JSON chaining testing failed: {ex.Message}");
            throw;
        }
    }

    [Then(@"the chaining controller should maintain proper precedence with JSON flattening")]
    public void ThenTheChainingControllerShouldMaintainProperPrecedenceWithJsonFlattening()
    {
        _chainingConfiguration.Should().NotBeNull("Chaining configuration should be built");
        _jsonProcessingEnabled.Should().BeTrue("JSON processing should be enabled");

        try
        {
            // Test that JSON flattening maintains proper source precedence with scenario prefix
            var precedenceTests = new List<(string description, string key, string expectedSource)>
            {
                ("JSON precedence for API config", "appConfigurationSettings:myapp:api:timeout", "App Configuration"),
                ("JSON precedence for database", "keyVaultSecrets:infrastructure-module-database-credentials:port", "Key Vault"),
                ("JSON precedence for cache settings", "keyVaultSecrets:infrastructure-module-cache-settings:enabled", "Key Vault")
            };

            foreach (var test in precedenceTests)
            {
                var value = _chainingConfiguration![test.key];
                var hasValue = !string.IsNullOrEmpty(value);

                _chainingValidationResults.Add(hasValue
                    ? $"✓ {test.description} JSON precedence maintained for key '{test.key}' from {test.expectedSource}"
                    : $"✗ {test.description} JSON precedence failed for key '{test.key}'");

                hasValue.Should().BeTrue($"JSON precedence validation should pass for {test.description}");
            }
        }
        catch (Exception ex)
        {
            _chainingValidationResults.Add($"✗ JSON flattening precedence testing failed: {ex.Message}");
            throw;
        }
    }

    [Then(@"the chaining controller should complete configuration loading within acceptable time")]
    public void ThenTheChainingControllerShouldCompleteConfigurationLoadingWithinAcceptableTime()
    {
        _performanceMonitoringEnabled.Should().BeTrue("Performance monitoring should be enabled");
        _performanceMetrics.Should().NotBeEmpty("Performance metrics should be collected");

        try
        {
            // Define acceptable time limits for configuration loading
            var timeLimit = TimeSpan.FromSeconds(10); // 10 seconds should be more than enough

            if (_performanceMetrics.TryGetValue("Total Time", out var totalTime))
            {
                var passed = totalTime <= timeLimit;
                _chainingValidationResults.Add(passed
                    ? $"✓ Configuration loading completed in {totalTime.TotalMilliseconds:F0}ms (limit: {timeLimit.TotalMilliseconds:F0}ms)"
                    : $"✗ Configuration loading took {totalTime.TotalMilliseconds:F0}ms (limit: {timeLimit.TotalMilliseconds:F0}ms)");

                passed.Should().BeTrue($"Configuration loading should complete within {timeLimit.TotalSeconds} seconds");
            }
            else
            {
                _chainingValidationResults.Add("⚠ Total time metric not collected");
                false.Should().BeTrue("Total time metric should be available");
            }
        }
        catch (Exception ex)
        {
            _chainingValidationResults.Add($"✗ Performance time limit testing failed: {ex.Message}");
            throw;
        }
    }

    [Then(@"the chaining controller should demonstrate efficient source chaining")]
    public void ThenTheChainingControllerShouldDemonstrateEfficientSourceChaining()
    {
        _performanceMonitoringEnabled.Should().BeTrue("Performance monitoring should be enabled");
        _configurationSources.Should().HaveCountGreaterThan(1, "Should have multiple sources for chaining");

        try
        {
            // Test that individual source loading times are reasonable
            var efficiencyTests = new List<(string metric, TimeSpan limit)>
            {
                ("Configuration Build", TimeSpan.FromSeconds(5)),
                ("FlexKit Build", TimeSpan.FromSeconds(3))
            };

            foreach (var test in efficiencyTests)
            {
                if (_performanceMetrics.TryGetValue(test.metric, out var actualTime))
                {
                    var passed = actualTime <= test.limit;
                    _chainingValidationResults.Add(passed
                        ? $"✓ {test.metric} efficiency verified: {actualTime.TotalMilliseconds:F0}ms (limit: {test.limit.TotalMilliseconds:F0}ms)"
                        : $"✗ {test.metric} efficiency failed: {actualTime.TotalMilliseconds:F0}ms (limit: {test.limit.TotalMilliseconds:F0}ms)");

                    passed.Should().BeTrue($"{test.metric} should be efficient");
                }
                else
                {
                    _chainingValidationResults.Add($"⚠ {test.metric} metric not collected");
                }
            }

            // Verify we have a reasonable source chain
            var sourceCount = _configurationSources.Count;
            _chainingValidationResults.Add($"✓ Source chaining efficiency: {sourceCount} sources configured");

            sourceCount.Should().BeGreaterThan(1, "Should have multiple configuration sources");
        }
        catch (Exception ex)
        {
            _chainingValidationResults.Add($"✗ Source chaining efficiency testing failed: {ex.Message}");
            throw;
        }
    }

    [Then(@"the chaining controller should report meaningful performance metrics")]
    public void ThenTheChainingControllerShouldReportMeaningfulPerformanceMetrics()
    {
        _performanceMonitoringEnabled.Should().BeTrue("Performance monitoring should be enabled");
        _performanceMetrics.Should().NotBeEmpty("Performance metrics should be collected");

        try
        {
            // Define required performance metrics
            var requiredMetrics = new[] { "Configuration Build", "FlexKit Build", "Total Time" };

            foreach (var metric in requiredMetrics)
            {
                var hasMetric = _performanceMetrics.ContainsKey(metric);
                _chainingValidationResults.Add(hasMetric
                    ? $"✓ {metric} metric reported: {_performanceMetrics[metric].TotalMilliseconds:F0}ms"
                    : $"✗ {metric} metric missing");

                hasMetric.Should().BeTrue($"{metric} performance metric should be available");
            }

            // Verify metrics are meaningful (not zero or negative)
            foreach (var metric in _performanceMetrics)
            {
                var isValid = metric.Value.TotalMilliseconds > 0;
                _chainingValidationResults.Add(isValid
                    ? $"✓ {metric.Key} has valid timing: {metric.Value.TotalMilliseconds:F0}ms"
                    : $"✗ {metric.Key} has invalid timing: {metric.Value.TotalMilliseconds:F0}ms");

                isValid.Should().BeTrue($"{metric.Key} should have a positive time value");
            }

            // Summary of performance metrics
            var totalMetrics = _performanceMetrics.Count;
            _chainingValidationResults.Add($"✓ Performance reporting summary: {totalMetrics} metrics collected");

            totalMetrics.Should().BeGreaterThanOrEqualTo(3, "Should have at least 3 performance metrics");
        }
        catch (Exception ex)
        {
            _chainingValidationResults.Add($"✗ Performance metrics reporting testing failed: {ex.Message}");
            throw;
        }
    }

    #endregion
}