// <copyright file="AwsSecretsManagerConfigurationSource.cs" company="FlexKit">
// Copyright (c) FlexKit. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

using Amazon.Extensions.NETCore.Setup;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;

namespace FlexKit.Configuration.Providers.Aws.Sources;

/// <summary>
/// Configuration source that represents AWS Secrets Manager in the configuration system.
/// Implements IConfigurationSource to integrate Secrets Manager support with the standard .NET configuration
/// infrastructure, enabling secret data to be used alongside other configuration sources.
/// </summary>
/// <remarks>
/// This class serves as the factory and metadata container for AWS Secrets Manager configuration providers.
/// It follows the standard .NET configuration pattern where sources are responsible for creating their
/// corresponding providers and defining configuration parameters.
///
/// <para>
/// <strong>Role in Configuration Pipeline:</strong>
/// <list type="number">
/// <item>Defines the secret names and loading options</item>
/// <item>Integrates with ConfigurationBuilder through IConfigurationSource</item>
/// <item>Creates AwsSecretsManagerConfigurationProvider instances when requested</item>
/// <item>Provides metadata about the Secrets Manager source to the configuration system</item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Integration with FlexKit:</strong>
/// This source is designed to work seamlessly with FlexConfigurationBuilder and other FlexKit
/// configuration components, providing AWS Secrets Manager support as a first-class configuration
/// source option that maintains all FlexKit capabilities including dynamic access and type conversion.
/// </para>
///
/// <para>
/// <strong>Typical Usage Pattern:</strong>
/// This class is typically not instantiated directly by application code but rather through
/// the FlexConfigurationBuilder.AddAwsSecretsManager() extension method, which provides a more
/// convenient and fluent API.
/// </para>
///
/// <para>
/// <strong>Configuration Source Lifecycle:</strong>
/// <list type="number">
/// <item>Source is created with specified secret names and options</item>
/// <item>Source is added to a ConfigurationBuilder</item>
/// <item>When configuration is built, the Build() method is called to create provider</item>
/// <item>Provider loads Secrets Manager data and makes it available to the configuration system</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Direct usage (typically not recommended)
/// var source = new AwsSecretsManagerConfigurationSource
/// {
///     SecretNames = new[] { "myapp-database", "myapp-api-keys" },
///     Optional = true,
///     JsonProcessor = true,
///     ReloadAfter = TimeSpan.FromMinutes(15)
/// };
/// var configBuilder = new ConfigurationBuilder();
/// configBuilder.Add(source);
///
/// // Preferred usage through FlexConfigurationBuilder
/// var flexConfig = new FlexConfigurationBuilder()
///     .AddAwsSecretsManager(options =>
///     {
///         options.SecretNames = new[] { "myapp-database", "myapp-api-keys" };
///         options.Optional = true;
///         options.JsonProcessor = true;
///         options.ReloadAfter = TimeSpan.FromMinutes(15);
///     })
///     .Build();
/// </code>
/// </example>
public class AwsSecretsManagerConfigurationSource : IConfigurationSource
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
    /// <list type="table">
    /// <listheader>
    /// <term>Format</term>
    /// <description>Description</description>
    /// </listheader>
    /// <item>
    /// <term>myapp-database</term>
    /// <description>Load a specific secret by name</description>
    /// </item>
    /// <item>
    /// <term>arn:aws:secretsmanager:us-east-1:123456789012:secret:myapp-database-AbCdEf</term>
    /// <description>Load a specific secret by full ARN</description>
    /// </item>
    /// <item>
    /// <term>myapp/*</term>
    /// <description>Load all secrets with names starting with "myapp"</description>
    /// </item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Naming Conventions:</strong>
    /// <list type="bullet">
    /// <item>Use consistent prefixes for related secrets (e.g., "myapp-", "prod-myapp-")</item>
    /// <item>Group secrets by function (e.g., "myapp-database", "myapp-cache", "myapp-api")</item>
    /// <item>Include environment in names for environment-specific secrets</item>
    /// <item>Avoid special characters that might cause configuration key issues</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Pattern Loading:</strong>
    /// When using patterns (names ending with "*"), the provider uses the ListSecrets API
    /// to discover all secrets matching the pattern prefix. This is useful for loading
    /// groups of related secrets without hardcoding individual names.
    /// </para>
    /// </remarks>
    public string[]? SecretNames { get; [UsedImplicitly] set; }

    /// <summary>
    /// Gets or sets a value indicating whether the Secrets Manager source is optional.
    /// When true, failures to load secrets (due to missing secrets, permissions, etc.)
    /// will not cause the configuration building process to fail.
    /// </summary>
    /// <value>
    /// True if the Secrets Manager source is optional and loading failures should be ignored;
    /// false if loading failures should cause the configuration building to fail.
    /// Default is true.
    /// </value>
    /// <remarks>
    /// <para>
    /// <strong>Optional vs. Required Sources:</strong>
    /// <list type="bullet">
    /// <item>
    /// <strong>Optional (true):</strong> Loading failures are logged but don't prevent application startup
    /// </item>
    /// <item>
    /// <strong>Required (false):</strong> Loading failures cause exceptions and prevent application startup
    /// </item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Use Cases for Optional Sources:</strong>
    /// <list type="bullet">
    /// <item>Feature flags or optional configuration that has sensible defaults</item>
    /// <item>Development scenarios where Secrets Manager may not be fully configured</item>
    /// <item>Secrets that may not exist in all environments</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Use Cases for Required Sources:</strong>
    /// <list type="bullet">
    /// <item>Critical secrets like database passwords or API keys</item>
    /// <item>Production environments where missing secrets indicate deployment problems</item>
    /// <item>Security-critical configuration that must be externally managed</item>
    /// </list>
    /// </para>
    /// </remarks>
    public bool Optional { get; [UsedImplicitly] set; } = true;

    /// <summary>
    /// Gets or sets the specific version stage to retrieve from Secrets Manager.
    /// Supports Secrets Manager's versioning system for secret rotation and rollback scenarios.
    /// </summary>
    /// <value>
    /// The version stage to retrieve (e.g., "AWSCURRENT", "AWSPENDING", "AWSPREVIOUS", or custom stage).
    /// The default is "AWSCURRENT" which retrieves the current active version.
    /// </value>
    /// <remarks>
    /// <para>
    /// <strong>Standard Version Stages:</strong>
    /// <list type="table">
    /// <listheader>
    /// <term>Stage</term>
    /// <description>Description</description>
    /// </listheader>
    /// <item>
    /// <term>AWSCURRENT</term>
    /// <description>The current active version (default)</description>
    /// </item>
    /// <item>
    /// <term>AWSPENDING</term>
    /// <description>New version during a rotation process</description>
    /// </item>
    /// <item>
    /// <term>AWSPREVIOUS</term>
    /// <description>Previous version, useful for rollback scenarios</description>
    /// </item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Rotation Integration:</strong>
    /// AWS Secrets Manager automatically manages version stages during rotation.
    /// Using AWSCURRENT ensures your application always gets the latest rotated
    /// secret without code changes. Only specify other stages for specific use cases
    /// like testing new secrets or implementing rollback functionality.
    /// </para>
    ///
    /// <para>
    /// <strong>Custom Stages:</strong>
    /// You can create custom version stages for advanced deployment scenarios,
    /// such as "TESTING", "STAGING", or "CANARY" for gradual secret rollouts.
    /// </para>
    /// </remarks>
    public string? VersionStage { get; [UsedImplicitly] set; }

    /// <summary>
    /// Gets or sets the automatic reload interval for Secrets Manager data.
    /// When set, the provider will periodically refresh secrets from Secrets Manager
    /// to pick up rotated values or configuration changes without requiring application restart.
    /// </summary>
    /// <value>
    /// The interval at which to reload secrets or null to disable automatic reloading.
    /// Default is null (no automatic reloading).
    /// </value>
    /// <remarks>
    /// <para>
    /// <strong>Automatic Reloading Benefits:</strong>
    /// <list type="bullet">
    /// <item>Applications automatically pick up rotated secrets</item>
    /// <item>Enables real-time secret updates without deployment</item>
    /// <item>Supports continuous security practices with regular rotation</item>
    /// <item>Allows for emergency secret rotation and immediate propagation</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Performance and Cost Considerations:</strong>
    /// <list type="bullet">
    /// <item>Each reload makes API calls to AWS Secrets Manager</item>
    /// <item>AWS charges per API call, so frequent reloads increase costs</item>
    /// <item>Consider secret rotation frequency when setting a reload interval</item>
    /// <item>Use longer intervals for rarely changing secrets</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Recommended Intervals:</strong>
    /// <list type="table">
    /// <listheader>
    /// <term>Secret Type</term>
    /// <description>Recommended Interval</description>
    /// </listheader>
    /// <item>
    /// <term>Database passwords (auto-rotated)</term>
    /// <description>5-15 minutes</description>
    /// </item>
    /// <item>
    /// <term>API keys (manually rotated)</term>
    /// <description>30-60 minutes</description>
    /// </item>
    /// <item>
    /// <term>Certificates</term>
    /// <description>4-24 hours</description>
    /// </item>
    /// <item>
    /// <term>Development/testing</term>
    /// <description>1-5 minutes</description>
    /// </item>
    /// </list>
    /// </para>
    /// </remarks>
    public TimeSpan? ReloadAfter { get; [UsedImplicitly] set; }

    /// <summary>
    /// Gets or sets the AWS configuration options for Secrets Manager access.
    /// Provides control over AWS credentials, region, and other AWS SDK settings.
    /// </summary>
    /// <value>
    /// The AWS options to use it for Secrets Manager access, or null to use the default
    /// AWS credential resolution chain and configuration.
    /// </value>
    /// <remarks>
    /// <para>
    /// <strong>Default Behavior:</strong>
    /// When null, the provider uses the standard AWS credential resolution chain:
    /// <list type="number">
    /// <item>Environment variables (AWS_ACCESS_KEY_ID, AWS_SECRET_ACCESS_KEY)</item>
    /// <item>AWS credentials file (~/.aws/credentials)</item>
    /// <item>IAM instance profile (when running on EC2)</item>
    /// <item>IAM role for ECS tasks (when running on ECS)</item>
    /// <item>IAM role for Lambda functions (when running on Lambda)</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Cross-Region Access:</strong>
    /// Secrets Manager secrets are region-specific. Ensure the configured region
    /// matches where your secrets are stored or use secret replication for
    /// multi-region applications.
    /// </para>
    ///
    /// <para>
    /// <strong>Custom Configuration Examples:</strong>
    /// <code>
    /// // Specify a specific AWS region
    /// AwsOptions = new AWSOptions
    /// {
    ///     Region = RegionEndpoint.USWest2
    /// };
    ///
    /// // Use a specific AWS profile for different environments
    /// AwsOptions = new AWSOptions
    /// {
    ///     Profile = "production",
    ///     Region = RegionEndpoint.USEast1
    /// };
    /// </code>
    /// </para>
    /// </remarks>
    public AWSOptions? AwsOptions { get; [UsedImplicitly] set; }

    /// <summary>
    /// Gets or sets a value indicating whether SecretString values containing JSON
    /// should be automatically flattened into the configuration hierarchy.
    /// </summary>
    /// <value>
    /// True to enable JSON processing and flattening; false to treat all secrets as simple strings.
    /// Default is false.
    /// </value>
    /// <remarks>
    /// <para>
    /// <strong>JSON Processing Benefits:</strong>
    /// <list type="bullet">
    /// <item>Enables storing complex configuration structures in a single secret</item>
    /// <item>Supports database credential objects with multiple fields</item>
    /// <item>Maintains hierarchical relationships for strongly typed binding</item>
    /// <item>Reduces the number of individual secrets needed</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Database Credentials Example:</strong>
    /// <code>
    /// // Secret: myapp-database
    /// // Value: {"host": "db.example.com", "port": 5432, "username": "app", "password": "secret"}
    ///
    /// // Results in these configuration keys:
    /// // myapp-database:host = "db.example.com"
    /// // myapp-database:port = "5432"
    /// // myapp-database:username = "app"
    /// // myapp-database:password = "secret"
    /// </code>
    /// </para>
    ///
    /// <para>
    /// <strong>When to Enable JSON Processing:</strong>
    /// <list type="bullet">
    /// <item>When using AWS RDS/Aurora managed secret rotation (requires JSON format)</item>
    /// <item>When storing complex configuration objects</item>
    /// <item>When migrating from other configuration sources that use JSON</item>
    /// <item>When you want to use strongly typed configuration binding</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>When to Disable JSON Processing:</strong>
    /// <list type="bullet">
    /// <item>When secrets contain JSON strings that should remain as literals</item>
    /// <item>When all secrets are simple string values</item>
    /// <item>When you need maximum control over JSON processing</item>
    /// <item>When performance is critical, and you want to avoid JSON parsing</item>
    /// </list>
    /// </para>
    /// </remarks>
    public bool JsonProcessor { get; [UsedImplicitly] set; }

    /// <summary>
    /// Gets or sets the specific secret names that should have JSON processing applied.
    /// When specified, only secrets matching these names will be processed as JSON,
    /// even when JsonProcessor is enabled.
    /// </summary>
    /// <value>
    /// An array of secret names or patterns that should be processed as JSON
    /// or null to apply JSON processing to all secrets when JsonProcessor is enabled.
    /// </value>
    /// <remarks>
    /// <para>
    /// <strong>Selective JSON Processing:</strong>
    /// This property allows fine-grained control over which secrets are processed as JSON.
    /// This is useful when you have a mix of simple string secrets and complex JSON secrets
    /// and want to avoid accidentally processing string values as JSON.
    /// </para>
    ///
    /// <para>
    /// <strong>Pattern Matching Rules:</strong>
    /// <list type="bullet">
    /// <item>Names are matched using case-insensitive comparison</item>
    /// <item>Supports wildcard patterns using "*" suffix</item>
    /// <item>A secret matches if its name equals or starts with a specified pattern</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Example Usage:</strong>
    /// <code>
    /// JsonProcessorSecrets = new[]
    /// {
    ///     "myapp-database",     // Process this specific secret as JSON
    ///     "myapp-cache",        // Process this specific secret as JSON
    ///     "myapp-apis-*"        // Process all secrets starting with "myapp-apis-" as JSON
    /// };
    ///
    /// // Secrets processed as JSON:
    /// // myapp-database (JSON database credentials)
    /// // myapp-cache (JSON cache configuration)
    /// // myapp-apis-external (JSON API configurations)
    ///
    /// // Secrets processed as strings:
    /// // myapp-license-key (simple string)
    /// // myapp-encryption-key (simple string)
    /// </code>
    /// </para>
    /// </remarks>
    public string[]? JsonProcessorSecrets { get; [UsedImplicitly] set; }

    /// <summary>
    /// Gets or sets a custom secret processor for transforming secret names and values.
    /// Allows for custom logic to modify how Secrets Manager secret names are converted
    /// to configuration keys.
    /// </summary>
    /// <value>
    /// A custom secret processor implementation, or null to use the default transformation logic.
    /// </value>
    /// <remarks>
    /// <para>
    /// <strong>Default Transformation:</strong>
    /// When no custom processor is specified, the default behavior:
    /// <list type="number">
    /// <item>Converts hyphens (-) to colons (:) for hierarchical keys</item>
    /// <item>Preserves the original secret name structure</item>
    /// <item>Ensures compatibility with the .NET configuration key format</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Custom Processing Use Cases:</strong>
    /// <list type="bullet">
    /// <item>Adding environment prefixes to all configuration keys</item>
    /// <item>Implementing organization-specific naming conventions</item>
    /// <item>Filtering or transforming secret names based on runtime conditions</item>
    /// <item>Mapping legacy secret names to new configuration structures</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Example Custom Processor:</strong>
    /// <code>
    /// public class EnvironmentSecretProcessor : ISecretProcessor
    /// {
    ///     private readonly string _environment;
    ///
    ///     public EnvironmentSecretProcessor(string environment)
    ///     {
    ///         _environment = environment;
    ///     }
    ///
    ///     public string ProcessSecretName(string configKey, string originalSecretName)
    ///     {
    ///         // Add environment prefix to all configuration keys
    ///         return $"{_environment}:{configKey}";
    ///     }
    /// }
    ///
    /// // Usage:
    /// SecretProcessor = new EnvironmentSecretProcessor("production");
    /// </code>
    /// </para>
    /// </remarks>
    public ISecretProcessor? SecretProcessor { get; [UsedImplicitly] set; }

    /// <summary>
    /// Gets or sets the error handling callback invoked when configuration loading fails,
    /// and the source is marked as optional.
    /// </summary>
    /// <value>
    /// An action that receives a SecretsManagerConfigurationProviderException when loading fails,
    /// or null if no custom error handling is needed.
    /// </value>
    /// <remarks>
    /// <para>
    /// <strong>Error Handling Scenarios:</strong>
    /// This callback is only invoked when:
    /// <list type="bullet">
    /// <item>The source is marked as Optional = true</item>
    /// <item>An error occurs during secret loading (AWS API errors, permissions, etc.)</item>
    /// <item>The error would normally cause configuration loading to fail</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Common Error Handling Patterns:</strong>
    /// <code>
    /// // Logging errors for monitoring and debugging
    /// OnLoadException = exception =>
    /// {
    ///     logger.LogWarning(exception.InnerException,
    ///         "Failed to load optional Secrets Manager configuration: {Secrets}",
    ///         string.Join(",", SecretNames ?? Array.Empty&lt;string&gt;()));
    /// };
    ///
    /// // Metrics collection for operational monitoring
    /// OnLoadException = exception =>
    /// {
    ///     metrics.Increment("config.secretsmanager.load.failures",
    ///         new[] { ("secrets", string.Join(",", SecretNames ?? Array.Empty&lt;string&gt;())) });
    /// };
    ///
    /// // Alert for critical secret loading failures
    /// OnLoadException = exception =>
    /// {
    ///     if (SecretNames?.Any(s => s.Contains("database")) == true)
    ///     {
    ///         alertService.SendCriticalAlert("Database secret loading failed", exception.Message);
    ///     }
    /// };
    /// </code>
    /// </para>
    /// </remarks>
    public Action<SecretsManagerConfigurationProviderException>? OnLoadException { get; [UsedImplicitly] set; }

    /// <summary>
    /// Creates a new AWS Secrets Manager configuration provider that will load data from this source.
    /// This method is called by the .NET configuration system when building the configuration
    /// from all registered sources.
    /// </summary>
    /// <param name="builder">
    /// The configuration builder that is requesting the provider.
    /// This parameter is required by the IConfigurationSource interface but is not used by this implementation.
    /// </param>
    /// <returns>
    /// A new <see cref="AwsSecretsManagerConfigurationProvider"/> instance configured to load
    /// secrets from AWS Secrets Manager according to this source's properties.
    /// </returns>
    /// <remarks>
    /// This method implements the factory pattern used by the .NET configuration system.
    /// Each call to Build() creates a new provider instance, allowing the configuration
    /// system to manage the provider lifecycle and reloading as needed.
    ///
    /// <para>
    /// <strong>Provider Creation:</strong>
    /// The returned provider is initialized with this source instance, giving it access
    /// to all configuration properties (SecretNames, Optional, AWS options, etc.) needed to load secrets.
    /// </para>
    ///
    /// <para>
    /// <strong>Lifecycle Management:</strong>
    /// <list type="bullet">
    /// <item>Called once per configuration building operation</item>
    /// <item>Provider instances are managed by the configuration system</item>
    /// <item>Each provider is responsible for loading its specified secrets</item>
    /// <item>Providers are disposed when the configuration is disposed</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Integration Pattern:</strong>
    /// This method follows the standard .NET configuration source pattern where:
    /// <list type="number">
    /// <item>Sources define what configuration to load (metadata)</item>
    /// <item>Providers actually load the configuration data</item>
    /// <item>Build() method bridges between sources and providers</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
    /// <example>
    /// <code>
    /// // This method is typically called automatically by the configuration system:
    /// var configBuilder = new ConfigurationBuilder();
    /// var source = new AwsSecretsManagerConfigurationSource
    /// {
    ///     SecretNames = new[] { "myapp-database" },
    ///     Optional = true
    /// };
    /// configBuilder.Add(source);
    ///
    /// // When Build() is called on the configuration builder:
    /// var configuration = configBuilder.Build(); // This triggers source.Build(configBuilder)
    ///
    /// // Manual provider creation (for testing or custom scenarios):
    /// var provider = source.Build(configBuilder);
    /// provider.Load(); // Manually load Secrets Manager data
    /// </code>
    /// </example>
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return new AwsSecretsManagerConfigurationProvider(this);
    }
}

