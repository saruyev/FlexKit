using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using FlexKit.Logging.Configuration;
using FlexKit.Logging.Formatting.Core;
using FlexKit.Logging.Formatting.Models;
using FlexKit.Logging.Formatting.Translation;
using FlexKit.Logging.Formatting.Utils;
using FlexKit.Logging.Models;
using JetBrains.Annotations;

namespace FlexKit.Logging.Formatting.Formatters;

/// <summary>
/// Formats log entries using custom templates configured per service or context.
/// Allows templates like "Processing payment of {Amount} for {Customer}".
/// </summary>
/// <remarks>
/// Initializes a new instance of the CustomTemplateFormatter.
/// </remarks>
/// <param name="translator">The message translator for provider-specific syntax conversion.</param>
[UsedImplicitly]
public sealed partial class CustomTemplateFormatter(IMessageTranslator translator) : IMessageFormatter
{
    private readonly IMessageTranslator _translator = translator ?? throw new ArgumentNullException(nameof(translator));
    private readonly ConcurrentDictionary<string, string> _templateCache = new();

    [GeneratedRegex(@"\{([^}]+)\}", RegexOptions.Compiled)]
    private static partial Regex PlaceholderRegex();

    /// <inheritdoc />
    public FormatterType FormatterType => FormatterType.CustomTemplate;

