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
///       "DefaultFormatter": "StandardStructured",
///       "EnableFallbackFormatting": true,
///       "FallbackTemplate": "Method {TypeName}.{MethodName} - Status: {Success}",
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
    /// Gets or sets the default message formatting mode to use when no specific formatter is configured.
    /// </summary>
    /// <value>The default formatter type. Default is <see cref="FormatterType.StandardStructured"/>.</value>
    public FormatterType DefaultFormatter { get; set; } = FormatterType.StandardStructured;

    /// <summary>
    /// Gets or sets custom template configurations for different formatters.
    /// </summary>
    /// <value>Dictionary mapping template names to template configurations.</value>
    public Dictionary<string, TemplateConfig> Templates { get; set; } = new();

    /// <summary>
    /// Gets or sets formatter-specific settings.
    /// </summary>
    /// <value>Configuration settings for individual formatters.</value>
    public FormatterSettings Formatters { get; set; } = new();

    /// <summary>
    /// Gets or sets whether to enable fallback formatting when the primary formatter fails.
    /// When enabled, failed formatting attempts fall back to a simple default format.
    /// </summary>
    /// <value>True to enable fallback formatting; false to log formatting errors without a fallback. Default is true.</value>
    public bool EnableFallbackFormatting { get; set; } = true;

    /// <summary>
    /// Gets or sets the fallback template to use when primary formatting fails.
    /// Only used when <see cref="EnableFallbackFormatting"/> is true.
    /// </summary>
    /// <value>The fallback template string. Default provides basic method execution information.</value>
    public string FallbackTemplate { get; set; } = "Method {TypeName}.{MethodName} - Status: {Success}";
}
