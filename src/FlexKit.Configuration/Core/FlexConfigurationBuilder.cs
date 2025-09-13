// <copyright file="FlexConfigurationBuilder.cs" company="Michael Saruyev">
// Copyright (c) Michael Saruyev. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

using FlexKit.Configuration.Sources;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Configuration.Memory;

namespace FlexKit.Configuration.Core;

/// <summary>
/// Builder class for constructing FlexConfig instances with multiple configuration sources.
/// Provides a fluent API for adding various configuration sources including JSON files,
/// environment variables, and .env files, then building them into a unified FlexConfig instance.
/// </summary>
/// <remarks>
/// This class implements the builder pattern to provide a clean, fluent interface for
/// setting up complex configuration hierarchies. It serves as the primary way to create
/// FlexConfig instances with multiple configuration sources in the correct precedence order.
///
/// <para>
/// <strong>Builder Pattern Benefits:</strong>
/// <list type="bullet">
/// <item>Fluent method chaining for readable configuration setup</item>
/// <item>Clear separation between configuration building and usage</item>
/// <item>Type-safe configuration source registration</item>
/// <item>Validation of the builder state to prevent misuse</item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Configuration Precedence:</strong>
/// Configuration sources are applied in the order they are added to the builder.
/// Later sources override values from earlier sources for the same keys. Typical
/// precedence order (from lowest to highest priority):
/// <list type="number">
/// <item>Base JSON files (appsettings.json)</item>
/// <item>Environment-specific JSON files (appsettings.{env}.json)</item>
/// <item>.env files</item>
/// <item>Environment variables</item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Thread Safety:</strong>
/// This builder is not thread-safe and should not be used concurrently from multiple threads.
/// Each builder instance should be used to create a single FlexConfig instance.
/// </para>
///
/// <para>
/// <strong>Usage Example:</strong>
/// <code>
/// var flexConfig = new FlexConfigurationBuilder()
///     .AddJsonFile("appsettings.json", optional: false)
///     .AddJsonFile($"appsettings.{environment}.json", optional: true)
///     .AddDotEnvFile(".env")
///     .AddEnvironmentVariables()
///     .Build();
///
/// // Access configuration values
/// dynamic config = flexConfig;
/// var apiKey = config.Api.Key;
/// </code>
/// </para>
/// </remarks>
public class FlexConfigurationBuilder
{
    /// <summary>
    /// Collection of configuration sources that will be used to build the final configuration.
    /// Sources are applied in the order they appear in this list, with later sources
    /// taking precedence over earlier ones for duplicate keys.
    /// </summary>
    private readonly List<IConfigurationSource> _sources = [];

    /// <summary>
    /// Flag indicating whether the Build() method has been called on this builder instance.
    /// Used to prevent modification after building and to enforce single-use semantics.
    /// </summary>
    private bool _isBuilt;

