using System.Diagnostics;
using System.Reflection;
using Autofac;
using Autofac.Extras.DynamicProxy;
using FlexKit.Logging.Configuration;
using FlexKit.Logging.Core;
using FlexKit.Logging.Formatting.Core;
using FlexKit.Logging.Formatting.Formatters;
using FlexKit.Logging.Formatting.Translation;
using FlexKit.Logging.Interception;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Module = Autofac.Module;

namespace FlexKit.Logging.Detection;

/// <summary>
/// Autofac module that automatically discovers and registers classes for logging interception using three approaches:
/// 1. Attribute-based (highest priority)
/// 2. Configuration-based patterns (medium priority)
/// 3. Auto-interception for all public classes (lowest priority)
/// Provides transparent logging infrastructure that requires no manual service registration by the user.
/// </summary>
[UsedImplicitly]
public sealed class LoggingModule : Module
{
    /// <summary>
    /// Loads and configures the logging infrastructure components and discovers classes that need logging.
    /// Uses a three-tier discovery approach with proper precedence handling.
    /// </summary>
    /// <param name="builder">The Autofac container builder to register components with.</param>
    protected override void Load(ContainerBuilder builder)
    {
        // Register core logging infrastructure FIRST
        RegisterLoggingInfrastructure(builder);

        // Discover all potentially interceptable types (expanded discovery)
        var candidateTypes = DiscoverCandidateTypes().ToList();

        // Create cache that will resolve decisions using the three-tier approach
        // The LoggingConfig will be resolved when the container is built
        builder.Register(c =>
            {
                var loggingConfig = c.Resolve<LoggingConfig>();
                var cache = new InterceptionDecisionCache(loggingConfig);

                // Populate cache with all candidate types
                // The cache's decision logic will handle the three-tier precedence
                foreach (var type in candidateTypes)
                {
                    cache.CacheTypeDecisions(type);
                }

                return cache;
            })
            .AsSelf()
            .SingleInstance();

        // Register all candidate types with interception
        // The interceptor will use the cache to determine actual behavior at runtime
        foreach (var type in candidateTypes)
        {
            RegisterTypeWithLogging(builder, type);
        }
    }

    /// <summary>
    /// Discovers all types that could potentially be intercepted.
    /// This is an expanded discovery that includes:
    /// - Types with logging attributes (existing)
    /// - All public, non-abstract classes in user assemblies (new)
    /// The actual interception decisions will be made by the cache at runtime.
    /// </summary>
    /// <returns>A list of all types that could potentially need logging interception.</returns>
    private static List<Type> DiscoverCandidateTypes()
    {
        var assemblies = GetScannableAssemblies();
        var candidateTypes = new List<Type>();

        foreach (var assembly in assemblies)
        {
            var discoveredTypes = ScanAssemblyForCandidateTypes(assembly);
            candidateTypes.AddRange(discoveredTypes);
        }

        return candidateTypes;
    }

    /// <summary>
    /// Scans a single assembly for all potentially interceptable types.
    /// Includes all public, non-abstract classes that can be intercepted.
    /// </summary>
    /// <param name="assembly">The assembly to scan for candidate types.</param>
    /// <returns>A collection of candidate types from the assembly.</returns>
    private static IEnumerable<Type> ScanAssemblyForCandidateTypes(Assembly assembly)
    {
        try
        {
            return GetCandidateTypesFromAssembly(assembly);
        }
        catch (ReflectionTypeLoadException ex)
        {
            return HandlePartiallyLoadedAssembly(ex);
        }
        catch (Exception ex)
        {
            LogAssemblyScanFailure(assembly, ex);
            return Enumerable.Empty<Type>();
        }
    }

