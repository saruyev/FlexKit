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
public sealed class JsonFormatter(IMessageTranslator translator) : IMessageFormatter
{
    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = false,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

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
            var options = context.Configuration.Formatters.Json.PrettyPrint ? _prettySerializerOptions : _serializerOptions;

            return context.DisableFormatting ?
                FormattedMessage.Success(
                    translator.TranslateTemplate("{Metadata}"),
                    new Dictionary<string, object?> { ["Metadata"] = JsonSerializer.Serialize(entry, options) }) :
                FormattedMessage.Success(JsonSerializer.Serialize(entry, options));
        }
        catch (Exception ex)
        {
            return FormattedMessage.Failure($"JSON formatting failed: {ex.Message}");
        }
    }

    private FormattedMessage PrepareObject(LogEntry entry)
    {
        entry = entry.InputParameters is not null and not object[]? entry.WithInput(new List<object> { entry.InputParameters })
            : entry;

        return FormattedMessage.Success(
            translator.TranslateTemplate("{Metadata}"),
            new Dictionary<string, object?> { ["Metadata"] = entry });
    }
}
