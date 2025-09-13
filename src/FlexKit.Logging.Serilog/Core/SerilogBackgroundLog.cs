using System.Runtime.CompilerServices;
using FlexKit.Logging.Core;
using FlexKit.Logging.Models;

namespace FlexKit.Logging.Serilog.Core;

/// <summary>
/// Background log implementation that leverages Serilog's built-in batching and background processing.
/// Eliminates the need for FlexKit's Channel-based queuing by directly using Serilog's optimized batching.
/// </summary>
/// <remarks>
/// Initializes a new instance of SerilogBackgroundLog.
/// </remarks>
/// <param name="processor">The log entry processor that writes to Serilog.</param>
internal sealed class SerilogBackgroundLog(ILogEntryProcessor processor) : IBackgroundLog, IDisposable
{
    /// <summary>
    /// Represents the log entry processor used to process and write log entries to the configured destination.
    /// This field is an instance of <see cref="ILogEntryProcessor"/> responsible for handling the formatting
    /// and processing of log entries.
    /// </summary>
    /// <remarks>
    /// It is initialized in the constructor and is used throughout the lifecycle of
    /// <see cref="SerilogBackgroundLog"/>.
    /// The processor plays a critical role in enabling Serilog's optimized batching and background processing
    /// by directly processing log entries as they are received.
    /// </remarks>
    private readonly ILogEntryProcessor _processor = processor ?? throw new ArgumentNullException(nameof(processor));

    /// <summary>
    /// Indicates whether the current instance of <see cref="SerilogBackgroundLog"/> has been disposed.
    /// This field ensures proper resource cleanup and prevents further operations on a disposed instance.
    /// </summary>
    /// <remarks>
    /// Used to track the disposed state of the object. Set to <see langword="true"/> during the call
    /// to <see cref="Dispose"/>. Any operations attempted on a disposed instance, such as logging or enqueuing
    /// log entries, will be rejected. The field is marked as <see langword="volatile"/> to ensure thread-safe
    /// access in scenarios involving concurrent operations.
    /// </remarks>
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
            // Process immediately - let Serilog handle batching and background processing
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
        // Serilog handles its own queuing internally, so we can't dequeue individual entries
        // This method is primarily used for synchronous flushing during shutdown
        entry = default;
        return false;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<LogEntry> ReadAllAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Serilog handles background processing internally, so this enumerable is empty
        // The BackgroundLoggingService won't find any entries to process, which is correct
        // since Serilog is handling all the background work
        await Task.CompletedTask;
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

        // Serilog handles its own flushing through sink-specific mechanisms
        // No explicit flush needed since each sink manages its own batching and disposal
    }
}
