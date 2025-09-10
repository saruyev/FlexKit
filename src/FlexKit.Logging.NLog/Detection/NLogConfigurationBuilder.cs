using System.Diagnostics;
using System.Globalization;
using FlexKit.Logging.Configuration;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using NLog;
using NLog.Config;
using NLog.Filters;
using NLog.Layouts;
using NLog.Targets;
using NLog.Targets.Wrappers;

namespace FlexKit.Logging.NLog.Detection;

/// <summary>
/// Builds NLog LoggingConfiguration from FlexKit LoggingConfig by dynamically
/// detecting available targets and configuring them using reflection.
/// </summary>
public class NLogConfigurationBuilder
{
    private readonly Dictionary<string, NLogTargetDetector.TargetInfo> _availableTargets;

    /// <summary>
    /// Initializes a new instance of NLogConfigurationBuilder with auto-detected targets.
    /// </summary>
    public NLogConfigurationBuilder()
    {
        _availableTargets = NLogTargetDetector.DetectAvailableTargets();
    }

    /// <summary>
    /// Builds an NLog LoggingConfiguration from FlexKit LoggingConfig.
    /// </summary>
    /// <param name="config">FlexKit logging configuration.</param>
    /// <returns>Configured NLog LoggingConfiguration ready for LogManager.Configuration assignment.</returns>
    public LoggingConfiguration BuildConfiguration(LoggingConfig config)
    {
        var nlogConfig = new LoggingConfiguration();

        // Configure targets from FlexKit configuration
        var targetsConfigured = ConfigureTargets(config, nlogConfig);

        // Fallback to Console if no targets configured
        if (targetsConfigured != 0)
        {
            return nlogConfig;
        }

        var consoleTarget = new ConsoleTarget("console")
        {
            Layout =
                @"${date:format=yyyy-MM-dd HH\:mm\:ss} [${level:uppercase=true:padding=-5}] ${message} ${exception:format=tostring}"
        };
        nlogConfig.AddTarget(consoleTarget);
        nlogConfig.AddRuleForAllLevels(consoleTarget);

        return nlogConfig;
    }

    /// <summary>
    /// Configures the logging targets based on the provided FlexKit logging configuration.
    /// </summary>
    /// <param name="config">The FlexKit logging configuration containing the targets to configure.</param>
    /// <param name="nlogConfig">The NLog logging configuration to be updated with the configured targets.</param>
    /// <returns>The number of successfully configured logging targets.</returns>
    private int ConfigureTargets(
        LoggingConfig config,
        LoggingConfiguration nlogConfig) =>
        config.Targets.Values
            .Where(t => t.Enabled)
            .Count(target => TryConfigureTarget(target, nlogConfig));

