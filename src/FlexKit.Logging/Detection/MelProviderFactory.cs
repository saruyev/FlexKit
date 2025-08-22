using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using FlexKit.Logging.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FlexKit.Logging.Detection;

/// <summary>
/// A factory class responsible for managing and configuring logging providers
/// for the Microsoft.Extensions.Logging framework. Enables dynamic management
/// of log provider configurations based on a logging configuration model.
/// </summary>
public class MelProviderFactory
{
    private readonly Dictionary<string, Action<LoggingTarget>> _providerConfigurers = [];
    private readonly LoggingConfig _config;
    private readonly ILoggingBuilder _builder;
    private readonly string[] _types;

    /// <summary>
    /// Factory class responsible for creating and configuring Microsoft.Extensions.Logging (MEL) providers
    /// based on the provided logging configuration.
    /// </summary>
    /// <param name="builder">The configuration builder to extend.</param>
    /// <param name="config">FlexKit logging configuration settings.</param>
    public MelProviderFactory(
        ILoggingBuilder builder,
        LoggingConfig config)
    {
        _builder = builder;
        _config = config;
        _types = [.. config.Targets.Values.Select(t => t.Type).Distinct()];
        _providerConfigurers["Debug"] = TryAddDebug;
        _providerConfigurers["Console"] = TryAddConsole;
        _providerConfigurers["EventSource"] = TryAddEventSource;
        _providerConfigurers["EventLog"] = TryAddEventLog;
        _providerConfigurers["ApplicationInsights"] = TryAddApplicationInsights;
        _providerConfigurers["AzureWebAppDiagnostics"] = TryAddAzureWebAppDiagnostics;
    }

    /// <summary>
    /// Configures logging providers based on the targets defined in the logging configuration.
    /// This method clears any existing providers and dynamically adds providers for all enabled logging targets
    /// by using corresponding configuration handlers.
    /// </summary>
    public void ConfigureProviders()
    {
        _builder.ClearProviders();

        foreach (var target in _config.Targets.Values.Where(t => t.Enabled).ToArray())
        {
            if (_providerConfigurers.TryGetValue(target.Type, out var configurer))
            {
                configurer(target);
            }
        }
    }

    /// <summary>
    /// Attempts to add the Debug logging provider to the logging builder if it is available.
    /// Configures the Debug provider based on the specified logging target.
    /// </summary>
    /// <param name="target">The logging target configuration defining the Debug provider settings.</param>
    private void TryAddDebug(LoggingTarget target)
    {
        var type = Type.GetType(
            "Microsoft.Extensions.Logging.DebugLoggerFactoryExtensions, Microsoft.Extensions.Logging.Debug");
        var method = type?.GetMethod("AddDebug", [typeof(ILoggingBuilder)]);
        method?.Invoke(null, [_builder]);

        // Add filters for this provider
        AddFiltersForProvider(
            Type.GetType(
                "Microsoft.Extensions.Logging.Debug.DebugLoggerProvider, Microsoft.Extensions.Logging.Debug"),
            target);
    }

    /// <summary>
    /// Configures and adds a Console logging provider to the Microsoft.Extensions.Logging framework,
    /// using the specified logging target configuration. Dynamically applies formatter and filter
    /// settings based on the target's properties.
    /// </summary>
    /// <param name="target">
    /// The logging target configuration containing properties and settings for the Console provider.
    /// </param>
    private void TryAddConsole(LoggingTarget target)
    {
        var type = Type.GetType(
            "Microsoft.Extensions.Logging.ConsoleLoggerExtensions, Microsoft.Extensions.Logging.Console");
        target.Properties.TryGetValue("FormatterType", out var formatter);

        var methodName = formatter?.Value switch
        {
            "Simple" => "AddSimpleConsole",
            "Systemd" => "AddSystemdConsole",
            "Json" => "AddJsonConsole",
            _ => "AddSimpleConsole"
        };
        var configMethodType = GetFormatterOptionsType(methodName);

        if (configMethodType != null)
        {
            var actionType = typeof(Action<>).MakeGenericType(configMethodType);
            var method = type?.GetMethod(methodName, [typeof(ILoggingBuilder), actionType]);

            if (method != null)
            {
                var configAction = CreateConfigurationAction(target, configMethodType);
                method.Invoke(null, [_builder, configAction]);
            }
            else
            {
                var fallbackMethod = type?.GetMethod(methodName, [typeof(ILoggingBuilder)]);
                fallbackMethod?.Invoke(null, [_builder]);
            }
        }

        AddFiltersForProvider(
            Type.GetType(
                "Microsoft.Extensions.Logging.Console.ConsoleLoggerProvider, Microsoft.Extensions.Logging.Console"),
            target);
    }

