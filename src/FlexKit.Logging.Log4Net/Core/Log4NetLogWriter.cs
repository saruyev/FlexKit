using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using FlexKit.Logging.Configuration;
using FlexKit.Logging.Core;
using FlexKit.Logging.Formatting.Core;
using FlexKit.Logging.Formatting.Models;
using FlexKit.Logging.Models;
using JetBrains.Annotations;
using log4net.Core;
using log4net.Repository;
using ILogger = log4net.Core.ILogger;

namespace FlexKit.Logging.Log4Net.Core;

/// <summary>
/// Processes log entries by applying formatting and routing to Log4Net appenders.
/// Handles the message formatting pipeline, fallback logic, and output routing using Log4Net's infrastructure.
/// </summary>
/// <remarks>
/// <para>
/// This implementation follows the MEL pattern since Log4Net, like MEL, requires serialization
/// for complex objects and works best with string-based logging. It uses Log4Net's Properties
/// dictionary for structured data when needed.
/// </para>
/// <para>
/// <strong>Key Differences from Serilog/NLog:</strong>
/// <list type="bullet">
/// <item>Uses string-based formatting (like MEL) since RequiresSerialization = true</item>
/// <item>Leverages Log4Net's Properties dictionary for additional context</item>
/// <item>Caches ILog instances by logger name for performance</item>
/// <item>Uses Log4Net's native level checking and logging methods</item>
/// </list>
/// </para>
/// </remarks>
/// <param name="loggingConfig">Logging configuration.</param>
/// <param name="formatterFactory">Message formatter factory.</param>
/// <param name="repository">Log4Net repository.</param>
[UsedImplicitly]
[SuppressMessage("Major Code Smell", "S2629:Logging templates should be constant")]
public sealed class Log4NetLogWriter(
    LoggingConfig loggingConfig,
    IMessageFormatterFactory formatterFactory,
    ILoggerRepository repository) : ILogEntryProcessor
{
    private readonly IMessageFormatterFactory _formatterFactory =
        formatterFactory ?? throw new ArgumentNullException(nameof(formatterFactory));
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
    private string FormatLogEntry(in LogEntry entry)
    {
        var context = FormattingContext.Create(entry, Config);

        if (!string.IsNullOrEmpty(entry.TemplateName))
        {
            context = context.WithTemplateName(entry.TemplateName);
        }

        var formatter = _formatterFactory.GetFormatter(context);
        var result = formatter.Format(context);

        return !result.IsSuccess ? HandleFormattingFailure(entry, result) : result.Message;
    }

    /// <summary>
    /// Handles formatting failure by applying fallback logic or returning an error message.
    /// </summary>
    /// <param name="entry">The log entry that failed to format.</param>
    /// <param name="result">The formatting result.</param>
    /// <returns>The formatted message.</returns>
    private string HandleFormattingFailure(
        in LogEntry entry,
        in FormattedMessage result) =>
        Config.EnableFallbackFormatting ? FormatFallbackMessage(entry) : $"[Formatting Error: {result.ErrorMessage}]";

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
            ConvertLogLevel(entry.ExceptionLevel),
            entry.Target ?? Config.DefaultTarget ?? entry.TypeName);

        GetLoggerForType(nameof(Log4NetLogWriter)).Log(new LoggingEvent(new LoggingEventData
        {
            LoggerName = nameof(Log4NetLogWriter),
            Level = Level.Error,
            Message = $"Failed to process log entry {entry.Id} for method {entry.TypeName}.{entry.MethodName}",
            TimeStampUtc = DateTime.UtcNow,
            ExceptionString = ex.Message
        }));
    }

    /// <summary>
    /// Outputs the formatted message to the configured Log4Net destination.
    /// </summary>
    /// <param name="message">The formatted message to output.</param>
    /// <param name="level">The log level for this message.</param>
    /// <param name="typeName">The type name to create a logger for.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the log level is invalid.</exception>
    [SuppressMessage("ReSharper", "FlagArgument")]
    private void OutputMessage(
        string message,
        Level level,
        string typeName)
    {
        var logger = GetLoggerForType(typeName);

        if (!logger.IsEnabledFor(level))
        {
            return;
        }

        var loggingEvent = new LoggingEventData
        {
            LoggerName = logger.Name,
            Level = level,
            Message = message,
            TimeStampUtc = DateTime.UtcNow
        };

        // Log the event
        logger.Log(new LoggingEvent(loggingEvent));
    }

    /// <summary>
    /// Gets or creates a Log4Net logger for the specified type name.
    /// Caches loggers to avoid repeated LogManager calls.
    /// </summary>
    /// <param name="typeName">The type name to create a logger for.</param>
    /// <returns>A Log4Net logger instance for the specified type.</returns>
    private ILogger GetLoggerForType(string typeName) =>
        _loggerCache.GetOrAdd(typeName, repository.GetLogger);

    /// <summary>
    /// Converts a Microsoft.Extensions.Logging.LogLevel to a Log4Net.Core.Level.
    /// </summary>
    /// <param name="logLevel">The MEL log level to convert.</param>
    /// <returns>The equivalent Log4Net level.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the log level is invalid.</exception>
    private static Level ConvertLogLevel(Microsoft.Extensions.Logging.LogLevel logLevel) =>
        logLevel switch
        {
            Microsoft.Extensions.Logging.LogLevel.Trace => Level.Trace,
            Microsoft.Extensions.Logging.LogLevel.Debug => Level.Debug,
            Microsoft.Extensions.Logging.LogLevel.Information => Level.Info,
            Microsoft.Extensions.Logging.LogLevel.Warning => Level.Warn,
            Microsoft.Extensions.Logging.LogLevel.Error => Level.Error,
            Microsoft.Extensions.Logging.LogLevel.Critical => Level.Fatal,
            Microsoft.Extensions.Logging.LogLevel.None => Level.Off,
            _ => throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, "Invalid log level"),
        };
}
