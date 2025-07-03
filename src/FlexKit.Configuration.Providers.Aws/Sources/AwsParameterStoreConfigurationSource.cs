// <copyright file="AwsParameterStoreConfigurationSource.cs" company="FlexKit">
// Copyright (c) FlexKit. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

using Amazon.Extensions.NETCore.Setup;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;

namespace FlexKit.Configuration.Providers.Aws.Sources;

/// <summary>
/// Configuration source that represents AWS Systems Manager Parameter Store in the configuration system.
/// Implements IConfigurationSource to integrate Parameter Store support with the standard .NET configuration
/// infrastructure, enabling parameter store data to be used alongside other configuration sources.
/// </summary>
/// <remarks>
/// This class serves as the factory and metadata container for AWS Parameter Store configuration providers.
/// It follows the standard .NET configuration pattern where sources are responsible for creating their
/// corresponding providers and defining configuration parameters.
///
/// <para>
/// <strong>Role in Configuration Pipeline:</strong>
/// <list type="number">
/// <item>Defines the Parameter Store path and loading options</item>
/// <item>Integrates with ConfigurationBuilder through IConfigurationSource</item>
/// <item>Creates AwsParameterStoreConfigurationProvider instances when requested</item>
/// <item>Provides metadata about the Parameter Store source to the configuration system</item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Integration with FlexKit:</strong>
/// This source is designed to work seamlessly with FlexConfigurationBuilder and other FlexKit
/// configuration components, providing AWS Parameter Store support as a first-class configuration
/// source option that maintains all FlexKit capabilities including dynamic access and type conversion.
/// </para>
///
/// <para>
/// <strong>Typical Usage Pattern:</strong>
/// This class is typically not instantiated directly by application code but rather through
/// the FlexConfigurationBuilder.AddAwsParameterStore() extension method, which provides a more
/// convenient and fluent API.
/// </para>
///
/// <para>
/// <strong>Configuration Source Lifecycle:</strong>
/// <list type="number">
/// <item>Source is created with specified path and options</item>
/// <item>Source is added to a ConfigurationBuilder</item>
/// <item>When configuration is built, the Build() method is called to create provider</item>
/// <item>Provider loads Parameter Store data and makes it available to the configuration system</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Direct usage (typically not recommended)
/// var source = new AwsParameterStoreConfigurationSource
/// {
///     Path = "/myapp/",
///     Optional = true,
///     JsonProcessor = true,
///     ReloadAfter = TimeSpan.FromMinutes(5)
/// };
/// var configBuilder = new ConfigurationBuilder();
/// configBuilder.Add(source);
///
/// // Preferred usage through FlexConfigurationBuilder
/// var flexConfig = new FlexConfigurationBuilder()
///     .AddAwsParameterStore(options =>
///     {
///         options.Path = "/myapp/";
///         options.Optional = true;
///         options.JsonProcessor = true;
///         options.ReloadAfter = TimeSpan.FromMinutes(5);
///     })
///     .Build();
/// </code>
/// </example>
public class AwsParameterStoreConfigurationSource : IConfigurationSource
{
    /// <summary>
    /// Gets or sets the Parameter Store path prefix to load parameters from.
    /// This path defines the root location in Parameter Store from which to retrieve configuration data.
    /// </summary>
    /// <value>
    /// The Parameter Store path prefix (e.g., "/myapp/", "/prod/database/").
    /// Must start with a forward slash and should typically end with a forward slash
    /// to ensure proper parameter hierarchy matching.
    /// </value>
    /// <remarks>
    /// <para>
    /// <strong>Path Format:</strong>
    /// Parameter Store paths use forward slashes (/) as hierarchical separators.
    /// The path should start with a forward slash and typically end with one to
    /// ensure that only parameters under the specified hierarchy are loaded.
    /// </para>
    ///
    /// <para>
    /// <strong>Examples:</strong>
    /// <list type="table">
    /// <listheader>
    /// <term>Path</term>
    /// <description>Description</description>
    /// </listheader>
    /// <item>
    /// <term>"/myapp/"</term>
    /// <description>Loads all parameters under the /myapp/ hierarchy</description>
    /// </item>
    /// <item>
    /// <term>"/prod/database/"</term>
    /// <description>Loads only database-related parameters for production</description>
    /// </item>
    /// <item>
    /// <term>"/shared/config/"</term>
    /// <description>Loads shared configuration parameters across applications</description>
    /// </item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Hierarchical Loading:</strong>
    /// When Recursive is true (default), all parameters under the specified path
    /// and its sub-paths are loaded. This enables loading entire configuration
    /// trees with a single path specification.
    /// </para>
    /// </remarks>
    public string Path { get; [UsedImplicitly] set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the Parameter Store source is optional.
    /// When true, failures to load parameters (due to missing paths, permissions, etc.)
    /// will not cause the configuration building process to fail.
    /// </summary>
    /// <value>
    /// True if the Parameter Store source is optional and loading failures should be ignored;
    /// false if loading failures should cause the configuration building to fail.
    /// Default is true.
    /// </value>
    /// <remarks>
    /// <para>
    /// <strong>Optional vs Required Sources:</strong>
    /// <list type="bullet">
    /// <item><strong>Optional (true):</strong> Loading failures are logged but don't prevent application startup</item>
    /// <item><strong>Required (false):</strong> Loading failures cause exceptions and prevent application startup</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Use Cases for Optional Sources:</strong>
    /// <list type="bullet">
    /// <item>Environment-specific parameters that may not exist in all environments</item>
    /// <item>Feature flags or optional configuration that has sensible defaults</item>
    /// <item>Development scenarios where Parameter Store may not be fully configured</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Use Cases for Required Sources:</strong>
    /// <list type="bullet">
    /// <item>Critical configuration like database connection strings or API keys</item>
    /// <item>Production environments where missing configuration indicates a deployment problem</item>
    /// <item>Configuration that has no reasonable defaults and must be externally provided</item>
    /// </list>
    /// </para>
    /// </remarks>
    public bool Optional { get; [UsedImplicitly] set; } = true;

    /// <summary>
    /// Gets or sets the automatic reload interval for Parameter Store data.
    /// When set, the provider will periodically refresh parameters from Parameter Store
    /// to pick up configuration changes without requiring application restart.
    /// </summary>
    /// <value>
    /// The interval at which to reload parameters, or null to disable automatic reloading.
    /// Default is null (no automatic reloading).
    /// </value>
    /// <remarks>
    /// <para>
    /// <strong>Automatic Reloading Benefits:</strong>
    /// <list type="bullet">
    /// <item>Applications can pick up configuration changes without restart</item>
    /// <item>Enables dynamic feature flag toggling and configuration adjustments</item>
    /// <item>Supports blue-green deployments with configuration updates</item>
    /// <item>Allows for real-time debugging and troubleshooting configuration changes</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Performance Considerations:</strong>
    /// <list type="bullet">
    /// <item>Each reload makes API calls to AWS Parameter Store</item>
    /// <item>Frequent reloads may incur AWS API costs and rate limiting</item>
    /// <item>Recommended minimum interval is 1 minute for production scenarios</item>
    /// <item>Consider using AWS Parameter Store's standard vs advanced parameters for cost optimization</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Recommended Intervals:</strong>
    /// <list type="table">
    /// <listheader>
    /// <term>Scenario</term>
    /// <description>Recommended Interval</description>
    /// </listheader>
    /// <item>
    /// <term>Development</term>
    /// <description>1-2 minutes for rapid configuration testing</description>
    /// </item>
    /// <item>
    /// <term>Production</term>
    /// <description>5-15 minutes for stable configuration updates</description>
    /// </item>
    /// <item>
    /// <term>Critical Systems</term>
    /// <description>15+ minutes to minimize API calls and costs</description>
    /// </item>
    /// </list>
    /// </para>
    /// </remarks>
    public TimeSpan? ReloadAfter { get; [UsedImplicitly] set; }

    /// <summary>
    /// Gets or sets the AWS configuration options for Parameter Store access.
    /// Provides control over AWS credentials, region, and other AWS SDK settings.
    /// </summary>
    /// <value>
    /// The AWS options to use for Parameter Store access, or null to use the default
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
    /// <strong>Custom Configuration Examples:</strong>
    /// <code>
    /// // Specify a specific AWS region
    /// AwsOptions = new AWSOptions
    /// {
    ///     Region = RegionEndpoint.USWest2
    /// };
    ///
    /// // Use a specific AWS profile
    /// AwsOptions = new AWSOptions
    /// {
    ///     Profile = "production",
    ///     Region = RegionEndpoint.USEast1
    /// };
    ///
    /// // Custom credentials (not recommended for production)
    /// AwsOptions = new AWSOptions
    /// {
    ///     Credentials = new BasicAWSCredentials("accessKey", "secretKey"),
    ///     Region = RegionEndpoint.EUWest1
    /// };
    /// </code>
    /// </para>
    /// </remarks>
    public AWSOptions? AwsOptions { get; [UsedImplicitly] set; }

    /// <summary>
    /// Gets or sets a value indicating whether String and SecureString parameters
    /// containing JSON should be automatically flattened into the configuration hierarchy.
    /// </summary>
    /// <value>
    /// True to enable JSON processing and flattening; false to treat all parameters as simple strings.
    /// Default is false.
    /// </value>
    /// <remarks>
    /// <para>
    /// <strong>JSON Processing Benefits:</strong>
    /// <list type="bullet">
    /// <item>Enables storing complex configuration structures in a single parameter</item>
    /// <item>Reduces the number of individual parameters needed</item>
    /// <item>Maintains hierarchical relationships in configuration data</item>
    /// <item>Supports strongly-typed configuration binding for complex objects</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Example JSON Processing:</strong>
    /// <code>
    /// // Parameter: /myapp/database/config
    /// // Value: {"host": "localhost", "port": 5432, "ssl": true, "pool": {"min": 5, "max": 20}}
    ///
    /// // Results in these configuration keys:
    /// // myapp:database:config:host = "localhost"
    /// // myapp:database:config:port = "5432"
    /// // myapp:database:config:ssl = "true"
    /// // myapp:database:config:pool:min = "5"
    /// // myapp:database:config:pool:max = "20"
    /// </code>
    /// </para>
    ///
    /// <para>
    /// <strong>When to Enable JSON Processing:</strong>
    /// <list type="bullet">
    /// <item>When you have complex configuration objects that benefit from hierarchical organization</item>
    /// <item>When migrating from file-based configuration (JSON/YAML) to Parameter Store</item>
    /// <item>When you want to use strongly-typed configuration binding with RegisterConfig</item>
    /// <item>When you need to store arrays or nested objects in configuration</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>When to Disable JSON Processing:</strong>
    /// <list type="bullet">
    /// <item>When parameters contain JSON strings that should remain as literal values</item>
    /// <item>When all configuration is simple key-value pairs</item>
    /// <item>When you want maximum control over how JSON is processed</item>
    /// <item>When performance is critical and you want to avoid JSON parsing overhead</item>
    /// </list>
    /// </para>
    /// </remarks>
    public bool JsonProcessor { get; [UsedImplicitly] set; }

    /// <summary>
    /// Gets or sets the specific parameter paths that should have JSON processing applied.
    /// When specified, only parameters matching these paths will be processed as JSON,
    /// even when JsonProcessor is enabled.
    /// </summary>
    /// <value>
    /// An array of parameter path prefixes that should be processed as JSON,
    /// or null to apply JSON processing to all parameters when JsonProcessor is enabled.
    /// </value>
    /// <remarks>
    /// <para>
    /// <strong>Selective JSON Processing:</strong>
    /// This property allows fine-grained control over which parameters are processed as JSON.
    /// This is useful when you have a mix of simple string parameters and complex JSON parameters
    /// and want to avoid accidentally processing string values as JSON.
    /// </para>
    ///
    /// <para>
    /// <strong>Path Matching Rules:</strong>
    /// <list type="bullet">
    /// <item>Paths are matched using case-insensitive prefix matching</item>
    /// <item>Forward slashes in Parameter Store paths are converted to colons for matching</item>
    /// <item>A parameter matches if its configuration key starts with any specified path</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Example Usage:</strong>
    /// <code>
    /// JsonProcessorPaths = new[]
    /// {
    ///     "/myapp/database/",
    ///     "/myapp/redis/",
    ///     "/myapp/features/"
    /// };
    ///
    /// // Only parameters under these paths will be processed as JSON:
    /// // /myapp/database/config (processed as JSON)
    /// // /myapp/redis/cluster (processed as JSON)
    /// // /myapp/api/timeout (NOT processed as JSON)
    /// </code>
    /// </para>
    /// </remarks>
    public string[]? JsonProcessorPaths { get; [UsedImplicitly] set; }

    /// <summary>
    /// Gets or sets a custom parameter processor for transforming parameter names and values.
    /// Allows for custom logic to modify how Parameter Store parameter names are converted
    /// to configuration keys.
    /// </summary>
    /// <value>
    /// A custom parameter processor implementation, or null to use the default transformation logic.
    /// </value>
    /// <remarks>
    /// <para>
    /// <strong>Default Transformation:</strong>
    /// When no custom processor is specified, the default behavior:
    /// <list type="number">
    /// <item>Removes the configured path prefix from parameter names</item>
    /// <item>Converts forward slashes (/) to colons (:)</item>
    /// <item>Removes leading slashes</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Custom Processing Use Cases:</strong>
    /// <list type="bullet">
    /// <item>Applying naming conventions or transformations specific to your organization</item>
    /// <item>Filtering out certain parameters based on naming patterns</item>
    /// <item>Adding prefixes or suffixes to configuration keys</item>
    /// <item>Implementing environment-specific parameter name mapping</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Example Custom Processor:</strong>
    /// <code>
    /// public class CustomParameterProcessor : IParameterProcessor
    /// {
    ///     public string ProcessParameterName(string configKey, string originalParameterName)
    ///     {
    ///         // Add environment prefix to all keys
    ///         return $"prod:{configKey}";
    ///     }
    /// }
    ///
    /// // Usage:
    /// ParameterProcessor = new CustomParameterProcessor();
    /// </code>
    /// </para>
    /// </remarks>
    public IParameterProcessor? ParameterProcessor { get; [UsedImplicitly] set; }

    /// <summary>
    /// Gets or sets the error handling callback that is invoked when configuration loading fails
    /// and the source is marked as optional.
    /// </summary>
    /// <value>
    /// An action that receives a ConfigurationProviderException when loading fails,
    /// or null if no custom error handling is needed.
    /// </value>
    /// <remarks>
    /// <para>
    /// <strong>Error Handling Scenarios:</strong>
    /// This callback is only invoked when:
    /// <list type="bullet">
    /// <item>The source is marked as Optional = true</item>
    /// <item>An error occurs during parameter loading (AWS API errors, permissions, etc.)</item>
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
    ///         "Failed to load optional Parameter Store configuration from {Path}",
    ///         exception.Source.Path);
    /// };
    ///
    /// // Metrics collection for operational monitoring
    /// OnLoadException = exception =>
    /// {
    ///     metrics.Increment("config.parameterstore.load.failures",
    ///         new[] { ("path", exception.Source.Path) });
    /// };
    ///
    /// // Conditional retry logic
    /// OnLoadException = exception =>
    /// {
    ///     if (ShouldRetry(exception.InnerException))
    ///     {
    ///         // Schedule retry or set flag for retry
    ///         ScheduleRetry(exception.Source);
    ///     }
    /// };
    /// </code>
    /// </para>
    /// </remarks>
    public Action<ConfigurationProviderException>? OnLoadException { get; [UsedImplicitly] set; }

    /// <summary>
    /// Creates a new AWS Parameter Store configuration provider that will load data from this source.
    /// This method is called by the .NET configuration system when building the configuration
    /// from all registered sources.
    /// </summary>
    /// <param name="builder">
    /// The configuration builder that is requesting the provider.
    /// This parameter is required by the IConfigurationSource interface but is not used by this implementation.
    /// </param>
    /// <returns>
    /// A new <see cref="AwsParameterStoreConfigurationProvider"/> instance configured to load
    /// parameters from the AWS Parameter Store path specified by this source's properties.
    /// </returns>
    /// <remarks>
    /// This method implements the factory pattern used by the .NET configuration system.
    /// Each call to Build() creates a new provider instance, allowing the configuration
    /// system to manage the provider lifecycle and reloading as needed.
    ///
    /// <para>
    /// <strong>Provider Creation:</strong>
    /// The returned provider is initialized with this source instance, giving it access
    /// to all configuration properties (Path, Optional, AWS options, etc.) needed to load parameters.
    /// </para>
    ///
    /// <para>
    /// <strong>Lifecycle Management:</strong>
    /// <list type="bullet">
    /// <item>Called once per configuration building operation</item>
    /// <item>Provider instances are managed by the configuration system</item>
    /// <item>Each provider is responsible for loading its specific Parameter Store path</item>
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
    /// var source = new AwsParameterStoreConfigurationSource
    /// {
    ///     Path = "/myapp/",
    ///     Optional = true
    /// };
    /// configBuilder.Add(source);
    ///
    /// // When Build() is called on the configuration builder:
    /// var configuration = configBuilder.Build(); // This triggers source.Build(configBuilder)
    ///
    /// // Manual provider creation (for testing or custom scenarios):
    /// var provider = source.Build(configBuilder);
    /// provider.Load(); // Manually load Parameter Store data
    /// </code>
    /// </example>
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return new AwsParameterStoreConfigurationProvider(this);
    }
}

