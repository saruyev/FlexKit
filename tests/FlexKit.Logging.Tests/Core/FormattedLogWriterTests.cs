using FlexKit.Logging.Configuration;
using FlexKit.Logging.Core;
using FlexKit.Logging.Formatting.Core;
using FlexKit.Logging.Formatting.Models;
using FlexKit.Logging.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;
// ReSharper disable NullableWarningSuppressionIsUsed
// ReSharper disable FlagArgument

namespace FlexKit.Logging.Tests.Core;

public class FormattedLogWriterTests
{
    private readonly LoggingConfig _config = new()
    {
        EnableFallbackFormatting = true,
        FallbackTemplate = "Fallback {TypeName}.{MethodName} Success={Success} Id={Id}"
    };

    private readonly IMessageFormatterFactory _formatterFactory =
        Substitute.For<IMessageFormatterFactory>();

    private readonly ILoggerFactory _loggerFactory =
        Substitute.For<ILoggerFactory>();

    private readonly ILogger _logger =
        Substitute.For<ILogger>();

    public FormattedLogWriterTests()
    {
        // Any logger requested -> return our fake logger
        _loggerFactory.CreateLogger(Arg.Any<string>()).Returns(_logger);
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
    }

    private static LogEntry CreateEntry(string typeName = "TestType", string methodName = "TestMethod") =>
        LogEntry.CreateStart(methodName, typeName).WithCompletion(success: true);

    [Fact]
    public void ProcessEntry_ShouldOutputFormattedMessage_WhenFormattingSucceeds()
    {
        // Arrange
        var entry = CreateEntry();

        var formatter = Substitute.For<IMessageFormatter>();
        formatter.Format(Arg.Any<FormattingContext>()).Returns(FormattedMessage.Success("Hello World"));

        _formatterFactory.GetFormatter(Arg.Any<FormattingContext>()).Returns((formatter, false));

        var writer = new FormattedLogWriter(_config, _formatterFactory, _loggerFactory);

        // Act
        writer.ProcessEntry(entry);

        // Assert: output was logged with Info level
        _logger.Received().IsEnabled(LogLevel.Information);
        _logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Hello World")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void ProcessEntry_ShouldUseFallback_WhenFormattingFails_AndFallbackEnabled()
    {
        // Arrange
        var entry = CreateEntry();
        var context = FormattingContext.Create(entry, _config);

        var formatter = Substitute.For<IMessageFormatter>();
        formatter.Format(context).Returns(FormattedMessage.Failure("boom"));

        _formatterFactory.GetFormatter(Arg.Any<FormattingContext>()).Returns((formatter, false));

        var writer = new FormattedLogWriter(_config, _formatterFactory, _loggerFactory);

        // Act
        writer.ProcessEntry(entry);

        // Assert: warning about fallback and fallback message logged
        _logger.Received().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            null,
            Arg.Any<Func<object, Exception?, string>>());

        _logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Fallback TestType.TestMethod")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void ProcessEntry_ShouldLogError_WhenFormattingFails_AndFallbackDisabled()
    {
        // Arrange
        var config = new LoggingConfig { EnableFallbackFormatting = false };
        var entry = CreateEntry();

        var formatter = Substitute.For<IMessageFormatter>();
        formatter.Format(Arg.Any<FormattingContext>())
            .Returns(FormattedMessage.Failure("bad"));

        _formatterFactory.GetFormatter(Arg.Any<FormattingContext>()).Returns((formatter, false));

        var writer = new FormattedLogWriter(config, _formatterFactory, _loggerFactory);

        // Act
        writer.ProcessEntry(entry);

        // Assert
        _logger.Received().Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("bad")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void ProcessEntry_ShouldHandleUnexpectedException()
    {
        // Arrange
        var entry = CreateEntry();

        var formatter = Substitute.For<IMessageFormatter>();
        formatter.Format(Arg.Any<FormattingContext>())
            .Returns(_ => throw new InvalidOperationException("boom"));

        _formatterFactory.GetFormatter(Arg.Any<FormattingContext>()).Returns((formatter, false));

        var writer = new FormattedLogWriter(_config, _formatterFactory, _loggerFactory);

        // Act
        writer.ProcessEntry(entry);

        // Assert: safe message logged and warning with exception
        _logger.Received().Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Error")),
            null,
            Arg.Any<Func<object, Exception?, string>>());

        _logger.Received().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Failed to process log entry")),
            Arg.Any<InvalidOperationException>(),
            Arg.Any<Func<object, Exception?, string>>());
    }
    
    [Fact]
    public void FormatLogEntry_WithNonEmptyTemplateName_CallsWithTemplateName()
    {
        // Arrange
        const string testTemplateName = "MyCustomTemplate";
        
        var loggingConfig = new LoggingConfig();
        var mockFormatterFactory = Substitute.For<IMessageFormatterFactory>();
        var mockFormatter = Substitute.For<IMessageFormatter>();
        var loggerFactory = NullLoggerFactory.Instance;
        
        var logEntry = LogEntry.CreateStart("TestMethod", "TestNamespace.TestClass")
            .WithTemplate(testTemplateName);
        
        var formattedResult = FormattedMessage.Success("test message");
        
        mockFormatterFactory.GetFormatter(Arg.Any<FormattingContext>())
            .Returns((mockFormatter, false));
        
        mockFormatter.Format(Arg.Any<FormattingContext>())
            .Returns(formattedResult);
        
        var writer = new FormattedLogWriter(loggingConfig, mockFormatterFactory, loggerFactory);
        
        // Act
        writer.ProcessEntry(logEntry);
        
        // Assert - The code block should call WithTemplateName when TemplateName is not null/empty
        mockFormatterFactory.Received(1).GetFormatter(
            Arg.Is<FormattingContext>(ctx => ctx.TemplateName == testTemplateName));
    }
    
