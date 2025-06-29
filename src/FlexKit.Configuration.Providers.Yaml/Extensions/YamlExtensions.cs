using FlexKit.Configuration.Core;
using FlexKit.Configuration.Providers.Yaml.Sources;
using JetBrains.Annotations;

namespace FlexKit.Configuration.Providers.Yaml.Extensions;

/// <summary>
/// Extension methods for FlexConfigurationBuilder to add YAML file support.
/// Provides fluent API methods for integrating YAML configuration files
/// into the FlexKit configuration pipeline.
/// </summary>
/// <remarks>
/// These extension methods follow the same patterns established by the core FlexKit.Configuration
/// library, providing a consistent API experience across different configuration sources.
///
/// <para>
/// <strong>Integration Benefits:</strong>
/// <list type="bullet">
/// <item>Seamless integration with the existing FlexConfigurationBuilder workflow</item>
/// <item>Consistent parameter naming and behavior with other configuration sources</item>
/// <item>Support for both required and optional YAML files</item>
/// <item>Fluent method chaining for readable configuration setup</item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Usage in Configuration Pipeline:</strong>
/// YAML files can be mixed with other configuration sources and will follow
/// the precedence order established by the order of method calls on the builder.
/// </para>
/// </remarks>
public static class YamlExtensions
{
    /// <summary>
    /// Adds a YAML configuration file to the FlexConfigurationBuilder.
    /// Enables loading hierarchical configuration data from YAML files with support
    /// for nested objects, arrays, and all standard YAML data types.
    /// </summary>
    /// <param name="builder">The FlexConfigurationBuilder to extend with YAML support.</param>
    /// <param name="path">
    /// The path to the YAML file. Can be relative to the application base directory
    /// or an absolute path. If null or empty, defaults to "appsettings.yaml".
    /// </param>
    /// <param name="optional">
    /// <c>true</c> if the file is optional and missing files should not cause configuration
    /// loading to fail; <c>false</c> if the file is required. Default is <c>true</c>.
    /// </param>
    /// <returns>The same FlexConfigurationBuilder instance to enable method chaining.</returns>
    /// <remarks>
    /// This method integrates YAML file support into the FlexKit configuration system,
    /// allowing YAML files to be used alongside JSON files, environment variables,
    /// and other configuration sources.
    ///
    /// <para>
    /// <strong>YAML Advantages:</strong>
    /// <list type="bullet">
    /// <item>More readable than JSON for complex configuration</item>
    /// <item>Support for comments and documentation within config files</item>
    /// <item>Better support for multi-line strings</item>
    /// <item>Cleaner syntax for hierarchical data</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Configuration Precedence:</strong>
    /// YAML files follow the same precedence rules as other configuration sources.
    /// Sources added later to the builder override values from earlier sources
    /// for the same configuration keys.
    /// </para>
    ///
    /// <para>
    /// <strong>File Path Handling:</strong>
    /// <list type="bullet">
    /// <item>Relative paths are resolved against the application's base directory</item>
    /// <item>Absolute paths are used as-is</item>
    /// <item>Path separators are normalized to the current platform</item>
    /// <item>Common YAML file extensions: .yaml, .yml</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Error Handling:</strong>
    /// <list type="bullet">
    /// <item>Missing required files (optional = false) throw FileNotFoundException</item>
    /// <item>Invalid YAML syntax throws parsing exceptions with detailed error information</item>
    /// <item>Empty or comment-only files result in an empty configuration (no error)</item>
    /// <item>File permission errors throw UnauthorizedAccessException</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Best Practices:</strong>
    /// <list type="bullet">
    /// <item>Use .yaml extension for consistency</item>
    /// <item>Set optional=false for critical configuration files</item>
    /// <item>Set optional=true for environment-specific overrides</item>
    /// <item>Validate YAML syntax in development and CI/CD pipelines</item>
    /// <item>Use meaningful hierarchical structure to organize configuration</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when called after Build() has been called on the builder.</exception>
    /// <example>
    /// <code>
    /// // Basic YAML file addition
    /// var config = new FlexConfigurationBuilder()
    ///     .AddYamlFile("appsettings.yaml")
    ///     .Build();
    ///
    /// // Environment-specific YAML files with precedence
    /// var config = new FlexConfigurationBuilder()
    ///     .AddYamlFile("appsettings.yaml", optional: false)        // Base configuration (required)
    ///     .AddYamlFile($"appsettings.{environment}.yaml", optional: true)  // Environment overrides
    ///     .AddEnvironmentVariables()                              // Highest precedence
    ///     .Build();
    ///
    /// // Mixed configuration sources
    /// var config = new FlexConfigurationBuilder()
    ///     .AddJsonFile("appsettings.json")          // JSON base
    ///     .AddYamlFile("features.yaml")             // YAML for complex features' config
    ///     .AddYamlFile("database.yaml")             // YAML for database config
    ///     .AddEnvironmentVariables()                // Environment overrides
    ///     .Build();
    /// </code>
    /// </example>
    [UsedImplicitly]
    public static FlexConfigurationBuilder AddYamlFile(
        this FlexConfigurationBuilder builder,
        string path = "appsettings.yaml",
        bool optional = true)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.AddSource(new YamlConfigurationSource
        {
            Path = path,
            Optional = optional
        });
    }
}
