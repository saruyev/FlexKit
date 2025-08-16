using JetBrains.Annotations;

namespace FlexKit.Logging.Interception.Attributes;

/// <summary>
/// Attribute to enable logging of output values for method calls.
/// When applied to a method or class, return values will be captured and logged
/// when methods complete successfully.
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
/// Output logging only occurs for successful method completions - exceptions are handled
/// separately and don't trigger output logging.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [LogOutput]
/// public class CalculationService
/// {
///     // All methods will log return values
///     public decimal CalculateTotal(List&lt;OrderItem&gt; items) { ... }
///
///     [NoLog] // Override class-level attribute
///     public void InternalCleanup() { ... }
/// }
///
/// public class UserService
/// {
///     [LogOutput] // Only this method logs output
///     public User GetUserById(int userId) { ... }
///
///     public void UpdateLastLogin(int userId) { ... }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
[UsedImplicitly]
public sealed class LogOutputAttribute : Attribute;
