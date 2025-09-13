using FlexKit.Configuration.Providers.Aws.Sources;
using FlexKit.IntegrationTests.Utils;
using Reqnroll;
using FlexKit.Configuration.Core;
using System.Text.Json;
using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
// ReSharper disable NullableWarningSuppressionIsUsed

// ReSharper disable MethodTooLong
// ReSharper disable ComplexConditionExpression
// ReSharper disable FlagArgument
// ReSharper disable TooManyArguments

namespace FlexKit.Configuration.Providers.Aws.IntegrationTests.Utils;

/// <summary>
/// Test configuration builder specifically for AWS Parameter Store and Secrets Manager configuration testing.
/// Inherits from BaseTestConfigurationBuilder to provide AWS-specific configuration methods.
/// </summary>
public class AwsTestConfigurationBuilder(ScenarioContext? scenarioContext = null)
    : BaseTestConfigurationBuilder<AwsTestConfigurationBuilder>(scenarioContext)
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
    public AwsTestConfigurationBuilder AddParameterStoreFromTestData(
        string testDataPath,
        bool optional = true,
        bool jsonProcessor = false)
    {
        var normalizedPath = testDataPath.Replace('/', Path.DirectorySeparatorChar);

        if (!File.Exists(normalizedPath))
        {
            throw new FileNotFoundException($"Test data file not found: {normalizedPath}");
        }

        var jsonContent = File.ReadAllText(normalizedPath);

        // First, parse as JsonDocument to properly handle the structure
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
                    var type = paramElement.TryGetProperty("type", out var typeEl) ? typeEl.GetString() ?? "String" : "String";

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
                            var values = value?.Split(',') ?? [];
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

    /// <summary>
    /// Adds AWS Secrets Manager as a configuration source.
    /// </summary>
    /// <param name="secretNames">Array of secret names to load</param>
    /// <param name="optional">Whether the source is optional</param>
    /// <param name="jsonProcessor">Whether to enable JSON processing</param>
    /// <returns>This builder for method chaining</returns>
    public AwsTestConfigurationBuilder AddAwsSecretsManager(string[] secretNames, bool optional = true, bool jsonProcessor = false)
    {
        var secretsManagerSource = new AwsSecretsManagerConfigurationSource
        {
            SecretNames = secretNames,
            Optional = optional,
            JsonProcessor = jsonProcessor,
            AwsOptions = CreateTestAwsOptions()
        };

        return AddSource(secretsManagerSource);
    }

    /// <summary>
    /// Adds AWS Secrets Manager with advanced options.
    /// </summary>
    /// <param name="configureOptions">Action to configure Secrets Manager options</param>
    /// <returns>This builder for method chaining</returns>
    public AwsTestConfigurationBuilder AddAwsSecretsManager(Action<AwsSecretsManagerConfigurationSource> configureOptions)
    {
        var secretsManagerSource = new AwsSecretsManagerConfigurationSource
        {
            AwsOptions = CreateTestAwsOptions()
        };

        configureOptions(secretsManagerSource);

        return AddSource(secretsManagerSource);
    }

    /// <summary>
    /// Creates a temporary JSON file with secrets manager test data and configures secrets manager from it.
    /// </summary>
    /// <param name="testDataPath">Path to JSON test data file</param>
    /// <param name="optional">Whether the secrets manager source is optional</param>
    /// <param name="jsonProcessor">Whether to enable JSON processing</param>
    /// <returns>This builder for method chaining</returns>
    public AwsTestConfigurationBuilder AddSecretsManagerFromTestData(
        string testDataPath,
        bool optional = true,
        bool jsonProcessor = false)
    {
        var normalizedPath = testDataPath.Replace('/', Path.DirectorySeparatorChar);

        if (!File.Exists(normalizedPath))
        {
            throw new FileNotFoundException($"Test data file not found: {normalizedPath}");
        }

        var jsonContent = File.ReadAllText(normalizedPath);

        // First, parse as JsonDocument to properly handle the structure
        using var document = JsonDocument.Parse(jsonContent);
        var root = document.RootElement;

        // Simulate Secrets Manager by converting test data to in-memory configuration
        var configData = new Dictionary<string, string?>();

        // Look for infrastructure_module section
        if (root.TryGetProperty("infrastructure_module", out var infraModule))
        {
            // Handle test_secrets structure (aws-config.json)
            if (infraModule.TryGetProperty("test_secrets", out var secretsElement))
            {
                foreach (var secretElement in secretsElement.EnumerateArray())
                {
                    var name = secretElement.GetProperty("name").GetString() ?? "";
                    var configKey = ConvertParameterNameToConfigKey(name);

                    // Handle string secrets
                    if (secretElement.TryGetProperty("value", out var valueElement))
                    {
                        var value = valueElement.GetString();

                        if (jsonProcessor && IsValidJson(value))
                        {
                            // Flatten JSON into configuration keys
                            FlattenJsonToConfiguration(value!, configKey, configData);
                        }
                        else
                        {
                            configData[configKey] = value;
                        }
                    }
                    // Handle binary secrets (base64 encoded)
                    else if (secretElement.TryGetProperty("binary_value", out var binaryElement))
                    {
                        var binaryValue = binaryElement.GetString();
                        configData[configKey] = binaryValue;
                    }
                }
            }

            // Handle complex_test_data structure (complex-parameters.json) 
            // Treat parameters as secrets for testing JSON processing patterns
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

    // Extension methods to add to AwsTestConfigurationBuilder.cs

    /// <summary>
    /// Creates a temporary JSON file with secrets manager test data and configures secrets manager from it with version stage support.
    /// This method extends the existing AddSecretsManagerFromTestData functionality to support version stages.
    /// </summary>
    /// <param name="testDataPath">Path to JSON test data file</param>
    /// <param name="versionStage">Version stage to retrieve (e.g., "AWSCURRENT", "AWSPENDING", "AWSPREVIOUS")</param>
    /// <param name="optional">Whether the secrets manager source is optional</param>
    /// <param name="jsonProcessor">Whether to enable JSON processing</param>
    /// <returns>This builder for method chaining</returns>
    public AwsTestConfigurationBuilder AddSecretsManagerFromTestDataWithVersionStage(
        string testDataPath,
        string versionStage,
        bool optional = true,
        bool jsonProcessor = false)
    {
        if (versionStage == "MISSING_STAGE" && !optional)
        {
            throw new InvalidOperationException($"Required version stage '{versionStage}' not found for secrets in AWS Secrets Manager.");
        }

        if (versionStage == "MISSING_STAGE" && optional)
        {
            AddInMemoryCollection(new Dictionary<string, string?>());
            return this;
        }

        var normalizedPath = testDataPath.Replace('/', Path.DirectorySeparatorChar);

        if (!File.Exists(normalizedPath))
        {
            throw new FileNotFoundException($"Test data file not found: {normalizedPath}");
        }

        var jsonContent = File.ReadAllText(normalizedPath);

        // First, parse as JsonDocument to properly handle the structure
        using var document = JsonDocument.Parse(jsonContent);
        var root = document.RootElement;

        // Simulate Secrets Manager by converting test data to in-memory configuration
        var configData = new Dictionary<string, string?>();

        // Look for infrastructure_module section
        if (root.TryGetProperty("infrastructure_module", out var infraModule))
        {
            // Handle test_secrets structure (aws-config.json)
            if (infraModule.TryGetProperty("test_secrets", out var secretsElement))
            {
                foreach (var secretElement in secretsElement.EnumerateArray())
                {
                    var name = secretElement.GetProperty("name").GetString() ?? "";
                    var configKey = ConvertParameterNameToConfigKey(name);

                    // Simulate different values based on the version stage
                    string? value = GetVersionedSecretValue(secretElement, versionStage);

                    if (value != null)
                    {
                        if (jsonProcessor && IsValidJson(value))
                        {
                            // Flatten JSON into configuration keys
                            FlattenJsonToConfiguration(value, configKey, configData);
                        }
                        else
                        {
                            configData[configKey] = value;
                        }
                    }
                    // Handle binary secrets (base64 encoded)
                    else if (secretElement.TryGetProperty("binary_value", out var binaryElement))
                    {
                        var binaryValue = binaryElement.GetString();
                        configData[configKey] = binaryValue;
                    }
                }
            }

            // Handle complex_test_data structure for version-aware scenarios
            if (infraModule.TryGetProperty("complex_test_data", out var complexDataElement))
            {
                if (complexDataElement.TryGetProperty("json_parameter", out var jsonParamElement))
                {
                    var name = jsonParamElement.GetProperty("name").GetString() ?? "";
                    var valueElement = jsonParamElement.GetProperty("value");

                    var configKey = ConvertParameterNameToConfigKey(name);

                    // Apply version-specific modifications if needed
                    var versionedValue = GetVersionedJsonValue(valueElement, versionStage);

                    if (jsonProcessor)
                    {
                        // Flatten the versioned JSON value into configuration keys
                        if (versionedValue != null)
                        {
                            FlattenJsonToConfiguration(versionedValue, configKey, configData);
                        }
                    }
                    else
                    {
                        // Store as JSON string
                        configData[configKey] = versionedValue ?? valueElement.GetRawText();
                    }
                }
            }
        }

        // Add the configuration data as an in-memory configuration
        AddInMemoryCollection(configData);

        return this;
    }

    /// <summary>
    /// Gets a versioned secret value based on the version stage.
    /// Simulates different secret values for different version stages in testing.
    /// </summary>
    /// <param name="secretElement">The secret element from test data</param>
    /// <param name="versionStage">The version stage to simulate</param>
    /// <returns>The versioned secret value or null if not found</returns>
    private static string? GetVersionedSecretValue(JsonElement secretElement, string versionStage)
    {
        // First, try to get the base value
        if (!secretElement.TryGetProperty("value", out var valueElement))
        {
            return null;
        }

        var baseValue = valueElement.GetString();
        if (baseValue == null)
        {
            return null;
        }

        // For testing purposes, modify the value based on the version stage
        return versionStage switch
        {
            "AWSCURRENT" => baseValue, // Use original value for current
            "AWSPENDING" => ModifySecretForVersionStage(baseValue, "pending"),
            "AWSPREVIOUS" => ModifySecretForVersionStage(baseValue, "previous"),
            "MISSING_STAGE" => null, // Simulate missing version
            _ when versionStage.StartsWith("CUSTOM") || versionStage == "STAGING" => ModifySecretForVersionStage(
                baseValue,
                "staging"),
            _ => baseValue // Default to base value for unknown stages
        };
    }

    /// <summary>
    /// Gets a versioned JSON value based on the version stage.
    /// </summary>
    /// <param name="valueElement">The JSON value element</param>
    /// <param name="versionStage">The version stage to simulate</param>
    /// <returns>The versioned JSON string</returns>
    private static string? GetVersionedJsonValue(JsonElement valueElement, string versionStage)
    {
        var jsonString = valueElement.GetRawText();

        return versionStage switch
        {
            "AWSCURRENT" => jsonString,
            "AWSPENDING" => ModifyJsonForVersionStage(jsonString, "pending"),
            "AWSPREVIOUS" => ModifyJsonForVersionStage(jsonString, "previous"),
            "MISSING_STAGE" => null,
            _ when versionStage.StartsWith("CUSTOM") || versionStage == "STAGING" => ModifyJsonForVersionStage(
                jsonString,
                "staging"),
            _ => jsonString
        };
    }

    /// <summary>
    /// Modifies a secret value for version stage simulation.
    /// </summary>
    /// <param name="originalValue">The original secret value</param>
    /// <param name="stageSuffix">The stage suffix to add</param>
    /// <returns>The modified secret value</returns>
    private static string ModifySecretForVersionStage(string originalValue, string stageSuffix)
    {
        // If it's JSON, parse and modify
        if (IsValidJson(originalValue))
        {
            try
            {
                using var document = JsonDocument.Parse(originalValue);
                var root = document.RootElement;

                var modifiedJson = new Dictionary<string, object?>();

                foreach (var property in root.EnumerateObject())
                {
                    object? value = property.Value.ValueKind switch
                    {
                        JsonValueKind.String => property.Name == "host"
                            ? $"{stageSuffix}-{property.Value.GetString()}"
                            : property.Name == "username"
                                ? $"{stageSuffix}user"
                                : property.Name == "password"
                                    ? $"{stageSuffix}pass123"
                                    : property.Value.GetString(),
                        JsonValueKind.Number => property.Value.GetInt32(),
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        _ => property.Value.GetString()
                    };

                    modifiedJson[property.Name] = value;
                }

                return JsonSerializer.Serialize(modifiedJson);
            }
            catch
            {
                // Fall back to simple modification if JSON parsing fails
            }
        }

        // Simple string modification for non-JSON values
        return $"{stageSuffix}-{originalValue}";
    }

    /// <summary>
    /// Modifies a JSON string for version stage simulation.
    /// </summary>
    /// <param name="jsonString">The original JSON string</param>
    /// <param name="stageSuffix">The stage suffix to add</param>
    /// <returns>The modified JSON string</returns>
    private static string ModifyJsonForVersionStage(string jsonString, string stageSuffix)
    {
        try
        {
            using var document = JsonDocument.Parse(jsonString);
            var root = document.RootElement;

            // Create a modified version of the JSON for testing
            var modifiedJson = new Dictionary<string, object?>();

            CopyJsonElementWithModifications(root, modifiedJson, stageSuffix);

            return JsonSerializer.Serialize(modifiedJson);
        }
        catch
        {
            return jsonString; // Return original if modification fails
        }
    }

    /// <summary>
    /// Recursively copies JSON elements while applying version-specific modifications.
    /// </summary>
    /// <param name="source">Source JSON element</param>
    /// <param name="target">Target dictionary</param>
    /// <param name="stageSuffix">Stage suffix for modifications</param>
    private static void CopyJsonElementWithModifications(JsonElement source, Dictionary<string, object?> target, string stageSuffix)
    {
        foreach (var property in source.EnumerateObject())
        {
            switch (property.Value.ValueKind)
            {
                case JsonValueKind.Object:
                    var nestedDict = new Dictionary<string, object?>();
                    CopyJsonElementWithModifications(property.Value, nestedDict, stageSuffix);
                    target[property.Name] = nestedDict;
                    break;

                case JsonValueKind.Array:
                    var arrayList = new List<object?>();
                    foreach (var arrayElement in property.Value.EnumerateArray())
                    {
                        if (arrayElement.ValueKind == JsonValueKind.Object)
                        {
                            var arrayDict = new Dictionary<string, object?>();
                            CopyJsonElementWithModifications(arrayElement, arrayDict, stageSuffix);
                            arrayList.Add(arrayDict);
                        }
                        else
                        {
                            arrayList.Add(GetJsonElementValue(arrayElement));
                        }
                    }

                    target[property.Name] = arrayList;
                    break;

                case JsonValueKind.String:
                    // Apply version-specific modifications to string values
                    var stringValue = property.Value.GetString();
                    if (property.Name.Contains("host", StringComparison.OrdinalIgnoreCase))
                    {
                        target[property.Name] = $"{stageSuffix}-{stringValue}";
                    }
                    else if (property.Name.Contains("name", StringComparison.OrdinalIgnoreCase) &&
                             property.Name.Contains("user", StringComparison.OrdinalIgnoreCase))
                    {
                        target[property.Name] = $"{stageSuffix}user";
                    }
                    else
                    {
                        target[property.Name] = stringValue;
                    }

                    break;

                default:
                    target[property.Name] = GetJsonElementValue(property.Value);
                    break;
            }
        }
    }

    /// <summary>
    /// Gets the .NET value from a JsonElement.
    /// </summary>
    /// <param name="element">The JSON element</param>
    /// <returns>The corresponding .NET value</returns>
    private static object? GetJsonElementValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt32(out var intVal) ? intVal : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.GetString()
        };
    }
}