    /// <summary>
    /// Adds a JSON file configuration source to the builder.
    /// JSON files are the most common configuration source and support hierarchical
    /// configuration structures with strong typing support.
    /// </summary>
    /// <param name="path">The file path to the JSON configuration file, relative to the application base directory.</param>
    /// <param name="optional">
    /// Whether the file is optional. If <c>false</c> and the file doesn't exist,
    /// an exception will be thrown during configuration building.
    /// </param>
    /// <param name="reloadOnChange">
    /// Whether the configuration should automatically reload when the file changes.
    /// Useful for development scenarios but should be used cautiously in production.
    /// </param>
    /// <returns>The same builder instance to enable method chaining.</returns>
    /// <remarks>
    /// JSON configuration files support nested objects, arrays, and all JSON data types.
    /// The configuration system automatically flattens the JSON hierarchy using colon
    /// notation (e.g., "Database:ConnectionString").
    ///
    /// <para>
    /// <strong>File Path Resolution:</strong>
    /// Relative paths are resolved relative to the application's base directory
    /// (typically the directory containing the executable). Absolute paths are used as-is.
    /// </para>
    ///
    /// <para>
    /// <strong>JSON Format Support:</strong>
    /// <list type="bullet">
    /// <item>Standard JSON with objects, arrays, and primitive values</item>
    /// <item>Comments are not supported (use // or /* */ style comments will cause parsing errors)</item>
    /// <item>Unicode strings are fully supported</item>
    /// <item>Numbers are parsed according to JSON specification</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Reload Behavior:</strong>
    /// When <paramref name="reloadOnChange"/> is <c>true</c>, the configuration system
    /// monitors the file for changes and automatically reloads the configuration.
    /// This triggers change notifications that can be observed through IOptionsMonitor
    /// and similar change-tracking mechanisms.
    /// </para>
    ///
    /// <para>
    /// <strong>Example JSON Structure:</strong>
    /// <code>
    /// {
    ///   "Database": {
    ///     "ConnectionString": "Server=localhost;Database=MyApp;",
    ///     "CommandTimeout": 30
    ///   },
    ///   "Logging": {
    ///     "LogLevel": {
    ///       "Default": "Information",
    ///       "Microsoft": "Warning"
    ///     }
    ///   },
    ///   "AllowedHosts": "*"
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when called after Build() has been called.</exception>
    /// <example>
    /// <code>
    /// // Required configuration file
    /// builder.AddJsonFile("appsettings.json", optional: false);
    ///
    /// // Optional environment-specific file
    /// builder.AddJsonFile($"appsettings.{env}.json", optional: true);
    ///
    /// // Development file with reload support
    /// builder.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);
    /// </code>
    /// </example>
    public FlexConfigurationBuilder AddJsonFile(
        string path,
        bool optional = true,
        bool reloadOnChange = true) =>
        AddSource(new JsonConfigurationSource
        {
            Path = path,
            Optional = optional,
            ReloadOnChange = reloadOnChange
        });

    /// <summary>
    /// Adds environment variables as a configuration source to the builder.
    /// Environment variables provide a way to override configuration values at runtime
    /// without modifying configuration files, making them ideal for deployment-specific settings.
    /// </summary>
    /// <returns>The same builder instance to enable method chaining.</returns>
    /// <remarks>
    /// Environment variables are typically added last in the configuration hierarchy
    /// to allow them to override all file-based configuration sources. This follows
    /// the common pattern of allowing deployment environments to customize application
    /// behavior without code changes.
    ///
    /// <para>
    /// <strong>Variable Name Mapping:</strong>
    /// Environment variable names are mapped to configuration keys using the following rules:
    /// <list type="bullet">
    /// <item>Double underscores (__) are converted to colons (:) for hierarchical keys</item>
    /// <item>Variable names are case-insensitive on Windows, case-sensitive on Linux/macOS</item>
    /// <item>Leading and trailing whitespace is trimmed from values</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Hierarchical Configuration:</strong>
    /// <code>
    /// // Environment variables
    /// DATABASE__CONNECTIONSTRING=Server=prod;Database=MyApp;
    /// LOGGING__LOGLEVEL__DEFAULT=Warning
    /// API__TIMEOUT=5000
    ///
    /// // Equivalent configuration structure
    /// {
    ///   "Database": {
    ///     "ConnectionString": "Server=prod;Database=MyApp;"
    ///   },
    ///   "Logging": {
    ///     "LogLevel": {
    ///       "Default": "Warning"
    ///     }
    ///   },
    ///   "Api": {
    ///     "Timeout": "5000"
    ///   }
    /// }
    /// </code>
    /// </para>
    ///
    /// <para>
    /// <strong>Security Considerations:</strong>
    /// <list type="bullet">
    /// <item>Environment variables are visible to all processes run by the same user</item>
    /// <item>Consider using secure configuration providers for sensitive data in production</item>
    /// <item>Avoid logging or exposing environment variable values in application output</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Platform Differences:</strong>
    /// <list type="bullet">
    /// <item>Windows: Variable names are case-insensitive</item>
    /// <item>Linux/macOS: Variable names are case-sensitive</item>
    /// <item>Container environments typically use all uppercase naming conventions</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when called after Build() has been called.</exception>
    /// <example>
    /// <code>
    /// // Typical usage - add environment variables last for the highest precedence
    /// var config = new FlexConfigurationBuilder()
    ///     .AddJsonFile("appsettings.json")
    ///     .AddJsonFile($"appsettings.{env}.json", optional: true)
    ///     .AddEnvironmentVariables()  // Highest precedence
    ///     .Build();
    ///
    /// // Setting environment variables (examples)
    /// // PowerShell: $env:DATABASE__CONNECTIONSTRING="Server=localhost"
    /// // Bash: export DATABASE__CONNECTIONSTRING="Server=localhost"
    /// // Docker: -e DATABASE__CONNECTIONSTRING="Server=localhost"
    /// </code>
    /// </example>
    public FlexConfigurationBuilder AddEnvironmentVariables() =>
        AddSource(new EnvironmentVariablesConfigurationSource());

