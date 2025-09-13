using Autofac;
using FlexKit.Logging.Configuration;
using FlexKit.Logging.Core;
using FlexKit.Logging.Formatting.Translation;
using FlexKit.Logging.NLog.Core;
using FlexKit.Logging.NLog.Translation;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using NLog;
using ILogger = NLog.ILogger;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace FlexKit.Logging.NLog.Detection;

/// <summary>
/// Autofac module that registers NLog-based logging components for FlexKit.Logging.
/// Replaces MEL components when FlexKit.Logging.NLog is present.
/// </summary>
/// <remarks>
/// <para>
/// This module follows the same pattern as SerilogLoggingModule, registering NLog-specific
/// implementations of FlexKit's core logging interfaces. When this module is loaded,
/// it replaces the default MEL-based implementations with NLog equivalents.
/// </para>
/// <para>
/// <strong>Component Replacements:</strong>
/// <list type="bullet">
/// <item>DefaultMessageTranslator → NLogMessageTranslator</item>
/// <item>FormattedLogWriter → NLogLogWriter</item>
/// <item>BackgroundLog (Channel-based) → NLogBackgroundLog</item>
/// <item>MEL ILoggerFactory → NLog ILogger</item>
/// </list>
/// </para>
/// <para>
/// <strong>Configuration Integration:</strong>
/// The module automatically detects available NLog targets and configures them
/// based on FlexKit's LoggingConfig. It supports both programmatic and configuration-based
/// target setup through the NLogConfigurationBuilder.
/// </para>
/// </remarks>
[UsedImplicitly]
internal sealed class NLogLoggingModule : Module
{
    /// <summary>
    /// Configures the container with NLog-based logging services.
    /// Registers all components needed to replace MEL with NLog infrastructure.
    /// </summary>
    /// <param name="builder">The Autofac container builder to register components with.</param>
    protected override void Load(ContainerBuilder builder)
    {
        RegisterNLogComponents(builder);
        RegisterLoggerFactory(builder);
    }

    /// <summary>
    /// Registers the NLog-based logging components within the Autofac container.
    /// Configures and replaces the default implementations of logging services
    /// with NLog-specific components for logging, message translation,
    /// and background processing.
    /// </summary>
    /// <param name="builder">The Autofac container builder to register components with.</param>
    private static void RegisterNLogComponents(ContainerBuilder builder)
    {
        // Register NLog message translator (replaces DefaultMessageTranslator)
        builder.RegisterType<NLogMessageTranslator>()
            .As<IMessageTranslator>()
            .SingleInstance();

        // Register NLog configuration builder for dynamic target detection and configuration
        builder.RegisterType<NLogConfigurationBuilder>()
            .SingleInstance();

        // Register the NLog logger instance
        builder.Register(c =>
            {
                var loggingConfig = c.Resolve<LoggingConfig>();
                var configBuilder = c.Resolve<NLogConfigurationBuilder>();

                // Set RequiresSerialization to false for structured logging with NLog.
                // NLog handles object serialization internally through its layout renderers
                loggingConfig.RequiresSerialization = false;

                // Build and apply the NLog configuration
                LogManager.Configuration = configBuilder.BuildConfiguration(loggingConfig);

                // Return the current class logger from LogManager
                // This will use the configured targets and rules
                return LogManager.GetCurrentClassLogger();
            })
            .As<ILogger>()
            .SingleInstance();

        // Register NLog log writer (replaces FormattedLogWriter)
        builder.RegisterType<NLogLogWriter>()
            .As<ILogEntryProcessor>()
            .SingleInstance();

        // Register NLog background log (replaces BackgroundLog with Channel-based queuing)
        builder.RegisterType<NLogBackgroundLog>()
            .As<IBackgroundLog>()
            .SingleInstance();
    }

    /// <summary>
    /// Registers the ILoggerFactory that bridges ASP.NET Core logging to NLog.
    /// This ensures all framework logs (Microsoft.AspNetCore.*, etc.) are routed to NLog.
    /// </summary>
    /// <param name="builder">The Autofac container builder used to register the logger factory.</param>
    private static void RegisterLoggerFactory(ContainerBuilder builder)
    {
        builder.Register(c =>
            {
                var loggingConfig = c.Resolve<LoggingConfig>();

                // Create a logger factory that bridges MEL to NLog
                var factory = LoggerFactory.Create(loggingBuilder =>
                {
                    // Clear any default providers
                    loggingBuilder.ClearProviders();
                    loggingBuilder.SetMinimumLevel(LogLevel.Trace);

                    // Add the NLog provider that bridges to our configured NLog
                    loggingBuilder.AddProvider(new NLogLoggerProvider(loggingConfig));
                });

                // Ensure proper cleanup
                AppDomain.CurrentDomain.ProcessExit += (_, _) => factory.Dispose();

                return factory;
            })
            .As<ILoggerFactory>()
            .SingleInstance();

        // Register generic ILogger<T> for dependency injection
        builder.RegisterGeneric(typeof(Logger<>))
            .As(typeof(ILogger<>))
            .InstancePerDependency();
    }
}
