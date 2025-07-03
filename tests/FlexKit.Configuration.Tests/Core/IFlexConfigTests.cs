using System.Dynamic;
using System.Globalization;
using Autofac;
using AutoFixture.Xunit2;
using FlexKit.Configuration.Core;
using FlexKit.Configuration.Tests.TestBase;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Xunit;
// ReSharper disable MethodTooLong
// ReSharper disable TooManyDeclarations

namespace FlexKit.Configuration.Tests.Core;

/// <summary>
/// Unit tests for IFlexConfig interface contract and implementation behavior.
/// </summary>
// ReSharper disable once InconsistentNaming
public class IFlexConfigTests : UnitTestBase
{
    private IFlexConfig _flexConfig = null!;
    private IConfiguration _mockConfiguration = null!;

    protected override void ConfigureContainer(ContainerBuilder builder)
    {
        _mockConfiguration = CreateMock<IConfiguration>();
        _flexConfig = new FlexConfiguration(_mockConfiguration);
        builder.RegisterInstance(_flexConfig).As<IFlexConfig>();
        builder.RegisterInstance(_mockConfiguration).As<IConfiguration>();
    }

    protected override void RegisterFixtureCustomizations()
    {
        // Customize string generation to avoid null values in configuration keys
        Fixture.Customize<string>(composer => composer.FromFactory(() => "test-string-" + Guid.NewGuid().ToString("N")[..8]));
    }

    [Fact]
    public void Configuration_Property_ReturnsUnderlyingConfiguration()
    {
        // Act
        var result = _flexConfig.Configuration;

        // Assert
        result.Should().BeSameAs(_mockConfiguration);
    }

