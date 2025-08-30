using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using FlexKit.Logging.Configuration;
using FlexKit.Logging.Core;
using FlexKit.Logging.Formatting.Core;
using FlexKit.Logging.Formatting.Models;
using FlexKit.Logging.Models;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace FlexKit.Logging.ZLogger.Core;

/// <summary>
/// Processes log entries by applying formatting and routing to ZLogger providers.
/// Handles the message formatting pipeline, fallback logic, and output routing using ZLogger's high-performance infrastructure.
/// </summary>
/// <remarks>
/// <para>
/// This implementation leverages ZLogger's high-performance logging capabilities while integrating with
/// FlexKit's formatting pipeline. ZLogger's native string interpolation and UTF8 processing provide
/// superior performance compared to traditional logging frameworks.
/// </para>
/// <para>
/// <strong>ZLogger Integration Benefits:</strong>
/// <list type="bullet">
/// <item>Zero-allocation UTF8 logging for maximum performance</item>
/// <item>Compile-time string interpolation optimization</item>
/// <item>Native async background processing without additional queuing</item>
/// <item>Built-in structured logging with JSON serialization support</item>
/// <item>Multiple provider support (Console, File, RollingFile, Stream, InMemory, LogProcessor)</item>
/// </list>
/// </para>
/// <para>
/// <strong>Performance Considerations:</strong>
/// ZLogger handles its own async processing and buffering internally, so FlexKit's background
/// processing is bypassed in favor of ZLogger's native optimizations. This provides better
/// throughput and lower memory allocation compared to channel-based queuing.
/// </para>
/// </remarks>
/// <remarks>
/// Processes log entries by applying formatting and routing to ZLogger providers.
/// Handles the message formatting pipeline, fallback logic, and output routing using ZLogger's high-performance infrastructure.
/// </remarks>
/// <remarks>
/// <para>
/// This implementation leverages ZLogger's high-performance logging capabilities while integrating with
/// FlexKit's formatting pipeline. ZLogger's native string interpolation and UTF8 processing provide
/// superior performance compared to traditional logging frameworks.
/// </para>
/// <para>
/// <strong>ZLogger Integration Benefits:</strong>
/// <list type="bullet">
/// <item>Zero-allocation UTF8 logging for maximum performance</item>
/// <item>Compile-time string interpolation optimization</item>
/// <item>Native async background processing without additional queuing</item>
/// <item>Built-in structured logging with JSON serialization support</item>
/// <item>Multiple provider support (Console, File, RollingFile, Stream, InMemory, LogProcessor)</item>
/// </list>
/// </para>
/// <para>
/// <strong>Performance Considerations:</strong>
/// ZLogger handles its own async processing and buffering internally, so FlexKit's background
/// processing is bypassed in favor of ZLogger's native optimizations. This provides better
/// throughput and lower memory allocation compared to channel-based queuing.
/// </para>
/// </remarks>
/// <param name="loggingConfig">Logging configuration.</param>
/// <param name="formatterFactory">Message formatter factory.</param>
/// <param name="loggerFactory">ZLogger-configured logger factory.</param>
/// <param name="engine">ZLogger template engine.</param>
[UsedImplicitly]
[SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates")]
public sealed class ZLoggerLogWriter(
    LoggingConfig loggingConfig,
    IMessageFormatterFactory formatterFactory,
    ILoggerFactory loggerFactory,
    IZLoggerTemplateEngine engine) : ILogEntryProcessor
{
    private readonly IMessageFormatterFactory _formatterFactory = formatterFactory ?? throw new ArgumentNullException(nameof(formatterFactory));
    private readonly ILoggerFactory _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
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
                entry.Target ?? Config.DefaultTarget ?? "Console");
        }
        catch (Exception ex)
        {
            HandleProcessingError(entry, ex);
        }
    }

    /// <summary>
    /// Formats a log entry using the configured formatting pipeline.
    /// For ZLogger, we can choose between string-based formatting or structured logging
    /// based on the configuration and formatter type.
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

        var formatter = _formatterFactory.GetFormatter(context);
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

        GetLoggerForCategory("FlexKit.Logging.ZLogger").LogDebug(
            "Used fallback formatting for entry {EntryId}: {ErrorMessage}",
            entryId,
            result.ErrorMessage);
    }

    /// <summary>
    /// Outputs the formatted message using ZLogger's high-performance logging infrastructure.
    /// Routes to the appropriate logger based on the target category.
    /// </summary>
    /// <param name="message">The formatted log message.</param>
    /// <param name="level">The log level.</param>
    /// <param name="category">The target category (used for routing to specific providers).</param>
    [SuppressMessage("ReSharper", "FlagArgument")]
    private void OutputMessage(
        in FormattedMessage message,
        LogLevel level,
        string category)
    {
        var logger = GetLoggerForCategory(category);

        if (!logger.IsEnabled(level))
        {
            return;
        }

        if (message.IsSuccess && !string.IsNullOrEmpty(message.Template))
        {
            engine.ExecuteTemplate(logger, message, level);
        }
        else
        {
            // Fallback to simple string logging
            logger.Log(level, "{Message}", message.Message);
        }
    }

    /// <summary>
    /// Gets or creates a cached logger for the specified category.
    /// Categories are used to route messages to specific ZLogger providers.
    /// </summary>
    /// <param name="category">The logger category (typically the target type like "Console", "File", etc.).</param>
    /// <returns>A cached logger instance for the category.</returns>
    private ILogger GetLoggerForCategory(string category) =>
        _loggerCache.GetOrAdd(category, _loggerFactory.CreateLogger);

    /// <summary>
    /// Handles unexpected processing errors during log entry processing.
    /// </summary>
    /// <param name="entry">The log entry that caused the error.</param>
    /// <param name="exception">The exception that occurred.</param>
    private void HandleProcessingError(
        in LogEntry entry,
        Exception exception)
    {
        try
        {
            GetLoggerForCategory("FlexKit.Logging.ZLogger").LogError(exception,
                "Error processing log entry {EntryId} from {TypeName}.{MethodName}",
                entry.Id,
                entry.TypeName,
                entry.MethodName);
        }
        catch
        {
            // ZLogger's internal error handling will take over
        }
    }
}