    [Fact]
    public void FormatLogEntry_WhenFallbackFormatterUsed_CallsLogDebug()
    {
        // Arrange
        var loggingConfig = new LoggingConfig();
        var mockFormatterFactory = Substitute.For<IMessageFormatterFactory>();
        var mockFormatter = Substitute.For<IMessageFormatter>();
        var mockLogger = Substitute.For<ILogger>();
        var mockLoggerFactory = Substitute.For<ILoggerFactory>();
    
        var logEntry = LogEntry.CreateStart("TestMethod", "TestNamespace.TestClass");
        var testEntryId = logEntry.Id;
    
        // Setup: Factory returns fallback formatter (isFallback = true)
        mockFormatterFactory.GetFormatter(Arg.Any<FormattingContext>())
            .Returns((mockFormatter, true));
    
        // Setup: Formatter returns successful result
        var successResult = FormattedMessage.Success("test message");
        mockFormatter.Format(Arg.Any<FormattingContext>())
            .Returns(successResult);
    
        // Setup: Logger factory returns our mock logger for FormattedLogWriter
        mockLoggerFactory.CreateLogger(nameof(FormattedLogWriter))
            .Returns(mockLogger);
    
        var writer = new FormattedLogWriter(loggingConfig, mockFormatterFactory, mockLoggerFactory);
    
        // Act
        writer.ProcessEntry(logEntry);
    
        // Assert - Verify LogDebug was called with fallback information
        mockLogger.Received(1).Log(
            LogLevel.Debug,
            Arg.Any<EventId>(),
            Arg.Is<object>(state => state.ToString()!.Contains($"Used fallback formatting for entry {testEntryId}: Primary formatter not available")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }
    
    [Theory]
    [InlineData(LogLevel.Trace)]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Information)]
    [InlineData(LogLevel.Warning)]
    [InlineData(LogLevel.Error)]
    [InlineData(LogLevel.Critical)]
    [InlineData(LogLevel.None)]
    public void ProcessEntry_WithDifferentLogLevels_CallsCorrectLogWriter(LogLevel logLevel)
    {
        // Arrange
        var loggingConfig = new LoggingConfig();
        var mockFormatterFactory = Substitute.For<IMessageFormatterFactory>();
        var mockFormatter = Substitute.For<IMessageFormatter>();
        var mockLogger = Substitute.For<ILogger>();
        var mockLoggerFactory = Substitute.For<ILoggerFactory>();

        var logEntry = LogEntry.CreateStart("TestMethod", "TestNamespace.TestClass", logLevel);
    
        var formattedResult = FormattedMessage.Success("test message");
    
        mockFormatterFactory.GetFormatter(Arg.Any<FormattingContext>())
            .Returns((mockFormatter, false));
    
        mockFormatter.Format(Arg.Any<FormattingContext>())
            .Returns(formattedResult);
    
        mockLoggerFactory.CreateLogger("TestNamespace.TestClass")
            .Returns(mockLogger);
    
        mockLogger.IsEnabled(logLevel).Returns(true);
    
        var writer = new FormattedLogWriter(loggingConfig, mockFormatterFactory, mockLoggerFactory);
    
        // Act
        writer.ProcessEntry(logEntry);
    
        // Assert - Verify the logger was called with the correct log level
        if (logLevel == LogLevel.None)
        {
            mockLogger.DidNotReceive().Log(
                Arg.Any<LogLevel>(),
                Arg.Any<EventId>(),
                Arg.Any<object>(),
                Arg.Any<Exception?>(),
                Arg.Any<Func<object, Exception?, string>>());
        }
        else
        {
            mockLogger.Received(1).Log(
                logLevel,
                Arg.Any<EventId>(),
                Arg.Any<object>(),
                null,
                Arg.Any<Func<object, Exception?, string>>());
        }
    }
    
    [Fact]
    public void ProcessEntry_WithInputParametersAndFallbackFormatting_ReplacesInputParametersPlaceholder()
    {
        // Arrange
        var loggingConfig = new LoggingConfig 
        { 
            EnableFallbackFormatting = true,
            FallbackTemplate = "Method {MethodName} failed with input {InputParameters}"
        };
        var mockFormatterFactory = Substitute.For<IMessageFormatterFactory>();
        var mockFormatter = Substitute.For<IMessageFormatter>();
        var mockLogger = Substitute.For<ILogger>();
        var mockLoggerFactory = Substitute.For<ILoggerFactory>();
    
        var inputParams = new { userId = 123, email = "test@example.com" };
        var logEntry = LogEntry.CreateStart("TestMethod", "TestNamespace.TestClass")
            .WithInput(inputParams);
    
        // Setup formatter to fail so we trigger fallback formatting
        var failedResult = FormattedMessage.Failure("Formatter failed");
    
        mockFormatterFactory.GetFormatter(Arg.Any<FormattingContext>())
            .Returns((mockFormatter, false));
    
        mockFormatter.Format(Arg.Any<FormattingContext>())
            .Returns(failedResult);
    
        mockLoggerFactory.CreateLogger(Arg.Any<string>())
            .Returns(mockLogger);
    
        mockLogger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
    
        var writer = new FormattedLogWriter(loggingConfig, mockFormatterFactory, mockLoggerFactory);
    
        // Act
        writer.ProcessEntry(logEntry);
    
        // Assert - Verify the message contains the replaced InputParameters
        mockLogger.Received(1).Log(
            Arg.Any<LogLevel>(),
            Arg.Any<EventId>(),
            Arg.Is<object>(state => state.ToString()!.Contains(inputParams.ToString()!)),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }
}
