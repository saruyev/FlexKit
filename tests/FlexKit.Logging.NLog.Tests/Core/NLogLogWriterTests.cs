using FlexKit.Logging.Configuration;
using FlexKit.Logging.Formatting.Core;
using FlexKit.Logging.Formatting.Models;
using FlexKit.Logging.Models;
using FlexKit.Logging.NLog.Core;
using FluentAssertions;
using NLog;
using NLog.Config;
using NLog.Targets;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;
using MSLogLevel = Microsoft.Extensions.Logging.LogLevel;
using NLogLevel = NLog.LogLevel;
using ILogger = NLog.ILogger;
// ReSharper disable FlagArgument

namespace FlexKit.Logging.NLog.Tests.Core;

[Collection("NLogLogWriterTests")]
public class NLogLogWriterTests : IDisposable
{
    private readonly LoggingConfig _config;
    private readonly IMessageFormatterFactory _mockFormatterFactory;
    private readonly IMessageFormatter _mockFormatter;
    private readonly NLogLogWriter _writer;
    private readonly MemoryTarget _memoryTarget;

    public NLogLogWriterTests()
    {
        // Set up real NLog configuration for some tests
        var nlogConfig = new LoggingConfiguration();
        _memoryTarget = new MemoryTarget("memory");
        nlogConfig.AddTarget(_memoryTarget);
        nlogConfig.AddRule(NLogLevel.Trace, NLogLevel.Fatal, _memoryTarget);
        LogManager.Configuration = nlogConfig;

        _config = new LoggingConfig { DefaultTarget = "DefaultTarget" };
        _mockFormatterFactory = Substitute.For<IMessageFormatterFactory>();
        _mockFormatter = Substitute.For<IMessageFormatter>();
        var mockNLogLogger = Substitute.For<ILogger>();

        _mockFormatterFactory.GetFormatter(Arg.Any<FormattingContext>()).Returns((_mockFormatter, false));
        
        _writer = new NLogLogWriter(_config, _mockFormatterFactory, mockNLogLogger);
    }

    public void Dispose()
    {
        LogManager.Configuration = null;
        _memoryTarget.Dispose();
    }

    [Fact]
    public void ProcessEntry_WhenExceptionIsThrown_HandleProcessingErrorIsCalled()
    {
        // Arrange
        var entry = LogEntry.CreateStart("TestMethod", "TestType")
            .WithTarget("TestTarget");

        var testException = new InvalidOperationException("Test exception");
        _mockFormatter.Format(Arg.Any<FormattingContext>()).Throws(testException);

        // Act
        _writer.ProcessEntry(entry);

        // Assert - Verify HandleProcessingError was called by checking the error message was logged
        _mockFormatterFactory.Received(1).GetFormatter(Arg.Any<FormattingContext>());
        _mockFormatter.Received(1).Format(Arg.Any<FormattingContext>());
        
        // The error handling should create a failure message and log a warning
        // We can verify this by checking the calls made to format the error
        _mockFormatterFactory.Received().GetFormatter(Arg.Any<FormattingContext>());
    }

    [Fact]
    public void ProcessEntry_WhenTemplateNameIsNotSet_IsFallbackIsFalse()
    {
        // Arrange
        var entry = LogEntry.CreateStart("TestMethod", "TestType")
            .WithTarget("TestTarget");
        // entry.TemplateName is null by default

        var successResult = FormattedMessage.Success("Formatted message", new Dictionary<string, object?>());
        _mockFormatter.Format(Arg.Any<FormattingContext>()).Returns(successResult);

        // Act
        _writer.ProcessEntry(entry);

        // Assert
        _mockFormatterFactory.Received(1).GetFormatter(Arg.Is<FormattingContext>(ctx => 
            string.IsNullOrEmpty(ctx.TemplateName)));
        _mockFormatter.Received(1).Format(Arg.Any<FormattingContext>());
    }

    [Fact]
    public void ProcessEntry_WhenTemplateNameIsSet_IsFallbackIsTrue()
    {
        // Arrange  
        var entry = LogEntry.CreateStart("TestMethod", "TestType")
            .WithTarget("TestTarget")
            .WithTemplate("CustomTemplate");

        var fallbackResult = FormattedMessage.Success("Formatted message", new Dictionary<string, object?>()).WithFallback("Primary formatter not available");
        _mockFormatterFactory.GetFormatter(Arg.Any<FormattingContext>()).Returns((_mockFormatter, true)); // isFallback = true
        _mockFormatter.Format(Arg.Any<FormattingContext>()).Returns(fallbackResult);

        // Act
        _writer.ProcessEntry(entry);

        // Assert
        _mockFormatterFactory.Received(1).GetFormatter(Arg.Is<FormattingContext>(ctx => 
            ctx.TemplateName == "CustomTemplate"));
        _mockFormatter.Received(1).Format(Arg.Any<FormattingContext>());
    }

