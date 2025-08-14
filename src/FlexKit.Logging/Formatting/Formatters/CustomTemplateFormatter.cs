using System.Diagnostics.CodeAnalysis;
using FlexKit.Logging.Configuration;
using FlexKit.Logging.Formatting.Core;
using FlexKit.Logging.Formatting.Models;
using FlexKit.Logging.Formatting.Translation;

namespace FlexKit.Logging.Formatting.Formatters;

/// <summary>
/// Formats log entries using custom templates configured per service or context.
/// Allows templates like "Processing payment of {Amount} for {Customer}".
/// </summary>
public sealed class CustomTemplateFormatter : IMessageFormatter
{
    private readonly IMessageTranslator _translator;

    /// <summary>
    /// Initializes a new instance of the CustomTemplateFormatter.
    /// </summary>
    /// <param name="translator">The message translator for provider-specific syntax conversion.</param>
    public CustomTemplateFormatter(IMessageTranslator translator)
    {
        _translator = translator ?? throw new ArgumentNullException(nameof(translator));
    }

    /// <inheritdoc />
    public FormatterType FormatterType => FormatterType.CustomTemplate;

    /// <inheritdoc />
    public FormattedMessage Format(FormattingContext context)
    {
        try
        {
            var template = GetCustomTemplate(context);
            if (string.IsNullOrEmpty(template))
            {
                return FormattedMessage.Failure("No custom template found for context");
            }

            var parameters = ExtractParameters(context);
            var translatedTemplate = _translator.TranslateTemplate(template);
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
        var template = GetCustomTemplate(context);
        return !string.IsNullOrEmpty(template) &&
               _translator.CanTranslate(template, ExtractParameters(context));
    }

    private static string? GetCustomTemplate(FormattingContext context)
    {
        // First, try to get template by explicit name
        if (!string.IsNullOrEmpty(context.TemplateName) &&
            context.Configuration.Templates.TryGetValue(context.TemplateName, out var namedTemplate))
        {
            return context.LogEntry.Success ? namedTemplate.SuccessTemplate : namedTemplate.ErrorTemplate;
        }

        // Try to get a template by type name
        var typeName = context.LogEntry.TypeName;
        if (context.Configuration.Templates.TryGetValue(typeName, out var typeTemplate))
        {
            return context.LogEntry.Success ? typeTemplate.SuccessTemplate : typeTemplate.ErrorTemplate;
        }

        // Try to get a template by method name
        var methodName = context.LogEntry.MethodName;
        if (context.Configuration.Templates.TryGetValue(methodName, out var methodTemplate))
        {
            return context.LogEntry.Success ? methodTemplate.SuccessTemplate : methodTemplate.ErrorTemplate;
        }

        // Check for default template
        if (!context.Configuration.Templates.TryGetValue("Default", out var defaultTemplate))
        {
            return null;
        }

        return context.LogEntry.Success ? defaultTemplate.SuccessTemplate : defaultTemplate.ErrorTemplate;
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
}
