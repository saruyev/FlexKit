using FlexKit.Logging.Configuration;
using log4net.Core;
using log4net.Repository;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace FlexKit.Logging.Log4Net.Core;

/// <summary>
/// Logger implementation that bridges Microsoft.Extensions.Logging to Log4Net.
/// Routes log messages to the appropriate Log4Net logger based on category.
/// </summary>
/// <param name="categoryName">The category name of the logger.</param>
/// <param name="repository">The Log4Net repository to configure.</param>
/// <param name="config">The FlexKit logging configuration.</param>
/// <remarks>
/// Logger implementation that bridges Microsoft.Extensions.Logging to Log4Net.
/// Routes log messages to the appropriate Log4Net logger based on category.
/// </remarks>
public class Log4NetLogger(
    string categoryName,
    ILoggerRepository repository,
    LoggingConfig config) : ILogger
{
    /// <summary>
    /// Represents the underlying core logger instance used to route log messages
    /// to the Log4Net logging framework.
    /// </summary>
    /// <remarks>
    /// The <c>_logger</c> field is an instance of <see cref="log4net.Core.ILogger"/>
    /// initialized with a specified category name from the Log4Net repository.
    /// It serves as the internal mechanism for emitting log events to Log4Net,
    /// bridging the functionality between Microsoft.Extensions.Logging abstractions
    /// and Log4Net's implementation.
    /// </remarks>
    private readonly log4net.Core.ILogger _logger = repository.GetLogger(categoryName);

    /// <inheritdoc />
    public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

    /// <summary>
    /// Logs the specified information using the appropriate level of logging.
    /// Routes the log message to the Log4Net logger based on the configured category and log level.
    /// </summary>
    /// <typeparam name="TState">The type of the state object to be logged.</typeparam>
    /// <param name="logLevel">The severity level of the log entry.</param>
    /// <param name="eventId">The identifier for the log event.</param>
    /// <param name="state">The state or message to log.</param>
    /// <param name="exception">An optional exception related to the log message.</param>
    /// <param name="formatter">A function to format the log message.</param>
    // ReSharper disable once TooManyArguments
    public void Log<TState>(
        // ReSharper disable once FlagArgument
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var message = formatter(state, exception);
        var level = ConvertLogLevel(logLevel);

        // Create LoggingEvent manually since we're using ILogger, not ILog
        var loggingEvent = new LoggingEvent(
            typeof(Log4NetLogger),
            _logger.Repository,
            categoryName,
            level,
            message,
            exception);
        _logger.Log(loggingEvent);
    }

    /// <inheritdoc />
    // ReSharper disable once FlagArgument
    public bool IsEnabled(LogLevel logLevel) =>
        logLevel >= GetMinimumLogLevelForCategory() && _logger.IsEnabledFor(ConvertLogLevel(logLevel));

    /// <summary>
    /// Converts a <see cref="LogLevel"/> from Microsoft.Extensions.Logging to a corresponding
    /// <see cref="Level"/> used by Log4Net.
    /// </summary>
    /// <param name="logLevel">The <see cref="LogLevel"/> to convert.</param>
    /// <returns>The corresponding <see cref="Level"/> in Log4Net.</returns>
    private static Level ConvertLogLevel(LogLevel logLevel) =>
        logLevel switch
        {
            LogLevel.Trace => Level.Trace,
            LogLevel.Debug => Level.Debug,
            LogLevel.Information => Level.Info,
            LogLevel.Warning => Level.Warn,
            LogLevel.Error => Level.Error,
            LogLevel.Critical => Level.Fatal,
            _ => Level.Off,
        };

    /// <summary>
    /// Determines the minimum log level for the specified category based on
    /// the configuration. Categories matching suppressed categories in the configuration
    /// inherit the suppressed log level, otherwise the trace level is used as default.
    /// </summary>
    /// <returns>The minimum <see cref="LogLevel"/> required for logging in the current category.</returns>
    private LogLevel GetMinimumLogLevelForCategory() =>
        config.SuppressedCategories.Any(
            c => categoryName.StartsWith(c, StringComparison.InvariantCultureIgnoreCase))
            ? config.SuppressedLogLevel
            : LogLevel.Trace;

    /// <summary>
    /// Represents a no-operation (no-op) disposable scope.
    /// Used to satisfy scope-related APIs without allocating unnecessary resources.
    /// </summary>
    /// <remarks>
    /// Provides an implementation of IDisposable that performs no operations when disposed.
    /// Typically used in logging or scenarios where scoping functionality is required but no actual
    /// scope management is needed.
    /// </remarks>
    private sealed class NullScope : IDisposable
    {
        /// <summary>
        /// Gets the singleton instance of the <see cref="NullScope"/> class.
        /// Represents a no-operation (no-op) scope that satisfies IDisposable without performing any actions.
        /// </summary>
        /// <remarks>
        /// This property provides access to a shared, singleton instance of the <see cref="NullScope"/> class.
        /// It is used in scenarios where a disposable resource is required, but no actual operations
        /// or resource management is necessary.
        /// </remarks>
        public static NullScope Instance { get; } = new();

        /// <inheritdoc />
        public void Dispose() { }
    }
}
