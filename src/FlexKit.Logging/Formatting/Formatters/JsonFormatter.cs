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
            [GetPropertyName("id", settings)] = entry.Id.ToString(),
            [GetPropertyName("method_name", settings)] = entry.MethodName,
            [GetPropertyName("type_name", settings)] = entry.TypeName,
            [GetPropertyName("success", settings)] = entry.Success
        };

        AddTimingInfo(jsonObject, entry, settings);
        AddThreadInfo(jsonObject, entry, settings);
        AddExceptionInfo(jsonObject, entry, settings);

        return jsonObject;
    }

    private static void AddTimingInfo(Dictionary<string, object?> jsonObject, LogEntry entry,
        JsonFormatterSettings settings)
    {
        if (!settings.IncludeTimingInfo)
        {
            return;
        }

        jsonObject[GetPropertyName("timestamp", settings)] =
            new DateTimeOffset(entry.TimestampTicks, TimeSpan.Zero).ToString("O");

        if (!entry.DurationTicks.HasValue)
        {
            return;
        }

        var duration = TimeSpan.FromTicks(entry.DurationTicks.Value).TotalMilliseconds;
        jsonObject[GetPropertyName("duration_ms", settings)] = Math.Round(duration, 2);
    }

    private static void AddThreadInfo(Dictionary<string, object?> jsonObject, LogEntry entry,
        JsonFormatterSettings settings)
    {
        if (!settings.IncludeThreadInfo)
        {
            return;
        }

        jsonObject[GetPropertyName("thread_id", settings)] = entry.ThreadId;

        if (string.IsNullOrEmpty(entry.ActivityId))
        {
            return;
        }

        jsonObject[GetPropertyName("activity_id", settings)] = entry.ActivityId;
    }

    private static void AddExceptionInfo(Dictionary<string, object?> jsonObject, LogEntry entry,
        JsonFormatterSettings settings)
    {
        if (entry.Success)
        {
            return;
        }

        jsonObject[GetPropertyName("exception", settings)] = new
        {
            type = entry.ExceptionType,
            message = entry.ExceptionMessage
        };

        if (!settings.IncludeStackTrace)
        {
            return;
        }

        jsonObject[GetPropertyName("stack_trace", settings)] = "Stack trace not available in LogEntry model";
    }

    private static string GetPropertyName(string defaultName, JsonFormatterSettings settings) =>
        settings.CustomPropertyNames.TryGetValue(defaultName, out var customName)
            ? customName
            : defaultName;
}
