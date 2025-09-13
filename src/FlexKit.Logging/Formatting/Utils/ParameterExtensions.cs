using System.Text.Json;
using System.Text.Json.Serialization;
using FlexKit.Logging.Formatting.Models;
using FlexKit.Logging.Models;

namespace FlexKit.Logging.Formatting.Utils;

/// <summary>
/// Provides extension methods for building and populating parameter dictionaries based
/// on the information found in log entries.
/// </summary>
internal static class ParameterExtensions
{
    /// <summary>
    /// A static instance of <see cref="JsonSerializerOptions"/> used for JSON serialization settings
    /// in the context of parameter extraction and formatting.
    /// The configuration includes options such as ignoring cycles during reference handling,
    /// preventing null values from being written, limiting the maximum depth of serialization,
    /// and disabling indentation in the JSON output.
    /// </summary>
    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = false,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        MaxDepth = 3,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// Extracts all available parameters from a log entry for template substitution.
    /// </summary>
    /// <param name="context">The formatting context containing log entry and additional properties.</param>
    /// <returns>A dictionary of parameters available for template substitution.</returns>
    public static IReadOnlyDictionary<string, object?> ExtractParameters(
        this in FormattingContext context)
    {
        var parameters = new Dictionary<string, object?>();

        parameters.AddBasicParameters(context.LogEntry);
        parameters.AddExceptionParameters(context.LogEntry);
        parameters.AddContextProperties(context.Properties);

        return parameters;
    }

    /// <summary>
    /// Adds method parameter and output value information to the JSON object if available.
    /// </summary>
    /// <param name="entry">The log entry containing parameter and output information.</param>
    /// <returns>The log entry with parameter and output information added to the JSON object.</returns>
    public static LogEntry WithParametersJson(this LogEntry entry)
    {
        if (entry.InputParameters is not object[] rawInput || rawInput.Length == 0)
        {
            entry = entry.WithInput(entry.InputParameters ?? Array.Empty<object>());
        }
        else
        {
            entry = entry.WithInput(
                rawInput.Cast<InputParameter>().Select(item => new
                {
                    name = item.Name,
                    type = item.Type,
                    value = item.Value ?? "null",
                }));
        }

        return entry.OutputValue == null ? entry : entry.WithOutput(entry.OutputValue);
    }

    /// <summary>
    /// Enriches a log entry with input parameters and output values serialized as strings.
    /// </summary>
    /// <param name="entry">The log entry to be populated with string-formatted parameters.</param>
    /// <returns>The updated log entry with parameters and outputs serialized as strings.</returns>
    public static LogEntry WithParametersString(this LogEntry entry)
    {
        try
        {
            entry = SerializeInput(entry);

            return entry.OutputValue == null
                ? entry
                : entry.WithOutput(SerializeValueForJson(entry.OutputValue) ?? "null");
        }
        catch (Exception)
        {
            return entry;
        }
    }

    /// <summary>
    /// Processes and serializes the input parameters of the log entry into a JSON format.
    /// </summary>
    /// <param name="entry">The log entry containing input parameters to be serialized.</param>
    /// <returns>A log entry with serialized input parameters.</returns>
    private static LogEntry SerializeInput(LogEntry entry)
    {
        if (entry.InputParameters is not object[] rawInput || rawInput.Length == 0)
        {
            return entry.WithInput(entry.InputParameters == null
                ? JsonSerializer.Serialize(Array.Empty<object>())
                : SerializeValueForJson(entry.InputParameters));
        }

        return entry.WithInput(
            JsonSerializer.Serialize(rawInput.Cast<InputParameter>().Select(item => new
            {
                name = item.Name,
                type = item.Type,
                value = SerializeValueForJson(item.Value) ?? "null"
            })));
    }

