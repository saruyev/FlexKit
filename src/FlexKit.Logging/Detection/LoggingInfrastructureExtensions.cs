using System.Diagnostics;
using Autofac;
using FlexKit.Logging.Configuration;
using FlexKit.Logging.Core;
using FlexKit.Logging.Formatting.Core;
using FlexKit.Logging.Formatting.Formatters;
using FlexKit.Logging.Formatting.Translation;
using FlexKit.Logging.Interception;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FlexKit.Logging.Detection;

/// <summary>
/// Extension methods for registering the complete logging infrastructure in Autofac.
/// Handles configuration, formatters, background services, and Microsoft Extensions Logging integration.
/// </summary>
internal static class LoggingInfrastructureExtensions
{
    /// <summary>
    /// Registers all logging infrastructure components required for the logging system to function.
    /// Call this before registering any types that need logging interception.
    /// </summary>
    /// <param name="builder">The Autofac container builder.</param>
    public static void RegisterLoggingInfrastructure(this ContainerBuilder builder)
    {
        builder.RegisterLoggingConfiguration();
        builder.RegisterMessageFormatting();
        builder.RegisterInterceptionComponents();
        builder.RegisterManualLogging();

        if (!HasLoggingProviderAssemblies())
        {
            builder.RegisterBackgroundLogging();
            builder.RegisterMicrosoftExtensionsLogging();
        }
    }

    /// <summary>
    /// Registers the logging configuration, loading from app configuration or using defaults.
    /// </summary>
    /// <param name="builder">The container builder.</param>
    public static void RegisterLoggingConfiguration(this ContainerBuilder builder) =>
        builder.Register(c =>
            {
                var configuration = c.Resolve<IConfiguration>();
                return configuration.GetSection("FlexKit:Logging").Get<LoggingConfig>() ?? new LoggingConfig();
            })
            .As<LoggingConfig>()
            .SingleInstance()
            .IfNotRegistered(typeof(LoggingConfig));

    /// <summary>
    /// Registers all message formatters and formatting infrastructure.
    /// </summary>
    /// <param name="builder">The container builder.</param>
    public static void RegisterMessageFormatting(this ContainerBuilder builder)
    {
        // Register all available formatters
        builder.RegisterType<CustomTemplateFormatter>().As<IMessageFormatter>().InstancePerDependency();
        builder.RegisterType<HybridFormatter>().As<IMessageFormatter>().InstancePerDependency();
        builder.RegisterType<JsonFormatter>().As<IMessageFormatter>().InstancePerDependency();
        builder.RegisterType<StandardStructuredFormatter>().As<IMessageFormatter>().InstancePerDependency();
        builder.RegisterType<SuccessErrorFormatter>().As<IMessageFormatter>().InstancePerDependency();

        // Register formatter factory
        builder.RegisterType<MessageFormatterFactory>()
            .As<IMessageFormatterFactory>()
            .SingleInstance();
    }

    /// <summary>
    /// Registers the method interception components.
    /// </summary>
    /// <param name="builder">The container builder.</param>
    public static void RegisterInterceptionComponents(this ContainerBuilder builder) =>
        builder.RegisterType<MethodLoggingInterceptor>()
            .AsSelf()
            .InstancePerLifetimeScope();

    /// <summary>
    /// Registers components for manual logging (non-interception-based logging).
    /// </summary>
    /// <param name="builder">The container builder.</param>
    public static void RegisterManualLogging(this ContainerBuilder builder)
    {
        // Register ActivitySource for distributed tracing
        builder.Register(c =>
            {
                var config = c.Resolve<LoggingConfig>();
                var activitySource = new ActivitySource(config.ActivitySourceName);

                ActivitySource.AddActivityListener(new ActivityListener
                {
                    ShouldListenTo = source => source.Name == config.ActivitySourceName,
                    Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                    SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData
                });

                return activitySource;
            })
            .As<ActivitySource>()
            .SingleInstance();

        // Register FlexKit's logger for manual logging
        builder.RegisterType<FlexKitLogger>()
            .As<IFlexKitLogger>()
            .InstancePerLifetimeScope();
    }

