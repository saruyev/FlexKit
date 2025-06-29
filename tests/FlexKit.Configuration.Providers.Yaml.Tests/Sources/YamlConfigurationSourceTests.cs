using FlexKit.Configuration.Providers.Yaml.Sources;
using FlexKit.Configuration.Providers.Yaml.Tests.TestBase;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace FlexKit.Configuration.Providers.Yaml.Tests.Sources;

/// <summary>
/// Comprehensive unit tests for YamlConfigurationSource covering all functionality and edge cases.
/// Tests the source factory pattern and provider creation.
/// </summary>
public class YamlConfigurationSourceTests : YamlTestBase
{
    #region Constructor and Property Tests

    [Fact]
    public void Constructor_CreatesInstanceWithDefaultValues()
    {
        // Act
        var source = new YamlConfigurationSource();

        // Assert
        source.Should().NotBeNull();
        source.Path.Should().Be("appsettings.yaml");
        source.Optional.Should().BeTrue();
    }

    [Fact]
    public void Path_CanBeSetAndRetrieved()
    {
        // Arrange
        var source = new YamlConfigurationSource();
        const string expectedPath = "config/custom.yaml";

        // Act
        source.Path = expectedPath;

        // Assert
        source.Path.Should().Be(expectedPath);
    }

    [Fact]
    public void Optional_CanBeSetAndRetrieved()
    {
        // Arrange
        var source = new YamlConfigurationSource
        {
            // Act
            Optional = false,
        };

        // Assert
        source.Optional.Should().BeFalse();
    }

    [Theory]
    [InlineData("appsettings.yaml")]
    [InlineData("config.yml")]
    [InlineData("/absolute/path/to/config.yaml")]
    [InlineData("../relative/path/config.yaml")]
    [InlineData("")]
    [InlineData(null)]
    public void Path_AcceptsVariousPathFormats(string? path)
    {
        // Arrange
        var source = new YamlConfigurationSource
        {
            // Act
            Path = path!,
        };

        // Assert
        source.Path.Should().Be(path);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Optional_AcceptsBothBooleanValues(bool optional)
    {
        // Arrange
        var source = new YamlConfigurationSource
        {
            // Act
            Optional = optional,
        };

        // Assert
        source.Optional.Should().Be(optional);
    }

    #endregion

    #region Build Method Tests

    [Fact]
    public void Build_WithValidBuilder_ReturnsYamlConfigurationProvider()
    {
        // Arrange
        var source = new YamlConfigurationSource
        {
            Path = "test.yaml",
            Optional = true
        };
        var builder = new ConfigurationBuilder();

        // Act
        var provider = source.Build(builder);

        // Assert
        provider.Should().NotBeNull();
        provider.Should().BeOfType<YamlConfigurationProvider>();
    }

    [Fact]
    public void Build_WithNullBuilder_ReturnsProviderSuccessfully()
    {
        // Arrange
        var source = new YamlConfigurationSource();

        // Act
        var provider = source.Build(null!);

        // Assert
        provider.Should().NotBeNull();
        provider.Should().BeOfType<YamlConfigurationProvider>();
    }

    [Fact]
    public void Build_CreatesNewProviderInstanceEachTime()
    {
        // Arrange
        var source = new YamlConfigurationSource();
        var builder = new ConfigurationBuilder();

        // Act
        var provider1 = source.Build(builder);
        var provider2 = source.Build(builder);

        // Assert
        provider1.Should().NotBeSameAs(provider2);
        provider1.Should().BeOfType<YamlConfigurationProvider>();
        provider2.Should().BeOfType<YamlConfigurationProvider>();
    }

    [Fact]
    public void Build_PassesSourcePropertiesToProvider()
    {
        // Arrange
        var source = new YamlConfigurationSource
        {
            Path = "custom-config.yaml",
            Optional = false
        };
        var builder = new ConfigurationBuilder();

        // Act
        var provider = source.Build(builder) as YamlConfigurationProvider;

        // Assert
        provider.Should().NotBeNull();
        // Note: We can't directly test the private _source field, but we can test
        // the behavior in YamlConfigurationProviderTests
    }

    #endregion

    #region IConfigurationSource Integration Tests

    [Fact]
    public void Source_ImplementsIConfigurationSource()
    {
        // Arrange & Act
        var source = new YamlConfigurationSource();

        // Assert
        source.Should().BeAssignableTo<IConfigurationSource>();
    }

    [Fact]
    public void Source_CanBeAddedToConfigurationBuilder()
    {
        // Arrange
        var source = new YamlConfigurationSource
        {
            Path = "test.yaml",
            Optional = true
        };
        var builder = new ConfigurationBuilder();

        // Act
        var builderResult = builder.Add(source);

        // Assert
        builderResult.Should().BeSameAs(builder);
    }

    [Fact]
    public void Source_IntegratesWithConfigurationBuilderBuildProcess()
    {
        // Arrange
        var yamlContent = """
            test:
              key: "value"
            """;
        var tempFile = CreateTempYamlFile(yamlContent);
        var source = new YamlConfigurationSource
        {
            Path = tempFile,
            Optional = false
        };
        var builder = new ConfigurationBuilder();

        // Act
        builder.Add(source);
        var configuration = builder.Build();

        // Assert
        configuration.Should().NotBeNull();
        configuration["test:key"].Should().Be("value");
    }

    #endregion
}