// <copyright file="ContainerBuilderConfigurationExtensions.cs" company="Michael Saruyev">
// Copyright (c) Michael Saruyev. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

using Autofac;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;

namespace FlexKit.Configuration.Core;

/// <summary>
/// Extension methods for ContainerBuilder to register strongly typed configuration objects.
/// Enables automatic binding and registration of configuration classes for dependency injection.
/// </summary>
/// <remarks>
/// This class extends Autofac's ContainerBuilder with configuration-specific registration methods
/// that automatically bind configuration sections to strongly typed objects and register them
/// for dependency injection. This provides seamless integration between FlexKit configuration
/// and Autofac's dependency injection container.
///
/// <para>
/// <strong>Usage Pattern:</strong>
/// These extensions are designed to be used after setting up FlexConfig, providing a clean
/// separation between configuration building and dependency injection registration:
/// <code>
/// containerBuilder.AddFlexConfig(config => config
///     .AddJsonFile("appsettings.json")
///     .AddEnvironmentVariables())
///     .RegisterConfig&lt;DatabaseConfig&gt;("Database")
///     .RegisterConfig&lt;ApiConfig&gt;("External:Api")
///     .RegisterConfig&lt;MyAppConfig&gt;(); // Binds to root
/// </code>
/// </para>
///
/// <para>
/// <strong>Integration Benefits:</strong>
/// <list type="bullet">
/// <item>Automatic constructor and property injection of configuration objects</item>
/// <item>Singleton lifetime management for configuration instances</item>
/// <item>Integration with IOptions pattern for validation and change notifications</item>
/// <item>Type-safe configuration access throughout the application</item>
/// </list>
/// </para>
/// </remarks>
public static class ContainerBuilderConfigurationExtensions
{
    /// <summary>
    /// Registers a strongly typed configuration object bound to the root configuration.
    /// Creates a singleton registration that binds the entire configuration root to the specified type.
    /// </summary>
    /// <typeparam name="T">The configuration class type to register. Must have a parameterless constructor.</typeparam>
    /// <param name="builder">The container builder to register the configuration with.</param>
    /// <returns>The same container builder instance to enable method chaining.</returns>
    /// <remarks>
    /// This method binds the entire configuration root to the specified type using
    /// <see cref="Microsoft.Extensions.Configuration.ConfigurationBinder.Get{T}(IConfiguration)"/>.
    /// The resulting instance is registered as a singleton and can be injected into services
    /// via constructor parameters or properties.
    ///
    /// <para>
    /// <strong>Binding Behavior:</strong>
    /// <list type="bullet">
    /// <item>Uses Microsoft's configuration binding to map configuration keys to object properties</item>
    /// <item>Supports nested objects, collections, and dictionaries</item>
    /// <item>Missing configuration values result in default property values</item>
    /// <item>Invalid configuration values throw exceptions during binding</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Registration Details:</strong>
    /// <list type="bullet">
    /// <item>Registered as singleton (single instance per container)</item>
    /// <item>Available for both constructor and property injection</item>
    /// <item>Resolved lazily when first requested</item>
    /// <item>Depends on IConfiguration being registered in the container</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Example Configuration Class:</strong>
    /// <code>
    /// public class AppConfig
    /// {
    ///     public string ApplicationName { get; set; } = string.Empty;
    ///     public DatabaseConfig Database { get; set; } = new();
    ///     public List&lt;string&gt; AllowedHosts { get; set; } = new();
    /// }
    ///
    /// // Registration
    /// builder.RegisterConfig&lt;AppConfig&gt;();
    ///
    /// // Usage in service
    /// public class MyService
    /// {
    ///     public MyService(AppConfig config)
    ///     {
    ///         var appName = config.ApplicationName;
    ///         var dbConnection = config.Database.ConnectionString;
    ///     }
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when IConfiguration is not registered in the container.</exception>
    /// <exception cref="FormatException">Thrown when configuration values cannot be converted to the target property types.</exception>
    /// <example>
    /// <code>
    /// // Register application-wide configuration
    /// containerBuilder.AddFlexConfig(config => config
    ///     .AddJsonFile("appsettings.json")
    ///     .AddEnvironmentVariables())
    ///     .RegisterConfig&lt;ApplicationConfig&gt;();
    ///
    /// // The configuration will be available for injection
    /// public class EmailService
    /// {
    ///     private readonly ApplicationConfig _config;
    ///
    ///     public EmailService(ApplicationConfig config)
    ///     {
    ///         _config = config;
    ///     }
    ///
    ///     public void SendEmail()
    ///     {
    ///         var smtpSettings = _config.Email.Smtp;
    ///         // Use configuration...
    ///     }
    /// }
    /// </code>
    /// </example>
    [UsedImplicitly]
    public static ContainerBuilder RegisterConfig<T>(this ContainerBuilder builder)
        where T : class, new()
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Register(c =>
        {
            var configuration = c.Resolve<IConfiguration>();
            return configuration.Get<T>() ?? new T();
        }).As<T>().SingleInstance();

