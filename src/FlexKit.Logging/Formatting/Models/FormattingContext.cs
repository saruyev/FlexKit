using FlexKit.Logging.Configuration;
using FlexKit.Logging.Models;

namespace FlexKit.Logging.Formatting.Models;

/// <summary>
/// Provides contextual information and settings for the message formatting process.
/// Contains the log entry, configuration settings, and additional metadata needed by formatters.
/// </summary>
public readonly record struct FormattingContext
{
    /// <summary>
    /// Gets the log entry to be formatted.
    /// </summary>
    public LogEntry LogEntry { get; private init; }

    /// <summary>
    /// Gets the logging configuration containing formatter settings.
    /// </summary>
    public LoggingConfig Configuration { get; private init; }

    /// <summary>
    /// Gets the specific formatter type to use for this context.
    /// </summary>
    public FormatterType FormatterType { get; private init; }

    /// <summary>
    /// Gets additional properties that can be used during formatting.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Properties { get; private init; }

    /// <summary>
    /// Gets the template name to use it for custom template formatting.
    /// </summary>
    public string? TemplateName { get; private init; }

    /// <summary>
    /// Gets whether fallback formatting should be enabled if primary formatting fails.
    /// </summary>
    public bool EnableFallback { get; private init; }

    /// <summary>
    /// Creates a new formatting context.
    /// </summary>
    public static FormattingContext Create(
        LogEntry logEntry,
        LoggingConfig configuration) =>
        new()
        {
            LogEntry = logEntry,
            Configuration = configuration,
            FormatterType = configuration.DefaultFormatter,
            Properties = new Dictionary<string, object?>(),
            EnableFallback = configuration.EnableFallbackFormatting
        };

    /// <summary>
    /// Creates a copy of this context with a different formatter type.
    /// </summary>
    public FormattingContext WithFormatterType(FormatterType formatterType) => this with { FormatterType = formatterType };

    /// <summary>
    /// Creates a copy of this context with additional properties.
    /// </summary>
    public FormattingContext WithProperties(IReadOnlyDictionary<string, object?> properties) => this with { Properties = properties };

    /// <summary>
    /// Creates a copy of this context with a specific template name.
    /// </summary>
    public FormattingContext WithTemplateName(string templateName) => this with { TemplateName = templateName };
}
