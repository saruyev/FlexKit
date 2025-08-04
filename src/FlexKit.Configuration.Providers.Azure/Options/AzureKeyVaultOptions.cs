using Azure.Core;
using FlexKit.Configuration.Providers.Azure.Sources;
using JetBrains.Annotations;

namespace FlexKit.Configuration.Providers.Azure.Options;

/// <summary>
/// Configuration options for Azure Key Vault integration with FlexKit configuration.
/// Provides a strongly typed way to configure all aspects of Key Vault access,
/// including Azure settings, processing options, and error handling.
/// </summary>
/// <remarks>
/// This class serves as a data transfer object for Key Vault configuration options,
/// providing a clean API surface for the configuration lambda while maintaining type safety
/// and IntelliSense support.
///
/// <para>
/// <strong>Configuration Categories:</strong>
/// <list type="bullet">
/// <item><strong>Azure Settings:</strong> Credentials, vault URI, and client configuration</item>
/// <item><strong>Secret Selection:</strong> Filtering and name-based selection</item>
/// <item><strong>Processing Options:</strong> JSON flattening and custom transformations</item>
/// <item><strong>Operational Settings:</strong> Reloading, error handling, and monitoring</item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Default Values:</strong>
/// The class provides sensible defaults for most scenarios:
/// <list type="bullet">
/// <item>Optional = true (non-blocking failures)</item>
/// <item>JsonProcessor = false (simple string processing)</item>
/// <item>ReloadAfter = null (no automatic reloading)</item>
/// <item>Credential = null (use default credential chain)</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Typical production configuration
/// var options = new AzureKeyVaultOptions
/// {
///     VaultUri = "https://prod-vault.vault.azure.net/",
///     Optional = false,
///     JsonProcessor = true,
///     JsonProcessorSecrets = new[] { "database-config", "cache-config" },
///     ReloadAfter = TimeSpan.FromMinutes(15),
///     Credential = new DefaultAzureCredential(),
///     OnLoadException = ex => logger.LogError(ex, "Key Vault error")
/// };
/// </code>
/// </example>
public class AzureKeyVaultOptions
{
    /// <summary>
    /// Gets or sets the Key Vault URI to load secrets from.
    /// This URI defines the specific Key Vault instance from which to retrieve secrets.
    /// </summary>
    /// <value>
    /// The Key Vault URI (e.g., "https://myapp-vault.vault.azure.net/").
    /// Must be a valid HTTPS URI pointing to an Azure Key Vault.
    /// </value>
    /// <remarks>
    /// <para>
    /// <strong>URI Format:</strong>
    /// Key Vault URIs follow the format: https://{vault-name}.vault.azure.net/
    /// The vault name must be globally unique across Azure.
    /// </para>
    ///
    /// <para>
    /// <strong>Best Practices:</strong>
    /// <list type="bullet">
    /// <item>Use environment-specific vault names: prod-myapp-vault, dev-myapp-vault</item>
    /// <item>Keep vault names short and descriptive</item>
    /// <item>Ensure the vault exists in the same region as your application for the best performance</item>
    /// <item>Use separate vaults for different environments to maintain isolation</item>
    /// </list>
    /// </para>
    /// </remarks>
    public string? VaultUri { get; [UsedImplicitly] set; }

    /// <summary>
    /// Gets or sets a value indicating whether the Key Vault source is optional.
    /// When true, failures to load secrets will not cause configuration building to fail.
    /// </summary>
    /// <value>
    /// True if the Key Vault source is optional; false if it's required.
    /// Default is true.
    /// </value>
    /// <remarks>
    /// <para>
    /// <strong>Production Recommendations:</strong>
    /// <list type="bullet">
    /// <item><strong>Critical Secrets:</strong> Set to false for essential secrets like database connections</item>
    /// <item><strong>Feature Flags:</strong> Set to true for optional features that have reasonable defaults</item>
    /// <item><strong>Environment-Specific:</strong> Set to true for dev/test, false for production</item>
    /// </list>
    /// </para>
    /// </remarks>
    public bool Optional { get; [UsedImplicitly] set; } = true;

