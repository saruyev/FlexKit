// <copyright file="AwsParameterStoreConfigurationProvider.cs" company="FlexKit">
// Copyright (c) FlexKit. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

using System.Text.Json;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using Microsoft.Extensions.Configuration;

namespace FlexKit.Configuration.Providers.Aws.Sources;

/// <summary>
/// Configuration provider that reads AWS Systems Manager Parameter Store parameters and makes them available
/// through the .NET configuration system. Supports hierarchical parameter organization, automatic type conversion,
/// and JSON parameter processing for complex configuration structures.
/// </summary>
/// <remarks>
/// This provider integrates AWS Parameter Store with the .NET configuration infrastructure, enabling
/// applications to store configuration data in AWS while maintaining compatibility with existing
/// configuration patterns and strongly typed configuration binding.
///
/// <para>
/// <strong>Supported Parameter Types:</strong>
/// <list type="bullet">
/// <item><strong>String</strong> - Simple values, JSON objects, or serialized data</item>
/// <item><strong>StringList</strong> - Comma-separated values converted to configuration arrays</item>
/// <item><strong>SecureString</strong> - Encrypted sensitive data (automatically decrypted)</item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Key Transformation:</strong>
/// Parameter names are transformed from AWS path format to .NET configuration keys:
/// <list type="table">
/// <listheader>
/// <term>AWS Parameter Name</term>
/// <description>.NET Configuration Key</description>
/// </listheader>
/// <item>
/// <term>/myapp/database/host</term>
/// <description>myapp:database:host</description>
/// </item>
/// <item>
/// <term>/myapp/features/caching</term>
/// <description>myapp:features:caching</description>
/// </item>
/// </list>
/// </para>
///
/// <para>
/// <strong>JSON Parameter Processing:</strong>
/// When JsonProcessor is enabled, String and SecureString parameters containing valid JSON
/// are automatically flattened into the configuration hierarchy:
/// <code>
/// // Parameter: /myapp/database/config
/// // Value: {"host": "localhost", "port": 5432, "ssl": true}
/// // Results in:
/// // myapp:database:config:host = "localhost"
/// // myapp:database:config:port = "5432"
/// // myapp:database:config:ssl = "true"
/// </code>
/// </para>
///
/// <para>
/// <strong>Error Handling:</strong>
/// The provider handles AWS API errors gracefully and supports optional parameter loading.
/// When a parameter path is marked as optional, missing parameters do not cause the
/// configuration loading to fail.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Basic parameter store configuration
/// var config = new FlexConfigurationBuilder()
///     .AddAwsParameterStore(options =>
///     {
///         options.Path = "/myapp/";
///         options.Optional = true;
///         options.ReloadAfter = TimeSpan.FromMinutes(5);
///     })
///     .Build();
///
/// // Access configuration values
/// var dbHost = config["myapp:database:host"];
/// var features = config["myapp:features:caching"];
///
/// // With strongly typed binding
/// containerBuilder.RegisterConfig&lt;DatabaseConfig&gt;("myapp:database");
/// </code>
/// </example>
public sealed class AwsParameterStoreConfigurationProvider : ConfigurationProvider, IDisposable
{
    private readonly AwsParameterStoreConfigurationSource _source;
    private readonly IAmazonSimpleSystemsManagement _ssmClient;
    private readonly Timer? _reloadTimer;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="AwsParameterStoreConfigurationProvider"/> class.
    /// Creates the AWS SSM client and configures automatic reloading if specified in the source options.
    /// </summary>
    /// <param name="source">
    /// The configuration source that contains the Parameter Store access configuration,
    /// including the path prefix, AWS options, and processing settings.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when AWS credentials cannot be resolved or SSM client creation fails.</exception>
    /// <remarks>
    /// The constructor creates an AWS SimpleSystemsManagement client using the credentials and region
    /// specified in the source options, or falls back to the default AWS credential resolution chain.
    /// If automatic reloading is configured, a timer is set up to periodically refresh the parameters.
    /// </remarks>
    public AwsParameterStoreConfigurationProvider(AwsParameterStoreConfigurationSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        _source = source;

        try
        {
            // Create AWS SSM client using the credential resolution chain
            var awsOptions = _source.AwsOptions ?? new Amazon.Extensions.NETCore.Setup.AWSOptions();
            _ssmClient = awsOptions.CreateServiceClient<IAmazonSimpleSystemsManagement>();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Failed to create AWS Systems Manager client. Ensure AWS credentials are properly configured.", ex);
        }

        // Set up automatic reloading if configured
        if (_source.ReloadAfter.HasValue)
        {
            _reloadTimer = new Timer(
                callback: _ => LoadAsync().ConfigureAwait(false),
                state: null,
                dueTime: _source.ReloadAfter.Value,
                period: _source.ReloadAfter.Value);
        }
    }

