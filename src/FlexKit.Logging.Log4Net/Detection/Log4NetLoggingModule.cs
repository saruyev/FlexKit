using System.Diagnostics;
using System.Reflection;
using Autofac;
using FlexKit.Logging.Configuration;
using FlexKit.Logging.Core;
using FlexKit.Logging.Detection;
using FlexKit.Logging.Formatting.Translation;
using FlexKit.Logging.Log4Net.Core;
using JetBrains.Annotations;
using log4net.Core;
using log4net.Repository;
using Microsoft.Extensions.Logging;
using Module = Autofac.Module;

namespace FlexKit.Logging.Log4Net.Detection;

/// <summary>
/// Autofac module that registers Log4Net-based logging components for FlexKit.Logging.
/// Replaces MEL components when FlexKit.Logging.Log4Net is present.
/// </summary>
/// <remarks>
/// <para>
/// This module follows the same pattern as SerilogLoggingModule and NLogLoggingModule, registering
/// Log4Net-specific implementations of FlexKit's core logging interfaces. When this module is loaded,
/// it replaces the default MEL-based implementations with Log4Net equivalents.
/// </para>
/// <para>
/// <strong>Component Replacements:</strong>
/// <list type="bullet">
/// <item>DefaultMessageTranslator → Log4NetMessageTranslator</item>
/// <item>FormattedLogWriter → Log4NetLogWriter</item>
/// <item>BackgroundLog (Channel-based) → Log4NetBackgroundLog (uses FlexKit queue)</item>
/// <item>MEL ILoggerFactory → Log4Net LoggerRepository</item>
/// </list>
/// </para>
/// <para>
/// <strong>Configuration Integration:</strong>
/// The module automatically detects available Log4Net appenders and configures them
/// based on FlexKit's LoggingConfig. It supports both programmatic and configuration-based
/// appender setup through the Log4NetConfigurationBuilder.
/// </para>
/// <para>
/// <strong>Targeting System:</strong>
/// Unlike Serilog and NLog, which use context/filtering, Log4Net uses separate logger instances
/// for targeting. The configuration builder creates loggers named after target types
/// (e.g., "Console", "File", "Debug") that route to specific appenders.
/// </para>
/// </remarks>
[UsedImplicitly]
public class Log4NetLoggingModule : Module
{
    /// <summary>
    /// Configures the container with Log4Net-based logging services.
    /// Registers all components needed to replace MEL with Log4Net infrastructure.
    /// </summary>
    /// <param name="builder">The Autofac container builder to register components with.</param>
    protected override void Load(ContainerBuilder builder)
    {
        RegisterLog4NetComponents(builder);
        RegisterLoggerFactory(builder);
        RegisterBackgroundLogging(builder);
    }

    /// <summary>
    /// Registers components required for configuring Log4Net-based logging services.
    /// Sets up the message translator, configuration builder, logger repository,
    /// and log writer necessary for Log4Net integration.
    /// </summary>
    /// <param name="builder">The Autofac container builder used to register components.</param>
    private static void RegisterLog4NetComponents(ContainerBuilder builder)
    {
        // Register Log4Net message translator (replaces DefaultMessageTranslator)
        builder.RegisterType<DefaultMessageTranslator>()
            .As<IMessageTranslator>()
            .SingleInstance();

        // Register Log4Net configuration builder for dynamic appender detection and configuration
        builder.RegisterType<Log4NetConfigurationBuilder>()
            .SingleInstance();

        // Configure Log4Net and register a repository
        builder.Register(c =>
            {
                var loggingConfig = c.Resolve<LoggingConfig>();
                var configBuilder = c.Resolve<Log4NetConfigurationBuilder>();

                // Set RequiresSerialization to true for string-based logging like MEL
                loggingConfig.RequiresSerialization = true;

                // Build and apply the Log4Net configuration
                return configBuilder.BuildConfiguration(loggingConfig);
            })
            .As<ILoggerRepository>()
            .SingleInstance();

        // Register Log4Net log writer (replaces FormattedLogWriter)
        builder.RegisterType<Log4NetLogWriter>()
            .As<ILogEntryProcessor>()
            .SingleInstance();
    }

    /// <summary>
    /// Registers the ILoggerFactory that bridges ASP.NET Core logging to Log4Net.
    /// This ensures all framework logs (Microsoft.AspNetCore.*, etc.) are routed to Log4Net.
    /// </summary>
    /// <param name="builder">The Autofac container builder used to register the logger factory.</param>
    private static void RegisterLoggerFactory(ContainerBuilder builder)
    {
        builder.Register(c =>
            {
                var loggingConfig = c.Resolve<LoggingConfig>();
                var repository = c.Resolve<ILoggerRepository>();

                // Create a logger factory that bridges MEL to Log4Net
                var factory = LoggerFactory.Create(loggingBuilder =>
                {
                    // Clear any default providers
                    loggingBuilder.ClearProviders();
                    loggingBuilder.SetMinimumLevel(LogLevel.Trace);

                    // Add the Log4Net provider that bridges to our configured Log4Net
                    loggingBuilder.AddProvider(new Log4NetLoggerProvider(repository, loggingConfig));
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

    /// <summary>
    /// Registers the background logging services needed for Log4Net,
    /// which use FlexKit's channel-based queuing mechanism for managing log entries.
    /// </summary>
    /// <param name="builder">The Autofac container builder used to register background logging components.</param>
    private static void RegisterBackgroundLogging(ContainerBuilder builder)
    {
        // Register Log4Net background log (uses FlexKit's Channel-based queuing like MEL)
        builder.RegisterType<BackgroundLog>()
            .AsImplementedInterfaces()
            .SingleInstance();

        builder.RegisterType<BackgroundLoggingService>()
            .AsImplementedInterfaces()
            .AsSelf()
            .SingleInstance()
            .AutoActivate();

        builder.RegisterBuildCallback(container =>
        {
            DisableLog4NetAutoShutdown();
            var backgroundService = container.Resolve<BackgroundLoggingService>();

            // Start the service in the background
            _ = Task.Run(() => LoggingInfrastructureExtensions.RunBackgroundServiceAsync(backgroundService));

            // Ensure logs are flushed on process exit
            AppDomain.CurrentDomain.ProcessExit +=
                (_, _) =>
                {
                    LoggingInfrastructureExtensions.FlushLogsOnExit(backgroundService);
                    LoggerManager.Shutdown();
                };
        });
    }

    /// <summary>
    /// Disables log4net's automatic ProcessExit shutdown handler.
    /// Solves the Windows problem with an early called auto-registered shutdown handler.
    /// </summary>
    private static void DisableLog4NetAutoShutdown()
    {
        try
        {
            // Force log4net static constructor to run first
            _ = log4net.LogManager.GetRepository();

            // Get the static field that holds the ProcessExit handler
            var onProcessExitMethod = typeof(LoggerManager).GetMethod(
                "OnProcessExit",
#pragma warning disable S3011
                BindingFlags.NonPublic | BindingFlags.Static);
#pragma warning restore S3011

            if (onProcessExitMethod == null)
            {
                return;
            }

            var handler = Delegate.CreateDelegate(typeof(EventHandler), onProcessExitMethod);
            AppDomain.CurrentDomain.ProcessExit -= (EventHandler)handler;
            Debug.WriteLine("Log4net automatic shutdown disabled");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to disable log4net auto shutdown: {ex.Message}");
        }
    }
}
