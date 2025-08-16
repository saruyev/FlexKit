// <copyright file="HostBuilderExtensions.cs" company="Michael Saruyev">
// Copyright (c) Michael Saruyev. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

using Autofac;
using Autofac.Extensions.DependencyInjection;
using FlexKit.Configuration.Assembly;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FlexKit.Configuration.Core;

/// <summary>
/// Extension methods for IHostBuilder to simplify FlexKit.Configuration integration
/// with generic hosting scenarios including console applications and background services.
/// Provides a streamlined API that automatically configures Autofac as the service provider
/// and integrates FlexConfig with the existing host configuration.
/// </summary>
/// <remarks>
/// This class bridges the gap between FlexKit.Configuration and Microsoft's generic hosting model,
/// enabling developers to easily set up FlexConfig in console applications, background services,
/// and other non-web hosting scenarios with minimal boilerplate code.
///
/// <para>
/// <strong>Key Benefits:</strong>
/// <list type="bullet">
/// <item>Single method call setup for FlexConfig in generic hosting scenarios</item>
/// <item>Automatic Autofac service provider factory configuration</item>
/// <item>Seamless integration with existing Host.CreateDefaultBuilder configuration</item>
/// <item>Preserves all standard .NET hosting configuration sources</item>
/// <item>Support for additional FlexKit-specific configuration sources</item>
/// <item>Automatic assembly module discovery and registration</item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Integration Strategy:</strong>
/// The extension method uses the existing host configuration as the foundation and allows
/// developers to add FlexKit-specific sources on top. This ensures compatibility with
/// existing .NET hosting patterns while enabling FlexKit's enhanced capabilities.
/// </para>
/// </remarks>
public static class HostBuilderExtensions
{
    /// <summary>
    /// Adds FlexKit.Configuration to the host builder with automatic Autofac integration.
    /// This method configures Autofac as the service provider factory, integrates the existing
    /// host configuration with FlexConfig, and sets up additional FlexKit-specific configuration
    /// sources, providing a streamlined setup experience for console applications and background services.
    /// </summary>
    /// <param name="hostBuilder">The host builder to configure with FlexConfig support.</param>
    /// <param name="configure">
    /// Optional configuration action that defines additional FlexKit configuration sources.
    /// The existing host configuration is automatically included as the base, so this
    /// action should focus on adding FlexKit-specific sources like .env files, YAML files, etc.
    /// If not provided, only the existing host configuration will be used.
    /// </param>
    /// <returns>
    /// The same <see cref="IHostBuilder"/> instance to enable method chaining.
    /// </returns>
    /// <remarks>
    /// This method performs the following setup steps automatically:
    ///
    /// <list type="number">
    /// <item>Configures Autofac as the service provider factory using <see cref="AutofacServiceProviderFactory"/></item>
    /// <item>Captures the existing host configuration (from Host.CreateDefaultBuilder)</item>
    /// <item>Sets up FlexConfig using the existing configuration as the base</item>
    /// <item>Applies additional configuration sources specified in the configure action</item>
    /// <item>Registers assembly modules for automatic dependency injection discovery</item>
    /// <item>Registers all necessary FlexKit configuration components for dependency injection</item>
    /// </list>
    ///
    /// <para>
    /// <strong>Configuration Source Hierarchy:</strong>
    /// The final configuration follows this precedence order (highest to lowest):
    /// <list type="number">
    /// <item>Additional sources from configure action (the highest precedence)</item>
    /// <item>Host.CreateDefaultBuilder sources in standard order:
    ///   <list type="bullet">
    ///   <item>Command line arguments</item>
    ///   <item>Environment variables</item>
    ///   <item>User secrets (in Development)</item>
    ///   <item>appsettings.{Environment}.json</item>
    ///   <item>appsettings.json (the lowest precedence)</item>
    ///   </list>
    /// </item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Service Registration:</strong>
    /// The following services are automatically registered and available for injection:
    /// <list type="table">
    /// <listheader>
    /// <term>Service Type</term>
    /// <description>Description</description>
    /// </listheader>
    /// <item>
    /// <term>IConfiguration</term>
    /// <description>Enhanced configuration with both host and FlexKit sources</description>
    /// </item>
    /// <item>
    /// <term>IFlexConfig</term>
    /// <description>FlexKit's enhanced configuration interface with dynamic access</description>
    /// </item>
    /// <item>
    /// <term>dynamic</term>
    /// <description>Dynamic configuration access (same instance as IFlexConfig)</description>
    /// </item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Assembly Module Discovery:</strong>
    /// The method automatically scans and registers Autofac modules from application assemblies,
    /// enabling automatic discovery of dependency injection configurations across the application.
    /// </para>
    ///
    /// <para>
    /// <strong>Compatibility:</strong>
    /// This extension is fully compatible with existing IHostBuilder configurations and
    /// doesn't interfere with other service registrations. It can be used alongside
    /// ConfigureServices, ConfigureLogging, and other standard hosting extensions.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="hostBuilder"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when configuration building fails due to invalid sources or settings.</exception>
    /// <example>
    /// <code>
    /// // Console application with FlexConfig - minimal setup (existing host config only)
    /// var builder = Host.CreateDefaultBuilder(args);
    /// builder.AddFlexConfig();  // No additional sources, just FlexKit dynamic access
    ///
    /// var host = builder.Build();
    ///
    /// // Access configuration with FlexKit dynamic access
    /// var flexConfig = host.Services.GetRequiredService&lt;IFlexConfig&gt;();
    /// dynamic config = flexConfig;
    /// var apiKey = config.Api.Key;  // From appsettings.json or environment variables
    ///
    /// await host.RunAsync();
    /// </code>
    ///
    /// <code>
    /// // Console application with additional FlexKit sources
    /// var builder = Host.CreateDefaultBuilder(args);
    /// builder.AddFlexConfig(config => config
    ///     .AddDotEnvFile(".env", optional: true));
    ///
    /// var host = builder.Build();
    ///
    /// // Access configuration with values from host + .env
    /// var flexConfig = host.Services.GetRequiredService&lt;IFlexConfig&gt;();
    /// dynamic config = flexConfig;
    /// var apiKey = config.Api.Key;  // From any source
    ///
    /// await host.RunAsync();
    /// </code>
    ///
    /// <code>
    /// // Background service with FlexConfig and multiple sources
    /// var builder = Host.CreateDefaultBuilder(args);
    /// builder.ConfigureServices(services => {
    ///     services.AddHostedService&lt;MyBackgroundService&gt;();
    /// });
    /// builder.AddFlexConfig(config => config
    ///     .AddDotEnvFile(".env", optional: true)
    ///     .AddYamlFile("features.yaml", optional: true)
    ///     .AddAwsParameterStore("/myapp/config", optional: true));
    ///
    /// var host = builder.Build();
    /// await host.RunAsync();
    ///
    /// // In MyBackgroundService constructor
    /// public MyBackgroundService(IFlexConfig config)
    /// {
    ///     dynamic settings = config;
    ///     var interval = settings.Processing.IntervalMs; // From any source
    ///     var awsRegion = settings.Aws.Region;           // From Parameter Store
    /// }
    /// </code>
    ///
    /// <code>
    /// // Advanced setup with conditional configuration
    /// var builder = Host.CreateDefaultBuilder(args);
    /// builder.AddFlexConfig(config =>
    /// {
    ///     config.AddDotEnvFile(".env", optional: true);
    ///
    ///     if (builder.Environment.IsDevelopment())
    ///     {
    ///         config.AddYamlFile("development.yaml", optional: true);
    ///     }
    ///
    ///     if (builder.Environment.IsProduction())
    ///     {
    ///         config.AddAwsParameterStore("/prod/myapp", optional: false);
    ///     }
    /// });
    ///
    /// var host = builder.Build();
    /// await host.RunAsync();
    /// </code>
    /// </example>
    [UsedImplicitly]
    public static IHostBuilder AddFlexConfig(
        this IHostBuilder hostBuilder,
        Action<FlexConfigurationBuilder>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(hostBuilder);

        return hostBuilder
            .ConfigureServices((_, services) => services.AddAutofac())
            .UseServiceProviderFactory(new AutofacServiceProviderFactory())
            .ConfigureContainer<ContainerBuilder>((context, containerBuilder) =>
                ConfigureContainer(
                    new ConfigContext
                    {
                        Configure = configure,
                        ContainerBuilder = containerBuilder,
                        Configuration = context.Configuration,
                        Environment = context.HostingEnvironment
                    }));
    }