    /// <summary>
    /// Formats a log entry using the appropriate custom template based on context.
    /// </summary>
    /// <param name="context">The formatting context containing log entry and configuration.</param>
    /// <returns>A formatted message result indicating success or failure.</returns>
    public FormattedMessage Format(FormattingContext context)
    {
        try
        {
            var customSettings = context.Configuration.Formatters.CustomTemplate;

            var template = GetCustomTemplate(context, customSettings);
            if (string.IsNullOrEmpty(template))
            {
                return FormattedMessage.Failure("No custom template found for context");
            }

            // Validate template if strict validation is enabled
            if (customSettings.StrictValidation && !ValidateTemplate(template, context))
            {
                return FormattedMessage.Failure($"Template validation failed for: {template}");
            }

            var processedTemplate = customSettings.CacheTemplates
                ? GetCachedTemplate(template)
                : template;

            var parameters = ExtractParameters(context);
            var translatedTemplate = _translator.TranslateTemplate(processedTemplate);
            var translatedParameters = _translator.TranslateParameters(parameters);

            var message = FormatMessage(translatedTemplate, translatedParameters);

            return FormattedMessage.Success(message);
        }
        catch (Exception ex)
        {
            return FormattedMessage.Failure($"Custom template formatting failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Determines whether this formatter can handle the given formatting context.
    /// </summary>
    /// <param name="context">The formatting context to evaluate.</param>
    /// <returns>True if the formatter can handle the context; otherwise, false.</returns>
    public bool CanFormat(FormattingContext context)
    {
        var customSettings = context.Configuration.Formatters.CustomTemplate;
        var template = GetCustomTemplate(context, customSettings);
        return !string.IsNullOrEmpty(template) &&
               _translator.CanTranslate(template, ExtractParameters(context));
    }

    /// <summary>
    /// Gets the appropriate custom template for the given context using priority-based resolution.
    /// </summary>
    /// <param name="context">The formatting context.</param>
    /// <param name="settings">The custom template formatter settings.</param>
    /// <returns>The resolved template or null if none found.</returns>
    private static string? GetCustomTemplate(
        in FormattingContext context,
        CustomTemplateFormatterSettings settings) =>
        TryGetServiceTemplate(context, settings) ??
        TryGetNamedTemplate(context, settings) ??
        TryGetTypeTemplate(context, settings) ??
        TryGetMethodTemplate(context, settings) ??
        TryGetDefaultTemplate(context, settings) ??
        TryGetFallbackTemplate(settings);

    /// <summary>
    /// Attempts to get a template based on the service name extracted from the log entry type.
    /// </summary>
    /// <param name="context">The formatting context.</param>
    /// <param name="settings">The custom template formatter settings.</param>
    /// <returns>The service template or null if not found.</returns>
    private static string? TryGetServiceTemplate(
        in FormattingContext context,
        CustomTemplateFormatterSettings settings)
    {
        var serviceName = GetServiceName(context.LogEntry.TypeName);
        return string.IsNullOrEmpty(serviceName) ||
               !settings.ServiceTemplates.TryGetValue(serviceName, out var serviceTemplate)
            ? null
            : GetValidTemplateFromConfig(context, serviceTemplate, settings);
    }

    /// <summary>
    /// Attempts to get a template based on the explicitly provided template name in the context.
    /// </summary>
    /// <param name="context">The formatting context.</param>
    /// <param name="settings">The custom template formatter settings.</param>
    /// <returns>The named template or null if not found.</returns>
    private static string? TryGetNamedTemplate(
        in FormattingContext context,
        CustomTemplateFormatterSettings settings) =>
        string.IsNullOrEmpty(context.TemplateName)
            ? null
            : GetValidTemplateFromConfig(context, context.TemplateName, settings);

    /// <summary>
    /// Attempts to get a template based on the log entry's type name.
    /// </summary>
    /// <param name="context">The formatting context.</param>
    /// <param name="settings">The custom template formatter settings.</param>
    /// <returns>The type-specific template or null if not found.</returns>
    private static string? TryGetTypeTemplate(
        FormattingContext context,
        CustomTemplateFormatterSettings settings) =>
        GetValidTemplateFromConfig(context, context.LogEntry.TypeName, settings);

    /// <summary>
    /// Attempts to get a template based on the log entry's method name.
    /// </summary>
    /// <param name="context">The formatting context.</param>
    /// <param name="settings">The custom template formatter settings.</param>
    /// <returns>The method-specific template or null if not found.</returns>
    private static string? TryGetMethodTemplate(
        in FormattingContext context,
        CustomTemplateFormatterSettings settings) =>
        GetValidTemplateFromConfig(context, context.LogEntry.MethodName, settings);

    /// <summary>
    /// Attempts to get the default template from configuration.
    /// </summary>
    /// <param name="context">The formatting context.</param>
    /// <param name="settings">The custom template formatter settings.</param>
    /// <returns>The default template or null if not found.</returns>
    private static string? TryGetDefaultTemplate(
        in FormattingContext context,
        CustomTemplateFormatterSettings settings) =>
        GetValidTemplateFromConfig(context, "Default", settings);

    /// <summary>
    /// Gets the fallback template from settings if all other template resolution methods fail.
    /// </summary>
    /// <param name="settings">The custom template formatter settings.</param>
    /// <returns>The fallback template or null if not available or invalid.</returns>
    private static string? TryGetFallbackTemplate(CustomTemplateFormatterSettings settings) =>
        !string.IsNullOrEmpty(settings.DefaultTemplate) &&
        (!settings.StrictValidation || IsValidTemplate(settings.DefaultTemplate))
            ? settings.DefaultTemplate
            : null;

    /// <summary>
    /// Retrieves and validates a template from a configuration based on the provided key.
    /// </summary>
    /// <param name="context">The formatting context.</param>
    /// <param name="templateKey">The template key to look up.</param>
    /// <param name="settings">The custom template formatter settings.</param>
    /// <returns>The valid template or null if not found or invalid.</returns>
    private static string? GetValidTemplateFromConfig(
        in FormattingContext context,
        string templateKey,
        CustomTemplateFormatterSettings settings)
    {
        if (
            !context.Configuration.Templates.TryGetValue(templateKey, out var templateConfig) ||
            !templateConfig.Enabled)
        {
            return null;
        }

        var template = templateConfig.GetTemplateForOutcome(context.LogEntry.Success);

        return !string.IsNullOrEmpty(template) && (!settings.StrictValidation || IsValidTemplate(template))
            ? template
            : null;
    }

    /// <summary>
    /// Extracts all available parameters from the formatting context for template substitution.
    /// </summary>
    /// <param name="context">The formatting context containing log entry and additional properties.</param>
    /// <returns>A dictionary of parameters available for template substitution.</returns>
    [SuppressMessage(
        "Performance",
        "CA1859:Use concrete types when possible for improved performance")]
    private static IReadOnlyDictionary<string, object?> ExtractParameters(in FormattingContext context)
    {
        var parameters = new Dictionary<string, object?>();

        AddBasicParameters(parameters, context.LogEntry);
        AddDurationParameters(parameters, context.LogEntry);
        AddExceptionParameters(parameters, context.LogEntry);
        AddActivityParameters(parameters, context.LogEntry);
        AddInputOutputParameters(parameters, context.LogEntry);
        AddContextProperties(parameters, context.Properties);

        return parameters;
    }

    /// <summary>
    /// Adds basic log entry parameters like method name, type, success status, etc.
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
        parameters["Id"] = entry.Id.ToString();
        parameters["Timestamp"] = entry.Timestamp.ToString("O");
    }

    /// <summary>
    /// Adds duration-related parameters if available in the log entry.
    /// </summary>
    /// <param name="parameters">The parameter dictionary to populate.</param>
    /// <param name="entry">The log entry that may contain duration information.</param>
    private static void AddDurationParameters(
        Dictionary<string, object?> parameters,
        in LogEntry entry)
    {
        if (!entry.DurationTicks.HasValue)
        {
            return;
        }

        var duration = TimeSpan.FromTicks(entry.DurationTicks.Value);
        parameters["Duration"] = Math.Round(duration.TotalMilliseconds, 2);
        parameters["DurationSeconds"] = Math.Round(duration.TotalSeconds, 3);
    }

    /// <summary>
    /// Adds exception-related parameters if the log entry represents a failure.
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
    /// Adds formatted input and output parameters if available in the log entry.
    /// </summary>
    /// <param name="parameters">The parameter dictionary to populate.</param>
    /// <param name="entry">The log entry that may contain input/output data.</param>
    private static void AddInputOutputParameters(
        Dictionary<string, object?> parameters,
        in LogEntry entry)
    {
        var inputDisplay = JsonParameterUtils.FormatParametersForDisplay(entry.InputParameters);
        if (!string.IsNullOrEmpty(inputDisplay))
        {
            parameters["InputParameters"] = inputDisplay;
        }

        var outputDisplay = JsonParameterUtils.FormatOutputForDisplay(entry.OutputValue);
        if (string.IsNullOrEmpty(outputDisplay))
        {
            return;
        }

        parameters["OutputValue"] = outputDisplay;
    }

    /// <summary>
    /// Adds additional properties from the formatting context to the parameter dictionary.
    /// </summary>
    /// <param name="parameters">The parameter dictionary to populate.</param>
    /// <param name="contextProperties">Additional properties from the formatting context.</param>
    private static void AddContextProperties(
        Dictionary<string, object?> parameters,
        IReadOnlyDictionary<string, object?> contextProperties)
    {
        foreach (var property in contextProperties)
        {
            parameters[property.Key] = property.Value;
        }
    }

    /// <summary>
    /// Formats a template string by replacing placeholders with parameter values.
    /// </summary>
    /// <param name="template">The template string containing placeholders.</param>
    /// <param name="parameters">The parameters to substitute into the template.</param>
    /// <returns>The formatted message with all placeholders replaced.</returns>
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

    /// <summary>
    /// Gets a cached version of the template or adds it to the cache if not present.
    /// </summary>
    /// <param name="template">The template to cache.</param>
    /// <returns>The cached template instance.</returns>
    private string GetCachedTemplate(string template) => _templateCache.GetOrAdd(template, t => t);

    /// <summary>
    /// Extracts a service name from a full type name using namespace conventions.
    /// </summary>
    /// <param name="typeName">The full type name including namespace.</param>
    /// <returns>The extracted service name or the type name if extraction fails.</returns>
    private static string GetServiceName(string typeName)
    {
        // Extract service name from namespace or type name
        // This could be more sophisticated based on naming conventions
        var parts = typeName.Split('.');
        return parts.Length > 1 ? parts[^1] : typeName;
    }

    /// <summary>
    /// Performs basic validation on a template string to ensure it's well-formed.
    /// </summary>
    /// <param name="template">The template string to validate.</param>
    /// <returns>True if the template has balanced braces and contains placeholders; otherwise, false.</returns>
    private static bool IsValidTemplate(string template)
    {
        // Basic template validation - check for balanced braces
        var openBraces = template.Count(c => c == '{');
        var closeBraces = template.Count(c => c == '}');
        return openBraces == closeBraces && openBraces > 0;
    }

    /// <summary>
    /// Validates that a template is well-formed and that all placeholders have corresponding parameters.
    /// </summary>
    /// <param name="template">The template to validate.</param>
    /// <param name="context">The formatting context containing available parameters.</param>
    /// <returns>True if the template is valid and all placeholders can be resolved; otherwise, false.</returns>
    private static bool ValidateTemplate(
        string template,
        in FormattingContext context)
    {
        if (!IsValidTemplate(template))
        {
            return false;
        }

        // Additional validation could check if all placeholders have corresponding parameters
        var parameters = ExtractParameters(context);

        // Extract placeholder names from the template
        var placeholders = PlaceholderRegex().Matches(template)
            .Select(m => m.Groups[1].Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Check if all placeholders have corresponding parameters
        return placeholders.All(p => parameters.ContainsKey(p));
    }
}
