using FlexKit.Logging.Configuration;
using FlexKit.Logging.Formatting.Core;
using FlexKit.Logging.Formatting.Models;
using FlexKit.Logging.Formatting.Translation;
using System.Diagnostics.CodeAnalysis;
using FlexKit.Logging.Formatting.Utils;
using FlexKit.Logging.Models;

namespace FlexKit.Logging.Formatting.Formatters;

/// <summary>
/// Formats log entries using standard structured templates with parameter placeholders.
/// Produces human-readable messages like "Method ProcessPayment completed in 450 ms".
/// </summary>
/// <remarks>
/// Initializes a new instance of the StandardStructuredFormatter.
/// </remarks>
/// <param name="translator">The message translator for provider-specific syntax conversion.</param>
public sealed class StandardStructuredFormatter(IMessageTranslator translator) : IMessageFormatter
{
    private readonly IMessageTranslator _translator =
        translator ?? throw new ArgumentNullException(nameof(translator));

    /// <inheritdoc />
    public FormatterType FormatterType => FormatterType.StandardStructured;

    /// <summary>
    /// Formats a log entry using structured templates with parameter substitution.
    /// </summary>
    /// <param name="context">The formatting context containing log entry and configuration.</param>
    /// <returns>A formatted message result with structured template output.</returns>
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

    /// <summary>
    /// Determines whether this formatter can handle the given formatting context.
    /// </summary>
    /// <param name="context">The formatting context to evaluate.</param>
    /// <returns>True if the translator can handle the template and parameters; otherwise, false.</returns>
    public bool CanFormat(FormattingContext context) =>
        _translator.CanTranslate(GetTemplate(context), ExtractParameters(context.LogEntry));

    /// <summary>
    /// Gets the appropriate template for formatting based on configuration or fallback templates.
    /// </summary>
    /// <param name="context">The formatting context containing configuration and log entry.</param>
    /// <returns>The template string to use for formatting.</returns>
    private static string GetTemplate(in FormattingContext context)
    {
        var configuredTemplate = TryGetConfiguredTemplate(context);
        return !string.IsNullOrEmpty(configuredTemplate) ? configuredTemplate : GetFallbackTemplate(context.LogEntry);
    }

    /// <summary>
    /// Attempts to get a template from the configuration if available and valid.
    /// </summary>
    /// <param name="context">The formatting context containing configuration.</param>
    /// <returns>The configured template if available and valid; otherwise, null.</returns>
    private static string? TryGetConfiguredTemplate(in FormattingContext context)
    {
        var validTemplate = context.Configuration.Templates.TryGetValue(
            "StandardStructured",
            out var templateConfig) && templateConfig.Enabled && templateConfig.IsValid();

        return !validTemplate ? null : templateConfig?.GetTemplateForOutcome(context.LogEntry.Success);
    }

    /// <summary>
    /// Gets a fallback template based on the log entry characteristics when no configured template is available.
    /// </summary>
    /// <param name="entry">The log entry to create a template for.</param>
    /// <returns>A fallback template appropriate for the log entry type.</returns>
    private static string GetFallbackTemplate(in LogEntry entry) =>
        entry.Success
            ? GetSuccessTemplate(entry)
            : GetFailureTemplate(entry);

    /// <summary>
    /// Creates a template for successful operations, including duration and parameter information.
    /// </summary>
    /// <param name="entry">The log entry representing a successful operation.</param>
    /// <returns>A template string for successful operations.</returns>
    private static string GetSuccessTemplate(in LogEntry entry)
    {
        var baseTemplate = entry.DurationTicks.HasValue
            ? "Method {MethodName} completed in {Duration}ms"
            : "Method {MethodName} started";

        return AppendParameterTemplates(baseTemplate, entry);
    }

    /// <summary>
    /// Creates a template for failed operations, including duration and parameter information.
    /// </summary>
    /// <param name="entry">The log entry representing a failed operation.</param>
    /// <returns>A template string for failed operations.</returns>
    private static string GetFailureTemplate(in LogEntry entry)
    {
        var baseTemplate = entry.DurationTicks.HasValue
            ? "Method {MethodName} failed after {Duration}ms"
            : "Method {MethodName} failed";

        return AppendParameterTemplates(baseTemplate, entry);
    }

    /// <summary>
    /// Appends input and output parameter template sections to a base template if the data is available.
    /// </summary>
    /// <param name="baseTemplate">The base template to append to.</param>
    /// <param name="entry">The log entry containing parameter information.</param>
    /// <returns>The template with parameter sections appended.</returns>
    private static string AppendParameterTemplates(
        string baseTemplate,
        in LogEntry entry)
    {
        var template = baseTemplate;

        if (!string.IsNullOrEmpty(entry.InputParameters?.ToString()))
        {
            template += " | Input: {InputParameters}";
        }

        if (!string.IsNullOrEmpty(entry.OutputValue?.ToString()))
        {
            template += " | Output: {OutputValue}";
        }

        return template;
    }

