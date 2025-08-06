using Amazon.Extensions.NETCore.Setup;
using FlexKit.Configuration.Providers.Aws.Sources;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Xunit;

namespace FlexKit.Configuration.Providers.Aws.Tests.Sources;

public class AwsSecretsManagerConfigurationSourceTests
{
    [Fact]
    public void Constructor_CreatesInstanceWithDefaultValues()
    {
        // Act
        var source = new AwsSecretsManagerConfigurationSource();

        // Assert
        source.SecretNames.Should().BeNull();
        source.Optional.Should().BeTrue();
        source.JsonProcessor.Should().BeFalse();
        source.JsonProcessorSecrets.Should().BeNull();
        source.VersionStage.Should().BeNull();
        source.ReloadAfter.Should().BeNull();
        source.AwsOptions.Should().BeNull();
        source.SecretProcessor.Should().BeNull();
        source.OnLoadException.Should().BeNull();
    }

    [Fact]
    public void SecretNames_CanBeSetAndRetrieved()
    {
        // Arrange
        var source = new AwsSecretsManagerConfigurationSource();
        var secretNames = new[] { "app-database", "app-cache" };

        // Act
        source.SecretNames = secretNames;

        // Assert
        source.SecretNames.Should().BeEquivalentTo(secretNames);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Optional_CanBeSetAndRetrieved(bool optional)
    {
        // Arrange
        var source = new AwsSecretsManagerConfigurationSource
        {
            // Act
            Optional = optional,
        };

        // Assert
        source.Optional.Should().Be(optional);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void JsonProcessor_CanBeSetAndRetrieved(bool jsonProcessor)
    {
        // Arrange
        var source = new AwsSecretsManagerConfigurationSource
        {
            // Act
            JsonProcessor = jsonProcessor,
        };

        // Assert
        source.JsonProcessor.Should().Be(jsonProcessor);
    }

    [Fact]
    public void JsonProcessorSecrets_CanBeSetAndRetrieved()
    {
        // Arrange
        var source = new AwsSecretsManagerConfigurationSource();
        var secrets = new[] { "database-secret", "cache-secret" };

        // Act
        source.JsonProcessorSecrets = secrets;

        // Assert
        source.JsonProcessorSecrets.Should().BeEquivalentTo(secrets);
    }

    [Theory]
    [InlineData("AWSCURRENT")]
    [InlineData("AWSPENDING")]
    [InlineData("custom-version")]
    public void VersionStage_CanBeSetAndRetrieved(string versionStage)
    {
        // Arrange
        var source = new AwsSecretsManagerConfigurationSource
        {
            // Act
            VersionStage = versionStage,
        };

        // Assert
        source.VersionStage.Should().Be(versionStage);
    }

    [Fact]
    public void ReloadAfter_CanBeSetAndRetrieved()
    {
        // Arrange
        var source = new AwsSecretsManagerConfigurationSource();
        var reloadAfter = TimeSpan.FromMinutes(20);

        // Act
        source.ReloadAfter = reloadAfter;

        // Assert
        source.ReloadAfter.Should().Be(reloadAfter);
    }

    [Fact]
    public void AwsOptions_CanBeSetAndRetrieved()
    {
        // Arrange
        var source = new AwsSecretsManagerConfigurationSource();
        var awsOptions = new AWSOptions();

        // Act
        source.AwsOptions = awsOptions;

        // Assert
        source.AwsOptions.Should().BeSameAs(awsOptions);
    }

    [Fact]
    public void SecretProcessor_CanBeSetAndRetrieved()
    {
        // Arrange
        var source = new AwsSecretsManagerConfigurationSource();
        var processor = Substitute.For<ISecretProcessor>();

        // Act
        source.SecretProcessor = processor;

        // Assert
        source.SecretProcessor.Should().BeSameAs(processor);
    }

    [Fact]
    public void OnLoadException_CanBeSetAndRetrieved()
    {
        // Arrange
        var source = new AwsSecretsManagerConfigurationSource();
        var exceptionHandler = new Action<SecretsManagerConfigurationProviderException>(_ => { });

        // Act
        source.OnLoadException = exceptionHandler;

        // Assert
        source.OnLoadException.Should().BeSameAs(exceptionHandler);
    }

    [Fact]
    public void Build_WithNullBuilder_ThrowsArgumentNullException()
    {
        // Arrange
        var source = new AwsSecretsManagerConfigurationSource();

        // Act & Assert
        var action = () => source.Build(null!);
        action.Should().Throw<ArgumentNullException>().WithParameterName("builder");
    }

    [Fact]
    public void Build_WithValidBuilder_ReturnsProvider()
    {
        // Arrange
        var source = new AwsSecretsManagerConfigurationSource
        {
            AwsOptions = CreateMockedAwsOptions()
        };
        var builder = Substitute.For<IConfigurationBuilder>();

        // Act
        var provider = source.Build(builder);

        // Assert
        provider.Should().NotBeNull();
        provider.Should().BeOfType<AwsSecretsManagerConfigurationProvider>();
    }

    [Fact]
    public void Build_CalledMultipleTimes_ReturnsNewInstances()
    {
        // Arrange
        var source = new AwsSecretsManagerConfigurationSource
        {
            AwsOptions = CreateMockedAwsOptions()
        };
        var builder = Substitute.For<IConfigurationBuilder>();

        // Act
        var provider1 = source.Build(builder);
        var provider2 = source.Build(builder);

        // Assert
        provider1.Should().NotBeSameAs(provider2);
        provider1.Should().BeOfType<AwsSecretsManagerConfigurationProvider>();
        provider2.Should().BeOfType<AwsSecretsManagerConfigurationProvider>();
    }

    private static AWSOptions CreateMockedAwsOptions()
    {
        var mockCredentials = Substitute.For<Amazon.Runtime.AWSCredentials>();
        mockCredentials.GetCredentials().Returns(new Amazon.Runtime.ImmutableCredentials("fake-key", "fake-secret", "fake-token"));

        return new AWSOptions
        {
            Credentials = mockCredentials,
            Region = Amazon.RegionEndpoint.USEast1
        };
    }

    [Fact]
    public void Properties_SupportFluentConfiguration()
    {
        // Arrange & Act
        var source = new AwsSecretsManagerConfigurationSource
        {
            SecretNames = ["prod-database", "prod-cache"],
            Optional = false,
            JsonProcessor = true,
            JsonProcessorSecrets = ["prod-database"],
            VersionStage = "AWSCURRENT",
            ReloadAfter = TimeSpan.FromMinutes(15),
            AwsOptions = new AWSOptions(),
            SecretProcessor = Substitute.For<ISecretProcessor>(),
            OnLoadException = _ => { }
        };

        // Assert
        source.SecretNames.Should().ContainInOrder("prod-database", "prod-cache");
        source.Optional.Should().BeFalse();
        source.JsonProcessor.Should().BeTrue();
        source.JsonProcessorSecrets.Should().ContainSingle("prod-database");
        source.VersionStage.Should().Be("AWSCURRENT");
        source.ReloadAfter.Should().Be(TimeSpan.FromMinutes(15));
        source.AwsOptions.Should().NotBeNull();
        source.SecretProcessor.Should().NotBeNull();
        source.OnLoadException.Should().NotBeNull();
    }

    [Fact]
    public void JsonProcessorSecrets_WithEmptyArray_SetsEmptyArray()
    {
        // Arrange
        var source = new AwsSecretsManagerConfigurationSource
        {
            // Act
            JsonProcessorSecrets = []
        };

        // Assert
        source.JsonProcessorSecrets.Should().NotBeNull();
        source.JsonProcessorSecrets.Should().BeEmpty();
    }

    [Fact]
    public void SecretNames_WithEmptyArray_SetsEmptyArray()
    {
        // Arrange
        var source = new AwsSecretsManagerConfigurationSource
        {
            // Act
            SecretNames = []
        };

        // Assert
        source.SecretNames.Should().NotBeNull();
        source.SecretNames.Should().BeEmpty();
    }

    [Fact]
    public void ReloadAfter_WithZeroTimeSpan_AllowsZeroValue()
    {
        // Arrange
        var source = new AwsSecretsManagerConfigurationSource
        {
            // Act
            ReloadAfter = TimeSpan.Zero
        };

        // Assert
        source.ReloadAfter.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void ReloadAfter_WithNegativeTimeSpan_AllowsNegativeValue()
    {
        // Arrange
        var source = new AwsSecretsManagerConfigurationSource
        {
            // Act
            ReloadAfter = TimeSpan.FromMinutes(-1)
        };

        // Assert
        source.ReloadAfter.Should().Be(TimeSpan.FromMinutes(-1));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("AWSCURRENT")]
    [InlineData("AWSPENDING")]
    [InlineData("custom-version-id")]
    public void VersionStage_WithVariousValues_StoresCorrectly(string? versionStage)
    {
        // Arrange
        var source = new AwsSecretsManagerConfigurationSource
        {
            // Act
            VersionStage = versionStage
        };

        // Assert
        source.VersionStage.Should().Be(versionStage);
    }
}