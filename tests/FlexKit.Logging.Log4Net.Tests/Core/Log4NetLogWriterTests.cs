using FlexKit.Logging.Configuration;
using FlexKit.Logging.Formatting.Core;
using FlexKit.Logging.Formatting.Models;
using FlexKit.Logging.Log4Net.Core;
using FlexKit.Logging.Models;
using log4net.Core;
using log4net.Repository;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;
using ILogger = log4net.Core.ILogger;

namespace FlexKit.Logging.Log4Net.Tests.Core;

public class Log4NetLogWriterTests
{
    private readonly LoggingConfig _config;
    private readonly IMessageFormatterFactory _mockFormatterFactory;
    private readonly ILoggerRepository _mockRepository;
    private readonly Log4NetLogWriter _writer;
    private readonly IMessageFormatter _mockFormatter;
    private readonly ILogger _mockLog4NetLogger;

    public Log4NetLogWriterTests()
    {
        _config = new LoggingConfig
        {
            DefaultTarget = "DefaultTarget",
            EnableFallbackFormatting = true,
            FallbackTemplate = "Method {TypeName}.{MethodName} - Status: {Success}"
        };
        
        _mockFormatterFactory = Substitute.For<IMessageFormatterFactory>();
        _mockRepository = Substitute.For<ILoggerRepository>();
        _mockFormatter = Substitute.For<IMessageFormatter>();
        _mockLog4NetLogger = Substitute.For<ILogger>();
        
        // Setup default repository behavior
        _mockRepository.GetLogger(Arg.Any<string>()).Returns(_mockLog4NetLogger);
        _mockLog4NetLogger.IsEnabledFor(Arg.Any<Level>()).Returns(true);
        _mockLog4NetLogger.Name.Returns("TestLogger");
        
        // Setup default formatter behavior
        _mockFormatterFactory.GetFormatter(Arg.Any<FormattingContext>())
            .Returns((_mockFormatter, false));
            
        _writer = new Log4NetLogWriter(_config, _mockFormatterFactory, _mockRepository);
    }
    
    [Fact]
    public void ProcessEntry_WhenTemplateNameNotNullOrEmpty_ExceptionMessageNull_TargetNotNull_ResultIsSuccess()
    {
        // Arrange
        var entry = LogEntry.CreateStart("TestMethod", "TestType")
            .WithTemplate("CustomTemplate")
            .WithTarget("CustomTarget")
            .WithCompletion(success: true, durationTicks: 1000);
        
        var successResult = FormattedMessage.Success("Formatted message");
        _mockFormatter.Format(Arg.Any<FormattingContext>()).Returns(successResult);

        // Act
        _writer.ProcessEntry(entry); 

        // Assert
        _mockFormatterFactory.Received(1).GetFormatter(Arg.Is<FormattingContext>(ctx => 
            ctx.TemplateName == "CustomTemplate"));
        _mockFormatter.Received(1).Format(Arg.Any<FormattingContext>());
        _mockRepository.Received(1).GetLogger("CustomTarget");
        _mockLog4NetLogger.Received(1).Log(Arg.Is<LoggingEvent>(e => 
            e.RenderedMessage == "Formatted message" &&
            e.Level == Level.Info));
    }
    
    [Fact]
    public void ProcessEntry_WhenTemplateNameNullOrEmpty_ExceptionMessageNotNull_TargetNull_DefaultTargetNotNull_ResultIsSuccess()
    {
        // Arrange
        var entry = LogEntry.CreateStart("TestMethod", "TestType")
            .WithCompletion(success: true, durationTicks: 1000, exception: new InvalidOperationException("Test exception"));
        
        var successResult = FormattedMessage.Success("Formatted message");
        _mockFormatter.Format(Arg.Any<FormattingContext>()).Returns(successResult);

        // Act
        _writer.ProcessEntry(entry);

        // Assert
        _mockFormatterFactory.Received(1).GetFormatter(Arg.Is<FormattingContext>(ctx => 
            string.IsNullOrEmpty(ctx.TemplateName)));
        _mockFormatter.Received(1).Format(Arg.Any<FormattingContext>());
        _mockRepository.Received(1).GetLogger("DefaultTarget"); // Uses Config.DefaultTarget since entry.Target is null
        _mockLog4NetLogger.Received(1).Log(Arg.Is<LoggingEvent>(e => 
            e.RenderedMessage == "Formatted message" &&
            e.Level == Level.Error)); // Uses ExceptionLevel because ExceptionMessage is not null
    }
    
