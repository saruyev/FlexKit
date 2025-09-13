using System.Runtime.CompilerServices;
using FlexKit.Logging.Core;
using FlexKit.Logging.Models;

namespace FlexKit.Logging.NLog.Core;

/// <summary>
/// Background log implementation that leverages NLog's built-in batching and asynchronous processing.
/// Eliminates the need for FlexKit's Channel-based queuing by directly using NLog's optimized async targets.
/// </summary>
/// <remarks>
/// <para>
/// This implementation follows the same pattern as SerilogBackgroundLog by delegating background processing
/// to the underlying logging framework. NLog handles batching, async writing, and buffer management through
/// its async targets and internal queuing mechanisms.
/// </para>
/// <para>
/// <strong>NLog Async Processing:</strong>
/// NLog provides several mechanisms for background processing:
/// <list type="bullet">
/// <item>AsyncWrapper target - wraps any target with async processing</item>
/// <item>Built-in async targets - File, Database, Network targets with async support</item>
/// <item>Internal buffering - NLog manages its own buffers and batching</item>
/// <item>Graceful shutdown - NLog handles flushing during application shutdown</item>
/// </list>
/// </para>
/// </remarks>
/// <param name="processor">The log entry processor that writes to NLog.</param>
internal sealed class NLogBackgroundLog(ILogEntryProcessor processor) : IBackgroundLog, IDisposable
{
    /// <summary>
    /// Represents the processor responsible for handling log entries.
    /// This instance is used to process log entries either immediately or in a background context,
    /// delegating actual processing tasks to the specified implementation of <see cref="ILogEntryProcessor"/>.
    /// </summary>
    private readonly ILogEntryProcessor _processor = processor ?? throw new ArgumentNullException(nameof(processor));

    /// <summary>
    /// Indicates whether the current instance has been disposed.
    /// This field is used to ensure that operations are not performed on a disposed object,
    /// preventing potential resource leaks or invalid operation exceptions.
    /// </summary>
    private volatile bool _disposed;

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryEnqueue(LogEntry entry)
    {
        if (_disposed)
        {
            return false;
        }

        try
        {
            // Process immediately - let NLog handle batching and background processing
            // NLog's async targets and wrappers will handle the actual queuing and background work
            _processor.ProcessEntry(entry);
            return true;
        }
        catch
        {
            // Return false to indicate enqueue failure, matching IBackgroundLog contract
            return false;
        }
    }

    /// <inheritdoc />
    public bool TryDequeue(out LogEntry entry)
    {
        // NLog handles its own queuing internally through async targets and wrappers,
        // so we can't dequeue individual entries from its internal buffers.
        // This method is primarily used for synchronous flushing during shutdown.
        entry = default;
        return false;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<LogEntry> ReadAllAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // NLog handles background processing internally through async targets and wrappers,
        // so this enumerable is empty. The BackgroundLoggingService won't find any entries
        // to process, which is correct since NLog is handling all the background work.
        await Task.CompletedTask.ConfigureAwait(false);
        yield break;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // NLog handles its own flushing through target-specific mechanisms:
        // - AsyncWrapper targets flush their internal queues
        // - File targets flush and close files
        // - Database targets complete pending transactions
        // - Network targets complete pending sends

        // Note: If explicit flushing is needed, it should be handled at the
        // LogManager level (LogManager.Flush() or LogManager.Shutdown())
        // rather than at individual logger instances.
    }
}
