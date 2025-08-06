using Azure.Core;
using Azure.Data.AppConfiguration;
using FlexKit.Configuration.Core;
using Reqnroll;
using System.Text.Json;
using Azure;
using Microsoft.Extensions.Configuration;
using Azure.Identity;

namespace FlexKit.Configuration.Providers.Azure.IntegrationTests.Utils;

/// <summary>
/// Model for Azure test data configuration.
/// Represents the structure of test data JSON files used in integration tests.
/// </summary>
public class AzureTestDataModel
{
    /// <summary>
    /// Key Vault secrets data.
    /// </summary>
    public Dictionary<string, string>? KeyVaultSecrets { get; set; }

    /// <summary>
    /// App Configuration settings data.
    /// </summary>
    public Dictionary<string, string>? AppConfigurationSettings { get; set; }

    /// <summary>
    /// App Configuration settings with labels.
    /// </summary>
    public Dictionary<string, Dictionary<string, string>>? LabeledAppConfigurationSettings { get; set; }

    /// <summary>
    /// Feature flags configuration.
    /// </summary>
    public Dictionary<string, bool>? FeatureFlags { get; set; }

    /// <summary>
    /// JSON secrets that should be processed as hierarchical configuration.
    /// </summary>
    public Dictionary<string, object>? JsonSecrets { get; set; }

    /// <summary>
    /// LocalStack configuration settings.
    /// </summary>
    public LocalStackConfiguration? LocalStack { get; set; }

    /// <summary>
    /// Azure service configuration.
    /// </summary>
    public AzureConfiguration? Azure { get; set; }
}

/// <summary>
/// LocalStack configuration model.
/// </summary>
public class LocalStackConfiguration
{
    /// <summary>
    /// Azure services to enable in LocalStack.
    /// </summary>
    public string Services { get; set; } = "keyvault,appconfig";

    /// <summary>
    /// Debug mode setting.
    /// </summary>
    public bool Debug { get; set; } = true;

    /// <summary>
    /// Port configuration.
    /// </summary>
    public int Port { get; set; } = 4566;
}

/// <summary>
/// Azure configuration model.
/// </summary>
public class AzureConfiguration
{
    /// <summary>
    /// Azure region.
    /// </summary>
    public string Region { get; set; } = "eastus";

    /// <summary>
    /// Key Vault configuration.
    /// </summary>
    public KeyVaultConfiguration? KeyVault { get; set; }

    /// <summary>
    /// App Configuration settings.
    /// </summary>
    public AppConfigurationConfiguration? AppConfiguration { get; set; }
}

/// <summary>
/// Key Vault configuration model.
/// </summary>
public class KeyVaultConfiguration
{
    /// <summary>
    /// Key Vault URI.
    /// </summary>
    public string Uri { get; set; } = "https://test-vault.vault.azure.net/";

    /// <summary>
    /// Enable JSON processing.
    /// </summary>
    public bool JsonProcessor { get; set; } = false;

    /// <summary>
    /// Optional source flag.
    /// </summary>
    public bool Optional { get; set; } = true;

    /// <summary>
    /// Reload interval in minutes.
    /// </summary>
    public int? ReloadAfterMinutes { get; set; }
}

/// <summary>
/// App Configuration settings model.
/// </summary>
public class AppConfigurationConfiguration
{
    /// <summary>
    /// App Configuration connection string.
    /// </summary>
    public string ConnectionString { get; set; } = "Endpoint=https://test-appconfig.azconfig.io;Id=test;Secret=test";

    /// <summary>
    /// Configuration label filter.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Key filter pattern.
    /// </summary>
    public string? KeyFilter { get; set; }

    /// <summary>
    /// Optional source flag.
    /// </summary>
    public bool Optional { get; set; } = true;

    /// <summary>
    /// Reload interval in minutes.
    /// </summary>
    public int? ReloadAfterMinutes { get; set; }
}