    /// <summary>
    /// Adds basic log entry parameters like method name, type, success status, and thread ID.
    /// </summary>
    /// <param name="parameters">The parameter dictionary to populate.</param>
    /// <param name="entry">The log entry containing basic information.</param>
    private static void AddBasicParameters(
        this Dictionary<string, object?> parameters,
        in LogEntry entry)
    {
        parameters["MethodName"] = entry.MethodName;
        parameters["TypeName"] = entry.TypeName;
        parameters["Success"] = entry.Success;
        parameters["ThreadId"] = entry.ThreadId;
        parameters["Timestamp"] = entry.Timestamp;
        parameters["Id"] = entry.Id.ToString();
        parameters["ActivityId"] = entry.ActivityId;
        parameters["Duration"] = entry.Duration;
        parameters["DurationSeconds"] = entry.DurationSeconds;
        parameters["InputParameters"] = entry.InputParameters;
        parameters["OutputValue"] = entry.OutputValue;
    }

    /// <summary>
    /// Adds exception-related parameters for failed operations.
    /// </summary>
    /// <param name="parameters">The parameter dictionary to populate.</param>
    /// <param name="entry">The log entry that may contain exception information.</param>
    private static void AddExceptionParameters(
        this Dictionary<string, object?> parameters,
        in LogEntry entry)
    {
        if (entry.Success)
        {
            return;
        }

        parameters["ExceptionType"] = entry.ExceptionType;
        parameters["ExceptionMessage"] = entry.ExceptionMessage;
        parameters["StackTrace"] = entry.StackTrace;
    }

    /// <summary>
    /// Adds additional properties from the formatting context to the parameter dictionary.
    /// </summary>
    /// <param name="parameters">The parameter dictionary to populate.</param>
    /// <param name="contextProperties">Additional properties from the formatting context.</param>
    private static void AddContextProperties(
        this Dictionary<string, object?> parameters,
        IReadOnlyDictionary<string, object?> contextProperties)
    {
        foreach (var property in contextProperties)
        {
            parameters[property.Key] = property.Value;
        }
    }

    /// <summary>
    /// Serializes a value specifically for JSON output, returning objects that can be properly JSON serialized.
    /// Handles various data types including primitives, collections, and complex objects with appropriate formatting.
    /// </summary>
    /// <param name="value">The value to serialize for JSON output.</param>
    /// <returns>A JSON-serializable representation of the value.</returns>
    private static object? SerializeValueForJson(object? value) =>
        value switch
        {
            null => null,
            string or bool or byte or sbyte or short or ushort or int or uint or long or ulong or float or double
                or decimal => value,
            DateTime dt => dt.ToString("O"),
            DateTimeOffset dto => dto.ToString("O"),
            Guid guid => guid.ToString(),

            // For collections, create a proper array structure
            System.Collections.ICollection { Count: > 10 } collection =>
                new
                {
                    _type = "Collection",
                    _count = collection.Count,
                    _truncated = true,
                    items = collection.Cast<object>().Take(3).Select(SerializeValueForJson).ToArray()
                },

            System.Collections.IEnumerable enumerable =>
                enumerable.Cast<object>().Take(10).Select(SerializeValueForJson).ToArray(),

            // For complex objects, try to create a JSON-serializable representation
            var complexObj => SerializeComplexObjectForJson(complexObj)
        };

    /// <summary>
    /// Serializes complex objects for JSON output with truncation and error handling.
    /// Attempts deep serialization with cycle detection and size limits to prevent performance issues.
    /// </summary>
    /// <param name="obj">The complex object to serialize.</param>
    /// <returns>A JSON-serializable representation of the complex object with metadata.</returns>
    private static object SerializeComplexObjectForJson(object obj)
    {
        try
        {
            var json = JsonSerializer.Serialize(obj, _options);

            if (json.Length > 2000)
            {
                return new { _type = obj.GetType().Name, _truncated = true, _preview = json[..100] + "...", };
            }

            return JsonSerializer.Deserialize<object>(json, _options) ??
                   new { _type = obj.GetType().Name, _value = obj.ToString() };
        }
        catch
        {
            return new
            {
                _type = obj.GetType().Name,
                _error = "Serialization failed",
                _toString = obj.ToString() ?? "null",
            };
        }
    }
}