    [Fact]
    public void ProcessEntry_WhenExceptionThrown_CallsHandleProcessingError()
    {
        // Arrange  
        var entry = LogEntry.CreateStart("TestMethod", "TestType")
            .WithTarget("TestTarget")
            .WithCompletion(success: true, durationTicks: 1000);

        var testException = new InvalidOperationException("Test exception");
        _mockFormatter.Format(Arg.Any<FormattingContext>()).Throws(testException);

        // Setup additional logger for error handling
        var errorLogger = Substitute.For<ILogger>();
        errorLogger.Name.Returns("Log4NetLogWriter");
        _mockRepository.GetLogger("Log4NetLogWriter").Returns(errorLogger);

        // Act
        _writer.ProcessEntry(entry);

        // Assert
        // Verify the main logger was called for the safe error message
        _mockLog4NetLogger.Received(1).Log(Arg.Is<LoggingEvent>(e => 
            e.RenderedMessage == "[Error] Method TestType.TestMethod - Success: True"));
        
        // Verify error logger was called for the exception handling
        errorLogger.Received(1).Log(Arg.Is<LoggingEvent>(e => 
            e.LoggerName == "Log4NetLogWriter" &&
            e.Level == Level.Error &&
            e.RenderedMessage!.Contains("Failed to process log entry")));
    }
    
    [Fact]
    public void ProcessEntry_WhenTemplateNameEmpty_ExceptionMessageNotNull_TargetNull_DefaultTargetNull_ResultIsFailure_FallbackEnabled_InputParametersNull()
    {
        // Arrange
        _config.DefaultTarget = null;
        _config.EnableFallbackFormatting = true;
    
        var entry = LogEntry.CreateStart("TestMethod", "TestType")
            .WithCompletion(success: false, durationTicks: 1000, exception: new InvalidOperationException("Test exception"));
        
        var failureResult = FormattedMessage.Failure("Formatting failed");
        _mockFormatter.Format(Arg.Any<FormattingContext>()).Returns(failureResult);

        // Act
        _writer.ProcessEntry(entry);

        // Assert
        _mockFormatterFactory.Received(1).GetFormatter(Arg.Is<FormattingContext>(ctx => 
            string.IsNullOrEmpty(ctx.TemplateName)));
        _mockFormatter.Received(1).Format(Arg.Any<FormattingContext>());
        _mockRepository.Received(1).GetLogger("TestType"); // Falls back to entry.TypeName since both Target and DefaultTarget are null
        _mockLog4NetLogger.Received(1).Log(Arg.Is<LoggingEvent>(e => 
            e.RenderedMessage == "Method TestType.TestMethod - Status: False" && // Fallback template without InputParameters
            e.Level == Level.Error)); // Uses ExceptionLevel because ExceptionMessage is not null
    }
    
    [Fact]
    public void ProcessEntry_WhenResultIsFailure_FallbackEnabled_InputParametersNotEmpty()
    {
        // Arrange
        _config.EnableFallbackFormatting = true;
        _config.FallbackTemplate = "Method {TypeName}.{MethodName} - Status: {Success} - Input: {InputParameters}";
    
        var inputParams = new { UserId = 123, Name = "Test" };
        var entry = LogEntry.CreateStart("TestMethod", "TestType")
            .WithInput(inputParams)
            .WithTarget("TestTarget")
            .WithCompletion(success: true, durationTicks: 1000);
        
        var failureResult = FormattedMessage.Failure("Formatting failed");
        _mockFormatter.Format(Arg.Any<FormattingContext>()).Returns(failureResult);

        // Act
        _writer.ProcessEntry(entry);

        // Assert
        _mockFormatter.Received(1).Format(Arg.Any<FormattingContext>());
        _mockRepository.Received(1).GetLogger("TestTarget");
        _mockLog4NetLogger.Received(1).Log(Arg.Is<LoggingEvent>(e => 
            e.RenderedMessage!.Contains("Method TestType.TestMethod - Status: True") &&
            e.RenderedMessage.Contains(inputParams.ToString()!) && // Fallback template includes InputParameters
            e.Level == Level.Info)); // Uses entry.Level since ExceptionMessage is null
    }
    
