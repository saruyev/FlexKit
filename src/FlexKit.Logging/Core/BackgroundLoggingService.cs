using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using FlexKit.Logging.Models;

namespace FlexKit.Logging.Core;

/// <summary>
/// Background service that processes queued log entries with basic serialization.
/// Handles the heavy lifting of JSON serialization and I/O operations off the main application threads.
/// </summary>
[SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates")]
public sealed class BackgroundLoggingService : BackgroundService
{
    private readonly IBackgroundLog _logQueue;
    private readonly ILogger<BackgroundLoggingService> _logger;
    private readonly ILogEntryProcessor _logEntryProcessor;

    private readonly SemaphoreSlim _processingLock = new(1, 1);


    /// <summary>
    /// Initializes a new instance of the BackgroundLoggingService.
    /// </summary>
    public BackgroundLoggingService(
        IBackgroundLog logQueue,
        ILogger<BackgroundLoggingService> logger,
        ILogEntryProcessor logEntryProcessor)
    {
        _logQueue = logQueue ?? throw new ArgumentNullException(nameof(logQueue));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logEntryProcessor = logEntryProcessor ?? throw new ArgumentNullException(nameof(logEntryProcessor));
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Background logging service starting...");

        try
        {
            await ProcessLogEntriesAsync(stoppingToken);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogInformation(ex, "Background logging service stopped due to cancellation");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in background logging service");
            throw new InvalidOperationException("Background logging service encountered an unexpected error and cannot continue", ex);
        }
        finally
        {
            _logger.LogInformation("Background logging service stopped");
        }
    }

    /// <summary>
    /// Main processing loop that reads and processes log entries from the queue.
    /// </summary>
    private async Task ProcessLogEntriesAsync(CancellationToken cancellationToken)
    {
        const int maxBatchSize = 100;
        var batch = new List<LogEntry>(maxBatchSize);

        await foreach (var entry in _logQueue.ReadAllAsync(cancellationToken))
        {
            batch.Add(entry);

            if (batch.Count >= maxBatchSize)
            {
                await ProcessBatchAsync(batch, cancellationToken);
                batch.Clear();
            }
        }

        // Process any remaining entries
        if (batch.Count > 0)
        {
            await ProcessBatchAsync(batch, cancellationToken);
        }
    }

    /// <summary>
    /// Processes a batch of log entries.
    /// </summary>
    [SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance")]
    private async Task ProcessBatchAsync(IReadOnlyList<LogEntry> entries, CancellationToken cancellationToken)
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
    private void ProcessSingleEntry(LogEntry entry)
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
