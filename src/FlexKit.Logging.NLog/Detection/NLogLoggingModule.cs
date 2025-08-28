using Autofac;
using FlexKit.Logging.Configuration;
using FlexKit.Logging.Core;
using FlexKit.Logging.Formatting.Translation;
using FlexKit.Logging.NLog.Core;
using FlexKit.Logging.NLog.Translation;
using JetBrains.Annotations;
using NLog;
using ILogger = NLog.ILogger;

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
public class NLogLoggingModule : Module
{
    /// <summary>
    /// Configures the container with NLog-based logging services.
    /// Registers all components needed to replace MEL with NLog infrastructure.
    /// </summary>
    /// <param name="builder">The Autofac container builder to register components with.</param>
    protected override void Load(ContainerBuilder builder)
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
}
