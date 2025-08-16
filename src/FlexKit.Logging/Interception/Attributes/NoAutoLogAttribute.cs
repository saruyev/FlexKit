using JetBrains.Annotations;

namespace FlexKit.Logging.Interception.Attributes;

/// <summary>
/// Attribute to disable automatic interception for specific methods or classes.
/// Similar to <see cref="NoLogAttribute"/> but more explicitly indicates that
/// automatic logging should be bypassed while still allowing manual logging.
/// </summary>
/// <remarks>
/// <para>
/// This attribute serves the same purpose as <see cref="NoLogAttribute"/> but provides
/// clearer semantic intent when the goal is specifically to disable auto-interception
/// rather than all logging. Manual logging calls within the method will still work.
/// </para>
/// <para>
/// Use this attribute when:
/// <list type="bullet">
/// <item>You want to implement custom logging logic within the method</item>
/// <item>The method already has activity-based logging that would conflict</item>
/// <item>Auto-interception would interfere with method behavior</item>
/// <item>You need fine-grained control over what gets logged</item>
/// </list>
/// </para>
/// <para>
/// This attribute can be applied at both method and class levels:
/// <list type="bullet">
/// <item><strong>Method level:</strong> Disables auto-interception for the specific method</item>
/// <item><strong>Class level:</strong> Disables auto-interception for all methods in the class</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [LogBoth]
/// public class OrderService
/// {
///     // This method will be auto-intercepted
///     public OrderResult CreateOrder(OrderRequest request) { ... }
///
///     [NoAutoLog] // Custom logging logic inside
///     public void ProcessComplexOrder(ComplexOrderRequest request)
///     {
///         _logger.LogInformation("Starting complex order processing for {OrderId}", request.Id);
///
///         // Custom business logic with selective logging
///         foreach (var item in request.Items)
///         {
///             _logger.LogDebug("Processing item {ItemId}", item.Id);
///             // Process item...
///         }
///
///         _logger.LogInformation("Completed complex order processing");
///     }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
[UsedImplicitly]
public sealed class NoAutoLogAttribute : Attribute;
