using System.Diagnostics;
using FlexKit.Logging.Models;

namespace FlexKit.Logging.Core;

/// <summary>
/// Manual logging interface that integrates with FlexKit.Logging's LogEntry and background pipeline.
/// </summary>
public interface IFlexKitLogger
{
    /// <summary>
    /// Logs a LogEntry to the background queue.
    /// </summary>
    /// <param name="entry">The log entry to log.</param>
    void Log(LogEntry entry);

    /// <summary>
    /// Creates and starts a System.Diagnostics.Activity, returning it for manual management.
    /// </summary>
    Activity? StartActivity(string activityName);
}
