using FlexKit.Logging.Configuration;
using FlexKit.Logging.Formatting.Core;
using FlexKit.Logging.Formatting.Models;
using System.Text.Json;
using FlexKit.Logging.Formatting.Utils;
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

    /// <summary>
    /// Determines whether this formatter can handle the given formatting context.
    /// The JSON formatter can handle any log entry.
    /// </summary>
    /// <param name="context">The formatting context to evaluate.</param>
    /// <returns>Always returns true as JSON formatter can handle any log entry.</returns>
    public bool CanFormat(FormattingContext context) => true; // JSON formatter can handle any log entry

    /// <summary>
    /// Creates a JSON object representation of a log entry with configurable property names and content.
    /// </summary>
    /// <param name="entry">The log entry to convert to JSON.</param>
    /// <param name="settings">The JSON formatter settings that control property names and included information.</param>
    /// <returns>A dictionary representing the JSON object structure.</returns>
    private static Dictionary<string, object?> CreateJsonObject(
        in LogEntry entry,
        JsonFormatterSettings settings)
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
        AddParameterInfo(jsonObject, entry, settings);

        return jsonObject;
    }

    /// <summary>
    /// Adds method parameter and output value information to the JSON object if available.
    /// </summary>
    /// <param name="jsonObject">The JSON object dictionary to populate.</param>
    /// <param name="entry">The log entry containing parameter and output information.</param>
    /// <param name="settings">The formatter settings that control property names.</param>
    private static void AddParameterInfo(
        Dictionary<string, object?> jsonObject,
        in LogEntry entry,
        JsonFormatterSettings settings)
    {
        var inputParams = JsonParameterUtils.ParseParametersAsJson(entry.InputParameters);
        if (inputParams != null)
        {
            jsonObject[GetPropertyName("input_parameters", settings)] = inputParams;
        }

        var outputValue = JsonParameterUtils.ParseOutputAsJson(entry.OutputValue);
        if (outputValue == null)
        {
            return;
        }

        jsonObject[GetPropertyName("output_value", settings)] = outputValue;
    }

    /// <summary>
    /// Adds timing information including timestamp and duration to the JSON object if enabled in settings.
    /// </summary>
    /// <param name="jsonObject">The JSON object dictionary to populate.</param>
    /// <param name="entry">The log entry containing timing information.</param>
    /// <param name="settings">The formatter settings that control whether timing info is included.</param>
    private static void AddTimingInfo(
        Dictionary<string, object?> jsonObject,
        in LogEntry entry,
        JsonFormatterSettings settings)
    {
        if (!settings.IncludeTimingInfo)
        {
            return;
        }

        jsonObject[GetPropertyName("timestamp", settings)] = entry.Timestamp.ToString("O");

        if (!entry.DurationTicks.HasValue)
        {
            return;
        }

        var duration = TimeSpan.FromTicks(entry.DurationTicks.Value).TotalMilliseconds;
        jsonObject[GetPropertyName("duration_ms", settings)] = Math.Round(duration, 2);
    }

    /// <summary>
    /// Adds thread and activity tracking information to the JSON object if enabled in settings.
    /// </summary>
    /// <param name="jsonObject">The JSON object dictionary to populate.</param>
    /// <param name="entry">The log entry containing thread and activity information.</param>
    /// <param name="settings">The formatter settings that control whether thread info is included.</param>
    private static void AddThreadInfo(
        Dictionary<string, object?> jsonObject,
        in LogEntry entry,
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

    /// <summary>
    /// Adds exception information to the JSON object for failed operations, including optional stack trace.
    /// </summary>
    /// <param name="jsonObject">The JSON object dictionary to populate.</param>
    /// <param name="entry">The log entry that may contain exception information.</param>
    /// <param name="settings">The formatter settings that control property names and stack trace inclusion.</param>
    private static void AddExceptionInfo(
        Dictionary<string, object?> jsonObject,
        in LogEntry entry,
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

    /// <summary>
    /// Gets the appropriate property name for a JSON field, using custom names from settings if available.
    /// </summary>
    /// <param name="defaultName">The default property name to use.</param>
    /// <param name="settings">The formatter settings that may contain custom property name mappings.</param>
    /// <returns>The custom property name if configured, otherwise the default name.</returns>
    private static string GetPropertyName(
        string defaultName,
        JsonFormatterSettings settings) =>
        settings.CustomPropertyNames.TryGetValue(defaultName, out var customName)
            ? customName
            : defaultName;
}