    /// <summary>
    /// Adds FlexKit.Configuration to the host builder with support for both FlexConfig and ContainerBuilder customization.
    /// This overload provides maximum flexibility by allowing customization of both the configuration builder
    /// and the Autofac container builder in a single call.
    /// </summary>
    /// <param name="hostBuilder">The host builder to configure with FlexConfig support.</param>
    /// <param name="configure">
    /// Configuration action that receives both FlexConfigurationBuilder and ContainerBuilder,
    /// allowing customization of configuration sources and dependency registrations in one place.
    /// </param>
    /// <returns>
    /// The same <see cref="IHostBuilder"/> instance to enable method chaining.
    /// </returns>
    /// <remarks>
    /// This overload is useful when you need to customize both configuration and dependency injection
    /// without having to use separate ConfigureContainer calls. The configuration builder is set up
    /// with automatic JSON file detection before calling your configure action.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="hostBuilder"/> or <paramref name="configure"/> is null.</exception>
    /// <example>
    /// <code>
    /// // Single configuration action for both config and DI
    /// var builder = Host.CreateDefaultBuilder(args);
    /// builder.AddFlexConfig((configBuilder, containerBuilder) =>
    /// {
    ///     // Configure additional config sources
    ///     configBuilder.AddDotEnvFile(".env", optional: true);
    ///
    ///     // Register additional services
    ///     containerBuilder.RegisterType&lt;MyService&gt;().AsImplementedInterfaces();
    ///     containerBuilder.RegisterModule&lt;MyModule&gt;();
    /// });
    /// </code>
    /// </example>
    [UsedImplicitly]
    public static IHostBuilder AddFlexConfig(
        this IHostBuilder hostBuilder,
        Action<FlexConfigurationBuilder, ContainerBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(hostBuilder);
        ArgumentNullException.ThrowIfNull(configure);

        return hostBuilder
            .ConfigureServices((_, services) => services.AddAutofac())
            .UseServiceProviderFactory(new AutofacServiceProviderFactory())
            .ConfigureContainer<ContainerBuilder>((context, containerBuilder) =>
                ConfigureContainer(
                    new ConfigContext
                    {
                        ExtendedConfigure = configure,
                        ContainerBuilder = containerBuilder,
                        Configuration = context.Configuration,
                        Environment = context.HostingEnvironment
                    }));
    }