    /// <summary>
    /// Extracts all available parameters from a log entry for template substitution.
    /// </summary>
    /// <param name="entry">The log entry to extract parameters from.</param>
    /// <returns>A dictionary of parameters available for template substitution.</returns>
    [SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance")]
    private static IReadOnlyDictionary<string, object?> ExtractParameters(in LogEntry entry)
    {
        var parameters = new Dictionary<string, object?>();

        AddBasicParameters(parameters, entry);
        AddDurationParameters(parameters, entry);
        AddExceptionParameters(parameters, entry);
        AddActivityParameters(parameters, entry);
        AddInputOutputParameters(parameters, entry);

        return parameters;
    }

    /// <summary>
    /// Adds basic log entry parameters like method name, type, success status, and thread ID.
    /// </summary>
    /// <param name="parameters">The parameter dictionary to populate.</param>
    /// <param name="entry">The log entry containing basic information.</param>
    private static void AddBasicParameters(
        Dictionary<string, object?> parameters,
        in LogEntry entry)
    {
        parameters["MethodName"] = entry.MethodName;
        parameters["TypeName"] = entry.TypeName;
        parameters["Success"] = entry.Success;
        parameters["ThreadId"] = entry.ThreadId;
        parameters["Timestamp"] = entry.Timestamp;
    }

    /// <summary>
    /// Adds duration parameters if timing information is available in the log entry.
    /// </summary>
    /// <param name="parameters">The parameter dictionary to populate.</param>
    /// <param name="entry">The log entry that may contain duration information.</param>
    private static void AddDurationParameters(
        Dictionary<string, object?> parameters,
        in LogEntry entry)
    {
        if (!entry.DurationTicks.HasValue)
        {
            parameters["Duration"] = 0;
            return;
        }

        var duration = TimeSpan.FromTicks(entry.DurationTicks.Value).TotalMilliseconds;
        parameters["Duration"] = Math.Round(duration, 2);
    }

    /// <summary>
    /// Adds exception-related parameters for failed operations.
    /// </summary>
    /// <param name="parameters">The parameter dictionary to populate.</param>
    /// <param name="entry">The log entry that may contain exception information.</param>
    private static void AddExceptionParameters(
        Dictionary<string, object?> parameters,
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
    /// Adds activity tracking parameters if available in the log entry.
    /// </summary>
    /// <param name="parameters">The parameter dictionary to populate.</param>
    /// <param name="entry">The log entry that may contain activity information.</param>
    private static void AddActivityParameters(
        Dictionary<string, object?> parameters,
        in LogEntry entry)
    {
        if (string.IsNullOrEmpty(entry.ActivityId))
        {
            return;
        }

        parameters["ActivityId"] = entry.ActivityId;
    }

    /// <summary>
    /// Adds formatted input and output parameters using utility methods for proper display formatting.
    /// </summary>
    /// <param name="parameters">The parameter dictionary to populate.</param>
    /// <param name="entry">The log entry that may contain input/output data.</param>
    private static void AddInputOutputParameters(
        Dictionary<string, object?> parameters,
        in LogEntry entry)
    {
        var inputDisplay = JsonParameterUtils.FormatParametersForDisplay(entry.InputParameters?.ToString());
        if (!string.IsNullOrEmpty(inputDisplay))
        {
            parameters["InputParameters"] = inputDisplay;
        }

        parameters["OutputValue"] = JsonParameterUtils.FormatOutputForDisplay(entry.OutputValue?.ToString());
    }

    /// <summary>
    /// Formats a template string by replacing placeholders with parameter values.
    /// </summary>
    /// <param name="template">The template string containing placeholders in {PropertyName} format.</param>
    /// <param name="parameters">The parameters to substitute into the template.</param>
    /// <returns>The formatted message with all placeholders replaced by their corresponding values.</returns>
    private static string FormatMessage(
        string template,
        IReadOnlyDictionary<string, object?> parameters)
    {
        var result = template;

        if (
            parameters.ContainsKey("ExceptionMessage") &&
            !string.IsNullOrEmpty(parameters["ExceptionMessage"]?.ToString()))
        {
            result += " | Exception: {ExceptionType} - {ExceptionMessage}";
        }

        foreach (var parameter in parameters)
        {
            var placeholder = $"{{{parameter.Key}}}";
            var value = parameter.Value?.ToString() ?? "null";
            result = result.Replace(placeholder, value, StringComparison.OrdinalIgnoreCase);
        }

        return result;
    }
}
