using System.Collections.Concurrent;
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
/// <param name="loggerFactory">The logger factory to create category-specific loggers.</param>
[SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates",
    Justification = "Used only for warnings and exceptions, no real performance impact")]
[UsedImplicitly]
internal sealed class FormattedLogWriter(
    LoggingConfig loggingConfig,
    IMessageFormatterFactory formatterFactory,
    ILoggerFactory loggerFactory) : ILogEntryProcessor
{
    /// <summary>
    /// Provides an abstraction for creating instances of message formatters to handle
    /// the formatting of log entries based on the provided context.
    /// </summary>
    private readonly IMessageFormatterFactory _formatterFactory =
        formatterFactory ?? throw new ArgumentNullException(nameof(formatterFactory));

    /// <summary>
    /// Supplies functionality for creating logger instances that are scoped by category,
    /// enabling custom logging behavior per category within the application.
    /// </summary>
    private readonly ILoggerFactory _loggerFactory =
        loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));

    /// <summary>
    /// A thread-safe dictionary used to cache and retrieve instances of loggers
    /// associated with specific type names, optimizing logger creation and reuse.
    /// </summary>
    private readonly ConcurrentDictionary<string, ILogger> _loggerCache = new();

    /// <inheritdoc />
    public LoggingConfig Config { get; } = loggingConfig ?? throw new ArgumentNullException(nameof(loggingConfig));

    /// <inheritdoc />
    public void ProcessEntry(LogEntry entry)
    {
        try
        {
            OutputMessage(
                FormatLogEntry(entry),
                entry.ExceptionMessage == null ? entry.Level : entry.ExceptionLevel,
                entry.Target ?? Config.DefaultTarget ?? entry.TypeName);
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

        if (!string.IsNullOrEmpty(entry.TemplateName))
        {
            context = context.WithTemplateName(entry.TemplateName);
        }

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
            GetLoggerForType(nameof(FormattedLogWriter)).LogWarning(
                "Message formatting failed for entry {EntryId}, used fallback: {ErrorMessage}",
                entry.Id,
                result.ErrorMessage);
            return FormatFallbackMessage(entry);
        }

        GetLoggerForType(nameof(FormattedLogWriter)).LogError(
            "Message formatting failed for entry {EntryId}: {ErrorMessage}",
            entry.Id,
            result.ErrorMessage);
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

        GetLoggerForType(nameof(FormattedLogWriter)).LogDebug(
            "Used fallback formatting for entry {EntryId}: {ErrorMessage}",
            entryId,
            result.ErrorMessage);
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
        OutputMessage(
            safeMessage,
            entry.ExceptionLevel,
            entry.Target ?? Config.DefaultTarget ?? entry.TypeName);

        GetLoggerForType(nameof(FormattedLogWriter)).LogWarning(
            ex,
            "Failed to process log entry {EntryId} for method {TypeName}.{MethodName}",
            entry.Id,
            entry.TypeName,
            entry.MethodName);
    }

    /// <summary>
    /// Outputs the formatted message to the configured destination.
    /// For now, includes the log level in the console output for testing purposes.
    /// </summary>
    /// <param name="message">The formatted message to output.</param>
    /// <param name="level">The log level for this message.</param>
    /// <param name="typeName">The type name to create a logger for.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the log level is invalid.</exception>
    [SuppressMessage("ReSharper", "FlagArgument")]
    private void OutputMessage(
        string message,
        LogLevel level,
        string typeName)
    {
        var logger = GetLoggerForType(typeName);

        if (!logger.IsEnabled(level))
        {
            return;
        }

        switch (level)
        {
            case LogLevel.Trace:
                LogWriter.LogTrace(logger, message, null);
                break;
            case LogLevel.Debug:
                LogWriter.LogDebug(logger, message, null);
                break;
            case LogLevel.Information:
                LogWriter.LogInfo(logger, message, null);
                break;
            case LogLevel.Warning:
                LogWriter.LogWarning(logger, message, null);
                break;
            case LogLevel.Error:
                LogWriter.LogError(logger, message, null);
                break;
            case LogLevel.Critical:
                LogWriter.LogCritical(logger, message, null);
                break;
            case LogLevel.None:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(level), level, null);
        }
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
        var inputParameters = entry.InputParameters?.ToString();

        if (!string.IsNullOrEmpty(inputParameters))
        {
            result = result.Replace(
                "{InputParameters}",
                inputParameters,
                StringComparison.OrdinalIgnoreCase);
        }

        return result.Replace(
            "{OutputValue}",
            entry.OutputValue?.ToString() ?? string.Empty,
            StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets or creates a logger for the specified type name.
    /// Caches loggers to avoid repeated factory calls.
    /// </summary>
    /// <param name="typeName">The type name to create a logger for.</param>
    /// <returns>A logger instance for the specified type.</returns>
    private ILogger GetLoggerForType(string typeName) =>
        _loggerCache.GetOrAdd(typeName, _loggerFactory.CreateLogger);
}
