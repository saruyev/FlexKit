using Amazon.Extensions.NETCore.Setup;
using FlexKit.Configuration.Providers.Aws.Options;
using FlexKit.Configuration.Providers.Aws.Sources;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace FlexKit.Configuration.Providers.Aws.Tests.Options;

public class AwsSecretsManagerOptionsTests
{
    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Act
        var options = new AwsSecretsManagerOptions();

        // Assert
        options.SecretNames.Should().BeNull();
        options.Optional.Should().BeTrue();
        options.JsonProcessor.Should().BeFalse();
        options.JsonProcessorSecrets.Should().BeNull();
        options.VersionStage.Should().BeNull();
        options.ReloadAfter.Should().BeNull();
        options.AwsOptions.Should().BeNull();
        options.SecretProcessor.Should().BeNull();
        options.OnLoadException.Should().BeNull();
    }

    [Fact]
    public void SecretNames_CanBeSetAndRetrieved()
    {
        // Arrange
        var options = new AwsSecretsManagerOptions();
        var secretNames = new[] { "secret1", "secret2", "secret3" };

        // Act
        options.SecretNames = secretNames;

        // Assert
        options.SecretNames.Should().BeEquivalentTo(secretNames);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Optional_CanBeSetAndRetrieved(bool optional)
    {
        // Arrange
        var options = new AwsSecretsManagerOptions
        {
            // Act
            Optional = optional,
        };

        // Assert
        options.Optional.Should().Be(optional);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void JsonProcessor_CanBeSetAndRetrieved(bool jsonProcessor)
    {
        // Arrange
        var options = new AwsSecretsManagerOptions
        {
            // Act
            JsonProcessor = jsonProcessor,
        };

        // Assert
        options.JsonProcessor.Should().Be(jsonProcessor);
    }

    [Fact]
    public void JsonProcessorSecrets_CanBeSetAndRetrieved()
    {
        // Arrange
        var options = new AwsSecretsManagerOptions();
        var secrets = new[] { "database-secret", "cache-secret" };

        // Act
        options.JsonProcessorSecrets = secrets;

        // Assert
        options.JsonProcessorSecrets.Should().BeEquivalentTo(secrets);
    }

    [Theory]
    [InlineData("AWSCURRENT")]
    [InlineData("AWSPENDING")]
    [InlineData("custom-version")]
    public void VersionStage_CanBeSetAndRetrieved(string versionStage)
    {
        // Arrange
        var options = new AwsSecretsManagerOptions
        {
            // Act
            VersionStage = versionStage,
        };

        // Assert
        options.VersionStage.Should().Be(versionStage);
    }

    [Fact]
    public void ReloadAfter_CanBeSetAndRetrieved()
    {
        // Arrange
        var options = new AwsSecretsManagerOptions();
        var reloadAfter = TimeSpan.FromMinutes(20);

        // Act
        options.ReloadAfter = reloadAfter;

        // Assert
        options.ReloadAfter.Should().Be(reloadAfter);
    }

    [Fact]
    public void AwsOptions_CanBeSetAndRetrieved()
    {
        // Arrange
        var options = new AwsSecretsManagerOptions();
        var awsOptions = new AWSOptions();

        // Act
        options.AwsOptions = awsOptions;

        // Assert
        options.AwsOptions.Should().BeSameAs(awsOptions);
    }

    [Fact]
    public void SecretProcessor_CanBeSetAndRetrieved()
    {
        // Arrange
        var options = new AwsSecretsManagerOptions();
        var processor = Substitute.For<ISecretProcessor>();

        // Act
        options.SecretProcessor = processor;

        // Assert
        options.SecretProcessor.Should().BeSameAs(processor);
    }

    [Fact]
    public void OnLoadException_CanBeSetAndRetrieved()
    {
        // Arrange
        var options = new AwsSecretsManagerOptions();
        var exceptionHandler = new Action<SecretsManagerConfigurationProviderException>(_ => { });

        // Act
        options.OnLoadException = exceptionHandler;

        // Assert
        options.OnLoadException.Should().BeSameAs(exceptionHandler);
    }

    [Fact]
    public void Properties_SupportFluentConfiguration()
    {
        // Arrange & Act
        var options = new AwsSecretsManagerOptions
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
        options.SecretNames.Should().ContainInOrder("prod-database", "prod-cache");
        options.Optional.Should().BeFalse();
        options.JsonProcessor.Should().BeTrue();
        options.JsonProcessorSecrets.Should().ContainSingle("prod-database");
        options.VersionStage.Should().Be("AWSCURRENT");
        options.ReloadAfter.Should().Be(TimeSpan.FromMinutes(15));
        options.AwsOptions.Should().NotBeNull();
        options.SecretProcessor.Should().NotBeNull();
        options.OnLoadException.Should().NotBeNull();
    }
}