using FlexKit.Logging.Configuration;
using FlexKit.Logging.Log4Net.Core;
using FluentAssertions;
using log4net.Core;
using log4net.Repository;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
// ReSharper disable FlagArgument

namespace FlexKit.Logging.Log4Net.Tests.Core;

public class Log4NetLoggerTests
{
    private readonly log4net.Core.ILogger _mockLog4NetLogger;
    private readonly LoggingConfig _config;
    private readonly Log4NetLogger _logger;

    public Log4NetLoggerTests()
    {
        var mockRepository = Substitute.For<ILoggerRepository>();
        _mockLog4NetLogger = Substitute.For<log4net.Core.ILogger>();
        _config = new LoggingConfig();
        
        mockRepository.GetLogger("TestCategory").Returns(_mockLog4NetLogger);
        _logger = new Log4NetLogger("TestCategory", mockRepository, _config);
    }

    [Fact]
    public void Log_WhenLogLevelIsDisabled_DoesNotCallLog4NetLogger()
    {
        // Arrange
        var logLevel = LogLevel.Information;
        _mockLog4NetLogger.IsEnabledFor(Arg.Any<Level>()).Returns(false);

        // Act
        _logger.Log(logLevel, new EventId(1), "Test message", null, (state, _) => state);

        // Assert
        _mockLog4NetLogger.DidNotReceive().Log(Arg.Any<LoggingEvent>());
    }

    [Fact]
    public void Log_WhenLogLevelIsEnabled_CallsLog4NetLoggerWithCorrectParameters()
    {
        // Arrange
        var logLevel = LogLevel.Information;
        var eventId = new EventId(1, "TestEvent");
        var state = "Test message";
        var exception = new ArgumentException("Test exception");
        
        _mockLog4NetLogger.IsEnabledFor(Arg.Any<Level>()).Returns(true);

        // Act
        _logger.Log(logLevel, eventId, state, exception, (s, _) => $"Formatted: {s}");

        // Assert
        _mockLog4NetLogger.Received(1).Log(Arg.Is<LoggingEvent>(e => 
            e.LoggerName == "TestCategory" &&
            e.Level == Level.Info &&
            e.MessageObject!.ToString() == "Formatted: Test message" &&
            e.ExceptionObject == exception));
    }

    [Theory]
    [InlineData(LogLevel.Trace, true)]
    [InlineData(LogLevel.Debug, true)]
    [InlineData(LogLevel.Information, true)]
    [InlineData(LogLevel.Warning, true)]
    [InlineData(LogLevel.Error, true)]
    [InlineData(LogLevel.Critical, true)]
    [InlineData(LogLevel.None, true)]
    public void IsEnabled_WithEmptySuppressedCategories_CallsLog4NetIsEnabledFor(LogLevel logLevel, bool log4NetEnabled)
    {
        // Arrange
        _config.SuppressedCategories.Clear();
        var expectedLevel = ConvertToLog4NetLevel(logLevel);
        _mockLog4NetLogger.IsEnabledFor(expectedLevel).Returns(log4NetEnabled);

        // Act
        var result = _logger.IsEnabled(logLevel);

        // Assert
        result.Should().Be(log4NetEnabled);
        _mockLog4NetLogger.Received(1).IsEnabledFor(expectedLevel);
    }

