using System.Diagnostics;
using FlexKit.Logging.Models;
using Microsoft.Extensions.Logging;

namespace FlexKit.Logging.Core;

/// <summary>
/// The FlexKitLogger class provides logging functionality by integrating with the
/// IBackgroundLog interface for background logging, and the ActivitySource class
/// for activity tracing. It implements the IFlexKitLogger interface to support structured
/// logging and activity management capabilities.
/// </summary>
/// <param name="backgroundLog">Background log interface.</param>
/// <param name="activitySource">Activity source for activity tracing.</param>
/// <param name="logger">Fallback logger.</param>
public class FlexKitLogger(
    IBackgroundLog backgroundLog,
    ActivitySource activitySource,
    ILogger<FlexKitLogger> logger) : IFlexKitLogger
{
    /// <summary>
    /// Represents the background logging mechanism used to handle log entries in a high-performance
    /// and thread-safe manner. Implements the <see cref="IBackgroundLog"/> interface to provide
    /// functionality for enqueuing log entries for asynchronous processing.
    /// </summary>
    /// <remarks>
    /// This field is initialized through constructor injection and is a core component of the
    /// logging system in the <see cref="FlexKitLogger"/> class.
    /// </remarks>
    private readonly IBackgroundLog _backgroundLog =
        backgroundLog ?? throw new ArgumentNullException(nameof(backgroundLog));

    /// <summary>
    /// Represents the <see cref="System.Diagnostics.ActivitySource"/> instance used
    /// to create and manage diagnostic activities within the logging framework.
    /// Facilitates distributed tracing and observability by enabling the initiation,
    /// propagation, and completion of activity spans.
    /// </summary>
    /// <remarks>
    /// This field is a critical element of the activity management system in
    /// the <see cref="FlexKitLogger"/> class, allowing the incorporation of activity-based
    /// tracing within the logging infrastructure.
    /// </remarks>
    private readonly ActivitySource _activitySource =
        activitySource ?? throw new ArgumentNullException(nameof(activitySource));

    /// <summary>
    /// Defines a precompiled log message template for warning logs used when the log entry queue
    /// cannot enqueue additional items due to being full. This static delegate enhances performance
    /// by avoiding repeated parsing and formatting of log messages at runtime.
    /// </summary>
    /// <remarks>
    /// This field is used in scenarios where a log entry fails to be added to the background log queue,
    /// ensuring that the failure is reported through a fallback logger. The log message is associated
    /// with <see cref="LogLevel.Warning"/> and carries an event ID of 2.
    /// </remarks>
    private static readonly Action<ILogger, Exception?> _logQueueFullWarning =
        LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(2), // Different event ID from interceptor
            "Failed to enqueue manual log entry.");

    /// <inheritdoc />
    public void Log(LogEntry entry)
    {
        if (_backgroundLog.TryEnqueue(entry))
        {
            return;
        }

        _logQueueFullWarning(logger, null);
    }

    /// <inheritdoc />
    public Activity? StartActivity(string activityName) => _activitySource.StartActivity(activityName);
}