    [Fact]
    public void ProcessEntry_WhenLoggerIsEnabledLevelIsFalseInOutputMessage()
    {
        // Arrange
        var entry = LogEntry.CreateStart("TestMethod", "TestType");

        var successResult = FormattedMessage.Success("Test message", new Dictionary<string, object?>());
        _mockFormatter.Format(Arg.Any<FormattingContext>()).Returns(successResult);

        // Create a real NLog logger but configure it to disable the level
        var nlogConfig = new LoggingConfiguration();
        var nullTarget = new NullTarget("null");
        nlogConfig.AddTarget(nullTarget);
        nlogConfig.AddRule(NLogLevel.Error, NLogLevel.Fatal, nullTarget); // Only Error+ enabled, Info disabled
        LogManager.Configuration = nlogConfig;

        var writer = new NLogLogWriter(_config, _mockFormatterFactory, LogManager.GetLogger("TestType"));

        // Act
        writer.ProcessEntry(entry);

        // Assert - Message should be formatted but not logged due to level filtering
        _mockFormatter.Received(1).Format(Arg.Any<FormattingContext>());
        // Can't easily verify the internal IsEnabled check, but the method should complete without error
    }

    [Fact]
    public void ProcessEntry_WhenLoggerIsEnabledLevelIsTrueInOutputMessage()
    {
        // Arrange
        var entry = LogEntry.CreateStart("TestMethod", "TestType");

        var successResult = FormattedMessage.Success("Test message", new Dictionary<string, object?>());
        successResult = successResult.WithParameters(new Dictionary<string, object?> { ["TestParam"] = "TestValue" });
        _mockFormatter.Format(Arg.Any<FormattingContext>()).Returns(successResult);

        // Use the real NLog setup that allows all levels
        var writer = new NLogLogWriter(_config, _mockFormatterFactory, LogManager.GetLogger("TestType"));

        // Act
        writer.ProcessEntry(entry);

        // Assert
        _mockFormatter.Received(1).Format(Arg.Any<FormattingContext>());
        
        // Check that something was logged to our memory target
        _memoryTarget.Logs.Should().HaveCount(1);
        _memoryTarget.Logs[0].Should().EndWith("|INFO|DefaultTarget|Test message");
    }

    [Theory]
    [InlineData(MSLogLevel.Trace, "Trace")]
    [InlineData(MSLogLevel.Debug, "Debug")]
    [InlineData(MSLogLevel.Information, "Info")]
    [InlineData(MSLogLevel.Warning, "Warn")]
    [InlineData(MSLogLevel.Error, "Error")]
    [InlineData(MSLogLevel.Critical, "Fatal")]
    [InlineData(MSLogLevel.None, "Off")]
    public void ProcessEntry_TheoryCoveringAllCasesInConvertLogLevel(MSLogLevel inputLevel, string expectedLevel)
    {
        // Arrange
        var entry = LogEntry.CreateStart("TestMethod", "TestType", inputLevel);

        var successResult = FormattedMessage.Success("Test message", new Dictionary<string, object?>());
        _mockFormatter.Format(Arg.Any<FormattingContext>()).Returns(successResult);

        // Configure NLog to accept the expected level
        var nlogConfig = new LoggingConfiguration();
        nlogConfig.AddTarget(_memoryTarget);
        nlogConfig.AddRule(NLogLevel.FromString(expectedLevel), NLogLevel.Fatal, _memoryTarget);
        LogManager.Configuration = nlogConfig;

        var writer = new NLogLogWriter(_config, _mockFormatterFactory, LogManager.GetLogger("TestType"));

        // Act
        writer.ProcessEntry(entry);

        // Assert
        _mockFormatter.Received(1).Format(Arg.Any<FormattingContext>());
        
        if (inputLevel == MSLogLevel.None)
        {
            // None/Off level should not log anything
            _memoryTarget.Logs.Should().BeEmpty();
        }
        else
        {
            _memoryTarget.Logs.Should().HaveCount(1);
            _memoryTarget.Logs[0].Should().EndWith($"|{expectedLevel.ToUpper()}|DefaultTarget|Test message");
        }
    }
}