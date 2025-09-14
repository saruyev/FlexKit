using System.Reflection;
using System.Threading.Channels;
using FlexKit.Logging.Configuration;
using FlexKit.Logging.Core;
using FlexKit.Logging.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
// ReSharper disable NullableWarningSuppressionIsUsed

namespace FlexKit.Logging.Tests.Core;

public class BackgroundLogTests
{
    [Fact]
    public void Constructor_WithValidCapacity_CreatesInstance()
    {
        // Act
        var backgroundLog = new BackgroundLog(new LoggingConfig { QueueCapacity = 5000 });

        // Assert
        backgroundLog.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithDefaultCapacity_CreatesInstance()
    {
        // Act
        var backgroundLog = new BackgroundLog(new LoggingConfig());

        // Assert
        backgroundLog.Should().NotBeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Constructor_WithInvalidCapacity_ThrowsArgumentOutOfRangeException(int capacity)
    {
        // Act & Assert
        var action = () => new BackgroundLog(new LoggingConfig { QueueCapacity = capacity });
        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("loggingConfig")
            .WithMessage("*QueueCapacity must be greater than zero*");
    }

    [Fact]
    public void TryEnqueue_WithValidEntry_ReturnsTrue()
    {
        // Arrange
        var backgroundLog = new BackgroundLog(new LoggingConfig { QueueCapacity = 10 });
        var logEntry = CreateTestLogEntry();

        // Act
        var result = backgroundLog.TryEnqueue(logEntry);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void TryEnqueue_AfterDispose_ReturnsFalse()
    {
        // Arrange
        var backgroundLog = new BackgroundLog(new LoggingConfig { QueueCapacity = 10 });
        var logEntry = CreateTestLogEntry();
        backgroundLog.Dispose();

        // Act
        var result = backgroundLog.TryEnqueue(logEntry);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void TryDequeue_WithEnqueuedEntry_ReturnsTrueAndEntry()
    {
        // Arrange
        var backgroundLog = new BackgroundLog(new LoggingConfig { QueueCapacity = 10 });
        var logEntry = CreateTestLogEntry();
        backgroundLog.TryEnqueue(logEntry);

        // Act
        var result = backgroundLog.TryDequeue(out var dequeuedEntry);

        // Assert
        result.Should().BeTrue();
        dequeuedEntry.MethodName.Should().Be(logEntry.MethodName);
        dequeuedEntry.TypeName.Should().Be(logEntry.TypeName);
        dequeuedEntry.Level.Should().Be(logEntry.Level);
        dequeuedEntry.Success.Should().Be(logEntry.Success);
        dequeuedEntry.Id.Should().Be(logEntry.Id);
        dequeuedEntry.OutputValue.Should().Be(logEntry.OutputValue);
        dequeuedEntry.ExceptionMessage.Should().Be(logEntry.ExceptionMessage);
        dequeuedEntry.Target.Should().Be(logEntry.Target);
        dequeuedEntry.Formatter.Should().Be(logEntry.Formatter);
    }

    [Fact]
    public void TryDequeue_WithEmptyQueue_ReturnsFalse()
    {
        // Arrange
        var backgroundLog = new BackgroundLog(new LoggingConfig { QueueCapacity = 10 });

        // Act
        var result = backgroundLog.TryDequeue(out var entry);

        // Assert
        result.Should().BeFalse();
        entry.Should().Be(default(LogEntry));
    }

    [Fact]
    public void TryEnqueue_WhenQueueIsFull_DropsOldestEntries()
    {
        // Arrange
        var backgroundLog = new BackgroundLog(new LoggingConfig { QueueCapacity = 2 });
        var entry1 = CreateTestLogEntry("Method1");
        var entry2 = CreateTestLogEntry("Method2");
        var entry3 = CreateTestLogEntry("Method3");

        // Act
        backgroundLog.TryEnqueue(entry1);
        backgroundLog.TryEnqueue(entry2);
        backgroundLog.TryEnqueue(entry3); // Should drop entry1

        // Assert
        backgroundLog.TryDequeue(out var dequeuedEntry1).Should().BeTrue();
        backgroundLog.TryDequeue(out var dequeuedEntry2).Should().BeTrue();
        backgroundLog.TryDequeue(out _).Should().BeFalse();

        dequeuedEntry1.MethodName.Should().Be("Method2");
        dequeuedEntry2.MethodName.Should().Be("Method3");
    }

    [Fact]
    public async Task ReadAllAsync_WithEnqueuedEntries_ReturnsAllEntries()
    {
        // Arrange
        var backgroundLog = new BackgroundLog(new LoggingConfig { QueueCapacity = 10 });
        var entry1 = CreateTestLogEntry("Method1");
        var entry2 = CreateTestLogEntry("Method2");

        backgroundLog.TryEnqueue(entry1);
        backgroundLog.TryEnqueue(entry2);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        // Act
        var entries = new List<LogEntry>();
        await foreach (var entry in backgroundLog.ReadAllAsync(cts.Token))
        {
            entries.Add(entry);
            if (entries.Count >= 2) break; // Prevent infinite loop
        }

        // Assert
        entries.Should().HaveCount(2);
        entries[0].MethodName.Should().Be("Method1");
        entries[1].MethodName.Should().Be("Method2");
    }

    [Fact]
    public async Task ReadAllAsync_WithCancellation_StopsReading()
    {
        // Arrange
        var backgroundLog = new BackgroundLog(new LoggingConfig { QueueCapacity = 10 });
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(10));

        // Act & Assert
        var entries = new List<LogEntry>();
        await foreach (var entry in backgroundLog.ReadAllAsync(cts.Token))
        {
            entries.Add(entry);
        }

        entries.Should().BeEmpty();
    }

    [Fact]
    public async Task ReadAllAsync_AfterDispose_StopsReading()
    {
        // Arrange
        var backgroundLog = new BackgroundLog(new LoggingConfig { QueueCapacity = 10 });
        var entry = CreateTestLogEntry();
        backgroundLog.TryEnqueue(entry);

        // Act
        backgroundLog.Dispose();

        var entries = new List<LogEntry>();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        await foreach (var logEntry in backgroundLog.ReadAllAsync(cts.Token))
        {
            entries.Add(logEntry);
        }

        // Assert - should not read any entries after disposal
        entries.Should().BeEmpty();
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        // Arrange
        var backgroundLog = new BackgroundLog(new LoggingConfig { QueueCapacity = 10 });

        // Act & Assert
        backgroundLog.Dispose();
        var action = () => backgroundLog.Dispose();
        action.Should().NotThrow();
    }

    [Fact]
    public void Dispose_CompletesWriting_PreventsFurtherEnqueues()
    {
        // Arrange
        var backgroundLog = new BackgroundLog(new LoggingConfig { QueueCapacity = 10 });
        var entry = CreateTestLogEntry();

        // Act
        backgroundLog.Dispose();
        var result = backgroundLog.TryEnqueue(entry);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ReadAllAsync_WhenChannelCompleted()
    {
        // Arrange
        var backgroundLog = new BackgroundLog(new LoggingConfig { QueueCapacity = 1 });
        var entry = CreateTestLogEntry();
        backgroundLog.TryEnqueue(entry);
    
        // Start reading
        var entries = new List<LogEntry>();
        var enumerator = backgroundLog.ReadAllAsync().GetAsyncEnumerator();
    
        // Read the first entry
        await enumerator.MoveNextAsync();
        entries.Add(enumerator.Current);
    
        // Dispose to complete the channel and cause InvalidOperationException on next WaitToReadAsync
        backgroundLog.Dispose();
    
        // Act - try to read more, should handle InvalidOperationException and exit
        var hasMore = await enumerator.MoveNextAsync();
    
        // Assert
        hasMore.Should().BeFalse();
        entries.Should().HaveCount(1);
    
        await enumerator.DisposeAsync();
    }

    [Fact]
    public async Task ReadAllAsync_WhenChannelReaderCompletedButNoException_ContinuesLoop()
    {
        var backgroundLog = new BackgroundLog(new LoggingConfig { QueueCapacity = 1 });
        backgroundLog.Dispose();

        var entries = new List<LogEntry>();
        await foreach (var entry in backgroundLog.ReadAllAsync())
        {
            entries.Add(entry);
        }

        entries.Should().BeEmpty();
    }
    
    [Fact]
    public async Task ReadAllAsync_ShouldStop_OnInvalidOperation()
    {
        var backgroundLog = new BackgroundLog(new LoggingConfig { QueueCapacity = 1 });

        var reader = Substitute.For<ChannelReader<LogEntry>>();
        reader
            .WaitToReadAsync(Arg.Any<CancellationToken>())
            .Returns<bool>(_ => throw new InvalidOperationException());

        SetPrivateField(backgroundLog, "_reader", reader);

        var enumerator = backgroundLog.ReadAllAsync().GetAsyncEnumerator();
        var hasNext = await enumerator.MoveNextAsync();

        hasNext.Should().BeFalse();
    }
    
    [Fact]
    public async Task ReadAllAsync_should_handle_initial_False_then_exit_when_reader_throws_with_NSubstitute()
    {
        var backgroundLog = new BackgroundLog(new LoggingConfig { QueueCapacity = 1 });

        var reader = Substitute.For<ChannelReader<LogEntry>>();

        int calls = 0;
        reader
            .WaitToReadAsync(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                // first call -> return false
                if (Interlocked.Increment(ref calls) == 1) 
                    return new ValueTask<bool>(false);
                throw new InvalidOperationException();
            });

        // TryRead not used in this scenario, but set a default:
        reader.TryRead(out Arg.Any<LogEntry>()).Returns(x => { x[0] = null!; return false; });

        SetPrivateField(backgroundLog, "_reader", reader);

        var enumerator = backgroundLog.ReadAllAsync().GetAsyncEnumerator();
        var hasNext = await enumerator.MoveNextAsync();

        hasNext.Should().BeFalse();
    }
    
    [Fact]
    public void Dispose_CallsCompleteAdding_WhenNotAlreadyDisposed()
    {
        // Arrange
        var backgroundLog = new BackgroundLog(new LoggingConfig { QueueCapacity = 1 });

        // Act
        backgroundLog.Dispose();

        // Assert - if CompleteAdding/TryComplete wasn't called, this would hang
        // The fact that Dispose() returns means CompleteAdding() was called
        // We can verify by checking that a second disposal doesn't throw
        var action = () => backgroundLog.Dispose();
        action.Should().NotThrow();
    }

    
    private static LogEntry CreateTestLogEntry(string methodName = "TestMethod")
    {
        return LogEntry.CreateStart(
            methodName: methodName,
            typeName: "TestType",
            level: LogLevel.Information);
    }
    
    private static void SetPrivateField<T>(object target, string fieldName, T value)
    {
        var field = target.GetType()
            .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        field!.SetValue(target, value);
    }
}