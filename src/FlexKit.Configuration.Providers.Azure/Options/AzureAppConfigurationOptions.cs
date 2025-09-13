using Azure.Core;
using Azure.Data.AppConfiguration;
using FlexKit.Configuration.Providers.Azure.Sources;
using JetBrains.Annotations;

namespace FlexKit.Configuration.Providers.Azure.Options;

/// <summary>
/// Configuration options for Azure App Configuration integration with FlexKit configuration.
/// Provides a strongly typed way to configure all aspects of App Configuration access,
/// including Azure settings, key filtering, labels, and error handling.
/// </summary>
/// <remarks>
/// This class serves as a data transfer object for App Configuration options,
/// providing a clean API surface for the configuration lambda while maintaining type safety
/// and IntelliSense support.
///
/// <para>
/// <strong>Configuration Categories:</strong>
/// <list type="bullet">
/// <item><strong>Azure Settings:</strong> Credentials, connection string, and client configuration</item>
/// <item><strong>Key Selection:</strong> Filtering by key patterns and labels</item>
/// <item><strong>Operational Settings:</strong> Reloading, error handling, and monitoring</item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Default Values:</strong>
/// The class provides sensible defaults for most scenarios:
/// <list type="bullet">
/// <item>Optional = true (non-blocking failures)</item>
/// <item>KeyFilter = null (load all keys)</item>
/// <item>Label = null (use default label)</item>
/// <item>ReloadAfter = null (no automatic reloading)</item>
/// <item>Credential = null (use default credential chain)</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Typical production configuration
/// var options = new AzureAppConfigurationOptions
/// {
///     ConnectionString = "https://prod-config.azconfig.io",
///     Optional = false,
///     KeyFilter = "myapp:*",
///     Label = "production",
///     ReloadAfter = TimeSpan.FromMinutes(5),
///     Credential = new DefaultAzureCredential(),
///     OnLoadException = ex => logger.LogError(ex, "App Configuration error")
/// };
/// </code>
/// </example>
public class AzureAppConfigurationOptions
{
    /// <summary>
    /// Gets or sets the App Configuration connection string or endpoint URI.
    /// This defines the specific App Configuration store from which to retrieve configuration data.
    /// </summary>
    /// <value>
    /// The App Configuration connection string or endpoint URI.
    /// Can be a full connection string or just an endpoint like "https://myapp-config.azconfig.io".
    /// </value>
    /// <remarks>
    /// <para>
    /// <strong>Connection String Format:</strong>
    /// Full connection strings include credentials: "Endpoint=https://myapp-config.azconfig.io;Id=...;Secret=..."
    /// Endpoint-only format requires credential to be provided separately.
    /// </para>
    ///
    /// <para>
    /// <strong>Best Practices:</strong>
    /// <list type="bullet">
    /// <item>Use environment-specific store names: prod-myapp-config, dev-myapp-config</item>
    /// <item>Prefer endpoint + credential over connection strings for better security</item>
    /// <item>Ensure the store exists in the same region as your application</item>
    /// <item>Use separate stores for different environments to maintain isolation</item>
    /// </list>
    /// </para>
    /// </remarks>
    public string? ConnectionString { get; [UsedImplicitly] set; }

    /// <summary>
    /// Gets or sets a value indicating whether the App Configuration source is optional.
    /// When true, failures to load configuration will not cause configuration building to fail.
    /// </summary>
    /// <value>
    /// True if the App Configuration source is optional; false if it's required.
    /// Default is true.
    /// </value>
    /// <remarks>
    /// <para>
    /// <strong>Production Recommendations:</strong>
    /// <list type="bullet">
    /// <item>
    /// <strong>Critical Config:</strong> Set to false for essential configuration like database connections
    /// </item>
    /// <item><strong>Feature Flags:</strong> Set to true for optional features that have reasonable defaults</item>
    /// <item><strong>Environment-Specific:</strong> Set to true for dev/test, false for production</item>
    /// </list>
    /// </para>
    /// </remarks>
    public bool Optional { get; [UsedImplicitly] set; } = true;

    /// <summary>
    /// Gets or sets the key filter pattern to limit which configuration keys are loaded.
    /// When specified, only keys matching this pattern will be retrieved from App Configuration.
    /// </summary>
    /// <value>
    /// A key filter pattern (e.g., "myapp:*", "database:*") or null to load all keys.
    /// Default is null (no filtering).
    /// </value>
    /// <remarks>
    /// <para>
    /// <strong>Filter Patterns:</strong>
    /// <list type="bullet">
    /// <item><strong>Exact match:</strong> "myapp:database:host" loads only that specific key</item>
    /// <item><strong>Prefix match:</strong> "myapp:*" loads all keys starting with "myapp:"</item>
    /// <item><strong>Wildcard match:</strong> "*:database:*" loads all database-related keys</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Performance Benefits:</strong>
    /// Using key filters reduces the amount of data transferred and processed,
    /// improving application startup time and reducing memory usage.
    /// </para>
    /// </remarks>
    public string? KeyFilter { get; [UsedImplicitly] set; }

