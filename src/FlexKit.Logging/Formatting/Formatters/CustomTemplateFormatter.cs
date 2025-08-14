using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using FlexKit.Logging.Configuration;
using FlexKit.Logging.Formatting.Core;
using FlexKit.Logging.Formatting.Models;
using FlexKit.Logging.Formatting.Translation;
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

    /// <inheritdoc />
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

    /// <inheritdoc />
    public bool CanFormat(FormattingContext context)
    {
        var customSettings = context.Configuration.Formatters.CustomTemplate;
        var template = GetCustomTemplate(context, customSettings);
        return !string.IsNullOrEmpty(template) &&
               _translator.CanTranslate(template, ExtractParameters(context));
    }

    private static string? GetCustomTemplate(FormattingContext context, CustomTemplateFormatterSettings settings) =>
        TryGetServiceTemplate(context, settings) ??
        TryGetNamedTemplate(context, settings) ??
        TryGetTypeTemplate(context, settings) ??
        TryGetMethodTemplate(context, settings) ??
        TryGetDefaultTemplate(context, settings) ??
        TryGetFallbackTemplate(settings);

    private static string? TryGetServiceTemplate(FormattingContext context, CustomTemplateFormatterSettings settings)
    {
        var serviceName = GetServiceName(context.LogEntry.TypeName);
        if (string.IsNullOrEmpty(serviceName) || !settings.ServiceTemplates.TryGetValue(serviceName, out var serviceTemplate))
        {
            return null;
        }

        return GetValidTemplateFromConfig(context, serviceTemplate, settings);
    }

    private static string? TryGetNamedTemplate(FormattingContext context, CustomTemplateFormatterSettings settings)
    {
        if (string.IsNullOrEmpty(context.TemplateName))
        {
            return null;
        }

        return GetValidTemplateFromConfig(context, context.TemplateName, settings);
    }

    private static string? TryGetTypeTemplate(FormattingContext context, CustomTemplateFormatterSettings settings) => GetValidTemplateFromConfig(context, context.LogEntry.TypeName, settings);

    private static string? TryGetMethodTemplate(FormattingContext context, CustomTemplateFormatterSettings settings) => GetValidTemplateFromConfig(context, context.LogEntry.MethodName, settings);

    private static string? TryGetDefaultTemplate(FormattingContext context, CustomTemplateFormatterSettings settings) => GetValidTemplateFromConfig(context, "Default", settings);

    private static string? TryGetFallbackTemplate(CustomTemplateFormatterSettings settings) =>
        !string.IsNullOrEmpty(settings.DefaultTemplate) &&
        (!settings.StrictValidation || IsValidTemplate(settings.DefaultTemplate))
            ? settings.DefaultTemplate
            : null;

    private static string? GetValidTemplateFromConfig(FormattingContext context, string templateKey, CustomTemplateFormatterSettings settings)
    {
        if (!context.Configuration.Templates.TryGetValue(templateKey, out var templateConfig) || !templateConfig.Enabled)
        {
            return null;
        }

        var template = templateConfig.GetTemplateForOutcome(context.LogEntry.Success);

        return !string.IsNullOrEmpty(template) && (!settings.StrictValidation || IsValidTemplate(template))
            ? template
            : null;
    }

    [SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance")]
    private static IReadOnlyDictionary<string, object?> ExtractParameters(FormattingContext context)
    {
        var entry = context.LogEntry;
        var parameters = new Dictionary<string, object?>
        {
            ["MethodName"] = entry.MethodName,
            ["TypeName"] = entry.TypeName,
            ["Success"] = entry.Success,
            ["ThreadId"] = entry.ThreadId,
            ["Id"] = entry.Id.ToString(),
            ["Timestamp"] = new DateTimeOffset(entry.TimestampTicks, TimeSpan.Zero).ToString("O")
        };

        if (entry.DurationTicks.HasValue)
        {
            var duration = TimeSpan.FromTicks(entry.DurationTicks.Value);
            parameters["Duration"] = Math.Round(duration.TotalMilliseconds, 2);
            parameters["DurationSeconds"] = Math.Round(duration.TotalSeconds, 3);
        }

        if (!entry.Success)
        {
            parameters["ExceptionType"] = entry.ExceptionType;
            parameters["ExceptionMessage"] = entry.ExceptionMessage;
        }

        if (!string.IsNullOrEmpty(entry.ActivityId))
        {
            parameters["ActivityId"] = entry.ActivityId;
        }

        // Add any additional properties from context
        foreach (var property in context.Properties)
        {
            parameters[property.Key] = property.Value;
        }

        return parameters;
    }

    private static string FormatMessage(string template, IReadOnlyDictionary<string, object?> parameters)
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

    private string GetCachedTemplate(string template) => _templateCache.GetOrAdd(template, t => t);

    private static string GetServiceName(string typeName)
    {
        // Extract service name from namespace or type name
        // This could be more sophisticated based on naming conventions
        var parts = typeName.Split('.');
        return parts.Length > 1 ? parts[^2] : typeName;
    }

    private static bool IsValidTemplate(string template)
    {
        // Basic template validation - check for balanced braces
        var openBraces = template.Count(c => c == '{');
        var closeBraces = template.Count(c => c == '}');
        return openBraces == closeBraces && openBraces > 0;
    }

    private static bool ValidateTemplate(string template, FormattingContext context)
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