    /// <summary>
    /// Attempts to add an EventSource logger to the logging builder, enabling
    /// logging to an EventSource-based provider if available and configured.
    /// </summary>
    /// <param name="target">
    /// The logging target configuration that specifies the type and settings for the EventSource logger.
    /// </param>
    private void TryAddEventSource(LoggingTarget target)
    {
        var type = Type.GetType(
            "Microsoft.Extensions.Logging.EventSourceLoggerFactoryExtensions, Microsoft.Extensions.Logging.EventSource");
        var method = type?.GetMethod("AddEventSourceLogger", [typeof(ILoggingBuilder)]);
        method?.Invoke(null, [_builder]);

        AddFiltersForProvider(
            Type.GetType(
                "Microsoft.Extensions.Logging.EventSource.EventSourceLoggerProvider, Microsoft.Extensions.Logging.EventSource"),
            target);
    }

    /// <summary>
    /// Attempts to add the Event Log provider to the logging builder, with optional configuration
    /// based on the provided logging target.
    /// </summary>
    /// <param name="target">
    /// The logging target containing configuration details for the Event Log provider.
    /// </param>
    private void TryAddEventLog(LoggingTarget target)
    {
        var type = Type.GetType(
            "Microsoft.Extensions.Logging.EventLoggerFactoryExtensions, Microsoft.Extensions.Logging.EventLog");
        var settingsType = Type.GetType(
            "Microsoft.Extensions.Logging.EventLog.EventLogSettings, Microsoft.Extensions.Logging.EventLog");

        if (settingsType != null && HasEventLogConfiguration(target))
        {
            var actionType = typeof(Action<>).MakeGenericType(settingsType);
            var method = type?.GetMethod("AddEventLog", [typeof(ILoggingBuilder), actionType]);

            if (method != null)
            {
                var configAction = CreateConfigurationAction(target, settingsType);
                method.Invoke(null, [_builder, configAction]);
            }
            else
            {
                var fallbackMethod = type?.GetMethod("AddEventLog", [typeof(ILoggingBuilder)]);
                fallbackMethod?.Invoke(null, [_builder]);
            }
        }
        else
        {
            var method = type?.GetMethod("AddEventLog", [typeof(ILoggingBuilder)]);
            method?.Invoke(null, [_builder]);
        }

        AddFiltersForProvider(
            Type.GetType(
                "Microsoft.Extensions.Logging.EventLog.EventLogLoggerProvider, Microsoft.Extensions.Logging.EventLog"),
            target);
    }

    /// <summary>
    /// Attempts to add an Application Insights logging provider to the logging builder configuration
    /// if the necessary Application Insights dependencies and configuration methods are available.
    /// </summary>
    /// <param name="target">
    /// The logging target configuration that specifies the provider's settings and properties.
    /// </param>
    private void TryAddApplicationInsights(LoggingTarget target)
    {
        var type = Type.GetType(
            "Microsoft.Extensions.Logging.ApplicationInsightsLoggingBuilderExtensions, Microsoft.Extensions.Logging.ApplicationInsights");

        // Check for the two-parameter configuration method
        var telemetryConfigType = Type.GetType(
            "Microsoft.ApplicationInsights.Extensibility.TelemetryConfiguration, Microsoft.ApplicationInsights");
        var optionsType = Type.GetType(
            "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerOptions, Microsoft.Extensions.Logging.ApplicationInsights");

        if (telemetryConfigType != null && optionsType != null)
        {
            var telemetryActionType = typeof(Action<>).MakeGenericType(telemetryConfigType);
            var optionsActionType = typeof(Action<>).MakeGenericType(optionsType);
            var method = type?.GetMethod(
                "AddApplicationInsights",
                [typeof(ILoggingBuilder), telemetryActionType, optionsActionType]);

            if (method != null)
            {
                var telemetryAction = CreateTelemetryConfigurationAction(target, telemetryConfigType);
                var optionsAction = CreateConfigurationAction(target, optionsType);
                method.Invoke(null, [_builder, telemetryAction, optionsAction]);
            }
            else
            {
                var fallbackMethod = type?.GetMethod("AddApplicationInsights", [typeof(ILoggingBuilder)]);
                fallbackMethod?.Invoke(null, [_builder]);
            }
        }

        AddFiltersForProvider(
            Type.GetType(
                "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider, Microsoft.Extensions.Logging.ApplicationInsights"),
            target);
    }

