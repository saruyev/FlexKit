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
/// <remarks>
/// Initializes a new instance of the LogEntryProcessor.
/// </remarks>
/// <param name="loggingConfig">Logging configuration.</param>
/// <param name="formatterFactory">Message formatter factory.</param>
/// <param name="logger">Logger for the interceptor itself.</param>
[SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates")]
[UsedImplicitly]
public sealed class FormattedLogWriter(
    LoggingConfig loggingConfig,
    IMessageFormatterFactory formatterFactory,
    ILogger<FormattedLogWriter> logger) : ILogEntryProcessor
{
    private readonly IMessageFormatterFactory _formatterFactory = formatterFactory ?? throw new ArgumentNullException(nameof(formatterFactory));
    private readonly ILogger<FormattedLogWriter> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public LoggingConfig Config { get; } = loggingConfig ?? throw new ArgumentNullException(nameof(loggingConfig));

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
    /// <param name="entry">The log entry to format.</param>
    /// <returns>The formatted message.</returns>
    private string FormatLogEntry(in LogEntry entry)
    {
        var context = FormattingContext.Create(entry, Config);
        var formatter = _formatterFactory.GetFormatter(context);
        var result = formatter.Format(context);

        if (!result.IsSuccess)
        {
            return HandleFormattingFailure(entry, result);
        }

        LogFallbackUsageIfNeeded(entry.Id, result);
        return result.Message;
    }

    /// <summary>
    /// Handles formatting failure by applying fallback logic or returning an error message.
    /// </summary>
    /// <param name="entry">The log entry that failed to format.</param>
    /// <param name="result">The formatting result.</param>
    /// <returns>The formatted message.</returns>
    private string HandleFormattingFailure(
        in LogEntry entry,
        in FormattedMessage result)
    {
        if (Config.EnableFallbackFormatting)
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
    /// <param name="entryId">The log entry ID.</param>
    /// <param name="result">The formatting result.</param>
    private void LogFallbackUsageIfNeeded(
        in Guid entryId,
        in FormattedMessage result)
    {
        if (!result.IsFallback)
        {
            return;
        }

        _logger.LogDebug("Used fallback formatting for entry {EntryId}: {ErrorMessage}",
            entryId, result.ErrorMessage);
    }

    /// <summary>
    /// Handles unexpected processing errors.
    /// </summary>
    /// <param name="entry">The log entry that failed to process.</param>
    /// <param name="ex">The exception that was thrown.</param>
    private void HandleProcessingError(
        in LogEntry entry,
        Exception ex)
    {
        var safeMessage = $"[Error] Method {entry.TypeName}.{entry.MethodName} - Success: {entry.Success}";
        OutputMessage(safeMessage);

        _logger.LogWarning(ex, "Failed to process log entry {EntryId} for method {TypeName}.{MethodName}",
            entry.Id, entry.TypeName, entry.MethodName);
    }

    /// <summary>
    /// Outputs the formatted message to the configured destination.
    /// </summary>
    /// <param name="message">The formatted message to output.</param>
    private static void OutputMessage(string message)
    {
        Console.WriteLine(message);
    }

    /// <summary>
    /// Creates a fallback-formatted message when primary formatting fails.
    /// </summary>
    /// <param name="entry">The log entry that failed to format.</param>
    /// <returns>The formatted message.</returns>
    private string FormatFallbackMessage(in LogEntry entry)
    {
        var template = Config.FallbackTemplate;

        var result = template
            .Replace("{TypeName}", entry.TypeName, StringComparison.OrdinalIgnoreCase)
            .Replace("{MethodName}", entry.MethodName, StringComparison.OrdinalIgnoreCase)
            .Replace("{Success}", entry.Success.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("{Id}", entry.Id.ToString(), StringComparison.OrdinalIgnoreCase);

        // Add InputParameters and OutputValue to fallback
        if (!string.IsNullOrEmpty(entry.InputParameters))
        {
            result = result.Replace("{InputParameters}", entry.InputParameters, StringComparison.OrdinalIgnoreCase);
        }

        if (!string.IsNullOrEmpty(entry.OutputValue))
        {
            result = result.Replace("{OutputValue}", entry.OutputValue, StringComparison.OrdinalIgnoreCase);
        }

        return result;
    }
}
