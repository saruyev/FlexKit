// <copyright file="AzureAppConfigurationProvider.cs" company="FlexKit">
// Copyright (c) FlexKit. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

using System.Diagnostics.CodeAnalysis;
using Azure.Data.AppConfiguration;
using Azure.Identity;
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
public sealed class AzureAppConfigurationProvider : ConfigurationProvider, IDisposable
{
    private readonly AzureAppConfigurationSource _source;
    private readonly ConfigurationClient _configClient;
    private readonly Timer? _reloadTimer;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureAppConfigurationProvider"/> class.
    /// Creates the Azure App Configuration client and configures automatic reloading if specified in the source options.
    /// </summary>
    /// <param name="source">
    /// The configuration source that contains the App Configuration access configuration,
    /// including the connection string, Azure options, and processing settings.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when Azure credentials cannot be resolved or App Configuration client creation fails.</exception>
    public AzureAppConfigurationProvider(AzureAppConfigurationSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        _source = source;

        try
        {
            // Create Azure App Configuration client
            if (IsConnectionString(_source.ConnectionString))
            {
                _configClient = new ConfigurationClient(_source.ConnectionString);
            }
            else
            {
                // Treat as endpoint URI and use credential
                var credential = _source.Credential ?? new DefaultAzureCredential();
                _configClient = new ConfigurationClient(new Uri(_source.ConnectionString), credential);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Failed to create Azure App Configuration client. Ensure connection string or credentials are properly configured.", ex);
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
    /// Loads configuration data from Azure App Configuration.
    /// Retrieves configuration keys according to the specified filters and processes them.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when App Configuration access fails and the source is not optional.</exception>
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
        var configurationData = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

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
    private async Task LoadConfigurationAsync(Dictionary<string, string?> configurationData)
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
                configurationData[setting.Key] = setting.Value;
            }
        }
    }

    /// <summary>
    /// Determines if the connection string is a full connection string or just an endpoint.
    /// </summary>
    /// <param name="connectionString">The connection string to check.</param>
    /// <returns>True if it's a connection string, false if it's an endpoint URI.</returns>
    private static bool IsConnectionString(string connectionString)
    {
        return connectionString.Contains("Endpoint=", StringComparison.OrdinalIgnoreCase) &&
               (connectionString.Contains("Id=", StringComparison.OrdinalIgnoreCase) ||
                connectionString.Contains("Secret=", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Releases the unmanaged resources used by the provider and optionally releases the managed resources.
    /// Disposes of the Azure App Configuration client and stops the reload timer if configured.
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
    /// Releases all resources used by the current instance of the <see cref="AzureAppConfigurationProvider"/> class.
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
    }
}
