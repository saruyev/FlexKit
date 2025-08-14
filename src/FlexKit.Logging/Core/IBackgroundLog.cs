using FlexKit.Logging.Models;

namespace FlexKit.Logging.Core;

/// <summary>
/// High-performance queue interface for background logging operations.
/// Provides thread-safe enqueueing with minimal overhead for method call instrumentation.
/// </summary>
public interface IBackgroundLog
{
    /// <summary>
    /// Attempts to enqueue a log entry for background processing.
    /// This is a non-blocking operation designed for minimal overhead.
    /// </summary>
    /// <param name="entry">The log entry to queue for processing</param>
    /// <returns>true if the entry was successfully queued; false if the queue is at capacity</returns>
    bool TryEnqueue(LogEntry entry);

    /// <summary>
    /// Reads log entries from the queue for background processing.
    /// This operation blocks until entries are available or cancellation is requested.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the read operation</param>
    /// <returns>An async enumerable of log entries for processing</returns>
    IAsyncEnumerable<LogEntry> ReadAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current number of queued entries waiting for processing.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Gets the maximum capacity of the queue.
    /// </summary>
    int Capacity { get; }
}
