// <copyright file="AzureAppConfigurationProvider.cs" company="FlexKit">
// Copyright (c) FlexKit. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Azure.Data.AppConfiguration;
using Azure.Identity;
using FlexKit.Configuration.Providers.Azure.Extensions;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;

namespace FlexKit.Configuration.Providers.Azure.Sources;

/// <summary>
/// Configuration provider that reads Azure App Configuration and makes it available
/// through the .NET configuration system. Supports key filtering, labels, and automatic reloading
/// for dynamic configuration management.
/// </summary>
/// <remarks>
/// This provider integrates Azure App Configuration with the .NET configuration infrastructure, enabling
/// applications to store configuration data in Azure while maintaining compatibility with existing
/// configuration patterns and strongly typed configuration binding.
///
/// <para>
/// <strong>Key Features:</strong>
/// <list type="bullet">
/// <item>Loads configuration keys from Azure App Configuration</item>
/// <item>Supports key filtering to load only the relevant configuration</item>
/// <item>Uses labels for environment or version-specific configuration</item>
/// <item>Provides automatic reloading for dynamic configuration updates</item>
/// <item>Integrates with an Azure credential resolution chain</item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Configuration Key Format:</strong>
/// App Configuration keys are used directly as .NET configuration keys,
/// maintaining the hierarchical structure using colon separators.
/// </para>
/// </remarks>
internal sealed class AzureAppConfigurationProvider : ConfigurationProvider, IDisposable
{
    /// <summary>
    /// An instance of the AzureAppConfigurationSource that provides the configuration source
    /// details and settings for the Azure App Configuration integration.
    /// This is used to manage and retrieve configuration data asynchronously from the Azure App Configuration.
    /// </summary>
    private readonly AzureAppConfigurationSource _source;

    /// <summary>
    /// A readonly instance of the ConfigurationClient used to communicate with Azure App Configuration service.
    /// It facilitates loading, retrieving, and managing configuration settings from the Azure App Configuration
    /// storage.
    /// </summary>
    private readonly ConfigurationClient _configClient;

    /// <summary>
    /// A timer instance used to trigger periodic configuration reloads
    /// based on the specified reload interval in the AzureAppConfigurationSource.
    /// Responsible for invoking asynchronous configuration updates at regular intervals.
    /// </summary>
    private Timer? _reloadTimer;

    /// <summary>
    /// Indicates whether the resources used by the AzureAppConfigurationProvider
    /// have already been released. This flag is used to ensure that the Dispose
    /// method is only called once and avoids potential multiple disposal of resources.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureAppConfigurationProvider"/> class with an existing
    /// configuration client.
    /// This constructor is primarily used for testing scenarios where a pre-configured client is provided.
    /// </summary>
    /// <param name="source">The configuration source that contains the App Configuration access configuration.</param>
    /// <param name="configClient">The pre-configured Azure App Configuration client to use.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source"/> or <paramref name="configClient"/> is null.
    /// </exception>
    [UsedImplicitly]
    internal AzureAppConfigurationProvider(
        AzureAppConfigurationSource source,
        ConfigurationClient configClient)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(configClient);

        _source = source;
        _configClient = configClient;

        SetupReloadTimer();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureAppConfigurationProvider"/> class.
    /// Creates the Azure App Configuration client and configures automatic reloading if specified
    /// in the source options.
    /// </summary>
    /// <param name="source">
    /// The configuration source that contains the App Configuration access configuration,
    /// including the connection string, Azure options, and processing settings.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when Azure credentials cannot be resolved or App Configuration client creation fails.
    /// </exception>
    public AzureAppConfigurationProvider(AzureAppConfigurationSource source)
        : this(source, CreateConfigurationClient(source))
    {
    }

