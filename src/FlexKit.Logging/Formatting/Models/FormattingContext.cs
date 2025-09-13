using FlexKit.Logging.Configuration;
using FlexKit.Logging.Formatting.Utils;
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
    /// Indicates whether formatting is disabled for the current context.
    /// When set to true, the raw template and parameters are used without applying formatting transformations.
    /// </summary>
    public bool DisableFormatting { get; private init; }

    /// <summary>
    /// Creates a new instance of the <see cref="FormattingContext"/> with the specified
    /// <paramref name="logEntry"/> and <paramref name="configuration"/>.
    /// </summary>
    /// <param name="logEntry">The log entry to be associated with the formatting context.</param>
    /// <param name="configuration">The logging configuration to use for the formatting context.</param>
    /// <returns>
    /// A new <see cref="FormattingContext"/> configured with the specified log entry and logging configuration.
    /// </returns>
    public static FormattingContext Create(
        in LogEntry logEntry,
        LoggingConfig configuration) =>
        new()
        {
            LogEntry = logEntry,
            Configuration = configuration,
            FormatterType = logEntry.Formatter ??
                            GetTargetFormatter(logEntry.Target, configuration) ??
                            configuration.DefaultFormatter,
            Properties = new Dictionary<string, object?>(),
            EnableFallback = configuration.EnableFallbackFormatting,
        };

    /// <summary>
    /// Creates a new instance of the <see cref="FormattingContext"/> with the specified
    /// <paramref name="formatterType"/>.
    /// </summary>
    /// <param name="formatterType">The formatter type to be used in the new context.</param>
    /// <returns>
    /// A new <see cref="FormattingContext"/> where the <see cref="FormatterType"/> has been set to the specified value.
    /// </returns>
    public FormattingContext WithFormatterType(FormatterType formatterType) =>
        this with { FormatterType = formatterType };

    /// <summary>
    /// Updates the formatting context with a new set of properties.
    /// </summary>
    /// <param name="properties">
    /// A read-only dictionary containing key-value pairs to associate with the formatting context.
    /// </param>
    /// <returns>A new <see cref="FormattingContext"/> instance with the specified properties applied.</returns>
    public FormattingContext WithProperties(IReadOnlyDictionary<string, object?> properties) =>
        this with { Properties = properties };

    /// <summary>
    /// Creates a new <see cref="FormattingContext"/> with the specified template name applied.
    /// </summary>
    /// <param name="templateName">The name of the template to use in the formatting context.</param>
    /// <returns>A new <see cref="FormattingContext"/> with the provided template name set.</returns>
    public FormattingContext WithTemplateName(string templateName) =>
        this with { TemplateName = templateName };

    /// <summary>
    /// Returns a new <see cref="FormattingContext"/> instance with formatting explicitly disabled.
    /// </summary>
    /// <returns>A copy of the current <see cref="FormattingContext"/> with formatting disabled.</returns>
    public FormattingContext WithoutFormatting() =>
        this with { DisableFormatting = true };

    /// <summary>
    /// Creates a new instance of the <see cref="FormattingContext"/> with the log entry's parameters
    /// converted to their string representations.
    /// </summary>
    /// <returns>
    /// A new <see cref="FormattingContext"/> containing the log entry with its parameters represented as strings.
    /// </returns>
    public FormattingContext Stringify() =>
        this with { LogEntry = LogEntry.WithParametersString() };

    /// <summary>
    /// Converts the parameters in the associated log entry to their JSON representation.
    /// </summary>
    /// <returns>
    /// A new <see cref="FormattingContext"/> with the parameters of the associated log entry
    /// represented in JSON format.
    /// </returns>
    public FormattingContext Jsonify() =>
        this with { LogEntry = LogEntry.WithParametersJson() };

    /// <summary>
    /// Retrieves the formatter type associated with the specified target name
    /// using the provided logging configuration.
    /// </summary>
    /// <param name="targetName">The name of the logging target whose formatter is to be retrieved.</param>
    /// <param name="configuration">
    /// The logging configuration that contains target definitions and their associated formatters.
    /// </param>
    /// <returns>
    /// The <see cref="FormatterType"/> associated with the specified target, or <c>null</c> if no
    /// matching target is found or the target name is null or empty.
    /// </returns>
    private static FormatterType? GetTargetFormatter(string? targetName, LoggingConfig configuration) =>
        !string.IsNullOrEmpty(targetName) &&
        configuration.Targets.TryGetValue(targetName, out var target)
            ? target.Formatter
            : null;
}
