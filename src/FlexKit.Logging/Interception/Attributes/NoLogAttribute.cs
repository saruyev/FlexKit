using JetBrains.Annotations;

namespace FlexKit.Logging.Interception.Attributes;

/// <summary>
/// Attribute to completely disable logging for specific methods or classes.
/// When applied, it prevents any automatic interception and logging from occurring,
/// regardless of configuration settings or other attributes.
/// </summary>
/// <remarks>
/// <para>
/// This attribute provides the highest precedence in the logging decision hierarchy:
/// <list type="number">
/// <item><strong>NoLog/NoAutoLog attributes:</strong> Completely disable logging</item>
/// <item>Manual/Activity logging → Skip auto-intercept</item>
/// <item>Logging attributes → Use attribute configuration</item>
/// <item>Configuration patterns → Use config rules</item>
/// <item>Default → Auto-intercept all public methods</item>
/// </list>
/// </para>
/// <para>
/// This attribute can be applied at both method and class levels:
/// <list type="bullet">
/// <item><strong>Method level:</strong> Disables logging for the specific method</item>
/// <item><strong>Class level:</strong> Disables logging for all methods in the class</item>
/// </list>
/// </para>
/// <para>
/// Use this attribute for:
/// <list type="bullet">
/// <item>Performance-critical methods where logging overhead must be avoided</item>
/// <item>Methods handling sensitive data that should not be logged</item>
/// <item>Internal utility methods that don't provide business value when logged</item>
/// <item>Methods that would create infinite recursion if logged</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [LogBoth]
/// public class PaymentService
/// {
///     // This method will be logged (input and output)
///     public PaymentResult ProcessPayment(PaymentRequest request) { ... }
///
///     [NoLog] // Override class-level attribute - no logging
///     public string EncryptSensitiveData(string data) { ... }
///
///     [NoLog] // Performance-critical method - no logging overhead
///     public bool ValidateChecksum(byte[] data) { ... }
/// }
///
/// [NoLog] // Entire utility class excluded from logging
/// public class InternalUtilities
/// {
///     public static string FormatMessage(string template, params object[] args) { ... }
///     public static T DeepClone&lt;T&gt;(T obj) { ... }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
[UsedImplicitly]
public sealed class NoLogAttribute : Attribute;
