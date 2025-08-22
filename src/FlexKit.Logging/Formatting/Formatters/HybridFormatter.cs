using System.Diagnostics.CodeAnalysis;
using FlexKit.Logging.Configuration;
using FlexKit.Logging.Formatting.Core;
using FlexKit.Logging.Formatting.Models;
using FlexKit.Logging.Formatting.Translation;
using System.Text.Json;
using FlexKit.Logging.Formatting.Utils;
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
        _jsonFormatter = new JsonFormatter();
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

    /// <summary>
    /// Determines whether this formatter can handle the given formatting context.
    /// Requires both message and JSON formatters to be capable of formatting the context.
    /// </summary>
    /// <param name="context">The formatting context to evaluate.</param>
    /// <returns>True if both underlying formatters can handle the context; otherwise, false.</returns>
    public bool CanFormat(FormattingContext context) =>
        _messageFormatter.CanFormat(context) && _jsonFormatter.CanFormat(context);

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

    /// <summary>
    /// Generates the JSON metadata part of the hybrid output containing additional log entry details.
    /// </summary>
    /// <param name="context">The formatting context containing log entry and additional properties.</param>
    /// <param name="settings">The hybrid formatter settings that control which metadata fields to include.</param>
    /// <returns>A JSON string containing the metadata, or an empty string if no metadata is available.</returns>
    private string GetMetadataPart(
        in FormattingContext context,
        HybridFormatterSettings settings)
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

    /// <summary>
    /// Adds duration information to the metadata dictionary if available and enabled in settings.
    /// </summary>
    /// <param name="metadata">The metadata dictionary to populate.</param>
    /// <param name="entry">The log entry containing potential duration information.</param>
    /// <param name="settings">The formatter settings that control which fields to include.</param>
    private static void AddDurationMetadata(
        Dictionary<string, object?> metadata,
        in LogEntry entry,
        HybridFormatterSettings settings)
    {
        var includeProperty =
            settings.MetadataFields.Count != 0 && !settings.MetadataFields.Contains("duration");
        if (includeProperty || !entry.DurationTicks.HasValue)
        {
            return;
        }

        var duration = TimeSpan.FromTicks(entry.DurationTicks.Value).TotalMilliseconds;
        metadata["duration"] = Math.Round(duration, 2);
    }

    /// <summary>
    /// Adds thread and activity ID information to the metadata dictionary if enabled in settings.
    /// </summary>
    /// <param name="metadata">The metadata dictionary to populate.</param>
    /// <param name="entry">The log entry containing thread and activity information.</param>
    /// <param name="settings">The formatter settings that control which fields to include.</param>
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

    /// <summary>
    /// Adds exception type information to the metadata dictionary for failed operations if enabled in settings.
    /// </summary>
    /// <param name="metadata">The metadata dictionary to populate.</param>
    /// <param name="entry">The log entry that may contain exception information.</param>
    /// <param name="settings">The formatter settings that control which fields to include.</param>
    private static void AddExceptionMetadata(
        Dictionary<string, object?> metadata,
        in LogEntry entry,
        HybridFormatterSettings settings)
    {
        if (entry.Success)
        {
            return;
        }

        if (settings.MetadataFields.Count == 0 || settings.MetadataFields.Contains("exception_type"))
        {
            metadata["exception_type"] = entry.ExceptionType;
        }

        if ((settings.MetadataFields.Count != 0 && !settings.MetadataFields.Contains("stack_trace"))
            || string.IsNullOrEmpty(entry.StackTrace))
        {
            return;
        }

        metadata["stack_trace"] = entry.StackTrace;
    }

    /// <summary>
    /// Adds timestamp information to the metadata dictionary if enabled in settings.
    /// </summary>
    /// <param name="metadata">The metadata dictionary to populate.</param>
    /// <param name="entry">The log entry containing timestamp information.</param>
    /// <param name="settings">The formatter settings that control which fields to include.</param>
    private static void AddTimestampMetadata(
        Dictionary<string, object?> metadata,
        in LogEntry entry,
        HybridFormatterSettings settings)
    {
        if (settings.MetadataFields.Count != 0 && !settings.MetadataFields.Contains("timestamp"))
        {
            return;
        }

        metadata["timestamp"] = entry.Timestamp.ToString("O");
    }

    /// <summary>
    /// Adds success status information to the metadata dictionary if enabled in settings.
    /// </summary>
    /// <param name="metadata">The metadata dictionary to populate.</param>
    /// <param name="entry">The log entry containing success status information.</param>
    /// <param name="settings">The formatter settings that control which fields to include.</param>
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

    /// <summary>
    /// Adds additional context properties and parameter information to the metadata dictionary.
    /// </summary>
    /// <param name="metadata">The metadata dictionary to populate.</param>
    /// <param name="context">The formatting context containing additional properties.</param>
    /// <param name="settings">The formatter settings that control which fields to include.</param>
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

        AddParameterMetadata(metadata, context.LogEntry, settings);
    }

    /// <summary>
    /// Adds input parameters and output values to the metadata dictionary if available and enabled in settings.
    /// </summary>
    /// <param name="metadata">The metadata dictionary to populate.</param>
    /// <param name="entry">The log entry containing parameter and output information.</param>
    /// <param name="settings">The formatter settings that control which fields to include.</param>
    private static void AddParameterMetadata(
        Dictionary<string, object?> metadata,
        in LogEntry entry,
        HybridFormatterSettings settings)
    {
        if ((settings.MetadataFields.Count == 0 || settings.MetadataFields.Contains("input_parameters"))
            && !string.IsNullOrEmpty(entry.InputParameters))
        {
            metadata["input_parameters"] = JsonParameterUtils.ParseParametersAsJson(entry.InputParameters);
        }

        if ((settings.MetadataFields.Count != 0 && !settings.MetadataFields.Contains("output_value"))
            || string.IsNullOrEmpty(entry.OutputValue))
        {
            return;
        }

        metadata["output_value"] = JsonParameterUtils.ParseOutputAsJson(entry.OutputValue);
    }
}
