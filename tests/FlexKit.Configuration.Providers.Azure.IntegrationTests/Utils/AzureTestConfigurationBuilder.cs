using FlexKit.Configuration.Core;
using FlexKit.Configuration.Providers.Azure.Sources;
using FlexKit.IntegrationTests.Utils;
using Reqnroll;
using System.Text.Json;
using JetBrains.Annotations;
// ReSharper disable TooManyArguments
// ReSharper disable NullableWarningSuppressionIsUsed

// ReSharper disable MethodTooLong
// ReSharper disable FlagArgument

namespace FlexKit.Configuration.Providers.Azure.IntegrationTests.Utils;

/// <summary>
/// Test configuration builder specifically for Azure Key Vault and App Configuration testing.
/// Inherits from BaseTestConfigurationBuilder to provide Azure-specific configuration methods.
/// </summary>
public class AzureTestConfigurationBuilder : BaseTestConfigurationBuilder<AzureTestConfigurationBuilder>
{
    private AppConfigurationEmulatorContainer _appConfiguration;
    /// <summary>
    /// Initializes a new instance of AzureTestConfigurationBuilder.
    /// </summary>
    /// <param name="scenarioContext">Optional scenario context for automatic cleanup</param>
    public AzureTestConfigurationBuilder(ScenarioContext? scenarioContext = null) : base(scenarioContext)
    {
        _appConfiguration = new AppConfigurationEmulatorContainer();
    }

    public AzureTestConfigurationBuilder() : this(null) { }

    /// <summary>
    /// Creates a temporary JSON file with Key Vault test data and configures Key Vault from it.
    /// For error scenarios, this can simulate failures based on simulation flags.
    /// </summary>
    /// <param name="testDataPath">Path to test data JSON file</param>
    /// <param name="optional">Whether the source is optional</param>
    /// <param name="jsonProcessor">Whether to enable JSON processing</param>
    /// <param name="simulateFailure">Whether to simulate a failure for error testing</param>
    /// <returns>This builder for method chaining</returns>
    public AzureTestConfigurationBuilder AddKeyVaultFromTestData(string testDataPath, bool optional = true, bool jsonProcessor = false, bool simulateFailure = false)
    {
        var testData = LoadTestDataFromFile(testDataPath);
        
        if (simulateFailure && !optional)
        {
            // For error scenarios, create a source that will fail during Build()
            var failingSource = new FailingConfigurationSource
            {
                ErrorMessage = "Failed to load configuration from Azure Key Vault. Simulated failure for testing.",
                SourceType = "Key Vault"
            };
            return AddSource(failingSource);
        }
        
        var keyVaultData = ExtractKeyVaultData(testData, jsonProcessor);
        
        // Create an in-memory configuration source with the test data
        var memorySource = new Microsoft.Extensions.Configuration.Memory.MemoryConfigurationSource
        {
            InitialData = keyVaultData
        };
        
        return AddSource(memorySource);
    }
    