    /// <summary>
    /// Adds FlexKit.Configuration to the host application builder with automatic Autofac integration.
    /// This method configures Autofac as the service provider factory, integrates the existing
    /// host configuration with FlexConfig, and sets up additional FlexKit-specific configuration
    /// sources, providing a streamlined setup experience for modern hosting scenarios including
    /// ASP.NET Core applications and other IHostApplicationBuilder-based applications.
    /// </summary>
    /// <param name="builder">The host application builder to configure with FlexConfig support.</param>
    /// <param name="configure">
    /// Optional configuration action that defines additional FlexKit configuration sources.
    /// The existing host configuration is automatically included as the base, so this
    /// action should focus on adding FlexKit-specific sources like .env files, YAML files,
    /// AWS Parameter Store, Azure Key Vault, etc. If not provided, only the existing host
    /// configuration will be used.
    /// </param>
    /// <returns>
    /// The same <see cref="IHostApplicationBuilder"/> instance to enable method chaining.
    /// </returns>
    /// <remarks>
    /// This method performs the following setup steps automatically:
    ///
    /// <list type="number">
    /// <item>Registers Autofac services with the built-in service collection using <see cref="ServiceCollectionExtensions.AddAutofac"/></item>
    /// <item>Configures Autofac as the service provider factory using <see cref="AutofacServiceProviderFactory"/></item>
    /// <item>Captures the existing host configuration from <see cref="IHostApplicationBuilder.Configuration"/></item>
    /// <item>Sets up FlexConfig using the existing configuration as the base</item>
    /// <item>Applies additional configuration sources specified in the configure action</item>
    /// <item>Registers assembly modules for automatic dependency injection discovery</item>
    /// <item>Registers all necessary FlexKit configuration components for dependency injection</item>
    /// </list>
    ///
    /// <para>
    /// <strong>Configuration Source Hierarchy:</strong>
    /// The final configuration follows this precedence order (highest to lowest):
    /// <list type="number">
    /// <item>Additional sources from configure action (the highest precedence)</item>
    /// <item>Host application builder sources in standard order:
    ///   <list type="bullet">
    ///   <item>Command line arguments</item>
    ///   <item>Environment variables</item>
    ///   <item>User secrets (in Development)</item>
    ///   <item>appsettings.{Environment}.json</item>
    ///   <item>appsettings.json (the lowest precedence)</item>
    ///   </list>
    /// </item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Service Registration:</strong>
    /// The following services are automatically registered and available for injection:
    /// <list type="table">
    /// <listheader>
    /// <term>Service Type</term>
    /// <description>Description</description>
    /// </listheader>
    /// <item>
    /// <term>IConfiguration</term>
    /// <description>Enhanced configuration with both host and FlexKit sources</description>
    /// </item>
    /// <item>
    /// <term>IFlexConfig</term>
    /// <description>FlexKit's enhanced configuration interface with dynamic access</description>
    /// </item>
    /// <item>
    /// <term>dynamic</term>
    /// <description>Dynamic configuration access (same instance as IFlexConfig)</description>
    /// </item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Assembly Module Discovery:</strong>
    /// The method automatically scans and registers Autofac modules from application assemblies,
    /// enabling automatic discovery of dependency injection configurations across the application.
    /// This follows the same pattern as the <see cref="IHostBuilder"/> extension method.
    /// </para>
    ///
    /// <para>
    /// <strong>Compatibility with IHostApplicationBuilder:</strong>
    /// This extension is designed to work with modern .NET hosting patterns, including:
    /// <list type="bullet">
    /// <item>ASP.NET Core applications using WebApplicationBuilder</item>
    /// <item>Generic host applications using HostApplicationBuilder</item>
    /// <item>Other hosting scenarios implementing <see cref="IHostApplicationBuilder"/></item>
    /// </list>
    /// The method provides the same functionality as the <see cref="IHostBuilder"/> extension
    /// but with direct integration into the <see cref="IHostApplicationBuilder"/> pattern.
    /// </para>
    ///
    /// <para>
    /// <strong>Differences from IHostBuilder Version:</strong>
    /// Unlike the <see cref="IHostBuilder"/> extension method which uses callback patterns,
    /// this method works with the direct access model of <see cref="IHostApplicationBuilder"/>:
    /// <list type="bullet">
    /// <item>Uses <see cref="IHostApplicationBuilder.Configuration"/> directly instead of <see cref="HostBuilderContext.Configuration"/></item>
    /// <item>Registers Autofac services with <see cref="IHostApplicationBuilder.Services"/> before container configuration</item>
    /// <item>Explicitly passes <see cref="AutofacServiceProviderFactory"/> to <see cref="IHostApplicationBuilder.ConfigureContainer{TContainerBuilder}(IServiceProviderFactory{TContainerBuilder}, Action{TContainerBuilder}?)"/></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when configuration building fails due to invalid sources or settings.</exception>
    /// <example>
    /// <code>
    /// // ASP.NET Core application with FlexConfig - minimal setup
    /// var builder = WebApplication.CreateBuilder(args);
    /// builder.AddFlexConfig();  // No additional sources, just FlexKit dynamic access
    ///
    /// var app = builder.Build();
    ///
    /// // Access configuration with FlexKit dynamic access
    /// var flexConfig = app.Services.GetRequiredService&lt;IFlexConfig&gt;();
    /// dynamic config = flexConfig;
    /// var apiKey = config.Api.Key;  // From appsettings.json or environment variables
    ///
    /// app.Run();
    /// </code>
    ///
    /// <code>
    /// // ASP.NET Core application with additional FlexKit sources
    /// var builder = WebApplication.CreateBuilder(args);
    /// builder.AddFlexConfig(config => config
    ///     .AddDotEnvFile(".env", optional: true)
    ///     .AddYamlFile("features.yaml", optional: true)
    ///     .AddAwsParameterStore("/myapp/", optional: true));
    ///
    /// var app = builder.Build();
    ///
    /// // Configuration now includes data from all sources
    /// var flexConfig = app.Services.GetRequiredService&lt;IFlexConfig&gt;();
    /// dynamic settings = flexConfig;
    /// var dbConnection = settings.Database.ConnectionString; // From any source
    /// var awsConfig = settings.Aws.Region;                   // From Parameter Store
    ///
    /// app.Run();
    /// </code>
    ///
    /// <code>
    /// // Generic host application with FlexConfig
    /// var builder = Host.CreateApplicationBuilder(args);
    /// builder.AddFlexConfig(config => config
    ///     .AddDotEnvFile(".env", optional: true)
    ///     .AddYamlFile("worker-config.yaml", optional: true));
    ///
    /// builder.Services.AddHostedService&lt;MyWorkerService&gt;();
    ///
    /// var host = builder.Build();
    /// await host.RunAsync();
    ///
    /// // In MyWorkerService constructor
    /// public MyWorkerService(IFlexConfig config)
    /// {
    ///     dynamic settings = config;
    ///     var workerInterval = settings.Worker.IntervalSeconds;
    ///     var enableMetrics = settings.Features.EnableMetrics;
    /// }
    /// </code>
    ///
    /// <code>
    /// // Conditional configuration based on environment
    /// var builder = WebApplication.CreateBuilder(args);
    /// builder.AddFlexConfig(config =>
    /// {
    ///     config.AddDotEnvFile(".env", optional: true);
    ///
    ///     if (builder.Environment.IsDevelopment())
    ///     {
    ///         config.AddYamlFile("development.yaml", optional: true);
    ///     }
    ///
    ///     if (builder.Environment.IsProduction())
    ///     {
    ///         config.AddAwsParameterStore("/prod/myapp", optional: false)
    ///               .AddAwsSecretsManager(new[] { "prod-myapp-secrets" });
    ///     }
    /// });
    ///
    /// var app = builder.Build();
    /// app.Run();
    /// </code>
    /// </example>
    [UsedImplicitly]
    public static IHostApplicationBuilder AddFlexConfig(
        this IHostApplicationBuilder builder,
        Action<FlexConfigurationBuilder>? configure = null)
    {
        builder.Services.AddAutofac();
        builder.ConfigureContainer(
            new AutofacServiceProviderFactory(),
            containerBuilder =>
            {
                ConfigureContainer(
                    new ConfigContext
                    {
                        Configure = configure,
                        ContainerBuilder = containerBuilder,
                        Configuration = builder.Configuration,
                        Environment = builder.Environment
                    });
            });

        return builder;
    }

