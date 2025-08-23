using System.Diagnostics.CodeAnalysis;
using FlexKit.Logging.Configuration;
using FlexKit.Logging.Formatting.Core;
using FlexKit.Logging.Formatting.Models;
using FlexKit.Logging.Formatting.Translation;
using FlexKit.Logging.Formatting.Utils;
using FlexKit.Logging.Models;
using JetBrains.Annotations;

namespace FlexKit.Logging.Formatting.Formatters;

/// <summary>
/// Formats log entries using different templates for successful vs. failed method executions.
/// Success: "Method ProcessPayment completed in 450 ms"
/// Error: "Method ProcessPayment failed: InvalidOperationException after 450 ms"
/// </summary>
/// <remarks>
/// Initializes a new instance of the SuccessErrorFormatter.
/// </remarks>
/// <param name="translator">The message translator for provider-specific syntax conversion.</param>
[UsedImplicitly]
public sealed class SuccessErrorFormatter(IMessageTranslator translator) : IMessageFormatter
{
    private readonly IMessageTranslator _translator =
        translator ?? throw new ArgumentNullException(nameof(translator));

    /// <inheritdoc />
    public FormatterType FormatterType => FormatterType.SuccessError;

    /// <summary>
    /// Formats a log entry using success/error-specific templates with visual indicators.
    /// </summary>
    /// <param name="context">The formatting context containing log entry and configuration.</param>
    /// <returns>A formatted message result with success/error-specific formatting.</returns>
    public FormattedMessage Format(FormattingContext context)
    {
        try
        {
            var template = GetTemplate(context.LogEntry, context);
            var parameters = ExtractParameters(context.LogEntry);

            var translatedTemplate = _translator.TranslateTemplate(template);
            var translatedParameters = _translator.TranslateParameters(parameters);

            var message = FormatMessage(translatedTemplate, translatedParameters);

            return FormattedMessage.Success(message);
        }
        catch (Exception ex)
        {
            return FormattedMessage.Failure($"SuccessError formatting failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Determines whether this formatter can handle the given formatting context.
    /// </summary>
    /// <param name="context">The formatting context to evaluate.</param>
    /// <returns>True if the translator can handle the template and parameters; otherwise, false.</returns>
    public bool CanFormat(FormattingContext context)
    {
        var template = GetTemplate(context.LogEntry, context);
        var parameters = ExtractParameters(context.LogEntry);
        return _translator.CanTranslate(template, parameters);
    }

    /// <summary>
    /// Gets the appropriate template for formatting based on success/failure status and configuration.
    /// </summary>
    /// <param name="entry">The log entry to get a template for.</param>
    /// <param name="context">The formatting context containing configuration.</param>
    /// <returns>The template string to use for formatting.</returns>
    private static string GetTemplate(
        in LogEntry entry,
        in FormattingContext context)
    {
        var configuredTemplate = TryGetConfiguredTemplate(entry, context);
        return !string.IsNullOrEmpty(configuredTemplate) ? configuredTemplate : GetFallbackTemplate(entry);
    }

    /// <summary>
    /// Attempts to get a template from the configuration based on a success/failure outcome.
    /// </summary>
    /// <param name="entry">The log entry to determine the outcome for.</param>
    /// <param name="context">The formatting context containing configuration.</param>
    /// <returns>The configured template if available and valid; otherwise, null.</returns>
    private static string? TryGetConfiguredTemplate(
        in LogEntry entry,
        in FormattingContext context)
    {
        var validTemplate = context.Configuration.Templates.TryGetValue(
            "SuccessError",
            out var templateConfig) && templateConfig.Enabled && templateConfig.IsValid();
        return !validTemplate ? null : templateConfig?.GetTemplateForOutcome(entry.Success);
    }

    /// <summary>
    /// Gets a fallback template with visual indicators based on the log entry's success/failure status.
    /// </summary>
    /// <param name="entry">The log entry to create a template for.</param>
    /// <returns>A fallback template with appropriate visual indicators and structure.</returns>
    private static string GetFallbackTemplate(in LogEntry entry) =>
        entry.Success
            ? GetSuccessTemplate(entry)
            : GetFailureTemplate(entry);

    /// <summary>
    /// Creates a success template with a green checkmark indicator and completion messaging.
    /// </summary>
    /// <param name="entry">The log entry representing a successful operation.</param>
    /// <returns>A template string for successful operations with visual indicators.</returns>
    private static string GetSuccessTemplate(in LogEntry entry)
    {
        var baseTemplate = entry.DurationTicks.HasValue
            ? "✅ Method {MethodName} completed successfully in {Duration}ms"
            : "✅ Method {MethodName} started successfully";

        return AppendParameterTemplates(baseTemplate, entry, includeOutput: true);
    }

    /// <summary>
    /// Creates a failure template with red X indicator and exception information.
    /// </summary>
    /// <param name="entry">The log entry representing a failed operation.</param>
    /// <returns>A template string for failed operations with visual indicators and exception details.</returns>
    private static string GetFailureTemplate(in LogEntry entry)
    {
        var baseTemplate = entry.DurationTicks.HasValue
            ? "❌ Method {MethodName} failed: {ExceptionType} - {ExceptionMessage} (after {Duration}ms)"
            : "❌ Method {MethodName} failed: {ExceptionType} - {ExceptionMessage}";

        return AppendParameterTemplates(baseTemplate, entry, includeOutput: false);
    }

    /// <summary>
    /// Appends input and optionally output parameter template sections to a base template.
    /// </summary>
    /// <param name="baseTemplate">The base template to append to.</param>
    /// <param name="entry">The log entry containing parameter information.</param>
    /// <param name="includeOutput">Whether to include an output value template (typically false for failures).</param>
    /// <returns>The template with parameter sections appended as appropriate.</returns>
    [SuppressMessage("ReSharper", "FlagArgument")]
    private static string AppendParameterTemplates(
        string baseTemplate,
        in LogEntry entry,
        bool includeOutput)
    {
        var template = baseTemplate;

        if (!string.IsNullOrEmpty(entry.InputParameters?.ToString()))
        {
            template += " | Input: {InputParameters}";
        }

        if (includeOutput && !string.IsNullOrEmpty(entry.OutputValue?.ToString()))
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
    [SuppressMessage(
        "Performance",
        "CA1859:Use concrete types when possible for improved performance")]
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
    /// Adds exception-related parameters for failed operations with fallback values for missing data.
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

        parameters["ExceptionType"] = entry.ExceptionType ?? "UnknownException";
        parameters["ExceptionMessage"] = entry.ExceptionMessage ?? "No exception message available";
        parameters["StackTrace"] = entry.StackTrace ?? "No stack trace available";
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

        foreach (var parameter in parameters)
        {
            var placeholder = $"{{{parameter.Key}}}";
            var value = parameter.Value?.ToString() ?? "null";
            result = result.Replace(placeholder, value, StringComparison.OrdinalIgnoreCase);
        }

        return result;
    }
}
