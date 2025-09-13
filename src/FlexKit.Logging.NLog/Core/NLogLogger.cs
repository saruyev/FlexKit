using FlexKit.Logging.Configuration;
using Microsoft.Extensions.Logging;
using NLog;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using LogLevel = NLog.LogLevel;
using INLogLogger = NLog.Logger;

namespace FlexKit.Logging.NLog.Core;

/// <summary>
/// Logger implementation that bridges Microsoft.Extensions.Logging to NLog.
/// Routes log messages to the appropriate NLog logger based on category.
/// </summary>
/// <param name="categoryName">The category name of the logger.</param>
/// <param name="config">The FlexKit logging configuration.</param>
/// <remarks>
/// Logger implementation that bridges Microsoft.Extensions.Logging to NLog.
/// Routes log messages to the appropriate NLog logger based on category.
/// </remarks>
public class NLogLogger(string categoryName, LoggingConfig config) : ILogger
{
    /// <summary>
    /// Represents the NLog logger instance used to log messages for a specific category.
    /// Provides functionality to log messages, exceptions, and additional properties
    /// using the NLog logging framework within the <see cref="NLogLogger"/> implementation.
    /// </summary>
    /// <remarks>
    /// The logger instance is created based on the specified category name.
    /// It serves as the underlying NLog component responsible for handling log messages,
    /// routing them to the appropriate targets, and managing the configured logging behavior.
    /// </remarks>
    private readonly INLogLogger _nlogLogger = LogManager.GetLogger(categoryName);

    /// <inheritdoc />
    public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

    /// <summary>
    /// Logs the specified information using the appropriate level of logging.
    /// Routes the log message to the NLog logger based on the configured category and log level.
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
        Microsoft.Extensions.Logging.LogLevel logLevel,
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
        var nlogLevel = ConvertLogLevel(logLevel);

        // Create LogEventInfo for NLog with structured data
        var logEventInfo = new LogEventInfo(nlogLevel, categoryName, message)
        {
            Exception = exception,
            Properties = { ["EventId"] = eventId.Id }
        };

        // Add state properties if the state is a structured object
        if (state is IEnumerable<KeyValuePair<string, object?>> properties)
        {
            foreach (var property in properties)
            {
                if (property.Key != "{OriginalFormat}")
                {
                    logEventInfo.Properties[property.Key] = property.Value;
                }
            }
        }

        _nlogLogger.Log(logEventInfo);
    }

    /// <inheritdoc />
    // ReSharper disable once FlagArgument
    public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) =>
        logLevel >= GetMinimumLogLevelForCategory() && _nlogLogger.IsEnabled(ConvertLogLevel(logLevel));

    /// <summary>
    /// Converts a <see cref="LogLevel"/> from Microsoft.Extensions.Logging to a corresponding <see cref="LogLevel"/> used by NLog.
    /// </summary>
    /// <param name="logLevel">The <see cref="LogLevel"/> to convert.</param>
    /// <returns>The corresponding <see cref="LogLevel"/> in NLog.</returns>
    private static LogLevel ConvertLogLevel(Microsoft.Extensions.Logging.LogLevel logLevel) =>
        logLevel switch
        {
            Microsoft.Extensions.Logging.LogLevel.Trace => LogLevel.Trace,
            Microsoft.Extensions.Logging.LogLevel.Debug => LogLevel.Debug,
            Microsoft.Extensions.Logging.LogLevel.Information => LogLevel.Info,
            Microsoft.Extensions.Logging.LogLevel.Warning => LogLevel.Warn,
            Microsoft.Extensions.Logging.LogLevel.Error => LogLevel.Error,
            Microsoft.Extensions.Logging.LogLevel.Critical => LogLevel.Fatal,
            Microsoft.Extensions.Logging.LogLevel.None => LogLevel.Off,
            _ => LogLevel.Info,
        };

    /// <summary>
    /// Determines the minimum log level for the specified category based on
    /// the configuration. Categories matching suppressed categories in the configuration
    /// inherit the suppressed log level, otherwise the trace level is used as default.
    /// </summary>
    /// <returns>The minimum <see cref="LogLevel"/> required for logging in the current category.</returns>
    private Microsoft.Extensions.Logging.LogLevel GetMinimumLogLevelForCategory() =>
        config.SuppressedCategories.Any(c => categoryName.StartsWith(c, StringComparison.InvariantCultureIgnoreCase))
            ? config.SuppressedLogLevel
            : Microsoft.Extensions.Logging.LogLevel.Trace;

    /// <summary>
    /// Represents a no-operation (no-op) disposable scope.
    /// Used to satisfy scope-related APIs without allocating unnecessary resources.
    /// </summary>
    /// <remarks>
    /// Provides an implementation of IDisposable that performs no operations when disposed.
    /// Typically used in logging or scenarios where scoping functionality is required but no actual scope management is needed.
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
