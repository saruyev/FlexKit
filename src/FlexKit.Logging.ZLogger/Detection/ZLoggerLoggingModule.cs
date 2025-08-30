using Autofac;
using FlexKit.Logging.Configuration;
using FlexKit.Logging.Core;
using FlexKit.Logging.Formatting.Translation;
using FlexKit.Logging.ZLogger.Core;
using FlexKit.Logging.ZLogger.Translation;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace FlexKit.Logging.ZLogger.Detection;

/// <summary>
/// Autofac module that registers ZLogger-based logging components for FlexKit.Logging.
/// Replaces MEL components when FlexKit.Logging.ZLogger is present.
/// </summary>
/// <remarks>
/// <para>
/// This module follows the same pattern as SerilogLoggingModule and NLogLoggingModule, registering
/// ZLogger-specific implementations of FlexKit's core logging interfaces. When this module is loaded,
/// it replaces the default MEL-based implementations with ZLogger equivalents.
/// </para>
/// <para>
/// <strong>Component Replacements:</strong>
/// <list type="bullet">
/// <item>DefaultMessageTranslator → ZLoggerMessageTranslator</item>
/// <item>FormattedLogWriter → ZLoggerLogWriter</item>
/// <item>BackgroundLog (Channel-based) → ZLoggerBackgroundLog</item>
/// <item>MEL ILoggerFactory → ZLogger-configured ILoggerFactory</item>
/// </list>
/// </para>
/// <para>
/// <strong>Configuration Integration:</strong>
/// The module automatically detects available ZLogger processors (both built-in extensions
/// like AddZLoggerConsole/AddZLoggerFile and custom IAsyncLogProcessor implementations) and
/// configures them based on FlexKit's LoggingConfig. It supports both programmatic and
/// configuration-based processor setup through the ZLoggerConfigurationBuilder.
/// </para>
/// <para>
/// <strong>ZLogger Features:</strong>
/// ZLogger provides zero-allocation structured logging with string interpolation and source
/// generators. It supports both built-in processors (Console, File, RollingFile, etc.) and
/// custom IAsyncLogProcessor implementations for advanced scenarios.
/// </para>
/// </remarks>
[UsedImplicitly]
public class ZLoggerLoggingModule : Module
{
    /// <summary>
    /// Configures the container with ZLogger-based logging services.
    /// Registers all components needed to replace MEL with ZLogger infrastructure.
    /// </summary>
    /// <param name="builder">The Autofac container builder to register components with.</param>
    protected override void Load(ContainerBuilder builder)
    {
        RegisterZLoggerServices(builder);
        ConfigureLoggerFactory(builder);
    }

    /// <summary>
    /// Configures and registers the logger factory with ZLogger-based providers and services.
    /// Initializes and integrates required components with the Autofac container for logging functionality.
    /// </summary>
    /// <param name="builder">
    /// The Autofac container builder used for registering the logger factory and related services.
    /// </param>
    private static void ConfigureLoggerFactory(ContainerBuilder builder)
    {
        builder.Register(c =>
            {
                var loggingConfig = c.Resolve<LoggingConfig>();
                var configBuilder = c.Resolve<ZLoggerConfigurationBuilder>();

                loggingConfig.RequiresSerialization = false;

                // Create and configure the logger factory with ZLogger providers
                var factory = LoggerFactory.Create(loggingBuilder =>
                {
                    loggingBuilder.ClearProviders();
                    loggingBuilder.SetMinimumLevel(LogLevel.Trace);
                    configBuilder.BuildConfiguration(loggingConfig, loggingBuilder);
                });

                AppDomain.CurrentDomain.ProcessExit += (_, _) => factory.Dispose();

                return factory;
            })
            .As<ILoggerFactory>()
            .SingleInstance();
        builder.RegisterType<ZLoggerLogWriter>()
            .As<ILogEntryProcessor>()
            .SingleInstance();

        builder.RegisterType<ZLoggerBackgroundLog>()
            .As<IBackgroundLog>()
            .SingleInstance();
    }

    /// <summary>
    /// Registers the required services for ZLogger-based logging within the Autofac container.
    /// Includes services for message translation, configuration, and template engine setup.
    /// </summary>
    /// <param name="builder">The Autofac container builder used to register ZLogger components.</param>
    private static void RegisterZLoggerServices(ContainerBuilder builder)
    {
        builder.RegisterType<ZLoggerMessageTranslator>()
            .As<IMessageTranslator>()
            .SingleInstance();
        builder.RegisterType<ZLoggerConfigurationBuilder>()
            .SingleInstance();
        builder.RegisterType<ZLoggerTemplateEngine>()
            .AsImplementedInterfaces()
            .SingleInstance()
            .OnActivated(e => e.Instance.PrecompileTemplates());
    }
}