    /// <summary>
    /// Adds a ".env" file configuration source to the builder.
    /// .env files provide a convenient way to manage environment-like configuration
    /// in development scenarios, following the twelve-factor app configuration principles.
    /// </summary>
    /// <param name="path">The file path to the .env file, relative to the application base directory. Defaults to ".env".</param>
    /// <param name="optional">
    /// Whether the .env file is optional. If <c>false</c> and the file doesn't exist,
    /// an exception will be thrown during configuration building.
    /// </param>
    /// <returns>The same builder instance to enable method chaining.</returns>
    /// <remarks>
    /// .env files provide a developer-friendly way to manage configuration that would
    /// normally come from environment variables. They are particularly popular in
    /// containerized applications and follow widely adopted conventions.
    ///
    /// <para>
    /// <strong>.env File Format:</strong>
    /// <list type="bullet">
    /// <item>One key-value pair per line in the format KEY=value</item>
    /// <item>Lines starting with # are treated as comments and ignored</item>
    /// <item>Empty lines are ignored</item>
    /// <item>Values can be quoted with single or double quotes</item>
    /// <item>Basic escape sequences are supported (\n, \t, \r, \\)</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Example .env File:</strong>
    /// <code>
    /// # Database configuration
    /// DATABASE_URL=postgresql://localhost:5432/myapp
    /// DATABASE_POOL_SIZE=10
    ///
    /// # API Keys (use quotes for values with spaces)
    /// API_KEY="your-secret-api-key-here"
    /// EXTERNAL_SERVICE_URL=https://api.example.com
    ///
    /// # Feature flags
    /// ENABLE_CACHING=true
    /// DEBUG_MODE=false
    ///
    /// # Multiline values (limited support)
    /// WELCOME_MESSAGE="Welcome to our application!\nPlease enjoy your stay."
    /// </code>
    /// </para>
    ///
    /// <para>
    /// <strong>Precedence and Overrides:</strong>
    /// .env files typically have lower precedence than actual environment variables,
    /// allowing local development settings to be overridden in deployment environments.
    /// The precedence depends on the order in which sources are added to the builder.
    /// </para>
    ///
    /// <para>
    /// <strong>Security and Best Practices:</strong>
    /// <list type="bullet">
    /// <item>Never commit .env files containing sensitive data to version control</item>
    /// <item>Use .env.example files to document required environment variables</item>
    /// <item>Consider using different .env files for different environments (.env.development, .env.testing)</item>
    /// <item>Validate that all required configuration values are present at an application startup</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Limitations:</strong>
    /// <list type="bullet">
    /// <item>No native support for hierarchical keys (no nested objects like JSON)</item>
    /// <item>All values are strings and require explicit type conversion</item>
    /// <item>Limited escape sequence support compared to JSON</item>
    /// <item>No support for arrays or complex data structures</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when called after Build() has been called.</exception>
    /// <exception cref="FileNotFoundException">
    /// Thrown when the file doesn't exist and <paramref name="optional"/> is <c>false</c>.
    /// </exception>
    /// <example>
    /// <code>
    /// // Standard .env file
    /// builder.AddDotEnvFile(".env", optional: true);
    ///
    /// // Environment-specific .env files
    /// builder.AddDotEnvFile($".env.{environment}", optional: true);
    ///
    /// // Required .env file for development
    /// builder.AddDotEnvFile(".env.development", optional: false);
    /// </code>
    /// </example>
    public FlexConfigurationBuilder AddDotEnvFile(
        string path = ".env",
        bool optional = true) =>
        AddSource(new DotEnvConfigurationSource
        {
            Path = path,
            Optional = optional
        });

