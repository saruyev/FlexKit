using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

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
///     // All methods will log return values at Information level
///     public decimal CalculateTotal(List&lt;OrderItem&gt; items) { ... }
///
///     [LogOutput(LogLevel.Debug)] // Override with Debug level for detailed calculations
///     public decimal CalculateInterestRate(decimal principal, int months) { ... }
///
///     [NoLog] // Override class-level attribute
///     public void InternalCleanup() { ... }
/// }
///
/// public class UserService
/// {
///     [LogOutput(LogLevel.Warning)] // Only this method logs output at Warning level
///     public User GetUserById(int userId) { ... }
///
///     public void UpdateLastLogin(int userId) { ... }
/// }
/// </code>
/// </example>
/// <remarks>
/// Initializes a new instance of the LogOutputAttribute with the specified log level.
/// </remarks>
/// <param name="level">The log level to use when logging output values.</param>
/// <param name="exceptionLevel">The log level to use when an exception is thrown.</param>
/// <param name="target">The target name to route logs to.</param>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
[UsedImplicitly]
public sealed class LogOutputAttribute(
    LogLevel level = LogLevel.Information,
    LogLevel exceptionLevel = LogLevel.Error,
    string? target = null) : Attribute
{
    /// <summary>
    /// Gets the log level to use when logging output values.
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