    /// <summary>
    /// Adds FlexKit.Configuration to the host application builder with support for both FlexConfig and ContainerBuilder customization.
    /// This overload provides maximum flexibility by allowing customization of both the configuration builder
    /// and the Autofac container builder in a single call.
    /// </summary>
    /// <param name="builder">The host application builder to configure with FlexConfig support.</param>
    /// <param name="configure">
    /// Configuration action that receives both FlexConfigurationBuilder and ContainerBuilder,
    /// allowing customization of configuration sources and dependency registrations in one place.
    /// </param>
    /// <returns>
    /// The same <see cref="IHostApplicationBuilder"/> instance to enable method chaining.
    /// </returns>
    /// <remarks>
    /// This overload is useful when you need to customize both configuration and dependency injection
    /// without having to use separate ConfigureContainer calls. The configuration builder is set up
    /// with automatic JSON file detection before calling your configure action.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="configure"/> is null.</exception>
    /// <example>
    /// <code>
    /// // Single configuration action for both config and DI
    /// var builder = WebApplication.CreateBuilder(args);
    /// builder.AddFlexConfig((configBuilder, containerBuilder) =>
    /// {
    ///     // Configure additional config sources
    ///     configBuilder.AddDotEnvFile(".env", optional: true);
    ///
    ///     // Register additional services
    ///     containerBuilder.RegisterType&lt;MyService&gt;().AsImplementedInterfaces();
    ///     containerBuilder.RegisterModule&lt;MyModule&gt;();
    /// });
    /// </code>
    /// </example>
    [UsedImplicitly]
    public static IHostApplicationBuilder AddFlexConfig(
        this IHostApplicationBuilder builder,
        Action<FlexConfigurationBuilder, ContainerBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        builder.Services.AddAutofac();
        builder.ConfigureContainer(
            new AutofacServiceProviderFactory(),
            containerBuilder =>
            {
                ConfigureContainer(
                    new ConfigContext
                    {
                        ExtendedConfigure = configure,
                        ContainerBuilder = containerBuilder,
                        Configuration = builder.Configuration,
                        Environment = builder.Environment
                    });
            });

        return builder;
    }

