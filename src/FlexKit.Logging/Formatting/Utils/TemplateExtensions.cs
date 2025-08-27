using FlexKit.Logging.Configuration;
using FlexKit.Logging.Formatting.Models;
using FlexKit.Logging.Models;

namespace FlexKit.Logging.Formatting.Utils;

/// <summary>
/// Provides an extension method for retrieving the appropriate template for log entry formatting
/// based on configuration or fallback logic.
/// </summary>
public static class TemplateExtensions
{
    private static readonly Dictionary<FormatterType, Dictionary<TemplateKeys, string>> _templates = new()
    {
        [FormatterType.StandardStructured] = new()
        {
            [TemplateKeys.SuccessStart] = "Method {MethodName} started",
            [TemplateKeys.SuccessEnd] = "Method {MethodName} completed in {Duration}ms",
            [TemplateKeys.ErrorStart] = "Method {MethodName} failed",
            [TemplateKeys.ErrorEnd] = "Method {MethodName} failed after {Duration}ms"
        },
        [FormatterType.SuccessError] = new()
        {
            [TemplateKeys.SuccessStart] = "✅ Method {MethodName} started successfully",
            [TemplateKeys.SuccessEnd] = "✅ Method {MethodName} completed successfully in {Duration}ms",
            [TemplateKeys.ErrorStart] = "❌ Method {MethodName} failed: {ExceptionType} - {ExceptionMessage}",
            [TemplateKeys.ErrorEnd] = "❌ Method {MethodName} failed: {ExceptionType} - {ExceptionMessage} (after {Duration}ms)"
        },
    };

    /// <summary>
    /// Gets the appropriate template for formatting based on configuration or fallback templates.
    /// </summary>
    /// <param name="context">The formatting context containing configuration and log entry.</param>
    /// <param name="key">The formatter type to use for the template.</param>
    /// <returns>The template string to use for formatting.</returns>
    public static string GetTemplate(this in FormattingContext context, FormatterType key)
    {
        var configuredTemplate = TryGetConfiguredTemplate(context, key.ToString());
        return !string.IsNullOrEmpty(configuredTemplate) ? configuredTemplate : GetFallbackTemplate(context.LogEntry, key);
    }

    /// <summary>
    /// Attempts to get a template from the configuration if available and valid.
    /// </summary>
    /// <param name="context">The formatting context containing configuration.</param>
    /// <param name="key">The template key to look up.</param>
    /// <returns>The configured template if available and valid; otherwise, null.</returns>
    private static string? TryGetConfiguredTemplate(in FormattingContext context, string key)
    {
        var validTemplate = context.Configuration.Templates.TryGetValue(
            key,
            out var templateConfig) && templateConfig.Enabled && templateConfig.IsValid();

        return !validTemplate ? null : templateConfig?.GetTemplateForOutcome(context.LogEntry.Success);
    }

    /// <summary>
    /// Gets a fallback template based on the log entry characteristics when no configured template is available.
    /// </summary>
    /// <param name="entry">The log entry to create a template for.</param>
    /// <param name="key">The formatter type to use for the template.</param>
    /// <returns>A fallback template appropriate for the log entry type.</returns>
    private static string GetFallbackTemplate(in LogEntry entry, FormatterType key) =>
        entry.Success
            ? GetSuccessTemplate(entry, key)
            : GetFailureTemplate(entry, key);

    /// <summary>
    /// Creates a template for successful operations, including duration and parameter information.
    /// </summary>
    /// <param name="entry">The log entry representing a successful operation.</param>
    /// <param name="key">The formatter type to use for the template.</param>
    /// <returns>A template string for successful operations.</returns>
    private static string GetSuccessTemplate(in LogEntry entry, FormatterType key)
    {
        var baseTemplate = entry.DurationTicks.HasValue
            ? _templates[key][TemplateKeys.SuccessEnd]
            : _templates[key][TemplateKeys.SuccessStart];

        return AppendParameterTemplates(baseTemplate, entry);
    }

    /// <summary>
    /// Creates a template for failed operations, including duration and parameter information.
    /// </summary>
    /// <param name="entry">The log entry representing a failed operation.</param>
    /// <param name="key">The formatter type to use for the template.</param>
    /// <returns>A template string for failed operations.</returns>
    private static string GetFailureTemplate(in LogEntry entry, FormatterType key)
    {
        var baseTemplate = entry.DurationTicks.HasValue
            ? _templates[key][TemplateKeys.ErrorEnd]
            : _templates[key][TemplateKeys.ErrorStart];

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
    /// Represents the keys used to identify specific template strings for log entry formatting.
    /// These keys are used in conjunction with formatter types to map to predefined or
    /// configured templates for log messages, such as method start and end messages.
    /// </summary>
    private enum TemplateKeys
    {
        SuccessStart = 0,
        SuccessEnd = 1,
        ErrorStart = 2,
        ErrorEnd = 3,
    }
}
