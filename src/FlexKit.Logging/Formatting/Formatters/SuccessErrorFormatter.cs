using System.Diagnostics.CodeAnalysis;
using FlexKit.Logging.Configuration;
using FlexKit.Logging.Formatting.Core;
using FlexKit.Logging.Formatting.Models;
using FlexKit.Logging.Formatting.Translation;
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
    private readonly IMessageTranslator _translator = translator ?? throw new ArgumentNullException(nameof(translator));

    /// <inheritdoc />
    public FormatterType FormatterType => FormatterType.SuccessError;

    /// <inheritdoc />
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

    /// <inheritdoc />
    public bool CanFormat(FormattingContext context)
    {
        var template = GetTemplate(context.LogEntry, context);
        var parameters = ExtractParameters(context.LogEntry);
        return _translator.CanTranslate(template, parameters);
    }

    private static string GetTemplate(LogEntry entry, FormattingContext context)
    {
        // First try to get from TemplateConfig
        if (context.Configuration.Templates.TryGetValue("SuccessError", out var templateConfig)
            && templateConfig.Enabled && templateConfig.IsValid())
        {
            var configuredTemplate = templateConfig.GetTemplateForOutcome(entry.Success);
            if (!string.IsNullOrEmpty(configuredTemplate))
            {
                return configuredTemplate;
            }
        }

        // Fall back to hardcoded templates
        if (entry.Success)
        {
            return entry.DurationTicks.HasValue
                ? "✅ Method {MethodName} completed successfully in {Duration}ms"
                : "✅ Method {MethodName} started successfully";
        }

        return entry.DurationTicks.HasValue
            ? "❌ Method {MethodName} failed: {ExceptionType} - {ExceptionMessage} (after {Duration}ms)"
            : "❌ Method {MethodName} failed: {ExceptionType} - {ExceptionMessage}";
    }

    [SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance")]
    private static IReadOnlyDictionary<string, object?> ExtractParameters(LogEntry entry)
    {
        var parameters = new Dictionary<string, object?>
        {
            ["MethodName"] = entry.MethodName,
            ["TypeName"] = entry.TypeName,
            ["Success"] = entry.Success,
            ["ThreadId"] = entry.ThreadId
        };

        if (entry.DurationTicks.HasValue)
        {
            var duration = TimeSpan.FromTicks(entry.DurationTicks.Value).TotalMilliseconds;
            parameters["Duration"] = Math.Round(duration, 2);
        }

        if (!entry.Success)
        {
            parameters["ExceptionType"] = entry.ExceptionType ?? "UnknownException";
            parameters["ExceptionMessage"] = entry.ExceptionMessage ?? "No exception message available";
        }

        if (!string.IsNullOrEmpty(entry.ActivityId))
        {
            parameters["ActivityId"] = entry.ActivityId;
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