    /// <summary>
    /// Adds a custom configuration source to the builder.
    /// This method allows integration of any configuration source that implements
    /// <see cref="IConfigurationSource"/>, providing extensibility for custom scenarios.
    /// </summary>
    /// <param name="source">The configuration source to add. Must not be null.</param>
    /// <returns>The same builder instance to enable method chaining.</returns>
    /// <remarks>
    /// This method provides the fundamental extensibility mechanism for the FlexConfig
    /// system. Any configuration source compatible with the Microsoft.Extensions.Configuration
    /// system can be integrated through this method.
    ///
    /// <para>
    /// <strong>Custom Source Examples:</strong>
    /// <list type="bullet">
    /// <item>Database-backed configuration sources</item>
    /// <item>Remote configuration services (Azure App Configuration, AWS Parameter Store)</item>
    /// <item>Encrypted configuration files</item>
    /// <item>In-memory configuration for testing</item>
    /// <item>Configuration from network endpoints</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Source Ordering:</strong>
    /// Configuration sources are processed in the order they are added to the builder.
    /// Each source can override values from previously added sources, so the order
    /// of AddSource calls determines the final configuration hierarchy.
    /// </para>
    ///
    /// <para>
    /// <strong>Integration with Standard Sources:</strong>
    /// Custom sources can be mixed freely with standard sources (JSON, environment variables, etc.).
    /// The final configuration will be a merged view of all sources according to their precedence order.
    /// </para>
    ///
    /// <para>
    /// <strong>Error Handling:</strong>
    /// Configuration sources that fail to load will cause the Build() method to throw
    /// an exception. Sources that need to handle loading errors gracefully should
    /// implement an appropriate error handling within their IConfigurationProvider implementation.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when called after Build() has been called.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
    /// <example>
    /// <code>
    /// // Add a custom database configuration source
    /// builder.AddSource(new DatabaseConfigurationSource(connectionString));
    ///
    /// // Add in-memory configuration for testing
    /// var memorySource = new MemoryConfigurationSource
    /// {
    ///     InitialData = new Dictionary&lt;string, string&gt;
    ///     {
    ///         ["TestSetting"] = "TestValue",
    ///         ["Database:ConnectionString"] = "InMemoryDatabase"
    ///     }
    /// };
    /// builder.AddSource(memorySource);
    ///
    /// // Add Azure App Configuration
    /// builder.AddSource(new AzureAppConfigurationSource(endpoint, credential));
    /// </code>
    /// </example>
    [UsedImplicitly]
    public FlexConfigurationBuilder AddSource(IConfigurationSource source)
    {
        if (_isBuilt)
        {
            throw new InvalidOperationException("Cannot add sources after Build() has been called");
        }

        _sources.Add(source);
        return this;
    }

