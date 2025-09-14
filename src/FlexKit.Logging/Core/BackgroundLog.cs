using System.Runtime.CompilerServices;
using System.Threading.Channels;
using FlexKit.Logging.Configuration;
using FlexKit.Logging.Models;

namespace FlexKit.Logging.Core;

/// <summary>
/// High-performance background log queue implementation using System.Threading.Channels.
/// Optimized for minimal overhead during enqueue operations (~20ns) while providing
/// reliable background processing capabilities.
/// </summary>
public sealed class BackgroundLog : IBackgroundLog, IDisposable
{
    /// <summary>
    /// Represents a writer component from the bounded channel that enables enqueuing
    /// log entries for asynchronous background processing in the logging system.
    /// </summary>
    private readonly ChannelWriter<LogEntry> _writer;

    /// <summary>
    /// Represents a reader component from the bounded channel used to dequeue
    /// log entries for asynchronous background processing and consumption in the logging system.
    /// </summary>
    private readonly ChannelReader<LogEntry> _reader;

    /// <summary>
    /// Indicates whether the <see cref="BackgroundLog"/> instance has been disposed,
    /// preventing further enqueue or processing operations. Once set to <c>true</c>,
    /// the instance is considered no longer usable for its intended functionality.
    /// </summary>
    private volatile bool _disposed;

    /// <summary>
    /// Initializes a new instance of the BackgroundLogQueue with the specified capacity.
    /// </summary>
    public BackgroundLog(LoggingConfig loggingConfig)
    {
        if (loggingConfig.QueueCapacity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(loggingConfig),
                "QueueCapacity must be greater than zero");
        }

        var options = new BoundedChannelOptions(loggingConfig.QueueCapacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false,
        };

        var channel = Channel.CreateBounded<LogEntry>(options);
        _writer = channel.Writer;
        _reader = channel.Reader;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryEnqueue(LogEntry entry) => !_disposed && _writer.TryWrite(entry);

    /// <summary>
    /// Attempts to dequeue a log entry for immediate processing.
    /// Used for synchronous flushing during shutdown.
    /// </summary>
    /// <param name="entry">The dequeued log entry, if any</param>
    /// <returns>true if an entry was dequeued; false if the queue is empty</returns>
    public bool TryDequeue(out LogEntry entry) => _reader.TryRead(out entry);

    /// <inheritdoc />
    public async IAsyncEnumerable<LogEntry> ReadAllAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested && !_disposed)
        {
            bool canRead;
            try
            {
                canRead = await _reader.WaitToReadAsync(cancellationToken);
            }
            catch (InvalidOperationException)
            {
                yield break;
            }
            catch (OperationCanceledException)
            {
                yield break;
            }

            if (!canRead)
            {
                continue;
            }

            while (_reader.TryRead(out var entry))
            {
                yield return entry;
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _writer.TryComplete();
        _disposed = true;
    }
}
