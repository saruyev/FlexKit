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
    private readonly IBackgroundLog _backgroundLog =
        backgroundLog ?? throw new ArgumentNullException(nameof(backgroundLog));
    private readonly ActivitySource _activitySource =
        activitySource ?? throw new ArgumentNullException(nameof(activitySource));
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
