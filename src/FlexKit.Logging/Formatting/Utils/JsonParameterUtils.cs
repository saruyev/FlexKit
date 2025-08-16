using System.Text.Json;

namespace FlexKit.Logging.Formatting.Utils;

/// <summary>
/// Utility class for parsing and formatting parameter data between JSON and display formats.
/// Provides consistent handling of InputParameters and OutputValue across different formatters.
/// </summary>
public static class JsonParameterUtils
{
    /// <summary>
    /// Parses JSON-serialized input parameters into a proper JSON array structure.
    /// Handles parameter arrays with name, type, and value properties.
    /// </summary>
    /// <param name="parametersJson">JSON string containing a serialized parameter array.</param>
    /// <returns>JSON-serializable array of parameter objects or null if parsing fails.</returns>
    public static object? ParseParametersAsJson(string? parametersJson)
    {
        if (string.IsNullOrEmpty(parametersJson))
        {
            return null;
        }

        try
        {
            return TryParseParameterArray(parametersJson);
        }
        catch
        {
            return TryParseJsonOrReturnString(parametersJson);
        }
    }

    /// <summary>
    /// Parses JSON-serialized output value into a proper JSON object structure.
    /// Handles output objects with type and value properties.
    /// </summary>
    /// <param name="outputJson">JSON string containing a serialized output object.</param>
    /// <returns>JSON-serializable output object or null if parsing fails.</returns>
    public static object? ParseOutputAsJson(string? outputJson)
    {
        if (string.IsNullOrEmpty(outputJson))
        {
            return null;
        }

        try
        {
            return TryParseOutputObject(outputJson);
        }
        catch
        {
            return TryParseJsonOrReturnString(outputJson);
        }
    }

    /// <summary>
    /// Formats JSON-serialized input parameters for human-readable text display.
    /// Creates a bracketed list with name-value pairs for each parameter.
    /// </summary>
    /// <param name="parametersJson">JSON string containing a serialized parameter array.</param>
    /// <returns>Human-readable string representation of parameters in format "[name: value, ...]".</returns>
    public static string FormatParametersForDisplay(string? parametersJson)
    {
        if (string.IsNullOrEmpty(parametersJson))
        {
            return string.Empty;
        }

        try
        {
            return TryFormatParameterArray(parametersJson);
        }
        catch
        {
            return parametersJson; // Fallback to raw string
        }
    }

    /// <summary>
    /// Formats JSON-serialized output value for human-readable text display.
    /// Extracts the value from output objects or formats the entire element.
    /// </summary>
    /// <param name="outputJson">JSON string containing a serialized output object.</param>
    /// <returns>Human-readable string representation of the output value.</returns>
    public static string FormatOutputForDisplay(string? outputJson)
    {
        if (string.IsNullOrEmpty(outputJson))
        {
            return string.Empty;
        }

        try
        {
            return TryFormatOutputValue(outputJson);
        }
        catch
        {
            return outputJson; // Fallback to raw string
        }
    }

    /// <summary>
    /// Attempts to parse a parameter array from JSON, converting each parameter to a structured object.
    /// </summary>
    /// <param name="parametersJson">The JSON string containing parameter array data.</param>
    /// <returns>An array of parameter objects with name, type, and value properties.</returns>
    private static Dictionary<string, object?>[]? TryParseParameterArray(string parametersJson)
    {
        var paramArray = JsonSerializer.Deserialize<JsonElement[]>(parametersJson);
        return paramArray?.Select(ConvertParameterElement).ToArray();
    }

    /// <summary>
    /// Converts a single parameter JsonElement to a structured dictionary object.
    /// </summary>
    /// <param name="param">The JsonElement representing a parameter.</param>
    /// <returns>A dictionary containing the parameter's name, type, and value.</returns>
    private static Dictionary<string, object?> ConvertParameterElement(JsonElement param)
    {
        var obj = new Dictionary<string, object?>();

        if (param.TryGetProperty("name", out var nameElement))
        {
            obj["name"] = nameElement.GetString();
        }

        if (param.TryGetProperty("type", out var typeElement))
        {
            obj["type"] = typeElement.GetString();
        }

        if (param.TryGetProperty("value", out var valueElement))
        {
            obj["value"] = ParseJsonValue(valueElement);
        }

        return obj;
    }

    /// <summary>
    /// Attempts to parse an output object from JSON, handling structured output with type and value.
    /// </summary>
    /// <param name="outputJson">The JSON string containing output object data.</param>
    /// <returns>A structured output object or the parsed JSON value.</returns>
    private static object? TryParseOutputObject(string outputJson)
    {
        var outputElement = JsonSerializer.Deserialize<JsonElement>(outputJson);

        return outputElement.TryGetProperty("value", out var valueElement)
            ? CreateOutputObject(outputElement, valueElement)
            : ParseJsonValue(outputElement);
    }

    /// <summary>
    /// Creates a structured output object with type and value properties.
    /// </summary>
    /// <param name="outputElement">The complete output JsonElement.</param>
    /// <param name="valueElement">The value portion of the output.</param>
    /// <returns>A dictionary containing the output's type and value.</returns>
    private static Dictionary<string, object?> CreateOutputObject(
        in JsonElement outputElement,
        in JsonElement valueElement)
    {
        var result = new Dictionary<string, object?>();

        if (outputElement.TryGetProperty("type", out var typeElement))
        {
            result["type"] = typeElement.GetString();
        }

        result["value"] = ParseJsonValue(valueElement);
        return result;
    }

