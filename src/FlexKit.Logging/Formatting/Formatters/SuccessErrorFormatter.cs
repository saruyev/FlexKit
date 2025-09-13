using FlexKit.Logging.Configuration;
using FlexKit.Logging.Formatting.Core;
using FlexKit.Logging.Formatting.Models;
using FlexKit.Logging.Formatting.Translation;
using FlexKit.Logging.Formatting.Utils;
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
internal sealed class SuccessErrorFormatter(IMessageTranslator translator) : IMessageFormatter
{
    /// <summary>
    /// Provides functionality to translate message templates and parameters
    /// during the formatting process.
    /// </summary>
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
            var template = context.GetTemplate(FormatterType);
            var parameters = context.DisableFormatting
                ? context.ExtractParameters()
                : context.Stringify().ExtractParameters();

            var translatedTemplate = _translator.TranslateTemplate(template);
            var translatedParameters =
                _translator.TranslateParameters(parameters, template);

            return context.DisableFormatting
                ? FormattedMessage.Success(translatedTemplate, translatedParameters)
                : FormattedMessage.Success(FormatMessage(translatedTemplate, translatedParameters));
        }
        catch (Exception ex)
        {
            return FormattedMessage.Failure($"SuccessError formatting failed: {ex.Message}");
        }
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
