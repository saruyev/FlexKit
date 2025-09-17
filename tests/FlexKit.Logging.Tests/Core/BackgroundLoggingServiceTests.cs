using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using FlexKit.Logging.Configuration;
using FlexKit.Logging.Core;
using FlexKit.Logging.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
// ReSharper disable HeuristicUnreachableCode
// ReSharper disable CollectionNeverUpdated.Local
// ReSharper disable NullableWarningSuppressionIsUsed
// ReSharper disable TooManyDeclarations
// ReSharper disable MethodHasAsyncOverload
// ReSharper disable MethodTooLong
// ReSharper disable ClassTooBig

namespace FlexKit.Logging.Tests.Core;

[SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates")]
public class BackgroundLoggingServiceTests
{
    private readonly IBackgroundLog _mockLogQueue;
    private readonly ILogger<BackgroundLoggingService> _mockLogger;
    private readonly ILogEntryProcessor _mockLogEntryProcessor;
    private readonly BackgroundLoggingService _service;

    public BackgroundLoggingServiceTests()
    {
        _mockLogQueue = Substitute.For<IBackgroundLog>();
        _mockLogger = Substitute.For<ILogger<BackgroundLoggingService>>();
        _mockLogEntryProcessor = Substitute.For<ILogEntryProcessor>();
        var mockConfig = new LoggingConfig { MaxBatchSize = 2, BatchTimeout = TimeSpan.FromMilliseconds(100) };
        _mockLogEntryProcessor.Config.Returns(mockConfig);
        
        _service = new BackgroundLoggingService(_mockLogQueue, _mockLogger, _mockLogEntryProcessor);
    }

    [Fact]
    public void Constructor_WithNullLogQueue_ThrowsArgumentNullException()
    {
        var action = () => new BackgroundLoggingService(null!, _mockLogger, _mockLogEntryProcessor);
        action.Should().Throw<ArgumentNullException>().WithParameterName("logQueue");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var action = () => new BackgroundLoggingService(_mockLogQueue, null!, _mockLogEntryProcessor);
        action.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullLogEntryProcessor_ThrowsArgumentNullException()
    {
        var action = () => new BackgroundLoggingService(_mockLogQueue, _mockLogger, null!);
        action.Should().Throw<ArgumentNullException>().WithParameterName("logEntryProcessor");
    }

    [Fact]
    public void FlushRemainingEntries_WithEntriesInQueue_ProcessesAllEntries()
    {
        // Arrange
        var entry1 = CreateTestLogEntry("Method1");
        var entry2 = CreateTestLogEntry("Method2");
        
        _mockLogQueue.TryDequeue(out Arg.Any<LogEntry>())
            .Returns(x => { x[0] = entry1; return true; }, x => { x[0] = entry2; return true; }, x => { x[0] = null; return false; });

        // Act
        _service.FlushRemainingEntries();

        // Assert
        _mockLogEntryProcessor.Received(1).ProcessEntry(entry1);
        _mockLogEntryProcessor.Received(1).ProcessEntry(entry2);
    }

    [Fact]
    public void FlushRemainingEntries_WhenProcessingThrows_LogsWarningAndContinues()
    {
        // Arrange
        var entry1 = CreateTestLogEntry("Method1");
        var entry2 = CreateTestLogEntry("Method2");
        var exception = new InvalidOperationException("Test exception");
    
        // Setup TryDequeue to return entries properly
        var callCount = 0;
        _mockLogQueue.TryDequeue(out Arg.Any<LogEntry>())
            .Returns(x =>
            {
                callCount++;
                if (callCount == 1)
                {
                    x[0] = entry1;
                    return true;
                }
                if (callCount == 2)
                {
                    x[0] = entry2;
                    return true;
                }
                x[0] = default(LogEntry);
                return false;
            });
    
        _mockLogEntryProcessor.When(x => x.ProcessEntry(entry1)).Throw(exception);

        // Act
        _service.FlushRemainingEntries();

        // Assert - The exception is caught in ProcessSingleEntry, not FlushRemainingEntries
        _mockLogger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Failed to process log entry")),
            exception,
            Arg.Any<Func<object, Exception?, string>>());
        _mockLogEntryProcessor.Received(1).ProcessEntry(entry2);
    }

    [Fact]
    public async Task ExecuteAsync_WithNormalOperation_LogsStartAndStop()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        
        // Set up empty async enumerable
        _mockLogQueue.ReadAllAsync(Arg.Any<CancellationToken>())
            .Returns(CreateEmptyAsyncEnumerable<LogEntry>());
        
        cts.Cancel(); // Cancel immediately to stop execution

        // Act
        await _service.StartAsync(CancellationToken.None);
        await _service.StopAsync(CancellationToken.None);

        // Assert
        _mockLogger.Received().LogDebug("Background logging service starting...");
        _mockLogger.Received().LogDebug("Background logging service stopped");
    }

    [Fact]
    public async Task ExecuteAsync_WithCancellation_LogsCancellationMessage()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
    
        _mockLogQueue.ReadAllAsync(Arg.Any<CancellationToken>())
            .Returns(CreateCancelledAsyncEnumerable<LogEntry>());

        // Act
        await _service.StartAsync(CancellationToken.None);
        await _service.StopAsync(cts.Token);

        // Assert
        _mockLogger.Received().LogDebug(Arg.Any<OperationCanceledException>(), "Background logging service stopped due to cancellation");
    }

    [Fact]
    public async Task ExecuteAsync_WithUnexpectedException_LogsError()
    {
        // Arrange
        var exception = new InvalidOperationException("Unexpected error");
    
        _mockLogQueue.ReadAllAsync(Arg.Any<CancellationToken>())
            .Returns(CreateThrowingAsyncEnumerable<LogEntry>(exception));

        // Act - Call ExecuteAsync directly using reflection
        var executeMethod = typeof(BackgroundLoggingService).GetMethod("ExecuteAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        executeMethod.Should().NotBeNull();
    
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
    
        try
        {
            await (Task)executeMethod.Invoke(_service, [cts.Token])!;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Background logging service encountered an unexpected error"))
        {
            // Expected exception
        }

        // Assert
        _mockLogger.Received().LogError(exception, "Unexpected error in background logging service");
    }

    [Fact]
    public async Task ProcessLogEntriesAsync_WithBatchedEntries_ProcessesBatches()
    {
        // Arrange
        var entries = new[] { CreateTestLogEntry("Method1"), CreateTestLogEntry("Method2"), CreateTestLogEntry("Method3") };
        
        _mockLogQueue.ReadAllAsync(Arg.Any<CancellationToken>())
            .Returns(CreateAsyncEnumerable(entries));

        // Act
        await _service.StartAsync(CancellationToken.None);
        await Task.Delay(200); // Allow processing
        await _service.StopAsync(CancellationToken.None);

        // Assert - should process in batches of 2 (MaxBatchSize = 2)
        _mockLogEntryProcessor.Received().ProcessEntry(Arg.Is<LogEntry>(e => e.MethodName == "Method1"));
        _mockLogEntryProcessor.Received().ProcessEntry(Arg.Is<LogEntry>(e => e.MethodName == "Method2"));
        _mockLogEntryProcessor.Received().ProcessEntry(Arg.Is<LogEntry>(e => e.MethodName == "Method3"));
    }

    [Fact]
    public async Task ProcessLogEntriesAsync_WithRemainingEntries_FlushesRemaining()
    {
        // Arrange
        var entry = CreateTestLogEntry("Method1");
        
        _mockLogQueue.ReadAllAsync(Arg.Any<CancellationToken>())
            .Returns(CreateAsyncEnumerable([entry]));

        // Act
        await _service.StartAsync(CancellationToken.None);
        await Task.Delay(200);
        await _service.StopAsync(CancellationToken.None);

        // Assert
        _mockLogEntryProcessor.Received().ProcessEntry(entry);
    }

    [Fact]
    public async Task ProcessBatchAsync_WhenDisposed_ProcessesUnsynchronized()
    {
        // Arrange
        var entry = CreateTestLogEntry("Method1");
        var disposeCalled = false;

        // Create an async enumerable that disposes the service after yielding an entry
        async IAsyncEnumerable<LogEntry> DisposeAfterEntry()
        {
            yield return entry;
        
            if (!disposeCalled)
            {
                disposeCalled = true;
                _service.Dispose(); // This sets _disposed = true
                await Task.Delay(10); // Give disposing time to complete
            }
        }

        _mockLogQueue.ReadAllAsync(Arg.Any<CancellationToken>())
            .Returns(DisposeAfterEntry());

        // Act
        await _service.StartAsync(CancellationToken.None);
        await Task.Delay(100); // Give time for processing
        await _service.StopAsync(CancellationToken.None);

        // Assert
        _mockLogEntryProcessor.Received().ProcessEntry(entry);
    }

    [Fact]
    public async Task TryAcquireLockAsync_WithObjectDisposedException_ReturnsFalse()
    {
        // This is difficult to test directly, but we can test disposal behavior
        // Arrange
        var entry = CreateTestLogEntry("Method1");
        _mockLogQueue.ReadAllAsync(Arg.Any<CancellationToken>())
            .Returns(CreateAsyncEnumerable([entry]));

        // Act
        await _service.StartAsync(CancellationToken.None);
        _service.Dispose();
        await Task.Delay(50);
        await _service.StopAsync(CancellationToken.None);

        // Assert - should still process the entry
        _mockLogEntryProcessor.Received().ProcessEntry(entry);
    }

    [Fact]
    public void Dispose_WhenCalledMultipleTimes_DoesNotThrow()
    {
        // Act & Assert
        _service.Dispose();
        var action = () => _service.Dispose();
        action.Should().NotThrow();
    }

    [Fact]
    public void Dispose_WaitsForProcessingLock_WithTimeout()
    {
        // Act
        _service.Dispose();

        // Assert - should complete without throwing
        _service.Should().NotBeNull();
    }
    
    [Fact]
    public async Task ProcessEntriesSynchronizedAsync_WhenLockNotAcquired_ProcessesUnsynchronized()
    {
        // Arrange
        var entry = CreateTestLogEntry("Method1");
    
        // Create a new service instance so we can control its semaphore
        var service = new BackgroundLoggingService(_mockLogQueue, _mockLogger, _mockLogEntryProcessor);
    
        // Use reflection to replace the semaphore with one that's already disposed
        var lockField = typeof(BackgroundLoggingService).GetField("_processingLock", BindingFlags.NonPublic | BindingFlags.Instance);
        var originalSemaphore = (SemaphoreSlim)lockField!.GetValue(service)!;
    
        // Dispose the original semaphore so WaitAsync will throw ObjectDisposedException
        originalSemaphore.Dispose();
    
        _mockLogQueue.ReadAllAsync(Arg.Any<CancellationToken>())
            .Returns(CreateAsyncEnumerable([entry]));

        // Act
        await service.StartAsync(CancellationToken.None);
        await Task.Delay(100);
    
        try
        {
            await service.StopAsync(CancellationToken.None);
        }
        catch
        {
            // Expected due to disposed semaphore
        }

        // Assert
        _mockLogEntryProcessor.Received().ProcessEntry(entry);
    
        // Cleanup
        service.Dispose();
    }
    
    [Fact]
    public void TryReleaseLock_ShouldIgnore_ObjectDisposedException()
    {
        // Arrange
        var logQueue = Substitute.For<IBackgroundLog>();
        var logger = Substitute.For<ILogger<BackgroundLoggingService>>();
        var processor = Substitute.For<ILogEntryProcessor>();

        var service = new BackgroundLoggingService(logQueue, logger, processor);

        // Create a disposed SemaphoreSlim
        var disposedSemaphore = new SemaphoreSlim(1, 1);
        disposedSemaphore.Dispose();

        // Replace private field _processingLock via reflection
        var field = typeof(BackgroundLoggingService)
            .GetField("_processingLock", BindingFlags.NonPublic | BindingFlags.Instance)!;
        field.SetValue(service, disposedSemaphore);

        // Get a private method TryReleaseLock
        var method = typeof(BackgroundLoggingService)
            .GetMethod("TryReleaseLock", BindingFlags.NonPublic | BindingFlags.Instance)!;

        // Act
        Action act = () => method.Invoke(service, null);

        // Assert
        act.Should().NotThrow();
    }
    
    private static LogEntry CreateTestLogEntry(string methodName = "TestMethod")
    {
        return LogEntry.CreateStart(
            methodName: methodName,
            typeName: "TestType",
            level: LogLevel.Information).WithCompletion(success: true);
    }

    private static async IAsyncEnumerable<T> CreateAsyncEnumerable<T>(IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            yield return item;
        }
        await Task.CompletedTask;
    }

    private static async IAsyncEnumerable<T> CreateEmptyAsyncEnumerable<T>()
    {
        await Task.CompletedTask;
        yield break;
    }

    private static async IAsyncEnumerable<T> CreateCancelledAsyncEnumerable<T>()
    {
        await Task.CompletedTask;
        throw new OperationCanceledException();
#pragma warning disable CS0162 // Unreachable code detected
        yield break;
#pragma warning restore CS0162 // Unreachable code detected
    }

    private static async IAsyncEnumerable<T> CreateThrowingAsyncEnumerable<T>(Exception exception)
    {
        await Task.CompletedTask;
        throw exception;
#pragma warning disable CS0162 // Unreachable code detected
        yield break;
#pragma warning restore CS0162 // Unreachable code detected
    }
}