    /// <summary>
    /// Configures and adds Azure Web App Diagnostics logging to the logging builder
    /// if the required extensions are available. Applies additional configuration
    /// for Azure file and blob logging options and registers any specified filters
    /// for logging providers.
    /// </summary>
    /// <param name="target">
    /// The logging target containing configuration for the Azure Web App Diagnostics provider.
    /// </param>
    private void TryAddAzureWebAppDiagnostics(LoggingTarget target)
    {
        var type = Type.GetType(
            "Microsoft.Extensions.Logging.AzureAppServicesLoggerFactoryExtensions, Microsoft.Extensions.Logging.AzureAppServices");
        var method = type?.GetMethod("AddAzureWebAppDiagnostics", [typeof(ILoggingBuilder)]);

        method?.Invoke(null, [_builder]);
        ConfigureAzureFileLoggerOptions(target);
        ConfigureAzureBlobLoggerOptions(target);
        AddFiltersForProvider(
            Type.GetType("Microsoft.Extensions.Logging.AzureAppServices.Internal.AzureAppServicesLoggerProvider, Microsoft.Extensions.Logging.AzureAppServices"),
            target);
    }

    /// <summary>
    /// Determines the type of formatter options for the specified logging method
    /// based on the method name.
    /// </summary>
    /// <param name="methodName">
    /// The name of the logging method for which the formatter options type is to be retrieved.
    /// </param>
    /// <returns>
    /// The type of the formatter options associated with the provided method name, or null if no matching type exists.
    /// </returns>
    private static Type? GetFormatterOptionsType(string methodName) =>
        methodName switch
        {
            "AddSimpleConsole" => Type.GetType(
                "Microsoft.Extensions.Logging.Console.SimpleConsoleFormatterOptions, Microsoft.Extensions.Logging.Console"),
            "AddSystemdConsole" => Type.GetType(
                "Microsoft.Extensions.Logging.Console.ConsoleFormatterOptions, Microsoft.Extensions.Logging.Console"),
            "AddJsonConsole" => Type.GetType(
                "Microsoft.Extensions.Logging.Console.JsonConsoleFormatterOptions, Microsoft.Extensions.Logging.Console"),
            _ => null
        };

    /// <summary>
    /// Determines whether the specified logging target has configuration values
    /// specific to the Event Log provider, such as "LogName", "SourceName", or "MachineName".
    /// </summary>
    /// <param name="target">The logging target to evaluate for Event Log-specific configuration properties.</param>
    /// <returns>
    /// Returns true if the logging target contains one or more Event Log-specific configuration properties.
    /// Otherwise, returns false.
    /// </returns>
    private static bool HasEventLogConfiguration(LoggingTarget target) =>
        target.Properties.ContainsKey("LogName") ||
        target.Properties.ContainsKey("SourceName") ||
        target.Properties.ContainsKey("MachineName");

    /// <summary>
    /// Configures Azure File Logger options for the specified logging target.
    /// </summary>
    /// <param name="target">The logging target whose Azure File Logger options need to be configured.</param>
    private void ConfigureAzureFileLoggerOptions(LoggingTarget target)
    {
        var optionsType = Type.GetType(
            "Microsoft.Extensions.Logging.AzureAppServices.AzureFileLoggerOptions, Microsoft.Extensions.Logging.AzureAppServices");

        if (optionsType == null)
        {
            return;
        }

        var servicesProperty = _builder.GetType().GetProperty("Services");
        var services = servicesProperty?.GetValue(_builder);

        if (services == null)
        {
            return;
        }

        // Find IServiceCollection.Configure<T>(Action<T>) method
        var servicesType = services.GetType();
        var configureMethod = servicesType.GetMethods()
            .FirstOrDefault(IsConfigure);

        if (configureMethod == null)
        {
            return;
        }

        var specificMethod = configureMethod.MakeGenericMethod(optionsType);
        var configAction = CreateConfigurationAction(target, optionsType);
        specificMethod.Invoke(null, [services, configAction]);
    }