    /// <summary>
    /// Adds an existing IConfiguration instance as the base configuration source.
    /// This method allows FlexConfigurationBuilder to build upon an already configured
    /// IConfiguration instance, enabling integration with existing .NET hosting and
    /// configuration setups while still allowing additional FlexKit-specific sources.
    /// </summary>
    /// <param name="configuration">
    /// The existing IConfiguration instance to use as the base configuration.
    /// All key-value pairs from this configuration will be included in the final FlexConfig.
    /// </param>
    /// <returns>The same builder instance to enable method chaining.</returns>
    /// <remarks>
    /// This method is particularly useful for integrating FlexConfig with existing
    /// hosting scenarios where an IConfiguration has already been built with standard
    /// .NET configuration sources (JSON files, environment variables, command line args, etc.).
    ///
    /// <para>
    /// <strong>Usage Pattern:</strong>
    /// The existing configuration acts as the foundation, with any subsequently added
    /// sources taking precedence over values from the existing configuration:
    /// <code>
    /// // In a generic host builder context
    /// builder.ConfigureContainer&lt;ContainerBuilder&gt;((context, containerBuilder) =>
    /// {
    ///     containerBuilder.AddFlexConfig(config => config
    ///         .UseExistingConfiguration(context.Configuration) // Base from host
    ///         .AddDotEnvFile(".env", optional: true)           // FlexKit-specific
    ///         .AddYamlFile("config.yaml", optional: true));    // Additional sources
    /// });
    /// </code>
    /// </para>
    ///
    /// <para>
    /// <strong>Source Precedence:</strong>
    /// The existing configuration is added as the first (lowest precedence) source.
    /// Any sources added after this method call will override values from the
    /// existing configuration for matching keys.
    /// </para>
    ///
    /// <para>
    /// <strong>Implementation Details:</strong>
    /// The method converts the existing IConfiguration into a MemoryConfigurationSource
    /// by enumerating all its key-value pairs. This ensures that all configuration
    /// data is preserved while allowing the FlexConfigurationBuilder to manage
    /// the final configuration composition.
    /// </para>
    ///
    /// <para>
    /// <strong>Performance Considerations:</strong>
    /// The method performs a one-time enumeration of the existing configuration
    /// at builder setup time. The resulting configuration access performance
    /// is equivalent to other FlexKit configuration sources.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configuration"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when called after Build() has been called.</exception>
    /// <example>
    /// <code>
    /// // Basic usage with existing configuration
    /// var existingConfig = new ConfigurationBuilder()
    ///     .AddJsonFile("appsettings.json")
    ///     .AddEnvironmentVariables()
    ///     .Build();
    ///
    /// var flexConfig = new FlexConfigurationBuilder()
    ///     .UseExistingConfiguration(existingConfig)
    ///     .AddDotEnvFile(".env")
    ///     .Build();
    /// </code>
    ///
    /// <code>
    /// // Integration with ASP.NET Core host builder
    /// builder.Host.ConfigureContainer&lt;ContainerBuilder&gt;((context, containerBuilder) =>
    /// {
    ///     containerBuilder.AddFlexConfig(config => config
    ///         .UseExistingConfiguration(context.Configuration)
    ///         .AddYamlFile("features.yaml", optional: true));
    /// });
    /// </code>
    ///
    /// <code>
    /// // Console application with Host.CreateDefaultBuilder
    /// var builder = Host.CreateDefaultBuilder(args);
    /// builder.ConfigureContainer&lt;ContainerBuilder&gt;((context, containerBuilder) =>
    /// {
    ///     containerBuilder.AddFlexConfig(config => config
    ///         .UseExistingConfiguration(context.Configuration)
    ///         .AddDotEnvFile(".env", optional: true));
    /// });
    /// </code>
    /// </example>
    [UsedImplicitly]
    public FlexConfigurationBuilder UseExistingConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        if (_isBuilt)
        {
            throw new InvalidOperationException("Cannot add sources after Build() has been called");
        }

        // Convert the existing IConfiguration to a dictionary for MemoryConfigurationSource
        var configurationData = new Dictionary<string, string?>();

        // Enumerate all configuration key-value pairs
        foreach (var pair in configuration.AsEnumerable())
        {
            configurationData[pair.Key] = pair.Value;
        }

