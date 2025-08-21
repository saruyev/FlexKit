using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace FlexKit.Logging.Interception.Attributes;

/// <summary>
/// Attribute to enable logging of input parameters for method calls.
/// When applied to a method or class, input parameters will be captured and logged
/// during method execution.
/// </summary>
/// <remarks>
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
/// [LogInput]
/// public class PaymentService
/// {
///     // All methods will log input parameters at Information level
///     public PaymentResult ProcessPayment(PaymentRequest request) { ... }
///
///     [LogInput(LogLevel.Debug)] // Override with Debug level
///     public ValidationResult ValidatePayment(PaymentRequest request) { ... }
///
///     [NoLog] // Override class-level attribute
///     public void InternalMethod() { ... }
/// }
///
/// public class OrderService
/// {
///     [LogInput(LogLevel.Warning)] // Only this method logs input at Warning level
///     public OrderResult CreateOrder(OrderRequest request) { ... }
///
///     public void UpdateStatus(int orderId, OrderStatus status) { ... }
/// }
/// </code>
/// </example>
/// <remarks>
/// Initializes a new instance of the LogInputAttribute with the specified log level.
/// </remarks>
/// <param name="level">The log level to use when logging input parameters.</param>
/// <param name="exceptionLevel">The log level to use when an exception is thrown.</param>
/// <param name="target">The target name to route logs to.</param>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
[UsedImplicitly]
public sealed class LogInputAttribute(
    LogLevel level = LogLevel.Information,
    LogLevel exceptionLevel = LogLevel.Error,
    string? target = null) : Attribute
{
    /// <summary>
    /// Gets the log level to use when logging input parameters.
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
