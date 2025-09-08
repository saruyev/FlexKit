using JetBrains.Annotations;

namespace FlexKit.Logging.Interception.Attributes;

/// <summary>
/// Attribute to mark parameters, properties, or types as sensitive data that should be masked in log output.
/// When applied, the actual values will be replaced with mask text during logging operations.
/// </summary>
/// <remarks>
/// <para>
/// This attribute provides fine-grained control over data masking at the code level, complementing
/// configuration-based masking patterns. It can be applied to:
/// <list type="bullet">
/// <item><strong>Parameters:</strong> Masks the parameter value in method input logging</item>
/// <item><strong>Properties:</strong> Masks the property value when the containing object is logged</item>
/// <item><strong>Types:</strong> Masks all instances of the type regardless of context</item>
/// </list>
/// </para>
/// <para>
/// <strong>Precedence Order:</strong>
/// <list type="number">
/// <item>Attribute-based masking (this attribute) - highest priority</item>
/// <item>Configuration-based patterns - medium priority</item>
/// <item>No masking - default behavior</item>
/// </list>
/// </para>
/// <para>
/// Use this attribute for:
/// <list type="bullet">
/// <item>Sensitive data that should never appear in logs (passwords, tokens, keys)</item>
/// <item>Personally identifiable information (PII) that requires protection</item>
/// <item>Financial or confidential business data</item>
/// <item>Any data subject to regulatory compliance requirements</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Parameter-level masking
/// public void AuthenticateUser([Mask] string password, string username)
/// {
///     // password parameter will be masked in logs, username will not
/// }
///
/// // Property-level masking in DTOs
/// public class UserCredentials
/// {
///     public string Username { get; set; }
///
///     [Mask(Replacement = "[REDACTED]")]
///     public string Password { get; set; }
///
///     [Mask]
///     public string ApiKey { get; set; }
/// }
///
/// // Type-level masking
/// [Mask(Replacement = "[CLASSIFIED]")]
/// public class SecretConfiguration
/// {
///     // All instances of this type will be masked
///     public string DatabasePassword { get; set; }
///     public string EncryptionKey { get; set; }
/// }
///
/// // Usage in service methods
/// [LogBoth]
/// public class PaymentService
/// {
///     public PaymentResult ProcessPayment(
///         PaymentRequest request,
///         [Mask] string creditCardNumber)
///     {
///         // creditCardNumber will be masked in input logs
///         // If PaymentRequest contains [Mask] properties, they'll be masked too
///         // PaymentResult properties with [Mask] will be masked in output logs
///     }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct)]
[UsedImplicitly]
public sealed class MaskAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the replacement text to use when masking the sensitive data.
    /// If not specified, uses the default mask replacement from configuration.
    /// </summary>
    /// <value>
    /// The text to display instead of the actual sensitive value.
    /// Default is null, which means use the system default mask replacement.
    /// </value>
    /// <remarks>
    /// <para>
    /// When this property is null or empty, the masking system will use the default
    /// replacement text configured in the logging configuration. This allows for
    /// consistent masking appearance across the application while still allowing
    /// specific customization when needed.
    /// </para>
    /// <para>
    /// <strong>Common Replacement Patterns:</strong>
    /// <list type="bullet">
    /// <item><strong>"***MASKED***"</strong> - Clear sign of masked data</item>
    /// <item><strong>"[REDACTED]"</strong> - Formal, document-style masking</item>
    /// <item><strong>"&lt;hidden&gt;"</strong> - XML-style masking</item>
    /// <item><strong>"########"</strong> - Character-based masking</item>
    /// <item><strong>"[PII_REMOVED]"</strong> - Compliance-oriented masking</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class SecurityContext
    /// {
    ///     [Mask] // Uses default mask replacement
    ///     public string AccessToken { get; set; }
    ///
    ///     [Mask(Replacement = "[CLASSIFIED]")] // Custom replacement
    ///     public string SecurityKey { get; set; }
    ///
    ///     [Mask(Replacement = "****-****-****-XXXX")] // Pattern-based replacement
    ///     public string CreditCardNumber { get; set; }
    /// }
    /// </code>
    /// </example>
    public string? Replacement { get; set; }
}
