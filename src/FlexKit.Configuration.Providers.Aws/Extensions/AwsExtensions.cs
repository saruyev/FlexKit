// <copyright file="FlexConfigurationBuilderAwsExtensions.cs" company="FlexKit">
// Copyright (c) FlexKit. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

using Amazon.Extensions.NETCore.Setup;
using FlexKit.Configuration.Core;
using FlexKit.Configuration.Providers.Aws.Sources;
using JetBrains.Annotations;

namespace FlexKit.Configuration.Providers.Aws.Extensions;

/// <summary>
/// Extension methods for <see cref="FlexConfigurationBuilder"/> that add AWS configuration source support.
/// Provides fluent API methods to integrate AWS services like Parameter Store and Secrets Manager
/// with the FlexKit configuration system.
/// </summary>
/// <remarks>
/// These extension methods follow the FlexKit configuration pattern of providing a fluent,
/// strongly-typed API for adding configuration sources. They maintain consistency with other
/// FlexKit configuration providers while providing AWS-specific functionality and options.
///
/// <para>
/// <strong>AWS Integration Benefits:</strong>
/// <list type="bullet">
/// <item>Centralized configuration management across multiple applications and environments</item>
/// <item>Built-in encryption for sensitive configuration data using AWS KMS</item>
/// <item>Hierarchical organization of configuration parameters</item>
/// <item>Automatic credential management using AWS IAM roles and policies</item>
/// <item>Support for configuration versioning and change tracking</item>
/// <item>Integration with AWS CloudFormation and other infrastructure-as-code tools</item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Security Considerations:</strong>
/// AWS configuration sources automatically use the AWS credential resolution chain,
/// ensuring secure access to configuration data without hardcoding credentials in
/// application code. IAM policies can be used to provide fine-grained access control
/// to specific configuration parameters.
/// </para>
/// </remarks>
public static class AwsExtensions
{
    /// <summary>
    /// Adds AWS Parameter Store as a configuration source to the FlexKit configuration builder.
    /// Enables applications to load configuration data from AWS Systems Manager Parameter Store
    /// with support for hierarchical parameters, JSON processing, and automatic reloading.
    /// </summary>
    /// <param name="builder">The FlexKit configuration builder to add the Parameter Store source to.</param>
    /// <param name="path">
    /// The Parameter Store path prefix to load parameters from.
    /// Should start with a forward slash (e.g., "/myapp/", "/prod/database/").
    /// </param>
    /// <param name="optional">
    /// Indicates whether the Parameter Store source is optional.
    /// When true, failures to load parameters will not cause configuration building to fail.
    /// Default is true.
    /// </param>
    /// <returns>The same FlexKit configuration builder instance to enable method chaining.</returns>
    /// <remarks>
    /// This is the simplest overload for adding Parameter Store support. It uses default
    /// AWS credential resolution and basic parameter processing without JSON flattening
    /// or automatic reloading.
    ///
    /// <para>
    /// <strong>Default Behavior:</strong>
    /// <list type="bullet">
    /// <item>Uses the default AWS credential resolution chain</item>
    /// <item>Loads all parameters under the specified path recursively</item>
    /// <item>Transforms parameter names from AWS format to .NET configuration keys</item>
    /// <item>Does not process JSON parameters (treats them as simple strings)</item>
    /// <item>Does not automatically reload parameters</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Parameter Transformation Example:</strong>
    /// <code>
    /// // AWS Parameter Store:
    /// // /myapp/database/host = "localhost"
    /// // /myapp/database/port = "5432"
    /// // /myapp/features/caching = "true"
    ///
    /// // Resulting configuration keys:
    /// // myapp:database:host = "localhost"
    /// // myapp:database:port = "5432"
    /// // myapp:features:caching = "true"
    /// </code>
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is null or empty.</exception>
    /// <example>
    /// <code>
    /// // Basic Parameter Store configuration
    /// var config = new FlexConfigurationBuilder()
    ///     .AddAwsParameterStore("/myapp/")
    ///     .Build();
    ///
    /// // Access parameter values
    /// var dbHost = config["myapp:database:host"];
    /// var caching = config["myapp:features:caching"];
    ///
    /// // With other configuration sources
    /// var config = new FlexConfigurationBuilder()
    ///     .AddJsonFile("appsettings.json")
    ///     .AddAwsParameterStore("/myapp/")
    ///     .AddEnvironmentVariables()
    ///     .Build();
    /// </code>
    /// </example>
    [UsedImplicitly]
    public static FlexConfigurationBuilder AddAwsParameterStore(
        this FlexConfigurationBuilder builder,
        string path,
        bool optional = true)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(path);

