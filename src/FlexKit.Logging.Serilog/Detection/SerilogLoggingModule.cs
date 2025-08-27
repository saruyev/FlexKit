using Autofac;
using FlexKit.Logging.Configuration;
using FlexKit.Logging.Core;
using FlexKit.Logging.Formatting.Translation;
using FlexKit.Logging.Serilog.Core;
using FlexKit.Logging.Serilog.Translation;
using JetBrains.Annotations;
using ILogger = Serilog.ILogger;

namespace FlexKit.Logging.Serilog.Detection;

/// <summary>
/// Autofac module that registers Serilog-based logging components for FlexKit.Logging.
/// Replaces MEL components when FlexKit.Logging.Serilog is present.
/// </summary>
[UsedImplicitly]
public class SerilogLoggingModule : Module
{
    /// <summary>
    /// Configures the container with Serilog-based logging services.
    /// </summary>
    /// <param name="builder">The Autofac container builder to register components with.</param>
    protected override void Load(ContainerBuilder builder)
    {
        // Register Serilog message translator (replaces DefaultMessageTranslator)
        builder.RegisterType<SerilogMessageTranslator>()
            .As<IMessageTranslator>()
            .SingleInstance();

        // Register Serilog configuration builder
        builder.RegisterType<SerilogConfigurationBuilder>()
            .SingleInstance();

        // Register the Serilog logger instance
        builder.Register(c =>
        {
            var loggingConfig = c.Resolve<LoggingConfig>();
            var configBuilder = c.Resolve<SerilogConfigurationBuilder>();

            // Set RequiresSerialization to false for object-based logging
            loggingConfig.RequiresSerialization = false;

            // Build and create the Serilog logger
            var loggerConfig = configBuilder.BuildConfiguration(loggingConfig);
            return loggerConfig.CreateLogger();
        })
        .As<ILogger>()
        .SingleInstance();

        // Register Serilog log writer (replaces FormattedLogWriter)
        builder.RegisterType<SerilogLogWriter>()
            .As<ILogEntryProcessor>()
            .SingleInstance();

        // Register Serilog background log (replaces BackgroundLog with Channel)
        builder.RegisterType<SerilogBackgroundLog>()
            .As<IBackgroundLog>()
            .SingleInstance();
    }
}
