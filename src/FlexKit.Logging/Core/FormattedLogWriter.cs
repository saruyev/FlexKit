using System.Diagnostics.CodeAnalysis;
using FlexKit.Logging.Configuration;
using FlexKit.Logging.Formatting.Core;
using FlexKit.Logging.Formatting.Models;
using FlexKit.Logging.Models;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace FlexKit.Logging.Core;

/// <summary>
/// Processes log entries by applying formatting and routing to output destinations.
/// Handles the message formatting pipeline, fallback logic, and output routing.
/// </summary>
[SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates")]
[UsedImplicitly]
public sealed class FormattedLogWriter : ILogEntryProcessor
{
    private readonly LoggingConfig _loggingConfig;
    private readonly IMessageFormatterFactory _formatterFactory;
    private readonly ILogger<FormattedLogWriter> _logger;

    /// <summary>
    /// Initializes a new instance of the LogEntryProcessor.
    /// </summary>
    public FormattedLogWriter(
        LoggingConfig loggingConfig,
        IMessageFormatterFactory formatterFactory,
        ILogger<FormattedLogWriter> logger)
    {
        _loggingConfig = loggingConfig ?? throw new ArgumentNullException(nameof(loggingConfig));
        _formatterFactory = formatterFactory ?? throw new ArgumentNullException(nameof(formatterFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public void ProcessEntry(LogEntry entry)
    {
        try
        {
            var formattedMessage = FormatLogEntry(entry);
            OutputMessage(formattedMessage);
        }
        catch (Exception ex)
        {
            HandleProcessingError(entry, ex);
        }
    }

    /// <summary>
    /// Formats a log entry using the configured formatting pipeline.
    /// </summary>
    private string FormatLogEntry(LogEntry entry)
    {
        var context = FormattingContext.Create(entry, _loggingConfig);
        var formatter = _formatterFactory.GetFormatter(context);
        var result = formatter.Format(context);

        if (result.IsSuccess)
        {
            LogFallbackUsageIfNeeded(entry.Id, result);
            return result.Message;
        }

        return HandleFormattingFailure(entry, result);
    }

    /// <summary>
    /// Handles formatting failure by applying fallback logic or returning an error message.
    /// </summary>
    private string HandleFormattingFailure(LogEntry entry, FormattedMessage result)
    {
        if (_loggingConfig.EnableFallbackFormatting)
        {
            _logger.LogWarning("Message formatting failed for entry {EntryId}, used fallback: {ErrorMessage}",
                entry.Id, result.ErrorMessage);
            return FormatFallbackMessage(entry);
        }

        _logger.LogError("Message formatting failed for entry {EntryId}: {ErrorMessage}",
            entry.Id, result.ErrorMessage);
        return $"[Formatting Error: {result.ErrorMessage}]";
    }

    /// <summary>
    /// Logs fallback usage for monitoring purposes.
    /// </summary>
    private void LogFallbackUsageIfNeeded(Guid entryId, FormattedMessage result)
    {
        if (result.IsFallback)
        {
            _logger.LogDebug("Used fallback formatting for entry {EntryId}: {ErrorMessage}",
                entryId, result.ErrorMessage);
        }
    }

    /// <summary>
    /// Handles unexpected processing errors.
    /// </summary>
    private void HandleProcessingError(LogEntry entry, Exception ex)
    {
        var safeMessage = $"[Error] Method {entry.TypeName}.{entry.MethodName} - Success: {entry.Success}";
        OutputMessage(safeMessage);

        _logger.LogWarning(ex, "Failed to process log entry {EntryId} for method {TypeName}.{MethodName}",
            entry.Id, entry.TypeName, entry.MethodName);
    }

    /// <summary>
    /// Outputs the formatted message to the configured destination.
    /// </summary>
    private static void OutputMessage(string message)
    {
        // For now, output to console - this will be configurable in future phases
        Console.WriteLine(message);
    }

    /// <summary>
    /// Creates a fallback-formatted message when primary formatting fails.
    /// </summary>
    private string FormatFallbackMessage(LogEntry entry)
    {
        var template = _loggingConfig.FallbackTemplate;

        // Simple string replacement for fallback
        return template
            .Replace("{TypeName}", entry.TypeName, StringComparison.OrdinalIgnoreCase)
            .Replace("{MethodName}", entry.MethodName, StringComparison.OrdinalIgnoreCase)
            .Replace("{Success}", entry.Success.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("{Id}", entry.Id.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}
