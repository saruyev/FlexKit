// <copyright file="AwsSecretsManagerConfigurationProvider.cs" company="FlexKit">
// Copyright (c) FlexKit. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

using System.Diagnostics.CodeAnalysis;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using FlexKit.Configuration.Providers.Aws.Extensions;
using Microsoft.Extensions.Configuration;

namespace FlexKit.Configuration.Providers.Aws.Sources;

/// <summary>
/// Configuration provider that reads AWS Secrets Manager secrets and makes them available
/// through the .NET configuration system. Supports both string and binary secrets with
/// automatic JSON processing and Base64 encoding for binary data.
/// </summary>
/// <remarks>
/// This provider integrates AWS Secrets Manager with the .NET configuration infrastructure, enabling
/// applications to store sensitive configuration data in AWS while maintaining compatibility with existing
/// configuration patterns and strongly typed configuration binding.
///
/// <para>
/// <strong>Supported Secret Types:</strong>
/// <list type="bullet">
/// <item><strong>SecretString</strong> - JSON objects, simple strings, database credentials</item>
/// <item><strong>SecretBinary</strong> - Certificates, keystores, encrypted files (Base64 encoded)</item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Secret Name Resolution:</strong>
/// Secrets can be specified by name or ARN. The provider supports both individual secrets
/// and pattern-based loading using name prefixes:
/// <list type="table">
/// <listheader>
/// <term>Secret Name/Pattern</term>
/// <description>.NET Configuration Key</description>
/// </listheader>
/// <item>
/// <term>myapp-database</term>
/// <description>myapp-database (single secret)</description>
/// </item>
/// <item>
/// <term>myapp/* (pattern)</term>
/// <description>myapp:database, myapp:cache, etc.</description>
/// </item>
/// </list>
/// </para>
///
/// <para>
/// <strong>JSON Secret Processing:</strong>
/// When JsonProcessor is enabled, SecretString values containing valid JSON
/// are automatically flattened into the configuration hierarchy:
/// <code>
/// // Secret: myapp-database
/// // Value: {"host": "localhost", "port": 5432, "ssl": true}
/// // Results in:
/// // myapp-database:host = "localhost"
/// // myapp-database:port = "5432"
/// // myapp-database:ssl = "true"
/// </code>
/// </para>
///
/// <para>
/// <strong>Binary Secret Handling:</strong>
/// Binary secrets (certificates, keystores, etc.) are automatically converted to Base64 strings
/// for compatibility with the .NET configuration system. Applications can decode the Base64
/// data when needed.
/// </para>
///
/// <para>
/// <strong>Automatic Rotation Support:</strong>
/// The provider respects AWS Secrets Manager's automatic rotation by always retrieving
/// the AWSCURRENT version of secrets unless a specific version is requested.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Basic Secrets Manager configuration
/// var config = new FlexConfigurationBuilder()
///     .AddAwsSecretsManager(options =>
///     {
///         options.SecretNames = new[] { "myapp-database", "myapp-api-keys" };
///         options.JsonProcessor = true;
///     })
///     .Build();
///
/// // Access secret values
/// var dbHost = config["myapp-database:host"];
/// var apiKey = config["myapp-api-keys:external-service"];
///
/// // With strongly typed binding
/// containerBuilder.RegisterConfig&lt;DatabaseConfig&gt;("myapp-database");
/// </code>
/// </example>
public sealed class AwsSecretsManagerConfigurationProvider : ConfigurationProvider, IDisposable
{
    private readonly AwsSecretsManagerConfigurationSource _source;
    private readonly IAmazonSecretsManager _secretsClient;
    private readonly Timer? _reloadTimer;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="AwsSecretsManagerConfigurationProvider"/> class.
    /// Creates the AWS Secrets Manager client and configures automatic reloading if specified in the source options.
    /// </summary>
    /// <param name="source">
    /// The configuration source that contains the Secrets Manager access configuration,
    /// including secret names, AWS options, and processing settings.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when AWS credentials cannot be resolved or Secrets Manager client creation fails.</exception>
    /// <remarks>
    /// The constructor creates an AWS Secrets Manager client using the credentials and region
    /// specified in the source options, or falls back to the default AWS credential resolution chain.
    /// If automatic reloading is configured, a timer is set up to periodically refresh the secrets.
    /// </remarks>
    public AwsSecretsManagerConfigurationProvider(AwsSecretsManagerConfigurationSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        _source = source;

        try
        {
            // Create AWS Secrets Manager client using the credential resolution chain
            var awsOptions = _source.AwsOptions ?? new Amazon.Extensions.NETCore.Setup.AWSOptions();
            _secretsClient = awsOptions.CreateServiceClient<IAmazonSecretsManager>();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Failed to create AWS Secrets Manager client. Ensure AWS credentials are properly configured.", ex);
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
    /// Loads configuration data from AWS Secrets Manager.
    /// Retrieves all specified secrets and processes them according to their type and the provider's configuration options.
    /// </summary>
    /// <remarks>
    /// This method is called by the .NET configuration system during application startup
    /// and can be called again during automatic reloading. It handles both SecretString and SecretBinary
    /// types and applies appropriate processing based on the provider configuration.
    ///
    /// <para>
    /// <strong>Loading Process:</strong>
    /// <list type="number">
    /// <item>Clear existing configuration data</item>
    /// <item>Retrieve secrets from AWS Secrets Manager by name or pattern</item>
    /// <item>Process each secret according to its type (string or binary)</item>
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
    /// <exception cref="InvalidOperationException">Thrown when Secrets Manager access fails and the source is not optional.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when the configured credentials lack necessary permissions.</exception>
    public override void Load()
    {
        try
        {
            LoadAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            // For required sources, wrap and re-throw the exception
            throw new InvalidOperationException(
                $"Failed to load configuration from AWS Secrets Manager. " +
                $"Ensure the secrets exist and you have the necessary permissions.", ex);
        }
    }

    /// <summary>
    /// Asynchronously loads configuration data from AWS Secrets Manager.
    /// This is the core implementation that handles the actual AWS API calls and secret processing.
    /// </summary>
    /// <returns>A task that represents the asynchronous load operation.</returns>
    private async Task LoadAsync()
    {
        var configurationData = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        try
        {
            await LoadSecretsAsync(configurationData).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (!_source.Optional)
            {
                throw;
            }

            _source.OnLoadException?.Invoke(new SecretsManagerConfigurationProviderException(_source, ex));
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
    /// Loads secrets from AWS Secrets Manager and processes them into configuration data.
    /// Handles both individual secret names and pattern-based secret discovery.
    /// </summary>
    /// <param name="configurationData">The dictionary to store the processed configuration data.</param>
    /// <returns>A task that represents the asynchronous secret loading operation.</returns>
    private async Task LoadSecretsAsync(Dictionary<string, string?> configurationData)
    {
        if (_source.SecretNames == null || _source.SecretNames.Length == 0)
        {
            return; // No secrets specified
        }

        var tasks = _source.SecretNames.Select(
            secretName => ProcessSecretAsync(secretName, configurationData));

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    /// <summary>
    /// Loads a secret by its name or pattern and adds its values to the configuration data.
    /// If the secret name ends with '*', loads all secrets matching the pattern.
    /// If the secret is not found and is required, an exception is thrown.
    /// </summary>
    /// <param name="secretName">The name or pattern of the secret to load.</param>
    /// <param name="configurationData">The dictionary to populate with configuration values.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task ProcessSecretAsync(string secretName, Dictionary<string, string?> configurationData)
    {
        try
        {
            if (secretName.EndsWith("*", StringComparison.OrdinalIgnoreCase))
            {
                await LoadSecretsWithPatternAsync(secretName, configurationData).ConfigureAwait(false);
            }
            else
            {
                await LoadSingleSecretAsync(secretName, configurationData).ConfigureAwait(false);
            }
        }
        catch (ResourceNotFoundException)
        {
            if (!_source.Optional)
            {
                throw new InvalidOperationException(
                    $"Required secret '{secretName}' not found in AWS Secrets Manager.");
            }
        }
    }

    /// <summary>
    /// Loads a single secret from AWS Secrets Manager and processes it into configuration data.
    /// </summary>
    /// <param name="secretName">The name or ARN of the secret to load.</param>
    /// <param name="configurationData">The dictionary to store the processed configuration data.</param>
    /// <returns>A task that represents the asynchronous secret loading operation.</returns>
    private async Task LoadSingleSecretAsync(string secretName, Dictionary<string, string?> configurationData)
    {
        var request = new GetSecretValueRequest
        {
            SecretId = secretName,
            VersionStage = _source.VersionStage ?? "AWSCURRENT"
        };

        var response = await _secretsClient.GetSecretValueAsync(request).ConfigureAwait(false);
        var configKey = TransformSecretNameToConfigKey(response.Name);

        ProcessSecretValue(response, configurationData, configKey);
    }

    /// <summary>
    /// Loads secrets from AWS Secrets Manager whose names match the specified pattern.
    /// </summary>
    /// <param name="secretPattern">The secret name pattern ending with '*', used as a prefix filter.</param>
    /// <param name="configurationData">The dictionary to populate with loaded secrets.</param>
    /// <returns>A task representing the asynchronous secret loading operation.</returns>
    private async Task LoadSecretsWithPatternAsync(string secretPattern, Dictionary<string, string?> configurationData)
    {
        var secretNamePrefix = secretPattern.TrimEnd('*');
        var request = CreateListSecretsRequest(secretNamePrefix);
        var allSecrets = await ListAllSecretsWithPrefixAsync(request, secretNamePrefix);
        var loadTasks = allSecrets.Select(secret => LoadSingleSecretAsync(secret.Name, configurationData));

        await Task.WhenAll(loadTasks).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates and configures a ListSecretsRequest with a filter by secret name prefix.
    /// </summary>
    /// <param name="secretNamePrefix">The prefix to use for filtering secrets by name.</param>
    /// <returns>A configured <see cref="ListSecretsRequest"/> instance.</returns>
    private static ListSecretsRequest CreateListSecretsRequest(string secretNamePrefix) =>
        new()
        {
            MaxResults = 100,
            Filters =
            [
                new() { Key = FilterNameStringType.Name, Values = [secretNamePrefix] },
            ]
        };

    /// <summary>
    /// Iterates through all pages of secret listings, accumulating only those secrets whose names match the specified prefix.
    /// </summary>
    /// <param name="request">Initial ListSecretsRequest with applied filters and no NextToken.</param>
    /// <param name="secretNamePrefix">The prefix used for additional filtering.</param>
    /// <returns>A list of secrets matching the prefix across all pages.</returns>
    private async Task<List<SecretListEntry>> ListAllSecretsWithPrefixAsync(ListSecretsRequest request, string secretNamePrefix)
    {
        var collectedSecrets = new List<SecretListEntry>();
        do
        {
            var response = await _secretsClient.ListSecretsAsync(request).ConfigureAwait(false);
            collectedSecrets.AddRange(FilterSecretsByPrefix(response.SecretList, secretNamePrefix));
            request.NextToken = response.NextToken;
        }
        while (!string.IsNullOrEmpty(request.NextToken));
        return collectedSecrets;
    }

    /// <summary>
    /// Filters a collection of secrets, returning only those whose names start with the specified prefix.
    /// </summary>
    /// <param name="secrets">The source collection of secrets.</param>
    /// <param name="prefix">The prefix that secret names should start with.</param>
    /// <returns>An enumerable of secrets filtered by name prefix.</returns>
    private static IEnumerable<SecretListEntry> FilterSecretsByPrefix(IEnumerable<SecretListEntry> secrets, string prefix) =>
        secrets.Where(secret => secret.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Transforms an AWS Secrets Manager secret name to a .NET configuration key format.
    /// Converts hyphens to colons and applies a custom secret processor if configured.
    /// </summary>
    /// <param name="secretName">The AWS secret name (e.g., "myapp-database").</param>
    /// <returns>The transformed configuration key (e.g., "myapp:database").</returns>
    /// <remarks>
    /// The transformation process:
    /// <list type="number">
    /// <item>Replaces hyphens with colons to follow .NET configuration conventions</item>
    /// <item>Applies custom secret processor if configured</item>
    /// <item>Ensures the key is suitable for hierarchical configuration binding</item>
    /// </list>
    /// </remarks>
    private string TransformSecretNameToConfigKey(string secretName)
    {
        // Convert secret name to configuration key format
        // Secrets Manager typically uses hyphens, configuration uses colons
        var configKey = secretName.Replace('-', ':');

        // Apply a custom secret processor if configured
        if (_source.SecretProcessor != null)
        {
            configKey = _source.SecretProcessor.ProcessSecretName(configKey, secretName);
        }

        return configKey;
    }

    /// <summary>
    /// Processes a secret value according to its type and stores it in the configuration data.
    /// Handles both SecretString and SecretBinary types with appropriate processing logic.
    /// </summary>
    /// <param name="secretResponse">The AWS secret response containing the secret value.</param>
    /// <param name="configurationData">The dictionary to store the processed configuration data.</param>
    /// <param name="configKey">The transformed configuration key for this secret.</param>
    private void ProcessSecretValue(GetSecretValueResponse secretResponse, Dictionary<string, string?> configurationData, string configKey)
    {
        if (!string.IsNullOrEmpty(secretResponse.SecretString))
        {
            ProcessSecretString(secretResponse.SecretString, configurationData, configKey);
        }
        else if (secretResponse.SecretBinary != null)
        {
            ProcessSecretBinary(secretResponse.SecretBinary, configurationData, configKey);
        }
    }

    /// <summary>
    /// Processes a SecretString value, applying JSON flattening if enabled and applicable.
    /// SecretString values can contain simple values, JSON objects, or database credentials.
    /// </summary>
    /// <param name="secretString">The SecretString value to process.</param>
    /// <param name="configurationData">The dictionary to store the processed configuration data.</param>
    /// <param name="configKey">The configuration key for this secret.</param>
    private void ProcessSecretString(string secretString, Dictionary<string, string?> configurationData, string configKey)
    {
        // Check if JSON processing is enabled and this value contains JSON
        if (_source.JsonProcessor && ShouldProcessAsJson(configKey) && secretString.IsValidJson())
        {
            secretString.FlattenJsonValue(configurationData, configKey);
        }
        else
        {
            // Store as a simple key-value pair
            configurationData[configKey] = secretString;
        }
    }

    /// <summary>
    /// Processes a SecretBinary value by converting it to a Base64-encoded string.
    /// This enables storage of binary data (certificates, keystores, etc.) in the configuration system.
    /// </summary>
    /// <param name="secretBinary">The SecretBinary value to process.</param>
    /// <param name="configurationData">The dictionary to store the processed configuration data.</param>
    /// <param name="configKey">The configuration key for this secret.</param>
    private static void ProcessSecretBinary(MemoryStream secretBinary, Dictionary<string, string?> configurationData, string configKey)
    {
        // Convert binary data to Base64 string for compatibility with the configuration system
        var binaryData = secretBinary.ToArray();
        var base64String = Convert.ToBase64String(binaryData);
        configurationData[configKey] = base64String;
    }

    /// <summary>
    /// Determines whether a configuration key should be processed as JSON based on the provider configuration.
    /// Checks against JsonProcessorSecrets if specified, otherwise applies to all secrets when JsonProcessor is enabled.
    /// </summary>
    /// <param name="configKey">The configuration key to check.</param>
    /// <returns>True if the secret should be processed as JSON, false otherwise.</returns>
    private bool ShouldProcessAsJson(string configKey)
    {
        if (_source.JsonProcessorSecrets == null || _source.JsonProcessorSecrets.Length == 0)
        {
            return true; // Process all secrets as JSON when JsonProcessor is enabled
        }

        // Check if the key matches any of the specified secret patterns
        return _source.JsonProcessorSecrets.Any(pattern =>
        {
            var transformedPattern = TransformSecretNameToConfigKey(pattern.TrimEnd('*'));
            return configKey.StartsWith(transformedPattern, StringComparison.OrdinalIgnoreCase);
        });
    }

    /// <summary>
    /// Releases the unmanaged resources used by the provider and optionally releases the managed resources.
    /// Disposes of the AWS Secrets Manager client and stops the reload timer if configured.
    /// </summary>
    /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    [SuppressMessage("ReSharper", "FlagArgument", Justification =
        "Flag argument is used to indicate whether to dispose managed resources." +
        "No SRP violation as this is a standard pattern for IDisposable implementations.")]
    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _reloadTimer?.Dispose();
                _secretsClient.Dispose();
            }

            _disposed = true;
        }
    }

    /// <summary>
    /// Releases all resources used by the current instance of the <see cref="AwsSecretsManagerConfigurationProvider"/> class.
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
    }
}

/// <summary>
/// Represents an exception that occurs during Secrets Manager configuration provider loading.
/// Used to provide context about configuration loading failures for error handling and logging.
/// </summary>
public class SecretsManagerConfigurationProviderException : Exception
{
    private readonly string _source;

    /// <summary>
    /// Gets the source of the exception (the Secrets Manager configuration details).
    /// </summary>
    public override string Source => _source;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecretsManagerConfigurationProviderException"/> class.
    /// </summary>
    /// <param name="source">The configuration source that caused the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public SecretsManagerConfigurationProviderException(AwsSecretsManagerConfigurationSource source, Exception innerException)
        : base($"Failed to load configuration from AWS Secrets Manager", innerException)
    {
        _source = $"SecretsManager[{string.Join(",", source.SecretNames ?? [])}]";
    }
}
