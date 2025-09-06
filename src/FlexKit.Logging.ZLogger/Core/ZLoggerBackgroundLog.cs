using System.Runtime.CompilerServices;
using FlexKit.Logging.Core;
using FlexKit.Logging.Models;

namespace FlexKit.Logging.ZLogger.Core;

/// <summary>
/// Provides a sealed implementation of the IBackgroundLog interface for managing background logging operations.
/// Integrates with ZLogger and uses ILogEntryProcessor for processing and outputting log entries.
/// </summary>
/// <remarks>
/// Provides a sealed implementation of the IBackgroundLog interface for managing background logging operations.
/// Integrates with ZLogger and uses ILogEntryProcessor for processing and outputting log entries.
/// </remarks>
/// <param name="processor">The log entry processor that writes to ZLogger.</param>
public sealed class ZLoggerBackgroundLog(ILogEntryProcessor processor) : IBackgroundLog, IDisposable
{
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
            processor.ProcessEntry(entry);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public bool TryDequeue(out LogEntry entry)
    {
        entry = default;
        return false;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<LogEntry> ReadAllAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
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
    }
}
