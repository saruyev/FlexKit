using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace FlexKit.Logging.Interception.Attributes;

/// <summary>
/// Attribute to enable logging of both input parameters and output values for method calls.
/// When applied to a method or class, both input parameters and return values will be
/// captured and logged during method execution.
/// </summary>
/// <remarks>
/// <para>
/// This attribute is equivalent to applying both <see cref="LogInputAttribute"/> and
/// <see cref="LogOutputAttribute"/> to the same target. It provides a convenient way
/// to enable comprehensive logging for methods that require full visibility.
/// </para>
/// <para>
/// This attribute can be applied at both method and class levels:
/// <list type="bullet">
/// <item><strong>Method level:</strong> Affects only the specific method</item>
/// <item><strong>Class level:</strong> Affects all public methods in the class</item>
/// </list>
/// </para>
/// <para>
/// Method-level attributes take precedence over class-level attributes and configuration settings.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [LogBoth]
/// public class AuditService
/// {
///     // All methods will log both input and output at Information level
///     public AuditResult LogUserAction(UserActionRequest request) { ... }
///
///     [LogBoth(LogLevel.Debug)] // Override with Debug level for detailed auditing
///     public AuditResult LogSecurityEvent(SecurityEvent securityEvent) { ... }
///
///     [NoLog] // Override class-level attribute for sensitive operations
///     public void ProcessSensitiveData(SensitiveData data) { ... }
/// }
///
/// public class PaymentService
/// {
///     [LogBoth(LogLevel.Warning)] // Critical method needs full logging at Warning level
///     public PaymentResult ProcessPayment(PaymentRequest request) { ... }
///
///     [LogInput] // Less critical method only needs input logging
///     public ValidationResult ValidatePayment(PaymentRequest request) { ... }
///
///     public void SendNotification(string message) { ... }
/// }
/// </code>
/// </example>
/// <remarks>
/// Initializes a new instance of the LogBothAttribute with the specified log level.
/// </remarks>
/// <param name="level">The log level to use when logging both input and output.</param>
/// <param name="exceptionLevel">The log level to use when an exception is thrown.</param>
/// <param name="target">The target name to route logs to.</param>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
[UsedImplicitly]
public sealed class LogBothAttribute(
    LogLevel level = LogLevel.Information,
    LogLevel exceptionLevel = LogLevel.Error,
    string? target = null) : Attribute
{
    /// <summary>
    /// Gets the log level to use when logging both input parameters and output values.
    /// Defaults to Information if not specified.
    /// </summary>
    public LogLevel Level { get; } = level;

    /// <summary>
    /// Gets the log level to use when an exception is thrown.
    /// </summary>
    public LogLevel? ExceptionLevel { get; } = exceptionLevel;

    /// <summary>
    /// Gets the target name to route logs to.
    /// If null, uses the default target.
    /// </summary>
    public string? Target { get; } = target;
}