/// <summary>
/// Extension methods for Azure test scenarios.
/// </summary>
public static class AzureTestExtensions
{
    /// <summary>
    /// Registers the Azure test configuration builder for cleanup.
    /// </summary>
    /// <param name="scenarioContext">Scenario context</param>
    /// <param name="builder">Azure test configuration builder</param>
    public static void RegisterForCleanup(this ScenarioContext scenarioContext, AzureTestConfigurationBuilder builder)
    {
        scenarioContext.Set(builder, "AzureTestConfigurationBuilder");
        
        // Register LocalStack helper for cleanup if it exists
        var localStackHelper = builder.GetLocalStackHelper();
        if (localStackHelper != null)
        {
            scenarioContext.RegisterForCleanup(builder);
        }
    }

    /// <summary>
    /// Gets the Azure test configuration builder from scenario context.
    /// </summary>
    /// <param name="scenarioContext">Scenario context</param>
    /// <returns>Azure test configuration builder or null if not found</returns>
    public static AzureTestConfigurationBuilder? GetAzureTestConfigurationBuilder(this ScenarioContext scenarioContext)
    {
        return scenarioContext.TryGetValue("AzureTestConfigurationBuilder", out AzureTestConfigurationBuilder builder) 
            ? builder 
            : null;
    }

    /// <summary>
    /// Loads Azure test data from JSON file.
    /// </summary>
    /// <param name="testDataPath">Path to test data file</param>
    /// <returns>Parsed Azure test data model</returns>
    public static AzureTestDataModel LoadAzureTestData(string testDataPath)
    {
        if (!File.Exists(testDataPath))
        {
            throw new FileNotFoundException($"Azure test data file not found: {testDataPath}");
        }

        var jsonContent = File.ReadAllText(testDataPath);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        return JsonSerializer.Deserialize<AzureTestDataModel>(jsonContent, options) ?? new AzureTestDataModel();
    }

    /// <summary>
    /// Converts Azure test data to Key Vault secrets format.
    /// </summary>
    /// <param name="testData">Azure test data</param>
    /// <returns>Dictionary of secret names and values</returns>
    public static Dictionary<string, string> ToKeyVaultSecrets(this AzureTestDataModel testData)
    {
        var secrets = new Dictionary<string, string>();

        // Add regular secrets
        if (testData.KeyVaultSecrets != null)
        {
            foreach (var secret in testData.KeyVaultSecrets)
            {
                // Convert configuration keys to Key Vault secret names (replace : with --)
                var secretName = secret.Key.Replace(":", "--");
                secrets[secretName] = secret.Value;
            }
        }

        // Add JSON secrets
        if (testData.JsonSecrets != null)
        {
            foreach (var jsonSecret in testData.JsonSecrets)
            {
                var secretName = jsonSecret.Key.Replace(":", "--");
                var jsonValue = JsonSerializer.Serialize(jsonSecret.Value);
                secrets[secretName] = jsonValue;
            }
        }

        return secrets;
    }

    /// <summary>
    /// Converts Azure test data to App Configuration settings format.
    /// </summary>
    /// <param name="testData">Azure test data</param>
    /// <param name="label">Optional label for settings</param>
    /// <returns>List of configuration settings</returns>
    public static List<ConfigurationSetting> ToAppConfigurationSettings(this AzureTestDataModel testData, string? label = null)
    {
        var settings = new List<ConfigurationSetting>();

        // Add regular settings
        if (testData.AppConfigurationSettings != null)
        {
            foreach (var setting in testData.AppConfigurationSettings)
            {
                settings.Add(new ConfigurationSetting(setting.Key, setting.Value)
                {
                    Label = label
                });
            }
        }

        // Add labeled settings
        if (testData.LabeledAppConfigurationSettings != null)
        {
            foreach (var labeledGroup in testData.LabeledAppConfigurationSettings)
            {
                var settingLabel = labeledGroup.Key;
                foreach (var setting in labeledGroup.Value)
                {
                    settings.Add(new ConfigurationSetting(setting.Key, setting.Value)
                    {
                        Label = settingLabel
                    });
                }
            }
        }

        // Add feature flags
        if (testData.FeatureFlags != null)
        {
            foreach (var featureFlag in testData.FeatureFlags)
            {
                var flagKey = $"FeatureFlags:{featureFlag.Key}";
                settings.Add(new ConfigurationSetting(flagKey, featureFlag.Value.ToString().ToLowerInvariant())
                {
                    Label = label
                });
            }
        }

        return settings;
    }