        return builder;
    }

    /// <summary>
    /// Registers a strongly typed configuration object bound to a specific configuration section.
    /// Creates a singleton registration that binds a named configuration section to the specified type.
    /// </summary>
    /// <typeparam name="T">The configuration class type to register. Must have a parameterless constructor.</typeparam>
    /// <param name="builder">The container builder to register the configuration with.</param>
    /// <param name="sectionPath">
    /// The hierarchical path to the configuration section to bind (e.g., "Database", "External:Api").
    /// Uses colon notation for nested sections.
    /// </param>
    /// <returns>The same container builder instance to enable method chaining.</returns>
    /// <remarks>
    /// This method binds a specific configuration section to the specified type, allowing
    /// for modular configuration where different parts of the configuration tree are bound
    /// to different strongly typed objects.
    ///
    /// <para>
    /// <strong>Section Path Resolution:</strong>
    /// <list type="bullet">
    /// <item>Uses colon (:) as the hierarchy separator</item>
    /// <item>Case-insensitive section matching (depends on configuration provider)</item>
    /// <item>Non-existent sections result in default object instances</item>
    /// <item>Empty section paths are treated as root configuration</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Configuration Organization Benefits:</strong>
    /// <list type="bullet">
    /// <item>Enables modular configuration design</item>
    /// <item>Reduces coupling between configuration sections</item>
    /// <item>Allows different teams to own different configuration areas</item>
    /// <item>Supports configuration validation at the section level</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Example JSON Configuration:</strong>
    /// <code>
    /// {
    ///   "Database": {
    ///     "ConnectionString": "Server=localhost;Database=MyApp;",
    ///     "CommandTimeout": 30,
    ///     "RetryCount": 3
    ///   },
    ///   "External": {
    ///     "Api": {
    ///       "BaseUrl": "https://api.example.com",
    ///       "ApiKey": "secret-key",
    ///       "Timeout": 5000
    ///     }
    ///   }
    /// }
    ///
    /// // Corresponding registrations:
    /// builder.RegisterConfig&lt;DatabaseConfig&gt;("Database")
    ///        .RegisterConfig&lt;ApiConfig&gt;("External:Api");
    /// </code>
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="sectionPath"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="sectionPath"/> is empty or whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown when IConfiguration is not registered in the container.</exception>
    /// <exception cref="FormatException">Thrown when configuration values cannot be converted to the target property types.</exception>
    /// <example>
    /// <code>
    /// // Register section-specific configurations
    /// containerBuilder.AddFlexConfig(config => config
    ///     .AddJsonFile("appsettings.json")
    ///     .AddEnvironmentVariables())
    ///     .RegisterConfig&lt;DatabaseConfig&gt;("Database")
    ///     .RegisterConfig&lt;ApiConfig&gt;("External:PaymentApi")
    ///     .RegisterConfig&lt;LoggingConfig&gt;("Logging");
    ///
    /// // Each configuration type gets its own section
    /// public class DatabaseService
    /// {
    ///     public DatabaseService(DatabaseConfig dbConfig)
    ///     {
    ///         var connectionString = dbConfig.ConnectionString;
    ///         var timeout = dbConfig.CommandTimeout;
    ///     }
    /// }
    ///
    /// public class PaymentService
    /// {
    ///     public PaymentService(ApiConfig apiConfig)
    ///     {
    ///         var baseUrl = apiConfig.BaseUrl;
    ///         var apiKey = apiConfig.ApiKey;
    ///     }
    /// }
    /// </code>
    /// </example>
    [UsedImplicitly]
    public static ContainerBuilder RegisterConfig<T>(this ContainerBuilder builder, string sectionPath)
        where T : class, new()
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionPath);

        builder.Register(c =>
        {
            var configuration = c.Resolve<IConfiguration>();
            return configuration.GetSection(sectionPath).Get<T>() ?? new T();
        }).As<T>().SingleInstance();

        return builder;
    }

    /// <summary>
    /// Registers multiple strongly typed configuration objects from different sections in a single call.
    /// Provides a convenient way to register several configuration types at once when they follow
    /// a predictable section naming pattern.
    /// </summary>
    /// <param name="builder">The container builder to register the configurations with.</param>
    /// <param name="configurations">
    /// A collection of tuples specifying the type and section path for each configuration to register.
    /// Each tuple contains the configuration type and its corresponding section path.
    /// </param>
    /// <returns>The same container builder instance to enable method chaining.</returns>
    /// <remarks>
    /// This method provides a batch registration mechanism for scenarios where multiple
    /// configuration types need to be registered from different sections. It's particularly
    /// useful for large applications with many configuration areas.
    ///
    /// <para>
    /// <strong>Batch Registration Benefits:</strong>
    /// <list type="bullet">
    /// <item>Reduces repetitive registration code</item>
    /// <item>Centralizes configuration registration logic</item>
    /// <item>Enables dynamic configuration registration scenarios</item>
    /// <item>Maintains consistency across configuration registrations</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Usage Scenarios:</strong>
    /// <list type="bullet">
    /// <item>Large applications with many configuration sections</item>
    /// <item>Modular applications where configuration types are discovered dynamically</item>
    /// <item>Configuration registration based on environment or feature flags</item>
    /// <item>Automated configuration setup from metadata</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="configurations"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when IConfiguration is not registered in the container.</exception>
    /// <exception cref="FormatException">Thrown when configuration values cannot be converted to any of the target types.</exception>
    /// <example>
    /// <code>
    /// // Batch registration of multiple configuration types
    /// var configMappings = new[]
    /// {
    ///     (typeof(DatabaseConfig), "Database"),
    ///     (typeof(ApiConfig), "External:Api"),
    ///     (typeof(CacheConfig), "Cache"),
    ///     (typeof(LoggingConfig), "Logging")
    /// };
    ///
    /// containerBuilder.AddFlexConfig(config => config.AddJsonFile("appsettings.json"))
    ///     .RegisterConfigs(configMappings);
    ///
    /// // Alternative: Dynamic registration based on assembly scanning
    /// var configTypes = Assembly.GetExecutingAssembly()
    ///     .GetTypes()
    ///     .Where(t => t.Name.EndsWith("Config"))
    ///     .Select(t => (t, t.Name.Replace("Config", "")));
    ///
    /// containerBuilder.RegisterConfigs(configTypes);
    /// </code>
    /// </example>
    [UsedImplicitly]
    public static ContainerBuilder RegisterConfigs(this ContainerBuilder builder,
        IEnumerable<(Type ConfigType, string SectionPath)> configurations)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configurations);

        foreach (var (configType, sectionPath) in configurations)
        {
            builder.Register(c =>
            {
                var configuration = c.Resolve<IConfiguration>();
                // ReSharper disable once NullableWarningSuppressionIsUsed - blow up if config type is not compatible
                return configuration.GetSection(sectionPath).Get(configType) ?? Activator.CreateInstance(configType)!;
            }).As(configType).SingleInstance();
        }

        return builder;
    }
}
