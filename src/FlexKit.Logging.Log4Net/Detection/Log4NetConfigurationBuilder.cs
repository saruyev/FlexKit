using System.Diagnostics;
using System.Globalization;
using FlexKit.Logging.Configuration;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Repository;
using log4net.Repository.Hierarchy;
using Microsoft.Extensions.Configuration;

namespace FlexKit.Logging.Log4Net.Detection;

/// <summary>
/// Builds Log4Net configuration from FlexKit LoggingConfig by dynamically
/// detecting available appenders and configuring them using reflection.
/// </summary>
public class Log4NetConfigurationBuilder
{
    private readonly Dictionary<string, Log4NetAppenderDetector.AppenderInfo> _availableAppenders;

    /// <summary>
    /// Initializes a new instance of Log4NetConfigurationBuilder with auto-detected appenders.
    /// </summary>
    public Log4NetConfigurationBuilder()
    {
        _availableAppenders = Log4NetAppenderDetector.DetectAvailableAppenders();
    }

    /// <summary>
    /// Builds a Log4Net configuration from FlexKit LoggingConfig and applies it to the default repository.
    /// </summary>
    /// <param name="config">FlexKit logging configuration.</param>
    /// <returns>The configured Log4Net repository for use with LogManager.</returns>
    public ILoggerRepository BuildConfiguration(LoggingConfig config)
    {
        var repository = LogManager.GetRepository();

        // Clear existing configuration
        repository.ResetConfiguration();

        // Configure appenders from FlexKit configuration
        var appendersConfigured = ConfigureTargets(config, repository);

        // Fallback to Console if no appenders configured
        if (appendersConfigured == 0)
        {
            ConfigureFallbackConsoleAppender(repository);
        }

        repository.Threshold = Level.Debug;
        repository.Configured = true;

        return repository;
    }

    /// <summary>
    /// Configures the logging targets based on the provided FlexKit logging configuration.
    /// </summary>
    /// <param name="config">The FlexKit logging configuration containing the targets to configure.</param>
    /// <param name="repository">The Log4Net repository to configure with appenders.</param>
    /// <returns>The number of successfully configured logging appenders.</returns>
    private int ConfigureTargets(
        LoggingConfig config,
        ILoggerRepository repository) =>
        config.Targets.Values
            .Where(t => t.Enabled)
            .Count(target => TryConfigureAppender(target, repository));