/// <summary>
/// Interface for custom parameter name processing logic.
/// Implementations can transform Parameter Store parameter names into configuration keys
/// according to application-specific requirements.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Purpose:</strong>
/// This interface allows applications to customize how AWS Parameter Store parameter names
/// are converted to .NET configuration keys. This is useful for organizations with specific
/// naming conventions or requirements for parameter transformation.
/// </para>
///
/// <para>
/// <strong>Implementation Guidelines:</strong>
/// <list type="bullet">
/// <item>Implementations should be stateless and thread-safe</item>
/// <item>Parameter name transformations should be deterministic and reversible where possible</item>
/// <item>Consider caching expensive transformations if they involve complex logic</item>
/// <item>Validate that transformed keys are valid .NET configuration key formats</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class EnvironmentPrefixProcessor : IParameterProcessor
/// {
///     private readonly string _environment;
///
///     public EnvironmentPrefixProcessor(string environment)
///     {
///         _environment = environment;
///     }
///
///     public string ProcessParameterName(string configKey, string originalParameterName)
///     {
///         // Add environment prefix to all configuration keys
///         return $"{_environment}:{configKey}";
///     }
/// }
///
/// // Usage:
/// var source = new AwsParameterStoreConfigurationSource
/// {
///     Path = "/shared/",
///     ParameterProcessor = new EnvironmentPrefixProcessor("production")
/// };
/// </code>
/// </example>
public interface IParameterProcessor
{
    /// <summary>
    /// Processes a parameter name to transform it into a configuration key.
    /// This method is called for each parameter retrieved from Parameter Store
    /// after the default path transformation has been applied.
    /// </summary>
    /// <param name="configKey">
    /// The configuration key after default processing (path prefix removed, slashes converted to colons).
    /// </param>
    /// <param name="originalParameterName">
    /// The original Parameter Store parameter name before any processing.
    /// </param>
    /// <returns>
    /// The final configuration key to use for this parameter in the .NET configuration system.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <strong>Processing Order:</strong>
    /// This method is called after the default parameter name transformation:
    /// <list type="number">
    /// <item>Path prefix is removed from the parameter name</item>
    /// <item>Leading slashes are removed</item>
    /// <item>Remaining slashes are converted to colons</item>
    /// <item>This method is called with the transformed key</item>
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
    string ProcessParameterName(string configKey, string originalParameterName);
}
