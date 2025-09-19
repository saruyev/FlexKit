using FlexKit.Logging.Configuration;
using FlexKit.Logging.Log4Net.Core;
using FluentAssertions;
using log4net.Repository;
using NSubstitute;
using Xunit;

namespace FlexKit.Logging.Log4Net.Tests.Core;

public class Log4NetLoggerProviderTests
{
    private readonly ILoggerRepository _mockRepository;
    private readonly LoggingConfig _config;
    private readonly Log4NetLoggerProvider _provider;

    public Log4NetLoggerProviderTests()
    {
        _mockRepository = Substitute.For<ILoggerRepository>();
        _config = new LoggingConfig();
        _provider = new Log4NetLoggerProvider(_mockRepository, _config);
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange & Act
        var provider = new Log4NetLoggerProvider(_mockRepository, _config);

        // Assert
        provider.Should().NotBeNull();
        provider.Should().BeOfType<Log4NetLoggerProvider>();
    }

    [Fact]
    public void CreateLogger_WithValidCategoryName_ReturnsLog4NetLogger()
    {
        // Arrange
        var categoryName = "TestCategory";
        var mockLog4NetLogger = Substitute.For<log4net.Core.ILogger>();
        _mockRepository.GetLogger(categoryName).Returns(mockLog4NetLogger);

        // Act
        var logger = _provider.CreateLogger(categoryName);

        // Assert
        logger.Should().NotBeNull();
        logger.Should().BeOfType<Log4NetLogger>();
        _mockRepository.Received(1).GetLogger(categoryName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Microsoft.AspNetCore")]
    [InlineData("MyApp.Services.PaymentService")]
    [InlineData("System.Net.Http")]
    [InlineData("A.Very.Long.Namespace.With.Many.Segments.TestLogger")]
    public void CreateLogger_WithVariousCategoryNames_ReturnsLog4NetLogger(string categoryName)
    {
        // Arrange
        var mockLog4NetLogger = Substitute.For<log4net.Core.ILogger>();
        _mockRepository.GetLogger(categoryName).Returns(mockLog4NetLogger);

        // Act
        var logger = _provider.CreateLogger(categoryName);

        // Assert
        logger.Should().NotBeNull();
        logger.Should().BeOfType<Log4NetLogger>();
        _mockRepository.Received(1).GetLogger(categoryName);
    }

    [Fact]
    public void CreateLogger_WithNullCategoryName_CallsRepositoryWithNull()
    {
        // Arrange
        string categoryName = null!;
        var mockLog4NetLogger = Substitute.For<log4net.Core.ILogger>();
        _mockRepository.GetLogger(categoryName).Returns(mockLog4NetLogger);

        // Act
        var logger = _provider.CreateLogger(categoryName);

        // Assert
        logger.Should().NotBeNull();
        logger.Should().BeOfType<Log4NetLogger>();
        _mockRepository.Received(1).GetLogger(categoryName);
    }

    [Fact]
    public void CreateLogger_CalledMultipleTimes_CreatesNewInstancesEachTime()
    {
        // Arrange
        var categoryName = "TestCategory";
        var mockLog4NetLogger = Substitute.For<log4net.Core.ILogger>();
        _mockRepository.GetLogger(categoryName).Returns(mockLog4NetLogger);

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
        
        // Repository should be called for each creation
        _mockRepository.Received(3).GetLogger(categoryName);
    }

    [Fact]
    public void CreateLogger_WithDifferentCategoryNames_CallsRepositoryForEach()
    {
        // Arrange
        var categoryName1 = "Category1";
        var categoryName2 = "Category2";
        var mockLogger1 = Substitute.For<log4net.Core.ILogger>();
        var mockLogger2 = Substitute.For<log4net.Core.ILogger>();
        
        _mockRepository.GetLogger(categoryName1).Returns(mockLogger1);
        _mockRepository.GetLogger(categoryName2).Returns(mockLogger2);

        // Act
        var logger1 = _provider.CreateLogger(categoryName1);
        var logger2 = _provider.CreateLogger(categoryName2);

        // Assert
        logger1.Should().NotBeNull();
        logger2.Should().NotBeNull();
        
        _mockRepository.Received(1).GetLogger(categoryName1);
        _mockRepository.Received(1).GetLogger(categoryName2);
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
        var mockLog4NetLogger = Substitute.For<log4net.Core.ILogger>();
        _mockRepository.GetLogger(categoryName).Returns(mockLog4NetLogger);

        // Act
        _provider.Dispose();
        var logger = _provider.CreateLogger(categoryName);

        // Assert
        logger.Should().NotBeNull();
        logger.Should().BeOfType<Log4NetLogger>();
        _mockRepository.Received(1).GetLogger(categoryName);
    }

    [Fact]
    public void Constructor_WithNullRepository_AllowsCreation()
    {
        // Arrange & Act
        var creating = () => new Log4NetLoggerProvider(null!, _config);

        // Assert
        creating.Should().NotThrow("Constructor allows null repository");
    }

    [Fact]
    public void Constructor_WithNullConfig_AllowsCreation()
    {
        // Arrange & Act  
        var creating = () => new Log4NetLoggerProvider(_mockRepository, null!);

        // Assert
        creating.Should().NotThrow("Constructor allows null config");
    }

    [Fact]
    public void CreateLogger_WithNullRepositoryAndConfig_ThrowsWhenCreatingLogger()
    {
        // Arrange
        var provider = new Log4NetLoggerProvider(null!, null!);

        // Act & Assert
        var creating = () => provider.CreateLogger("TestCategory");
        creating.Should().Throw<Exception>("Log4NetLogger constructor should fail with null dependencies");
    }
}