    /// <summary>
    /// Loads configuration data from AWS Parameter Store.
    /// Retrieves all parameters under the specified path prefix and processes them according
    /// to their type and the provider's configuration options.
    /// </summary>
    /// <remarks>
    /// This method is called by the .NET configuration system during application startup
    /// and can be called again during automatic reloading. It handles all three parameter
    /// types (String, StringList, SecureString) and applies appropriate processing based
    /// on the provider configuration.
    ///
    /// <para>
    /// <strong>Loading Process:</strong>
    /// <list type="number">
    /// <item>Clear existing configuration data</item>
    /// <item>Retrieve parameters from AWS Parameter Store using GetParametersByPath</item>
    /// <item>Process each parameter according to its type</item>
    /// <item>Apply JSON flattening if enabled and applicable</item>
    /// <item>Store processed values in the configuration data dictionary</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Error Handling:</strong>
    /// AWS API errors are handled according to the Optional setting. If the source is marked
    /// as optional, errors are logged but do not prevent the configuration from loading.
    /// Required sources will throw exceptions on failure.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when Parameter Store access fails and the source is not optional.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when the configured credentials lack necessary permissions.</exception>
    public override void Load()
    {
        try
        {
            LoadAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex) when (_source.Optional)
        {
            // For optional sources, log the error but don't fail the configuration loading
            _source.OnLoadException?.Invoke(new ConfigurationProviderException(_source, ex));
            Data.Clear();
        }
        catch (Exception ex)
        {
            // For required sources, wrap and re-throw the exception
            throw new InvalidOperationException(
                $"Failed to load configuration from AWS Parameter Store path '{_source.Path}'. " +
                $"Ensure the path exists and you have the necessary permissions.", ex);
        }
    }

    /// <summary>
    /// Asynchronously loads configuration data from AWS Parameter Store.
    /// This is the core implementation that handles the actual AWS API calls and parameter processing.
    /// </summary>
    /// <returns>A task that represents the asynchronous load operation.</returns>
    private async Task LoadAsync()
    {
        var configurationData = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        try
        {
            await LoadParametersAsync(configurationData).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (!_source.Optional)
            {
                throw;
            }

            _source.OnLoadException?.Invoke(new ConfigurationProviderException(_source, ex));
            return;
        }

        // Atomically replace the configuration data
        Data.Clear();
        foreach (var kvp in configurationData)
        {
            Data[kvp.Key] = kvp.Value;
        }
    }

    /// <summary>
    /// Loads parameters from AWS Parameter Store and processes them into configuration data.
    /// Handles pagination and processes all parameter types according to the provider configuration.
    /// </summary>
    /// <param name="configurationData">The dictionary to store the processed configuration data.</param>
    /// <returns>A task that represents the asynchronous parameter loading operation.</returns>
    private async Task LoadParametersAsync(Dictionary<string, string?> configurationData)
    {
        var request = new GetParametersByPathRequest
        {
            Path = _source.Path,
            Recursive = true,
            WithDecryption = true, // Always decrypt SecureString parameters
            MaxResults = 10 // Use pagination for large parameter sets
        };

        var allParameters = new List<Parameter>();

        do
        {
            var response = await _ssmClient.GetParametersByPathAsync(request).ConfigureAwait(false);
            allParameters.AddRange(response.Parameters);
            request.NextToken = response.NextToken;
        }
        while (!string.IsNullOrEmpty(request.NextToken));

        // Process each parameter according to its type
        foreach (var parameter in allParameters)
        {
            var configKey = TransformParameterNameToConfigKey(parameter.Name);
            ProcessParameterValue(parameter, configurationData, configKey);
        }
    }