    /// <summary>
    /// Configures the Autofac container with FlexKit.Configuration integration.
    /// This private method handles the actual container setup, including FlexConfig registration
    /// with existing host configuration and assembly module discovery.
    /// </summary>
    /// <param name="configContext">
    /// The current configuration context holder.
    /// </param>
    /// <remarks>
    /// This method performs the core integration logic for FlexKit.Configuration in generic hosting scenarios.
    /// It is separated into a private method to maintain clean separation of concerns and enable
    /// easier testing and maintainability.
    ///
    /// <para>
    /// <strong>Configuration Integration Process:</strong>
    /// <list type="number">
    /// <item>Registers FlexConfig with the container using the existing host configuration as foundation</item>
    /// <item>Applies any additional configuration sources specified by the caller</item>
    /// <item>Scans and registers Autofac modules from application assemblies</item>
    /// <item>Ensures proper service lifetimes and dependency injection setup</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Host Configuration Integration:</strong>
    /// The method uses UseExistingConfiguration to preserve all configuration sources
    /// that were set up by Host.CreateDefaultBuilder, including appsettings.json files,
    /// environment variables, user secrets, and command line arguments. This ensures
    /// full compatibility with standard .NET hosting patterns.
    /// </para>
    ///
    /// <para>
    /// <strong>Assembly Module Registration:</strong>
    /// Automatically discovers and registers Autofac modules from the application's
    /// assemblies, enabling modular dependency injection configuration across the application.
    /// This follows the convention-over-configuration principle for DI setup.
    /// </para>
    /// </remarks>
    private static void ConfigureContainer(ConfigContext configContext)
    {
        // Register FlexConfig with the container, integrating host configuration and additional sources
        configContext.ContainerBuilder.AddFlexConfig(config =>
        {
            // Start with the existing host configuration as the base (the lowest precedence)
            config.UseExistingConfiguration(configContext.Configuration);

            AddAutomaticJsonFiles(config, configContext.Environment);

            // Apply additional FlexKit-specific configuration sources if provided
            // These sources will have higher precedence than the host configuration
            configContext.Configure?.Invoke(config);
            configContext.ExtendedConfigure?.Invoke(config, configContext.ContainerBuilder);
        });

        // Register assembly modules for automatic dependency injection discovery
        // Scans application assemblies for Autofac modules and registers them
        configContext.ContainerBuilder.AddModules(configContext.Configuration);
    }

