using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;

namespace FlexKit.Configuration.Providers.Yaml.Sources;

/// <summary>
/// Configuration source that represents a YAML file in the configuration system.
/// Implements IConfigurationSource to integrate YAML file support with the standard
/// .NET configuration infrastructure, enabling YAML files to be used alongside other configuration sources.
/// </summary>
/// <remarks>
/// This class serves as the factory and metadata container for YAML file configuration providers.
/// It follows the standard .NET configuration pattern where sources are responsible for
/// creating their corresponding providers and defining configuration parameters.
///
/// <para>
/// <strong>Role in Configuration Pipeline:</strong>
/// <list type="number">
/// <item>Defines the YAML file location and loading options</item>
/// <item>Integrates with ConfigurationBuilder through IConfigurationSource</item>
/// <item>Creates YamlConfigurationProvider instances when requested</item>
/// <item>Provides metadata about the YAML file source to the configuration system</item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Integration with FlexKit:</strong>
/// This source is designed to work seamlessly with FlexConfigurationBuilder and other
/// FlexKit configuration components, providing YAML file support as a first-class
/// configuration source option.
/// </para>
///
/// <para>
/// <strong>Typical Usage Pattern:</strong>
/// This class is typically not instantiated directly by application code but rather
/// through the FlexConfigurationBuilder.AddYamlFile() extension method, which
/// provides a more convenient and fluent API.
/// </para>
///
/// <para>
/// <strong>Configuration Source Lifecycle:</strong>
/// <list type="number">
/// <item>Source is created with a specified path and options</item>
/// <item>Source is added to a ConfigurationBuilder</item>
/// <item>When configuration is built, the Build() method is called to create provider</item>
/// <item>Provider loads YAML file data and makes it available to the configuration system</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Direct usage (typically not recommended)
/// var source = new YamlConfigurationSource
/// {
///     Path = "appsettings.yaml",
///     Optional = true
/// };
/// var configBuilder = new ConfigurationBuilder();
/// configBuilder.Add(source);
///
/// // Preferred usage through FlexConfigurationBuilder
/// var flexConfig = new FlexConfigurationBuilder()
///     .AddYamlFile("appsettings.yaml", optional: true)
///     .Build();
/// </code>
/// </example>
public class YamlConfigurationSource : IConfigurationSource
{
    /// <summary>
    /// Gets or sets the path to the YAML file.
    /// This path can be relative to the application's base directory or an absolute path.
    /// </summary>
    /// <value>
    /// The file path to the YAML configuration file. If null or empty,
    /// the provider will use "appsettings.yaml" as the default filename.
    /// </value>
    /// <remarks>
    /// <para>
    /// <strong>Path Resolution:</strong>
    /// <list type="bullet">
    /// <item>Relative paths are resolved against the application's base directory</item>
    /// <item>Absolute paths are used as-is</item>
    /// <item>Path separators are normalized to the current platform</item>
    /// <item>Environment variable expansion is not performed automatically</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Common Patterns:</strong>
    /// <list type="bullet">
    /// <item>"appsettings.yaml" - Standard application configuration</item>
    /// <item>"appsettings.{environment}.yaml" - Environment-specific overrides</item>
    /// <item>"config/database.yaml" - Component-specific configuration</item>
    /// <item>"/etc/myapp/config.yaml" - System-wide configuration (Linux)</item>
    /// </list>
    /// </para>
    /// </remarks>
    public string Path { get; [UsedImplicitly] set; } = "appsettings.yaml";

    /// <summary>
    /// Gets or sets a value indicating whether the YAML file is optional.
    /// When set to <c>true</c>, missing files will not cause configuration loading to fail.
    /// When set to <c>false</c>, missing files will throw a <see cref="FileNotFoundException"/>.
    /// </summary>
    /// <value>
    /// <c>true</c> if the file is optional and configuration should continue loading even if
    /// the file is missing; <c>false</c> if the file is required and missing files should
    /// cause configuration loading to fail. Default is <c>true</c>.
    /// </value>
    /// <remarks>
    /// <para>
    /// <strong>Recommended Usage:</strong>
    /// <list type="bullet">
    /// <item>Set to <c>false</c> for critical configuration files like base appsettings.yaml</item>
    /// <item>Set to <c>true</c> for environment-specific overrides and optional configurations</item>
    /// <item>Consider application startup requirements when deciding between optional and required</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Error Behavior:</strong>
    /// When <c>Optional = false</c> and the file is missing, the provider will throw
    /// a <see cref="FileNotFoundException"/> during the Load() operation, which will
    /// propagate up through the configuration building process.
    /// </para>
    /// </remarks>
    public bool Optional { get; [UsedImplicitly] set; } = true;

    /// <summary>
    /// Creates a new YAML configuration provider that will load data from this source.
    /// This method is called by the .NET configuration system when building the
    /// configuration from all registered sources.
    /// </summary>
    /// <param name="builder">
    /// The configuration builder that is requesting the provider.
    /// This parameter is required by the IConfigurationSource interface but is not used by this implementation.
    /// </param>
    /// <returns>
    /// A new <see cref="YamlConfigurationProvider"/> instance configured to load
    /// the YAML file specified by this source's properties.
    /// </returns>
    /// <remarks>
    /// This method implements the factory pattern used by the .NET configuration system.
    /// Each call to Build() creates a new provider instance, allowing the configuration
    /// system to manage the provider lifecycle and reloading as needed.
    ///
    /// <para>
    /// <strong>Provider Creation:</strong>
    /// The returned provider is initialized with this source instance, giving it access
    /// to all configuration properties (Path, Optional, etc.) needed to load the YAML file.
    /// </para>
    ///
    /// <para>
    /// <strong>Lifecycle Management:</strong>
    /// <list type="bullet">
    /// <item>Called once per configuration building operation</item>
    /// <item>Provider instances are managed by the configuration system</item>
    /// <item>Each provider is responsible for loading its specific YAML file</item>
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
    /// <example>
    /// <code>
    /// // This method is typically called automatically by the configuration system:
    /// var configBuilder = new ConfigurationBuilder();
    /// var source = new YamlConfigurationSource { Path = "config.yaml", Optional = true };
    /// configBuilder.Add(source);
    ///
    /// // When Build() is called on the configuration builder:
    /// var configuration = configBuilder.Build(); // This triggers source.Build(configBuilder)
    ///
    /// // Manual provider creation (for testing or custom scenarios):
    /// var provider = source.Build(configBuilder);
    /// provider.Load(); // Manually load YAML file data
    /// </code>
    /// </example>
    public IConfigurationProvider Build(IConfigurationBuilder builder) =>
        new YamlConfigurationProvider(this);
}
