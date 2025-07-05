using Amazon.Extensions.NETCore.Setup;
using FlexKit.Configuration.Providers.Aws.Options;
using FlexKit.Configuration.Providers.Aws.Sources;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace FlexKit.Configuration.Providers.Aws.Tests.Options;

public class AwsParameterStoreOptionsTests
{
    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Act
        var options = new AwsParameterStoreOptions();

        // Assert
        options.Path.Should().BeNull();
        options.Optional.Should().BeTrue();
        options.JsonProcessor.Should().BeFalse();
        options.JsonProcessorPaths.Should().BeNull();
        options.ReloadAfter.Should().BeNull();
        options.AwsOptions.Should().BeNull();
        options.ParameterProcessor.Should().BeNull();
        options.OnLoadException.Should().BeNull();
    }

    [Theory]
    [InlineData("/test/")]
    [InlineData("/prod/myapp/")]
    [InlineData("/dev/database/")]
    public void Path_CanBeSetAndRetrieved(string path)
    {
        // Arrange
        var options = new AwsParameterStoreOptions
        {
            // Act
            Path = path,
        };

        // Assert
        options.Path.Should().Be(path);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Optional_CanBeSetAndRetrieved(bool optional)
    {
        // Arrange
        var options = new AwsParameterStoreOptions
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
        var options = new AwsParameterStoreOptions
        {
            // Act
            JsonProcessor = jsonProcessor,
        };

        // Assert
        options.JsonProcessor.Should().Be(jsonProcessor);
    }

    [Fact]
    public void JsonProcessorPaths_CanBeSetAndRetrieved()
    {
        // Arrange
        var options = new AwsParameterStoreOptions();
        var paths = new[] { "/test/database/", "/test/cache/" };

        // Act
        options.JsonProcessorPaths = paths;

        // Assert
        options.JsonProcessorPaths.Should().BeEquivalentTo(paths);
    }

    [Fact]
    public void ReloadAfter_CanBeSetAndRetrieved()
    {
        // Arrange
        var options = new AwsParameterStoreOptions();
        var reloadAfter = TimeSpan.FromMinutes(5);

        // Act
        options.ReloadAfter = reloadAfter;

        // Assert
        options.ReloadAfter.Should().Be(reloadAfter);
    }

    [Fact]
    public void AwsOptions_CanBeSetAndRetrieved()
    {
        // Arrange
        var options = new AwsParameterStoreOptions();
        var awsOptions = new AWSOptions();

        // Act
        options.AwsOptions = awsOptions;

        // Assert
        options.AwsOptions.Should().BeSameAs(awsOptions);
    }

    [Fact]
    public void ParameterProcessor_CanBeSetAndRetrieved()
    {
        // Arrange
        var options = new AwsParameterStoreOptions();
        var processor = Substitute.For<IParameterProcessor>();

        // Act
        options.ParameterProcessor = processor;

        // Assert
        options.ParameterProcessor.Should().BeSameAs(processor);
    }

    [Fact]
    public void OnLoadException_CanBeSetAndRetrieved()
    {
        // Arrange
        var options = new AwsParameterStoreOptions();
        var exceptionHandler = new Action<ConfigurationProviderException>(_ => { });

        // Act
        options.OnLoadException = exceptionHandler;

        // Assert
        options.OnLoadException.Should().BeSameAs(exceptionHandler);
    }

    [Fact]
    public void Properties_SupportFluentConfiguration()
    {
        // Arrange & Act
        var options = new AwsParameterStoreOptions
        {
            Path = "/prod/myapp/",
            Optional = false,
            JsonProcessor = true,
            JsonProcessorPaths = ["/prod/myapp/database/"],
            ReloadAfter = TimeSpan.FromMinutes(15),
            AwsOptions = new AWSOptions(),
            ParameterProcessor = Substitute.For<IParameterProcessor>(),
            OnLoadException = _ => { }
        };

        // Assert
        options.Path.Should().Be("/prod/myapp/");
        options.Optional.Should().BeFalse();
        options.JsonProcessor.Should().BeTrue();
        options.JsonProcessorPaths.Should().ContainSingle("/prod/myapp/database/");
        options.ReloadAfter.Should().Be(TimeSpan.FromMinutes(15));
        options.AwsOptions.Should().NotBeNull();
        options.ParameterProcessor.Should().NotBeNull();
        options.OnLoadException.Should().NotBeNull();
    }
}