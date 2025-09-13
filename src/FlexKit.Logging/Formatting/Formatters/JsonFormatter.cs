using FlexKit.Logging.Configuration;
using FlexKit.Logging.Formatting.Core;
using FlexKit.Logging.Formatting.Models;
using System.Text.Json;
using FlexKit.Logging.Formatting.Translation;
using FlexKit.Logging.Formatting.Utils;
using FlexKit.Logging.Models;

namespace FlexKit.Logging.Formatting.Formatters;

/// <summary>
/// Formats log entries as JSON objects with method execution data.
/// Produces structured JSON like {"method_name": "ProcessPayment", "duration": 450, "success": true}.
/// </summary>
internal sealed class JsonFormatter(IMessageTranslator translator) : IMessageFormatter
{
    /// <summary>
    /// Contains preconfigured options used by the JSON serializer in the <see cref="JsonFormatter"/>.
    /// These options define the serialization behavior, including property naming policy,
    /// output formatting, and character encoding, optimized for log entry formatting.
    /// </summary>
    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = false,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// Defines preconfigured JSON serialization options optimized for human-readable output.
    /// These options include snake_case naming policy, indented formatting, and relaxed JSON escaping.
    /// Designed for use in contexts where pretty-printed serialization is required for
    /// enhanced readability of log entries or diagnostic information.
    /// </summary>
    private static readonly JsonSerializerOptions _prettySerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <inheritdoc />
    public FormatterType FormatterType => FormatterType.Json;

    /// <summary>
    /// Formats a log entry as a JSON object containing all relevant execution information.
    /// </summary>
    /// <param name="context">The formatting context containing log entry and configuration.</param>
    /// <returns>A formatted message result containing the JSON representation of the log entry.</returns>
    public FormattedMessage Format(FormattingContext context)
    {
        try
        {
            if (context.DisableFormatting && !context.Configuration.Formatters.Json.PrettyPrint)
            {
                return PrepareObject(context.LogEntry);
            }

            var entry = context.LogEntry.WithParametersJson();
            var options = context.Configuration.Formatters.Json.PrettyPrint
                ? _prettySerializerOptions
                : _serializerOptions;

            return context.DisableFormatting ?
                FormattedMessage.Success(
                    translator.TranslateTemplate("{Metadata}", context.Configuration),
                    new Dictionary<string, object?> { ["Metadata"] = JsonSerializer.Serialize(entry, options) }) :
                FormattedMessage.Success(JsonSerializer.Serialize(entry, options));
        }
        catch (Exception ex)
        {
            return FormattedMessage.Failure($"JSON formatting failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Prepares a formatted message based on the given log entry by processing its input parameters
    /// and translating metadata into the desired format.
    /// </summary>
    /// <param name="entry">The log entry to process and include in the formatted message.</param>
    /// <returns>A formatted message containing translated metadata and log entry details.</returns>
    private FormattedMessage PrepareObject(LogEntry entry)
    {
        entry = entry.InputParameters is not null and not object[]?
            entry.WithInput(new List<object> { entry.InputParameters })
            : entry;

        return FormattedMessage.Success(
            translator.TranslateTemplate("{Metadata}"),
            new Dictionary<string, object?> { ["Metadata"] = entry });
    }
}