        return builder.AddSource(new AwsParameterStoreConfigurationSource
        {
            Path = path,
            Optional = optional
        });
    }

    /// <summary>
    /// Adds AWS Parameter Store as a configuration source with advanced configuration options.
    /// Provides full control over Parameter Store integration including AWS credentials,
    /// JSON processing, automatic reloading, and custom parameter transformation.
    /// </summary>
    /// <param name="builder">The FlexKit configuration builder to add the Parameter Store source to.</param>
    /// <param name="configure">
    /// An action to configure the Parameter Store options, including path, AWS settings,
    /// JSON processing, reloading, and error handling.
    /// </param>
    /// <returns>The same FlexKit configuration builder instance to enable method chaining.</returns>
    /// <remarks>
    /// This overload provides access to all Parameter Store configuration options,
    /// making it suitable for production scenarios that require specific AWS settings,
    /// automatic reloading, or advanced parameter processing.
    ///
    /// <para>
    /// <strong>Advanced Configuration Options:</strong>
    /// <list type="bullet">
    /// <item><strong>AWS Options:</strong> Custom credentials, regions, and SDK settings</item>
    /// <item><strong>JSON Processing:</strong> Automatic flattening of JSON parameters</item>
    /// <item><strong>Automatic Reloading:</strong> Periodic refresh of parameters from AWS</item>
    /// <item><strong>Custom Processing:</strong> Parameter name transformation and filtering</item>
    /// <item><strong>Error Handling:</strong> Custom logic for handling loading failures</item>
    /// <item><strong>Selective JSON Processing:</strong> Apply JSON processing only to specific paths</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Production Configuration Example:</strong>
    /// <code>
    /// var config = new FlexConfigurationBuilder()
    ///     .AddAwsParameterStore(options =>
    ///     {
    ///         options.Path = "/prod/myapp/";
    ///         options.Optional = false; // Required in production
    ///         options.JsonProcessor = true;
    ///         options.JsonProcessorPaths = new[] { "/prod/myapp/database/", "/prod/myapp/cache/" };
    ///         options.ReloadAfter = TimeSpan.FromMinutes(10);
    ///         options.AwsOptions = new AWSOptions
    ///         {
    ///             Region = RegionEndpoint.USEast1,
    ///             Profile = "production"
    ///         };
    ///         options.OnLoadException = ex => logger.LogError(ex, "Parameter Store load failed");
    ///     })
    ///     .Build();
    /// </code>
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="configure"/> is null.</exception>
    /// <example>
    /// <code>
    /// // Development configuration with JSON processing and reloading
    /// var config = new FlexConfigurationBuilder()
    ///     .AddAwsParameterStore(options =>
    ///     {
    ///         options.Path = "/dev/myapp/";
    ///         options.Optional = true;
    ///         options.JsonProcessor = true;
    ///         options.ReloadAfter = TimeSpan.FromMinutes(2);
    ///     })
    ///     .Build();
    ///
    /// // Production configuration with custom AWS settings
    /// var config = new FlexConfigurationBuilder()
    ///     .AddAwsParameterStore(options =>
    ///     {
    ///         options.Path = "/prod/myapp/";
    ///         options.Optional = false;
    ///         options.AwsOptions = new AWSOptions
    ///         {
    ///             Region = RegionEndpoint.USWest2
    ///         };
    ///         options.OnLoadException = ex =>
    ///         {
    ///             // Log to monitoring system
    ///             logger.LogCritical(ex, "Critical Parameter Store failure");
    ///             // Send alert to operations team
    ///             alertService.SendAlert("Parameter Store Failure", ex.Message);
    ///         };
    ///     })
    ///     .Build();
    ///
    /// // Configuration with custom parameter processing
    /// var config = new FlexConfigurationBuilder()
    ///     .AddAwsParameterStore(options =>
    ///     {
    ///         options.Path = "/shared/";
    ///         options.ParameterProcessor = new EnvironmentPrefixProcessor("staging");
    ///         options.JsonProcessor = true;
    ///         options.JsonProcessorPaths = new[] { "/shared/database/", "/shared/cache/" };
    ///     })
    ///     .Build();
    /// </code>
    /// </example>
    [UsedImplicitly]
    public static FlexConfigurationBuilder AddAwsParameterStore(
        this FlexConfigurationBuilder builder,
        Action<AwsParameterStoreOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new AwsParameterStoreOptions();
        configure(options);

        return builder.AddSource(new AwsParameterStoreConfigurationSource
        {
            Path = options.Path ?? string.Empty,
            Optional = options.Optional,
            ReloadAfter = options.ReloadAfter,
            AwsOptions = options.AwsOptions,
            JsonProcessor = options.JsonProcessor,
            JsonProcessorPaths = options.JsonProcessorPaths,
            ParameterProcessor = options.ParameterProcessor,
            OnLoadException = options.OnLoadException
        });
    }
}

/// <summary>
/// Configuration options for AWS Parameter Store integration with FlexKit configuration.
/// Provides a strongly-typed way to configure all aspects of Parameter Store access,
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
    /// The interval at which to reload parameters, or null to disable automatic reloading.
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
    /// The AWS options to use for Parameter Store access, or null to use defaults.
    /// Default is null (uses AWS credential resolution chain).
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
    /// <item>Supports strongly-typed configuration binding</item>
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
    /// Use Parameter Store path format with forward slashes. These will be converted
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