    /// <summary>
    /// Extracts Key Vault data from test data and processes JSON if enabled.
    /// </summary>
    /// <param name="testData">Flattened test data</param>
    /// <param name="jsonProcessor">Whether to process JSON secrets</param>
    /// <returns>Dictionary of configuration data</returns>
    private static Dictionary<string, string?> ExtractKeyVaultData(Dictionary<string, string?> testData, bool jsonProcessor)
    {
        var result = new Dictionary<string, string?>();
        
        // Extract Key Vault secrets
        foreach (var kvp in testData)
        {
            if (kvp.Key.StartsWith("keyVaultSecrets:", StringComparison.OrdinalIgnoreCase))
            {
                var secretName = kvp.Key.Substring("keyVaultSecrets:".Length);
                var secretValue = kvp.Value;
                
                // Transform a secret name from Azure format (-- separators) to configuration format (: separators)
                var configKey = secretName.Replace("--", ":");
                
                // Check if this is a JSON secret and JSON processing is enabled
                if (jsonProcessor && IsJsonSecret(secretValue))
                {
                    try
                    {
                        var jsonData = JsonSerializer.Deserialize<Dictionary<string, object>>(secretValue!);
                        if (jsonData != null)
                        {
                            var flattened = FlattenConfiguration(jsonData, configKey);
                            foreach (var flatKvp in flattened)
                            {
                                result[flatKvp.Key] = flatKvp.Value;
                            }
                        }
                    }
                    catch
                    {
                        // If JSON parsing fails, treat as regular secret
                        result[configKey] = secretValue;
                    }
                }
                else
                {
                    result[configKey] = secretValue;
                }
            }
        }
        
        // Extract JSON secrets if JSON processing is enabled
        if (jsonProcessor)
        {
            // Process the jsonSecrets section which contains structured JSON data
            foreach (var kvp in testData)
            {
                if (kvp.Key.StartsWith("jsonSecrets:", StringComparison.OrdinalIgnoreCase))
                {
                    var keyParts = kvp.Key.Split(':');
                    if (keyParts.Length >= 3) // jsonSecrets:secretName:property
                    {
                        var secretName = keyParts[1];
                        var propertyPath = string.Join(":", keyParts.Skip(2));
                        var configurationKey = $"{secretName}:{propertyPath}";
                        
                        result[configurationKey] = kvp.Value;
                    }
                }
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Checks if a string value appears to be JSON.
    /// </summary>
    /// <param name="value">String value to check</param>
    /// <returns>True if the value looks like JSON</returns>
    private static bool IsJsonSecret(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }
        
        value = value.Trim();
        return (value.StartsWith("{") && value.EndsWith("}")) ||
               (value.StartsWith("[") && value.EndsWith("]"));
    }

    /// <summary>
    /// Adds Azure App Configuration as a configuration source.
    /// </summary>
    /// <param name="connectionString">App Configuration connection string</param>
    /// <param name="optional">Whether the source is optional</param>
    /// <param name="label">Configuration label filter</param>
    /// <param name="keyFilter">Key filter pattern</param>
    /// <returns>This builder for method chaining</returns>
    public async void StartAzureAppConfiguration()
    {
        await _appConfiguration.StartAsync();
    }

    /// <summary>
    /// Creates a temporary JSON file with App Configuration test data and configures App Configuration from it.
    /// For error scenarios, this can simulate failures based on simulation flags.
    /// </summary>
    /// <param name="testDataPath">Path to test data JSON file</param>
    /// <param name="optional">Whether the source is optional</param>
    /// <param name="label">Configuration label filter</param>
    /// <param name="keyFilter">Key filter pattern</param>
    /// <param name="simulateFailure">Whether to simulate a failure for error testing</param>
    /// <returns>This builder for method chaining</returns>
    public async void AddAppConfigurationFromTestData(string testDataPath, bool optional = true, string? label = null, string? keyFilter = null, bool simulateFailure = false)
    {
        if (simulateFailure && !optional)
        {
            // For error scenarios, create a source that will fail during Build()
            var failingSource = new FailingConfigurationSource
            {
                ErrorMessage = "Failed to load configuration from Azure App Configuration 'Endpoint=http://localhost:63657/;Id=test;Secret=test'. Simulated failure for testing.",
                SourceType = "App Configuration"
            };
            AddSource(failingSource);
        }
        else
        {
            await _appConfiguration.CreateTestDataAsync(testDataPath);
        }
    }
    
    /// <summary>
    /// Extracts App Configuration data from test data based on label and key filter.
    /// </summary>
    /// <param name="testData">Flattened test data</param>
    /// <param name="label">Label filter (e.g., "production")</param>
    /// <param name="keyFilter">Key filter pattern (e.g., "myapp:*")</param>
    /// <returns>Dictionary of configuration data</returns>
    private static Dictionary<string, string?> ExtractAppConfigurationData(Dictionary<string, string?> testData, string? label, string? keyFilter)
    {
        var result = new Dictionary<string, string?>();
        
        // Extract basic app configuration settings
        foreach (var kvp in testData)
        {
            if (kvp.Key.StartsWith("appConfigurationSettings:", StringComparison.OrdinalIgnoreCase))
            {
                var configKey = kvp.Key.Substring("appConfigurationSettings:".Length);
                
                // Apply key filter if specified
                if (keyFilter != null && !MatchesKeyFilter(configKey, keyFilter))
                {
                    continue;
                }
                
                result[configKey] = kvp.Value;
            }
        }
        
        // Extract labeled settings if the label is specified
        if (!string.IsNullOrEmpty(label))
        {
            var labelPrefix = $"labeledAppConfigurationSettings:{label}:";
            foreach (var kvp in testData)
            {
                if (kvp.Key.StartsWith(labelPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    var configKey = kvp.Key.Substring(labelPrefix.Length);
                    
                    // Apply key filter if specified
                    if (keyFilter != null && !MatchesKeyFilter(configKey, keyFilter))
                    {
                        continue;
                    }
                    
                    // Labeled settings override basic settings
                    result[configKey] = kvp.Value;
                }
            }
        }
        
        // Extract feature flags
        foreach (var kvp in testData)
        {
            if (kvp.Key.StartsWith("featureFlags:", StringComparison.OrdinalIgnoreCase))
            {
                var flagKey = $"FeatureFlags:{kvp.Key.Substring("featureFlags:".Length)}";
                
                // Apply key filter if specified
                if (keyFilter != null && !MatchesKeyFilter(flagKey, keyFilter))
                {
                    continue;
                }
                
                result[flagKey] = kvp.Value;
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Checks if a configuration key matches the specified filter pattern.
    /// </summary>
    /// <param name="key">Configuration key to check</param>
    /// <param name="filter">Filter pattern (supports * wildcard)</param>
    /// <returns>True if the key matches the filter</returns>
    private static bool MatchesKeyFilter(string key, string filter)
    {
        if (string.IsNullOrEmpty(filter) || filter == "*")
        {
            return true;
        }
        
        if (filter.EndsWith("*"))
        {
            var prefix = filter.Substring(0, filter.Length - 1);
            return key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }
        
        return string.Equals(key, filter, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Creates a temporary JSON file with Key Vault test data using JSON processing.
    /// </summary>
    /// <param name="testDataPath">Path to test data JSON file</param>
    /// <param name="optional">Whether the source is optional</param>
    /// <param name="jsonProcessor">Whether to enable JSON processing</param>
    /// <returns>This builder for method chaining</returns>
    public AzureTestConfigurationBuilder AddKeyVaultFromTestDataWithJsonProcessing(string testDataPath, bool optional = true, bool jsonProcessor = true)
    {
        return AddKeyVaultFromTestData(testDataPath, optional, jsonProcessor);
    }

    /// <summary>
    /// Creates a temporary JSON file with App Configuration test data using a specific label.
    /// </summary>
    /// <param name="testDataPath">Path to test data JSON file</param>
    /// <param name="label">Configuration label</param>
    /// <param name="optional">Whether the source is optional</param>
    /// <returns>This builder for method chaining</returns>
    public AzureTestConfigurationBuilder AddAppConfigurationFromTestDataWithLabel(string testDataPath, string label, bool optional = true)
    {
        return AddAppConfigurationFromTestData(testDataPath, optional, label);
    }

    /// <summary>
    /// Builds a FlexConfiguration instance with Azure sources.
    /// </summary>
    /// <returns>The built FlexConfiguration</returns>
    public IFlexConfig BuildFlexConfig()
    {
        var configuration = Build();
        return new FlexConfiguration(configuration);
    }

    /// <summary>
    /// Loads test configuration data from a JSON file.
    /// </summary>
    /// <param name="testDataPath">Path to the test data file</param>
    /// <returns>Dictionary of configuration data</returns>
    private static Dictionary<string, string?> LoadTestDataFromFile(string testDataPath)
    {
        if (!File.Exists(testDataPath))
        {
            throw new FileNotFoundException($"Test data file not found: {testDataPath}");
        }

        var jsonContent = File.ReadAllText(testDataPath);
        var testData = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent);

        return FlattenConfiguration(testData ?? new Dictionary<string, object>());
    }

    /// <summary>
    /// Flattens nested configuration data into configuration key format.
    /// </summary>
    /// <param name="data">Nested configuration data</param>
    /// <param name="prefix">Key prefix for nested data</param>
    /// <returns>Flattened configuration dictionary</returns>
    private static Dictionary<string, string?> FlattenConfiguration(Dictionary<string, object> data, string prefix = "")
    {
        var result = new Dictionary<string, string?>();

        foreach (var kvp in data)
        {
            var key = string.IsNullOrEmpty(prefix) ? kvp.Key : $"{prefix}:{kvp.Key}";

            if (kvp.Value is JsonElement jsonElement)
            {
                HandleJsonElement(result, key, jsonElement);
            }
            else if (kvp.Value is Dictionary<string, object> nestedDict)
            {
                var nested = FlattenConfiguration(nestedDict, key);
                foreach (var nestedKvp in nested)
                {
                    result[nestedKvp.Key] = nestedKvp.Value;
                }
            }
            else
            {
                result[key] = kvp.Value.ToString();
            }
        }

        return result;
    }

    /// <summary>
    /// Handles JsonElement conversion to configuration values.
    /// </summary>
    /// <param name="result">Result dictionary to populate</param>
    /// <param name="key">Configuration key</param>
    /// <param name="jsonElement">JsonElement to process</param>
    private static void HandleJsonElement(Dictionary<string, string?> result, string key, JsonElement jsonElement)
    {
        switch (jsonElement.ValueKind)
        {
            case JsonValueKind.Object:
                var nestedDict = new Dictionary<string, object>();
                foreach (var property in jsonElement.EnumerateObject())
                {
                    nestedDict[property.Name] = property.Value;
                }
                var nested = FlattenConfiguration(nestedDict, key);
                foreach (var nestedKvp in nested)
                {
                    result[nestedKvp.Key] = nestedKvp.Value;
                }
                break;
            
            case JsonValueKind.Array:
                var index = 0;
                foreach (var arrayElement in jsonElement.EnumerateArray())
                {
                    HandleJsonElement(result, $"{key}:{index}", arrayElement);
                    index++;
                }
                break;
            
            default:
                result[key] = jsonElement.ToString();
                break;
        }
    }

    /// <summary>
    /// Gets dynamic property value from FlexConfiguration for testing.
    /// </summary>
    /// <param name="flexConfig">FlexConfiguration instance</param>
    /// <param name="propertyPath">Dot-separated property path</param>
    /// <returns>Property value or null if not found</returns>
    public static object? GetDynamicProperty(IFlexConfig flexConfig, string propertyPath)
    {
        dynamic config = flexConfig;
        var parts = propertyPath.Split('.');
        
        object? current = config;
        foreach (var part in parts)
        {
            if (current == null) return null;
            
            try
            {
                current = ((dynamic)current)[part];
            }
            catch
            {
                return null;
            }
        }
        
        return current;
    }

    /// <summary>
    /// Disposes resources including LocalStack container.
    /// </summary>
    [UsedImplicitly]
    protected void Dispose(bool disposing)
    {
        if (disposing)
        {
            _localStackHelper?.Dispose();
        }
    }
}