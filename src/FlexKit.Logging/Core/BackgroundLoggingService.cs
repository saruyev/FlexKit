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
[SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates")]
public sealed class BackgroundLoggingService(
    IBackgroundLog logQueue,
    ILogger<BackgroundLoggingService> logger,
    ILogEntryProcessor logEntryProcessor) : BackgroundService
{
    private readonly IBackgroundLog _logQueue =
        logQueue ?? throw new ArgumentNullException(nameof(logQueue));
    private readonly ILogger<BackgroundLoggingService> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly ILogEntryProcessor _logEntryProcessor =
        logEntryProcessor ?? throw new ArgumentNullException(nameof(logEntryProcessor));
    private readonly SemaphoreSlim _processingLock = new(1, 1);

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

        await _processingLock.WaitAsync(cancellationToken);
        try
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
        finally
        {
            _processingLock.Release();
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
        _processingLock.Dispose();
        base.Dispose();
    }
}