    /// <summary>
    /// Configures the Azure Blob logging options for the specified logging target.
    /// </summary>
    /// <param name="target">The logging target that contains settings for configuring Azure Blob logging.</param>
    private void ConfigureAzureBlobLoggerOptions(LoggingTarget target)
    {
        var optionsType = Type.GetType(
            "Microsoft.Extensions.Logging.AzureAppServices.AzureBlobLoggerOptions, Microsoft.Extensions.Logging.AzureAppServices");

        if (optionsType == null)
        {
            return;
        }

        var servicesProperty = _builder.GetType().GetProperty("Services");
        var services = servicesProperty?.GetValue(_builder);

        if (services == null)
        {
            return;
        }

        // Find IServiceCollection.Configure<T>(Action<T>) method
        var servicesType = services.GetType();
        var configureMethod = servicesType.GetMethods()
            .FirstOrDefault(IsConfigure);

        if (configureMethod == null)
        {
            return;
        }

        var specificMethod = configureMethod.MakeGenericMethod(optionsType);
        var configAction = CreateConfigurationAction(target, optionsType);
        specificMethod.Invoke(null, [services, configAction]);
    }

    /// <summary>
    /// Determines whether the specified method is a generic method definition for
    /// configuring services with the pattern `IServiceCollection.Configure&lt;T&gt;(Action&lt;T&gt;)`.
    /// </summary>
    /// <param name="m">The method information to evaluate.</param>
    /// <returns>
    /// True if the method matches the expected pattern for Configure&lt;T&gt;(Action&lt;T&gt;); otherwise, false.
    /// </returns>
    private static bool IsConfigure(MethodInfo m) =>
        m is { Name: "Configure", IsGenericMethodDefinition: true } &&
        m.GetParameters().Length == 2 &&
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
    private static Delegate CreateTelemetryConfigurationAction(
        LoggingTarget target,
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
    private static Delegate CreateConfigurationAction(
        LoggingTarget target,
        Type optionsType)
    {
        var actionType = typeof(Action<>).MakeGenericType(optionsType);

        // Create a closure that captures the target
        var configureMethod = typeof(MelProviderFactory)
            .GetMethod(nameof(ConfigureOptions), BindingFlags.NonPublic | BindingFlags.Static)
            ?.MakeGenericMethod(optionsType) ?? throw new InvalidOperationException("ConfigureOptions method not found");

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
    /// Adds logging filters specific to a provider based on the provided logging target configuration.
    /// </summary>
    /// <param name="providerType">The type of the logging provider for which filters will be added.</param>
    /// <param name="target">
    /// The logging target configuration that defines logging behavior for a specific category.
    /// </param>
    private void AddFiltersForProvider(
        Type? providerType,
        LoggingTarget target)
    {
        if (providerType == null)
        {
            return;
        }

        var filterType = Type.GetType(
            "Microsoft.Extensions.Logging.FilterLoggingBuilderExtensions, Microsoft.Extensions.Logging");

        // Find the generic AddFilter<T> method: AddFilter<T>(ILoggingBuilder, string?, LogLevel)
        var genericMethod = filterType?.GetMethods()
            .FirstOrDefault(IsAddFilter);

        if (genericMethod == null)
        {
            return;
        }

        // Make it specific to our provider type
        var specificMethod = genericMethod.MakeGenericMethod(providerType);

        // Block other categories from this provider
        foreach (var otherCategory in _types.Where(t => t != target.Type))
        {
            specificMethod.Invoke(null, [_builder, otherCategory, LogLevel.None]);
        }

        // Allow this target's category through
        specificMethod.Invoke(null, [_builder, target.Type, GetLogLevel(target)]);

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
