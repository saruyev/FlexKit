namespace FlexKit.Logging.Detection;

/// <summary>
/// Provides constant string values representing various logger and logger provider types within the
/// Microsoft.Extensions.Logging namespace. These values are used for detecting and handling specific
/// logger implementations and their related configuration types.
/// </summary>
public static class MelNames
{
    /// <summary>
    /// Defines the fully qualified type name and assembly for the
    /// console logger extension class. This constant is used to
    /// dynamically load the `ConsoleLoggerExtensions` type from the
    /// `Microsoft.Extensions.Logging.Console` library.
    /// </summary>
    internal const string ConsoleType =
        "Microsoft.Extensions.Logging.ConsoleLoggerExtensions, Microsoft.Extensions.Logging.Console";

    /// <summary>
    /// Represents the fully qualified type name and assembly for the debug logger extension class.
    /// This constant is used to dynamically load the `DebugLoggerFactoryExtensions` type from the
    /// `Microsoft.Extensions.Logging.Debug` library.
    /// </summary>
    internal const string DebugType =
        "Microsoft.Extensions.Logging.DebugLoggerFactoryExtensions, Microsoft.Extensions.Logging.Debug";

    /// <summary>
    /// Represents the fully qualified type name and assembly for the
    /// debug logger provider. This constant is used to dynamically load
    /// the `DebugLoggerProvider` type from the `Microsoft.Extensions.Logging.Debug`
    /// library, enabling debug-specific logging functionality.
    /// </summary>
    internal const string DebugProviderType =
        "Microsoft.Extensions.Logging.Debug.DebugLoggerProvider, Microsoft.Extensions.Logging.Debug";

    /// <summary>
    /// Represents the fully qualified type name and assembly for the console logger provider
    /// in the `Microsoft.Extensions.Logging.Console` library. This constant is used
    /// for dynamically resolving and using the `ConsoleLoggerProvider` type within
    /// logging configurations.
    /// </summary>
    internal const string ConsoleProviderType =
        "Microsoft.Extensions.Logging.Console.ConsoleLoggerProvider, Microsoft.Extensions.Logging.Console";

    /// <summary>
    /// Represents the fully qualified type name and assembly for the
    /// EventSource logger extension class. This constant is used to
    /// dynamically load the `EventSourceLoggerFactoryExtensions` type
    /// from the `Microsoft.Extensions.Logging.EventSource` library.
    /// </summary>
    internal const string EventSourceType =
        "Microsoft.Extensions.Logging.EventSourceLoggerFactoryExtensions, Microsoft.Extensions.Logging.EventSource";

    /// <summary>
    /// Represents the fully qualified type name and assembly of the EventSource logger provider class.
    /// This constant is used to dynamically load the `EventSourceLoggerProvider` type from the
    /// `Microsoft.Extensions.Logging.EventSource` library.
    /// </summary>
    internal const string EventSourceProviderType =
        "Microsoft.Extensions.Logging.EventSource.EventSourceLoggerProvider, Microsoft.Extensions.Logging.EventSource";

    /// <summary>
    /// Specifies the fully qualified type name and assembly for the event logger
    /// factory extension class. This constant facilitates the dynamic loading of
    /// the `EventLoggerFactoryExtensions` type from the `Microsoft.Extensions.Logging.EventLog`
    /// library.
    /// </summary>
    internal const string EventLogType =
        "Microsoft.Extensions.Logging.EventLoggerFactoryExtensions, Microsoft.Extensions.Logging.EventLog";

    /// <summary>
    /// Represents the fully qualified type name and assembly for the
    /// `EventLogSettings` class. This constant is used to dynamically retrieve
    /// and interact with the type from the `Microsoft.Extensions.Logging.EventLog`
    /// library, often in scenarios requiring configuration of event log settings.
    /// </summary>
    internal const string EventLogSettingsType =
        "Microsoft.Extensions.Logging.EventLog.EventLogSettings, Microsoft.Extensions.Logging.EventLog";

    /// <summary>
    /// Specifies the fully qualified type name and assembly for the Event Log logger provider class.
    /// This constant is used to dynamically locate and load the implementation of the Event Log logger
    /// from the `Microsoft.Extensions.Logging.EventLog` library.
    /// </summary>
    internal const string EventLogProviderType =
        "Microsoft.Extensions.Logging.EventLog.EventLogLoggerProvider, Microsoft.Extensions.Logging.EventLog";

    /// <summary>
    /// Represents the fully qualified type name and assembly for the
    /// TelemetryConfiguration class. This constant is primarily used
    /// to dynamically load and use the `TelemetryConfiguration`
    /// from the `Microsoft.ApplicationInsights` library for application
    /// insights telemetry configuration and management.
    /// </summary>
    internal const string TelemetryConfigurationType =
        "Microsoft.ApplicationInsights.Extensibility.TelemetryConfiguration, Microsoft.ApplicationInsights";

