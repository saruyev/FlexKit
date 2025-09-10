using Autofac;
using FlexKit.Logging.Configuration;
using FlexKit.Logging.Core;
using FlexKit.Logging.Formatting.Translation;
using FlexKit.Logging.Serilog.Core;
using FlexKit.Logging.Serilog.Translation;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using ILogger = Serilog.ILogger;

namespace FlexKit.Logging.Serilog.Detection;

/// <summary>
/// Autofac module that registers Serilog-based logging components for FlexKit.Logging.
/// Replaces MEL components when "FlexKit.Logging.Serilog" is present.
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
        RegisterSerilogComponents(builder);
        RegisterLoggerFactory(builder);
    }

    /// <summary>
    /// Registers the Serilog-based logging components within the Autofac container.
    /// Configures and replaces the default implementations of logging services
    /// with Serilog-specific components for logging, message translation,
    /// and background processing.
    /// </summary>
    /// <param name="builder">The Autofac container builder to register components with.</param>
    private static void RegisterSerilogComponents(ContainerBuilder builder)
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

    /// <summary>
    /// Registers the ILoggerFactory that bridges ASP.NET Core logging to Serilog.
    /// This ensures all framework logs (Microsoft.AspNetCore.*, etc.) are routed to Serilog.
    /// </summary>
    /// <param name="builder">The Autofac container builder used to register the logger factory.</param>
    private static void RegisterLoggerFactory(ContainerBuilder builder)
    {
        builder.Register(c =>
            {
                var loggingConfig = c.Resolve<LoggingConfig>();

                // Create a logger factory that bridges MEL to Serilog
                var factory = LoggerFactory.Create(loggingBuilder =>
                {
                    // Clear any default providers
                    loggingBuilder.ClearProviders();
                    loggingBuilder.SetMinimumLevel(LogLevel.Trace);

                    // Add the Serilog provider that bridges to our configured Serilog
                    loggingBuilder.AddProvider(new SerilogLoggerProvider(loggingConfig, c.Resolve<ILogger>()));
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