        // Add as a MemoryConfigurationSource (the lowest precedence since it's added first)
        var memorySource = new MemoryConfigurationSource
        {
            InitialData = configurationData
        };

        _sources.Insert(0, memorySource);

        return this;
    }

    /// <summary>
    /// Builds the final FlexConfig instance from all configured sources.
    /// This method creates the underlying IConfiguration, wraps it in a FlexConfiguration,
    /// and prevents further modification of the builder.
    /// </summary>
    /// <returns>
    /// A new <see cref="IFlexConfig"/> instance that provides access to the merged
    /// configuration from all added sources.
    /// </returns>
    /// <remarks>
    /// The Build() method represents the culmination of the builder pattern, transforming
    /// the accumulated configuration sources into a usable configuration object. It performs
    /// the following operations:
    ///
    /// <list type="number">
    /// <item>Validates that the builder hasn't been used before</item>
    /// <item>Creates a new Microsoft.Extensions.Configuration.ConfigurationBuilder</item>
    /// <item>Adds all accumulated sources to the configuration builder</item>
    /// <item>Builds the underlying IConfiguration instance</item>
    /// <item>Wraps the IConfiguration in a FlexConfiguration for enhanced functionality</item>
    /// <item>Marks the builder as used to prevent reuse</item>
    /// </list>
    ///
    /// <para>
    /// <strong>Single-Use Semantics:</strong>
    /// Each builder instance can only be used to create one FlexConfig instance.
    /// Subsequent calls to Build() or attempts to add more sources will throw exceptions.
    /// This design prevents accidental reuse and ensures predictable behavior.
    /// </para>
    ///
    /// <para>
    /// <strong>Configuration Merging:</strong>
    /// All configuration sources are merged into a single, unified configuration tree.
    /// Values from later sources override values from earlier sources when keys conflict.
    /// The merging respects the hierarchical nature of configuration keys.
    /// </para>
    ///
    /// <para>
    /// <strong>Error Propagation:</strong>
    /// Any errors from configuration sources (missing required files, network failures,
    /// parse errors, etc.) will be thrown during the Build() operation. This ensures
    /// that configuration problems are detected early in the application lifecycle.
    /// </para>
    ///
    /// <para>
    /// <strong>Performance Considerations:</strong>
    /// The Build() operation may involve file I/O, network requests, or other potentially
    /// expensive operations depending on the configured sources. It should typically
    /// be called once during application startup rather than repeatedly.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when Build() has already been called on this builder instance.
    /// </exception>
    /// <exception cref="FileNotFoundException">
    /// Thrown when a required configuration file is not found.
    /// </exception>
    /// <exception cref="FormatException">
    /// Thrown when a configuration file has an invalid format (e.g., malformed JSON).
    /// </exception>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the application lacks permission to read configuration files.
    /// </exception>
    /// <example>
    /// <code>
    /// // Complete configuration setup
    /// var flexConfig = new FlexConfigurationBuilder()
    ///     .AddJsonFile("appsettings.json")
    ///     .AddJsonFile($"appsettings.{environment}.json", optional: true)
    ///     .AddDotEnvFile(".env", optional: true)
    ///     .AddEnvironmentVariables()
    ///     .Build(); // Creates the final configuration
    ///
    /// // Use the configuration
    /// dynamic config = flexConfig;
    /// var connectionString = config.Database.ConnectionString;
    ///
    /// // Builder cannot be reused after Build()
    /// // builder.AddJsonFile("another.json"); // This would throw InvalidOperationException
    /// </code>
    /// </example>
    public IFlexConfig Build()
    {
        if (_isBuilt)
        {
            throw new InvalidOperationException("Build() can only be called once");
        }

        _isBuilt = true;

        var configBuilder = new ConfigurationBuilder();
        foreach (var source in _sources)
        {
            configBuilder.Add(source);
        }

        var configuration = configBuilder.Build();
        return new FlexConfiguration(configuration);
    }
}
