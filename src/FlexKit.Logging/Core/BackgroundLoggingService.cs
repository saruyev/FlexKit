using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using FlexKit.Logging.Models;

namespace FlexKit.Logging.Core;

/// <summary>
/// Background service that processes queued log entries with basic serialization.
/// Handles the heavy lifting of JSON serialization and I/O operations off the main application threads.
/// </summary>
/// <remarks>
/// Initializes a new instance of the BackgroundLoggingService.
/// </remarks>
/// <param name="logQueue">The background log queue for processing log entries.</param>
/// <param name="logger">Logger for the interceptor itself.</param>
/// <param name="logEntryProcessor">The log entry processor to use for processing log entries.</param>
[SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates",
    Justification = "Used only for warnings and exceptions, no real performance impact")]
public sealed class BackgroundLoggingService(
    IBackgroundLog logQueue,
    ILogger<BackgroundLoggingService> logger,
    ILogEntryProcessor logEntryProcessor) : BackgroundService
{
    /// <summary>
    /// Represents the queue for background log processing in the <see cref="BackgroundLoggingService"/>.
    /// Used to store and dequeue log entries asynchronously for batch processing.
    /// </summary>
    /// <remarks>
    /// The queue is used for decoupling log entry creation and processing, ensuring minimal impact
    /// on the main application's performance. It serves as the intermediary storage for log entries before
    /// they are serialized and processed by the log entry processor.
    /// </remarks>
    private readonly IBackgroundLog _logQueue =
        logQueue ?? throw new ArgumentNullException(nameof(logQueue));

    /// <summary>
    /// Provides logging capabilities for the <see cref="BackgroundLoggingService"/>.
    /// Used to log messages, warnings, and errors related to the behavior and lifecycle of
    /// the background logging service.
    /// </summary>
    /// <remarks>
    /// This field is essential for monitoring the background service's execution,
    /// diagnostics, and error handling. It enables detailed logging for debugging
    /// and operational analysis throughout the service's lifecycle.
    /// </remarks>
    private readonly ILogger<BackgroundLoggingService> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Represents the log entry processor used by the <see cref="BackgroundLoggingService"/> to handle
    /// the processing of individual log entries.
    /// </summary>
    /// <remarks>
    /// Responsible for executing the main logic for processing log entries, including applying
    /// serialization, filtering, or other custom processing logic defined by the
    /// <see cref="ILogEntryProcessor"/> implementation.
    /// </remarks>
    private readonly ILogEntryProcessor _logEntryProcessor =
        logEntryProcessor ?? throw new ArgumentNullException(nameof(logEntryProcessor));

    /// <summary>
    /// A semaphore used to ensure exclusive access to critical sections of the logging process
    /// in the <see cref="BackgroundLoggingService"/>.
    /// Prevents concurrent processing of queued log entries, maintaining thread safety.
    /// </summary>
    /// <remarks>
    /// This lock helps coordinate asynchronous operations, ensuring only one task at a time
    /// can execute the critical logic for processing log entries. It is particularly useful
    /// in scenarios where multiple threads or tasks might attempt to access the same resource
    /// simultaneously, avoiding race conditions and inconsistencies.
    /// </remarks>
    private readonly SemaphoreSlim _processingLock = new(1, 1);

    /// <summary>
    /// Indicates whether the <see cref="BackgroundLoggingService"/> instance has been disposed.
    /// Used to prevent further processing or resource usage after the service is marked for disposal.
    /// </summary>
    /// <remarks>
    /// This flag is set to <c>true</c> when the <see cref="Dispose"/> method is called, ensuring that
    /// no further operations are attempted on disposed resources and ongoing tasks are either completed
    /// or gracefully terminated. It helps in managing the lifecycle of the service and avoiding resource
    /// leaks or unintended behaviors.
    /// </remarks>
    private volatile bool _disposed;

    /// <summary>
    /// Stops the service and flushes any remaining log entries.
    /// </summary>
    public void FlushRemainingEntries()
    {
        try
        {
            // Drain the queue and process entries synchronously
            while (_logQueue.TryDequeue(out var entry))
            {
                ProcessSingleEntry(entry);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error flushing remaining log entries");
        }
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogDebug("Background logging service starting...");

        try
        {
            await ProcessLogEntriesAsync(stoppingToken);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogDebug(ex, "Background logging service stopped due to cancellation");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in background logging service");
            throw new InvalidOperationException(
                "Background logging service encountered an unexpected error and cannot continue",
                ex);
        }
        finally
        {
            _logger.LogDebug("Background logging service stopped");
        }
    }

    /// <summary>
    /// Main processing loop that reads and processes log entries from the queue.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the read operation</param>
    private async Task ProcessLogEntriesAsync(CancellationToken cancellationToken)
    {
        var config = _logEntryProcessor.Config;
        var collector = new BatchCollector<LogEntry>(config.MaxBatchSize, config.BatchTimeout);

        await foreach (var entry in _logQueue.ReadAllAsync(cancellationToken))
        {
            if (collector.TryAdd(entry, out var batchToProcess) && batchToProcess != null)
            {
                await ProcessBatchAsync(batchToProcess, cancellationToken);
            }
        }

        // Process any remaining entries
        var remaining = collector.Flush();
        if (remaining is { Count: 0 })
        {
            return;
        }

        await ProcessBatchAsync(remaining, cancellationToken);
    }

    /// <summary>
    /// Processes a batch of log entries.
    /// </summary>
    /// <param name="entries">The batch of log entries to process.</param>
    /// <param name="cancellationToken">Token to cancel the processing operation</param>
    [SuppressMessage(
        "Performance",
        "CA1859:Use concrete types when possible for improved performance")]
    private async Task ProcessBatchAsync(
        IReadOnlyList<LogEntry> entries,
        CancellationToken cancellationToken)
    {
        if (entries.Count == 0)
        {
            return;
        }

        // Check if disposed before attempting to acquire the semaphore
        if (_disposed)
        {
            ProcessEntriesUnsynchronized(entries);
            return;
        }

        await ProcessEntriesSynchronizedAsync(entries, cancellationToken);
    }

    /// <summary>
    /// Processes entries without semaphore synchronization during shutdown.
    /// </summary>
    /// <param name="entries">The batch of log entries to process.</param>
    private void ProcessEntriesUnsynchronized(IReadOnlyList<LogEntry> entries)
    {
        foreach (var entry in entries)
        {
            try
            {
                ProcessSingleEntry(entry);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process log entry {EntryId}", entry.Id);
            }
        }
    }

    /// <summary>
    /// Processes entries with semaphore synchronization during normal operation.
    /// </summary>
    /// <param name="entries">The batch of log entries to process.</param>
    /// <param name="cancellationToken">Token to cancel the processing operation</param>
    private async Task ProcessEntriesSynchronizedAsync(
        IReadOnlyList<LogEntry> entries,
        CancellationToken cancellationToken)
    {
        var lockAcquired = false;

        try
        {
            lockAcquired = await TryAcquireLockAsync(cancellationToken);

            if (!lockAcquired)
            {
                ProcessEntriesUnsynchronized(entries);
                return;
            }

            ProcessEntriesUnsynchronized(entries);
        }
        finally
        {
            if (lockAcquired && !_disposed)
            {
                TryReleaseLock();
            }
        }
    }

    /// <summary>
    /// Attempts to acquire the processing lock safely.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the wait operation</param>
    /// <returns>True if a lock was acquired, false if disposed during acquisition</returns>
    private async Task<bool> TryAcquireLockAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _processingLock.WaitAsync(cancellationToken);
            return true;
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
    }

    /// <summary>
    /// Attempts to release the processing lock safely.
    /// </summary>
    private void TryReleaseLock()
    {
        try
        {
            _processingLock.Release();
        }
        catch (ObjectDisposedException)
        {
            // Semaphore was disposed between check and release - acceptable during shutdown
        }
    }

    /// <summary>
    /// Processes a single log entry by delegating to the log entry processor.
    /// </summary>
    /// <param name="entry">The log entry to process.</param>
    private void ProcessSingleEntry(in LogEntry entry)
    {
        try
        {
            _logEntryProcessor.ProcessEntry(entry);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to process log entry {EntryId}", entry.Id);
        }
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        _disposed = true;

        // Give any ongoing operations a moment to complete
        try
        {
            _processingLock.Wait(TimeSpan.FromMilliseconds(100));
        }
        catch (ObjectDisposedException)
        {
            // Expected if already disposed
        }

        _processingLock.Dispose();
        base.Dispose();
    }
}
