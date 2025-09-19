using FluentAssertions;
using Xunit;
using FlexKit.Logging.Core;

namespace FlexKit.Logging.Tests.Core
{
    public class BatchCollectorTests
    {
        [Fact]
        public void TryAdd_ShouldReturnFalse_WhenBelowThresholds()
        {
            // Arrange
            var collector = new BatchCollector<int>(maxSize: 3, timeout: TimeSpan.FromSeconds(10));

            // Act
            var result = collector.TryAdd(1, out var batch);

            // Assert
            result.Should().BeFalse();
            batch.Should().BeNull();
        }

        [Fact]
        public void TryAdd_ShouldFlush_WhenMaxSizeReached()
        {
            // Arrange
            var collector = new BatchCollector<int>(maxSize: 2, timeout: TimeSpan.FromSeconds(10));

            collector.TryAdd(1, out _);

            // Act
            var result = collector.TryAdd(2, out var batch);

            // Assert
            result.Should().BeTrue();
            batch.Should().NotBeNull().And.BeEquivalentTo([1, 2]);
        }

        [Fact]
        public void TryAdd_ShouldFlush_WhenTimeoutElapsed()
        {
            // Arrange
            var collector = new BatchCollector<int>(maxSize: 10, timeout: TimeSpan.FromMilliseconds(50));

            collector.TryAdd(1, out _);

            // Force timeout
            Thread.Sleep(60);

            // Act
            var result = collector.TryAdd(2, out var batch);

            // Assert
            result.Should().BeTrue();
            batch.Should().NotBeNull().And.Contain([1, 2]);
        }

        [Fact]
        public void Flush_ShouldReturnAndClearAllItems()
        {
            // Arrange
            var collector = new BatchCollector<string>(maxSize: 3, timeout: TimeSpan.FromSeconds(10));
            collector.TryAdd("a", out _);
            collector.TryAdd("b", out _);

            // Act
            var result = collector.Flush();

            // Assert
            result.Should().BeEquivalentTo(new[] { "a", "b" });

            // The next flush should be empty
            collector.Flush().Should().BeEmpty();
        }

        [Fact]
        public void Flush_ShouldReturnEmpty_WhenNoItems()
        {
            // Arrange
            var collector = new BatchCollector<int>(maxSize: 2, timeout: TimeSpan.FromSeconds(10));

            // Act
            var result = collector.Flush();

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void TryAdd_ShouldWorkAfterFlush()
        {
            // Arrange
            var collector = new BatchCollector<int>(maxSize: 2, timeout: TimeSpan.FromSeconds(10));

            collector.TryAdd(1, out _);
            collector.Flush(); // Clear

            // Act
            var result = collector.TryAdd(2, out var batch);

            // Assert
            result.Should().BeFalse();
            batch.Should().BeNull();
        }
    }
}