    /// <summary>
    /// Attempts to format a parameter array for display, creating a bracketed list of name-value pairs.
    /// </summary>
    /// <param name="parametersJson">The JSON string containing parameter array data.</param>
    /// <returns>A formatted string representation of the parameters.</returns>
    private static string TryFormatParameterArray(string parametersJson)
    {
        var args = JsonSerializer.Deserialize<JsonElement[]>(parametersJson);

        if (args?.Length == 0)
        {
            return "[]";
        }

        var formatted = args?.Select(FormatParameterElement) ?? Array.Empty<string>();
        return $"[{string.Join(", ", formatted)}]";
    }

    /// <summary>
    /// Formats a single parameter element for display as "name: value".
    /// </summary>
    /// <param name="arg">The JsonElement representing a parameter.</param>
    /// <returns>A formatted string in the "name: value" format.</returns>
    private static string FormatParameterElement(JsonElement arg)
    {
        var name = ExtractParameterName(arg);
        var value = ExtractParameterValue(arg);
        return $"{name}: {value}";
    }

    /// <summary>
    /// Extracts the parameter name from a parameter JsonElement.
    /// </summary>
    /// <param name="arg">The JsonElement representing a parameter.</param>
    /// <returns>The parameter name or "unknown" if not found.</returns>
    private static string ExtractParameterName(in JsonElement arg) =>
        arg.TryGetProperty("name", out var nameElement)
            ? nameElement.GetString() ?? "unknown"
            : "unknown";

    /// <summary>
    /// Extracts and formats the parameter value from a parameter JsonElement.
    /// </summary>
    /// <param name="arg">The JsonElement representing a parameter.</param>
    /// <returns>The formatted parameter value or "null" if not found.</returns>
    private static string ExtractParameterValue(in JsonElement arg) =>
        arg.TryGetProperty("value", out var valueElement)
            ? FormatValueForDisplay(valueElement)
            : "null";

    /// <summary>
    /// Attempts to format an output value for display, extracting the value from structured outputs.
    /// </summary>
    /// <param name="outputJson">The JSON string containing output data.</param>
    /// <returns>A formatted string representation of the output value.</returns>
    private static string TryFormatOutputValue(string outputJson)
    {
        var output = JsonSerializer.Deserialize<JsonElement>(outputJson);

        return FormatValueForDisplay(output.TryGetProperty("value", out var valueElement) ? valueElement : output);
    }

    /// <summary>
    /// Parses a JsonElement into a proper .NET object for JSON serialization.
    /// Handles all JSON value types, including primitives, objects, and arrays.
    /// </summary>
    /// <param name="element">The JsonElement to parse.</param>
    /// <returns>A .NET object representation suitable for JSON serialization.</returns>
    private static object? ParseJsonValue(in JsonElement element) =>
        element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var longVal) ? longVal : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Object => JsonSerializer.Deserialize<Dictionary<string, object?>>(element.GetRawText()),
            JsonValueKind.Array => JsonSerializer.Deserialize<object[]>(element.GetRawText()),
            _ => element.GetRawText()
        };

    /// <summary>
    /// Formats a JsonElement into a human-readable string for text display.
    /// Provides appropriate formatting for all JSON value types.
    /// </summary>
    /// <param name="element">The JsonElement to format.</param>
    /// <returns>A human-readable string representation of the value.</returns>
    private static string FormatValueForDisplay(JsonElement element) =>
        element.ValueKind switch
        {
            JsonValueKind.String => $"\"{element.GetString()}\"",
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => "null",
            JsonValueKind.Object => FormatObjectForDisplay(element),
            JsonValueKind.Array => FormatArrayForDisplay(element),
            _ => element.GetRawText()
        };

    /// <summary>
    /// Formats a JSON object for compact text display, showing up to 3 properties.
    /// </summary>
    /// <param name="element">The JsonElement representing an object.</param>
    /// <returns>A compact string representation like "{prop1: value1, prop2: value2, ...}".</returns>
    private static string FormatObjectForDisplay(in JsonElement element)
    {
        try
        {
            var properties = element.EnumerateObject().Take(3).Select(prop =>
                $"{prop.Name}: {FormatValueForDisplay(prop.Value)}");

            var result = $"{{{string.Join(", ", properties)}}}";

            if (element.EnumerateObject().Count() > 3)
            {
                result = result[..^1] + ", ...}";
            }

            return result;
        }
        catch
        {
            return element.GetRawText();
        }
    }

    /// <summary>
    /// Formats a JSON array for compact text display, showing up to 3 elements.
    /// </summary>
    /// <param name="element">The JsonElement representing an array.</param>
    /// <returns>A compact string representation like "[item1, item2, item3, ...]".</returns>
    private static string FormatArrayForDisplay(in JsonElement element)
    {
        try
        {
            var items = element.EnumerateArray().Take(3).Select(FormatValueForDisplay);
            var result = $"[{string.Join(", ", items)}]";

            if (element.GetArrayLength() > 3)
            {
                result = result[..^1] + ", ...]";
            }

            return result;
        }
        catch
        {
            return element.GetRawText();
        }
    }

    /// <summary>
    /// Attempts to parse a string as JSON, returning the original string if parsing fails.
    /// Used as a fallback when structured parsing is not possible.
    /// </summary>
    /// <param name="value">The string value to attempt to parse as JSON.</param>
    /// <returns>The parsed JSON object or the original string if parsing fails.</returns>
    private static object? TryParseJsonOrReturnString(string value)
    {
        try
        {
            return JsonSerializer.Deserialize<object>(value);
        }
        catch
        {
            return value;
        }
    }
}
