using System.Globalization;
using System.Reflection;
using FlexKit.Logging.Configuration;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace FlexKit.Logging.Serilog.Detection;

/// <summary>
/// Builds Serilog LoggerConfiguration from FlexKit LoggingConfig by dynamically
/// detecting available sinks and enrichers and configuring them using reflection.
/// </summary>
public class SerilogConfigurationBuilder
{
    private readonly Dictionary<string, SerilogComponentDetector.ComponentInfo> _availableSinks;
    private readonly Dictionary<string, SerilogComponentDetector.ComponentInfo> _availableEnrichers;

    /// <summary>
    /// Initializes a new instance of SerilogConfigurationBuilder with auto-detected sinks and enrichers.
    /// </summary>
    public SerilogConfigurationBuilder()
    {
        _availableSinks = SerilogComponentDetector.DetectAvailableSinks();
        _availableEnrichers = SerilogComponentDetector.DetectAvailableEnrichers();
    }

    /// <summary>
    /// Builds a Serilog LoggerConfiguration from FlexKit LoggingConfig.
    /// </summary>
    /// <param name="config">FlexKit logging configuration.</param>
    /// <returns>Configured Serilog LoggerConfiguration ready for CreateLogger().</returns>
    public LoggerConfiguration BuildConfiguration(LoggingConfig config)
    {
        var loggerConfig = new LoggerConfiguration();

        // Set the minimum level (default to Debug if not specified)
        loggerConfig.MinimumLevel.Debug();

        // Configure enrichers (built-in + auto-detected)
        ConfigureEnrichers(loggerConfig);

        var sinksConfigured = ConfigureTargets(config, loggerConfig);

        // Fallback to Console if no sinks configured
        if (sinksConfigured == 0)
        {
            loggerConfig.WriteTo.Console(formatProvider: CultureInfo.InvariantCulture);
        }

        return loggerConfig;
    }

    /// <summary>
    /// Configures enrichers: built-in FromLogContext plus auto-detected common enrichers.
    /// </summary>
    /// <param name="loggerConfig">LoggerConfiguration to configure.</param>
    private void ConfigureEnrichers(LoggerConfiguration loggerConfig)
    {
        // Always configure FromLogContext (built into Serilog core)
        loggerConfig.Enrich.FromLogContext();

        // Add common enrichers if available
        TryAddEnricher(loggerConfig, "WithThreadId");
        TryAddEnricher(loggerConfig, "WithMachineName");
        TryAddEnricher(loggerConfig, "WithEnvironmentName");
        TryAddEnricher(loggerConfig, "WithProcessId");
        TryAddEnricher(loggerConfig, "WithProcessName");
    }

    /// <summary>
    /// Attempts to add an enricher if it's available.
    /// </summary>
    /// <param name="loggerConfig">LoggerConfiguration to configure.</param>
    /// <param name="enricherName">Name of the enricher to add.</param>
    private void TryAddEnricher(
        LoggerConfiguration loggerConfig,
        string enricherName)
    {
        if (!_availableEnrichers.TryGetValue(enricherName, out var enricherInfo))
        {
            return;
        }

        try
        {
            // Most enrichers have no parameters, just invoke directly
            var args = new object[] { loggerConfig.Enrich };
            enricherInfo.Method.Invoke(null, args);
        }
        catch
        {
            // Skip enrichers that fail to initialize
        }
    }

    /// <summary>
    /// Configures the logging targets based on the provided logging configuration.
    /// </summary>
    /// <param name="config">The logging configuration containing the details of the targets to configure.</param>
    /// <param name="loggerConfig">The Serilog logger configuration to be updated with the configured targets.</param>
    /// <returns>The number of successfully configured logging targets.</returns>
    private int ConfigureTargets(
        LoggingConfig config,
        LoggerConfiguration loggerConfig) =>
        config.Targets.Values
            .Where(t => t.Enabled)
            .Count(target => TryConfigureSink(target, loggerConfig));

