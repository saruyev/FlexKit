using FlexKit.Logging.Configuration;
using FlexKit.Logging.Formatting.Models;

namespace FlexKit.Logging.Formatting.Core;

/// <summary>
/// Defines the contract for formatting log entries into human-readable messages.
/// Formatters transform structured log data into formatted strings using various strategies.
/// </summary>
public interface IMessageFormatter
{
    /// <summary>
    /// Gets the formatter type that this implementation handles.
    /// </summary>
    FormatterType FormatterType { get; }

    /// <summary>
    /// Formats a log entry into a structured message using the specified context.
    /// </summary>
    /// <param name="context">The formatting context containing the log entry and configuration.</param>
    /// <returns>A formatted message result containing the output string and metadata.</returns>
    /// <remarks>
    /// <para>
    /// This method should handle all formatting logic for its specific formatter type,
    /// including template processing, data extraction, and error handling.
    /// </para>
    /// <para>
    /// Implementations should:
    /// <list type="bullet">
    /// <item>Extract relevant data from the log entry</item>
    /// <item>Apply formatter-specific logic and templates</item>
    /// <item>Handle formatting errors gracefully</item>
    /// <item>Return appropriate success or failure results</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Performance Considerations:</strong>
    /// This method is called on background threads but should still be efficient
    /// as it processes every log entry. Avoid expensive operations and prefer
    /// cached templates and pre-compiled formatters where possible.
    /// </para>
    /// </remarks>
    FormattedMessage Format(FormattingContext context);
}
