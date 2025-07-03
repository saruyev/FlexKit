using Amazon.Extensions.NETCore.Setup;
using FlexKit.Configuration.Providers.Aws.Sources;
using JetBrains.Annotations;

namespace FlexKit.Configuration.Providers.Aws.Options;

/// <summary>
/// Configuration options for AWS Secrets Manager integration with FlexKit configuration.
/// Provides a strongly typed way to configure all aspects of Secrets Manager access,
/// including AWS settings, processing options, and error handling.
/// </summary>
/// <remarks>
/// This class serves as a data transfer object for Secrets Manager configuration options,
/// providing a clean API surface for the configuration lambda while maintaining type safety
/// and IntelliSense support.
///
/// <para>
/// <strong>Configuration Categories:</strong>
/// <list type="bullet">
/// <item><strong>AWS Settings:</strong> Credentials, region, and SDK configuration</item>
/// <item><strong>Secret Selection:</strong> Names, patterns, and version control</item>
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
/// <item>VersionStage = null (uses AWSCURRENT)</item>
/// <item>ReloadAfter = null (no automatic reloading)</item>
/// <item>AwsOptions = null (use default credential chain)</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Typical production configuration
/// var options = new AwsSecretsManagerOptions
/// {
///     SecretNames = new[] { "prod-myapp-database", "prod-myapp-api" },
///     Optional = false,
///     JsonProcessor = true,
///     JsonProcessorSecrets = new[] { "prod-myapp-database" },
///     ReloadAfter = TimeSpan.FromMinutes(15),
///     VersionStage = "AWSCURRENT",
///     AwsOptions = new AWSOptions { Region = RegionEndpoint.USEast1 },
///     OnLoadException = ex => logger.LogError(ex, "Secrets Manager error")
/// };
/// </code>
/// </example>
public class AwsSecretsManagerOptions
{
    /// <summary>
    /// Gets or sets the array of secret names or ARNs to load from Secrets Manager.
    /// This defines which secrets should be retrieved and made available in the configuration.
    /// </summary>
    /// <value>
    /// An array of secret names, ARNs, or patterns (e.g., "myapp-database", "myapp/*").
    /// Can include individual secret names or patterns using wildcards for bulk loading.
    /// </value>
    /// <remarks>
    /// <para>
    /// <strong>Secret Name Formats:</strong>
    /// <list type="bullet">
    /// <item><strong>Individual Names:</strong> "myapp-database", "api-keys-external"</item>
    /// <item><strong>Full ARNs:</strong> "arn:aws:secretsmanager:region:account:secret:name"</item>
    /// <item><strong>Patterns:</strong> "myapp/*" loads all secrets starting with "myapp"</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Naming Best Practices:</strong>
    /// <list type="bullet">
    /// <item>Use consistent prefixes for environment separation (prod-, dev-, staging-)</item>
    /// <item>Group related secrets with common prefixes (myapp-database, myapp-cache)</item>
    /// <item>Use descriptive names that indicate the secret's purpose</item>
    /// <item>Avoid special characters that might complicate configuration key generation</item>
    /// </list>
    /// </para>
    /// </remarks>
    public string[]? SecretNames { get; [UsedImplicitly] set; }

    /// <summary>
    /// Gets or sets a value indicating whether the Secrets Manager source is optional.
    /// When true, failures to load secrets will not cause configuration building to fail.
    /// </summary>
    /// <value>
    /// True if the Secrets Manager source is optional; false if it's required.
    /// Default is true.
    /// </value>
    /// <remarks>
    /// <para>
    /// <strong>Production Recommendations:</strong>
    /// <list type="bullet">
    /// <item><strong>Critical Secrets:</strong> Set to false for essential secrets like database passwords</item>
    /// <item><strong>Optional Features:</strong> Set to true for feature flags that have reasonable defaults</item>
    /// <item><strong>Environment-Specific:</strong> Set to true for dev/test, false for production</item>
    /// </list>
    /// </para>
    /// </remarks>
    public bool Optional { get; [UsedImplicitly] set; } = true;

