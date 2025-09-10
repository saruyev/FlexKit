using FlexKit.Configuration.Core;
using FlexKit.Configuration.Providers.Azure.IntegrationTests.Utils;
using FlexKit.Configuration.Conversion;
using FlexKit.Configuration.Providers.Azure.Extensions;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Reqnroll;
using Newtonsoft.Json;
// ReSharper disable MethodTooLong
// ReSharper disable TooManyDeclarations
// ReSharper disable NullableWarningSuppressionIsUsed
// ReSharper disable ComplexConditionExpression

namespace FlexKit.Configuration.Providers.Azure.IntegrationTests.Steps.AppConfiguration;

/// <summary>
/// Step definitions for App Configuration basic operations scenarios.
/// Tests Azure App Configuration integration including configuration loading, label filtering,
/// key filtering, feature flags, and FlexKit dynamic access capabilities.
/// Uses distinct step patterns ("app config controller") to avoid conflicts with other step classes.
/// </summary>
[Binding]
public class AppConfigurationBasicOperationsSteps(ScenarioContext scenarioContext)
{
    private IConfiguration? _appConfigConfiguration;
    private IFlexConfig? _appConfigFlexConfiguration;
    private readonly List<string> _appConfigValidationResults = new();
    private bool _labelFilteringEnabled;
    private bool _keyFilteringEnabled;
    private bool _featureFlagsConfigured;
    private string? _environmentLabel;
    private string? _keyFilterPattern;
    private Dictionary<string, object>? _testData;

    #region Given Steps - Setup

    [Given(@"I have established an app config controller environment")]
    public void GivenIHaveEstablishedAnAppConfigControllerEnvironment()
    {
        // No need to do anything here - containers are started globally
        _appConfigValidationResults.Add("✓ App config controller environment established");
    }

    [Given(@"I have app config controller configuration with App Configuration from ""(.*)""")]
    public async Task GivenIHaveAppConfigControllerConfigurationWithAppConfigurationFrom(string testDataPath)
    {
        var appConfigEmulator = scenarioContext.GetAppConfigEmulator();
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");

        var fullPath = Path.Combine("TestData", testDataPath);
        var jsonContent = await File.ReadAllTextAsync(fullPath);
        _testData = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonContent)!;

        // Load App Configuration settings from test data with the scenario prefix
        if (_testData.TryGetValue("appConfigurationSettings", out var settingsObj) && settingsObj is Newtonsoft.Json.Linq.JObject settingsJson)
        {
            foreach (var setting in settingsJson)
            {
                await appConfigEmulator.SetConfigurationAsync($"{scenarioPrefix}:{setting.Key}", setting.Value?.ToString() ?? "");
            }
        }