    [Theory]
    [InlineData(LogLevel.Trace)]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Information)]
    [InlineData(LogLevel.Warning)]
    [InlineData(LogLevel.Error)]
    [InlineData(LogLevel.Critical)]
    [InlineData(LogLevel.None)]
    public void IsEnabled_WithNonMatchingSuppressedCategories_ReturnsTrue_WhenLog4NetEnabled(LogLevel logLevel)
    {
        // Arrange
        _config.SuppressedCategories.Clear();
        _config.SuppressedCategories.Add("DifferentCategory");
        _config.SuppressedLogLevel = LogLevel.Warning;
        
        var expectedLevel = ConvertToLog4NetLevel(logLevel);
        _mockLog4NetLogger.IsEnabledFor(expectedLevel).Returns(true);

        // Act
        var result = _logger.IsEnabled(logLevel);

        // Assert
        result.Should().BeTrue();
        _mockLog4NetLogger.Received(1).IsEnabledFor(expectedLevel);
    }

    [Theory]
    [InlineData(LogLevel.Trace, LogLevel.Debug, false)]
    [InlineData(LogLevel.Debug, LogLevel.Information, false)]
    [InlineData(LogLevel.Information, LogLevel.Warning, false)]
    [InlineData(LogLevel.Warning, LogLevel.Error, false)]
    [InlineData(LogLevel.Error, LogLevel.Critical, false)]
    [InlineData(LogLevel.Warning, LogLevel.Debug, true)]
    [InlineData(LogLevel.Error, LogLevel.Warning, true)]
    [InlineData(LogLevel.Critical, LogLevel.Error, true)]
    public void IsEnabled_WithMatchingSuppressedCategoriesAndLevels_ReturnsExpectedResult(
        LogLevel requestedLevel, LogLevel suppressedLevel, bool expectedResult)
    {
        // Arrange
        _config.SuppressedCategories.Clear();
        _config.SuppressedCategories.Add("TestCategory");
        _config.SuppressedLogLevel = suppressedLevel;
        
        var expectedLog4NetLevel = ConvertToLog4NetLevel(requestedLevel);
        _mockLog4NetLogger.IsEnabledFor(expectedLog4NetLevel).Returns(true);

        // Act
        var result = _logger.IsEnabled(requestedLevel);

        // Assert
        result.Should().Be(expectedResult);
        if (expectedResult)
        {
            _mockLog4NetLogger.Received(1).IsEnabledFor(expectedLog4NetLevel);
        }
        else
        {
            _mockLog4NetLogger.DidNotReceive().IsEnabledFor(Arg.Any<Level>());
        }
    }

    [Theory]
    [InlineData("TestCategory", "Test", true)]
    [InlineData("TestCategory", "test", true)] // Case-insensitive
    [InlineData("TestCategory", "TestCat", true)] // Prefix match
    [InlineData("TestCategory", "Category", false)] // Must be prefixed
    [InlineData("TestCategory", "Different", false)]
    [InlineData("My.App.Services.TestService", "My.App", true)]
    [InlineData("My.App.Services.TestService", "my.app", true)] // Case-insensitive
    [InlineData("My.App.Services.TestService", "My.App.Controllers", false)]
    public void IsEnabled_WithSuppressedCategoriesPatterns_MatchesCorrectly(
        string categoryName, string suppressedPattern, bool shouldMatch)
    {
        // Arrange
        var mockRepository = Substitute.For<ILoggerRepository>();
        var mockLog4NetLogger = Substitute.For<log4net.Core.ILogger>();
        var config = new LoggingConfig
        {
            SuppressedCategories = [suppressedPattern],
            SuppressedLogLevel = LogLevel.Warning
        };
        
        mockRepository.GetLogger(categoryName).Returns(mockLog4NetLogger);
        mockLog4NetLogger.IsEnabledFor(Arg.Any<Level>()).Returns(true);
        
        var logger = new Log4NetLogger(categoryName, mockRepository, config);

        // Act
        var result = logger.IsEnabled(LogLevel.Information);

        // Assert
        if (shouldMatch)
        {
            // If category matches and requested level are below suppressed level, should return false
            result.Should().BeFalse();
            mockLog4NetLogger.DidNotReceive().IsEnabledFor(Arg.Any<Level>());
        }
        else
        {
            result.Should().BeTrue();
            mockLog4NetLogger.Received(1).IsEnabledFor(Level.Info);
        }
    }

    [Fact]
    public void BeginScope_ReturnsNullScopeInstance_ThatCanBeDisposed()
    {
        // Act
        var scope = _logger.BeginScope("test state");

        // Assert
        scope.Should().NotBeNull();
        
        // Verify that disposing doesn't throw
        var disposing = () => scope.Dispose();
        disposing.Should().NotThrow();
        
        // Verify we can dispose multiple times without issues
        disposing.Should().NotThrow();
    }

    private static Level ConvertToLog4NetLevel(LogLevel logLevel) =>
        logLevel switch
        {
            LogLevel.Trace => Level.Trace,
            LogLevel.Debug => Level.Debug,
            LogLevel.Information => Level.Info,
            LogLevel.Warning => Level.Warn,
            LogLevel.Error => Level.Error,
            LogLevel.Critical => Level.Fatal,
            LogLevel.None => Level.Off,
            _ => Level.Info,
        };
}