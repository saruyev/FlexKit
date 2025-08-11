// <copyright file="AzureAppConfigurationSource.cs" company="FlexKit">
// Copyright (c) FlexKit. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

using Azure.Core;
using Azure.Data.AppConfiguration;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;

namespace FlexKit.Configuration.Providers.Azure.Sources;

/// <summary>
/// Configuration source that represents Azure App Configuration in the configuration system.
/// Implements IConfigurationSource to integrate App Configuration support with the standard .NET configuration
/// infrastructure, enabling App Configuration data to be used alongside other configuration sources.
/// </summary>
/// <remarks>
/// This class serves as the factory and metadata container for Azure App Configuration providers.
/// It follows the standard .NET configuration pattern where sources are responsible for creating their
/// corresponding providers and defining configuration parameters.
///
/// <para>
/// <strong>Role in Configuration Pipeline:</strong>
/// <list type="number">
/// <item>Defines the App Configuration connection string and loading options</item>
/// <item>Integrates with ConfigurationBuilder through IConfigurationSource</item>
/// <item>Creates AzureAppConfigurationProvider instances when requested</item>
/// <item>Provides metadata about the App Configuration source to the configuration system</item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Integration with FlexKit:</strong>
/// This source is designed to work seamlessly with FlexConfigurationBuilder and other FlexKit
/// configuration components, providing Azure App Configuration support as a first-class configuration
/// source option that maintains all FlexKit capabilities, including dynamic access and type conversion.
/// </para>
/// </remarks>
public class AzureAppConfigurationSource : IConfigurationSource
{
    /// <summary>
    /// Gets or sets the App Configuration connection string or endpoint URI.
    /// This defines the specific App Configuration store from which to retrieve configuration data.
    /// </summary>
    /// <value>
    /// The App Configuration connection string or endpoint URI.
    /// Can be a full connection string or just an endpoint like "https://myapp-config.azconfig.io".
    /// </value>
    public string ConnectionString { get; [UsedImplicitly] set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the App Configuration source is optional.
    /// When true, failures to load configuration will not cause configuration building to fail.
    /// </summary>
    /// <value>
    /// True if the App Configuration source is optional; false if it's required.
    /// Default is true.
    /// </value>
    public bool Optional { get; [UsedImplicitly] set; } = true;

    /// <summary>
    /// Gets or sets the key filter pattern to limit which configuration keys are loaded.
    /// When specified, only keys matching this pattern will be retrieved from App Configuration.
    /// </summary>
    /// <value>
    /// A key filter pattern (e.g., "myapp:*", "database:*") or null to load all keys.
    /// Default is null (no filtering).
    /// </value>
    public string? KeyFilter { get; [UsedImplicitly] set; }

    /// <summary>
    /// Gets or sets the label to filter configuration keys by.
    /// Labels in App Configuration provide a way to manage different versions or environments.
    /// </summary>
    /// <value>
    /// The label to filter by (e.g., "production", "staging", "v1.0") or null for the default label.
    /// Default is null (uses the default label).
    /// </value>
    public string? Label { get; [UsedImplicitly] set; }

    /// <summary>
    /// Gets or sets the automatic reload interval for App Configuration data.
    /// When set, the provider will periodically refresh the configuration from App Configuration.
    /// </summary>
    /// <value>
    /// The interval at which to reload configuration or null to disable automatic reloading.
    /// Default is null.
    /// </value>
    public TimeSpan? ReloadAfter { get; [UsedImplicitly] set; }

    /// <summary>
    /// Gets or sets the Azure credential for App Configuration access.
    /// Provides control over Azure authentication and credential management.
    /// </summary>
    /// <value>
    /// The Azure credential to use for App Configuration access, or null to use defaults.
    /// Default is null (uses the Azure credential resolution chain).
    /// </value>
    public TokenCredential? Credential { get; [UsedImplicitly] set; }

    /// <summary>
    /// Gets or sets the error handling callback for optional configuration loading failures.
    /// Invoked when configuration loading fails and the source is marked as optional.
    /// </summary>
    /// <value>
    /// An action that handles configuration loading exceptions, or null for default handling.
    /// </value>
    public Action<AppConfigurationProviderException>? OnLoadException { get; [UsedImplicitly] set; }

    /// <summary>
    /// Gets or sets a pre-configured Azure App Configuration client for testing scenarios.
    /// When provided, this client will be used instead of creating a new one from the App Configuration URI and credentials.
    /// </summary>
    /// <value>
    /// A configured ConfigurationClient instance, or null to create a new client using the App Configuration URI and credentials.
    /// Default is null.
    /// </value>
    public ConfigurationClient? ConfigurationClient { get; [UsedImplicitly] set; }

    /// <summary>
    /// Gets or sets a value indicating whether JSON configuration settings should be automatically flattened.
    /// When enabled, configuration settings values containing valid JSON will be
    /// processed into hierarchical configuration keys.
    /// </summary>
    /// <value>
    /// True to enable JSON processing; false to treat all configuration settings as simple strings.
    /// Default is false.
    /// </value>
    public bool JsonProcessor { get; [UsedImplicitly] set; }

    /// <summary>
    /// Creates a new Azure App Configuration provider that will load data from this source.
    /// This method is called by the .NET configuration system when building the configuration
    /// from all registered sources.
    /// </summary>
    /// <param name="builder">
    /// The configuration builder that is requesting the provider.
    /// This parameter is required by the IConfigurationSource interface but is not used by this implementation.
    /// </param>
    /// <returns>
    /// A new <see cref="AzureAppConfigurationProvider"/> instance configured to load
    /// configuration from Azure App Configuration according to this source's properties.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return ConfigurationClient != null
            ? new AzureAppConfigurationProvider(this, ConfigurationClient)
            : new AzureAppConfigurationProvider(this);
    }
}

/// <summary>
/// Represents an exception that occurs during App Configuration provider loading.
/// Used to provide context about configuration loading failures for error handling and logging.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AppConfigurationProviderException"/> class.
/// </remarks>
/// <param name="source">The configuration source that caused the exception.</param>
/// <param name="innerException">The exception that is the cause of the current exception.</param>
public class AppConfigurationProviderException(AzureAppConfigurationSource source, Exception innerException) : Exception($"Failed to load configuration from Azure App Configuration source: {source.ConnectionString}", innerException)
{
    /// <summary>
    /// The configuration source that caused the exception.
    /// </summary>
    private readonly string _source = source.ConnectionString;

    /// <summary>
    /// Gets the source of the exception (the App Configuration connection string).
    /// </summary>
    public override string Source => _source;
}