    [Theory]
    [AutoData]
    public void StringIndexer_WithValidKey_ReturnsExpectedValue(string key, string expectedValue)
    {
        // Arrange
        _mockConfiguration[key].Returns(expectedValue);

        // Act
        var result = _flexConfig[key];

        // Assert
        result.Should().Be(expectedValue);
        _ = _mockConfiguration.Received(1)[key];
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void StringIndexer_WithInvalidKey_ReturnsNull(string? key)
    {
        // Act
        var result = _flexConfig[key!];

        // Assert
        result.Should().BeNull();
        _ = _mockConfiguration.DidNotReceive()[Arg.Any<string>()];
    }

    [Theory]
    [AutoData]
    public void NumericIndexer_WithValidIndex_ReturnsFlexConfig(int index)
    {
        // Arrange
        var key = index.ToString(CultureInfo.InvariantCulture);
        var mockSection = CreateMock<IConfigurationSection>();
        mockSection.Key.Returns(key);
        mockSection.Value.Returns("test-value");

        _mockConfiguration.GetChildren().Returns([mockSection]);

        // Act
        var result = _flexConfig[index];

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IFlexConfig>();
        result.Configuration.Should().BeSameAs(mockSection);
    }

    [Theory]
    [AutoData]
    public void NumericIndexer_WithNonExistentIndex_ReturnsNull(int index)
    {
        // Arrange
        _mockConfiguration.GetChildren().Returns([]);

        // Act
        var result = _flexConfig[index];

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void IDynamicMetaObjectProvider_Implementation_IsSupported()
    {
        // Act & Assert
        _flexConfig.Should().BeAssignableTo<IDynamicMetaObjectProvider>();
    }

    [Fact]
    public void DynamicAccess_ThroughInterface_WorksCorrectly()
    {
        // Arrange
        var configData = ConfigurationTestDataBuilder.CreateConfigurationDictionary();
        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(configData!);
        var configuration = builder.Build();
        
        IFlexConfig flexConfig = new FlexConfiguration(configuration);

        // Act
        dynamic config = flexConfig;
        string result = config.Application?.Name?.ToString() ?? "unexpected";

        // Assert
        result.Should().Be(configData["Application:Name"]);
    }

    [Fact]
    public void InterfaceContract_WithRealConfiguration_WorksAsExpected()
    {
        // Arrange
        var testData = new Dictionary<string, string?>
        {
            ["Database:ConnectionString"] = "Server=localhost;Database=TestDb;",
            ["Database:CommandTimeout"] = "30",
            ["Features:EnableCaching"] = "true",
            ["Api:BaseUrl"] = "https://api.example.com",
            ["Servers:0:Name"] = "Server1",
            ["Servers:0:Port"] = "8080",
            ["Servers:1:Name"] = "Server2", 
            ["Servers:1:Port"] = "8081"
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(testData);
        var configuration = builder.Build();
        
        IFlexConfig flexConfig = new FlexConfiguration(configuration);

        // Act & Assert - String indexer access
        flexConfig["Database:ConnectionString"].Should().Be(testData["Database:ConnectionString"]);
        flexConfig["Database:CommandTimeout"].Should().Be("30");
        flexConfig["Features:EnableCaching"].Should().Be("true");
        flexConfig["Api:BaseUrl"].Should().Be("https://api.example.com");

        // Act & Assert - Numeric indexer access for arrays
        var serversSection = configuration.GetSection("Servers");
        var serverFlexConfig = new FlexConfiguration(serversSection);
        
        var firstServer = serverFlexConfig[0];
        firstServer.Should().NotBeNull();
        firstServer["Name"].Should().Be("Server1");
        firstServer["Port"].Should().Be("8080");

        var secondServer = serverFlexConfig[1];
        secondServer.Should().NotBeNull();
        secondServer["Name"].Should().Be("Server2");
        secondServer["Port"].Should().Be("8081");

        // Act & Assert - Non-existent index returns null
        var nonExistentServer = serverFlexConfig[5];
        nonExistentServer.Should().BeNull();
    }

    [Fact]
    public void InterfaceContract_WithDynamicAccess_SupportsChaining()
    {
        // Arrange
        var testData = new Dictionary<string, string?>
        {
            ["External:PaymentApi:BaseUrl"] = "https://payments.example.com",
            ["External:PaymentApi:ApiKey"] = "secret-key-123",
            ["External:PaymentApi:Timeout"] = "5000",
            ["External:NotificationApi:BaseUrl"] = "https://notifications.example.com"
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(testData);
        var configuration = builder.Build();
        
        IFlexConfig flexConfig = new FlexConfiguration(configuration);

        // Act
        dynamic config = flexConfig;
        string paymentBaseUrl = config.External?.PaymentApi?.BaseUrl?.ToString() ?? "unexpected";
        string paymentApiKey = config.External?.PaymentApi?.ApiKey?.ToString() ?? "unexpected";
        string timeout = config.External?.PaymentApi?.Timeout?.ToString() ?? "unexpected";
        string notificationUrl = config.External?.NotificationApi?.BaseUrl?.ToString() ?? "unexpected";

        // Assert
        paymentBaseUrl.Should().Be("https://payments.example.com");
        paymentApiKey.Should().Be("secret-key-123");
        timeout.Should().Be("5000");
        notificationUrl.Should().Be("https://notifications.example.com");
    }

    [Fact]
    public void InterfaceContract_WithNonExistentKeys_ReturnsNullGracefully()
    {
        // Arrange
        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(new Dictionary<string, string?>());
        var configuration = builder.Build();
        
        IFlexConfig flexConfig = new FlexConfiguration(configuration);

        // Act & Assert - String indexer with non-existent keys
        flexConfig["NonExistent:Key"].Should().BeNull();
        flexConfig["Another:Missing:Value"].Should().BeNull();

        // Act & Assert - Numeric indexer with non-existent indices
        flexConfig[0].Should().BeNull();
        flexConfig[99].Should().BeNull();

        // Act & Assert - Dynamic access with non-existent properties
        dynamic config = flexConfig;
        var result = config.NonExistent?.Property?.Value?.ToString();
        result?.Should().BeNull();
    }

    [Fact]
    public void InterfaceContract_WithComplexConfiguration_HandlesAllAccessPatterns()
    {
        // Arrange
        var testData = ConfigurationTestDataBuilder.CreateConfigurationDictionary();
        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(testData!);
        var configuration = builder.Build();
        
        IFlexConfig flexConfig = new FlexConfiguration(configuration);

        // Act & Assert - Traditional access pattern
        var connectionString = flexConfig["ConnectionStrings:DefaultConnection"];
        connectionString.Should().NotBeNullOrEmpty();

        var appName = flexConfig["Application:Name"];
        appName.Should().NotBeNullOrEmpty();

        // Act & Assert - Dynamic access pattern
        dynamic config = flexConfig;
        string dynamicAppName = config.Application?.Name?.ToString() ?? "unexpected";
        dynamicAppName.Should().Be(appName);

        string logLevel = config.Logging?.LogLevel?.Default?.ToString() ?? "unexpected";
        logLevel.Should().NotBeNullOrEmpty();

        // Act & Assert - Mixed access patterns
        var apiTimeout = flexConfig["External:PaymentApi:Timeout"];
        string dynamicApiTimeout = config.External?.PaymentApi?.Timeout?.ToString() ?? "unexpected";
        dynamicApiTimeout.Should().Be(apiTimeout);
    }

    [Theory]
    [AutoData]
    public void StringIndexer_WithKeyContainingSpecialCharacters_HandlesCorrectly(string baseKey)
    {
        // Arrange
        var specialKey = $"{baseKey}:with:colons:and.dots";
        var expectedValue = Create<string>();
        _mockConfiguration[specialKey].Returns(expectedValue);

        // Act
        var result = _flexConfig[specialKey];

        // Assert
        result.Should().Be(expectedValue);
        _ = _mockConfiguration.Received(1)[specialKey];
    }

    [Fact]
    public void InterfaceContract_SupportsServiceInjectionPattern()
    {
        // Arrange
        var testData = new Dictionary<string, string?>
        {
            ["Service:Name"] = "TestService",
            ["Service:Version"] = "1.0.0",
            ["Service:Debug"] = "true"
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(testData);
        var configuration = builder.Build();
        
        IFlexConfig flexConfig = new FlexConfiguration(configuration);

        // Simulate service that depends on IFlexConfig
        var service = new TestServiceWithFlexConfig(flexConfig);

        // Act
        var serviceName = service.GetServiceName();
        var serviceVersion = service.GetServiceVersion();
        var isDebugMode = service.IsDebugMode();

        // Assert
        serviceName.Should().Be("TestService");
        serviceVersion.Should().Be("1.0.0");
        isDebugMode.Should().BeTrue();
    }
}

/// <summary>
/// Test service class to verify IFlexConfig interface contract in dependency injection scenarios.
/// </summary>
public class TestServiceWithFlexConfig(IFlexConfig config)
{
    public string? GetServiceName()
    {
        return config["Service:Name"];
    }

    public string? GetServiceVersion()
    {
        dynamic config1 = config;
        return config1.Service?.Version?.ToString();
    }

    public bool IsDebugMode()
    {
        var debugValue = config["Service:Debug"];
        return bool.TryParse(debugValue, out var result) && result;
    }
}
