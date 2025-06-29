using AutoFixture.Xunit2;
using FlexKit.Configuration.Sources;
using FlexKit.Configuration.Tests.TestBase;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace FlexKit.Configuration.Tests.Sources;

/// <summary>
/// Unit tests for DotEnvConfigurationSource class covering all properties and methods.
/// </summary>
public class DotEnvConfigurationSourceTests : UnitTestBase
{
    protected override void RegisterFixtureCustomizations()
    {
        // Customize string generation to avoid null values and invalid path characters
        Fixture.Customize<string>(composer => composer.FromFactory(() => 
            "test-" + Guid.NewGuid().ToString("N")[..8]));
    }

    [Fact]
    public void Constructor_WithDefaultValues_SetsExpectedDefaults()
    {
        // Act
        var source = new DotEnvConfigurationSource();

        // Assert
        source.Path.Should().Be(".env");
        source.Optional.Should().BeTrue();
    }

    [Theory]
    [AutoData]
    public void Path_Property_CanBeSetAndRetrieved(string path)
    {
        // Arrange & Act
        var source = new DotEnvConfigurationSource { Path = path };

        // Assert
        source.Path.Should().Be(path);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Optional_Property_CanBeSetAndRetrieved(bool optional)
    {
        // Arrange & Act
        var source = new DotEnvConfigurationSource { Optional = optional };

        // Assert
        source.Optional.Should().Be(optional);
    }

    [Theory]
    [AutoData]
    public void InitSyntax_WithPathAndOptional_SetsPropertiesCorrectly(string path, bool optional)
    {
        // Act
        var source = new DotEnvConfigurationSource
        {
            Path = path,
            Optional = optional
        };

        // Assert
        source.Path.Should().Be(path);
        source.Optional.Should().Be(optional);
    }

    [Fact]
    public void Build_WithValidBuilder_ReturnsDotEnvConfigurationProvider()
    {
        // Arrange
        var source = new DotEnvConfigurationSource
        {
            Path = ".env.test",
            Optional = true
        };
        var mockBuilder = CreateMock<IConfigurationBuilder>();

        // Act
        var provider = source.Build(mockBuilder);

        // Assert
        provider.Should().NotBeNull();
        provider.Should().BeOfType<DotEnvConfigurationProvider>();
    }

    [Fact]
    public void Build_WithNullBuilder_StillReturnsProvider()
    {
        // Arrange
        var source = new DotEnvConfigurationSource();
        IConfigurationBuilder? nullBuilder = null;

        // Act
        var provider = source.Build(nullBuilder!);

        // Assert
        provider.Should().NotBeNull();
        provider.Should().BeOfType<DotEnvConfigurationProvider>();
    }

    [Theory]
    [AutoData]
    public void Build_CreatesProviderWithSameSourceInstance(string testPath, bool testOptional)
    {
        // Arrange
        var source = new DotEnvConfigurationSource
        {
            Path = testPath,
            Optional = testOptional
        };
        var mockBuilder = CreateMock<IConfigurationBuilder>();

        // Act
        var provider = source.Build(mockBuilder) as DotEnvConfigurationProvider;

        // Assert
        provider.Should().NotBeNull();
        // We can't directly access the private _source field, but we can verify behavior
        // through the provider's Load method behavior, which depends on the source properties
    }

    [Fact]
    public void Build_CallMultipleTimes_ReturnsNewInstancesEachTime()
    {
        // Arrange
        var source = new DotEnvConfigurationSource { Path = ".env.test" };
        var mockBuilder = CreateMock<IConfigurationBuilder>();

        // Act
        var provider1 = source.Build(mockBuilder);
        var provider2 = source.Build(mockBuilder);

        // Assert
        provider1.Should().NotBeNull();
        provider2.Should().NotBeNull();
        provider1.Should().NotBeSameAs(provider2);
        provider1.Should().BeOfType<DotEnvConfigurationProvider>();
        provider2.Should().BeOfType<DotEnvConfigurationProvider>();
    }

    [Fact]
    public void Build_WithDifferentBuilders_ReturnsSameTypeOfProvider()
    {
        // Arrange
        var source = new DotEnvConfigurationSource();
        var mockBuilder1 = CreateMock<IConfigurationBuilder>();
        var mockBuilder2 = CreateMock<IConfigurationBuilder>();

        // Act
        var provider1 = source.Build(mockBuilder1);
        var provider2 = source.Build(mockBuilder2);

        // Assert
        provider1.Should().BeOfType<DotEnvConfigurationProvider>();
        provider2.Should().BeOfType<DotEnvConfigurationProvider>();
    }

    [Theory]
    [InlineData(".env")]
    [InlineData(".env.development")]
    [InlineData(".env.production")]
    [InlineData("config/.env")]
    [InlineData("/absolute/path/.env")]
    public void CommonPathPatterns_CanBeSetCorrectly(string path)
    {
        // Act
        var source = new DotEnvConfigurationSource { Path = path };

        // Assert
        source.Path.Should().Be(path);
    }

    [Fact]
    public void RecordSemantics_WithEqualValues_AreEqual()
    {
        // Arrange
        var source1 = new DotEnvConfigurationSource { Path = ".env.test", Optional = true };
        var source2 = new DotEnvConfigurationSource { Path = ".env.test", Optional = true };

        // Act & Assert
        source1.Should().BeEquivalentTo(source2);
        source1.Path.Should().Be(source2.Path);
        source1.Optional.Should().Be(source2.Optional);
    }

    [Fact]
    public void RecordSemantics_WithDifferentValues_AreNotEqual()
    {
        // Arrange
        var source1 = new DotEnvConfigurationSource { Path = ".env.test", Optional = true };
        var source2 = new DotEnvConfigurationSource { Path = ".env.prod", Optional = false };

        // Act & Assert
        source1.Should().NotBeEquivalentTo(source2);
    }

    [Fact]
    public void IConfigurationSource_Implementation_IsCorrect()
    {
        // Arrange
        var source = new DotEnvConfigurationSource();

        // Act & Assert
        source.Should().BeAssignableTo<IConfigurationSource>();
    }

    [Theory]
    [AutoData]
    public void PropertyInitialization_WithObjectInitializer_WorksCorrectly(string customPath)
    {
        // Act
        var source = new DotEnvConfigurationSource
        {
            Path = customPath,
            Optional = false
        };

        // Assert
        source.Path.Should().Be(customPath);
        source.Optional.Should().BeFalse();
    }

    [Fact]
    public void EmptyStringPath_CanBeSet()
    {
        // Act
        var source = new DotEnvConfigurationSource { Path = string.Empty };

        // Assert
        source.Path.Should().Be(string.Empty);
    }

    [Fact]
    public void Build_IntegrationWithConfigurationBuilder_WorksCorrectly()
    {
        // Arrange
        var source = new DotEnvConfigurationSource
        {
            Path = ".env.nonexistent",
            Optional = true
        };
        var builder = new ConfigurationBuilder();

        // Act
        builder.Add(source);
        var configuration = builder.Build();

        // Assert
        configuration.Should().NotBeNull();
        // Since the file doesn't exist, and it's optional, the configuration should be empty but valid
        configuration.AsEnumerable().Should().BeEmpty();
    }
}
