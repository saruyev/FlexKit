using FlexKit.Logging.Core;
using FlexKit.Logging.Models;
using FlexKit.Logging.NLog.Core;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace FlexKit.Logging.NLog.Tests.Core;

/// <summary>
/// Unit tests for NLogBackgroundLog class covering disposal scenarios,
/// enqueue/dequeue operations, and async enumeration behavior.
/// </summary>
public class NLogBackgroundLogTests
{
    private readonly ILogEntryProcessor _mockProcessor;
    private readonly NLogBackgroundLog _backgroundLog;

    public NLogBackgroundLogTests()
    {
        _mockProcessor = Substitute.For<ILogEntryProcessor>();
        _backgroundLog = new NLogBackgroundLog(_mockProcessor);
    }

    [Fact]
    public void TryEnqueue_WhenDisposedIsTrue_ReturnsFalse()
    {
        // Arrange
        var logEntry = CreateTestLogEntry();
        _backgroundLog.Dispose(); // Set _disposed to true

        // Act
        var result = _backgroundLog.TryEnqueue(logEntry);

        // Assert
        result.Should().BeFalse();
        _mockProcessor.DidNotReceive().ProcessEntry(Arg.Any<LogEntry>());
    }

    [Fact]
    public void TryEnqueue_WhenDisposedIsFalse_ProcessEntryThrowsException_ReturnsFalse()
    {
        // Arrange
        var logEntry = CreateTestLogEntry();
        _mockProcessor.When(x => x.ProcessEntry(Arg.Any<LogEntry>()))
            .Do(_ => throw new InvalidOperationException("Test exception"));

        // Act
        var result = _backgroundLog.TryEnqueue(logEntry);

        // Assert
        result.Should().BeFalse();
        _mockProcessor.Received(1).ProcessEntry(logEntry);
    }

    [Fact]
    public void TryEnqueue_ReturnsTrue()
    {
        // Arrange
        var logEntry = CreateTestLogEntry();

        // Act
        var result = _backgroundLog.TryEnqueue(logEntry);

        // Assert
        result.Should().BeTrue();
        _mockProcessor.Received(1).ProcessEntry(logEntry);
    }

    [Fact]
    public void TryDequeue_ReturnsFalse()
    {
        // Act
        var result = _backgroundLog.TryDequeue(out var entry);

        // Assert
        result.Should().BeFalse();
        entry.Should().Be(default(LogEntry));
    }

    [Fact]
    public async Task ReadAllAsync_YieldsBreak()
    {
        // Act
        var entries = new List<LogEntry>();
        await foreach (var entry in _backgroundLog.ReadAllAsync())
        {
            entries.Add(entry);
        }

        // Assert
        entries.Should().BeEmpty();
    }

    [Fact]
    public void Dispose_WhenDisposedIsTrue_DoesNotThrow()
    {
        // Arrange
        _backgroundLog.Dispose(); // Set _disposed to true

        // Act
        var action = () => _backgroundLog.Dispose();

        // Assert
        action.Should().NotThrow();
    }

    [Fact]
    public void Dispose_WhenDisposedIsFalse_SetsDisposedToTrue()
    {
        // Act
        _backgroundLog.Dispose();

        // Assert - verify disposed state by trying to enqueue
        var logEntry = CreateTestLogEntry();
        var result = _backgroundLog.TryEnqueue(logEntry);
        result.Should().BeFalse();
        _mockProcessor.DidNotReceive().ProcessEntry(Arg.Any<LogEntry>());
    }

    private static LogEntry CreateTestLogEntry(string methodName = "TestMethod")
    {
        return LogEntry.CreateStart(methodName, "TestClass");
    }
}