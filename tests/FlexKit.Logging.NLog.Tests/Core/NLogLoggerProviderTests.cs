using FlexKit.Logging.Configuration;
using FlexKit.Logging.NLog.Core;
using FluentAssertions;
using NLog;
using NLog.Config;
using NLog.Targets;
using Xunit;
using NLogLevel = NLog.LogLevel;

namespace FlexKit.Logging.NLog.Tests.Core;

/// <summary>
/// Unit tests for NLogLoggerProvider class.
/// </summary>
public class NLogLoggerProviderTests : IDisposable
{
    private readonly LoggingConfig _config;
    private readonly NLogLoggerProvider _provider;

    public NLogLoggerProviderTests()
    {
        // Set up minimal NLog configuration to avoid exceptions
        var nlogConfig = new LoggingConfiguration();
        var memoryTarget = new MemoryTarget("memory");
        nlogConfig.AddTarget(memoryTarget);
        nlogConfig.AddRule(NLogLevel.Trace, NLogLevel.Fatal, memoryTarget);
        LogManager.Configuration = nlogConfig;

        _config = new LoggingConfig();
        _provider = new NLogLoggerProvider(_config);
    }

    public void Dispose()
    {
        _provider.Dispose();
        LogManager.Configuration = null;
    }

    [Fact]
    public void CreateLogger_WithValidCategoryName_ReturnsNLogLogger()
    {
        // Arrange
        var categoryName = "TestCategory";

        // Act
        var logger = _provider.CreateLogger(categoryName);

        // Assert
        logger.Should().NotBeNull();
        logger.Should().BeOfType<NLogLogger>();
    }

    [Fact]
    public void CreateLogger_CalledMultipleTimes_CreatesNewInstancesEachTime()
    {
        // Arrange
        var categoryName = "TestCategory";

        // Act
        var logger1 = _provider.CreateLogger(categoryName);
        var logger2 = _provider.CreateLogger(categoryName);
        var logger3 = _provider.CreateLogger(categoryName);

        // Assert
        logger1.Should().NotBeNull();
        logger2.Should().NotBeNull();
        logger3.Should().NotBeNull();
        
        // Each call should create a new instance
        logger1.Should().NotBeSameAs(logger2);
        logger2.Should().NotBeSameAs(logger3);
        logger1.Should().NotBeSameAs(logger3);
    }

    [Fact]
    public void CreateLogger_WithDifferentCategoryNames_ReturnsDifferentLoggers()
    {
        // Arrange
        var categoryName1 = "Category1";
        var categoryName2 = "Category2";

        // Act
        var logger1 = _provider.CreateLogger(categoryName1);
        var logger2 = _provider.CreateLogger(categoryName2);

        // Assert
        logger1.Should().NotBeNull();
        logger2.Should().NotBeNull();
        logger1.Should().NotBeSameAs(logger2);
    }

    [Fact]
    public void Dispose_WhenCalled_DoesNotThrow()
    {
        // Act
        var disposing = () => _provider.Dispose();

        // Assert
        disposing.Should().NotThrow();
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        // Act & Assert - multiple dispose calls should be safe
        var disposing1 = () => _provider.Dispose();
        var disposing2 = () => _provider.Dispose();
        var disposing3 = () => _provider.Dispose();
        
        disposing1.Should().NotThrow();
        disposing2.Should().NotThrow();
        disposing3.Should().NotThrow();
    }

    [Fact]
    public void CreateLogger_AfterDispose_StillWorks()
    {
        // Arrange
        var categoryName = "TestCategory";

        // Act
        _provider.Dispose();
        var logger = _provider.CreateLogger(categoryName);

        // Assert
        logger.Should().NotBeNull();
        logger.Should().BeOfType<NLogLogger>();
    }
}