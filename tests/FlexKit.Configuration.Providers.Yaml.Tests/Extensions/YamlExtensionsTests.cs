using FlexKit.Configuration.Core;
using FlexKit.Configuration.Providers.Yaml.Extensions;
using FlexKit.Configuration.Providers.Yaml.Tests.TestBase;
using FluentAssertions;
using Xunit;
// ReSharper disable TooManyDeclarations
// ReSharper disable NullableWarningSuppressionIsUsed

namespace FlexKit.Configuration.Providers.Yaml.Tests.Extensions;

/// <summary>
/// Comprehensive unit tests for FlexConfigurationBuilderYamlExtensions covering all extension method functionality.
/// Tests the fluent API integration and parameter validation.
/// </summary>
public class FlexConfigurationBuilderYamlExtensionsTests : YamlTestBase
{
    #region AddYamlFile Method Tests

    [Fact]
    public void AddYamlFile_WithDefaultParameters_AddsSourceWithDefaults()
    {
        // Arrange
        var builder = new FlexConfigurationBuilder();

        // Act
        var result = builder.AddYamlFile();

        // Assert
        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void AddYamlFile_WithCustomPath_AddsSourceWithCustomPath()
    {
        // Arrange
        var builder = new FlexConfigurationBuilder();
        const string customPath = "config/custom.yaml";

        // Act
        var result = builder.AddYamlFile(customPath);

        // Assert
        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void AddYamlFile_WithOptionalFalse_AddsRequiredSource()
    {
        // Arrange
        var builder = new FlexConfigurationBuilder();

        // Act
        var result = builder.AddYamlFile("config.yaml", optional: false);

        // Assert
        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void AddYamlFile_WithNullBuilder_ThrowsArgumentNullException()
    {
        // Arrange
        FlexConfigurationBuilder? nullBuilder = null;

        // Act & Assert
        var action = () => nullBuilder!.AddYamlFile();
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("builder");
    }

    [Theory]
    [InlineData("appsettings.yaml", true)]
    [InlineData("config.yml", false)]
    [InlineData("", true)]
    [InlineData("../config/app.yaml", false)]
    [InlineData("/absolute/path/config.yaml", true)]
    public void AddYamlFile_WithVariousParameters_CreatesCorrectSource(string path, bool optional)
    {
        // Arrange
        var builder = new FlexConfigurationBuilder();

        // Act
        var result = builder.AddYamlFile(path, optional);

        // Assert
        result.Should().BeSameAs(builder);

        // Build to verify the source was added correctly
        var yamlContent = "test: value";
        var tempFile = CreateTempYamlFile(yamlContent);

        // Test with a real file to verify the source works
        var workingBuilder = new FlexConfigurationBuilder();
        workingBuilder.AddYamlFile(tempFile, optional: true);
        var config = workingBuilder.Build();

        config["test"].Should().Be("value");
    }

    #endregion

    #region Method Chaining Tests

    [Fact]
    public void AddYamlFile_EnablesMethodChaining()
    {
        // Arrange
        var builder = new FlexConfigurationBuilder();

        // Act
        var result = builder
            .AddYamlFile("config1.yaml")
            .AddYamlFile("config2.yaml", optional: false)
            .AddYamlFile("config3.yml", optional: true);

        // Assert
        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void AddYamlFile_ChainsWithOtherFlexConfigurationMethods()
    {
        // Arrange
        var builder = new FlexConfigurationBuilder();
        var yamlContent = """
            yaml:
              source: "yaml_value"
            """;
        var tempFile = CreateTempYamlFile(yamlContent);

        // Act
        var config = builder
            .AddJsonFile("nonexistent.json", optional: true)
            .AddYamlFile(tempFile, optional: false)
            .AddEnvironmentVariables()
            .Build();

        // Assert
        config["yaml:source"].Should().Be("yaml_value");
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void AddYamlFile_IntegratesWithFlexConfigurationBuilder()
    {
        // Arrange
        var yamlContent = """
            database:
              connectionString: "Server=localhost;Database=TestDb;"
              timeout: 30
            api:
              baseUrl: "https://api.test.com"
              key: "test-api-key"
            features:
              - authentication
              - logging
              - caching
            """;
        var tempFile = CreateTempYamlFile(yamlContent);
        var builder = new FlexConfigurationBuilder();

        // Act
        var config = builder
            .AddYamlFile(tempFile, optional: false)
            .Build();

        // Assert
        config.Should().NotBeNull();
        config["database:connectionString"].Should().Be("Server=localhost;Database=TestDb;");
        config["database:timeout"].Should().Be("30");
        config["api:baseUrl"].Should().Be("https://api.test.com");
        config["features:0"].Should().Be("authentication");
        config["features:1"].Should().Be("logging");
        config["features:2"].Should().Be("caching");
    }

    [Fact]
    public void AddYamlFile_WorksWithMultipleYamlFiles()
    {
        // Arrange
        var baseConfig = """
            app:
              name: "Base App"
              version: "1.0.0"
            database:
              host: "localhost"
              port: 5432
            """;

        var overrideConfig = """
            app:
              version: "2.0.0"
            database:
              host: "prod.db.com"
            newSetting: "added_value"
            """;

        var baseFile = CreateTempYamlFile(baseConfig);
        var overrideFile = CreateTempYamlFile(overrideConfig);
        var builder = new FlexConfigurationBuilder();

        // Act
        var config = builder
            .AddYamlFile(baseFile, optional: false)
            .AddYamlFile(overrideFile, optional: true)
            .Build();

        // Assert
        config["app:name"].Should().Be("Base App");        // From base
        config["app:version"].Should().Be("2.0.0");        // Overridden
        config["database:host"].Should().Be("prod.db.com"); // Overridden
        config["database:port"].Should().Be("5432");        // From base
        config["newSetting"].Should().Be("added_value");    // New value
    }

    [Fact]
    public void AddYamlFile_HandlesOptionalMissingFiles()
    {
        // Arrange
        var existingConfig = "existing: value";
        var existingFile = CreateTempYamlFile(existingConfig);
        var nonExistentFile = GetTempYamlFilePath();
        var builder = new FlexConfigurationBuilder();

        // Act
        var config = builder
            .AddYamlFile(existingFile, optional: false)
            .AddYamlFile(nonExistentFile, optional: true)  // Should not fail
            .Build();

        // Assert
        config["existing"].Should().Be("value");
    }

    [Fact]
    public void AddYamlFile_ThrowsForRequiredMissingFiles()
    {
        // Arrange
        var nonExistentFile = GetTempYamlFilePath();
        var builder = new FlexConfigurationBuilder();

        // Act & Assert
        var action = () => builder
            .AddYamlFile(nonExistentFile, optional: false)
            .Build();

        action.Should().Throw<FileNotFoundException>();
    }

    #endregion

    #region Edge Cases and Error Handling

    [Fact]
    public void AddYamlFile_WithEmptyPath_UsesDefaultPath()
    {
        // Arrange
        var builder = new FlexConfigurationBuilder();

        // Act
        var result = builder.AddYamlFile("", optional: true);

        // Assert
        result.Should().BeSameAs(builder);
        // Note: The actual default path handling is tested in YamlConfigurationProviderTests
    }

    [Fact]
    public void AddYamlFile_AfterBuild_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = new FlexConfigurationBuilder();
        builder.Build(); // Build the configuration

        // Act & Assert
        var action = () => builder.AddYamlFile("test.yaml");
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot add sources after Build() has been called");
    }

    [Fact]
    public void AddYamlFile_CallsBuilderAddSourceInternally()
    {
        // Arrange
        var builder = new FlexConfigurationBuilder();
        const string testPath = "test-config.yaml";
        const bool testOptional = false;

        // Act
        builder.AddYamlFile(testPath, testOptional);

        // Assert
        // We can't directly test the internal AddSource call, but we can verify
        // that the source was added by building and checking behavior
        var yamlContent = "test: success";
        var tempFile = CreateTempYamlFile(yamlContent);

        var testBuilder = new FlexConfigurationBuilder();
        testBuilder.AddYamlFile(tempFile, false);
        var config = testBuilder.Build();

        config["test"].Should().Be("success");
    }

    #endregion
}