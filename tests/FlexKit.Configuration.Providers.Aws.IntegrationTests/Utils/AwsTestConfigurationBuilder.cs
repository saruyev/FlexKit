using FlexKit.Configuration.Providers.Aws.Sources;
using FlexKit.IntegrationTests.Utils;
using Reqnroll;
using FlexKit.Configuration.Core;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
using FlexKit.Configuration.Providers.Aws.Extensions;

namespace FlexKit.Configuration.Providers.Aws.IntegrationTests.Utils;

/// <summary>
/// Test configuration builder specifically for AWS Parameter Store and Secrets Manager configuration testing.
/// Inherits from BaseTestConfigurationBuilder to provide AWS-specific configuration methods.
/// </summary>
public class AwsTestConfigurationBuilder(ScenarioContext? scenarioContext = null) : BaseTestConfigurationBuilder<AwsTestConfigurationBuilder>(scenarioContext)
{
    public AwsTestConfigurationBuilder() : this(null) { }

    /// <summary>
    /// Adds AWS Parameter Store as a configuration source.
    /// </summary>
    /// <param name="path">Parameter Store path prefix</param>
    /// <param name="optional">Whether the source is optional</param>
    /// <param name="jsonProcessor">Whether to enable JSON processing</param>
    /// <returns>This builder for method chaining</returns>
    public AwsTestConfigurationBuilder AddAwsParameterStore(string path, bool optional = true, bool jsonProcessor = false)
    {
        var parameterStoreSource = new AwsParameterStoreConfigurationSource
        {
            Path = path,
            Optional = optional,
            JsonProcessor = jsonProcessor,
            AwsOptions = CreateTestAwsOptions()
        };
        
        return AddSource(parameterStoreSource);
    }

    /// <summary>
    /// Adds AWS Parameter Store with advanced options.
    /// </summary>
    /// <param name="configureOptions">Action to configure Parameter Store options</param>
    /// <returns>This builder for method chaining</returns>
    public AwsTestConfigurationBuilder AddAwsParameterStore(Action<AwsParameterStoreConfigurationSource> configureOptions)
    {
        var parameterStoreSource = new AwsParameterStoreConfigurationSource
        {
            AwsOptions = CreateTestAwsOptions()
        };
        
        configureOptions(parameterStoreSource);
        
        return AddSource(parameterStoreSource);
    }

    /// <summary>
    /// Creates a temporary JSON file with parameter store test data and configures parameter store from it.
    /// </summary>
    /// <param name="testDataPath">Path to JSON test data file</param>
    /// <param name="optional">Whether the parameter store source is optional</param>
    /// <param name="jsonProcessor">Whether to enable JSON processing</param>
    /// <returns>This builder for method chaining</returns>
    public AwsTestConfigurationBuilder AddParameterStoreFromTestData(string testDataPath, bool optional = true, bool jsonProcessor = false)
    {
        var normalizedPath = testDataPath.Replace('/', Path.DirectorySeparatorChar);
        
        if (!File.Exists(normalizedPath))
        {
            throw new FileNotFoundException($"Test data file not found: {normalizedPath}");
        }

        var jsonContent = File.ReadAllText(normalizedPath);
        
        // First parse as JsonDocument to properly handle the structure
        using var document = JsonDocument.Parse(jsonContent);
        var root = document.RootElement;
        
        // Simulate Parameter Store by converting test data to in-memory configuration
        var configData = new Dictionary<string, string?>();
        
        // Look for infrastructure_module section
        if (root.TryGetProperty("infrastructure_module", out var infraModule))
        {
            // Handle test_parameters structure (aws-config.json)
            if (infraModule.TryGetProperty("test_parameters", out var parametersElement))
            {
                foreach (var paramElement in parametersElement.EnumerateArray())
                {
                    var name = paramElement.GetProperty("name").GetString() ?? "";
                    var value = paramElement.GetProperty("value").GetString();
                    var type = paramElement.TryGetProperty("type", out var typeEl) ? 
                        typeEl.GetString() ?? "String" : "String";
                    
                    var configKey = ConvertParameterNameToConfigKey(name);
                    
                    switch (type.ToLowerInvariant())
                    {
                        case "string":
                        case "securestring":
                            if (jsonProcessor && IsValidJson(value))
                            {
                                // Flatten JSON into configuration keys
                                FlattenJsonToConfiguration(value!, configKey, configData);
                            }
                            else
                            {
                                configData[configKey] = value;
                            }
                            break;
                            
                        case "stringlist":
                            var values = value?.Split(',') ?? Array.Empty<string>();
                            for (int i = 0; i < values.Length; i++)
                            {
                                configData[$"{configKey}:{i}"] = values[i].Trim();
                            }
                            break;
                    }
                }
            }
            
            // Handle complex_test_data structure (complex-parameters.json)
            if (infraModule.TryGetProperty("complex_test_data", out var complexDataElement))
            {
                if (complexDataElement.TryGetProperty("json_parameter", out var jsonParamElement))
                {
                    var name = jsonParamElement.GetProperty("name").GetString() ?? "";
                    var valueElement = jsonParamElement.GetProperty("value");
                    
                    var configKey = ConvertParameterNameToConfigKey(name);
                    
                    if (jsonProcessor)
                    {
                        // Flatten the JSON value into configuration keys
                        FlattenJsonElement(valueElement, configKey, configData);
                    }
                    else
                    {
                        // Store as JSON string
                        configData[configKey] = valueElement.GetRawText();
                    }
                }
                
                if (complexDataElement.TryGetProperty("nested_array_parameter", out var arrayParamElement))
                {
                    var name = arrayParamElement.GetProperty("name").GetString() ?? "";
                    var valueElement = arrayParamElement.GetProperty("value");
                    
                    var configKey = ConvertParameterNameToConfigKey(name);
                    
                    if (jsonProcessor)
                    {
                        // Flatten the JSON value into configuration keys
                        FlattenJsonElement(valueElement, configKey, configData);
                    }
                    else
                    {
                        // Store as JSON string
                        configData[configKey] = valueElement.GetRawText();
                    }
                }
            }
        }

        AddInMemoryCollection(configData);
        return this;
    }