    /// <summary>
    /// Validates that FlexConfig contains expected Azure configuration values.
    /// </summary>
    /// <param name="flexConfig">FlexConfig instance</param>
    /// <param name="expectedKey">Expected configuration key</param>
    /// <param name="expectedValue">Expected configuration value</param>
    /// <returns>True if validation passes, false otherwise</returns>
    public static bool ValidateAzureConfiguration(this IFlexConfig flexConfig, string expectedKey, string expectedValue)
    {
        try
        {
            var actualValue = flexConfig[expectedKey];
            return string.Equals(actualValue, expectedValue, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets all configuration keys from FlexConfig that start with a specific prefix.
    /// </summary>
    /// <param name="flexConfig">FlexConfig instance</param>
    /// <param name="prefix">Key prefix to filter</param>
    /// <returns>List of matching configuration keys</returns>
    public static List<string> GetConfigurationKeysWithPrefix(this IFlexConfig flexConfig, string prefix)
    {
        var keys = new List<string>();
        
        try
        {
            var configuration = flexConfig.Configuration;
            foreach (var kvp in configuration.AsEnumerable())
            {
                if (kvp.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    keys.Add(kvp.Key);
                }
            }
        }
        catch
        {
            // Return empty list if enumeration fails
        }

        return keys;
    }

    /// <summary>
    /// Creates a mock Azure credential for testing.
    /// </summary>
    /// <returns>Mock token credential</returns>
    public static TokenCredential CreateMockAzureCredential()
    {
        return new MockAzureCredential();
    }

    /// <summary>
    /// Validates JSON processing by checking flattened configuration structure.
    /// </summary>
    /// <param name="flexConfig">FlexConfig instance</param>
    /// <param name="jsonSecretKey">Key of the JSON secret</param>
    /// <param name="expectedFlattenedKeys">Expected flattened keys</param>
    /// <returns>True if JSON processing validation passes</returns>
    public static bool ValidateJsonProcessing(this IFlexConfig flexConfig, string jsonSecretKey, params string[] expectedFlattenedKeys)
    {
        try
        {
            foreach (var expectedKey in expectedFlattenedKeys)
            {
                var fullKey = $"{jsonSecretKey}:{expectedKey}";
                var value = flexConfig[fullKey];
                if (string.IsNullOrEmpty(value))
                {
                    return false;
                }
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Simulates Azure service errors for testing error handling.
    /// </summary>
    /// <param name="errorType">Type of error to simulate</param>
    /// <returns>Exception representing the simulated error</returns>
    public static Exception SimulateAzureServiceError(AzureServiceErrorType errorType)
    {
        return errorType switch
        {
            AzureServiceErrorType.NetworkFailure => new HttpRequestException("Network failure simulated for testing"),
            AzureServiceErrorType.AccessDenied => new UnauthorizedAccessException("Access denied simulated for testing"),
            AzureServiceErrorType.InvalidCredentials => new AuthenticationFailedException("Invalid credentials simulated for testing"),
            AzureServiceErrorType.RateLimitExceeded => new RequestFailedException(429, "Rate limit exceeded simulated for testing"),
            AzureServiceErrorType.ServiceUnavailable => new RequestFailedException(503, "Service unavailable simulated for testing"),
            AzureServiceErrorType.InvalidConfiguration => new InvalidOperationException("Invalid configuration simulated for testing"),
            _ => new Exception("Unknown error simulated for testing")
        };
    }
}

/// <summary>
/// Types of Azure service errors that can be simulated for testing.
/// </summary>
public enum AzureServiceErrorType
{
    NetworkFailure,
    AccessDenied,
    InvalidCredentials,
    RateLimitExceeded,
    ServiceUnavailable,
    InvalidConfiguration
}

/// <summary>
/// Mock Azure credential for testing scenarios.
/// </summary>
internal class MockAzureCredential : TokenCredential
{
    public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        return new AccessToken("mock-access-token", DateTimeOffset.UtcNow.AddHours(1));
    }

    public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        return new ValueTask<AccessToken>(GetToken(requestContext, cancellationToken));
    }
}