    /// <summary>
    /// Registers the background logging infrastructure including queues, processors, and services.
    /// Only registers if no external logging providers are detected.
    /// </summary>
    /// <param name="builder">The container builder.</param>
    public static void RegisterBackgroundLogging(this ContainerBuilder builder)
    {
        // Register the background log queue
        builder.RegisterType<BackgroundLog>()
            .AsImplementedInterfaces()
            .SingleInstance();

        // Register log entry processor
        builder.RegisterType<FormattedLogWriter>()
            .As<ILogEntryProcessor>()
            .SingleInstance();

        // Register and auto-start the background service
        builder.RegisterType<BackgroundLoggingService>()
            .AsImplementedInterfaces()
            .AsSelf()
            .SingleInstance()
            .AutoActivate();

        // Setup background service lifecycle
        builder.RegisterBuildCallback(container =>
        {
            var backgroundService = container.Resolve<BackgroundLoggingService>();

            // Start the service in the background
            _ = Task.Run(() => RunBackgroundServiceAsync(backgroundService));

            // Ensure logs are flushed on process exit
            AppDomain.CurrentDomain.ProcessExit += (_, _) => FlushLogsOnExit(backgroundService);
        });
    }

    /// <summary>
    /// Registers Microsoft Extensions Logging integration using configured targets.
    /// Only registers if logging targets are configured and no external providers exist.
    /// </summary>
    /// <param name="builder">The container builder.</param>
    public static void RegisterMicrosoftExtensionsLogging(this ContainerBuilder builder)
    {
        builder.RegisterType<DefaultMessageTranslator>().As<IMessageTranslator>();

        builder.Register(c =>
            {
                var loggingConfig = c.Resolve<LoggingConfig>();

                // Return minimal logger factory if no targets configured
                if (loggingConfig.Targets.Count == 0)
                {
                    var emptyServices = new ServiceCollection();
                    emptyServices.AddLogging();
                    return emptyServices.BuildServiceProvider().GetRequiredService<ILoggerFactory>();
                }

                // Configure full logging with targets
                var services = new ServiceCollection();
                services.AddLogging(loggingBuilder =>
                {
                    new MelProviderFactory(loggingBuilder, loggingConfig).ConfigureProviders();
                });

                return services.BuildServiceProvider().GetRequiredService<ILoggerFactory>();
            })
            .As<ILoggerFactory>()
            .SingleInstance();

        // Register generic ILogger<T> for dependency injection
        builder.RegisterGeneric(typeof(Logger<>))
            .As(typeof(ILogger<>))
            .InstancePerDependency();
    }

    private static bool HasLoggingProviderAssemblies() =>
        AppDomain.CurrentDomain.GetAssemblies()
            .Any(assembly =>
            {
                var name = assembly.GetName().Name;
                return name?.StartsWith("FlexKit.Logging.", StringComparison.InvariantCulture) == true &&
                       name != "FlexKit.Logging";
            });

    private static async Task RunBackgroundServiceAsync(BackgroundLoggingService backgroundService)
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        try
        {
            await backgroundService.StartAsync(cancellationTokenSource.Token);
            await Task.Delay(Timeout.Infinite, cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Background logging service failed: {ex}");
        }
        finally
        {
            await StopBackgroundServiceSafelyAsync(backgroundService);
        }
    }

    private static async Task StopBackgroundServiceSafelyAsync(BackgroundLoggingService backgroundService)
    {
        try
        {
            await backgroundService.StopAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error stopping background logging service: {ex}");
        }
    }

    private static void FlushLogsOnExit(BackgroundLoggingService backgroundService)
    {
        try
        {
            backgroundService.FlushRemainingEntries();
            Thread.Sleep(1000); // Give time for the final flush
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error flushing logs on exit: {ex.Message}");
        }
    }
}