    /// <summary>
    /// Creates an Azure App Configuration client based on the provided source configuration.
    /// Handles both connection string and endpoint URI with credential-based authentication.
    /// </summary>
    /// <param name="source">The configuration source containing connection details.</param>
    /// <returns>A configured <see cref="ConfigurationClient"/> instance.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the client cannot be created due to invalid configuration or credential issues.
    /// </exception>
    private static ConfigurationClient CreateConfigurationClient(AzureAppConfigurationSource source)
    {
        try
        {
            // Create Azure App Configuration client
            if (IsConnectionString(source.ConnectionString))
            {
                return new ConfigurationClient(source.ConnectionString);
            }

            // Treat as endpoint URI and use credential
            var credential = source.Credential ?? new DefaultAzureCredential();
            return new ConfigurationClient(new Uri(source.ConnectionString), credential);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Failed to create Azure App Configuration client. Ensure connection" +
                " string or credentials are properly configured.",
                ex);
        }
    }

    /// <summary>
    /// Sets up the automatic reload timer if a reload interval is specified in the source configuration.
    /// The timer will periodically trigger configuration reloading to keep data synchronized with
    /// Azure App Configuration.
    /// </summary>
    private void SetupReloadTimer()
    {
        if (!_source.ReloadAfter.HasValue)
        {
            return;
        }

        _reloadTimer = new Timer(
            callback: _ => LoadAsync().ConfigureAwait(false),
            state: null,
            dueTime: _source.ReloadAfter.Value,
            period: _source.ReloadAfter.Value);
    }

    /// <summary>
    /// Loads configuration data from Azure App Configuration.
    /// Retrieves configuration keys according to the specified filters and processes them.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when App Configuration access fails and the source is not optional.
    /// </exception>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the configured credentials lack necessary permissions.
    /// </exception>
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
                $"Failed to load configuration from Azure App Configuration '{_source.ConnectionString}'. " +
                $"Ensure the connection string is valid and you have the necessary permissions.", ex);
        }
    }

    /// <summary>
    /// Asynchronously loads configuration data from Azure App Configuration.
    /// This is the core implementation that handles the actual Azure API calls and configuration processing.
    /// </summary>
    /// <returns>A task that represents the asynchronous load operation.</returns>
    private async Task LoadAsync()
    {
        var configurationData = new ConcurrentDictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        try
        {
            await LoadConfigurationAsync(configurationData).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (!_source.Optional)
            {
                throw;
            }

            _source.OnLoadException?.Invoke(new AppConfigurationProviderException(_source, ex));
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
    /// Loads configuration from Azure App Configuration and processes it into configuration data.
    /// Handles pagination and applies the specified key filter and label.
    /// </summary>
    /// <param name="configurationData">The dictionary to store the processed configuration data.</param>
    /// <returns>A task that represents the asynchronous configuration loading operation.</returns>
    private async Task LoadConfigurationAsync(ConcurrentDictionary<string, string?> configurationData)
    {
        var selector = new SettingSelector
        {
            KeyFilter = _source.KeyFilter ?? "*",
            LabelFilter = _source.Label
        };

        await foreach (var setting in _configClient.GetConfigurationSettingsAsync(selector).ConfigureAwait(false))
        {
            if (setting is { Key: not null, Value: not null })
            {
                ProcessConfigurationSetting(setting, configurationData);
            }
        }
    }

    /// <summary>
    /// Processes a single configuration setting from Azure App Configuration and adds it
    /// to the configuration data dictionary.
    /// </summary>
    /// <param name="setting">The configuration setting retrieved from Azure App Configuration.</param>
    /// <param name="configurationData">
    /// The concurrent dictionary storing processed configuration key-value pairs.
    /// </param>
    /// <remarks>
    /// If JSON processing is enabled and the setting value is valid JSON, the method will flatten the JSON structure
    /// into multiple configuration keys. Otherwise, the setting is stored as a simple key-value pair.
    /// For example, if JSON processing is enabled and the value is {"nested":{"key":"value"}}, it will create
    /// configuration entries like "parentKey:nested:key" = "value". If JSON processing is disabled or the value
    /// is not JSON, it will store the raw value directly.
    /// </remarks>
    private void ProcessConfigurationSetting(
        ConfigurationSetting setting,
        ConcurrentDictionary<string, string?> configurationData)
    {
        // Check if JSON processing is enabled and this value contains JSON
        if (_source.JsonProcessor && setting.Value.IsValidJson())
        {
            setting.Value.FlattenJsonValue(configurationData, setting.Key);
        }
        else
        {
            // Store as a simple key-value pair
            configurationData[setting.Key] = setting.Value;
        }
    }

    /// <summary>
    /// Determines if the connection string is a full connection string or just an endpoint.
    /// </summary>
    /// <param name="connectionString">The connection string to check.</param>
    /// <returns>True if it's a connection string, false if it's an endpoint URI.</returns>
    private static bool IsConnectionString(string connectionString) =>
        connectionString.Contains("Endpoint=", StringComparison.OrdinalIgnoreCase) &&
        (connectionString.Contains("Id=", StringComparison.OrdinalIgnoreCase) ||
         connectionString.Contains("Secret=", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Releases the unmanaged resources used by the provider and optionally releases the managed resources.
    /// Disposes of the Azure App Configuration client and stops the reload timer if configured.
    /// </summary>
    /// <param name="disposing">
    /// True to release both managed and unmanaged resources; false to release only unmanaged resources.
    /// </param>
    [SuppressMessage("ReSharper", "FlagArgument", Justification =
        "Flag argument is used to indicate whether to dispose managed resources." +
        "No SRP violation as this is a standard pattern for IDisposable implementations.")]
    private void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _reloadTimer?.Dispose();
        }

        _disposed = true;
    }

    /// <summary>
    /// Releases all resources used by the current instance of the <see cref="AzureAppConfigurationProvider"/> class.
    /// </summary>
    public void Dispose() => Dispose(disposing: true);
}