    /// <summary>
    /// Gets or sets the automatic reload interval for Key Vault data.
    /// When set, the provider will periodically refresh secrets from Key Vault.
    /// </summary>
    /// <value>
    /// The interval at which to reload secrets or null to disable automatic reloading.
    /// Default is null.
    /// </value>
    /// <remarks>
    /// <para>
    /// <strong>Cost Considerations:</strong>
    /// Each reload operation makes API calls to Azure Key Vault. Consider the balance
    /// between configuration freshness and Azure API costs when setting this interval.
    /// </para>
    ///
    /// <para>
    /// <strong>Recommended Values:</strong>
    /// <list type="bullet">
    /// <item><strong>Development:</strong> 1-2 minutes for rapid iteration</item>
    /// <item><strong>Production:</strong> 10-30 minutes for operational stability</item>
    /// <item><strong>Critical Systems:</strong> 30+ minutes to minimize API overhead</item>
    /// </list>
    /// </para>
    /// </remarks>
    public TimeSpan? ReloadAfter { get; [UsedImplicitly] set; }

    /// <summary>
    /// Gets or sets the Azure credential for Key Vault access.
    /// Provides control over Azure authentication and credential management.
    /// </summary>
    /// <value>
    /// The Azure credential to use for Key Vault access, or null to use defaults.
    /// Default is null (uses the Azure credential resolution chain).
    /// </value>
    /// <remarks>
    /// <para>
    /// <strong>Security Best Practices:</strong>
    /// <list type="bullet">
    /// <item>Use Managed Identity instead of hardcoded credentials</item>
    /// <item>Apply least-privilege permissions for Key Vault access</item>
    /// <item>Use different credentials for different environments</item>
    /// <item>Avoid storing credentials in application configuration</item>
    /// </list>
    /// </para>
    /// </remarks>
    public TokenCredential? Credential { get; [UsedImplicitly] set; }

    /// <summary>
    /// Gets or sets a value indicating whether JSON secrets should be automatically flattened.
    /// When enabled, secret values containing valid JSON will be
    /// processed into hierarchical configuration keys.
    /// </summary>
    /// <value>
    /// True to enable JSON processing; false to treat all secrets as simple strings.
    /// Default is false.
    /// </value>
    /// <remarks>
    /// <para>
    /// <strong>JSON Processing Benefits:</strong>
    /// <list type="bullet">
    /// <item>Enables complex configuration structures in single secrets</item>
    /// <item>Supports strongly typed configuration binding</item>
    /// <item>Reduces the total number of secrets needed</item>
    /// <item>Maintains hierarchical relationships in configuration</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Performance Impact:</strong>
    /// JSON processing adds parsing overhead during configuration loading.
    /// For applications with many secrets or frequent reloading, consider
    /// using JsonProcessorSecrets to limit processing to specific secret groups.
    /// </para>
    /// </remarks>
    public bool JsonProcessor { get; [UsedImplicitly] set; }

    /// <summary>
    /// Gets or sets the specific secret names that should have JSON processing applied.
    /// When specified, only secrets matching these names will be processed as JSON.
    /// </summary>
    /// <value>
    /// An array of secret names for selective JSON processing,
    /// or null to apply to all secrets when JsonProcessor is enabled.
    /// </value>
    /// <remarks>
    /// <para>
    /// <strong>Selective Processing Use Cases:</strong>
    /// <list type="bullet">
    /// <item>Mixed secret types (some JSON, some simple strings)</item>
    /// <item>Performance optimization for large secret sets</item>
    /// <item>Avoiding accidental JSON parsing of string values</item>
    /// <item>Gradual migration from simple to complex secrets</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Name Format:</strong>
    /// Use Key Vault secret names directly. These will be matched exactly
    /// during processing.
    /// </para>
    /// </remarks>
    public string[]? JsonProcessorSecrets { get; [UsedImplicitly] set; }

    /// <summary>
    /// Gets or sets a custom secret processor for transforming secret names.
    /// Allows for application-specific secret name transformation logic.
    /// </summary>
    /// <value>
    /// A custom secret processor implementation, or null for default processing.
    /// </value>
    /// <remarks>
    /// <para>
    /// <strong>Custom Processing Scenarios:</strong>
    /// <list type="bullet">
    /// <item>Adding environment or application prefixes to all secrets</item>
    /// <item>Implementing organization-specific naming conventions</item>
    /// <item>Filtering or modifying secret names based on runtime conditions</item>
    /// <item>Mapping legacy secret names to new configuration structures</item>
    /// </list>
    /// </para>
    /// </remarks>
    public IKeyVaultSecretProcessor? SecretProcessor { get; [UsedImplicitly] set; }

    /// <summary>
    /// Gets or sets the error handling callback for optional configuration loading failures.
    /// Invoked when secret loading fails and the source is marked as optional.
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
    /// Implement comprehensive error handling to ensure secret loading failures
    /// are visible to operations teams and can be quickly diagnosed and resolved.
    /// </para>
    /// </remarks>
    public Action<KeyVaultConfigurationProviderException>? OnLoadException { get; [UsedImplicitly] set; }
}
