using FlexKit.Logging.Models;

namespace FlexKit.Logging.Core;

/// <summary>
/// Processes individual log entries by formatting and outputting them.
/// Handles the formatting pipeline and output destination logic.
/// </summary>
public interface ILogEntryProcessor
{
    /// <summary>
    /// Processes a single log entry through the formatting pipeline and outputs the result.
    /// </summary>
    /// <param name="entry">The log entry to process.</param>
    void ProcessEntry(LogEntry entry);
}