    /// <summary>
    /// Attempts to configure a logging target for the specified FlexKit logging target.
    /// </summary>
    /// <param name="target">The FlexKit logging target to configure.</param>
    /// <param name="nlogConfig">The NLog configuration to add the target to.</param>
    /// <returns>True if the target was successfully configured; otherwise, false.</returns>
    private bool TryConfigureTarget(
        LoggingTarget target,
        LoggingConfiguration nlogConfig)
    {
        if (!_availableTargets.TryGetValue(target.Type, out var targetInfo))
        {
            return false;
        }

        try
        {
            // Create a target instance
            var nlogTarget = CreateTargetInstance(targetInfo, target);
            if (nlogTarget == null)
            {
                return false;
            }

            // Wrap with AsyncWrapper if the target supports async and not already async
            if (targetInfo.SupportsAsync && ShouldWrapWithAsync(nlogTarget))
            {
                nlogTarget = new AsyncTargetWrapper(nlogTarget)
                {
                    Name = target.Type + "_Async",
                };
            }

            // Add target to configuration
            nlogConfig.AddTarget(nlogTarget);

            // Create logging rules for this target
            CreateLoggingRules(nlogConfig, nlogTarget, target);
            CreateMelBridgeLoggingRules(nlogConfig, nlogTarget, target);

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Warning: Failed to configure NLog target {target.Type}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Creates an instance of the specified NLog target and configures it with FlexKit properties.
    /// </summary>
    /// <param name="targetInfo">Information about the target type and its properties.</param>
    /// <param name="flexKitTarget">FlexKit target configuration with properties.</param>
    /// <returns>Configured NLog target instance, or null if creation failed.</returns>
    private static Target? CreateTargetInstance(
        NLogTargetDetector.TargetInfo targetInfo,
        LoggingTarget flexKitTarget)
    {
        try
        {
            // Create a target instance
            var target = (Target?)Activator.CreateInstance(targetInfo.TargetType);
            if (target == null)
            {
                return null;
            }

            // Set a target name
            target.Name = flexKitTarget.Type;

            // Configure target properties from FlexKit configuration
            ConfigureTargetProperties(target, targetInfo, flexKitTarget);

            return target;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Configures the properties of an NLog target using FlexKit configuration values.
    /// </summary>
    /// <param name="target">The NLog target to configure.</param>
    /// <param name="targetInfo">Information about available target properties.</param>
    /// <param name="flexKitTarget">FlexKit configuration containing property values.</param>
    private static void ConfigureTargetProperties(
        Target target,
        NLogTargetDetector.TargetInfo targetInfo,
        LoggingTarget flexKitTarget)
    {
        foreach (var property in targetInfo.Properties)
        {
            var configSection = FindPropertySection(flexKitTarget.Properties, property.Name);
            if (configSection == null)
            {
                continue;
            }

            try
            {
                var value = ConvertConfigurationValue(configSection, property.PropertyType);
                if (value != null)
                {
                    property.SetValue(target, value);
                }
            }
            catch
            {
                // Skip properties that fail to set
            }
        }
    }

    /// <summary>
    /// Creates logging rules for the specified target based on FlexKit configuration.
    /// </summary>
    /// <param name="nlogConfig">The NLog configuration to add rules to.</param>
    /// <param name="target">The target to create rules for.</param>
    /// <param name="flexKitTarget">FlexKit target configuration.</param>
    private static void CreateLoggingRules(
        LoggingConfiguration nlogConfig,
        Target target,
        LoggingTarget flexKitTarget)
    {
        // Create a rule that filters by Target property to route FlexKit entries
        // This matches the Serilog pattern where each target gets a filtered log event
        var rule = new LoggingRule("*", GetLogLevelFromTarget(flexKitTarget), target);

        // Add filter condition to only accept events with matching Target property
        // This ensures FlexKit's target routing works correctly
        rule.Filters.Add(new WhenContainsFilter
        {
            Layout = "${event-properties:Target}",
            Substring = flexKitTarget.Type,
            Action = FilterResult.Log,
            IgnoreCase = true,
        });

        // Set the default action to ignore events that don't match the filter
        rule.FilterDefaultAction = FilterResult.Ignore;

        nlogConfig.LoggingRules.Add(rule);
    }

    /// <summary>
    /// Creates logging rules for MEL bridge logging (framework logs from ASP.NET Core, etc.).
    /// These logs don't have the Target property, so we route them to all targets.
    /// </summary>
    /// <param name="nlogConfig">The NLog configuration to add rules to.</param>
    /// <param name="target">The target to create rules for.</param>
    /// <param name="flexKitTarget">FlexKit target configuration.</param>
    private static void CreateMelBridgeLoggingRules(
        LoggingConfiguration nlogConfig,
        Target target,
        LoggingTarget flexKitTarget)
    {
        // Create a rule for MEL bridge logs (framework logs without Target property)
        var rule = new LoggingRule("*", GetLogLevelFromTarget(flexKitTarget), target);

        // Only accept events that DON'T have the Target property (MEL bridge logs)
        rule.Filters.Add(new WhenNotExistsFilter
        {
            Layout = "${event-properties:Target}",
            Action = FilterResult.Log,
        });

        // Set the default action to ignore events that have the Target property
        rule.FilterDefaultAction = FilterResult.Ignore;

        nlogConfig.LoggingRules.Add(rule);
    }

    /// <summary>
    /// Determines whether a target should be wrapped with AsyncTargetWrapper.
    /// </summary>
    /// <param name="target">The target to evaluate.</param>
    /// <returns>True if the target should be wrapped; otherwise, false.</returns>
    private static bool ShouldWrapWithAsync(Target target)
    {
        // Don't wrap if already an async wrapper
        if (target is AsyncTargetWrapper)
        {
            return false;
        }

        // Don't wrap certain target types that handle async internally
        var targetType = target.GetType().Name;
        var selfAsyncTargets = new[]
        {
            "NetworkTarget", "DatabaseTarget", "WebServiceTarget"
        };

        return !selfAsyncTargets.Any(asyncType =>
            targetType.Contains(asyncType, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Finds a matching configuration section for the specified property name within a given collection of properties.
    /// </summary>
    /// <param name="properties">A dictionary containing property names and their corresponding configuration sections.</param>
    /// <param name="propertyName">The name of the property for which to find a matching configuration section.</param>
    /// <returns>The configuration section that matches the property name, or null if no match is found.</returns>
    private static IConfigurationSection? FindPropertySection(
        Dictionary<string, IConfigurationSection?> properties,
        string? propertyName) =>
        string.IsNullOrEmpty(propertyName)
            ? null
            : properties.FirstOrDefault(kvp =>
                string.Equals(kvp.Key, propertyName, StringComparison.OrdinalIgnoreCase)).Value;

    /// <summary>
    /// Converts a configuration value from an IConfigurationSection into an object
    /// of the specified target type.
    /// </summary>
    /// <param name="configSection">The configuration section containing the value to convert.</param>
    /// <param name="targetType">The type to which the value should be converted.</param>
    /// <returns>The converted value as an object of the specified type, or null if conversion fails.</returns>
    private static object? ConvertConfigurationValue(
        IConfigurationSection configSection,
        Type targetType)
    {
        try
        {
            if (configSection.Value == null)
            {
                // Try to bind a complex object from configuration
                return configSection.Get(targetType);
            }

            if (targetType.IsEnum)
            {
                // Handle enum conversion from string (case-insensitive)
                return Enum.Parse(targetType, configSection.Value, ignoreCase: true);
            }

            if (targetType == typeof(string))
            {
                // Handle NLog layout strings directly
                return configSection.Value;
            }

            if (targetType == typeof(Layout))
            {
                return Layout.FromString(configSection.Value);
            }

            // Handle primitive type conversion
            return Convert.ChangeType(
                configSection.Value,
                targetType,
                CultureInfo.InvariantCulture);
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
    /// <returns>The default value of the specified type.</returns>
    private static object? GetDefaultValueForType(Type type) =>
        type.IsValueType ? Activator.CreateInstance(type) : null;

    /// <summary>
    /// Gets the log level from target properties, defaulting to Debug if not specified.
    /// </summary>
    /// <param name="target">The FlexKit target configuration.</param>
    /// <returns>The Log4Net Level corresponding to the target's LogLevel property.</returns>
    private static LogLevel GetLogLevelFromTarget(LoggingTarget target)
    {
        var logLevelString = target.Properties.TryGetValue("LogLevel", out var configSection)
            ? configSection?.Value
            : null;

        if (string.IsNullOrEmpty(logLevelString))
        {
            return LogLevel.Info;
        }

        // Convert from MEL LogLevel string to Log4Net Level
        return logLevelString.ToUpperInvariant() switch
        {
            "TRACE" => LogLevel.Trace,
            "DEBUG" => LogLevel.Debug,
            "INFORMATION" => LogLevel.Info,
            "WARNING" => LogLevel.Warn,
            "ERROR" => LogLevel.Error,
            "CRITICAL" => LogLevel.Fatal,
            "NONE" => LogLevel.Off,
            _ => LogLevel.Info,
        };
    }

    /// <summary>
    /// Custom NLog filter that checks if a layout renderer value exists (is not empty).
    /// </summary>
    private sealed class WhenNotExistsFilter : Filter
    {
        /// <summary>
        /// Gets or sets the layout to evaluate.
        /// </summary>
        public Layout? Layout { get; [UsedImplicitly] set; }

        /// <inheritdoc />
        protected override FilterResult Check(LogEventInfo logEvent)
        {
            if (Layout == null)
            {
                return FilterResult.Neutral;
            }

            var value = Layout.Render(logEvent);

            // If the layout renders to an empty string, the property doesn't exist
            return string.IsNullOrEmpty(value) ? FilterResult.Log : FilterResult.Ignore;
        }
    }
}