    /// <summary>
    /// Gets or sets the specific version stage to retrieve from Secrets Manager.
    /// Supports version control for secret rotation and rollback scenarios.
    /// </summary>
    /// <value>
    /// The version stage to retrieve (e.g., "AWSCURRENT", "AWSPENDING", "AWSPREVIOUS").
    /// Default is null (uses AWSCURRENT).
    /// </value>
    /// <remarks>
    /// <para>
    /// <strong>Common Version Stages:</strong>
    /// <list type="bullet">
    /// <item><strong>AWSCURRENT:</strong> Current active version (default)</item>
    /// <item><strong>AWSPENDING:</strong> New version during rotation</item>
    /// <item><strong>AWSPREVIOUS:</strong> Previous version for rollback</item>
    /// <item><strong>Custom stages:</strong> User-defined stages for advanced scenarios</item>
    /// </list>
    /// </para>
    /// </remarks>
    public string? VersionStage { get; [UsedImplicitly] set; }

    /// <summary>
    /// Gets or sets the automatic reload interval for Secrets Manager data.
    /// When set, the provider will periodically refresh secrets from Secrets Manager.
    /// </summary>
    /// <value>
    /// The interval at which to reload secrets or null to disable automatic reloading.
    /// Default is null.
    /// </value>
    /// <remarks>
    /// <para>
    /// <strong>Recommended Values:</strong>
    /// <list type="bullet">
    /// <item><strong>Database secrets (auto-rotated):</strong> 5-15 minutes</item>
    /// <item><strong>API keys (manually rotated):</strong> 30-60 minutes</item>
    /// <item><strong>Certificates:</strong> 4-24 hours</item>
    /// <item><strong>Development:</strong> 1-5 minutes for rapid testing</item>
    /// </list>
    /// </para>
    /// </remarks>
    public TimeSpan? ReloadAfter { get; [UsedImplicitly] set; }

    /// <summary>
    /// Gets or sets the AWS configuration options for Secrets Manager access.
    /// Provides control over AWS credentials, region, and other SDK settings.
    /// </summary>
    /// <value>
    /// The AWS options to use it for Secrets Manager access, or null to use defaults.
    /// Default is null (uses the AWS credential resolution chain).
    /// </value>
    /// <remarks>
    /// <para>
    /// <strong>Security Best Practices:</strong>
    /// <list type="bullet">
    /// <item>Use IAM roles instead of hardcoded credentials</item>
    /// <item>Apply least-privilege permissions for Secrets Manager access</item>
    /// <item>Use different AWS profiles for different environments</item>
    /// <item>Ensure secrets and applications are in the same AWS region</item>
    /// </list>
    /// </para>
    /// </remarks>
    public AWSOptions? AwsOptions { get; [UsedImplicitly] set; }

    /// <summary>
    /// Gets or sets a value indicating whether JSON secrets should be automatically flattened.
    /// When enabled, SecretString values containing valid JSON will be processed into
    /// hierarchical configuration keys.
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
    /// <item>Supports AWS RDS/Aurora managed rotation (requires JSON format)</item>
    /// <item>Allows strongly typed configuration binding</item>
    /// <item>Maintains hierarchical relationships in configuration</item>
    /// </list>
    /// </para>
    /// </remarks>
    public bool JsonProcessor { get; [UsedImplicitly] set; }

    /// <summary>
    /// Gets or sets the specific secret names that should have JSON processing applied.
    /// When specified, only secrets matching these names will be processed as JSON.
    /// </summary>
    /// <value>
    /// An array of secret names or patterns for selective JSON processing,
    /// or null to apply to all secrets when JsonProcessor is enabled.
    /// </value>
    /// <remarks>
    /// <para>
    /// <strong>Selective Processing Use Cases:</strong>
    /// <list type="bullet">
    /// <item>Mixed secret types (some JSON database configs, some simple API keys)</item>
    /// <item>Performance optimization for large numbers of secrets</item>
    /// <item>Avoiding accidental JSON parsing of string values</item>
    /// <item>Gradual migration from simple-to-complex secret structures</item>
    /// </list>
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
    public ISecretProcessor? SecretProcessor { get; [UsedImplicitly] set; }

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
    /// </remarks>
    public Action<SecretsManagerConfigurationProviderException>? OnLoadException { get; [UsedImplicitly] set; }
}
