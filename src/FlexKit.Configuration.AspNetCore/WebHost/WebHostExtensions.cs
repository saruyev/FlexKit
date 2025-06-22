// <copyright file="WebHostExtensions.cs" company="Michael Saruyev">
// Copyright (c) Michael Saruyev. All rights reserved.
// </copyright>

using JetBrains.Annotations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace FlexKit.Configuration.AspNetCore.WebHost;

/// <summary>
/// Extension methods for web host configuration.
/// </summary>
public static class WebHostExtensions
{
    /// <summary>
    /// Applies default settings to web host builder.
    /// </summary>
    /// <param name="builder">Web host builder instance.</param>
    /// <param name="jsonFiles">Custom JSON configuration files.</param>
    /// <returns>Modified instance of the web host builder.</returns>
    [UsedImplicitly]
    public static IWebHostBuilder PrepareWebHost(this IWebHostBuilder builder, params string[] jsonFiles)
    {
        builder
            .UseContentRoot(Directory.GetCurrentDirectory())
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config
                    .SetBasePath(hostingContext.HostingEnvironment.ContentRootPath)
                    .AddEnvironmentVariables();

                if (jsonFiles is not { Length: > 0 })
                {
                    return;
                }

                foreach (var file in jsonFiles)
                {
                    var fileName = file.EndsWith(".json", StringComparison.InvariantCulture) ? file : $"{file}.json";
                    config.AddJsonFile(fileName);
                }
            })
            .CaptureStartupErrors(true)
            .ConfigureServices((_, services) =>
            {
                // Configure Autofac as the service provider factory
                services.AddAutofac();
            })
            .UseFlexConfig(useEnvironment: true)
            .UseSetting("detailedErrors", "true");

        return builder;
    }

    /// <summary>
    /// Adds the configuration definitions to a web application configuration builder.
    /// </summary>
    /// <param name="builder">The web application configuration builder.</param>
    /// <param name="name">The configuration file name.</param>
    /// <param name="useEnvironment">Indicates whether to load a configuration file associated with the current environment.</param>
    /// <returns>The extended web configuration builder instance.</returns>
    [UsedImplicitly]
    public static IWebHostBuilder UseFlexConfig(
        this IWebHostBuilder builder,
        string name = "config",
        bool useEnvironment = false) =>
        builder.ConfigureAppConfiguration((context, config) =>
        {
            var environment = useEnvironment ? context.HostingEnvironment.EnvironmentName : null;
            config.AddFlexConfig(name, environment);
        });

    /// <summary>
    /// Adds the configuration definitions to the application configuration builder.
    /// </summary>
    /// <param name="builder">The application configuration builder.</param>
    /// <param name="name">The configuration file name.</param>
    /// <param name="environment">The current build environment.</param>
    /// <returns>The extended configuration builder instance.</returns>
    [UsedImplicitly]
    public static IConfigurationBuilder AddFlexConfig(
        this IConfigurationBuilder builder,
        string name = "config",
        string? environment = null) =>
        string.IsNullOrEmpty(environment)
            ? builder.AddJsonFile($"{name}.json", true, true)
            : builder.AddJsonFile($"{name}.{environment}.json", true, true);
}