    /// <summary>
    /// Gets or sets the label to filter configuration keys by.
    /// Labels in App Configuration provide a way to manage different versions or environments.
    /// </summary>
    /// <value>
    /// The label to filter by (e.g., "production", "staging", "v1.0") or null for the default label.
    /// Default is null (uses the default label).
    /// </value>
    /// <remarks>
    /// <para>
    /// <strong>Label Use Cases:</strong>
    /// <list type="bullet">
    /// <item><strong>Environment separation:</strong> "production", "staging", "development"</item>
    /// <item><strong>Version management:</strong> "v1.0", "v2.0", "beta"</item>
    /// <item><strong>Feature branches:</strong> "feature-x", "hotfix-y"</item>
    /// <item><strong>A/B testing:</strong> "variant-a", "variant-b"</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Default Label:</strong>
    /// When no label is specified (null), App Configuration returns keys with no label assigned.
    /// This is the default behavior and suitable for most scenarios.
    /// </para>
    /// </remarks>
    public string? Label { get; [UsedImplicitly] set; }

    /// <summary>
    /// Gets or sets the automatic reload interval for App Configuration data.
    /// When set, the provider will periodically refresh the configuration from App Configuration.
    /// </summary>
    /// <value>
    /// The interval at which to reload configuration or null to disable automatic reloading.
    /// Default is null.
    /// </value>
    /// <remarks>
    /// <para>
    /// <strong>Cost Considerations:</strong>
    /// Each reload operation makes API calls to Azure App Configuration. Consider the balance
    /// between configuration freshness and Azure API costs when setting this interval.
    /// </para>
    ///
    /// <para>
    /// <strong>Recommended Values:</strong>
    /// <list type="bullet">
    /// <item><strong>Development:</strong> 30 seconds to 2 minutes for rapid iteration</item>
    /// <item><strong>Production:</strong> 5-15 minutes for operational stability</item>
    /// <item><strong>Critical Systems:</strong> 15+ minutes to minimize API overhead</item>
    /// </list>
    /// </para>
    /// </remarks>
    public TimeSpan? ReloadAfter { get; [UsedImplicitly] set; }

    /// <summary>
    /// Gets or sets the Azure credential for App Configuration access.
    /// Provides control over Azure authentication and credential management.
    /// </summary>
    /// <value>
    /// The Azure credential to use for App Configuration access, or null to use defaults.
    /// Default is null (uses the Azure credential resolution chain).
    /// </value>
    /// <remarks>
    /// <para>
    /// <strong>Security Best Practices:</strong>
    /// <list type="bullet">
    /// <item>Use Managed Identity instead of hardcoded credentials</item>
    /// <item>Apply least-privilege permissions for App Configuration access</item>
    /// <item>Use different credentials for different environments</item>
    /// <item>Avoid storing credentials in application configuration</item>
    /// </list>
    /// </para>
    /// </remarks>
    public TokenCredential? Credential { get; [UsedImplicitly] set; }

    /// <summary>
    /// Gets or sets the error handling callback for optional configuration loading failures.
    /// Invoked when configuration loading fails and the source is marked as optional.
    /// </summary>
    /// <value>
    /// An action that handles configuration loading exceptions, or null for default handling.
    /// </value>
    /// <remarks>
    /// <para>
    /// <strong>Error Handling Strategies:</strong>
    /// <list type="bullet">
    /// <item><strong>Logging:</strong> Record failures for operational monitoring</item>
    /// <item><strong>Metrics:</strong> Track failure rates and patterns</item>
    /// <item><strong>Alerting:</strong> Notify operations teams of critical failures</item>
    /// <item><strong>Fallback:</strong> Implement retry logic or fallback configurations</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Production Monitoring:</strong>
    /// Implement comprehensive error handling to ensure configuration loading failures
    /// are visible to operations teams and can be quickly diagnosed and resolved.
    /// </para>
    /// </remarks>
    public Action<AppConfigurationProviderException>? OnLoadException { get; [UsedImplicitly] set; }

    /// <summary>
    /// Gets or sets a pre-configured Azure App Configuration client for testing scenarios.
    /// When provided, this client will be used instead of creating a new one from the
    /// App Configuration URI and credentials.
    /// </summary>
    /// <value>
    /// A configured ConfigurationClient instance, or null to create a new client using the
    /// App Configuration URI and credentials.
    /// Default is null.
    /// </value>
    /// <remarks>
    /// <para>
    /// <strong>Testing Use Cases:</strong>
    /// <list type="bullet">
    /// <item>Integration testing with Azure App Configuration Emulator</item>
    /// <item>Unit testing with mock SecretClient implementations</item>
    /// <item>Development environments using alternative App Configuration endpoints</item>
    /// <item>Testing scenarios requiring specific client configurations</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Production Considerations:</strong>
    /// This property is primarily intended for testing scenarios. In production,
    /// it's recommended to use the ConnectionString and Credential properties to let the
    /// provider create the SecretClient automatically.
    /// </para>
    /// </remarks>
    public ConfigurationClient? ConfigurationClient { get; [UsedImplicitly] set; }

    /// <summary>
    /// Gets or sets a value indicating whether JSON configuration settings should be automatically flattened.
    /// When enabled, configuration settings values containing valid JSON will be
    /// processed into hierarchical configuration keys.
    /// </summary>
    /// <value>
    /// True to enable JSON processing; false to treat all configuration settings  as simple strings.
    /// Default is false.
    /// </value>
    /// <remarks>
    /// <para>
    /// <strong>JSON Processing Benefits:</strong>
    /// <list type="bullet">
    /// <item>Enables complex configuration structures in single configuration setting</item>
    /// <item>Supports strongly typed configuration binding</item>
    /// <item>Reduces the total number of configuration settings needed</item>
    /// <item>Maintains hierarchical relationships in configuration</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Performance Impact:</strong>
    /// JSON processing adds parsing overhead during configuration loading.
    /// </para>
    /// </remarks>
    public bool JsonProcessor { get; [UsedImplicitly] set; }
}
