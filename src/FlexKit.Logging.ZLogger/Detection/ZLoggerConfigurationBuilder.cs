using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using FlexKit.Logging.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FlexKit.Logging.ZLogger.Detection;

/// <summary>
/// Builds ZLogger configuration from FlexKit LoggingConfig by dynamically
/// detecting available processors and configuring them using reflection.
/// </summary>
public class ZLoggerConfigurationBuilder
{
    private readonly Dictionary<string, ZLoggerProcessorDetector.ProcessorInfo> _availableProcessors;

    /// <summary>
    /// Initializes a new instance of ZLoggerConfigurationBuilder with auto-detected processors.
    /// </summary>
    public ZLoggerConfigurationBuilder()
    {
        _availableProcessors = ZLoggerProcessorDetector.DetectAvailableProcessors();
    }

    /// <summary>
    /// Builds a ZLogger configuration from FlexKit LoggingConfig and applies it to the logging builder.
    /// </summary>
    /// <param name="config">FlexKit logging configuration.</param>
    /// <param name="loggingBuilder">The ILoggingBuilder to configure with ZLogger providers.</param>
    /// <returns>The number of successfully configured ZLogger providers.</returns>
    public void BuildConfiguration(
        LoggingConfig config,
        ILoggingBuilder loggingBuilder)
    {
        // Store target types for filtering
        var types = config.Targets.Values.Select(t => t.Type).Distinct().ToArray();

        // Configure targets from FlexKit configuration
        var targetsConfigured = ConfigureTargets(config, loggingBuilder, types);

        // Fallback to Console if no targets configured
        if (targetsConfigured != 0)
        {
            return;
        }

        ConfigureFallbackConsoleLogger(loggingBuilder);
    }

    /// <summary>
    /// Configures the logging targets based on the provided FlexKit logging configuration.
    /// </summary>
    /// <param name="config">The FlexKit logging configuration containing the targets to configure.</param>
    /// <param name="loggingBuilder">The ILoggingBuilder to configure with ZLogger providers.</param>
    /// <param name="types">Array of all target types for filtering purposes.</param>
    /// <returns>The number of successfully configured logging targets.</returns>
    private int ConfigureTargets(
        LoggingConfig config,
        ILoggingBuilder loggingBuilder,
        string[] types) =>
        config.Targets.Values
            .Where(t => t.Enabled)
            .Count(target => TryConfigureProcessor(target, loggingBuilder, types));