    /// <summary>
    /// Specifies the fully qualified type name and assembly for the
    /// Application Insights logging builder extensions. This constant
    /// is used to dynamically load the `ApplicationInsightsLoggingBuilderExtensions`
    /// type from the `Microsoft.Extensions.Logging.ApplicationInsights` library,
    /// enabling Application Insights logging integration.
    /// </summary>
    internal const string ApplicationInsightsType =
        "Microsoft.Extensions.Logging.ApplicationInsightsLoggingBuilderExtensions, Microsoft.Extensions.Logging.ApplicationInsights";

    /// <summary>
    /// Represents the fully qualified type name and assembly for the
    /// Application Insights logger options. This constant is used to dynamically
    /// load the `ApplicationInsightsLoggerOptions` type from the
    /// `Microsoft.Extensions.Logging.ApplicationInsights` library.
    /// </summary>
    internal const string ApplicationInsightsOptionsType =
        "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerOptions, Microsoft.Extensions.Logging.ApplicationInsights";

    /// <summary>
    /// Specifies the fully qualified type name and assembly for the
    /// Application Insights logger provider. This constant is used to
    /// dynamically load the `ApplicationInsightsLoggerProvider` type from the
    /// `Microsoft.Extensions.Logging.ApplicationInsights` library.
    /// </summary>
    internal const string ApplicationInsightsProviderType =
        "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider, Microsoft.Extensions.Logging.ApplicationInsights";

    /// <summary>
    /// Represents the fully qualified type name and assembly for the
    /// Azure App Services logger factory extensions. This constant is
    /// used to dynamically load the `AzureAppServicesLoggerFactoryExtensions` type
    /// from the `Microsoft.Extensions.Logging.AzureAppServices` library.
    /// </summary>
    internal const string AzureAppServicesType =
        "Microsoft.Extensions.Logging.AzureAppServicesLoggerFactoryExtensions, Microsoft.Extensions.Logging.AzureAppServices";

    /// <summary>
    /// Represents the fully qualified type name and assembly for the Azure App Services Logger Provider.
    /// This constant is used to dynamically load the `AzureAppServicesLoggerProvider` type
    /// from the `Microsoft.Extensions.Logging.AzureAppServices` library.
    /// </summary>
    internal const string AzureAppServicesProviderType =
        "Microsoft.Extensions.Logging.AzureAppServices.Internal.AzureAppServicesLoggerProvider, Microsoft.Extensions.Logging.AzureAppServices";

    /// <summary>
    /// Represents the fully qualified type name and assembly for the
    /// Azure Blob Logger options class. This constant is leveraged to
    /// dynamically load the `AzureBlobLoggerOptions` type from the
    /// `Microsoft.Extensions.Logging.AzureAppServices` library, which allows
    /// configuration of logging behavior for Azure Blob storage logging.
    /// </summary>
    internal const string AzureBlobOptionsType =
        "Microsoft.Extensions.Logging.AzureAppServices.AzureBlobLoggerOptions, Microsoft.Extensions.Logging.AzureAppServices";

    /// <summary>
    /// Represents the fully qualified type name and assembly for the
    /// Azure File Logger options class. This constant is employed to
    /// dynamically load the `AzureFileLoggerOptions` type from the
    /// `Microsoft.Extensions.Logging.AzureAppServices` library.
    /// </summary>
    internal const string AzureFileOptionsType =
        "Microsoft.Extensions.Logging.AzureAppServices.AzureFileLoggerOptions, Microsoft.Extensions.Logging.AzureAppServices";

    /// <summary>
    /// Specifies the fully qualified type name and assembly for the
    /// Simple Console Formatter options class. This constant is used to
    /// dynamically reference the `SimpleConsoleFormatterOptions` from the
    /// `Microsoft.Extensions.Logging.Console` assembly.
    /// </summary>
    internal const string SimpleConsoleOptionsType =
        "Microsoft.Extensions.Logging.Console.SimpleConsoleFormatterOptions, Microsoft.Extensions.Logging.Console";

    /// <summary>
    /// Represents the fully qualified type name and assembly for the
    /// console formatter options class in the `Microsoft.Extensions.Logging.Console`
    /// library. This constant is used to dynamically load the
    /// `ConsoleFormatterOptions` type in scenarios where specific console
    /// formatter configurations need to be applied.
    /// </summary>
    internal const string ConsoleOptionsType =
        "Microsoft.Extensions.Logging.Console.ConsoleFormatterOptions, Microsoft.Extensions.Logging.Console";

    /// <summary>
    /// Represents the fully qualified type name and assembly for
    /// the JSON console formatter options used in the
    /// `Microsoft.Extensions.Logging.Console` library. This constant
    /// facilitates retrieving and configuring settings for the
    /// JSON-based console logger format dynamically.
    /// </summary>
    internal const string JsonConsoleOptionsType =
        "Microsoft.Extensions.Logging.Console.JsonConsoleFormatterOptions, Microsoft.Extensions.Logging.Console";

    /// <summary>
    /// Specifies the fully qualified type name and assembly for the
    /// filter logging builder extension class. This constant is used
    /// to dynamically load the `FilterLoggingBuilderExtensions`
    /// type from the `Microsoft.Extensions.Logging` library.
    /// </summary>
    public const string FilterType =
        "Microsoft.Extensions.Logging.FilterLoggingBuilderExtensions, Microsoft.Extensions.Logging";
}