    /// <summary>
    /// Transforms an AWS Parameter Store parameter name to a .NET configuration key format.
    /// Converts forward slash path separators to colon separators and removes the configured path prefix.
    /// </summary>
    /// <param name="parameterName">The full AWS parameter name (e.g., "/myapp/database/host").</param>
    /// <returns>The transformed configuration key (e.g., "myapp:database:host").</returns>
    /// <remarks>
    /// The transformation process:
    /// <list type="number">
    /// <item>Removes the configured path prefix if present</item>
    /// <item>Removes leading forward slashes</item>
    /// <item>Replaces remaining forward slashes with colons</item>
    /// <item>Applies custom parameter processor if configured</item>
    /// </list>
    /// </remarks>
    private string TransformParameterNameToConfigKey(string parameterName)
    {
        var configKey = parameterName;

        // Remove the path prefix if it matches
        if (!string.IsNullOrEmpty(_source.Path) && configKey.StartsWith(_source.Path, StringComparison.OrdinalIgnoreCase))
        {
            configKey = configKey[_source.Path.Length..];
        }

        // Remove leading slashes and convert to colon notation
        configKey = configKey.TrimStart('/').Replace('/', ':');

        // Apply custom parameter processor if configured
        if (_source.ParameterProcessor != null)
        {
            configKey = _source.ParameterProcessor.ProcessParameterName(configKey, parameterName);
        }

        return configKey;
    }

    /// <summary>
    /// Processes a parameter value according to its type and stores it in the configuration data.
    /// Handles String, StringList, and SecureString parameter types with appropriate processing logic.
    /// </summary>
    /// <param name="parameter">The AWS parameter to process.</param>
    /// <param name="configurationData">The dictionary to store the processed configuration data.</param>
    /// <param name="configKey">The transformed configuration key for this parameter.</param>
    private void ProcessParameterValue(Parameter parameter, Dictionary<string, string?> configurationData, string configKey)
    {
        switch (parameter.Type.Value)
        {
            case "String":
                ProcessStringParameter(parameter, configurationData, configKey);
                break;

            case "StringList":
                ProcessStringListParameter(parameter, configurationData, configKey);
                break;

            case "SecureString":
                ProcessSecureStringParameter(parameter, configurationData, configKey);
                break;

            default:
                // Unknown parameter type - treat as string
                configurationData[configKey] = parameter.Value;
                break;
        }
    }

    /// <summary>
    /// Processes a String parameter value, applying JSON flattening if enabled and applicable.
    /// String parameters can contain simple values, JSON objects, or other serialized data.
    /// </summary>
    /// <param name="parameter">The String parameter to process.</param>
    /// <param name="configurationData">The dictionary to store the processed configuration data.</param>
    /// <param name="configKey">The configuration key for this parameter.</param>
    private void ProcessStringParameter(Parameter parameter, Dictionary<string, string?> configurationData, string configKey)
    {
        var value = parameter.Value;

        // Check if JSON processing is enabled and this value contains JSON
        if (_source.JsonProcessor && ShouldProcessAsJson(configKey) && IsValidJson(value))
        {
            FlattenJsonValue(value, configurationData, configKey);
        }
        else
        {
            // Store as a simple key-value pair
            configurationData[configKey] = value;
        }
    }

    /// <summary>
    /// Processes a StringList parameter value by converting it to indexed configuration entries.
    /// StringList parameters contain comma-separated values that are converted to array format.
    /// </summary>
    /// <param name="parameter">The StringList parameter to process.</param>
    /// <param name="configurationData">The dictionary to store the processed configuration data.</param>
    /// <param name="configKey">The configuration key for this parameter.</param>
    private static void ProcessStringListParameter(Parameter parameter, Dictionary<string, string?> configurationData, string configKey)
    {
        if (string.IsNullOrEmpty(parameter.Value))
        {
            return;
        }

        var values = parameter.Value.Split(',', StringSplitOptions.RemoveEmptyEntries);

        // Convert to array format that .NET Configuration understands
        for (var i = 0; i < values.Length; i++)
        {
            configurationData[$"{configKey}:{i}"] = values[i].Trim();
        }
    }

    /// <summary>
    /// Processes a SecureString parameter value. SecureString parameters are automatically
    /// decrypted by the AWS SDK and processed the same way as String parameters.
    /// </summary>
    /// <param name="parameter">The SecureString parameter to process.</param>
    /// <param name="configurationData">The dictionary to store the processed configuration data.</param>
    /// <param name="configKey">The configuration key for this parameter.</param>
    private void ProcessSecureStringParameter(Parameter parameter, Dictionary<string, string?> configurationData, string configKey)
    {
        // SecureString parameters are automatically decrypted when WithDecryption=true
        // Process them the same way as String parameters
        ProcessStringParameter(parameter, configurationData, configKey);
    }

