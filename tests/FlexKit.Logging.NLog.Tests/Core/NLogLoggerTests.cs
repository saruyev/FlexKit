using FlexKit.Logging.Configuration;
using FlexKit.Logging.NLog.Core;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;
using NLog;
using NLog.Config;
using NLog.Targets;
using NLogLevel = NLog.LogLevel;
using MSLogLevel = Microsoft.Extensions.Logging.LogLevel;
// ReSharper disable FlagArgument

namespace FlexKit.Logging.NLog.Tests.Core;

[Collection("NLogLoggerTests")]
public class NLogLoggerTests : IDisposable
{
    private readonly LoggingConfig _config;
    private readonly NLogLogger _logger;
    private readonly MemoryTarget _memoryTarget;
    private readonly LoggingConfiguration _nlogConfig;

    public NLogLoggerTests()
    {
        // Set up real NLog configuration with MemoryTarget for testing
        _nlogConfig = new LoggingConfiguration();
        _memoryTarget = new MemoryTarget("memory");
        _nlogConfig.AddTarget(_memoryTarget);
        _nlogConfig.AddRule(NLogLevel.Trace, NLogLevel.Fatal, _memoryTarget);
        LogManager.Configuration = _nlogConfig;

        _config = new LoggingConfig();
        _logger = new NLogLogger("TestCategory", _config);
    }

    public void Dispose()
    {
        LogManager.Configuration = null;
        _memoryTarget.Dispose();
    }

    [Fact]
    public void BeginScope_VerifyThatNullScopeInstanceIsDisposed()
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

    [Fact]
    public void Log_WhenLogLevelBelowSuppressedLevel_DoesNotLog()
    {
        // Arrange
        _config.SuppressedCategories.Clear();
        _config.SuppressedCategories.Add("TestCategory"); // Matches our logger's category
        _config.SuppressedLogLevel = MSLogLevel.Warning; // Set minimum level to Warning
    
        var logLevel = MSLogLevel.Information; // Below Warning, so should be suppressed
        var eventId = new EventId(1);
        var state = "Test message";

        // Act
        _logger.Log(logLevel, eventId, state, null, (s, _) => s);

        // Assert - Verify no messages were logged due to suppression
        _memoryTarget.Logs.Should().BeEmpty();
    }

    [Fact] 
    public void Log_WhenStateIsIEnumerableKeyValuePairIsFalse()
    {
        // Arrange
        var logLevel = MSLogLevel.Information;
        var eventId = new EventId(1);
        var state = "Simple string state"; // Not IEnumerable<KeyValuePair<string, object?>>

        // Act
        _logger.Log(logLevel, eventId, state, null, (s, _) => s);

        // Assert
        _memoryTarget.Logs.Should().HaveCount(1);
        _memoryTarget.Logs[0].Should().EndWith("INFO|TestCategory|Simple string state");
    }

    [Fact]
    public void Log_WhenPropertiesIsEmpty()
    {
        // Arrange
        var logLevel = MSLogLevel.Information;
        var eventId = new EventId(1);
        var properties = new List<KeyValuePair<string, object?>>(); // Empty properties

        // Act
        _logger.Log(logLevel, eventId, properties, null, (_, _) => "formatted");

        // Assert - Verify message was logged
        _memoryTarget.Logs.Should().HaveCount(1);
        _memoryTarget.Logs[0].Should().EndWith("|INFO|TestCategory|formatted");
    }

    [Fact]
    public void Log_WhenOnePropertyKeyNotOriginalFormatIsFalseAndAnotherIsTrue_OnlyAddsNonOriginalFormatProperty()
    {
        // Arrange - Configure NLog to output properties in the message
        _nlogConfig.LoggingRules.Clear();
        _memoryTarget.Layout = @"${message} | CustomProperty: ${event-properties:CustomProperty} | OriginalFormat: ${event-properties:{OriginalFormat}}";
        _nlogConfig.AddRule(NLogLevel.Trace, NLogLevel.Fatal, _memoryTarget);
        LogManager.ReconfigExistingLoggers();

        var logLevel = MSLogLevel.Information;
        var eventId = new EventId(1);
        var properties = new List<KeyValuePair<string, object?>>
        {
            new("{OriginalFormat}", "template"), // Should be excluded
            new("CustomProperty", "value1")      // Should be included
        };

        // Act
        _logger.Log(logLevel, eventId, properties, null, (_, _) => "formatted");

        // Assert - Verify the output shows CustomProperty but not OriginalFormat
        _memoryTarget.Logs.Should().HaveCount(1);
        _memoryTarget.Logs[0].Should().EndWith("formatted | CustomProperty: value1 | OriginalFormat: }");
        //                                                              ^^^^^^^ present    ^^^^^ empty (filtered out)
    }

    [Theory]
    [InlineData(MSLogLevel.Trace, "Trace")]
    [InlineData(MSLogLevel.Debug, "Debug")]
    [InlineData(MSLogLevel.Information, "Info")]
    [InlineData(MSLogLevel.Warning, "Warn")]
    [InlineData(MSLogLevel.Error, "Error")]
    [InlineData(MSLogLevel.Critical, "Fatal")]
    [InlineData(MSLogLevel.None, "Off")]
    public void ConvertLogLevel_TheoryCoveringAllCasesInSwitchExpression(MSLogLevel inputLevel, string expected)
    {
        // Arrange
        var eventId = new EventId(1);
        var state = "Test message";
    
        // For None level, temporarily disable the rule to test conversion
        if (inputLevel == MSLogLevel.None)
        {
            _nlogConfig.LoggingRules.Clear();
            _nlogConfig.AddRule(NLogLevel.Off, NLogLevel.Off, _memoryTarget);
            LogManager.ReconfigExistingLoggers();
        }
    
        // Act
        _logger.Log(inputLevel, eventId, state, null, (s, _) => s);
    
        // Assert - For None/Off level, nothing should be logged
        if (inputLevel == MSLogLevel.None)
        {
            _memoryTarget.Logs.Should().BeEmpty();
        }
        else
        {
            _memoryTarget.Logs.Should().HaveCount(1);
            _memoryTarget.Logs[0].Should().EndWith($"|{expected.ToUpper()}|TestCategory|Test message");
        }
    }
}