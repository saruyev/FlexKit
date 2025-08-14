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
    private readonly IMessageFormatter _jsonFormatter;
    private readonly IMessageTranslator _translator;
    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = false
    };

    /// <summary>
    /// Initializes a new instance of the HybridFormatter.
    /// </summary>
    /// <param name="translator">The message translator for provider-specific syntax conversion.</param>
    public HybridFormatter(IMessageTranslator translator)
    {
        ArgumentNullException.ThrowIfNull(translator);

        _messageFormatter = new StandardStructuredFormatter(translator);
        _jsonFormatter = new JsonFormatter();
        _translator = translator;
    }

    /// <inheritdoc />
    public FormatterType FormatterType => FormatterType.Hybrid;

    /// <inheritdoc />
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
            var metadataPart = hybridSettings.IncludeMetadata
                ? GetMetadataPart(context)
                : string.Empty;

            var separator = string.IsNullOrEmpty(metadataPart) ? string.Empty : " | ";
            var hybridMessage = messageResult.Message + separator + metadataPart;

            return FormattedMessage.Success(hybridMessage);
        }
        catch (Exception ex)
        {
            return FormattedMessage.Failure($"Hybrid formatting failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public bool CanFormat(FormattingContext context)
    {
        return _messageFormatter.CanFormat(context) && _jsonFormatter.CanFormat(context);
    }

    private FormattedMessage GetMessagePart(FormattingContext context, HybridFormatterSettings settings)
    {
        if (!string.IsNullOrEmpty(settings.MessageTemplate))
        {
            // Add the custom template to context temporarily
            var tempTemplates = new Dictionary<string, TemplateConfig>(context.Configuration.Templates)
            {
                ["Hybrid"] = new TemplateConfig
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
                tempConfig).WithFormatterType(FormatterType.CustomTemplate)
                .WithTemplateName("Hybrid").WithProperties(context.Properties);

            var customFormatter = new CustomTemplateFormatter(_translator);
            return customFormatter.Format(tempFormattingContext);
        }

        // Use a standard structured formatter
        return _messageFormatter.Format(context);
    }

    private string GetMetadataPart(FormattingContext context)
    {
        var metadata = new Dictionary<string, object?>();

        var entry = context.LogEntry;

        if (entry.DurationTicks.HasValue)
        {
            var duration = TimeSpan.FromTicks(entry.DurationTicks.Value).TotalMilliseconds;
            metadata["duration"] = Math.Round(duration, 2);
        }

        metadata["thread_id"] = entry.ThreadId;

        if (!string.IsNullOrEmpty(entry.ActivityId))
        {
            metadata["activity_id"] = entry.ActivityId;
        }

        if (!entry.Success)
        {
            metadata["exception_type"] = entry.ExceptionType;
        }

        // Add any additional properties
        foreach (var property in context.Properties)
        {
            metadata[property.Key] = property.Value;
        }

        return JsonSerializer.Serialize(metadata, _options);
    }
}
