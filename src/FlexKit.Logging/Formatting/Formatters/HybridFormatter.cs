using System.Diagnostics.CodeAnalysis;
using FlexKit.Logging.Configuration;
using FlexKit.Logging.Formatting.Core;
using FlexKit.Logging.Formatting.Models;
using FlexKit.Logging.Formatting.Translation;
using System.Text.Json;
using FlexKit.Logging.Models;
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
                ? GetMetadataPart(context, hybridSettings)
                : string.Empty;

            var separator = string.IsNullOrEmpty(metadataPart) ? string.Empty : hybridSettings.MetadataSeparator;
            var hybridMessage = messageResult.Message + separator + metadataPart;

            return FormattedMessage.Success(hybridMessage);
        }
        catch (Exception ex)
        {
            return FormattedMessage.Failure($"Hybrid formatting failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public bool CanFormat(FormattingContext context) =>
        _messageFormatter.CanFormat(context) && _jsonFormatter.CanFormat(context);

    private FormattedMessage GetMessagePart(FormattingContext context, HybridFormatterSettings settings)
    {
        if (string.IsNullOrEmpty(settings.MessageTemplate))
        {
            return _messageFormatter.Format(context);
        }

        // Add the custom template to context temporarily
        var tempTemplates = new Dictionary<string, TemplateConfig>(context.Configuration.Templates)
        {
            ["Hybrid"] = new()
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

    private string GetMetadataPart(FormattingContext context, HybridFormatterSettings settings)
    {
        var metadata = new Dictionary<string, object?>();
        var entry = context.LogEntry;

        AddDurationMetadata(metadata, entry, settings);
        AddThreadMetadata(metadata, entry, settings);
        AddExceptionMetadata(metadata, entry, settings);
        AddTimestampMetadata(metadata, entry, settings);
        AddSuccessMetadata(metadata, entry, settings);
        AddContextProperties(metadata, context, settings);

        return metadata.Count > 0 ? JsonSerializer.Serialize(metadata, _options) : string.Empty;
    }

    private static void AddDurationMetadata(
        Dictionary<string, object?> metadata,
        in LogEntry entry,
        HybridFormatterSettings settings)
    {
        var includeProperty = settings.MetadataFields.Count != 0 && !settings.MetadataFields.Contains("duration");
        if (includeProperty || !entry.DurationTicks.HasValue)
        {
            return;
        }

        var duration = TimeSpan.FromTicks(entry.DurationTicks.Value).TotalMilliseconds;
        metadata["duration"] = Math.Round(duration, 2);
    }

    private static void AddThreadMetadata(
        Dictionary<string, object?> metadata,
        in LogEntry entry,
        HybridFormatterSettings settings)
    {
        if (settings.MetadataFields.Count == 0 || settings.MetadataFields.Contains("thread_id"))
        {
            metadata["thread_id"] = entry.ThreadId;
        }

        if ((settings.MetadataFields.Count != 0 && !settings.MetadataFields.Contains("activity_id"))
            || string.IsNullOrEmpty(entry.ActivityId))
        {
            return;
        }

        metadata["activity_id"] = entry.ActivityId;
    }

    private static void AddExceptionMetadata(
        Dictionary<string, object?> metadata,
        in LogEntry entry,
        HybridFormatterSettings settings)
    {
        if ((settings.MetadataFields.Count != 0 && !settings.MetadataFields.Contains("exception_type"))
            || entry.Success)
        {
            return;
        }

        metadata["exception_type"] = entry.ExceptionType;
    }

    private static void AddTimestampMetadata(
        Dictionary<string, object?> metadata,
        in LogEntry entry,
        HybridFormatterSettings settings)
    {
        if (settings.MetadataFields.Count != 0 && !settings.MetadataFields.Contains("timestamp"))
        {
            return;
        }

        metadata["timestamp"] = new DateTimeOffset(entry.TimestampTicks, TimeSpan.Zero).ToString("O");
    }

    private static void AddSuccessMetadata(
        Dictionary<string, object?> metadata,
        in LogEntry entry,
        HybridFormatterSettings settings)
    {
        if (settings.MetadataFields.Count != 0 && !settings.MetadataFields.Contains("success"))
        {
            return;
        }

        metadata["success"] = entry.Success;
    }

    private static void AddContextProperties(
        Dictionary<string, object?> metadata,
        in FormattingContext context,
        HybridFormatterSettings settings)
    {
        var propertiesToAdd = settings.MetadataFields.Count == 0
            ? context.Properties
            : context.Properties.Where(p => settings.MetadataFields.Contains(p.Key));

        foreach (var property in propertiesToAdd)
        {
            metadata[property.Key] = property.Value;
        }
    }
}
