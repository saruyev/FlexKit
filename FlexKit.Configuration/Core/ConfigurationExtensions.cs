// <copyright file="ConfigurationExtensions.cs" company="Michael Saruyev">
// Copyright (c) Michael Saruyev. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

using Autofac;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;

namespace FlexKit.Configuration.Core;

/// <summary>
/// Extension methods for IConfiguration that enhance configuration capabilities within the FlexKit framework.
/// Provides fluent APIs for setting up FlexConfig with Autofac containers and accessing configuration
/// through the FlexConfiguration wrapper with dynamic access capabilities.
/// </summary>
/// <remarks>
/// This class serves as the primary integration point between Microsoft's IConfiguration system
/// and FlexKit's enhanced configuration capabilities. It provides:
///
/// <list type="bullet">
/// <item>Fluent configuration setup with Autofac dependency injection</item>
/// <item>Dynamic configuration access through FlexConfiguration wrapper</item>
/// <item>Section-based configuration retrieval with type safety</item>
/// <item>Seamless integration with existing .NET configuration patterns</item>
/// </list>
///
/// <para>
/// <strong>Design Philosophy:</strong>
/// The extension methods follow the builder pattern to provide a fluent, discoverable API
/// that integrates naturally with existing .NET configuration and Autofac registration patterns.
/// </para>
///
/// <para>
/// <strong>Integration Example:</strong>
/// <code>
/// // In Program.cs or Startup.cs
/// var builder = new ContainerBuilder();
/// builder.AddFlexConfig(config => config
///     .AddJsonFile("appsettings.json")
///     .AddEnvironmentVariables()
///     .AddDotEnvFile(".env"));
///
/// var container = builder.Build();
///
/// // In application code
/// var flexConfig = container.Resolve&lt;IFlexConfig&gt;();
/// dynamic config = flexConfig;
/// var apiKey = config.Api.Key; // Dynamic access
/// </code>
/// </para>
/// </remarks>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Adds FlexConfig to an Autofac container builder with custom configuration setup.
    /// Provides a fluent API for configuring FlexConfig with multiple configuration sources
    /// and automatically registers all necessary components with the Autofac container.
    /// </summary>
    /// <param name="builder">The Autofac container builder to extend with FlexConfig capabilities.</param>
    /// <param name="configure">
    /// A configuration action that defines how FlexConfig should be set up.
    /// Use this to add configuration sources like JSON files, environment variables, or .env files.
    /// </param>
    /// <returns>
    /// The same <see cref="ContainerBuilder"/> instance to enable method chaining.
    /// </returns>
    /// <remarks>
    /// This method is the primary entry point for integrating FlexConfig with Autofac containers.
    /// It performs the following registration steps:
    ///
    /// <list type="number">
    /// <item>Creates and configures a <see cref="FlexConfigurationBuilder"/> using the provided action</item>
    /// <item>Builds the FlexConfig instance with all configured sources</item>
    /// <item>Registers the underlying <see cref="IConfiguration"/> as a singleton</item>
    /// <item>Registers the <see cref="IFlexConfig"/> as both typed and dynamic singleton</item>
    /// <item>Registers the <see cref="ConfigurationModule"/> for property injection</item>
    /// </list>
    ///
    /// <para>
    /// <strong>Registration Details:</strong>
    /// <list type="table">
    /// <listheader>
    /// <term>Service Type</term>
    /// <description>Lifetime / Notes</description>
    /// </listheader>
    /// <item>
    /// <term>IConfiguration</term>
    /// <description>Singleton - Standard .NET configuration interface</description>
    /// </item>
    /// <item>
    /// <term>IFlexConfig</term>
    /// <description>Singleton - FlexKit's enhanced configuration interface</description>
    /// </item>
    /// <item>
    /// <term>dynamic</term>
    /// <description>Singleton - Same instance as IFlexConfig for dynamic access</description>
    /// </item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Configuration Sources:</strong>
    /// The configure action can set up multiple configuration sources:
    /// <code>
    /// builder.AddFlexConfig(config => config
    ///     .AddJsonFile("appsettings.json", optional: false)
    ///     .AddJsonFile($"appsettings.{env}.json", optional: true)
    ///     .AddEnvironmentVariables()
    ///     .AddDotEnvFile(".env", optional: true));
    /// </code>
    /// </para>
    ///
    /// <para>
    /// <strong>Error Handling:</strong>
    /// Configuration errors (missing required files, invalid JSON, etc.) are propagated
    /// during the Build() phase, ensuring early detection of configuration issues.
    /// </para>
    ///
    /// <para>
    /// <strong>Property Injection:</strong>
    /// The registered ConfigurationModule enables automatic property injection for
    /// any properties named "FlexConfiguration" of type IFlexConfig in registered services.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="configure"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when configuration building fails due to invalid sources or settings.</exception>
    /// <example>
    /// <code>
    /// // Basic setup with JSON and environment variables
    /// builder.AddFlexConfig(config => config
    ///     .AddJsonFile("appsettings.json")
    ///     .AddEnvironmentVariables());
    ///
    /// // Advanced setup with multiple sources and environment-specific files
    /// builder.AddFlexConfig(config => config
    ///     .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    ///     .AddJsonFile($"appsettings.{environment}.json", optional: true)
    ///     .AddDotEnvFile(".env")
    ///     .AddEnvironmentVariables());
    ///
    /// // Minimal setup for testing
    /// builder.AddFlexConfig(config => config
    ///     .AddJsonFile("testsettings.json"));
    /// </code>
    /// </example>
    [UsedImplicitly]
    public static ContainerBuilder AddFlexConfig(
        this ContainerBuilder builder,
        Action<FlexConfigurationBuilder> configure)
    {
        var flexBuilder = new FlexConfigurationBuilder();
        configure(flexBuilder);
        var flexConfig = flexBuilder.Build();

        // Register configurations with appropriate lifetimes
        builder.RegisterInstance(flexConfig.Configuration).As<IConfiguration>().SingleInstance();
        builder.RegisterInstance(flexConfig).As<IFlexConfig>().As<dynamic>().SingleInstance();
        builder.RegisterModule<ConfigurationModule>();

        return builder;
    }

    /// <summary>
    /// Gets the configuration JSON wrapped in the FlexConfiguration accessor instance.
    /// Creates a FlexConfiguration wrapper around an existing IConfiguration instance,
    /// enabling dynamic access and FlexKit-specific functionality without changing the underlying configuration.
    /// </summary>
    /// <param name="configuration">The current configuration root to wrap.</param>
    /// <returns>
    /// A new <see cref="FlexConfiguration"/> instance that provides dynamic access
    /// to the underlying configuration data.
    /// </returns>
    /// <remarks>
    /// This method provides a bridge between existing IConfiguration instances and
    /// FlexKit's enhanced configuration capabilities. It's particularly useful when:
    ///
    /// <list type="bullet">
    /// <item>Integrating FlexKit into existing applications with established configuration</item>
    /// <item>Adding dynamic access capabilities to the configuration without changing DI setup</item>
    /// <item>Creating temporary FlexConfiguration instances for specific operations</item>
    /// <item>Testing scenarios where you need FlexConfig functionality with mock configurations</item>
    /// </list>
    ///
    /// <para>
    /// <strong>Dynamic Access Capabilities:</strong>
    /// The returned FlexConfiguration instance supports:
    /// <code>
    /// var flexConfig = configuration.GetFlexConfiguration();
    ///
    /// // Dynamic property access
    /// dynamic config = flexConfig;
    /// var apiKey = config.Api.Key;
    /// var timeout = config.Api.Timeout;
    ///
    /// // Traditional indexer access
    /// var connectionString = flexConfig["ConnectionStrings:DefaultConnection"];
    ///
    /// // Type conversion
    /// var port = flexConfig["Server:Port"].ToType&lt;int&gt;();
    /// </code>
    /// </para>
    ///
    /// <para>
    /// <strong>Relationship to Original Configuration:</strong>
    /// The FlexConfiguration wrapper maintains a reference to the original IConfiguration
    /// instance, so any changes to the underlying configuration (such as from reloadable
    /// configuration sources) are automatically reflected in the FlexConfiguration.
    /// </para>
    ///
    /// <para>
    /// <strong>Performance Considerations:</strong>
    /// Creating a FlexConfiguration wrapper is a lightweight operation that only
    /// creates a thin wrapper object. The underlying configuration data and structure
    /// remain unchanged, and no copying or transformation occurs.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configuration"/> is null.</exception>
    /// <example>
    /// <code>
    /// // Convert existing configuration to FlexConfiguration
    /// IConfiguration standardConfig = // ... obtained from DI or builder
    /// var flexConfig = standardConfig.GetFlexConfiguration();
    ///
    /// // Use dynamic access
    /// dynamic config = flexConfig;
    /// var settings = config.MySection.Settings;
    ///
    /// // Still compatible with IConfiguration interface
    /// var section = flexConfig.Configuration.GetSection("MySection");
    /// </code>
    /// </example>
    [UsedImplicitly]
    public static FlexConfiguration GetFlexConfiguration(this IConfiguration configuration) =>
        new(configuration);

    /// <summary>
    /// Retrieves a specific configuration section wrapped in a FlexConfig accessor.
    /// Provides access to a named configuration section through the FlexKit configuration interface,
    /// enabling dynamic access and type conversion for subsections of the configuration hierarchy.
    /// </summary>
    /// <param name="root">The current configuration root to search within.</param>
    /// <param name="name">
    /// The name of the configuration section to retrieve. If null or empty,
    /// returns the root configuration wrapped in FlexConfig.
    /// </param>
    /// <returns>
    /// An <see cref="IFlexConfig"/> instance representing the specified section,
    /// or <c>null</c> if the section is not found.
    /// </returns>
    /// <remarks>
    /// This method enables targeted access to specific configuration sections while
    /// maintaining all FlexKit functionality, including dynamic access and type conversion.
    /// It's particularly useful for:
    ///
    /// <list type="bullet">
    /// <item>Accessing deeply nested configuration sections</item>
    /// <item>Providing section-specific configuration to services or modules</item>
    /// <item>Creating scoped configuration contexts for different application areas</item>
    /// <item>Isolating configuration concerns in modular applications</item>
    /// </list>
    ///
    /// <para>
    /// <strong>Section Resolution Logic:</strong>
    /// <list type="number">
    /// <item>If <paramref name="name"/> is null or empty, wraps the root configuration</item>
    /// <item>Searches for a child section with the specified name (case-insensitive)</item>
    /// <item>Returns null if no matching section is found</item>
    /// <item>Wraps the found section in a FlexConfiguration instance</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Case Sensitivity:</strong>
    /// Section name matching is performed using <see cref="StringComparison.OrdinalIgnoreCase"/>,
    /// making it consistent with standard .NET configuration behavior.
    /// </para>
    ///
    /// <para>
    /// <strong>Dynamic Access on Sections:</strong>
    /// <code>
    /// // Access a specific database configuration section
    /// var dbConfig = configuration.CurrentConfig("Database");
    /// dynamic db = dbConfig;
    /// var connectionString = db.ConnectionString;
    /// var timeout = db.CommandTimeout;
    ///
    /// // Access nested sections
    /// var apiConfig = configuration.CurrentConfig("External").CurrentConfig("ApiSettings");
    /// </code>
    /// </para>
    ///
    /// <para>
    /// <strong>Service Integration Example:</strong>
    /// <code>
    /// public class DatabaseService
    /// {
    ///     public DatabaseService(IConfiguration configuration)
    ///     {
    ///         var dbConfig = configuration.CurrentConfig("Database");
    ///         var connectionString = dbConfig?["ConnectionString"];
    ///         var timeout = dbConfig?["CommandTimeout"].ToType&lt;int&gt;();
    ///     }
    /// }
    /// </code>
    /// </para>
    ///
    /// <para>
    /// <strong>Error Handling:</strong>
    /// <list type="bullet">
    /// <item>Returns null for non-existent sections rather than throwing exceptions</item>
    /// <item>Null-safe operations allow chaining without explicit null checks</item>
    /// <item>Maintains compatibility with standard IConfiguration section behavior</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="root"/> is null.</exception>
    /// <example>
    /// <code>
    /// // Access specific configuration sections
    /// var logging = configuration.CurrentConfig("Logging");
    /// var database = configuration.CurrentConfig("ConnectionStrings");
    /// var features = configuration.CurrentConfig("FeatureFlags");
    ///
    /// // Handle missing sections gracefully
    /// var optional = configuration.CurrentConfig("OptionalSection");
    /// if (optional != null)
    /// {
    ///     dynamic config = optional;
    ///     var setting = config.SomeSetting;
    /// }
    ///
    /// // Root configuration access
    /// var root = configuration.CurrentConfig(); // Same as wrapping entire config
    /// </code>
    /// </example>
    public static IFlexConfig? CurrentConfig(this IConfiguration root, string? name = null)
    {
        var section = !string.IsNullOrEmpty(name)
            ? root.GetChildren().FirstOrDefault(
                s => s.Key.Equals(name, StringComparison.OrdinalIgnoreCase))
            : root;

        return section is null ? null : new FlexConfiguration(section);
    }
}
