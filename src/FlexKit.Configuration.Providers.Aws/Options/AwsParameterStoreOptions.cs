using Amazon.Extensions.NETCore.Setup;
using FlexKit.Configuration.Providers.Aws.Sources;
using JetBrains.Annotations;

namespace FlexKit.Configuration.Providers.Aws.Options;

/// <summary>
/// Configuration options for AWS Parameter Store integration with FlexKit configuration.
/// Provides a strongly typed way to configure all aspects of Parameter Store access,
/// including AWS settings, processing options, and error handling.
/// </summary>
/// <remarks>
/// This class serves as a data transfer object for Parameter Store configuration options,
/// providing a clean API surface for the configuration lambda while maintaining type safety
/// and IntelliSense support.
///
/// <para>
/// <strong>Configuration Categories:</strong>
/// <list type="bullet">
/// <item><strong>AWS Settings:</strong> Credentials, region, and SDK configuration</item>
/// <item><strong>Parameter Selection:</strong> Path filtering and recursive loading</item>
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
/// <item>AwsOptions = null (use default credential chain)</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Typical production configuration
/// var options = new AwsParameterStoreOptions
/// {
///     Path = "/prod/myapp/",
///     Optional = false,
///     JsonProcessor = true,
///     JsonProcessorPaths = new[] { "/prod/myapp/database/" },
///     ReloadAfter = TimeSpan.FromMinutes(15),
///     AwsOptions = new AWSOptions { Region = RegionEndpoint.USEast1 },
///     OnLoadException = ex => logger.LogError(ex, "Parameter Store error")
/// };
/// </code>
/// </example>
public class AwsParameterStoreOptions
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
    /// <strong>Path Hierarchy:</strong>
    /// Parameter Store uses forward slashes (/) to create hierarchical parameter organization.
    /// The path acts as a filter - only parameters that start with this path will be loaded.
    /// </para>
    ///
    /// <para>
    /// <strong>Best Practices:</strong>
    /// <list type="bullet">
    /// <item>Use environment prefixes: "/prod/myapp/", "/dev/myapp/", "/staging/myapp/"</item>
    /// <item>Group related parameters: "/myapp/database/", "/myapp/cache/", "/myapp/api/"</item>
    /// <item>Always start with a forward slash</item>
    /// <item>End with a forward slash to avoid partial matches</item>
    /// </list>
    /// </para>
    /// </remarks>
    public string? Path { get; [UsedImplicitly] set; }

    /// <summary>
    /// Gets or sets a value indicating whether the Parameter Store source is optional.
    /// When true, failures to load parameters will not cause configuration building to fail.
    /// </summary>
    /// <value>
    /// True if the Parameter Store source is optional; false if it's required.
    /// Default is true.
    /// </value>
    /// <remarks>
    /// <para>
    /// <strong>Production Recommendations:</strong>
    /// <list type="bullet">
    /// <item><strong>Critical Config:</strong> Set to false for essential parameters like database connections</item>
    /// <item><strong>Feature Flags:</strong> Set to true for optional features that have reasonable defaults</item>
    /// <item><strong>Environment-Specific:</strong> Set to true for dev/test, false for production</item>
    /// </list>
    /// </para>
    /// </remarks>
    public bool Optional { get; [UsedImplicitly] set; } = true;

    /// <summary>
    /// Gets or sets the automatic reload interval for Parameter Store data.
    /// When set, the provider will periodically refresh parameters from Parameter Store.
    /// </summary>
    /// <value>
    /// The interval at which to reload parameters or null to disable automatic reloading.
    /// Default is null.
    /// </value>
    /// <remarks>
    /// <para>
    /// <strong>Cost Considerations:</strong>
    /// Each reload operation makes API calls to AWS Parameter Store. Consider the balance
    /// between configuration freshness and AWS API costs when setting this interval.
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
    /// Gets or sets the AWS configuration options for Parameter Store access.
    /// Provides control over AWS credentials, region, and other SDK settings.
    /// </summary>
    /// <value>
    /// The AWS options to use it for Parameter Store access, or null to use defaults.
    /// Default is null (uses the AWS credential resolution chain).
    /// </value>
    /// <remarks>
    /// <para>
    /// <strong>Security Best Practices:</strong>
    /// <list type="bullet">
    /// <item>Use IAM roles instead of hardcoded credentials</item>
    /// <item>Apply least-privilege permissions for Parameter Store access</item>
    /// <item>Use different AWS profiles for different environments</item>
    /// <item>Avoid storing credentials in application configuration</item>
    /// </list>
    /// </para>
    /// </remarks>
    public AWSOptions? AwsOptions { get; [UsedImplicitly] set; }

    /// <summary>
    /// Gets or sets a value indicating whether JSON parameters should be automatically flattened.
    /// When enabled, String and SecureString parameters containing valid JSON will be
    /// processed into hierarchical configuration keys.
    /// </summary>
    /// <value>
    /// True to enable JSON processing; false to treat all parameters as simple strings.
    /// Default is false.
    /// </value>
    /// <remarks>
    /// <para>
    /// <strong>JSON Processing Benefits:</strong>
    /// <list type="bullet">
    /// <item>Enables complex configuration structures in single parameters</item>
    /// <item>Supports strongly typed configuration binding</item>
    /// <item>Reduces the total number of parameters needed</item>
    /// <item>Maintains hierarchical relationships in configuration</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Performance Impact:</strong>
    /// JSON processing adds parsing overhead during configuration loading.
    /// For applications with many parameters or frequent reloading, consider
    /// using JsonProcessorPaths to limit processing to specific parameter groups.
    /// </para>
    /// </remarks>
    public bool JsonProcessor { get; [UsedImplicitly] set; }

    /// <summary>
    /// Gets or sets the specific parameter paths that should have JSON processing applied.
    /// When specified, only parameters matching these paths will be processed as JSON.
    /// </summary>
    /// <value>
    /// An array of parameter path prefixes for selective JSON processing,
    /// or null to apply to all parameters when JsonProcessor is enabled.
    /// </value>
    /// <remarks>
    /// <para>
    /// <strong>Selective Processing Use Cases:</strong>
    /// <list type="bullet">
    /// <item>Mixed parameter types (some JSON, some simple strings)</item>
    /// <item>Performance optimization for large parameter sets</item>
    /// <item>Avoiding accidental JSON parsing of string values</item>
    /// <item>Gradual migration from simple to complex parameters</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Path Format:</strong>
    /// Use a Parameter Store path format with forward slashes. These will be converted
    /// to configuration key format for matching during processing.
    /// </para>
    /// </remarks>
    public string[]? JsonProcessorPaths { get; [UsedImplicitly] set; }

    /// <summary>
    /// Gets or sets a custom parameter processor for transforming parameter names.
    /// Allows for application-specific parameter name transformation logic.
    /// </summary>
    /// <value>
    /// A custom parameter processor implementation, or null for default processing.
    /// </value>
    /// <remarks>
    /// <para>
    /// <strong>Custom Processing Scenarios:</strong>
    /// <list type="bullet">
    /// <item>Adding environment or application prefixes to all parameters</item>
    /// <item>Implementing organization-specific naming conventions</item>
    /// <item>Filtering or modifying parameter names based on runtime conditions</item>
    /// <item>Mapping legacy parameter names to new configuration structures</item>
    /// </list>
    /// </para>
    /// </remarks>
    public IParameterProcessor? ParameterProcessor { get; [UsedImplicitly] set; }

    /// <summary>
    /// Gets or sets the error handling callback for optional configuration loading failures.
    /// Invoked when parameter loading fails and the source is marked as optional.
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
    /// Implement comprehensive error handling to ensure parameter loading failures
    /// are visible to operations teams and can be quickly diagnosed and resolved.
    /// </para>
    /// </remarks>
    public Action<ConfigurationProviderException>? OnLoadException { get; [UsedImplicitly] set; }
}
