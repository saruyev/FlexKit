using System.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using FlexKit.Logging.Core;
using FlexKit.Logging.Models;
// ReSharper disable NullableWarningSuppressionIsUsed
// ReSharper disable ObjectCreationAsStatement
// ReSharper disable TooManyDeclarations

namespace FlexKit.Logging.Tests.Core
{
    public class FlexKitLoggerTests
    {
        [Fact]
        public void Ctor_ShouldThrow_WhenBackgroundLogIsNull()
        {
            // Act
            Action act = () => new FlexKitLogger(null!, new ActivitySource("test"), Substitute.For<ILogger<FlexKitLogger>>());

            // Assert
            act.Should().Throw<ArgumentNullException>()
               .WithParameterName("backgroundLog");
        }

        [Fact]
        public void Ctor_ShouldThrow_WhenActivitySourceIsNull()
        {
            // Act
            Action act = () => new FlexKitLogger(Substitute.For<IBackgroundLog>(), null!, Substitute.For<ILogger<FlexKitLogger>>());

            // Assert
            act.Should().Throw<ArgumentNullException>()
               .WithParameterName("activitySource");
        }

        [Fact]
        public void Log_ShouldDoNothing_WhenEnqueueSucceeds()
        {
            // Arrange
            var backgroundLog = Substitute.For<IBackgroundLog>();
            var logger = Substitute.For<ILogger<FlexKitLogger>>();
            var flexLogger = new FlexKitLogger(backgroundLog, new ActivitySource("test"), logger);

            var entry = LogEntry.CreateStart(nameof(FlexKitLoggerTests), nameof(Log_ShouldDoNothing_WhenEnqueueSucceeds));

            backgroundLog.TryEnqueue(entry).Returns(true);

            // Act
            flexLogger.Log(entry);

            // Assert
            logger.DidNotReceiveWithAnyArgs().Log(default, default, null, null, null!);
        }

        [Fact]
        public void Log_ShouldLogWarning_WhenEnqueueFails()
        {
            // Arrange
            var backgroundLog = Substitute.For<IBackgroundLog>();
            var logger = Substitute.For<ILogger<FlexKitLogger>>();
            logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
            var flexLogger = new FlexKitLogger(backgroundLog, new ActivitySource("test"), logger);

            var entry = LogEntry.CreateStart(nameof(FlexKitLoggerTests), nameof(Log_ShouldLogWarning_WhenEnqueueFails));

            backgroundLog.TryEnqueue(entry).Returns(false);

            // Act
            flexLogger.Log(entry);

            // Assert
            logger.Received(1).Log(
                LogLevel.Warning,
                Arg.Is<EventId>(id => id.Id == 2),
                Arg.Any<object>(),
                null,
                Arg.Any<Func<object, Exception?, string>>());
        }

        [Fact]
        public void StartActivity_ShouldReturnActivity_WhenListenerIsRegistered()
        {
            // Arrange
            var activitySourceName = "test-source";
            var activitySource = new ActivitySource(activitySourceName);

            var listener = new ActivityListener
            {
                ShouldListenTo = src => src.Name == activitySourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData
            };

            ActivitySource.AddActivityListener(listener);

            var flexLogger = new FlexKitLogger(
                Substitute.For<IBackgroundLog>(),
                activitySource,
                Substitute.For<ILogger<FlexKitLogger>>());

            // Act
            using var activity = flexLogger.StartActivity("my-activity");

            // Assert
            activity.Should().NotBeNull();
            activity.OperationName.Should().Be("my-activity");
        }

    }
}
