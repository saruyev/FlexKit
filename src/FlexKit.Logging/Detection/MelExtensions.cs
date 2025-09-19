using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using FlexKit.Logging.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FlexKit.Logging.Detection;

/// <summary>
/// Provides extension methods for working with logging targets and configuring
/// Microsoft.Extensions.Logging components.
/// </summary>
internal static class MelExtensions
{
    /// <summary>
    /// Retrieves the type and method name of the formatter options configuration for the specified logging target.
    /// </summary>
    /// <param name="target">The logging target containing the formatter type information.</param>
    /// <returns>
    /// A tuple where the first item is the formatter options type and the second item is the method name
    /// for configuring the formatter. If the formatter type is unrecognized, the first item of the tuple
    /// will be null and the method name will default to "AddSimpleConsole".
    /// </returns>
    internal static (Type?, string) GetFormatterOptionsType(this LoggingTarget target)
    {
        target.Properties.TryGetValue("FormatterType", out var formatter);

        var methodName = formatter?.Value switch
        {
            "Simple" => "AddSimpleConsole",
            "Systemd" => "AddSystemdConsole",
            "Json" => "AddJsonConsole",
            _ => "AddSimpleConsole",
        };

        return (methodName switch
        {
            "AddSystemdConsole" => Type.GetType(MelNames.ConsoleOptionsType),
            "AddJsonConsole" => Type.GetType(MelNames.JsonConsoleOptionsType),
            _ => Type.GetType(MelNames.SimpleConsoleOptionsType),
        }, methodName);
    }

    /// <summary>
    /// Determines whether the specified logging target has configuration values
    /// specific to the Event Log provider, such as "LogName", "SourceName", or "MachineName".
    /// </summary>
    /// <param name="target">The logging target to evaluate for Event Log-specific configuration properties.</param>
    /// <returns>
    /// Returns true if the logging target contains one or more Event Log-specific configuration properties.
    /// Otherwise, returns false.
    /// </returns>
    internal static bool HasEventLogConfiguration(this LoggingTarget target) =>
        target.Properties.ContainsKey("LogName") ||
        target.Properties.ContainsKey("SourceName") ||
        target.Properties.ContainsKey("MachineName");

    /// <summary>
    /// Determines whether the specified method is a generic method definition for
    /// configuring services with the pattern `IServiceCollection.Configure&lt;T&gt;(Action&lt;T&gt;)`.
    /// </summary>
    /// <param name="m">The method information to evaluate.</param>
    /// <returns>
    /// True if the method matches the expected pattern for Configure&lt;T&gt;(Action&lt;T&gt;); otherwise, false.
    /// </returns>
    internal static bool IsConfigure(MethodInfo m) =>
        m is { Name: "Configure", IsGenericMethodDefinition: true } &&
        m.GetParameters().Length == 2 &&
        m.GetParameters()[0].ParameterType == typeof(IServiceCollection) &&
        m.GetParameters()[1].ParameterType.IsGenericType &&
        m.GetParameters()[1].ParameterType.GetGenericTypeDefinition() == typeof(Action<>);

    /// <summary>
    /// Creates a telemetry configuration delegate for a specified telemetry configuration type.
    /// The delegate sets properties on the telemetry configuration object based on the
    /// provided logging target's properties.
    /// </summary>
    /// <param name="target">The logging target containing the configuration properties.</param>
    /// <param name="telemetryConfigType">The type of the telemetry configuration object.</param>
    /// <returns>A compiled delegate of type Action&lt;T&gt; where T is the telemetry configuration type.</returns>
    internal static Delegate CreateTelemetryConfigurationAction(
        this LoggingTarget target,
        Type telemetryConfigType)
    {
        var actionType = typeof(Action<>).MakeGenericType(telemetryConfigType);
        var parameter = Expression.Parameter(telemetryConfigType, "config");
        var body = Expression.Block();

        // Check if target has ConnectionString property
        if (target.Properties.TryGetValue("ConnectionString", out var connectionString) &&
            !string.IsNullOrEmpty(connectionString?.Value))
        {
            var connectionStringProperty = telemetryConfigType.GetProperty("ConnectionString");
            if (connectionStringProperty != null)
            {
                var assignment = Expression.Assign(
                    Expression.Property(parameter, connectionStringProperty),
                    Expression.Constant(connectionString.Value));
                body = Expression.Block(assignment);
            }
        }

        var lambda = Expression.Lambda(actionType, body, parameter);
        return lambda.Compile();
    }

