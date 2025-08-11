// <copyright file="DotEnvConfigurationSource.cs" company="Michael Saruyev">
// Copyright (c) Michael Saruyev. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

using Microsoft.Extensions.Configuration;

namespace FlexKit.Configuration.Sources;

/// <summary>
/// Configuration source that represents a ".env" file in the configuration system.
/// Implements IConfigurationSource to integrate .env file support with the standard
/// .NET configuration infrastructure, enabling .env files to be used alongside other configuration sources.
/// </summary>
/// <remarks>
/// This class serves as the factory and metadata container for .env file configuration providers.
/// It follows the standard .NET configuration pattern where sources are responsible for
/// creating their corresponding providers and defining configuration parameters.
///
/// <para>
/// <strong>Role in Configuration Pipeline:</strong>
/// <list type="number">
/// <item>Defines the .env file location and loading options</item>
/// <item>Integrates with ConfigurationBuilder through IConfigurationSource</item>
/// <item>Creates DotEnvConfigurationProvider instances when requested</item>
/// <item>Provides metadata about the .env file source to the configuration system</item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Integration with FlexKit:</strong>
/// This source is designed to work seamlessly with FlexConfigurationBuilder and other
/// FlexKit configuration components, providing .env file support as a first-class
/// configuration source option.
/// </para>
///
/// <para>
/// <strong>Typical Usage Pattern:</strong>
/// This class is typically not instantiated directly by application code but rather
/// through the FlexConfigurationBuilder.AddDotEnvFile() extension method, which
/// provides a more convenient and fluent API.
/// </para>
///
/// <para>
/// <strong>Configuration Source Lifecycle:</strong>
/// <list type="number">
/// <item>Source is created with a specified path and options</item>
/// <item>Source is added to a ConfigurationBuilder</item>
/// <item>When configuration is built, the Build() method is called to create provider</item>
/// <item>Provider loads .env file data and makes it available to the configuration system</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Direct usage (typically not recommended)
/// var source = new DotEnvConfigurationSource
/// {
///     Path = ".env.development",
///     Optional = true
/// };
/// var configBuilder = new ConfigurationBuilder();
/// configBuilder.Add(source);
///
/// // Preferred usage through FlexConfigurationBuilder
/// var flexConfig = new FlexConfigurationBuilder()
///     .AddDotEnvFile(".env.development", optional: true)
///     .Build();
/// </code>
/// </example>
public class DotEnvConfigurationSource : IConfigurationSource
{
    /// <summary>
    /// Gets or sets the path to the .env file.
    /// The path can be absolute or relative to the application's base directory.
    /// </summary>
    /// <value>
    /// The file system path to the .env file. Defaults to ".env" if not specified.
    /// </value>
    /// <remarks>
    /// <para>
    /// <strong>Path Resolution:</strong>
    /// <list type="bullet">
    /// <item>Relative paths are resolved relative to the application's base directory</item>
    /// <item>Absolute paths are used as-is</item>
    /// <item>Path separators are automatically normalized for the current platform</item>
    /// <item>The file extension is not enforced - any text file can be used</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Common Path Patterns:</strong>
    /// <list type="table">
    /// <listheader>
    /// <term>Pattern</term>
    /// <description>Use Case</description>
    /// </listheader>
    /// <item>
    /// <term>.env</term>
    /// <description>Standard .env file in an application root</description>
    /// </item>
    /// <item>
    /// <term>.env.development</term>
    /// <description>Development-specific environment file</description>
    /// </item>
    /// <item>
    /// <term>config/.env</term>
    /// <description>.env file in a config subdirectory</description>
    /// </item>
    /// <item>
    /// <term>/etc/myapp/.env</term>
    /// <description>Absolute path for system-wide configuration</description>
    /// </item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Security Considerations:</strong>
    /// Ensure that .env files containing sensitive information are:
    /// <list type="bullet">
    /// <item>Not committed to version control</item>
    /// <item>Protected with appropriate file system permissions</item>
    /// <item>Located outside web-accessible directories in web applications</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Standard .env file
    /// source.Path = ".env";
    ///
    /// // Environment-specific file
    /// source.Path = $".env.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}";
    ///
    /// // Config directory
    /// source.Path = Path.Combine("config", ".env");
    ///
    /// // Absolute path
    /// source.Path = "/etc/myapp/.env";
    /// </code>
    /// </example>
    public string Path { get; init; } = ".env";

