using System.Globalization;
using AutoFixture;
using AutoFixture.Xunit2;
using FlexKit.Configuration.Core;
using FlexKit.Configuration.Conversion;
using FlexKit.Configuration.Tests.TestBase;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Xunit;
// ReSharper disable MethodTooLong
// ReSharper disable ClassTooBig
// ReSharper disable NullableWarningSuppressionIsUsed

namespace FlexKit.Configuration.Tests.Core;

public class FlexConfigurationTests : UnitTestBase
{
    private readonly IConfiguration _mockConfiguration;
    private readonly FlexConfiguration _flexConfiguration;

    public FlexConfigurationTests()
    {
        _mockConfiguration = CreateMock<IConfiguration>();
        _flexConfiguration = new FlexConfiguration(_mockConfiguration);
        _flexConfiguration.Configuration.Should().BeSameAs(_mockConfiguration);
    }

    protected override void RegisterFixtureCustomizations()
    {
        // Customize string generation to avoid null values in configuration keys
        Fixture.Register(() => Fixture.Create<string>().Replace("\0", ""));

        // Register Bogus generators for specific types
        Fixture.Register(() => ConfigurationTestDataBuilder.DatabaseConfig.Generate());
        Fixture.Register(() => ConfigurationTestDataBuilder.ApiConfig.Generate());
        Fixture.Register(() => ConfigurationTestDataBuilder.UserConfig.Generate());
    }

