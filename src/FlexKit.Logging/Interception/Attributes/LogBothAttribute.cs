using JetBrains.Annotations;

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
///     // All methods will log both input and output
///     public AuditResult LogUserAction(UserActionRequest request) { ... }
///
///     [NoLog] // Override class-level attribute for sensitive operations
///     public void LogSecurityEvent(SecurityEvent securityEvent) { ... }
/// }
///
/// public class PaymentService
/// {
///     [LogBoth] // Critical method needs full logging
///     public PaymentResult ProcessPayment(PaymentRequest request) { ... }
///
///     [LogInput] // Less critical method only needs input logging
///     public ValidationResult ValidatePayment(PaymentRequest request) { ... }
///
///     public void SendNotification(string message) { ... }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
[UsedImplicitly]
public sealed class LogBothAttribute : Attribute;