    /// <summary>
    /// Builds a FlexConfiguration instance.
    /// </summary>
    /// <returns>The built FlexConfiguration</returns>
    public IFlexConfig BuildFlexConfig()
    {
        var configuration = Build();
        return new FlexConfiguration(configuration);
    }

    /// <summary>
    /// Builds a FlexConfiguration using FlexConfigurationBuilder.
    /// </summary>
    /// <param name="configureBuilder">Action to configure the FlexConfigurationBuilder</param>
    /// <returns>The built FlexConfiguration</returns>
    public IFlexConfig BuildFlexConfig(Action<FlexConfigurationBuilder> configureBuilder)
    {
        ApplyEnvironmentVariables();
        
        var flexBuilder = new FlexConfigurationBuilder();

        // Add in-memory data first
        if (InMemoryData.Count > 0)
        {
            AddInMemoryCollection(InMemoryData);
        }

        // Add other sources
        foreach (var source in Sources)
        {
            flexBuilder.AddSource(source);
        }

        // Apply additional configuration
        configureBuilder(flexBuilder);

        return flexBuilder.Build();
    }

    /// <summary>
    /// Creates AWS options for testing (typically LocalStack or mocked credentials).
    /// </summary>
    /// <returns>AWS options configured for testing</returns>
    private static AWSOptions CreateTestAwsOptions()
    {
        return new AWSOptions
        {
            Credentials = new AnonymousAWSCredentials(),
            Region = Amazon.RegionEndpoint.USEast1
        };
    }

    /// <summary>
    /// Converts AWS Parameter Store parameter name to .NET configuration key format.
    /// </summary>
    /// <param name="parameterName">AWS parameter name (e.g., "/myapp/database/host")</param>
    /// <returns>Configuration key (e.g., "myapp:database:host")</returns>
    private static string ConvertParameterNameToConfigKey(string parameterName)
    {
        return parameterName.TrimStart('/').Replace('/', ':');
    }

    /// <summary>
    /// Checks if a string is valid JSON.
    /// </summary>
    /// <param name="value">String to check</param>
    /// <returns>True if valid JSON, false otherwise</returns>
    private static bool IsValidJson(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        try
        {
            JsonDocument.Parse(value);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Flattens JSON content into configuration key-value pairs.
    /// </summary>
    /// <param name="jsonContent">JSON content to flatten</param>
    /// <param name="keyPrefix">Prefix for configuration keys</param>
    /// <param name="configData">Dictionary to store flattened data</param>
    private static void FlattenJsonToConfiguration(string jsonContent, string keyPrefix, Dictionary<string, string?> configData)
    {
        try
        {
            using var document = JsonDocument.Parse(jsonContent);
            FlattenJsonElement(document.RootElement, keyPrefix, configData);
        }
        catch (JsonException)
        {
            // If JSON parsing fails, treat as simple string value
            configData[keyPrefix] = jsonContent;
        }
    }

    /// <summary>
    /// Recursively flattens a JSON element into configuration key-value pairs.
    /// </summary>
    /// <param name="element">JSON element to flatten</param>
    /// <param name="keyPrefix">Current key prefix</param>
    /// <param name="configData">Dictionary to store flattened data</param>
    private static void FlattenJsonElement(JsonElement element, string keyPrefix, Dictionary<string, string?> configData)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var newKey = string.IsNullOrEmpty(keyPrefix) ? property.Name : $"{keyPrefix}:{property.Name}";
                    FlattenJsonElement(property.Value, newKey, configData);
                }
                break;
                
            case JsonValueKind.Array:
                int index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    var newKey = $"{keyPrefix}:{index}";
                    FlattenJsonElement(item, newKey, configData);
                    index++;
                }
                break;
                
            case JsonValueKind.String:
                configData[keyPrefix] = element.GetString();
                break;
                
            case JsonValueKind.Number:
                configData[keyPrefix] = element.GetRawText();
                break;
                
            case JsonValueKind.True:
                configData[keyPrefix] = "true";
                break;
                
            case JsonValueKind.False:
                configData[keyPrefix] = "false";
                break;
                
            case JsonValueKind.Null:
                configData[keyPrefix] = null;
                break;
        }
    }

    /// <summary>
    /// Gets dynamic property value from a dynamic object using dot notation.
    /// </summary>
    /// <param name="dynamicObject">Dynamic object to get property from</param>
    /// <param name="propertyPath">Property path using dot notation (e.g., "myapp.database.host")</param>
    /// <returns>Property value or null if not found</returns>
    public static object? GetDynamicProperty(dynamic? dynamicObject, string propertyPath)
    {
        if (dynamicObject == null || string.IsNullOrEmpty(propertyPath))
            return null;

        // Convert dot notation to colon notation for configuration keys
        var configKey = propertyPath.Replace('.', ':');
    
        try
        {
            // Use FlexConfiguration's indexer access instead of nested property traversal
            return dynamicObject![configKey];
        }
        catch
        {
            return null;
        }
    }
}