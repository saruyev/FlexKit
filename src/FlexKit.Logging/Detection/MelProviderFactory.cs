using System.Reflection;
using FlexKit.Logging.Configuration;
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
        var type = Type.GetType(MelExtensions.DebugType);
        var method = type?.GetMethod("AddDebug", [typeof(ILoggingBuilder)]);
        method?.Invoke(null, [_builder]);

        // Add filters for this provider
        AddFiltersForProvider(Type.GetType(MelExtensions.DebugProviderType), target);
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
        var type = Type.GetType(MelExtensions.ConsoleType);
        var (configMethodType, methodName) = target.GetFormatterOptionsType();

        if (configMethodType != null)
        {
            InvokeLoggingMethod(new(target, configMethodType, type, methodName));
        }

        AddFiltersForProvider(Type.GetType(MelExtensions.ConsoleProviderType), target);
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
        var type = Type.GetType(MelExtensions.EventSourceType);
        var method = type?.GetMethod("AddEventSourceLogger", [typeof(ILoggingBuilder)]);
        method?.Invoke(null, [_builder]);

        AddFiltersForProvider(Type.GetType(MelExtensions.EventSourceProviderType), target);
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
        var type = Type.GetType(MelExtensions.EventLogType);
        var settingsType = Type.GetType(MelExtensions.EventLogSettingsType);

        if (settingsType != null && target.HasEventLogConfiguration())
        {
            InvokeLoggingMethod(new(target, settingsType, type, "AddEventLog"));
        }
        else
        {
            var method = type?.GetMethod("AddEventLog", [typeof(ILoggingBuilder)]);
            method?.Invoke(null, [_builder]);
        }

        AddFiltersForProvider(Type.GetType(MelExtensions.EventLogProviderType), target);
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
        var type = Type.GetType(MelExtensions.ApplicationInsightsType);

        // Check for the two-parameter configuration method
        var telemetryConfigType = Type.GetType(MelExtensions.TelemetryConfigurationType);
        var optionsType = Type.GetType(MelExtensions.ApplicationInsightsOptionsType);

        if (telemetryConfigType != null && optionsType != null)
        {
            InvokeApplicationInsightsMethod(new(target, optionsType, type, "AddApplicationInsights"), telemetryConfigType);
        }

        AddFiltersForProvider(Type.GetType(MelExtensions.ApplicationInsightsProviderType), target);
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
        var type = Type.GetType(MelExtensions.AzureAppServicesType);
        var method = type?.GetMethod("AddAzureWebAppDiagnostics", [typeof(ILoggingBuilder)]);

        method?.Invoke(null, [_builder]);
        ConfigureAzureFileLoggerOptions(target);
        ConfigureAzureBlobLoggerOptions(target);
        AddFiltersForProvider(Type.GetType(MelExtensions.AzureAppServicesProviderType), target);
    }

    /// <summary>
    /// Invokes a logging provider's configuration method using reflection,
    /// based on the provided invocation context containing the target logging configuration type,
    /// the related method name, and the associated logging target.
    /// </summary>
    /// <param name="context">
    /// The context specifying the details of the invocation, including the logging target,
    /// type of configuration method, provider type, and method name.
    /// </param>
    private void InvokeLoggingMethod(InvocationContext context)
    {
        var actionType = typeof(Action<>).MakeGenericType(context.ConfigMethodType);
        var method = context.Type?.GetMethod(context.MethodName, [typeof(ILoggingBuilder), actionType]);

        if (method != null)
        {
            var configAction = context.Target.CreateConfigurationAction(context.ConfigMethodType);
            method.Invoke(null, [_builder, configAction]);
        }
        else
        {
            var fallbackMethod = context.Type?.GetMethod(context.MethodName, [typeof(ILoggingBuilder)]);
            fallbackMethod?.Invoke(null, [_builder]);
        }
    }

    /// <summary>
    /// Invokes a method specific to Application Insights logging integration,
    /// verifying method existence and handling both primary and fallback configuration approaches.
    /// </summary>
    /// <param name="context">
    /// An object encapsulating invocation parameters including the logging target,
    /// configuration method type, target type, and method name.
    /// </param>
    /// <param name="telemetryConfigType">
    /// The type representing the telemetry configuration model required for Application Insights.
    /// </param>
    private void InvokeApplicationInsightsMethod(InvocationContext context, Type telemetryConfigType)
    {
        var telemetryActionType = typeof(Action<>).MakeGenericType(telemetryConfigType);
        var actionType = typeof(Action<>).MakeGenericType(context.ConfigMethodType);
        var method = context.Type?.GetMethod(
            context.MethodName,
            [typeof(ILoggingBuilder), telemetryActionType, actionType]);

        if (method != null)
        {
            var telemetryAction = context.Target.CreateTelemetryConfigurationAction(telemetryConfigType);
            var configAction = context.Target.CreateConfigurationAction(context.ConfigMethodType);
            method.Invoke(null, [_builder, telemetryAction, configAction]);
        }
        else
        {
            var fallbackMethod = context.Type?.GetMethod(context.MethodName, [typeof(ILoggingBuilder)]);
            fallbackMethod?.Invoke(null, [_builder]);
        }
    }

    /// <summary>
    /// Configures Azure File Logger options for the specified logging target.
    /// </summary>
    /// <param name="target">The logging target whose Azure File Logger options need to be configured.</param>
    private void ConfigureAzureFileLoggerOptions(LoggingTarget target)
    {
        var optionsType = Type.GetType(MelExtensions.AzureFileOptionsType);

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
            .FirstOrDefault(MelExtensions.IsConfigure);

        if (configureMethod == null)
        {
            return;
        }

        var specificMethod = configureMethod.MakeGenericMethod(optionsType);
        var configAction = target.CreateConfigurationAction(optionsType);
        specificMethod.Invoke(null, [services, configAction]);
    }

    /// <summary>
    /// Configures the Azure Blob logging options for the specified logging target.
    /// </summary>
    /// <param name="target">The logging target that contains settings for configuring Azure Blob logging.</param>
    private void ConfigureAzureBlobLoggerOptions(LoggingTarget target)
    {
        var optionsType = Type.GetType(MelExtensions.AzureBlobOptionsType);

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
            .FirstOrDefault(MelExtensions.IsConfigure);

        if (configureMethod == null)
        {
            return;
        }

        var specificMethod = configureMethod.MakeGenericMethod(optionsType);
        var configAction = target.CreateConfigurationAction(optionsType);
        specificMethod.Invoke(null, [services, configAction]);
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

        var filterType = Type.GetType(MelExtensions.FilterType);

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
        specificMethod.Invoke(null, [_builder, target.Type, target.GetLogLevel()]);

        static bool IsAddFilter(MethodInfo m) =>
            m is { Name: "AddFilter", IsGenericMethodDefinition: true } &&
            m.GetParameters().Length == 3 &&
            m.GetParameters()[1].ParameterType == typeof(string) &&
            m.GetParameters()[2].ParameterType == typeof(LogLevel);
    }

    private sealed record InvocationContext(
        LoggingTarget Target,
        Type ConfigMethodType,
        Type? Type,
        string MethodName);
}
