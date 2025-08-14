using JetBrains.Annotations;

namespace FlexKit.Logging.Configuration;

/// <summary>
/// Configuration for a specific template used in message formatting.
/// Defines templates for different execution outcomes and validation rules.
/// </summary>
public class TemplateConfig
{
    /// <summary>
    /// Gets or sets the template for successful method executions.
    /// Used when <see cref="Models.LogEntry.Success"/> is true.
    /// </summary>
    /// <value>The success template string with placeholders for log entry properties.</value>
    /// <example>
    /// "Method {MethodName} completed successfully in {Duration}ms"
    /// </example>
    public string SuccessTemplate { get; [UsedImplicitly] set; } = string.Empty;

    /// <summary>
    /// Gets or sets the template for failed method executions.
    /// Used when <see cref="Models.LogEntry.Success"/> is false.
    /// </summary>
    /// <value>The error template string with placeholders for log entry properties and exception details.</value>
    /// <example>
    /// "Method {MethodName} failed with {ExceptionType}: {ExceptionMessage} after {Duration}ms"
    /// </example>
    public string ErrorTemplate { get; [UsedImplicitly] set; } = string.Empty;

    /// <summary>
    /// Gets or sets a general-purpose template that works for both success and error cases.
    /// Used when success/error specific templates are not provided or when the formatter doesn't distinguish between outcomes.
    /// </summary>
    /// <value>The general template string that works for all execution outcomes.</value>
    /// <example>
    /// "Method {MethodName} executed with status {Success} in {Duration}ms"
    /// </example>
    [UsedImplicitly]
    public string GeneralTemplate { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this template configuration is enabled.
    /// Disabled templates are ignored during formatting and fall back to defaults.
    /// </summary>
    /// <value>True if the template is enabled; false if disabled. Default is true.</value>
    public bool Enabled { get; [UsedImplicitly] set; } = true;

    /// <summary>
    /// Gets the appropriate template based on the success status of a log entry.
    /// Provides intelligent template selection based on an execution outcome.
    /// </summary>
    /// <param name="success">Whether the method execution was successful.</param>
    /// <returns>
    /// The appropriate template string:
    /// - <see cref="SuccessTemplate"/> if success is true and template is not empty
    /// - <see cref="ErrorTemplate"/> if success is false and template is not empty
    /// - <see cref="GeneralTemplate"/> as fallback if specific templates are not available
    /// - Empty string if no templates are configured
    /// </returns>
    /// <remarks>
    /// This method implements the template selection logic used by formatters that distinguish
    /// between successful and failed method executions. It ensures a graceful fallback when
    /// specific templates are not configured.
    ///
    /// <para>
    /// <strong>Selection Priority:</strong>
    /// <list type="number">
    /// <item>Success-specific template if success is true and SuccessTemplate is not empty</item>
    /// <item>Error-specific template if success is false and ErrorTemplate is not empty</item>
    /// <item>General template if a specific template is not available</item>
    /// <item>Empty string if no templates are configured</item>
    /// </list>
    /// </para>
    /// </remarks>
    public string GetTemplateForOutcome(bool success) =>
        success switch
        {
            true when !string.IsNullOrWhiteSpace(SuccessTemplate) => SuccessTemplate,
            false when !string.IsNullOrWhiteSpace(ErrorTemplate) => ErrorTemplate,
            _ => GeneralTemplate
        };

    /// <summary>
    /// Validates that the template configuration has at least one usable template.
    /// Used during startup validation to ensure templates are properly configured.
    /// </summary>
    /// <returns>true if the configuration has at least one non-empty template; false otherwise.</returns>
    /// <remarks>
    /// A valid template configuration must have at least one of:
    /// <list type="bullet">
    /// <item>A non-empty <see cref="SuccessTemplate"/></item>
    /// <item>A non-empty <see cref="ErrorTemplate"/></item>
    /// <item>A non-empty <see cref="GeneralTemplate"/></item>
    /// </list>
    ///
    /// This validation helps catch configuration errors early in the application lifecycle.
    /// </remarks>
    public bool IsValid() =>
        !string.IsNullOrWhiteSpace(SuccessTemplate) ||
        !string.IsNullOrWhiteSpace(ErrorTemplate) ||
        !string.IsNullOrWhiteSpace(GeneralTemplate);

    /// <summary>
    /// Gets all available templates in this configuration.
    /// Useful for validation and debugging purposes.
    /// </summary>
    /// <returns>An enumerable of all non-empty template strings in this configuration.</returns>
    public IEnumerable<string> GetAllTemplates()
    {
        if (!string.IsNullOrWhiteSpace(SuccessTemplate))
        {
            yield return SuccessTemplate;
        }

        if (!string.IsNullOrWhiteSpace(ErrorTemplate))
        {
            yield return ErrorTemplate;
        }

        if (string.IsNullOrWhiteSpace(GeneralTemplate))
        {
            yield break;
        }

        yield return GeneralTemplate;
    }
}