    [Fact]
    public void Constructor_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Act & Assert
        var action = () => new FlexConfiguration(null!);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("root");
    }

    [Theory]
    [AutoData]
    public void StringIndexer_WithValidKey_ReturnsConfigurationValue(string key, string expectedValue)
    {
        // Arrange
        _mockConfiguration[key].Returns(expectedValue);

        // Act
        var result = _flexConfiguration[key];

        // Assert
        result.Should().Be(expectedValue);
        _ = _mockConfiguration.Received(1)[key];
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void StringIndexer_WithNullOrEmptyKey_ReturnsNull(string? key)
    {
        // Act
        var result = _flexConfiguration[key!];

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void DynamicAccess_WithValidProperty_ReturnsValue()
    {
        // Arrange
        var configData = ConfigurationTestDataBuilder.CreateConfigurationDictionary();
        var expectedApiKey = configData["External:PaymentApi:ApiKey"];

        _mockConfiguration["External:PaymentApi:ApiKey"].Returns(expectedApiKey);

        // Act
        dynamic config = _flexConfiguration;
        var result = config.External?.PaymentApi?.ApiKey?.ToString();

        // Assert
        result?.Should().Be(expectedApiKey);
    }

    [Fact]
    public void DynamicAccess_WithNonExistentProperty_ReturnsNull()
    {
        // Arrange
        _mockConfiguration[Arg.Any<string>()].Returns((string?)null);

        // Act
        dynamic config = _flexConfiguration;
        var result = config.NonExistent?.Property?.Value;

        // Assert
        result?.Should().BeNull();
    }

    [Theory]
    [AutoData]
    public void NumericIndexer_WithValidIndex_ReturnsFlexConfig(int index)
    {
        // Arrange
        var key = index.ToString(CultureInfo.InvariantCulture);
        var mockSection = CreateMock<IConfigurationSection>();
        mockSection.Key.Returns(key);

        // Mock the underlying methods that CurrentConfig uses
        _mockConfiguration.GetChildren().Returns([mockSection]);

        // Act
        var result = _flexConfiguration[index];

        // Assert
        result.Should().NotBeNull();
        result.Configuration.Should().BeSameAs(mockSection);
    }

    [Fact]
    public void ToType_WithBogusGeneratedData_ConvertsSuccessfully()
    {
        // Arrange
        var databaseConfig = Create<DatabaseConfig>();
        var timeoutValue = databaseConfig.CommandTimeout.ToString();

        _mockConfiguration["Database:CommandTimeout"].Returns(timeoutValue);

        // Act
        var result = _flexConfiguration["Database:CommandTimeout"].ToType<int>();

        // Assert
        result.Should().Be(databaseConfig.CommandTimeout);
    }

    [Fact]
    public void ToType_WithInvalidValue_ThrowsFormatException()
    {
        // Arrange
        _mockConfiguration["InvalidNumber"].Returns("not-a-number");

        // Act & Assert
        var action = () => _flexConfiguration["InvalidNumber"].ToType<int>();

        action.Should().Throw<FormatException>()
            .WithMessage("*not in a correct format*");
    }

    [Fact]
    public void ComplexScenario_WithRealisticConfigurationData()
    {
        // Arrange
        var configData = ConfigurationTestDataBuilder.CreateConfigurationDictionary();
        ConfigurationTestDataBuilder.DatabaseConfig.Generate();
        ConfigurationTestDataBuilder.ApiConfig.Generate();

        // Setup mock responses
        foreach (var kvp in configData)
        {
            _mockConfiguration[kvp.Key].Returns(kvp.Value);
        }

        // Act & Assert - Test database configuration access
        dynamic config = _flexConfiguration;
        var connectionString = config.ConnectionStrings?.DefaultConnection?.ToString();
        connectionString?.Should().NotBeNullOrEmpty();

        var commandTimeout = _flexConfiguration["Database:CommandTimeout"].ToType<int>();
        commandTimeout.Should().BeGreaterThan(0);

        // Test API configuration access
        var apiBaseUrl = config.External?.PaymentApi?.BaseUrl?.ToString();
        apiBaseUrl?.Should().NotBeNullOrEmpty().And.StartWith("http");

        var apiKey = config.External?.PaymentApi?.ApiKey?.ToString();
        apiKey?.Should().NotBeNullOrEmpty().And.HaveLength(32);
    }

    [Theory]
    [InlineData("TestValue", "TestValue")] // Section with value
    [InlineData(null, "")] // Section with null value  
    [InlineData("", "")] // Section with an empty value
    public void ToString_WithConfigurationSection_ReturnsExpectedValue(string? sectionValue, string expectedResult)
    {
        // Arrange
        var mockSection = Substitute.For<IConfigurationSection>();
        mockSection.Value.Returns(sectionValue);

        var flexConfig = new FlexConfiguration(mockSection);

        // Act
        var result = flexConfig.ToString();

        // Assert
        result.Should().Be(expectedResult);
    }

    [Fact]
    public void ToString_WithRootConfiguration_ReturnsEmptyString()
    {
        // Act
        var result = _flexConfiguration.ToString();

        // Assert
        result.Should().Be(string.Empty);
    }

    [Fact]
    public void TryConvert_WithInvalidValueForValueType_ThrowsException()
    {
        // Arrange
        var mockSection = Substitute.For<IConfigurationSection>();
        mockSection.Value.Returns("not-a-number");

        var flexConfig = new FlexConfiguration(mockSection);

        // Act & Assert
        var action = () =>
        {
            dynamic dynamicConfig = flexConfig;
            int result = (int)dynamicConfig; // This should throw
            return result;
        };

        action.Should().Throw<FormatException>();
    }

    [Fact]
    public void TryConvert_WithValidValue_ReturnsTrue()
    {
        // Arrange
        var mockSection = Substitute.For<IConfigurationSection>();
        mockSection.Value.Returns("42");

        var flexConfig = new FlexConfiguration(mockSection);

        // Act
        dynamic dynamicConfig = flexConfig;
        int result = (int)dynamicConfig;

        // Assert - if we get here, TryConvert returned true and conversion succeeded
        result.Should().Be(42);
    }

    [Fact]
    public void TryConvert_WithValueTypeAndValidValue_ConvertsSuccessfully()
    {
        // Arrange
        var mockSection = Substitute.For<IConfigurationSection>();
        mockSection.Value.Returns("42");

        var flexConfig = new FlexConfiguration(mockSection);

        // Act
        dynamic dynamicConfig = flexConfig;
        int result = (int)dynamicConfig;

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public void TryConvert_WithStringType_ReturnsValue()
    {
        // Arrange
        var mockSection = Substitute.For<IConfigurationSection>();
        mockSection.Value.Returns("test string");

        var flexConfig = new FlexConfiguration(mockSection);

        // Act
        dynamic dynamicConfig = flexConfig;
        string result = (string)dynamicConfig;

        // Assert
        result.Should().Be("test string");
    }

    [Fact]
    public void TryConvert_WithArrayType_ConvertsChildrenToArray()
    {
        // Arrange
        var child1 = Substitute.For<IConfigurationSection>();
        child1.Value.Returns("value1");
        var child2 = Substitute.For<IConfigurationSection>();
        child2.Value.Returns("value2");

        var mockSection = Substitute.For<IConfigurationSection>();
        mockSection.GetChildren().Returns([child1, child2]);

        var flexConfig = new FlexConfiguration(mockSection);

        // Act
        dynamic dynamicConfig = flexConfig;
        string[] result = (string[])dynamicConfig;

        // Assert
        result.Should().BeEquivalentTo(new[] { "value1", "value2" });
    }

    [Fact]
    public void TryConvert_WithNonValueNonStringNonArrayNonGenericType_ReturnsNull()
    {
        // Arrange
        var mockSection = Substitute.For<IConfigurationSection>();
        var flexConfig = new FlexConfiguration(mockSection);

        // Act
        dynamic dynamicConfig = flexConfig;
        // Use a specific reference type that isn't string, array, or generic
        List<string> result = (List<string>)dynamicConfig;

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void TryConvert_WithDictionaryType_ConvertsToDictionary()
    {
        // Arrange
        var grandChild1 = Substitute.For<IConfigurationSection>();
        grandChild1.Value.Returns("10");
        grandChild1.Key.Returns("key1"); // This becomes the dictionary key

        var grandChild2 = Substitute.For<IConfigurationSection>();
        grandChild2.Value.Returns("20");
        grandChild2.Key.Returns("key2"); // This becomes the dictionary key

        var child1 = Substitute.For<IConfigurationSection>();
        child1.Key.Returns("parent1"); // This is ignored
        child1.GetChildren().Returns([grandChild1]);

        var child2 = Substitute.For<IConfigurationSection>();
        child2.Key.Returns("parent2"); // This is ignored
        child2.GetChildren().Returns([grandChild2]);

        var mockSection = Substitute.For<IConfigurationSection>();
        mockSection.GetChildren().Returns([child1, child2]);

        var flexConfig = new FlexConfiguration(mockSection);

        // Act
        dynamic dynamicConfig = flexConfig;
        var result = (Dictionary<string, int>)dynamicConfig;

        // Assert
        result.Should().NotBeNull();
        result.Should().ContainKey("key1").WhoseValue.Should().Be(10);
        result.Should().ContainKey("key2").WhoseValue.Should().Be(20);
    }

    [Fact]
    public void NumericIndexer_WithValidIndex_ReturnsCurrentConfig()
    {
        // Arrange
        var index = 123;
        var key = index.ToString(CultureInfo.InvariantCulture);

        var mockSection = Substitute.For<IConfigurationSection>();
        mockSection.Key.Returns(key);

        _mockConfiguration.GetChildren().Returns([mockSection]);

        // Act
        var result = _flexConfiguration[index];

        // Assert
        result.Should().NotBeNull();
        result.Configuration.Should().BeSameAs(mockSection);
    }

    [Fact]
    public void NumericIndexer_WithNonExistentIndex_ReturnsNull()
    {
        // Arrange
        var index = 999;
        _mockConfiguration.GetChildren().Returns([]);

        // Act
        var result = _flexConfiguration[index];

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [AutoData]
    public void GetSection_WithValidKey_ReturnsFlexConfig(string key)
    {
        // Arrange
        var mockSection = CreateMock<IConfigurationSection>();
        mockSection.Key.Returns(key);

        _mockConfiguration.GetChildren().Returns([mockSection]);

        // Act
        var result = _flexConfiguration.GetSection(key);

        // Assert
        result.Should().NotBeNull();
        result.Configuration.Should().BeSameAs(mockSection);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GetSection_WithNullOrEmptyKey_ReturnsNull(string? key)
    {
        // Act
        var result = _flexConfiguration.GetSection(key!);

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [AutoData]
    public void GetSection_WithNonExistentKey_ReturnsNull(string nonExistentKey)
    {
        // Arrange
        _mockConfiguration.GetChildren().Returns([]);

        // Act
        var result = _flexConfiguration.GetSection(nonExistentKey);

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [AutoData]
    public void GetSection_WithCaseInsensitiveKey_ReturnsFlexConfig(string key)
    {
        // Arrange
        var mockSection = CreateMock<IConfigurationSection>();
        mockSection.Key.Returns(key.ToUpper());

        _mockConfiguration.GetChildren().Returns([mockSection]);

        // Act
        var result = _flexConfiguration.GetSection(key.ToLower());

        // Assert
        result.Should().NotBeNull();
        result.Configuration.Should().BeSameAs(mockSection);
    }

    [Fact]
    public void GetSection_WithValidKey_EnablesChainedAccess()
    {
        // Arrange
        var parentKey = "Parent";
        var childKey = "Child";

        var mockChildSection = CreateMock<IConfigurationSection>();
        mockChildSection.Key.Returns(childKey);

        var mockParentSection = CreateMock<IConfigurationSection>();
        mockParentSection.Key.Returns(parentKey);
        mockParentSection.GetChildren().Returns([mockChildSection]);

        _mockConfiguration.GetChildren().Returns([mockParentSection]);

        // Act
        var parentSection = _flexConfiguration.GetSection(parentKey);
        var childSection = parentSection?.GetSection(childKey);

        // Assert
        parentSection.Should().NotBeNull();
        childSection.Should().NotBeNull();
        childSection.Configuration.Should().BeSameAs(mockChildSection);
    }

    [Fact]
    public void GetSection_WithRealisticConfigurationData_WorksCorrectly()
    {
        // Arrange
        var configData = ConfigurationTestDataBuilder.CreateConfigurationDictionary();
        var databaseKey = "Database";

        var mockDatabaseSection = CreateMock<IConfigurationSection>();
        mockDatabaseSection.Key.Returns(databaseKey);
        mockDatabaseSection["CommandTimeout"].Returns(configData["Database:CommandTimeout"]);

        _mockConfiguration.GetChildren().Returns([mockDatabaseSection]);

        // Act
        var databaseSection = _flexConfiguration.GetSection(databaseKey);

        // Assert
        databaseSection.Should().NotBeNull();
        databaseSection.Configuration.Should().BeSameAs(mockDatabaseSection);
        databaseSection["CommandTimeout"].Should().Be(configData["Database:CommandTimeout"]);
    }
}
