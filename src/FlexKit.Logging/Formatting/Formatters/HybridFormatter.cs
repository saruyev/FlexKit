using System.Diagnostics.CodeAnalysis;
using FlexKit.Logging.Configuration;
using FlexKit.Logging.Formatting.Core;
using FlexKit.Logging.Formatting.Models;
using FlexKit.Logging.Formatting.Translation;
using System.Text.Json;
using JetBrains.Annotations;

namespace FlexKit.Logging.Formatting.Formatters;

/// <summary>
/// Formats log entries using a combination of structured message and JSON metadata.
/// Produces output like "Method ProcessPayment completed | {"duration": 450, "thread_id": 12}".
/// </summary>
[SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance")]
[UsedImplicitly]
public sealed class HybridFormatter : IMessageFormatter
{
    private readonly IMessageFormatter _messageFormatter;
    private readonly IMessageTranslator _translator;

    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = false,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// Initializes a new instance of the HybridFormatter.
    /// </summary>
    /// <param name="translator">The message translator for provider-specific syntax conversion.</param>
    public HybridFormatter(IMessageTranslator translator)
    {
        ArgumentNullException.ThrowIfNull(translator);

        _messageFormatter = new StandardStructuredFormatter(translator);
        _translator = translator;
    }

    /// <inheritdoc />
    public FormatterType FormatterType => FormatterType.Hybrid;

    /// <summary>
    /// Formats a log entry by combining a structured message part with optional JSON metadata.
    /// </summary>
    /// <param name="context">The formatting context containing log entry and configuration.</param>
    /// <returns>A formatted message result combining structured text and JSON metadata.</returns>
    public FormattedMessage Format(FormattingContext context)
    {
        try
        {
            var hybridSettings = context.Configuration.Formatters.Hybrid;

            // Get the message part
            var messageResult = GetMessagePart(context, hybridSettings);
            if (!messageResult.IsSuccess)
            {
                return messageResult;
            }

            // Get the metadata part if enabled
            var metadataPart = hybridSettings.IncludeMetadata && !context.DisableFormatting
                ? GetMetadataPart(messageResult.Parameters)
                : string.Empty;

            if (context.DisableFormatting)
            {
                return FormattedMessage.Success(
                    messageResult.Template + _translator.TranslateTemplate(hybridSettings.MetadataSeparator + " {Metadata}"),
                    messageResult.Parameters);
            }

            return FormattedMessage.Success(messageResult.Message + hybridSettings.MetadataSeparator + metadataPart);
        }
        catch (Exception ex)
        {
            return FormattedMessage.Failure($"Hybrid formatting failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Generates the structured message part of the hybrid output using either a custom template or the standard formatter.
    /// </summary>
    /// <param name="context">The formatting context containing log entry and configuration.</param>
    /// <param name="settings">The hybrid formatter settings that may include a custom message template.</param>
    /// <returns>A formatted message result containing the structured message part.</returns>
    private FormattedMessage GetMessagePart(
        in FormattingContext context,
        HybridFormatterSettings settings)
    {
        if (string.IsNullOrEmpty(settings.MessageTemplate))
        {
            return _messageFormatter.Format(context);
        }

        // Add the custom template to context temporarily
        var tempTemplates = new Dictionary<string, TemplateConfig>(context.Configuration.Templates)
        {
            [nameof(FormatterType.Hybrid)] = new()
            {
                SuccessTemplate = settings.MessageTemplate,
                ErrorTemplate = settings.MessageTemplate
            }
        };

        var tempConfig = new LoggingConfig
        {
            DefaultFormatter = context.Configuration.DefaultFormatter,
            Templates = tempTemplates,
            Formatters = context.Configuration.Formatters,
            EnableFallbackFormatting = context.Configuration.EnableFallbackFormatting,
            FallbackTemplate = context.Configuration.FallbackTemplate
        };

        var tempFormattingContext = FormattingContext.Create(
                context.LogEntry,
                tempConfig).WithFormatterType(FormatterType.Hybrid)
            .WithTemplateName(context.TemplateName ?? context.LogEntry.TemplateName ?? nameof(FormatterType.Hybrid))
            .WithProperties(context.Properties);

        if (context.DisableFormatting)
        {
            tempFormattingContext = tempFormattingContext.WithoutFormatting();
        }

        var customFormatter = new CustomTemplateFormatter(_translator);
        return customFormatter.Format(tempFormattingContext);
    }

    /// <summary>
    /// Generates the JSON metadata part of the hybrid output containing additional log entry details.
    /// </summary>
    /// <returns>A JSON string containing the metadata, or an empty string if no metadata is available.</returns>
    private string GetMetadataPart(IReadOnlyDictionary<string, object?> parameters) =>
        parameters.Count > 0 ? JsonSerializer.Serialize(parameters, _options) : string.Empty;
}
