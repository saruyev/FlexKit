using System.Runtime.CompilerServices;
using System.Threading.Channels;
using FlexKit.Logging.Models;

namespace FlexKit.Logging.Core;

/// <summary>
/// High-performance background log queue implementation using System.Threading.Channels.
/// Optimized for minimal overhead during enqueue operations (~20ns) while providing
/// reliable background processing capabilities.
/// </summary>
public sealed class BackgroundLog : IBackgroundLog, IDisposable
{
    private readonly ChannelWriter<LogEntry> _writer;
    private readonly ChannelReader<LogEntry> _reader;
    private volatile bool _disposed;

    /// <summary>
    /// Initializes a new instance of the BackgroundLogQueue with the specified capacity.
    /// </summary>
    /// <param name="capacity">Maximum number of entries the queue can hold. Default is 10,000.</param>
    public BackgroundLog(int capacity = 10_000)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than zero");
        }

        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        };

        var channel = Channel.CreateBounded<LogEntry>(options);
        _writer = channel.Writer;
        _reader = channel.Reader;

        Capacity = capacity;
    }

    /// <inheritdoc />
    public int Count => _reader.CanCount ? _reader.Count : 0;

    /// <inheritdoc />
    public int Capacity { get; }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryEnqueue(LogEntry entry) => !_disposed && _writer.TryWrite(entry);

    /// <inheritdoc />
    public async IAsyncEnumerable<LogEntry> ReadAllAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
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

    /// <summary>
    /// Marks the queue as complete for writing and prevents new entries from being enqueued.
    /// </summary>
    private void CompleteAdding()
    {
        if (_disposed)
        {
            return;
        }

        _writer.TryComplete();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        CompleteAdding();
    }
}
