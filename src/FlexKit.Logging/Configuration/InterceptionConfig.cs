using FlexKit.Logging.Interception;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace FlexKit.Logging.Configuration;

/// <summary>
/// Configuration settings for method interception behavior on specific services or service patterns.
/// Defines what type of logging should occur when methods are intercepted and at what level.
/// </summary>
public class InterceptionConfig
{
    /// <summary>
    /// Gets or sets whether to log input parameters when methods are called.
    /// When true, method arguments will be captured and logged.
    /// </summary>
    /// <value>True to log input parameters; false to skip input logging. Default is false.</value>
    [UsedImplicitly]
    public bool LogInput { get; set; }

    /// <summary>
    /// Gets or sets whether to log output values when methods complete successfully.
    /// When true, method return values will be captured and logged.
    /// </summary>
    /// <value>True to log output values; false to skip output logging. Default is false.</value>
    [UsedImplicitly]
    public bool LogOutput { get; set; }

    /// <summary>
    /// Gets or sets the log level to use for logging.
    /// Defaults to Information if not specified.
    /// </summary>
    /// <value>The log level for logging. Default is Information.</value>
    [UsedImplicitly]
    public LogLevel Level { get; set; } = LogLevel.Information;

    /// <summary>
    /// Gets or sets the log level to use when an exception is thrown.
    /// </summary>
    /// <value>The log level for exception logging. Default is Error.</value>
    [UsedImplicitly]
    public LogLevel ExceptionLevel { get; set; } = LogLevel.Error;

    /// <summary>
    /// Gets or sets the target name to route logs to.
    /// If null, uses the default target. Allows configuration-based target routing.
    /// </summary>
    /// <value>The target name for routing logs. Default is null (use default target).</value>
    [UsedImplicitly]
    public string? Target { get; set; }

    /// <summary>
    /// Gets or sets the patterns of method names to exclude from interception and logging.
    /// Methods matching any of these patterns will be skipped during the interception process.
    /// </summary>
    /// <value>
    /// A list of string patterns representing method names to exclude from interception. Default is an empty list.
    /// </value>
    [UsedImplicitly]
    public List<string> ExcludeMethodPatterns { get; set; } = [];

    /// <summary>
    /// Gets or sets parameter name patterns that should be masked during logging.
    /// Supports wildcard patterns using asterisks (*).
    /// </summary>
    /// <value>
    /// A list of parameter name patterns to mask. Examples: ["*password*", "secret*", "*key"]
    /// Default is an empty list.
    /// </value>
    /// <remarks>
    /// <para>
    /// <strong>Pattern Matching Rules:</strong>
    /// <list type="bullet">
    /// <item><strong>Exact match:</strong> "password" matches a parameter named exactly "password"</item>
    /// <item><strong>Starts with:</strong> "secret*" matches any parameter starting with "secret"</item>
    /// <item><strong>Ends with:</strong> "*key" matches any parameter ending with "key"</item>
    /// <item><strong>Contains:</strong> "*password*" matches any parameter containing "password"</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Configuration Example:</strong>
    /// <code>
    /// "MaskParameterPatterns": ["*password*", "*secret*", "*key", "apiKey", "connectionString"]
    /// </code>
    /// </para>
    /// </remarks>
    [UsedImplicitly]
    public List<string> MaskParameterPatterns { get; set; } = [];

    /// <summary>
    /// Gets or sets property name patterns that should be masked in complex objects during logging.
    /// Supports wildcard patterns using asterisks (*).
    /// </summary>
    /// <value>
    /// A list of property name patterns to mask in logged objects. Examples: ["Password", "*Token*", "ApiKey"]
    /// Default is an empty list.
    /// </value>
    /// <remarks>
    /// <para>
    /// This setting applies to properties within complex objects that are logged as input parameters
    /// or return values. When a complex object is logged, properties matching these patterns will
    /// be replaced with the mask replacement text.
    /// </para>
    /// <para>
    /// <strong>Pattern Matching Rules:</strong>
    /// <list type="bullet">
    /// <item><strong>Exact match:</strong> "Password" matches property named exactly "Password"</item>
    /// <item><strong>Starts with:</strong> "Secret*" matches any property starting with "Secret"</item>
    /// <item><strong>Ends with:</strong> "*Token" matches any property ending with "Token"</item>
    /// <item><strong>Contains:</strong> "*Key*" matches any property containing "Key"</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Configuration Example:</strong>
    /// <code>
    /// "MaskPropertyPatterns": ["Password", "ApiKey", "*Token*", "*Secret*"]
    /// </code>
    /// </para>
    /// </remarks>
    [UsedImplicitly]
    public List<string> MaskPropertyPatterns { get; set; } = [];

    /// <summary>
    /// Gets or sets the replacement text to use when masking sensitive data.
    /// This text will replace the actual values of parameters and properties that match masking patterns.
    /// </summary>
    /// <value>
    /// The text to display instead of sensitive values. Default is "***MASKED***".
    /// </value>
    /// <remarks>
    /// <para>
    /// This setting provides a global default for masking replacement text within this service configuration.
    /// Individual [Mask] attributes can override this with their own replacement text.
    /// </para>
    /// <para>
    /// <strong>Usage Examples:</strong>
    /// <list type="bullet">
    /// <item><strong>Security-focused:</strong> "[REDACTED]" or "[CLASSIFIED]"</item>
    /// <item><strong>Development-friendly:</strong> "***HIDDEN***" or "&lt;masked&gt;"</item>
    /// <item><strong>Compliance-oriented:</strong> "[PII_REMOVED]" or "[SENSITIVE_DATA]"</item>
    /// </list>
    /// </para>
    /// </remarks>
    [UsedImplicitly]
    public string MaskReplacement { get; set; } = "***MASKED***";

    /// <summary>
    /// Gets or sets output value patterns that should be masked during logging.
    /// Supports wildcard patterns for matching return values that contain sensitive data.
    /// </summary>
    /// <value>
    /// A list of output patterns to mask. Examples: ["*connection*", "*password*"]
    /// Default is an empty list.
    /// </value>
    [UsedImplicitly]
    public List<string> MaskOutputPatterns { get; set; } = [];

    /// <summary>
    /// Gets the effective interception decision based on the configured flags and level.
    /// </summary>
    /// <returns>The appropriate InterceptionDecision with behavior and log level.</returns>
    public InterceptionDecision GetDecision() =>
        LogOutput switch
        {
            true when LogInput => new InterceptionDecision()
                .WithBehavior(InterceptionBehavior.LogBoth)
                .WithLevel(Level)
                .WithExceptionLevel(ExceptionLevel)
                .WithTarget(Target),
            true when !LogInput => new InterceptionDecision()
                .WithBehavior(InterceptionBehavior.LogOutput)
                .WithLevel(Level)
                .WithExceptionLevel(ExceptionLevel)
                .WithTarget(Target),
            _ => new InterceptionDecision()
                .WithBehavior(InterceptionBehavior.LogInput)
                .WithLevel(Level)
                .WithExceptionLevel(ExceptionLevel)
                .WithTarget(Target)
        };
}