/// <summary>
/// Interface for custom secret name processing logic.
/// Implementations can transform Secrets Manager secret names into configuration keys
/// according to application-specific requirements.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Purpose:</strong>
/// This interface allows applications to customize how AWS Secrets Manager secret names
/// are converted to .NET configuration keys. This is useful for organizations with specific
/// naming conventions or requirements for secret name transformation.
/// </para>
///
/// <para>
/// <strong>Implementation Guidelines:</strong>
/// <list type="bullet">
/// <item>Implementations should be stateless and thread-safe</item>
/// <item>Secret name transformations should be deterministic and reversible where possible</item>
/// <item>Consider caching expensive transformations if they involve complex logic</item>
/// <item>Validate that transformed keys are valid .NET configuration key formats</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class PrefixSecretProcessor : ISecretProcessor
/// {
///     private readonly string _prefix;
///
///     public PrefixSecretProcessor(string prefix)
///     {
///         _prefix = prefix;
///     }
///
///     public string ProcessSecretName(string configKey, string originalSecretName)
///     {
///         // Add application prefix to all configuration keys
///         return $"{_prefix}:{configKey}";
///     }
/// }
///
/// // Usage:
/// var source = new AwsSecretsManagerConfigurationSource
/// {
///     SecretNames = new[] { "database", "cache" },
///     SecretProcessor = new PrefixSecretProcessor("myapp")
/// };
/// // Results in: "myapp:database", "myapp:cache"
/// </code>
/// </example>
public interface ISecretProcessor
{
    /// <summary>
    /// Processes a secret name to transform it into a configuration key.
    /// This method is called for each secret retrieved from Secrets Manager
    /// after the default name transformation has been applied.
    /// </summary>
    /// <param name="configKey">
    /// The configuration key after default processing (hyphens converted to colons).
    /// </param>
    /// <param name="originalSecretName">
    /// The original Secrets Manager secret name before any processing.
    /// </param>
    /// <returns>
    /// The final configuration key to use for this secret in the .NET configuration system.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <strong>Processing Order:</strong>
    /// This method is called after the default secret name transformation:
    /// <list type="number">
    /// <item>Hyphens are converted to colons for hierarchical keys</item>
    /// <item>This method is called with the transformed key</item>
    /// <item>The result becomes the final configuration key</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Return Value Requirements:</strong>
    /// <list type="bullet">
    /// <item>Must return a valid .NET configuration key (non-null, non-empty)</item>
    /// <item>Should use colons (:) as hierarchy separators</item>
    /// <item>Should avoid characters that might cause configuration binding issues</item>
    /// <item>Should be consistent and deterministic for the same input</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when the transformation results in an invalid configuration key.</exception>
    string ProcessSecretName(
        string configKey,
        string originalSecretName);
}