    /// <summary>
    /// Determines whether a configuration key should be processed as JSON based on the provider configuration.
    /// Checks against JsonProcessorPaths if specified, otherwise applies to all parameters when JsonProcessor is enabled.
    /// </summary>
    /// <param name="configKey">The configuration key to check.</param>
    /// <returns>True if the parameter should be processed as JSON, false otherwise.</returns>
    private bool ShouldProcessAsJson(string configKey)
    {
        if (_source.JsonProcessorPaths == null || _source.JsonProcessorPaths.Length == 0)
        {
            return true; // Process all parameters as JSON when JsonProcessor is enabled
        }

        // Check if the key matches any of the specified paths
        return _source.JsonProcessorPaths.Any(path =>
            configKey.StartsWith(path.TrimStart('/').Replace('/', ':'), StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Checks if a string value contains valid JSON that can be parsed and flattened.
    /// Uses JSON parsing to validate the structure without throwing exceptions.
    /// </summary>
    /// <param name="value">The string value to check.</param>
    /// <returns>True if the value contains valid JSON, false otherwise.</returns>
    private static bool IsValidJson(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        value = value.Trim();
        if ((!value.StartsWith('{') || !value.EndsWith('}')) &&
            (!value.StartsWith('[') || !value.EndsWith(']')))
        {
            return false;
        }

        try
        {
            JsonDocument.Parse(value);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>
    /// Flattens a JSON string into hierarchical configuration keys following .NET configuration conventions.
    /// Converts JSON objects and arrays into a flat key-value structure that can be used with strongly typed binding.
    /// </summary>
    /// <param name="jsonValue">The JSON string to flatten.</param>
    /// <param name="configurationData">The dictionary to store the flattened configuration data.</param>
    /// <param name="prefix">The key prefix to prepend to all flattened keys.</param>
    private static void FlattenJsonValue(string jsonValue, Dictionary<string, string?> configurationData, string prefix)
    {
        try
        {
            using var document = JsonDocument.Parse(jsonValue);
            FlattenJsonElement(document.RootElement, configurationData, prefix);
        }
        catch (JsonException)
        {
            // If JSON parsing fails, store as a simple string value
            configurationData[prefix] = jsonValue;
        }
    }

    /// <summary>
    /// Recursively flattens a JSON element into configuration key-value pairs.
    /// Handles objects, arrays, and primitive values according to .NET configuration conventions.
    /// </summary>
    /// <param name="element">The JSON element to flatten.</param>
    /// <param name="configurationData">The dictionary to store the flattened configuration data.</param>
    /// <param name="prefix">The current key prefix for nested elements.</param>
    private static void FlattenJsonElement(JsonElement element, Dictionary<string, string?> configurationData, string prefix)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var key = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}:{property.Name}";
                    FlattenJsonElement(property.Value, configurationData, key);
                }
                break;

            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    var key = $"{prefix}:{index}";
                    FlattenJsonElement(item, configurationData, key);
                    index++;
                }
                break;

            case JsonValueKind.String:
                configurationData[prefix] = element.GetString();
                break;

            case JsonValueKind.Number:
                configurationData[prefix] = element.GetRawText();
                break;

            case JsonValueKind.True:
                configurationData[prefix] = "true";
                break;

            case JsonValueKind.False:
                configurationData[prefix] = "false";
                break;

            case JsonValueKind.Null:
                configurationData[prefix] = null;
                break;
            case JsonValueKind.Undefined:
                break;
            default:
                configurationData[prefix] = element.GetRawText();
                break;
        }
    }

    /// <summary>
    /// Releases the unmanaged resources used by the provider and optionally releases the managed resources.
    /// Disposes of the AWS SSM client and stops the reload timer if configured.
    /// </summary>
    /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _reloadTimer?.Dispose();
                _ssmClient.Dispose();
            }

            _disposed = true;
        }
    }

    /// <summary>
    /// Releases all resources used by the current instance of the <see cref="AwsParameterStoreConfigurationProvider"/> class.
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
    }
}

/// <summary>
/// Represents an exception that occurs during configuration provider loading.
/// Used to provide context about configuration loading failures for error handling and logging.
/// </summary>
public class ConfigurationProviderException : Exception
{
    private readonly string _source;

    /// <summary>
    /// Gets the source of the exception (the Parameter Store path).
    /// </summary>
    public override string Source => _source;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationProviderException"/> class.
    /// </summary>
    /// <param name="source">The configuration source that caused the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ConfigurationProviderException(AwsParameterStoreConfigurationSource source, Exception innerException)
        : base($"Failed to load configuration from AWS Parameter Store source: {source.Path}", innerException)
    {
        _source = source.Path; // Store in a private field, not virtual property
    }
}