    /// <summary>
    /// Gets or sets a value indicating whether the .env file is optional.
    /// When true, missing files don't cause configuration building to fail.
    /// </summary>
    /// <value>
    /// <c>true</c> if the .env file is optional and missing files should be ignored;
    /// <c>false</c> if the file is required and missing files should cause an error.
    /// Defaults to <c>true</c>.
    /// </value>
    /// <remarks>
    /// <para>
    /// <strong>Optional vs Required Files:</strong>
    /// <list type="table">
    /// <listheader>
    /// <term>Setting</term>
    /// <description>Behavior When File Missing</description>
    /// </listheader>
    /// <item>
    /// <term>Optional = true</term>
    /// <description>Configuration building continues normally, no error thrown</description>
    /// </item>
    /// <item>
    /// <term>Optional = false</term>
    /// <description>FileNotFoundException is thrown during configuration building</description>
    /// </item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Usage Recommendations:</strong>
    /// <list type="bullet">
    /// <item>Use Optional = true for development-specific .env files</item>
    /// <item>Use Optional = false for .env files that contain critical configuration</item>
    /// <item>Consider environment-specific settings (required in production, optional in development)</item>
    /// <item>Document required .env files in your project's setup instructions</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Error Handling Impact:</strong>
    /// When Optional is false, the application will fail to start if the .env file
    /// is missing, providing early detection of configuration problems. When Optional
    /// is true, the application will start successfully but may have default or
    /// incomplete configuration.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Optional .env file for development overrides
    /// var devSource = new DotEnvConfigurationSource
    /// {
    ///     Path = ".env.development",
    ///     Optional = true  // Won't fail if file doesn't exist
    /// };
    ///
    /// // Required .env file for production secrets
    /// var prodSource = new DotEnvConfigurationSource
    /// {
    ///     Path = ".env.production",
    ///     Optional = false  // Will fail if file doesn't exist
    /// };
    ///
    /// // Environment-dependent optional setting
    /// var source = new DotEnvConfigurationSource
    /// {
    ///     Path = ".env",
    ///     Optional = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") != "Production"
    /// };
    /// </code>
    /// </example>
    public bool Optional { get; init; } = true;

    /// <summary>
    /// Builds and returns a configuration provider for this .env file source.
    /// This method is called by the .NET configuration system when building the
    /// configuration from all registered sources.
    /// </summary>
    /// <param name="builder">
    /// The configuration builder that is requesting the provider.
    /// This parameter is required by the IConfigurationSource interface but is not used by this implementation.
    /// </param>
    /// <returns>
    /// A new <see cref="DotEnvConfigurationProvider"/> instance configured to load
    /// the .env file specified by this source's properties.
    /// </returns>
    /// <remarks>
    /// This method implements the factory pattern used by the .NET configuration system.
    /// Each call to Build() creates a new provider instance, allowing the configuration
    /// system to manage the provider lifecycle and reloading as needed.
    ///
    /// <para>
    /// <strong>Provider Creation:</strong>
    /// The returned provider is initialized with this source instance, giving it access
    /// to all configuration properties (Path, Optional, etc.) needed to load the .env file.
    /// </para>
    ///
    /// <para>
    /// <strong>Lifecycle Management:</strong>
    /// <list type="bullet">
    /// <item>Called once per configuration building operation</item>
    /// <item>Provider instances are managed by the configuration system</item>
    /// <item>Each provider is responsible for loading its specific .env file</item>
    /// <item>Providers are disposed when the configuration is disposed</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Integration Pattern:</strong>
    /// This method follows the standard .NET configuration source pattern where:
    /// <list type="number">
    /// <item>Sources define what configuration to load (metadata)</item>
    /// <item>Providers actually load the configuration data.</item>
    /// <item>Build() method bridges between sources and providers</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // This method is typically called automatically by the configuration system:
    /// var configBuilder = new ConfigurationBuilder();
    /// var source = new DotEnvConfigurationSource { Path = ".env", Optional = true };
    /// configBuilder.Add(source);
    ///
    /// // When Build() is called on the configuration builder:
    /// var configuration = configBuilder.Build(); // This triggers source.Build(configBuilder)
    ///
    /// // Manual provider creation (for testing or custom scenarios):
    /// var provider = source.Build(configBuilder);
    /// provider.Load(); // Manually load .env file data
    /// </code>
    /// </example>
    public IConfigurationProvider Build(IConfigurationBuilder builder) =>
        new DotEnvConfigurationProvider(this);
}
