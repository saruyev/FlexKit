using System.Diagnostics;
using System.Reflection;
using FlexKit.Logging.Configuration;
using FlexKit.Logging.Detection;
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
            var success = processorInfo.IsBuiltIn
                ? context.ConfigureBuiltInProcessor(processorInfo)
                : context.ConfigureAsyncLogProcessor(processorInfo);

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
                if (InvokeConsoleLoggerMethod(loggingBuilder, consoleProcessor))
                {
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

    private static bool InvokeConsoleLoggerMethod(ILoggingBuilder loggingBuilder, ZLoggerProcessorDetector.ProcessorInfo consoleProcessor)
    {
        // Call AddZLoggerConsole() without parameters
        var method = consoleProcessor.ExtensionMethod;
        if (method?.GetParameters().Length == 1) // Just ILoggingBuilder
        {
            method.Invoke(null, [loggingBuilder]);
            return true;
        }

        // Try to find a simpler overload
        var simpleMethod = method?.DeclaringType?.GetMethod("AddZLoggerConsole", [typeof(ILoggingBuilder)]);
        if (simpleMethod == null)
        {
            return false;
        }

        simpleMethod.Invoke(null, [loggingBuilder]);
        return true;

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
        var filterType = Type.GetType(MelExtensions.FilterType);

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
}
