using FlexKit.Logging.Configuration;
using FlexKit.Logging.Formatting.Core;
using FlexKit.Logging.Formatting.Models;
using System.Text.Json;
using FlexKit.Logging.Models;

namespace FlexKit.Logging.Formatting.Formatters;

/// <summary>
/// Formats log entries as JSON objects with method execution data.
/// Produces structured JSON like {"method_name": "ProcessPayment", "duration": 450, "success": true}.
/// </summary>
public sealed class JsonFormatter : IMessageFormatter
{
    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = false
    };

    private static readonly JsonSerializerOptions _prettySerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true
    };

    /// <inheritdoc />
    public FormatterType FormatterType => FormatterType.Json;

    /// <inheritdoc />
    public FormattedMessage Format(FormattingContext context)
    {
        try
        {
            var jsonSettings = context.Configuration.Formatters.Json;
            var jsonObject = CreateJsonObject(context.LogEntry, jsonSettings);

            var options = jsonSettings.PrettyPrint ? _prettySerializerOptions : _serializerOptions;
            var json = JsonSerializer.Serialize(jsonObject, options);

            return FormattedMessage.Success(json);
        }
        catch (Exception ex)
        {
            return FormattedMessage.Failure($"JSON formatting failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public bool CanFormat(FormattingContext context) => true; // JSON formatter can handle any log entry

    private static Dictionary<string, object?> CreateJsonObject(LogEntry entry, JsonFormatterSettings settings)
    {
        var jsonObject = new Dictionary<string, object?>
        {
            ["id"] = entry.Id.ToString(),
            ["timestamp"] = new DateTimeOffset(entry.TimestampTicks, TimeSpan.Zero).ToString("O"),
            ["method_name"] = entry.MethodName,
            ["type_name"] = entry.TypeName,
            ["success"] = entry.Success,
            ["thread_id"] = entry.ThreadId
        };

        if (entry.DurationTicks.HasValue)
        {
            var duration = TimeSpan.FromTicks(entry.DurationTicks.Value).TotalMilliseconds;
            jsonObject["duration_ms"] = Math.Round(duration, 2);
        }

        if (!entry.Success)
        {
            jsonObject["exception"] = new
            {
                type = entry.ExceptionType,
                message = entry.ExceptionMessage
            };

            if (settings.IncludeStackTrace)
            {
                jsonObject["stack_trace"] = "Stack trace not available in LogEntry model";
            }
        }

        if (!string.IsNullOrEmpty(entry.ActivityId))
        {
            jsonObject["activity_id"] = entry.ActivityId;
        }

        return jsonObject;
    }
}