    /// <summary>
    /// Creates a configuration action delegate for a specific logging target and options' type.
    /// This delegate is used to configure logging options dynamically based on the provided
    /// target and options type.
    /// </summary>
    /// <param name="target">The logging target that represents the configuration or environment context.</param>
    /// <param name="optionsType">The type of the logging options to configure.</param>
    /// <returns>A delegate representing the configuration action for the specified options type.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the <c>ConfigureOptions</c> method is not found or cannot be invoked
    /// to create the configuration action.
    /// </exception>
    [SuppressMessage(
        "Major Code Smell",
        "S3011:Reflection should not be used to increase accessibility of classes, methods, or fields")]
    internal static Delegate CreateConfigurationAction(
        this LoggingTarget target,
        Type optionsType)
    {
        var actionType = typeof(Action<>).MakeGenericType(optionsType);

        // Create a closure that captures the target
        var configureMethod = typeof(MelExtensions)
            .GetMethod(nameof(ConfigureOptions), BindingFlags.NonPublic | BindingFlags.Static)
            ?.MakeGenericMethod(optionsType) ??
                              throw new InvalidOperationException("ConfigureOptions method not found");

        // Create the action using a lambda that captures the target
        var parameter = Expression.Parameter(optionsType, "options");
        var targetConstant = Expression.Constant(target);
        var methodCall = Expression.Call(configureMethod, targetConstant, parameter);
        var lambda = Expression.Lambda(actionType, methodCall, parameter);

        return lambda.Compile();
    }

    /// <summary>
    /// Configures the options of a specific type for a logging target by mapping
    /// the target's properties to the corresponding writable properties of the options' object.
    /// </summary>
    /// <param name="target">The logging target containing configuration properties.</param>
    /// <param name="options">The options object to be configured.</param>
    /// <typeparam name="T">The type of the options' object.</typeparam>
    private static void ConfigureOptions<T>(
        LoggingTarget target,
        T options) where T : class
    {
        var properties = typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite);

        foreach (var prop in properties)
        {
            if (!target.Properties.TryGetValue(prop.Name, out var value))
            {
                continue;
            }

            try
            {
                if (value == null)
                {
                    return;
                }

                TryToSetValue(options, value, prop);
            }
            catch
            {
                // Skip invalid conversions
            }
        }
    }

    /// <summary>
    /// Attempts to set the value of a property on a specified options object using
    /// the specified configuration section and property information. Handles both
    /// standard type conversion and enum parsing.
    /// </summary>
    /// <param name="options">The target options object whose property needs to be set.</param>
    /// <param name="value">The configuration section containing the value to be assigned.</param>
    /// <param name="prop">The property information representing the property to be set.</param>
    /// <typeparam name="T">The type of the options' object.</typeparam>
    private static void TryToSetValue<T>(
        T options,
        IConfigurationSection value,
        PropertyInfo prop) where T : class
    {
        object? convertedValue;

        if (value.Value == null)
        {
            convertedValue = Convert.ChangeType(
                value.Get(prop.PropertyType),
                prop.PropertyType,
                CultureInfo.InvariantCulture);
        }
        else if (prop.PropertyType.IsEnum)
        {
            // Handle enum conversion from string
            convertedValue = Enum.Parse(
                prop.PropertyType,
                value.Value,
                true);
        }
        else
        {
            convertedValue = Convert.ChangeType(
                value.Value,
                prop.PropertyType,
                CultureInfo.InvariantCulture);
        }

        prop.SetValue(options, convertedValue);
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
    internal static LogLevel GetLogLevel(this LoggingTarget target)
    {
        if (!target.Properties.TryGetValue("LogLevel", out var level))
        {
            return LogLevel.Information;
        }

        return level?.Value is { } levelString && Enum.TryParse<LogLevel>(
            levelString.ToLower(CultureInfo.InvariantCulture).Capitalize(),
            out var parsedLevel)
            ? parsedLevel
            : LogLevel.Information;
    }

    /// <summary>
    /// Capitalizes the first letter of the specified string and converts the rest of the characters to lowercase.
    /// </summary>
    /// <param name="str">The string to be capitalized.</param>
    /// <returns>
    /// A new string with the first letter capitalized and the remaining characters converted to lowercase.
    /// If the input string is null or empty, the method returns the original string.
    /// </returns>
    private static string Capitalize(this string str)
    {
        if (string.IsNullOrEmpty(str))
        {
            return string.Empty;
        }

        var arr = str.ToLowerInvariant().ToCharArray();
        arr[0] = char.ToUpper(arr[0], CultureInfo.InvariantCulture);
        return new string(arr).Trim();
    }
}
