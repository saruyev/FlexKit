// <copyright file="AzureKeyVaultConfigurationProvider.cs" company="FlexKit">
// Copyright (c) FlexKit. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

using System.Diagnostics.CodeAnalysis;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using FlexKit.Configuration.Providers.Azure.Extensions;
using Microsoft.Extensions.Configuration;

namespace FlexKit.Configuration.Providers.Azure.Sources;

/// <summary>
/// Configuration provider that reads Azure Key Vault secrets and makes them available
/// through the .NET configuration system. Supports hierarchical secret organization, automatic type conversion,
/// and JSON secret processing for complex configuration structures.
/// </summary>
/// <remarks>
/// This provider integrates Azure Key Vault with the .NET configuration infrastructure, enabling
/// applications to store sensitive configuration data in Azure while maintaining compatibility with existing
/// configuration patterns and strongly typed configuration binding.
///
/// <para>
/// <strong>Secret Name Transformation:</strong>
/// Secret names are transformed from Azure Key Vault format to .NET configuration keys:
/// <list type="table">
/// <listheader>
/// <term>Key Vault Secret Name</term>
/// <description>.NET Configuration Key</description>
/// </listheader>
/// <item>
/// <term>myapp--database--host</term>
/// <description>myapp:database:host</description>
/// </item>
/// <item>
/// <term>myapp--features--caching</term>
/// <description>myapp:features:caching</description>
/// </item>
/// </list>
/// </para>
///
/// <para>
/// <strong>JSON Secret Processing:</strong>
/// When JsonProcessor is enabled, secret values containing valid JSON
/// are automatically flattened into the configuration hierarchy:
/// <code>
/// // Secret: database-config
/// // Value: {"host": "localhost", "port": 5432, "ssl": true}
/// // Results in:
/// // database-config:host = "localhost"
/// // database-config:port = "5432"
/// // database-config:ssl = "true"
/// </code>
/// </para>
/// </remarks>
public sealed class AzureKeyVaultConfigurationProvider : ConfigurationProvider, IDisposable
{
    private readonly AzureKeyVaultConfigurationSource _source;
    private readonly SecretClient _secretClient;
    private readonly Timer? _reloadTimer;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureKeyVaultConfigurationProvider"/> class.
    /// Creates the Azure Key Vault client and configures automatic reloading if specified in the source options.
    /// </summary>
    /// <param name="source">
    /// The configuration source that contains the Key Vault access configuration,
    /// including the vault URI, Azure options, and processing settings.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when Azure credentials cannot be resolved or Key Vault client creation fails.</exception>
    public AzureKeyVaultConfigurationProvider(AzureKeyVaultConfigurationSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        _source = source;

        try
        {
            // Create the Azure Key Vault client using the credential resolution chain
            var credential = _source.Credential ?? new DefaultAzureCredential();
            _secretClient = new SecretClient(new Uri(_source.VaultUri), credential);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Failed to create Azure Key Vault client. Ensure Azure credentials are properly configured.", ex);
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
    /// Loads configuration data from Azure Key Vault.
    /// Retrieves all secrets from the specified Key Vault and processes them according
    /// to their type and the provider's configuration options.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when Key Vault access fails and the source is not optional.</exception>
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
                $"Failed to load configuration from Azure Key Vault '{_source.VaultUri}'. " +
                $"Ensure the vault exists and you have the necessary permissions.", ex);
        }
    }

    /// <summary>
    /// Asynchronously loads configuration data from Azure Key Vault.
    /// This is the core implementation that handles the actual Azure API calls and secret processing.
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

            _source.OnLoadException?.Invoke(new KeyVaultConfigurationProviderException(_source, ex));
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
    /// Loads secrets from Azure Key Vault and processes them into configuration data.
    /// Handles pagination and processes all secrets according to the provider configuration.
    /// </summary>
    /// <param name="configurationData">The dictionary to store the processed configuration data.</param>
    /// <returns>A task that represents the asynchronous secret loading operation.</returns>
    private async Task LoadSecretsAsync(Dictionary<string, string?> configurationData)
    {
        var enabledSecrets = await GetEnabledSecretsAsync();
        await ProcessSecretsAsync(enabledSecrets, configurationData);
    }

    /// <summary>
    /// Retrieves all enabled secrets from the Azure Key Vault.
    /// </summary>
    /// <returns>A list of enabled secret properties from the Key Vault.</returns>
    /// <remarks>
    /// This method filters out disabled secrets during the retrieval process to ensure
    /// only active secrets are processed in subsequent operations.
    /// </remarks>
    private async Task<List<SecretProperties>> GetEnabledSecretsAsync()
    {
        var enabledSecrets = new List<SecretProperties>();
        await foreach (var secretProperties in _secretClient.GetPropertiesOfSecretsAsync())
        {
            if (secretProperties.Enabled == true)
            {
                enabledSecrets.Add(secretProperties);
            }
        }
        return enabledSecrets;
    }

    /// <summary>
    /// Processes a collection of secrets in parallel, loading their values and storing them in the configuration data.
    /// </summary>
    /// <param name="secrets">The collection of secret properties to process.</param>
    /// <param name="configurationData">The dictionary to store the processed configuration data.</param>
    /// <returns>A task that represents the asynchronous processing operation.</returns>
    private async Task ProcessSecretsAsync(List<SecretProperties> secrets, Dictionary<string, string?> configurationData)
    {
        var secretProcessingTasks = secrets.Select(secret => ProcessSingleSecretAsync(secret, configurationData));
        await Task.WhenAll(secretProcessingTasks);
    }

    /// <summary>
    /// Processes a single secret from the Key Vault, retrieving its value and storing it in the configuration data.
    /// </summary>
    /// <param name="secretProperties">The properties of the secret to process.</param>
    /// <param name="configurationData">The dictionary to store the processed configuration data.</param>
    /// <returns>A task that represents the asynchronous processing operation.</returns>
    /// <remarks>
    /// This method handles error cases differently based on whether the configuration source is optional:
    /// - For optional sources, errors are silently ignored
    /// - For required sources, errors are wrapped in an InvalidOperationException
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the secret cannot be loaded and the configuration source is not optional.
    /// </exception>
    private async Task ProcessSingleSecretAsync(SecretProperties secretProperties, Dictionary<string, string?> configurationData)
    {
        try
        {
            var secretResponse = await _secretClient.GetSecretAsync(secretProperties.Name).ConfigureAwait(false);
            var configKey = TransformSecretNameToConfigKey(secretResponse.Value.Name);
            ProcessSecretValue(secretResponse.Value, configurationData, configKey);
        }
        catch (Exception) when (_source.Optional)
        {
            // Log individual secret failures but continue with others
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to load secret '{secretProperties.Name}' from Key Vault.", ex);
        }
    }

    /// <summary>
    /// Transforms an Azure Key Vault secret name to a .NET configuration key format.
    /// Converts double hyphens to colon separators for hierarchical configuration keys.
    /// </summary>
    /// <param name="secretName">The Key Vault secret name (e.g., "myapp--database--host").</param>
    /// <returns>The transformed configuration key (e.g., "myapp:database:host").</returns>
    private string TransformSecretNameToConfigKey(string secretName)
    {
        var configKey = secretName;

        // Convert double hyphens to colon notation (Key Vault naming convention)
        configKey = configKey.Replace("--", ":");

        // Apply a custom secret processor if configured
        if (_source.SecretProcessor != null)
        {
            configKey = _source.SecretProcessor.ProcessSecretName(configKey, secretName);
        }

        return configKey;
    }

    /// <summary>
    /// Processes a secret value and stores it in the configuration data.
    /// Handles JSON processing if enabled and applicable.
    /// </summary>
    /// <param name="secret">The Key Vault secret to process.</param>
    /// <param name="configurationData">The dictionary to store the processed configuration data.</param>
    /// <param name="configKey">The transformed configuration key for this secret.</param>
    private void ProcessSecretValue(KeyVaultSecret secret, Dictionary<string, string?> configurationData, string configKey)
    {
        var value = secret.Value;

        // Check if JSON processing is enabled and this value contains JSON
        if (_source.JsonProcessor && ShouldProcessAsJson(secret.Name) && value.IsValidJson())
        {
            value.FlattenJsonValue(configurationData, configKey);
        }
        else
        {
            // Store as a simple key-value pair
            configurationData[configKey] = value;
        }
    }

    /// <summary>
    /// Determines whether a secret should be processed as JSON based on the provider configuration.
    /// Checks against JsonProcessorSecrets if specified, otherwise applies to all secrets when JsonProcessor is enabled.
    /// </summary>
    /// <param name="secretName">The secret name to check.</param>
    /// <returns>True if the secret should be processed as JSON, false otherwise.</returns>
    private bool ShouldProcessAsJson(string secretName)
    {
        if (_source.JsonProcessorSecrets?.Length is not > 0)
        {
            return true; // Process all secrets as JSON when JsonProcessor is enabled
        }

        // Check if the secret name matches any of the specified names
        return _source.JsonProcessorSecrets.Any(name =>
            secretName.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Releases the unmanaged resources used by the provider and optionally releases the managed resources.
    /// Disposes of the Azure Key Vault client and stops the reload timer if configured.
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
            }

            _disposed = true;
        }
    }

    /// <summary>
    /// Releases all resources used by the current instance of the <see cref="AzureKeyVaultConfigurationProvider"/> class.
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
    }
}
