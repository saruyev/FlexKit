using System.Diagnostics.CodeAnalysis;
using FlexKit.Logging.Configuration;
using FlexKit.Logging.Core;
using FlexKit.Logging.Formatting.Core;
using FlexKit.Logging.Formatting.Models;
using FlexKit.Logging.Models;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Serilog.Events;
using ILogger = Serilog.ILogger;

namespace FlexKit.Logging.Serilog.Core;

/// <summary>
/// Processes log entries by applying formatting and routing to output destinations.
/// Handles the message formatting pipeline, fallback logic, and output routing.
/// </summary>
/// <remarks>
/// Initializes a new instance of the LogEntryProcessor.
/// </remarks>
/// <param name="loggingConfig">Logging configuration.</param>
/// <param name="formatterFactory">Message formatter factory.</param>
/// <param name="serilogLogger">Configured Serilog logger instance.</param>
[SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates")]
[UsedImplicitly]
public sealed class SerilogLogWriter(
    LoggingConfig loggingConfig,
    IMessageFormatterFactory formatterFactory,
    ILogger serilogLogger) : ILogEntryProcessor
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

        serilogLogger.Debug(
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

        serilogLogger.Warning(
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
        FormattedMessage message,
        LogEventLevel level,
        string typeName)
    {
        if (!serilogLogger.IsEnabled(level))
        {
            return;
        }

#pragma warning disable CA2254
        serilogLogger
            .ForContext("Target", typeName)
            .Write(level, message.Template, [.. message.Parameters.Values]);
#pragma warning restore CA2254
    }

    /// <summary>
    /// Converts a Microsoft.Extensions.Logging.LogLevel to a Serilog.Events.LogEventLevel.
    /// </summary>
    /// <param name="level">The log level defined by Microsoft.Extensions.Logging.LogLevel.</param>
    /// <returns>The corresponding Serilog.Events.LogEventLevel.</returns>
    private static LogEventLevel ConvertLogLevel(LogLevel level)
    {
        return level switch
        {
            LogLevel.Trace => LogEventLevel.Verbose,
            LogLevel.Debug => LogEventLevel.Debug,
            LogLevel.Information => LogEventLevel.Information,
            LogLevel.Warning => LogEventLevel.Warning,
            LogLevel.Error => LogEventLevel.Error,
            LogLevel.Critical => LogEventLevel.Fatal,
            LogLevel.None => LogEventLevel.Verbose,
            _ => LogEventLevel.Information
        };
    }
}