    /// <summary>
    /// Gets all potentially interceptable types from a successfully loaded assembly.
    /// Includes all public, non-abstract classes that have interceptable methods.
    /// </summary>
    /// <param name="assembly">The assembly to get types from.</param>
    /// <returns>Candidate types from the assembly.</returns>
    private static IEnumerable<Type> GetCandidateTypesFromAssembly(Assembly assembly)
    {
        return assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsPublic: true, IsAbstract: false } &&
                           HasInterceptableMethods(type));
    }

    /// <summary>
    /// Handles assemblies that couldn't be fully loaded due to missing dependencies.
    /// Extracts the types that were successfully loaded and filters for interceptable types.
    /// </summary>
    /// <param name="ex">The ReflectionTypeLoadException containing partially loaded types.</param>
    /// <returns>Successfully loaded candidate types.</returns>
    private static IEnumerable<Type> HandlePartiallyLoadedAssembly(ReflectionTypeLoadException ex)
    {
        var loadedTypes = ex.Types.Where(t => t != null &&
                                             t.IsClass &&
                                             t.IsPublic &&
                                             !t.IsAbstract &&
                                             HasInterceptableMethods(t));
        return loadedTypes!;
    }

    /// <summary>
    /// Checks if a type has methods that can be intercepted (public instance methods).
    /// Used to avoid registering types that can't benefit from interception.
    /// </summary>
    /// <param name="type">The type to check for interceptable methods.</param>
    /// <returns>True if the type has interceptable methods; false otherwise.</returns>
    private static bool HasInterceptableMethods(Type type)
    {
        return type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.DeclaringType == type) // Only methods declared on this type
            .Any(ShouldInterceptMethod);
    }

    /// <summary>
    /// Determines if a method should be considered for interception based on its characteristics.
    /// Excludes private methods, static methods, constructors, property accessors, and Object methods.
    /// </summary>
    /// <param name="method">The method to evaluate.</param>
    /// <returns>True if the method should be considered for interception; false if it should be ignored.</returns>
    private static bool ShouldInterceptMethod(MethodInfo method) =>
        method is { IsPublic: true, IsStatic: false } and
        { IsConstructor: false, IsSpecialName: false } && // Excludes property getters/setters, event handlers
        method.DeclaringType != typeof(object); // Exclude Object methods

    // ... (rest of the existing infrastructure methods remain unchanged)

    /// <summary>
    /// Registers the core logging infrastructure components including configuration, formatters, and background services.
    /// Sets up the complete logging pipeline required for method interception.
    /// </summary>
    /// <param name="builder">The Autofac container builder to register components with.</param>
    private static void RegisterLoggingInfrastructure(ContainerBuilder builder)
    {
        RegisterLoggingConfiguration(builder);
        RegisterMessageFormatters(builder);
        RegisterFormattingInfrastructure(builder);
        RegisterInterceptionComponents(builder);
        RegisterBackgroundLogging(builder);
        RegisterMicrosoftExtensionsLogging(builder);
        RegisterManualLogging(builder);
    }

    /// <summary>
    /// Registers the logging configuration, loading it from app configuration or using defaults.
    /// </summary>
    /// <param name="builder">The container builder to register with.</param>
    private static void RegisterLoggingConfiguration(ContainerBuilder builder)
    {
        builder.Register(c =>
            {
                var configuration = c.Resolve<IConfiguration>();
                return configuration.GetSection("FlexKit:Logging").Get<LoggingConfig>() ?? new LoggingConfig();
            })
            .As<LoggingConfig>()
            .SingleInstance()
            .IfNotRegistered(typeof(LoggingConfig));
    }

    /// <summary>
    /// Registers all available message formatters for different output formats.
    /// </summary>
    /// <param name="builder">The container builder to register with.</param>
    private static void RegisterMessageFormatters(ContainerBuilder builder)
    {
        builder.RegisterType<MelMessageTranslator>().As<IMessageTranslator>();
        builder.RegisterType<CustomTemplateFormatter>().As<IMessageFormatter>().InstancePerDependency();
        builder.RegisterType<HybridFormatter>().As<IMessageFormatter>().InstancePerDependency();
        builder.RegisterType<JsonFormatter>().As<IMessageFormatter>().InstancePerDependency();
        builder.RegisterType<StandardStructuredFormatter>().As<IMessageFormatter>().InstancePerDependency();
        builder.RegisterType<SuccessErrorFormatter>().As<IMessageFormatter>().InstancePerDependency();
    }

    /// <summary>
    /// Registers the formatting infrastructure components that coordinate message formatting.
    /// </summary>
    /// <param name="builder">The container builder to register with.</param>
    private static void RegisterFormattingInfrastructure(ContainerBuilder builder)
    {
        builder.RegisterType<MessageFormatterFactory>()
            .As<IMessageFormatterFactory>()
            .SingleInstance();
    }

    /// <summary>
    /// Registers the interception components including the interceptor.
    /// The decision cache is registered separately in the main Load method.
    /// </summary>
    /// <param name="builder">The container builder to register with.</param>
    private static void RegisterInterceptionComponents(ContainerBuilder builder)
    {
        builder.RegisterType<MethodLoggingInterceptor>()
            .AsSelf()
            .InstancePerLifetimeScope();
    }

    /// <summary>
    /// Registers the background logging infrastructure including queues, processors, and hosted services.
    /// </summary>
    /// <param name="builder">The container builder to register with.</param>
    private static void RegisterBackgroundLogging(ContainerBuilder builder)
    {
        builder.RegisterType<BackgroundLog>()
            .AsImplementedInterfaces()
            .SingleInstance();

        builder.RegisterType<FormattedLogWriter>()
            .As<ILogEntryProcessor>()
            .SingleInstance();

        RegisterBackgroundService(builder);
    }

    /// <summary>
    /// Registers and starts the background logging service that processes log entries asynchronously.
    /// Uses a long-running task to manage the service lifecycle since it's not hosted in a typical ASP.NET Core host.
    /// </summary>
    /// <param name="builder">The container builder to register with.</param>
    private static void RegisterBackgroundService(ContainerBuilder builder)
    {
        builder.RegisterType<BackgroundLoggingService>()
            .AsImplementedInterfaces()  // Registers as IHostedService
            .AsSelf()                   // Also registers as BackgroundLoggingService
            .SingleInstance()
            .AutoActivate();

        builder.RegisterBuildCallback(container =>
        {
            var backgroundService = container.Resolve<BackgroundLoggingService>();
            _ = Task.Run(() => RunBackgroundServiceAsync(backgroundService));
            AppDomain.CurrentDomain.ProcessExit += (_, _) =>
            {
                FlushLogsOnExit(backgroundService);
            };
        });
    }

    /// <summary>
    /// Registers Microsoft.Extensions.Logging integration using MelProviderFactory.
    /// Only registers if targets are configured in LoggingConfig.
    /// </summary>
    /// <param name="builder">The container builder to register with.</param>
    private static void RegisterMicrosoftExtensionsLogging(ContainerBuilder builder)
    {
        builder.Register(c =>
            {
                var loggingConfig = c.Resolve<LoggingConfig>();

                // Skip registration if no targets are configured
                if (loggingConfig?.Targets == null || loggingConfig.Targets.Count == 0)
                {
                    // Return a minimal logger factory instead of null
                    var emptyServices = new ServiceCollection();
                    emptyServices.AddLogging();
                    return emptyServices.BuildServiceProvider().GetRequiredService<ILoggerFactory>();
                }

                var services = new ServiceCollection();
                services.AddLogging(loggingBuilder =>
                {
                    new MelProviderFactory(loggingBuilder, loggingConfig).ConfigureProviders();
                });

                var serviceProvider = services.BuildServiceProvider();
                return serviceProvider
                    .GetRequiredService<ILoggerFactory>(); // Changed from GetService to GetRequiredService
            })
            .As<ILoggerFactory>()
            .SingleInstance();

        // Also register ILogger<T> for injection
        builder.RegisterGeneric(typeof(Logger<>))
            .As(typeof(ILogger<>))
            .InstancePerDependency();
    }

    /// <summary>
    /// Flushes any remaining log entries when the process is exiting.
    /// </summary>
    /// <param name="backgroundService">The background service to stop and flush.</param>
    private static void FlushLogsOnExit(BackgroundLoggingService backgroundService)
    {
        try
        {
            backgroundService.FlushRemainingEntries();
            Thread.Sleep(1000);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error flushing logs on exit: {ex.Message}");
        }
    }

    /// <summary>
    /// Manages the complete lifecycle of the background logging service including startup, continuous operation, and cleanup.
    /// Handles exceptions during service operation and ensures proper cleanup on shutdown.
    /// </summary>
    /// <param name="backgroundService">The background service instance to manage.</param>
    private static async Task RunBackgroundServiceAsync(BackgroundLoggingService backgroundService)
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        try
        {
            await backgroundService.StartAsync(cancellationTokenSource.Token);

            // Keep the service running - ExecuteAsync should contain the main loop
            // The service should handle its own lifecycle and cancellation
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

    /// <summary>
    /// Safely stops the background service with exception handling to prevent shutdown failures.
    /// </summary>
    /// <param name="backgroundService">The background service to stop.</param>
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

    /// <summary>
    /// Gets the list of assemblies that should be scanned for logging.
    /// Excludes system assemblies and assemblies that don't reference FlexKit.Logging.
    /// </summary>
    /// <returns>A filtered list of assemblies to scan.</returns>
    private static List<Assembly> GetScannableAssemblies()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .Where(ShouldScanAssembly)
            .ToList();
    }

    /// <summary>
    /// Determines if an assembly should be scanned for logging.
    /// Excludes system assemblies and other known assemblies that won't have user logging.
    /// </summary>
    /// <param name="assembly">The assembly to evaluate for scanning.</param>
    /// <returns>True if the assembly should be scanned; false if it should be skipped.</returns>
    private static bool ShouldScanAssembly(Assembly assembly)
    {
        if (assembly.IsDynamic)
        {
            return false;
        }

        var name = assembly.FullName ?? "";

        return !IsFlexKitFrameworkAssembly(name) &&
               !IsSystemAssembly(name) &&
               ReferencesFlexKitLogging(assembly);
    }

    /// <summary>
    /// Checks if an assembly is part of the FlexKit framework itself.
    /// </summary>
    /// <param name="assemblyName">The full name of the assembly.</param>
    /// <returns>True if it's a FlexKit framework assembly; false otherwise.</returns>
    private static bool IsFlexKitFrameworkAssembly(string assemblyName)
    {
        return assemblyName.StartsWith("FlexKit.Logging.", StringComparison.InvariantCulture);
    }

    /// <summary>
    /// Checks if an assembly is a system assembly that should be excluded from scanning.
    /// </summary>
    /// <param name="assemblyName">The full name of the assembly.</param>
    /// <returns>True if it's a system assembly; false otherwise.</returns>
    private static bool IsSystemAssembly(string assemblyName)
    {
        string[] systemPrefixes = [
            "System.", "Microsoft.", "mscorlib", "netstandard", "Windows.",
            "Autofac", "Castle.", "Newtonsoft."
        ];

        return Array.Exists(systemPrefixes, p => assemblyName.StartsWith(p, StringComparison.InvariantCulture));
    }

    /// <summary>
    /// Checks if an assembly references FlexKit.Logging, indicating it might contain logging.
    /// </summary>
    /// <param name="assembly">The assembly to check references for.</param>
    /// <returns>True if the assembly references FlexKit.Logging; false otherwise.</returns>
    private static bool ReferencesFlexKitLogging(Assembly assembly)
    {
        try
        {
            var referencedAssemblies = assembly.GetReferencedAssemblies();

            return referencedAssemblies.Any(refAsm =>
                refAsm.Name != null &&
                refAsm.Name.Equals("FlexKit.Logging", StringComparison.Ordinal));
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Registers a type with appropriate interception based on its characteristics (interfaces vs. class interception).
    /// </summary>
    /// <param name="builder">The container builder to register with.</param>
    /// <param name="type">The type to register with logging interception.</param>
    private static void RegisterTypeWithLogging(ContainerBuilder builder, Type type)
    {
        var userInterfaces = type.GetInterfaces()
            .Where(i => !IsSystemInterface(i))
            .ToList();

        if (userInterfaces.Count != 0)
        {
            RegisterWithInterfaceInterception(builder, type, userInterfaces);
        }
        else if (CanUseClassInterception(type))
        {
            RegisterWithClassInterception(builder, type);
        }
        else
        {
            RegisterWithoutInterception(builder, type);
        }
    }

    /// <summary>
    /// Registers a type with interface-based interception for all user-defined interfaces.
    /// </summary>
    /// <param name="builder">The container builder to register with.</param>
    /// <param name="type">The implementing type to register.</param>
    /// <param name="userInterfaces">The user-defined interfaces to register as.</param>
    private static void RegisterWithInterfaceInterception(ContainerBuilder builder, Type type, List<Type> userInterfaces)
    {
        var registration = builder.RegisterType(type)
            .As(userInterfaces.ToArray())
            .EnableInterfaceInterceptors()
            .InterceptedBy(typeof(MethodLoggingInterceptor));

        registration.InstancePerLifetimeScope();
    }

    /// <summary>
    /// Registers a type with class-based interception when no user interfaces are available.
    /// </summary>
    /// <param name="builder">The container builder to register with.</param>
    /// <param name="type">The type to register with class interception.</param>
    private static void RegisterWithClassInterception(ContainerBuilder builder, Type type)
    {
        var registration = builder.RegisterType(type)
            .AsSelf()
            .EnableClassInterceptors()
            .InterceptedBy(typeof(MethodLoggingInterceptor));

        registration.InstancePerLifetimeScope();
    }

    /// <summary>
    /// Registers a type without interception when it cannot be intercepted (sealed classes, no virtual methods).
    /// Logs a warning about the inability to provide logging for this type.
    /// </summary>
    /// <param name="builder">The container builder to register with.</param>
    /// <param name="type">The type to register without interception.</param>
    private static void RegisterWithoutInterception(ContainerBuilder builder, Type type)
    {
        Debug.WriteLine($"Warning: Cannot intercept {type.FullName} - class is sealed or has no virtual methods. " +
                                           "Consider adding an interface or making methods virtual for logging to work.");

        builder.RegisterType(type)
            .AsSelf()
            .InstancePerLifetimeScope();
    }

    /// <summary>
    /// Determines if a type can use Castle DynamicProxy class interception.
    /// Requires the class to be non-sealed and have at least one public virtual method.
    /// </summary>
    /// <param name="type">The type to evaluate for class interception compatibility.</param>
    /// <returns>True if the type can use class interception; false otherwise.</returns>
    private static bool CanUseClassInterception(Type type)
    {
        // Class must not be sealed
        if (type.IsSealed)
        {
            return false;
        }

        // Must have at least one public virtual method (excluding Object methods)
        var publicMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.DeclaringType != typeof(object))
            .ToList();

        return publicMethods.Any(m => m is { IsVirtual: true, IsFinal: false });
    }

    /// <summary>
    /// Checks if an interface is a system interface that should be ignored for registration purposes.
    /// System interfaces are typically from the .NET Framework and not user-defined.
    /// </summary>
    /// <param name="interfaceType">The interface type to check.</param>
    /// <returns>True if it's a system interface; false if it's user-defined.</returns>
    private static bool IsSystemInterface(Type interfaceType)
    {
        var name = interfaceType.FullName ?? "";

        return name.StartsWith("System.", StringComparison.InvariantCulture) ||
               name.StartsWith("Microsoft.", StringComparison.InvariantCulture) ||
               interfaceType.Assembly == typeof(object).Assembly;
    }

    /// <summary>
    /// Logs a warning when an assembly scan fails completely.
    /// </summary>
    /// <param name="assembly">The assembly that failed to scan.</param>
    /// <param name="ex">The exception that occurred during scanning.</param>
    private static void LogAssemblyScanFailure(Assembly assembly, Exception ex)
    {
        Debug.WriteLine($"Warning: Failed to scan assembly {assembly.FullName}: {ex.Message}");
    }

    private static void RegisterManualLogging(ContainerBuilder builder)
    {
        // Register ActivitySource as singleton
        builder.Register(c =>
            {
                var config = c.Resolve<LoggingConfig>();
                var activitySource = new ActivitySource(config.ActivitySourceName);

                ActivitySource.AddActivityListener(new ActivityListener
                {
                    ShouldListenTo = source => source.Name == config.ActivitySourceName,
                    Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData,
                    SampleUsingParentId = (ref ActivityCreationOptions<string> options) => ActivitySamplingResult.AllData
                });

                return activitySource;
            })
            .As<ActivitySource>()
            .SingleInstance();

        // Register FlexKitLogger
        builder.RegisterType<FlexKitLogger>()
            .As<IFlexKitLogger>()
            .InstancePerLifetimeScope();
    }
}
