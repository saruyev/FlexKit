using System.Runtime.CompilerServices;
using FlexKit.Logging.Core;
using FlexKit.Logging.Models;

namespace FlexKit.Logging.Serilog.Core;

/// <summary>
/// Background log implementation that leverages Serilog's built-in batching and background processing.
/// Eliminates the need for FlexKit's Channel-based queuing by directly using Serilog's optimized batching.
/// </summary>
public sealed class SerilogBackgroundLog : IBackgroundLog, IDisposable
{
    private readonly ILogEntryProcessor _processor;
    private volatile bool _disposed;

    /// <summary>
    /// Initializes a new instance of SerilogBackgroundLog.
    /// </summary>
    /// <param name="processor">The log entry processor that writes to Serilog.</param>
    public SerilogBackgroundLog(ILogEntryProcessor processor)
    {
        _processor = processor ?? throw new ArgumentNullException(nameof(processor));
    }

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
        await Task.CompletedTask; // Move before yield break
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
