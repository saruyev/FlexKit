using FlexKit.Logging.Configuration;
using Microsoft.Extensions.Logging;
using Serilog.Events;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using ISerilogLogger = Serilog.ILogger;

namespace FlexKit.Logging.Serilog.Core;

/// <summary>
/// Logger implementation that bridges Microsoft.Extensions.Logging to Serilog.
/// Routes log messages to the appropriate Serilog logger based on category.
/// </summary>
/// <param name="categoryName">The category name of the logger.</param>
/// <param name="config">The FlexKit logging configuration.</param>
/// <param name="serilogLogger">The Serilog logger instance.</param>
/// <remarks>
/// Logger implementation that bridges Microsoft.Extensions.Logging to Serilog.
/// Routes log messages to the appropriate Serilog logger based on category.
/// </remarks>
public class SerilogLogger(
    string categoryName,
    LoggingConfig config,
    ISerilogLogger serilogLogger) : ILogger
{
    /// <summary>
    /// Holds the instance of the Serilog logger used for routing log messages to the appropriate Serilog sink.
    /// </summary>
    /// <remarks>
    /// This field is initialized with a Serilog logger context that enriches log messages with a "SourceContext"
    /// property based on the category name of the logger. It is used internally within the <see cref="SerilogLogger"/>
    /// implementation to write structured log events.
    /// </remarks>
    private readonly ISerilogLogger _serilogLogger =
        serilogLogger.ForContext("SourceContext", categoryName);

    /// <inheritdoc />
    public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

    /// <summary>
    /// Logs the specified information using the appropriate level of logging.
    /// Routes the log message to the Serilog logger based on the configured category and log level.
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
        var serilogLevel = ConvertLogLevel(logLevel);

        // Create the logger context with structured data
        var logger = _serilogLogger
            .ForContext("EventId", eventId.Id);

        // Add state properties if the state is a structured object
        if (state is IEnumerable<KeyValuePair<string, object?>> properties)
        {
            logger = properties
                .Where(property => property.Key != "{OriginalFormat}")
                .Aggregate(logger, (current, property) =>
                    current.ForContext(property.Key, property.Value, destructureObjects: true));
        }

        // Write the log event
#pragma warning disable CA2254
        logger.Write(serilogLevel, exception, message);
#pragma warning restore CA2254
    }

    /// <inheritdoc />
    // ReSharper disable once FlagArgument
    public bool IsEnabled(LogLevel logLevel) =>
        logLevel >= GetMinimumLogLevelForCategory() && _serilogLogger.IsEnabled(ConvertLogLevel(logLevel));

    /// <summary>
    /// Converts a <see cref="LogLevel"/> from Microsoft.Extensions.Logging to a corresponding
    /// <see cref="LogEventLevel"/> used by Serilog.
    /// </summary>
    /// <param name="logLevel">The <see cref="LogLevel"/> to convert.</param>
    /// <returns>The corresponding <see cref="LogEventLevel"/> in Serilog.</returns>
    private static LogEventLevel ConvertLogLevel(LogLevel logLevel) =>
        logLevel switch
        {
            LogLevel.Trace => LogEventLevel.Verbose,
            LogLevel.Debug => LogEventLevel.Debug,
            LogLevel.Information => LogEventLevel.Information,
            LogLevel.Warning => LogEventLevel.Warning,
            LogLevel.Error => LogEventLevel.Error,
            LogLevel.Critical => LogEventLevel.Fatal,
            LogLevel.None => LogEventLevel.Fatal, // Serilog doesn't have "Off"
            _ => LogEventLevel.Information,
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