    /// <summary>
    /// Attempts to configure a logging sink for the specified logging target using the provided configurations.
    /// </summary>
    /// <param name="target">
    /// The specific logging target to configure, including its type and related properties.
    /// </param>
    /// <param name="config">
    /// The Serilog LoggerConfiguration to apply the sink configuration to.
    /// </param>
    /// <returns>
    /// A boolean value indicating whether the sink was successfully configured. Returns <c>true</c> if
    /// the sink was configured without errors; otherwise, returns <c>false</c>.
    /// </returns>
    private bool TryConfigureSink(
        LoggingTarget target,
        LoggerConfiguration config)
    {
        if (!_availableSinks.TryGetValue(target.Type, out var sinkInfo))
        {
            return false;
        }

        try
        {
            config.WriteTo.Logger(lc =>
            {
                lc.Filter.ByIncludingOnly(e =>
                    e.Properties.ContainsKey("Target") &&
                    e.Properties["Target"].ToString().Contains(target.Type));
                InvokeSinkMethod(sinkInfo, target, lc);
            });
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Invokes a method of a detected Serilog sink using reflection with the provided target
    /// and configuration details.
    /// </summary>
    /// <param name="sinkInfo">Information about the detected sink, including its method and parameters.</param>
    /// <param name="target">The logging target that contains properties used to configure the sink.</param>
    /// <param name="config">The LoggerConfiguration instance used as the first argument to the sink's method.</param>
    private static void InvokeSinkMethod(
        SerilogComponentDetector.ComponentInfo sinkInfo,
        LoggingTarget target,
        LoggerConfiguration config)
    {
        // Build arguments' array: LoggerSinkConfiguration + method parameters
        var args = new object?[sinkInfo.Parameters.Length + 1];
        args[0] = config.WriteTo; // The first parameter is always LoggerSinkConfiguration

        // Map FlexKit properties to method parameters
        for (var i = 0; i < sinkInfo.Parameters.Length; i++)
        {
            args[i + 1] = ExtractParameterValue(target.Properties, sinkInfo.Parameters[i]);
        }

        // Invoke the extension method: e.g., WriteTo.File(...), WriteTo.Console(...)
        sinkInfo.Method.Invoke(null, args);
    }

    /// <summary>
    /// Extracts the value of a parameter from the given properties dictionary, considering
    /// the parameter's name, type, and optional default value.
    /// </summary>
    /// <param name="properties">
    /// A dictionary containing property keys and their corresponding configuration sections.
    /// </param>
    /// <param name="parameter">
    /// Information about the parameter, including its name, type, and default value (if any).
    /// </param>
    /// <returns>
    /// The extracted value for the parameter, converted to the parameter's type. If the parameter
    /// is not found in the properties, returns its default value if specified; otherwise, returns
    /// the default value for the parameter's type.
    /// </returns>
    private static object? ExtractParameterValue(
        Dictionary<string, IConfigurationSection?> properties,
        ParameterInfo parameter)
    {
        var paramName = parameter.Name;
        var paramType = parameter.ParameterType;

        // Try to find property by parameter name (case-insensitive)
        var configSection = FindPropertySection(properties, paramName);

        if (configSection == null)
        {
            // Return default value if parameter is optional
            return parameter.HasDefaultValue ? parameter.DefaultValue : GetDefaultValueForType(paramType);
        }

        return ConvertConfigurationValue(configSection, paramType);
    }

    /// <summary>
    /// Finds a matching configuration section for the specified parameter name within a given collection of properties.
    /// </summary>
    /// <param name="properties">
    /// A dictionary containing property names and their corresponding configuration sections.
    /// </param>
    /// <param name="parameterName">
    /// The name of the parameter for which to find a matching configuration section.
    /// </param>
    /// <returns>The configuration section that matches the parameter name, or null if no match is found.</returns>
    private static IConfigurationSection? FindPropertySection(
        Dictionary<string, IConfigurationSection?> properties,
        string? parameterName) =>
        string.IsNullOrEmpty(parameterName)
            ? null
            : properties.FirstOrDefault(kvp =>
                string.Equals(kvp.Key, parameterName, StringComparison.OrdinalIgnoreCase)).Value;

    /// <summary>
    /// Converts a configuration value from an <see cref="IConfigurationSection"/> into an object
    /// of the specified target type.
    /// </summary>
    /// <param name="configSection">The configuration section containing the value to convert.</param>
    /// <param name="targetType">The type to which the value should be converted.</param>
    /// <returns>
    /// The converted value as an object of the specified type, or a default value of the target
    /// type if the conversion fails.
    /// </returns>
    private static object? ConvertConfigurationValue(
        IConfigurationSection configSection,
        Type targetType)
    {
        try
        {
            if (configSection.Value == null)
            {
                // Try to bind a complex object from configuration
                return Convert.ChangeType(
                    configSection.Get(targetType),
                    targetType,
                    CultureInfo.InvariantCulture);
            }
            else if (targetType.IsEnum)
            {
                // Handle enum conversion from string (case-insensitive)
                return Enum.Parse(targetType, configSection.Value, ignoreCase: true);
            }
            else
            {
                // Handle primitive type conversion
                return Convert.ChangeType(
                    configSection.Value,
                    targetType,
                    CultureInfo.InvariantCulture);
            }
        }
        catch (Exception)
        {
            // Return default value on conversion failure
            return GetDefaultValueForType(targetType);
        }
    }

    /// <summary>
    /// Gets the default value for a specified type.
    /// </summary>
    /// <param name="type">The type for which to get the default value.</param>
    /// <returns>
    /// The default value of the specified type. If the type is a value type, its default instance
    /// is returned; otherwise, null is returned for reference types.
    /// </returns>
    private static object? GetDefaultValueForType(Type type) =>
        type.IsValueType ? Activator.CreateInstance(type) : null;
}
