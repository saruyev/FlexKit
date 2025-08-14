using FlexKit.Logging.Configuration;
using FlexKit.Logging.Formatting.Core;
using FlexKit.Logging.Formatting.Models;
using FlexKit.Logging.Formatting.Translation;
using System.Diagnostics.CodeAnalysis;
using FlexKit.Logging.Models;

namespace FlexKit.Logging.Formatting.Formatters;

/// <summary>
/// Formats log entries using standard structured templates with parameter placeholders.
/// Produces human-readable messages like "Method ProcessPayment completed in 450ms".
/// </summary>
public sealed class StandardStructuredFormatter : IMessageFormatter
{
    private readonly IMessageTranslator _translator;

    /// <summary>
    /// Initializes a new instance of the StandardStructuredFormatter.
    /// </summary>
    /// <param name="translator">The message translator for provider-specific syntax conversion.</param>
    public StandardStructuredFormatter(IMessageTranslator translator)
    {
        _translator = translator ?? throw new ArgumentNullException(nameof(translator));
    }

    /// <inheritdoc />
    public FormatterType FormatterType => FormatterType.StandardStructured;

    /// <inheritdoc />
    public FormattedMessage Format(FormattingContext context)
    {
        try
        {
            var template = GetTemplate(context);
            var parameters = ExtractParameters(context.LogEntry);

            var translatedTemplate = _translator.TranslateTemplate(template);
            var translatedParameters = _translator.TranslateParameters(parameters);

            var message = FormatMessage(translatedTemplate, translatedParameters);

            return FormattedMessage.Success(message);
        }
        catch (Exception ex)
        {
            return FormattedMessage.Failure($"StandardStructured formatting failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public bool CanFormat(FormattingContext context) => _translator.CanTranslate(GetTemplate(context), ExtractParameters(context.LogEntry));

    private static string GetTemplate(FormattingContext context)
    {
        var entry = context.LogEntry;

        if (entry.Success)
        {
            return entry.DurationTicks.HasValue
                ? "Method {MethodName} completed in {Duration}ms"
                : "Method {MethodName} started";
        }

        return entry.DurationTicks.HasValue
            ? "Method {MethodName} failed after {Duration}ms"
            : "Method {MethodName} failed";
    }

    [SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance")]
    private static IReadOnlyDictionary<string, object?> ExtractParameters(LogEntry entry)
    {
        var parameters = new Dictionary<string, object?>
        {
            ["MethodName"] = entry.MethodName,
            ["TypeName"] = entry.TypeName,
            ["Success"] = entry.Success,
            ["ThreadId"] = entry.ThreadId
        };

        if (entry.DurationTicks.HasValue)
        {
            var duration = TimeSpan.FromTicks(entry.DurationTicks.Value).TotalMilliseconds;
            parameters["Duration"] = Math.Round(duration, 2);
        }

        if (!entry.Success)
        {
            parameters["ExceptionType"] = entry.ExceptionType;
            parameters["ExceptionMessage"] = entry.ExceptionMessage;
        }

        if (!string.IsNullOrEmpty(entry.ActivityId))
        {
            parameters["ActivityId"] = entry.ActivityId;
        }

        return parameters;
    }

    private static string FormatMessage(string template, IReadOnlyDictionary<string, object?> parameters)
    {
        var result = template;

        foreach (var parameter in parameters)
        {
            var placeholder = $"{{{parameter.Key}}}";
            var value = parameter.Value?.ToString() ?? "null";
            result = result.Replace(placeholder, value, StringComparison.OrdinalIgnoreCase);
        }

        return result;
    }
}
