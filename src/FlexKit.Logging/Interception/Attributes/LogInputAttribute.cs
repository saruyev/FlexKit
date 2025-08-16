using JetBrains.Annotations;

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
///     // All methods will log input parameters
///     public PaymentResult ProcessPayment(PaymentRequest request) { ... }
///
///     [NoLog] // Override class-level attribute
///     public void InternalMethod() { ... }
/// }
///
/// public class OrderService
/// {
///     [LogInput] // Only this method logs input
///     public OrderResult CreateOrder(OrderRequest request) { ... }
///
///     public void UpdateStatus(int orderId, OrderStatus status) { ... }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
[UsedImplicitly]
public sealed class LogInputAttribute : Attribute;