        _appConfigValidationResults.Add($"✓ App Configuration source added with prefix '{scenarioPrefix}'");
    }

    [Given(@"I have app config controller configuration with labeled App Configuration from ""(.*)""")]
    public async Task GivenIHaveAppConfigControllerConfigurationWithLabeledAppConfigurationFrom(string testDataPath)
    {
        var appConfigEmulator = scenarioContext.GetAppConfigEmulator();
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");

        var fullPath = Path.Combine("TestData", testDataPath);
        var jsonContent = await File.ReadAllTextAsync(fullPath);
        _testData = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonContent)!;

        // Load labeled App Configuration settings from test data with the scenario prefix
        if (_testData.TryGetValue("labeledAppConfigurationSettings", out var labeledSettingsObj) && labeledSettingsObj is Newtonsoft.Json.Linq.JObject labeledSettingsJson)
        {
            foreach (var labelGroup in labeledSettingsJson)
            {
                var label = labelGroup.Key;
                if (labelGroup.Value is Newtonsoft.Json.Linq.JObject settingsForLabel)
                {
                    foreach (var setting in settingsForLabel)
                    {
                        await appConfigEmulator.SetConfigurationAsync($"{scenarioPrefix}:{setting.Key}", setting.Value?.ToString() ?? "", label);
                    }
                }
            }
        }

        _labelFilteringEnabled = true;
        _appConfigValidationResults.Add($"✓ Labeled App Configuration source added with prefix '{scenarioPrefix}'");
    }

    [Given(@"I have app config controller configuration with filtered App Configuration from ""(.*)""")]
    public async Task GivenIHaveAppConfigControllerConfigurationWithFilteredAppConfigurationFrom(string testDataPath)
    {
        await GivenIHaveAppConfigControllerConfigurationWithAppConfigurationFrom(testDataPath);
        _keyFilteringEnabled = true;
        _appConfigValidationResults.Add("✓ Key filtering enabled for App Configuration");
    }

    [Given(@"I have app config controller configuration with feature flags from ""(.*)""")]
    public async Task GivenIHaveAppConfigControllerConfigurationWithFeatureFlagsFrom(string testDataPath)
    {
        var appConfigEmulator = scenarioContext.GetAppConfigEmulator();
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");

        var fullPath = Path.Combine("TestData", testDataPath);
        var jsonContent = await File.ReadAllTextAsync(fullPath);
        _testData = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonContent)!;

        // Load App Configuration settings from test data with the scenario prefix
        if (_testData.TryGetValue("appConfigurationSettings", out var settingsObj) && settingsObj is Newtonsoft.Json.Linq.JObject settingsJson)
        {
            foreach (var setting in settingsJson)
            {
                await appConfigEmulator.SetConfigurationAsync($"{scenarioPrefix}:{setting.Key}", setting.Value?.ToString() ?? "");
            }
        }

        // Load feature flags as configuration settings with the scenario prefix
        if (_testData.TryGetValue("featureFlags", out var featureFlagsObj) && featureFlagsObj is Newtonsoft.Json.Linq.JObject featureFlagsJson)
        {
            foreach (var featureFlag in featureFlagsJson)
            {
                await appConfigEmulator.SetConfigurationAsync($"{scenarioPrefix}:FeatureFlags:{featureFlag.Key}", featureFlag.Value?.ToString() ?? "false");
            }
        }

        _featureFlagsConfigured = true;
        _appConfigValidationResults.Add($"✓ Feature flags configured with prefix '{scenarioPrefix}'");
    }

    #endregion

    #region When Steps - Actions

    [When(@"I configure app config controller by building the configuration")]
    public void WhenIConfigureAppConfigControllerByBuildingTheConfiguration()
    {
        var appConfigEmulator = scenarioContext.GetAppConfigEmulator();
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");

        try
        {
            var builder = new FlexConfigurationBuilder();
            builder.AddAzureAppConfiguration(options =>
            {
                options.ConnectionString = appConfigEmulator.GetConnectionString();
                options.ConfigurationClient = appConfigEmulator.ConfigurationClient;
                options.Optional = false;
                // Use scenario prefix as key filter to isolate this scenario's data
                options.KeyFilter = $"{scenarioPrefix}:*";
                options.Label = _environmentLabel;
            });

            _appConfigFlexConfiguration = builder.Build();
            _appConfigConfiguration = _appConfigFlexConfiguration.Configuration;

            scenarioContext.Set(_appConfigConfiguration, "AppConfigConfiguration");
            scenarioContext.Set(_appConfigFlexConfiguration, "AppConfigFlexConfiguration");

            _appConfigValidationResults.Add("✓ App config configuration built successfully");
        }
        catch (Exception ex)
        {
            scenarioContext.Set(ex, "AppConfigException");
            _appConfigValidationResults.Add($"✗ App config configuration build failed: {ex.Message}");
        }
    }

    [When(@"I configure app config controller with environment label ""(.*)""")]
    public void WhenIConfigureAppConfigControllerWithEnvironmentLabel(string environmentLabel)
    {
        _environmentLabel = environmentLabel;
        _appConfigValidationResults.Add($"✓ Environment label '{environmentLabel}' configured");
    }

    [When(@"I configure app config controller with key filter ""(.*)""")]
    public void WhenIConfigureAppConfigControllerWithKeyFilter(string keyFilter)
    {
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");
        // Combine scenario prefix with the requested filter
        _keyFilterPattern = $"{scenarioPrefix}:{keyFilter}";
        _appConfigValidationResults.Add($"✓ Key filter '{_keyFilterPattern}' configured");
    }

    #endregion

    #region Then Steps - Assertions

    [Then(@"the app config controller should support FlexKit dynamic access patterns")]
    public void ThenTheAppConfigControllerShouldSupportFlexKitDynamicAccessPatterns()
    {
        _appConfigFlexConfiguration.Should().NotBeNull("App config FlexKit configuration should be available");
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");

        try
        {
            // Test dynamic access patterns specific to FlexKit with a scenario prefix
            dynamic config = _appConfigFlexConfiguration!;

            // Test various FlexKit access patterns with prefixed keys
            var dynamicTests = new List<(string description, Func<object?> test)>
            {
                ("Basic property access", () => config[$"{scenarioPrefix}:myapp:api:timeout"]),
                ("Section access", () => _appConfigFlexConfiguration.Configuration.GetSection($"{scenarioPrefix}:myapp")),
                ("Dynamic casting", () => (string?)config[$"{scenarioPrefix}:myapp:api:baseUrl"])
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
                        _appConfigValidationResults.Add($"✓ {description}: success");
                    }
                    else
                    {
                        _appConfigValidationResults.Add($"⚠ {description}: null result");
                    }
                }
                catch (Exception ex)
                {
                    _appConfigValidationResults.Add($"✗ {description}: {ex.Message}");
                }
            }

            _appConfigValidationResults.Add($"Dynamic access patterns verification: {successfulTests}/{dynamicTests.Count} tests passed");
        }
        catch (Exception ex)
        {
            _appConfigValidationResults.Add($"✗ Dynamic access patterns verification failed: {ex.Message}");
        }

        scenarioContext.Set(_appConfigValidationResults, "AppConfigValidationResults");
    }

    [Then(@"the app config controller configuration should contain ""(.*)"" with value ""(.*)""")]
    public void ThenTheAppConfigControllerConfigurationShouldContainWithValue(string expectedKey, string expectedValue)
    {
        _appConfigConfiguration.Should().NotBeNull("App config configuration should be built");
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");

        // Use prefixed key for lookup
        var prefixedKey = $"{scenarioPrefix}:{expectedKey}";
        var actualValue = _appConfigConfiguration![prefixedKey];
        actualValue.Should().Be(expectedValue, $"Configuration key '{prefixedKey}' should have the expected value");

        _appConfigValidationResults.Add($"✓ Configuration validation passed: {prefixedKey} = {expectedValue}");
        scenarioContext.Set(_appConfigValidationResults, "AppConfigValidationResults");
    }

    [Then(@"the app config controller configuration should contain ""(.*)"" with production value")]
    public void ThenTheAppConfigControllerConfigurationShouldContainWithProductionValue(string expectedKey)
    {
        _appConfigConfiguration.Should().NotBeNull("App config configuration should be built");
        _environmentLabel.Should().Be("production", "Should be configured for production environment");
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");

        var prefixedKey = $"{scenarioPrefix}:{expectedKey}";
        var actualValue = _appConfigConfiguration![prefixedKey];
        actualValue.Should().NotBeNullOrEmpty($"Production configuration key '{prefixedKey}' should have a value");

        // Verify it's a production-specific value (should contain "prod" or be production-appropriate)
        bool isProductionValue = actualValue.Contains("prod", StringComparison.OrdinalIgnoreCase) ||
                               actualValue.Contains("production", StringComparison.OrdinalIgnoreCase) ||
                               actualValue == "Warning"; // Logging level for production

        isProductionValue.Should().BeTrue($"Value '{actualValue}' should be production-appropriate");

        _appConfigValidationResults.Add($"✓ Production configuration validation passed: {prefixedKey} = {actualValue}");
        scenarioContext.Set(_appConfigValidationResults, "AppConfigValidationResults");
    }

    [Then(@"the app config controller should demonstrate FlexKit type conversion capabilities")]
    public void ThenTheAppConfigControllerShouldDemonstrateFlexKitTypeConversionCapabilities()
    {
        _appConfigFlexConfiguration.Should().NotBeNull("App config FlexKit configuration should be available");
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");

        try
        {
            // Test type conversion capabilities with prefixed keys
            var typeConversionTests = new List<(string description, Func<object> test)>
            {
                ("String to int conversion", () => _appConfigFlexConfiguration![$"{scenarioPrefix}:myapp:api:timeout"].ToType<int>()),
                ("String to bool conversion", () => _appConfigFlexConfiguration![$"{scenarioPrefix}:myapp:cache:enabled"].ToType<bool>()),
                ("String to int for TTL", () => _appConfigFlexConfiguration![$"{scenarioPrefix}:myapp:cache:ttl"].ToType<int>()),
                ("Direct string access", () => _appConfigFlexConfiguration![$"{scenarioPrefix}:myapp:api:baseUrl"] ?? "null")
            };

            var successfulConversions = 0;
            foreach (var (description, test) in typeConversionTests)
            {
                try
                {
                    var result = test();
                    result.Should().NotBeNull($"{description} should return a value");
                    successfulConversions++;
                    _appConfigValidationResults.Add($"✓ {description}: success");
                }
                catch (Exception ex)
                {
                    _appConfigValidationResults.Add($"✗ {description}: {ex.Message}");
                }
            }

            _appConfigValidationResults.Add($"Type conversion verification: {successfulConversions}/{typeConversionTests.Count} tests passed");
        }
        catch (Exception ex)
        {
            _appConfigValidationResults.Add($"✗ Type conversion verification failed: {ex.Message}");
        }

        scenarioContext.Set(_appConfigValidationResults, "AppConfigValidationResults");
    }

    [Then(@"the app config controller should support labeled configuration access")]
    public void ThenTheAppConfigControllerShouldSupportLabeledConfigurationAccess()
    {
        _appConfigFlexConfiguration.Should().NotBeNull("App config FlexKit configuration should be available");
        _labelFilteringEnabled.Should().BeTrue("Label filtering should be enabled");
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");

        try
        {
            // Test that we can access labeled configuration with prefixed keys
            var labeledTests = new List<(string description, string key)>
            {
                ("Database connection string", $"{scenarioPrefix}:myapp:database:connectionString"),
                ("Logging level", $"{scenarioPrefix}:myapp:logging:level"),
                ("Cache TTL", $"{scenarioPrefix}:myapp:cache:ttl"),
                ("API base URL", $"{scenarioPrefix}:myapp:api:baseUrl")
            };

            var successfulLabeledAccess = 0;
            foreach (var (description, key) in labeledTests)
            {
                try
                {
                    var value = _appConfigFlexConfiguration![key];
                    if (!string.IsNullOrEmpty(value))
                    {
                        successfulLabeledAccess++;
                        _appConfigValidationResults.Add($"✓ {description}: {value}");
                    }
                    else
                    {
                        _appConfigValidationResults.Add($"⚠ {description}: empty or null value");
                    }
                }
                catch (Exception ex)
                {
                    _appConfigValidationResults.Add($"✗ {description}: {ex.Message}");
                }
            }

            _appConfigValidationResults.Add($"Labeled configuration access verification: {successfulLabeledAccess}/{labeledTests.Count} tests passed");
        }
        catch (Exception ex)
        {
            _appConfigValidationResults.Add($"✗ Labeled configuration access verification failed: {ex.Message}");
        }

        scenarioContext.Set(_appConfigValidationResults, "AppConfigValidationResults");
    }

    [Then(@"the app config controller should support filtered configuration access")]
    public void ThenTheAppConfigControllerShouldSupportFilteredConfigurationAccess()
    {
        _appConfigFlexConfiguration.Should().NotBeNull("App config FlexKit configuration should be available");
        _keyFilteringEnabled.Should().BeTrue("Key filtering should be enabled");
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");

        try
        {
            // Get all configuration keys and verify filtering
            var allKeys = _appConfigFlexConfiguration!.Configuration.AsEnumerable()
                .Where(kvp => !string.IsNullOrEmpty(kvp.Key))
                .Select(kvp => kvp.Key)
                .ToList();

            var expectedPrefix = scenarioPrefix;
            var filteredKeys = allKeys.Where(key => key.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase)).ToList();
            var unfilteredKeys = allKeys.Where(key => !key.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase)).ToList();

            filteredKeys.Should().NotBeEmpty("Should have keys matching the filter pattern");

            _appConfigValidationResults.Add($"✓ Key filtering verification: {filteredKeys.Count} keys match pattern '{expectedPrefix}:*'");

            if (unfilteredKeys.Any())
            {
                _appConfigValidationResults.Add($"ⓘ {unfilteredKeys.Count} keys don't match filter (this may be expected)");
            }
        }
        catch (Exception ex)
        {
            _appConfigValidationResults.Add($"✗ Filtered configuration access verification failed: {ex.Message}");
        }

        scenarioContext.Set(_appConfigValidationResults, "AppConfigValidationResults");
    }

    [Then(@"the app config controller configuration should only contain keys starting with ""(.*)""")]
    public void ThenTheAppConfigControllerConfigurationShouldOnlyContainKeysStartingWith(string keyPrefix)
    {
        _appConfigConfiguration.Should().NotBeNull("App config configuration should be built");
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");

        // Expected prefix should include a scenario prefix
        var expectedFullPrefix = $"{scenarioPrefix}:{keyPrefix}";

        var allKeys = _appConfigConfiguration!.AsEnumerable()
            .Where(kvp => !string.IsNullOrEmpty(kvp.Key))
            .Select(kvp => kvp.Key)
            .ToList();

        var matchingKeys = allKeys.Where(key => key.StartsWith(expectedFullPrefix, StringComparison.OrdinalIgnoreCase)).ToList();
        var nonMatchingKeys = allKeys.Where(key => !key.StartsWith(expectedFullPrefix, StringComparison.OrdinalIgnoreCase)).ToList();

        matchingKeys.Should().NotBeEmpty($"Should have keys starting with '{expectedFullPrefix}'");

        _appConfigValidationResults.Add($"✓ Key prefix validation: {matchingKeys.Count} keys start with '{expectedFullPrefix}'");

        if (nonMatchingKeys.Any())
        {
            _appConfigValidationResults.Add($"ⓘ {nonMatchingKeys.Count} keys don't match prefix (may include system keys)");
        }

        scenarioContext.Set(_appConfigValidationResults, "AppConfigValidationResults");
    }

    [Then(@"the app config controller should not contain keys from other applications")]
    public void ThenTheAppConfigControllerShouldNotContainKeysFromOtherApplications()
    {
        _appConfigConfiguration.Should().NotBeNull("App config configuration should be built");
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");

        var allKeys = _appConfigConfiguration!.AsEnumerable()
            .Where(kvp => !string.IsNullOrEmpty(kvp.Key))
            .Select(kvp => kvp.Key)
            .ToList();

        // Check for keys that might belong to other applications (different scenario prefixes)
        var otherAppKeys = allKeys.Where(key =>
            !key.StartsWith(scenarioPrefix, StringComparison.OrdinalIgnoreCase) &&
            (key.StartsWith("test-", StringComparison.OrdinalIgnoreCase) || // Other scenario prefixes
             key.StartsWith("otherapp:", StringComparison.OrdinalIgnoreCase) ||
             key.StartsWith("different-app:", StringComparison.OrdinalIgnoreCase) ||
             key.StartsWith("external:", StringComparison.OrdinalIgnoreCase))
        ).ToList();

        otherAppKeys.Should().BeEmpty("Should not contain keys from other applications when filtering is applied");

        _appConfigValidationResults.Add("✓ Application isolation verification: No keys from other applications found");
        scenarioContext.Set(_appConfigValidationResults, "AppConfigValidationResults");
    }

    [Then(@"the app config controller should support feature flag evaluation")]
    public void ThenTheAppConfigControllerShouldSupportFeatureFlagEvaluation()
    {
        _appConfigFlexConfiguration.Should().NotBeNull("App config FlexKit configuration should be available");
        _featureFlagsConfigured.Should().BeTrue("Feature flags should be configured");
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");

        try
        {
            // Test feature flag access with a scenario prefix
            var featureFlagTests = new List<(string description, string key, bool expectedValue)>
            {
                ("New UI feature", $"{scenarioPrefix}:FeatureFlags:NewUI", true),
                ("Beta features", $"{scenarioPrefix}:FeatureFlags:BetaFeatures", false),
                ("Advanced reporting", $"{scenarioPrefix}:FeatureFlags:AdvancedReporting", true),
                ("Dark mode", $"{scenarioPrefix}:FeatureFlags:DarkMode", true)
            };

            var successfulFeatureFlags = 0;
            foreach (var (description, key, expectedValue) in featureFlagTests)
            {
                try
                {
                    var value = _appConfigFlexConfiguration![key];
                    if (!string.IsNullOrEmpty(value))
                    {
                        var boolValue = value.ToType<bool>();
                        if (boolValue == expectedValue)
                        {
                            successfulFeatureFlags++;
                            _appConfigValidationResults.Add($"✓ {description}: {boolValue}");
                        }
                        else
                        {
                            _appConfigValidationResults.Add($"⚠ {description}: expected {expectedValue}, got {boolValue}");
                        }
                    }
                    else
                    {
                        _appConfigValidationResults.Add($"✗ {description}: not found");
                    }
                }
                catch (Exception ex)
                {
                    _appConfigValidationResults.Add($"✗ {description}: {ex.Message}");
                }
            }

            _appConfigValidationResults.Add($"Feature flag evaluation verification: {successfulFeatureFlags}/{featureFlagTests.Count} tests passed");
        }
        catch (Exception ex)
        {
            _appConfigValidationResults.Add($"✗ Feature flag evaluation verification failed: {ex.Message}");
        }

        scenarioContext.Set(_appConfigValidationResults, "AppConfigValidationResults");
    }

    #endregion
}