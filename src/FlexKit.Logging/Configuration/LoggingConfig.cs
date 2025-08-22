using JetBrains.Annotations;

namespace FlexKit.Logging.Configuration;

/// <summary>
/// Configuration settings for FlexKit.Logging message formatting and output behavior.
/// Defines how log entries are formatted and which formatting mode to use.
/// </summary>
/// <remarks>
/// This configuration class is designed to be registered using FlexKit's RegisterConfig pattern:
/// <code>
/// containerBuilder.AddFlexConfig(config => config
///     .AddJsonFile("appsettings.json")
///     .AddEnvironmentVariables())
///     .RegisterConfig&lt;LoggingConfig&gt;("FlexKit:Logging");
///
/// // Then inject into services:
/// public class MyService(LoggingConfig loggingConfig)
/// {
///     // Use loggingConfig directly
/// }
/// </code>
///
/// <para>
/// <strong>Configuration Structure Example:</strong>
/// <code>
/// {
///   "FlexKit": {
///     "Logging": {
///       "MaxBatchSize": 1,
///       "BatchTimeout": "00:00:05",
///       "DefaultFormatter": "StandardStructured",
///       "EnableFallbackFormatting": true,
///       "FallbackTemplate": "Method {TypeName}.{MethodName} - Status: {Success}",
///       "AutoIntercept": true,
///       "Services": {
///         "MyApp.Services.*": { "LogInput": true },
///         "MyApp.Services.PaymentService": { "LogBoth": true },
///         "MyApp.Controllers.*": { "LogOutput": true }
///       },
///       "Templates": {
///         "MethodExecution": {
///           "SuccessTemplate": "Method {MethodName} completed in {Duration}ms",
///           "ErrorTemplate": "Method {MethodName} failed: {ExceptionMessage} after {Duration}ms"
///         }
///       },
///       "Formatters": {
///         "Json": {
///           "IncludeStackTrace": false,
///           "PrettyPrint": false
///         },
///         "Hybrid": {
///           "MessageTemplate": "Method {MethodName} completed",
///           "IncludeMetadata": true
///         }
///       }
///     }
///   }
/// }
/// </code>
/// </para>
/// </remarks>
public class LoggingConfig
{
    /// <summary>
    /// Gets or sets the maximum number of log entries to batch before processing.
    /// </summary>
    /// <value>The maximum number of log entries to keep in the batch before processing. Default is 1.</value>
    public int MaxBatchSize { get; [UsedImplicitly] set; } = 1;

    /// <summary>
    /// Gets or sets the maximum time to wait for a batch to fill before processing.
    /// </summary>
    /// <value>The maximum time to wait for a batch to fill before processing. Default is 1 second.</value>
    public TimeSpan BatchTimeout { get; [UsedImplicitly] set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets or sets the default message formatting mode to use when no specific formatter is configured.
    /// </summary>
    /// <value>The default formatter type. Default is <see cref="FormatterType.StandardStructured"/>.</value>
    public FormatterType DefaultFormatter { get; [UsedImplicitly] set; } = FormatterType.StandardStructured;

    /// <summary>
    /// Gets or sets custom template configurations for different formatters.
    /// </summary>
    /// <value>Dictionary mapping template names to template configurations.</value>
    public Dictionary<string, TemplateConfig> Templates { get; [UsedImplicitly] set; } = [];

    /// <summary>
    /// Gets or sets formatter-specific settings.
    /// </summary>
    /// <value>Configuration settings for individual formatters.</value>
    public FormatterSettings Formatters { get; [UsedImplicitly] set; } = new();

    /// <summary>
    /// Gets or sets whether to enable fallback formatting when the primary formatter fails.
    /// When enabled, failed formatting attempts fall back to a simple default format.
    /// </summary>
    /// <value>True to enable fallback formatting; false to log formatting errors without a fallback. Default is true.</value>
    public bool EnableFallbackFormatting { get; [UsedImplicitly] set; } = true;

    /// <summary>
    /// Gets or sets the fallback template to use when primary formatting fails.
    /// Only used when <see cref="EnableFallbackFormatting"/> is true.
    /// </summary>
    /// <value>The fallback template string. Default provides basic method execution information.</value>
    public string FallbackTemplate { get; [UsedImplicitly] set; } = "Method {TypeName}.{MethodName} - Status: {Success}";

    /// <summary>
    /// Gets or sets whether to enable automatic method interception for all registered services.
    /// When true, all public methods will be automatically intercepted for logging unless explicitly excluded.
    /// </summary>
    /// <value>True to enable auto-interception; false to require explicit decoration. Default is true.</value>
    /// <remarks>
    /// <para>
    /// Auto-interception follows this decision hierarchy:
    /// <list type="number">
    /// <item>Manual/Activity logging → Skip auto-intercept</item>
    /// <item>Attributes → Use attribute configuration</item>
    /// <item>Configuration patterns → Use config rules from <see cref="Services"/></item>
    /// <item>Default → Auto-intercept all public methods</item>
    /// </list>
    /// </para>
    /// <para>
    /// FlexKit internal services are automatically excluded from interception to prevent
    /// infinite recursion and maintain performance.
    /// </para>
    /// </remarks>
    public bool AutoIntercept { get; [UsedImplicitly] set; } = true;

    /// <summary>
    /// Gets or sets service-specific interception configuration patterns.
    /// Maps service name patterns to their logging behavior configuration.
    /// </summary>
    /// <value>Dictionary mapping service patterns to interception configurations.</value>
    /// <remarks>
    /// <para>
    /// <strong>Pattern Matching Rules:</strong>
    /// <list type="bullet">
    /// <item><strong>Exact match:</strong> "MyApp.Services.PaymentService" matches only that specific service</item>
    /// <item><strong>Wildcard match:</strong> "MyApp.Services.*" matches all services in the MyApp.Services namespace</item>
    /// <item><strong>Precedence:</strong> Exact matches override wildcard patterns</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Configuration Examples:</strong>
    /// <code>
    /// "Services": {
    ///   "MyApp.Services.*": { "LogInput": true },
    ///   "MyApp.Services.PaymentService": { "LogBoth": true },
    ///   "MyApp.Controllers.*": { "LogOutput": true }
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    [UsedImplicitly]
    public Dictionary<string, InterceptionConfig> Services { get; set; } = [];

    /// <summary>
    /// Gets or sets the logging targets configuration.
    /// Maps targets to their configuration. Auto-detected targets are merged with configured targets.
    /// </summary>
    /// <value>Dictionary mapping target names to their configuration. Default is empty.</value>
    [UsedImplicitly]
    public Dictionary<string, LoggingTarget> Targets { get; set; } = [];

    /// <summary>
    /// Gets or sets the default target name to use when no specific target is specified.
    /// If null, FlexKit will attempt to use the first available target or create a default console target.
    /// </summary>
    /// <value>The default target name. Default is null (auto-detect).</value>
    public string? DefaultTarget { get; [UsedImplicitly] set; }

    /// <summary>
    /// Gets or sets the name of the ActivitySource to use for manual logging.
    /// </summary>
    /// <value>The name of the ActivitySource to use for manual logging. The default is "FlexKit.Logging".</value>
    [UsedImplicitly]
    public string ActivitySourceName { get; set; } = "FlexKit.Logging";
}