    [Fact]
    public void ProcessEntry_WhenResultIsFailure_FallbackDisabled()
    {
        // Arrange
        _config.EnableFallbackFormatting = false;
    
        var entry = LogEntry.CreateStart("TestMethod", "TestType")
            .WithTarget("TestTarget")
            .WithCompletion(success: true, durationTicks: 1000);
        
        var failureResult = FormattedMessage.Failure("Custom error message");
        _mockFormatter.Format(Arg.Any<FormattingContext>()).Returns(failureResult);

        // Act
        _writer.ProcessEntry(entry);

        // Assert
        _mockFormatter.Received(1).Format(Arg.Any<FormattingContext>());
        _mockRepository.Received(1).GetLogger("TestTarget");
        _mockLog4NetLogger.Received(1).Log(Arg.Is<LoggingEvent>(e => 
            e.RenderedMessage == "[Formatting Error: Custom error message]" &&
            e.Level == Level.Info));
    }
    
    [Fact]
    public void ProcessEntry_ConvertLogLevel_AllSwitchCases()
    {
        // Arrange - Test data for all LogLevel cases
        var testCases = new[]
        {
            (LogLevel.Trace, Level.Trace),
            (LogLevel.Debug, Level.Debug),
            (LogLevel.Information, Level.Info),
            (LogLevel.Warning, Level.Warn),
            (LogLevel.Error, Level.Error),
            (LogLevel.Critical, Level.Fatal),
            (LogLevel.None, Level.Off)
        };

        var successResult = FormattedMessage.Success("Test message");
        _mockFormatter.Format(Arg.Any<FormattingContext>()).Returns(successResult);

        foreach (var (inputLevel, expectedLevel) in testCases)
        {
            // Arrange for this iteration
            var entry = LogEntry.CreateStart("TestMethod", "TestType", inputLevel)
                .WithTarget("TestTarget")
                .WithCompletion(success: true, durationTicks: 1000);

            // Act
            _writer.ProcessEntry(entry);

            // Assert
            _mockLog4NetLogger.Received().Log(Arg.Is<LoggingEvent>(e => e.Level == expectedLevel));
        
            // Clear received calls for the next iteration
            _mockLog4NetLogger.ClearReceivedCalls();
        }
    }
    
    [Fact]
    public void ProcessEntry_WhenLoggerIsNotEnabledForLevel_DoesNotLog()
    {
        // Arrange
        var entry = LogEntry.CreateStart("TestMethod", "TestType")
            .WithTarget("TestTarget")
            .WithCompletion(success: true, durationTicks: 1000);
        
        var successResult = FormattedMessage.Success("Test message");
        _mockFormatter.Format(Arg.Any<FormattingContext>()).Returns(successResult);
    
        // Configure logger to not be enabled for Info level
        _mockLog4NetLogger.IsEnabledFor(Level.Info).Returns(false);

        // Act
        _writer.ProcessEntry(entry);

        // Assert
        _mockFormatter.Received(1).Format(Arg.Any<FormattingContext>()); // Formatting still happens
        _mockRepository.Received(1).GetLogger("TestTarget"); // Logger is still retrieved
        _mockLog4NetLogger.Received(1).IsEnabledFor(Level.Info); // Level check is performed
        _mockLog4NetLogger.DidNotReceive().Log(Arg.Any<LoggingEvent>()); // But no logging occurs
    }
}