    /// <summary>
    /// Automatically detects and adds appsettings.json and environment-specific JSON files
    /// to the FlexConfigurationBuilder if they exist in the application directory.
    /// </summary>
    /// <param name="config">The FlexConfigurationBuilder to add JSON sources to.</param>
    /// <param name="environment">The hosting environment for determining environment-specific files.</param>
    /// <remarks>
    /// This method implements the automatic JSON file detection logic:
    /// <list type="bullet">
    /// <item>Checks for appsettings.json and adds it if found (optional: true)</item>
    /// <item>Checks for appsettings.{Environment}.json and adds it if found (optional: true)</item>
    /// <item>Uses the current working directory as the base path for file detection</item>
    /// <item>Files are added with reloadOnChange: true for development convenience</item>
    /// </list>
    /// The files are added with low precedence, so they won't override existing host configuration
    /// but will be available for FlexKit's dynamic access capabilities.
    /// </remarks>
    private static void AddAutomaticJsonFiles(
        FlexConfigurationBuilder config,
        IHostEnvironment? environment)
    {
        // Check for appsettings.json
        var appSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
        if (File.Exists(appSettingsPath))
        {
            config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        }

        // Check for an environment-specific appsettings file
        var environmentAppSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), $"appsettings.{environment?.EnvironmentName}.json");
        if (!File.Exists(environmentAppSettingsPath))
        {
            return;
        }

        config.AddJsonFile($"appsettings.{environment?.EnvironmentName}.json", optional: true, reloadOnChange: true);
    }

    /// <summary>
    /// The current configuration context holder.
    /// </summary>
    private sealed class ConfigContext
    {
        /// <summary>
        /// The existing configuration and environment information.
        /// Provides access to the configuration built by Host.CreateDefaultBuilder.
        /// </summary>
        public required IConfiguration Configuration { get; init; }

        /// <summary>
        /// The hosting environment information used for automatic JSON file detection.
        /// </summary>
        public IHostEnvironment? Environment { get; init; }

        /// <summary>
        /// Optional configuration action for adding additional FlexKit-specific sources.
        /// If null, only the existing host configuration will be integrated.
        /// </summary>
        public Action<FlexConfigurationBuilder>? Configure { get; init; }

        /// <summary>
        /// Configuration action that receives both FlexConfigurationBuilder and ContainerBuilder,
        /// allowing customization of configuration sources and dependency registrations in one place.
        /// </summary>
        public Action<FlexConfigurationBuilder, ContainerBuilder>? ExtendedConfigure { get; init; }

        /// <summary>
        /// The Autofac container builder to configure with FlexKit services.
        /// </summary>
        public required ContainerBuilder ContainerBuilder { get; init; }
    }
}