    /// <summary>
    /// Attempts to configure a ZLogger processor for the specified FlexKit logging target.
    /// </summary>
    /// <param name="target">The FlexKit logging target to configure.</param>
    /// <param name="loggingBuilder">The ILoggingBuilder to configure.</param>
    /// <param name="types">Array of all target types for filtering purposes.</param>
    /// <returns>True if the processor was successfully configured; otherwise, false.</returns>
    private bool TryConfigureProcessor(
        LoggingTarget target,
        ILoggingBuilder loggingBuilder,
        string[] types)
    {
        if (!_availableProcessors.TryGetValue(target.Type, out var processorInfo))
        {
            Debug.WriteLine($"Warning: ZLogger processor '{target.Type}' not found in available processors");
            return false;
        }

        try
        {
            var context = new BuildContext { LoggingBuilder = loggingBuilder, Target = target };
            var success = processorInfo.IsBuiltIn ? ConfigureBuiltInProcessor(processorInfo, context) : ConfigureAsyncLogProcessor(processorInfo, context);

            // Add filtering for this provider if it was successfully configured
            if (success && context.ProviderType != null)
            {
                AddFiltersForProvider(context, types);
            }

            return success;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Warning: Failed to configure ZLogger processor '{target.Type}': {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Configures a built-in ZLogger processor using extension methods.
    /// </summary>
    /// <param name="processorInfo">Information about the processor to configure.</param>
    /// <param name="context">Context for the current configuration.</param>
    /// <returns>True if successful; otherwise, false.</returns>
    private static bool ConfigureBuiltInProcessor(
        ZLoggerProcessorDetector.ProcessorInfo processorInfo,
        BuildContext context)
    {
        if (processorInfo.ExtensionMethod == null)
        {
            return false;
        }

        var method = processorInfo.ExtensionMethod;
        var parameters = method.GetParameters();

        // Determine the provider type based on the processor name
        context.ProviderType = GetZLoggerProviderType(processorInfo.Name);

        // Try different overload patterns for ZLogger built-in methods
        return ConfigureZLoggerMethods(context, parameters, method);
    }

    /// <summary>
    /// Configures ZLogger methods based on the provided method signature and associated parameters.
    /// Attempts to match and invoke the suitable ZLogger configuration method for the specified context.
    /// </summary>
    /// <param name="context">The build context used for the ZLogger configuration.</param>
    /// <param name="parameters">An array of parameter information representing method arguments.</param>
    /// <param name="method">The method information representing the ZLogger configuration method.</param>
    /// <returns>True if a configuration method is successfully matched and invoked; otherwise, false.</returns>
    private static bool ConfigureZLoggerMethods(
        BuildContext context,
        ParameterInfo[] parameters,
        MethodInfo method)
    {
        switch (parameters.Length)
        {
            // Pattern 1: AddZLoggerConsole() - no parameters
            // Just ILoggingBuilder
            case 1:
                method.Invoke(null, [context.LoggingBuilder]);
                return true;
            // Pattern 2: AddZLoggerConsole(Action<ZLoggerOptions>) - with options configuration
            case 2 when IsZLoggerOptionsAction(parameters[1]):
            {
                var optionsAction = CreateZLoggerOptionsAction(context.Target);
                method.Invoke(null, [context.LoggingBuilder, optionsAction]);
                return true;
            }
            // Pattern 3: AddZLoggerFile(string filePath) - with a file path
            case 2 when parameters[1].ParameterType == typeof(string):
            {
                var filePath = GetStringPropertyFromTarget(context.Target, "FilePath") ?? "zlogger.log";
                method.Invoke(null, [context.LoggingBuilder, filePath]);
                return true;
            }
            // Pattern 4: AddZLoggerFile(string filePath, Action<ZLoggerOptions>) - with a file path and options
            case 3 when
                parameters[1].ParameterType == typeof(string) &&
                IsZLoggerOptionsAction(parameters[2]):
            {
                var filePath = GetStringPropertyFromTarget(context.Target, "FilePath") ?? "zlogger.log";
                var optionsAction = CreateZLoggerOptionsAction(context.Target);
                method.Invoke(null, [context.LoggingBuilder, filePath, optionsAction]);
                return true;
            }
            default:
                return ConfigureFallback(context, method);
        }
    }

    /// <summary>
    /// Attempts to configure a fallback method for a ZLogger logging provider
    /// when the method signature does not match known patterns.
    /// </summary>
    /// <param name="context">The context containing the logging builder and target configuration.</param>
    /// <param name="method">The method information for the fallback configuration.</param>
    /// <returns>True if the fallback method was successfully configured; otherwise, false.</returns>
    private static bool ConfigureFallback(
        BuildContext context,
        MethodInfo method)
    {
        var simpleMethod = method.DeclaringType?.GetMethod(method.Name, [typeof(ILoggingBuilder)]);
        if (simpleMethod == null)
        {
            return false;
        }

        simpleMethod.Invoke(null, [context.LoggingBuilder]);
        return true;
    }

    /// <summary>
    /// Configures a custom IAsyncLogProcessor implementation.
    /// </summary>
    /// <param name="processorInfo">Information about the processor to configure.</param>
    /// <param name="context">Context for the current configuration.</param>
    /// <returns>True if successful; otherwise, false.</returns>
    private static bool ConfigureAsyncLogProcessor(
        ZLoggerProcessorDetector.ProcessorInfo processorInfo,
        BuildContext context)
    {
        // Create an instance of the IAsyncLogProcessor
        var processor = CreateAsyncLogProcessor(processorInfo, context.Target);
        if (processor == null)
        {
            return false;
        }

        // Add the custom processor to ZLogger
        // This would typically be done through a method like AddZLoggerInMemory or similar
        // For custom processors, we need to find the appropriate registration method


        if (FindZLoggerAddMethods().Any(method => TryInvokeWithProcessor(method, context.LoggingBuilder, processor)))
        {
            context.ProviderType = GetZLoggerProviderType("LogProcessor");
            return true;
        }

        Debug.WriteLine($"Warning: Could not find suitable method to register IAsyncLogProcessor '{processorInfo.ProcessorType.Name}'");
        return false;
    }

    /// <summary>
    /// Creates an instance of an IAsyncLogProcessor and configures its properties.
    /// </summary>
    /// <param name="processorInfo">Information about the processor to create.</param>
    /// <param name="target">The FlexKit target configuration.</param>
    /// <returns>The configured processor instance, or null if creation failed.</returns>
    private static object? CreateAsyncLogProcessor(
        ZLoggerProcessorDetector.ProcessorInfo processorInfo,
        LoggingTarget target)
    {
        try
        {
            // Create an instance of the processor
            var processor = Activator.CreateInstance(processorInfo.ProcessorType);
            if (processor == null)
            {
                return null;
            }

            // Configure processor properties from FlexKit configuration
            ConfigureProcessorProperties(processor, processorInfo, target);

            return processor;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Warning: Failed to create IAsyncLogProcessor '{processorInfo.ProcessorType.Name}': {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Configures the properties of an IAsyncLogProcessor using FlexKit configuration values.
    /// </summary>
    /// <param name="processor">The processor instance to configure.</param>
    /// <param name="processorInfo">Information about available processor properties.</param>
    /// <param name="target">FlexKit configuration containing property values.</param>
    private static void ConfigureProcessorProperties(
        object processor,
        ZLoggerProcessorDetector.ProcessorInfo processorInfo,
        LoggingTarget target)
    {
        foreach (var property in processorInfo.Properties)
        {
            var configSection = FindPropertySection(target.Properties, property.Name);
            if (configSection == null)
            {
                continue;
            }

            try
            {
                var value = ConvertConfigurationValue(configSection, property.PropertyType);
                if (value != null)
                {
                    property.SetValue(processor, value);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Warning: Failed to set property '{property.Name}' on processor '{processorInfo.ProcessorType.Name}': {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Finds ZLogger extension methods that can accept custom processors.
    /// </summary>
    /// <returns>Array of methods that might accept IAsyncLogProcessor instances.</returns>
    private static MethodInfo[] FindZLoggerAddMethods()
    {
        var methods = new List<MethodInfo>();

        // Get all loaded assemblies and look for ZLogger extension methods
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                var types = assembly.GetTypes()
                    .Where(t => t.IsSealed && t is { IsAbstract: true, IsPublic: true }); // Static classes

                foreach (var type in types)
                {
                    var extensionMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                        .Where(FindZLoggerExtensionMethods);

                    methods.AddRange(extensionMethods);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Warning: Failed to scan assembly '{assembly.FullName}' for ZLogger methods: {ex.Message}");
            }
        }

        return [.. methods];
    }

    /// <summary>
    /// Determines whether the specified method is an extension method for adding ZLogger configuration.
    /// </summary>
    /// <param name="method">The method to evaluate.</param>
    /// <returns>True if the method is a ZLogger extension method; otherwise, false.</returns>
    private static bool FindZLoggerExtensionMethods(MethodInfo method) =>
        method.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute)) &&
        method.Name.StartsWith("AddZLogger", StringComparison.OrdinalIgnoreCase) &&
        method.GetParameters().Length > 1 &&
        method.GetParameters()[0].ParameterType == typeof(ILoggingBuilder);

    /// <summary>
    /// Attempts to invoke a ZLogger extension method with a custom processor.
    /// </summary>
    /// <param name="method">The extension method to invoke.</param>
    /// <param name="loggingBuilder">The logging builder instance.</param>
    /// <param name="processor">The processor to pass to the method.</param>
    /// <returns>True if the method was successfully invoked; otherwise, false.</returns>
    private static bool TryInvokeWithProcessor(
        MethodInfo method,
        ILoggingBuilder loggingBuilder,
        object processor)
    {
        try
        {
            var parameters = method.GetParameters();

            // Look for methods that accept IAsyncLogProcessor or similar
            if (parameters.Skip(1).Any(param => param.ParameterType.IsInstanceOfType(processor)))
            {
                var args = new object?[parameters.Length];
                args[0] = loggingBuilder;

                if (!ResolveProcessorParameters(processor, parameters, args))
                {
                    return false;
                }

                method.Invoke(null, args);
                return true;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Warning: Failed to invoke ZLogger method '{method.Name}': {ex.Message}");
        }

        return false;
    }

    /// <summary>
    /// Resolves the parameters required for invoking a method using the specified processor.
    /// </summary>
    /// <param name="processor">The processor object to be matched against the method parameters.</param>
    /// <param name="parameters">The parameter definitions from the method to be invoked.</param>
    /// <param name="args">The array to populate with resolved argument values for the method invocation.</param>
    /// <returns>True if all required parameters were successfully resolved; otherwise, false.</returns>
    private static bool ResolveProcessorParameters(
        object processor,
        ParameterInfo[] parameters,
        object?[] args)
    {
        for (var i = 1; i < parameters.Length; i++)
        {
            if (parameters[i].ParameterType.IsInstanceOfType(processor))
            {
                args[i] = processor;
            }
            else if (parameters[i].HasDefaultValue)
            {
                args[i] = parameters[i].DefaultValue;
            }
            else
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Determines if a parameter type is Action&lt;ZLoggerOptions&gt;
    /// </summary>
    /// <param name="parameter">The parameter to check.</param>
    /// <returns>True if the parameter is Action&lt;ZLoggerOptions&gt; otherwise, false.</returns>
    private static bool IsZLoggerOptionsAction(ParameterInfo parameter)
    {
        var paramType = parameter.ParameterType;

        if (!paramType.IsGenericType || paramType.GetGenericTypeDefinition() != typeof(Action<>))
        {
            return false;
        }

        var genericArg = paramType.GetGenericArguments()[0];
        return genericArg.Name.Contains("ZLoggerOptions", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Creates an Action&lt;ZLoggerOptions&gt; delegate to configure ZLogger options from FlexKit target.
    /// </summary>
    /// <param name="target">The FlexKit target configuration.</param>
    /// <returns>An action delegate to configure ZLogger options.</returns>
    [SuppressMessage("Major Code Smell", "S3011:Reflection should not be used to increase accessibility of classes, methods, or fields")]
    private static Delegate CreateZLoggerOptionsAction(LoggingTarget target)
    {
        // Find ZLoggerOptions type
        var zloggerOptionsType = FindZLoggerOptionsType();
        if (zloggerOptionsType == null)
        {
            // Return an empty action if we can't find ZLoggerOptions
            return new Action<object>(_ => { });
        }

        var actionType = typeof(Action<>).MakeGenericType(zloggerOptionsType);

        // Create a method that configures the options
        var method = typeof(ZLoggerConfigurationBuilder)
            .GetMethod(nameof(ConfigureZLoggerOptions), BindingFlags.NonPublic | BindingFlags.Static)
            ?.MakeGenericMethod(zloggerOptionsType);

        if (method == null)
        {
            return new Action<object>(_ => { });
        }

        // Create a delegate that captures the target configuration
        var configureMethod = method.CreateDelegate(actionType, target);
        return configureMethod;
    }

    /// <summary>
    /// Configures ZLogger options from FlexKit target configuration.
    /// </summary>
    /// <typeparam name="T">The ZLoggerOptions type.</typeparam>
    /// <param name="target">The FlexKit target configuration.</param>
    /// <param name="options">The ZLogger options instance to configure.</param>
    private static void ConfigureZLoggerOptions<T>(
        LoggingTarget target,
        T options) where T : class
    {
        var properties = typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite);

        foreach (var prop in properties)
        {
            var configSection = FindPropertySection(target.Properties, prop.Name);
            if (configSection == null)
            {
                continue;
            }

            try
            {
                var value = ConvertConfigurationValue(configSection, prop.PropertyType);
                if (value != null)
                {
                    prop.SetValue(options, value);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Warning: Failed to set ZLoggerOptions property '{prop.Name}': {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Finds the ZLoggerOptions type from loaded assemblies.
    /// </summary>
    /// <returns>The ZLoggerOptions type, or null if not found.</returns>
    private static Type? FindZLoggerOptionsType()
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                var types = assembly.GetTypes();
                var optionsType = types.FirstOrDefault(t => t.Name.Contains("ZLoggerOptions", StringComparison.OrdinalIgnoreCase));
                if (optionsType != null)
                {
                    return optionsType;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Warning: Failed to scan assembly '{assembly.FullName}' for ZLoggerOptions: {ex.Message}");
            }
        }

        return null;
    }

    /// <summary>
    /// Configures a fallback console logger when no targets are configured.
    /// </summary>
    /// <param name="loggingBuilder">The ILoggingBuilder to configure.</param>
    private void ConfigureFallbackConsoleLogger(ILoggingBuilder loggingBuilder)
    {
        if (_availableProcessors.TryGetValue("Console", out var consoleProcessor) &&
            consoleProcessor.IsBuiltIn &&
            consoleProcessor.ExtensionMethod != null)
        {
            try
            {
                // Call AddZLoggerConsole() without parameters
                var method = consoleProcessor.ExtensionMethod;
                if (method.GetParameters().Length == 1) // Just ILoggingBuilder
                {
                    method.Invoke(null, [loggingBuilder]);
                    return;
                }

                // Try to find a simpler overload
                var simpleMethod = method.DeclaringType?.GetMethod("AddZLoggerConsole", [typeof(ILoggingBuilder)]);
                if (simpleMethod != null)
                {
                    simpleMethod.Invoke(null, [loggingBuilder]);
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Warning: Failed to configure fallback ZLogger console: {ex.Message}");
            }
        }

        Debug.WriteLine("Warning: Could not configure fallback ZLogger console - no suitable method found");
    }

    /// <summary>
    /// Finds a matching configuration section for the specified property name.
    /// </summary>
    /// <param name="properties">Dictionary of properties to search.</param>
    /// <param name="propertyName">The property name to find.</param>
    /// <returns>The matching configuration section, or null if not found.</returns>
    private static IConfigurationSection? FindPropertySection(
        Dictionary<string, IConfigurationSection?> properties,
        string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
        {
            return null;
        }

        // Try the exact match first
        if (properties.TryGetValue(propertyName, out var exactMatch))
        {
            return exactMatch;
        }

        // Try a case-insensitive match
        var kvp = properties.FirstOrDefault(p =>
            string.Equals(p.Key, propertyName, StringComparison.OrdinalIgnoreCase));

        return kvp.Value;
    }

    /// <summary>
    /// Converts a configuration section value to the specified type.
    /// </summary>
    /// <param name="configSection">The configuration section containing the value.</param>
    /// <param name="targetType">The type to convert the value to.</param>
    /// <returns>The converted value, or null if the conversion failed.</returns>
    private static object? ConvertConfigurationValue(
        IConfigurationSection configSection,
        Type targetType)
    {
        try
        {
            if (configSection.Value == null)
            {
                return configSection.Get(targetType);
            }

            return targetType.IsEnum
                ? Enum.Parse(targetType, configSection.Value, true)
                : Convert.ChangeType(configSection.Value, targetType, CultureInfo.InvariantCulture);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Safely extracts a string value from target properties.
    /// </summary>
    /// <param name="target">The FlexKit target configuration.</param>
    /// <param name="propertyName">The name of the property to extract.</param>
    /// <returns>The string value if found, null otherwise.</returns>
    private static string? GetStringPropertyFromTarget(LoggingTarget target, string propertyName) =>
        target.Properties.TryGetValue(propertyName, out var configSection)
            ? configSection?.Value
            : null;

    /// <summary>
    /// Gets the ZLogger provider type based on the processor name.
    /// </summary>
    /// <param name="processorName">The name of the processor (e.g., "Console", "File", "Debug").</param>
    /// <returns>The corresponding ZLogger provider type, or null if not found.</returns>
    private static Type? GetZLoggerProviderType(string processorName)
    {
        // Map processor names to their corresponding ZLogger provider types
        var providerTypeName = processorName switch
        {
            "Console" => "ZLogger.Providers.ZLoggerConsoleLoggerProvider, ZLogger",
            "File" => "ZLogger.Providers.ZLoggerFileLoggerProvider, ZLogger",
            "RollingFile" => "ZLogger.Providers.ZLoggerRollingFileLoggerProvider, ZLogger",
            "Stream" => "ZLogger.Providers.ZLoggerStreamLoggerProvider, ZLogger",
            "InMemory" => "ZLogger.Providers.ZLoggerInMemoryLoggerProvider, ZLogger",
            "LogProcessor" => "ZLogger.Providers.ZLoggerLogProcessorLoggerProvider, ZLogger",
            _ => null
        };

        return providerTypeName != null ? Type.GetType(providerTypeName) : null;
    }

    /// <summary>
    /// Adds logging filters specific to a ZLogger provider based on the provided logging target configuration.
    /// This ensures each provider only processes logs from its designated category.
    /// </summary>
    /// <param name="context">Context for the current configuration.</param>
    /// <param name="types">Array of all target types for cross-category filtering.</param>
    private static void AddFiltersForProvider(
        BuildContext context,
        string[] types)
    {
        var filterType = Type.GetType(
            "Microsoft.Extensions.Logging.FilterLoggingBuilderExtensions, Microsoft.Extensions.Logging");

        // Find the generic AddFilter<T> method: AddFilter<T>(ILoggingBuilder, string?, LogLevel)
        var genericMethod = filterType?.GetMethods()
            .FirstOrDefault(IsAddFilter);

        if (genericMethod == null)
        {
            Debug.WriteLine("Warning: Could not find AddFilter method for ZLogger filtering");
            return;
        }

        // Make it specific to our provider type
        Debug.Assert(context.ProviderType != null);
        var specificMethod = genericMethod.MakeGenericMethod(context.ProviderType);

        try
        {
            // Block other categories from this provider
            foreach (var otherCategory in types.Where(t => t != context.Target.Type))
            {
                specificMethod.Invoke(null, [context.LoggingBuilder, otherCategory, LogLevel.None]);
            }

            // Allow this target's category through
            specificMethod.Invoke(null, [context.LoggingBuilder, context.Target.Type, GetLogLevel(context.Target)]);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Warning: Failed to configure filters for provider '{context.ProviderType?.Name}': {ex.Message}");
        }

        static bool IsAddFilter(MethodInfo m) =>
            m is { Name: "AddFilter", IsGenericMethodDefinition: true } &&
            m.GetParameters().Length == 3 &&
            m.GetParameters()[1].ParameterType == typeof(string) &&
            m.GetParameters()[2].ParameterType == typeof(LogLevel);
    }

    /// <summary>
    /// Determines the log level for the specified logging target by analyzing
    /// its properties. If a log level is not explicitly specified, a default
    /// value of LogLevel.Information is returned.
    /// </summary>
    /// <param name="target">The logging target whose log level is to be determined.</param>
    /// <returns>
    /// The determined log level for the given logging target, or LogLevel.Information if none
    /// is specified or the value is invalid.
    /// </returns>
    private static LogLevel GetLogLevel(LoggingTarget target)
    {
        if (!target.Properties.TryGetValue("LogLevel", out var level))
        {
            return LogLevel.Information;
        }

        return level?.Value is { } levelString && Enum.TryParse<LogLevel>(levelString, out var parsedLevel)
            ? parsedLevel
            : LogLevel.Information;
    }

    /// <summary>
    /// Represents the context required to build and configure a logging processor.
    /// Holds information about the target logging configuration, the logging builder,
    /// and optionally the provider type associated with the processor.
    /// </summary>
    private sealed record BuildContext
    {
        /// <summary>
        /// Specifies the type of the provider associated with the logging configuration.
        /// This property is primarily used to identify and configure the appropriate logging provider
        /// during the setup of a logging pipeline.
        /// </summary>
        public Type? ProviderType { get; set; }

        /// <summary>
        /// Represents the logging target configuration used during the building and setup
        /// of a logging processor. This property provides access to the underlying configuration,
        /// enabling the customization of logging behavior for specific targets.
        /// </summary>
        public required LoggingTarget Target { get; init; }

        /// <summary>
        /// Provides the logging builder used in the context of configuring logging processors.
        /// This property is integral for setting up logging providers and tailoring their behavior
        /// based on the specified logging configuration.
        /// </summary>
        public required ILoggingBuilder LoggingBuilder { get; init; }
    }
}
