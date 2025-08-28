using System.Diagnostics.CodeAnalysis;
using FlexKit.Logging.Configuration;
using FlexKit.Logging.Core;
using FlexKit.Logging.Formatting.Core;
using FlexKit.Logging.Formatting.Models;
using FlexKit.Logging.Models;
using JetBrains.Annotations;
using NLog;
using ILogger = NLog.ILogger;
using LogLevel = NLog.LogLevel;

namespace FlexKit.Logging.NLog.Core;

/// <summary>
/// Processes log entries by applying formatting and routing to NLog targets.
/// Handles the message formatting pipeline, fallback logic, and output routing using NLog's infrastructure.
/// </summary>
/// <remarks>
/// Initializes a new instance of the NLogLogWriter.
/// </remarks>
/// <param name="loggingConfig">Logging configuration.</param>
/// <param name="formatterFactory">Message formatter factory.</param>
/// <param name="nlogLogger">Configured NLog logger instance.</param>
[UsedImplicitly]
public sealed class NLogLogWriter(
    LoggingConfig loggingConfig,
    IMessageFormatterFactory formatterFactory,
    ILogger nlogLogger) : ILogEntryProcessor
{
    /// <inheritdoc />
    public LoggingConfig Config { get; } = loggingConfig ?? throw new ArgumentNullException(nameof(loggingConfig));

    /// <inheritdoc />
    public void ProcessEntry(LogEntry entry)
    {
        try
        {
            OutputMessage(
                FormatLogEntry(entry),
                ConvertLogLevel(entry.ExceptionMessage == null ? entry.Level : entry.ExceptionLevel),
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
    private FormattedMessage FormatLogEntry(in LogEntry entry)
    {
        var context = FormattingContext.Create(entry, Config).WithoutFormatting();

        if (!string.IsNullOrEmpty(entry.TemplateName))
        {
            context = context.WithTemplateName(entry.TemplateName);
        }

        var formatter = formatterFactory.GetFormatter(context);
        var result = formatter.Format(context);

        LogFallbackUsageIfNeeded(entry.Id, result);
        return result;
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

        nlogLogger.Debug(
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
        OutputMessage(
            FormattedMessage.Failure($"[Error] Method {entry.TypeName}.{entry.MethodName} - Success: {entry.Success}"),
            ConvertLogLevel(entry.ExceptionLevel),
            entry.Target ?? Config.DefaultTarget ?? entry.TypeName);

        nlogLogger.Warn(
            ex,
            "Failed to process log entry {EntryId} for method {TypeName}.{MethodName}",
            entry.Id,
            entry.TypeName,
            entry.MethodName);
    }

    /// <summary>
    /// Outputs the formatted message to the configured NLog targets.
    /// Uses NLog's structured logging capabilities to pass parameters as event properties.
    /// </summary>
    /// <param name="message">The formatted message to output.</param>
    /// <param name="level">The NLog log level for this message.</param>
    /// <param name="typeName">The type name to use as a logger category.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the log level is invalid.</exception>
    [SuppressMessage("ReSharper", "FlagArgument")]
    private void OutputMessage(
        FormattedMessage message,
        LogLevel level,
        string typeName)
    {
        if (!nlogLogger.IsEnabled(level))
        {
            return;
        }

        // Create log event info for structured logging
        var logEventInfo = new LogEventInfo(level, nlogLogger.Name, message.Template)
        {
            Exception = null,
            Properties = { ["Target"] = typeName },
            Parameters = message.Parameters.Values.ToArray()
        };

        // Log the event
        nlogLogger.Log(logEventInfo);
    }

    /// <summary>
    /// Converts a Microsoft.Extensions.Logging.LogLevel to an NLog.LogLevel.
    /// </summary>
    /// <param name="level">The log level defined by Microsoft.Extensions.Logging.LogLevel.</param>
    /// <returns>The corresponding NLog.LogLevel.</returns>
    private static LogLevel ConvertLogLevel(Microsoft.Extensions.Logging.LogLevel level) =>
        level switch
        {
            Microsoft.Extensions.Logging.LogLevel.Trace => LogLevel.Trace,
            Microsoft.Extensions.Logging.LogLevel.Debug => LogLevel.Debug,
            Microsoft.Extensions.Logging.LogLevel.Information => LogLevel.Info,
            Microsoft.Extensions.Logging.LogLevel.Warning => LogLevel.Warn,
            Microsoft.Extensions.Logging.LogLevel.Error => LogLevel.Error,
            Microsoft.Extensions.Logging.LogLevel.Critical => LogLevel.Fatal,
            Microsoft.Extensions.Logging.LogLevel.None => LogLevel.Off,
            _ => LogLevel.Info
        };
}