    /// <summary>
    /// Attempts to configure a logging appender for the specified FlexKit logging target.
    /// </summary>
    /// <param name="target">The FlexKit logging target to configure.</param>
    /// <param name="repository">The Log4Net repository to add the appender to.</param>
    /// <returns>True if the appender was successfully configured; otherwise, false.</returns>
    private bool TryConfigureAppender(
        LoggingTarget target,
        ILoggerRepository repository)
    {
        if (!_availableAppenders.TryGetValue(target.Type, out var appenderInfo))
        {
            return false;
        }

        try
        {
            // Create an appender instance
            var appender = CreateAppenderInstance(appenderInfo, target);
            if (appender == null)
            {
                return false;
            }

            // Configure the appender with FlexKit properties
            ConfigureAppenderProperties(appender, appenderInfo, target);

            // Set default layout if isn't already configured
            SetDefaultLayoutIfNeeded(appender, target);

            // Activate the appender if required
            if (appenderInfo.RequiresActivation && appender is IOptionHandler optionHandler)
            {
                optionHandler.ActivateOptions();
            }

            // Create a logger for this specific target
            CreateTargetSpecificLogger(repository, appender, target);

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Warning: Failed to configure Log4Net appender {target.Type}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Creates an instance of the specified Log4Net appender using dynamic instantiation.
    /// </summary>
    /// <param name="appenderInfo">Information about the appender type.</param>
    /// <param name="flexKitTarget">FlexKit target configuration.</param>
    /// <returns>Configured Log4Net appender instance, or null if creation failed.</returns>
    private static IAppender? CreateAppenderInstance(
        Log4NetAppenderDetector.AppenderInfo appenderInfo,
        LoggingTarget flexKitTarget)
    {
        try
        {
            // Create an appender instance dynamically
            var appender = (IAppender?)Activator.CreateInstance(appenderInfo.AppenderType);
            if (appender == null)
            {
                return null;
            }

            // Set appender name to match the target type for targeting
            appender.Name = flexKitTarget.Type;

            return appender;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Configures the properties of a Log4Net appender using FlexKit configuration values.
    /// </summary>
    /// <param name="appender">The Log4Net appender to configure.</param>
    /// <param name="appenderInfo">Information about available properties.</param>
    /// <param name="flexKitTarget">FlexKit target configuration with property values.</param>
    private static void ConfigureAppenderProperties(
        IAppender appender,
        Log4NetAppenderDetector.AppenderInfo appenderInfo,
        LoggingTarget flexKitTarget)
    {
        // Apply FlexKit configuration properties to the appender
        foreach (var property in appenderInfo.Properties)
        {
            var configSection = FindPropertySection(flexKitTarget.Properties, property.Name);
            if (configSection?.Value == null)
            {
                continue;
            }

            try
            {
                // Convert and set the property value
                var convertedValue = Convert.ChangeType(configSection.Value, property.PropertyType, CultureInfo.InvariantCulture);
                property.SetValue(appender, convertedValue);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Warning: Failed to set property {property.Name} on appender {appender.Name}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Finds a matching configuration section for the specified property name.
    /// </summary>
    /// <param name="properties">Dictionary of property names to configuration sections.</param>
    /// <param name="propertyName">The property name to search for (case-insensitive).</param>
    /// <returns>The matching configuration section, or null if not found.</returns>
    private static IConfigurationSection? FindPropertySection(
        Dictionary<string, IConfigurationSection?> properties,
        string? propertyName)
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
    /// Sets a default layout for the appender if one is not already configured.
    /// </summary>
    /// <param name="appender">The appender to configure.</param>
    /// <param name="target">The FlexKit target configuration.</param>
    private static void SetDefaultLayoutIfNeeded(IAppender appender, LoggingTarget target)
    {
        // Only set layout on appenders that support it
        if (appender is not AppenderSkeleton skeletonAppender)
        {
            return;
        }

        // Skip if the layout is already set
        if (skeletonAppender.Layout != null)
        {
            return;
        }

        // Get pattern from target properties
        var defaultPattern = GetStringPropertyFromTarget(target, "Pattern")
            ?? "%date [%thread] %-5level %logger - %message%newline";

        skeletonAppender.Layout = new PatternLayout(defaultPattern);
    }

    /// <summary>
    /// Gets the log level from target properties, defaulting to Debug if not specified.
    /// </summary>
    /// <param name="target">The FlexKit target configuration.</param>
    /// <returns>The Log4Net Level corresponding to the target's LogLevel property.</returns>
    private static Level GetLogLevelFromTarget(LoggingTarget target)
    {
        var logLevelString = GetStringPropertyFromTarget(target, "LogLevel");

        if (string.IsNullOrEmpty(logLevelString))
        {
            return Level.Info;
        }

        // Convert from MEL LogLevel string to Log4Net Level
        return logLevelString.ToUpperInvariant() switch
        {
            "TRACE" => Level.Trace,
            "DEBUG" => Level.Debug,
            "INFORMATION" => Level.Info,
            "WARNING" => Level.Warn,
            "ERROR" => Level.Error,
            "CRITICAL" => Level.Fatal,
            "NONE" => Level.Off,
            _ => Level.Info
        };
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
    /// Creates a target-specific logger configuration for proper message routing.
    /// This enables the targeting system where "Console" routes to console appender, etc.
    /// </summary>
    /// <param name="repository">The Log4Net repository to configure.</param>
    /// <param name="appender">The configured appender.</param>
    /// <param name="target">The FlexKit target configuration.</param>
    private static void CreateTargetSpecificLogger(
        ILoggerRepository repository,
        IAppender appender,
        LoggingTarget target)
    {
        var hierarchy = (Hierarchy)repository;

        // Create a logger specifically for this target type using the concrete Logger class
        // This allows routing: GetLogger("Console") -> Console appender, GetLogger("File") -> File appender

        if (hierarchy.GetLogger(target.Type) is Logger targetLogger)
        {
            // Set the level threshold from target properties
            targetLogger.Level = GetLogLevelFromTarget(target);

            // Add the appender
            targetLogger.AddAppender(appender);

            // Disable additivity to prevent messages from going to root logger's appenders
            targetLogger.Additivity = false;
        }

        // Also configure the root logger to use this appender for general logging
        hierarchy.Root.AddAppender(appender);
    }

    /// <summary>
    /// Configures a fallback console appender when no targets are configured.
    /// </summary>
    /// <param name="repository">The Log4Net repository to configure.</param>
    private void ConfigureFallbackConsoleAppender(ILoggerRepository repository)
    {
        // Try to find console appender from detected appenders
        if (!_availableAppenders.TryGetValue("Console", out var consoleAppenderInfo))
        {
            return; // No console appender available
        }

        try
        {
            CreateConsoleAppender(repository, consoleAppenderInfo);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Warning: Failed to configure fallback console appender: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates and configures a console appender dynamically for logging purposes.
    /// </summary>
    /// <param name="repository">The Log4Net repository where the appender will be added.</param>
    /// <param name="consoleAppenderInfo">
    /// Information about the console appender, including its type and configuration details.
    /// </param>
    private static void CreateConsoleAppender(ILoggerRepository repository,
        Log4NetAppenderDetector.AppenderInfo consoleAppenderInfo)
    {
        var hierarchy = (Hierarchy)repository;

        // Create console appender dynamically
        var consoleAppender = (IAppender?)Activator.CreateInstance(consoleAppenderInfo.AppenderType);
        if (consoleAppender == null)
        {
            return;
        }

        consoleAppender.Name = "Console";

        // Set layout if supported
        if (consoleAppender is AppenderSkeleton skeletonAppender)
        {
            skeletonAppender.Layout = new PatternLayout("%date [%thread] %-5level %logger - %message%newline");
        }

        // Activate the appender if required
        if (consoleAppenderInfo.RequiresActivation && consoleAppender is IOptionHandler optionHandler)
        {
            optionHandler.ActivateOptions();
        }

        // Configure root logger
        hierarchy.Root.Level = Level.Debug;
        hierarchy.Root.AddAppender(consoleAppender);
    }
}
