using FlexKit.Configuration.Providers.Yaml.Sources;
using FlexKit.Configuration.Providers.Yaml.Tests.TestBase;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace FlexKit.Configuration.Providers.Yaml.Tests.Sources;

/// <summary>
/// Comprehensive unit tests for YamlConfigurationProvider covering all functionality and edge cases.
/// Tests file loading, YAML parsing, error handling, and data flattening.
/// </summary>
public class YamlConfigurationProviderTests : YamlTestBase
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidSource_CreatesProvider()
    {
        // Arrange
        var source = new YamlConfigurationSource { Path = "test.yaml", Optional = true };

        // Act
        var provider = new YamlConfigurationProvider(source);

        // Assert
        provider.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullSource_ThrowsArgumentNullException()
    {
        // Act & Assert
        var action = () => new YamlConfigurationProvider(null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("source");
    }

    #endregion

    #region Load Method Tests - File Existence

    [Fact]
    public void Load_WithExistingFile_LoadsConfigurationData()
    {
        // Arrange
        var yamlContent = """
            # Database configuration
            database:
              host: localhost
              port: 5432
              name: myapp_db
            
            # API Configuration
            api:
              key: "your-secret-api-key-here"
              timeout: 5000
              baseUrl: "https://api.example.com"
            """;

        var tempFile = CreateTempYamlFile(yamlContent);
        var source = new YamlConfigurationSource { Path = tempFile, Optional = false };
        var provider = new YamlConfigurationProvider(source);

        // Act
        provider.Load();

        // Assert
        provider.TryGet("database:host", out var dbHost).Should().BeTrue();
        dbHost.Should().Be("localhost");

        provider.TryGet("database:port", out var dbPort).Should().BeTrue();
        dbPort.Should().Be("5432");

        provider.TryGet("api:key", out var apiKey).Should().BeTrue();
        apiKey.Should().Be("your-secret-api-key-here");

        provider.TryGet("api:timeout", out var timeout).Should().BeTrue();
        timeout.Should().Be("5000");

        provider.TryGet("api:baseUrl", out var baseUrl).Should().BeTrue();
        baseUrl.Should().Be("https://api.example.com");
    }

    [Fact]
    public void Load_WithNonExistentOptionalFile_LoadsEmptyConfiguration()
    {
        // Arrange
        var nonExistentFile = GetTempYamlFilePath();
        var source = new YamlConfigurationSource { Path = nonExistentFile, Optional = true };
        var provider = new YamlConfigurationProvider(source);

        // Act
        provider.Load();

        // Assert
        provider.GetChildKeys([], null).Should().BeEmpty();
    }

    [Fact]
    public void Load_WithNonExistentRequiredFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentFile = GetTempYamlFilePath();
        var source = new YamlConfigurationSource { Path = nonExistentFile, Optional = false };
        var provider = new YamlConfigurationProvider(source);

        // Act & Assert
        var action = () => provider.Load();
        action.Should().Throw<FileNotFoundException>()
            .WithMessage($"The configuration file '{nonExistentFile}' was not found and is not optional.");
    }

    [Fact]
    public void Load_WithEmptyFile_LoadsEmptyConfiguration()
    {
        // Arrange
        var tempFile = CreateTempYamlFile("");
        var source = new YamlConfigurationSource { Path = tempFile, Optional = false };
        var provider = new YamlConfigurationProvider(source);

        // Act
        provider.Load();

        // Assert
        provider.GetChildKeys([], null).Should().BeEmpty();
    }

    [Fact]
    public void Load_WithWhitespaceOnlyFile_LoadsEmptyConfiguration()
    {
        // Arrange
        var tempFile = CreateTempYamlFile("   \n\t  \r\n  ");
        var source = new YamlConfigurationSource { Path = tempFile, Optional = false };
        var provider = new YamlConfigurationProvider(source);

        // Act
        provider.Load();

        // Assert
        provider.GetChildKeys([], null).Should().BeEmpty();
    }

    [Fact]
    public void Load_WithCommentsOnlyFile_LoadsEmptyConfiguration()
    {
        // Arrange
        var yamlContent = """
            # This is a comment
            # Another comment
            #
            # More comments
            """;
        var tempFile = CreateTempYamlFile(yamlContent);
        var source = new YamlConfigurationSource { Path = tempFile, Optional = false };
        var provider = new YamlConfigurationProvider(source);

        // Act
        provider.Load();

        // Assert
        provider.GetChildKeys([], null).Should().BeEmpty();
    }

    #endregion

    #region Load Method Tests - YAML Parsing

    [Fact]
    public void Load_WithScalarValues_ParsesAllDataTypes()
    {
        // Arrange
        var yamlContent = """
            stringValue: "Hello World"
            integerValue: 42
            floatValue: 3.14
            booleanTrue: true
            booleanFalse: false
            nullValue: null
            emptyString: ""
            quotedNumber: "123"
            """;

        var tempFile = CreateTempYamlFile(yamlContent);
        var source = new YamlConfigurationSource { Path = tempFile, Optional = false };
        var provider = new YamlConfigurationProvider(source);

        // Act
        provider.Load();

        // Assert
        provider.TryGet("stringValue", out var stringVal).Should().BeTrue();
        stringVal.Should().Be("Hello World");

        provider.TryGet("integerValue", out var intVal).Should().BeTrue();
        intVal.Should().Be("42");

        provider.TryGet("floatValue", out var floatVal).Should().BeTrue();
        floatVal.Should().Be("3.14");

        provider.TryGet("booleanTrue", out var boolTrue).Should().BeTrue();
        boolTrue.Should().Be("true");

        provider.TryGet("booleanFalse", out var boolFalse).Should().BeTrue();
        boolFalse.Should().Be("false");

        provider.TryGet("nullValue", out var nullVal).Should().BeTrue();
        nullVal.Should().BeNull();

        provider.TryGet("emptyString", out var emptyVal).Should().BeTrue();
        emptyVal.Should().Be("");

        provider.TryGet("quotedNumber", out var quotedNum).Should().BeTrue();
        quotedNum.Should().Be("123");
    }

    [Fact]
    public void Load_WithNestedObjects_FlattensHierarchy()
    {
        // Arrange
        var yamlContent = """
            database:
              connection:
                host: localhost
                port: 5432
              pool:
                min: 5
                max: 20
            """;

        var tempFile = CreateTempYamlFile(yamlContent);
        var source = new YamlConfigurationSource { Path = tempFile, Optional = false };
        var provider = new YamlConfigurationProvider(source);

        // Act
        provider.Load();

        // Assert
        provider.TryGet("database:connection:host", out var host).Should().BeTrue();
        host.Should().Be("localhost");

        provider.TryGet("database:connection:port", out var port).Should().BeTrue();
        port.Should().Be("5432");

        provider.TryGet("database:pool:min", out var min).Should().BeTrue();
        min.Should().Be("5");

        provider.TryGet("database:pool:max", out var max).Should().BeTrue();
        max.Should().Be("20");
    }

    [Fact]
    public void Load_WithArrays_FlattensToIndexedKeys()
    {
        // Arrange
        var yamlContent = """
            servers:
              - name: web1
                port: 8080
              - name: web2
                port: 8081
            features:
              - caching
              - logging
              - metrics
            """;

        var tempFile = CreateTempYamlFile(yamlContent);
        var source = new YamlConfigurationSource { Path = tempFile, Optional = false };
        var provider = new YamlConfigurationProvider(source);

        // Act
        provider.Load();

        // Assert
        provider.TryGet("servers:0:name", out var server1Name).Should().BeTrue();
        server1Name.Should().Be("web1");

        provider.TryGet("servers:0:port", out var server1Port).Should().BeTrue();
        server1Port.Should().Be("8080");

        provider.TryGet("servers:1:name", out var server2Name).Should().BeTrue();
        server2Name.Should().Be("web2");

        provider.TryGet("servers:1:port", out var server2Port).Should().BeTrue();
        server2Port.Should().Be("8081");

        provider.TryGet("features:0", out var feature1).Should().BeTrue();
        feature1.Should().Be("caching");

        provider.TryGet("features:1", out var feature2).Should().BeTrue();
        feature2.Should().Be("logging");

        provider.TryGet("features:2", out var feature3).Should().BeTrue();
        feature3.Should().Be("metrics");
    }

    [Fact]
    public void Load_WithComplexNestedStructure_FlattensCorrectly()
    {
        // Arrange
        var yamlContent = """
            application:
              name: "My App"
              version: "1.0.0"
              environments:
                - name: development
                  database:
                    host: dev.db.com
                    credentials:
                      username: dev_user
                      password: dev_pass
                - name: production
                  database:
                    host: prod.db.com
                    credentials:
                      username: prod_user
                      password: prod_pass
            """;

        var tempFile = CreateTempYamlFile(yamlContent);
        var source = new YamlConfigurationSource { Path = tempFile, Optional = false };
        var provider = new YamlConfigurationProvider(source);

        // Act
        provider.Load();

        // Assert
        provider.TryGet("application:name", out var appName).Should().BeTrue();
        appName.Should().Be("My App");

        provider.TryGet("application:environments:0:name", out var env1Name).Should().BeTrue();
        env1Name.Should().Be("development");

        provider.TryGet("application:environments:0:database:credentials:username", out var devUser).Should().BeTrue();
        devUser.Should().Be("dev_user");

        provider.TryGet("application:environments:1:database:host", out var prodHost).Should().BeTrue();
        prodHost.Should().Be("prod.db.com");
    }

    [Fact]
    public void Load_WithRootArray_FlattensToIndexedKeys()
    {
        // Arrange
        var yamlContent = """
            - name: first
              value: 1
            - name: second
              value: 2
            """;

        var tempFile = CreateTempYamlFile(yamlContent);
        var source = new YamlConfigurationSource { Path = tempFile, Optional = false };
        var provider = new YamlConfigurationProvider(source);

        // Act
        provider.Load();

        // Assert
        provider.TryGet("0:name", out var firstName).Should().BeTrue();
        firstName.Should().Be("first");

        provider.TryGet("0:value", out var firstValue).Should().BeTrue();
        firstValue.Should().Be("1");

        provider.TryGet("1:name", out var secondName).Should().BeTrue();
        secondName.Should().Be("second");

        provider.TryGet("1:value", out var secondValue).Should().BeTrue();
        secondValue.Should().Be("2");
    }

    [Fact]
    public void Load_WithRootScalar_CreatesEmptyKeyEntry()
    {
        // Arrange
        var yamlContent = "simple_value";

        var tempFile = CreateTempYamlFile(yamlContent);
        var source = new YamlConfigurationSource { Path = tempFile, Optional = false };
        var provider = new YamlConfigurationProvider(source);

        // Act
        provider.Load();

        // Assert
        provider.TryGet("", out var value).Should().BeTrue();
        value.Should().Be("simple_value");
    }

    #endregion

    #region Load Method Tests - Error Handling

    [Fact]
    public void Load_WithInvalidYamlSyntax_ThrowsInvalidDataException()
    {
        // Arrange
        var invalidYaml = """
            database:
              host: localhost
            invalid_yaml_syntax: [unclosed_bracket
            """;

        var tempFile = CreateTempYamlFile(invalidYaml);
        var source = new YamlConfigurationSource { Path = tempFile, Optional = false };
        var provider = new YamlConfigurationProvider(source);

        // Act & Assert
        var action = () => provider.Load();
        action.Should().Throw<InvalidDataException>()
            .WithMessage($"Failed to parse YAML configuration file '{tempFile}':*");
    }

    [Fact]
    public void Load_WithDuplicateKeys_TakesLastValue()
    {
        // Arrange
        var yamlContent = """
            key: first_value
            key: second_value
            """;

        var tempFile = CreateTempYamlFile(yamlContent);
        var source = new YamlConfigurationSource { Path = tempFile, Optional = false };
        var provider = new YamlConfigurationProvider(source);

        // Act
        provider.Load();

        // Assert
        provider.TryGet("key", out var value).Should().BeTrue();
        value.Should().Be("second_value");
    }

    [Fact]
    public void Load_MultipleTimesOnSameProvider_ReloadsData()
    {
        // Arrange
        var yamlContent1 = "key: value1";
        var tempFile = CreateTempYamlFile(yamlContent1);
        var source = new YamlConfigurationSource { Path = tempFile, Optional = false };
        var provider = new YamlConfigurationProvider(source);

        // Act - First load
        provider.Load();
        provider.TryGet("key", out var value1).Should().BeTrue();
        value1.Should().Be("value1");

        // Update file content
        var yamlContent2 = "key: value2";
        File.WriteAllText(tempFile, yamlContent2);

        // Act - Second load
        provider.Load();

        // Assert
        provider.TryGet("key", out var value2).Should().BeTrue();
        value2.Should().Be("value2");
    }

    #endregion

    #region Load Method Tests - Special Characters and Encoding

    [Fact]
    public void Load_WithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var yamlContent = """
            specialChars: "!@#$%^&*()_+-=[]{}|;:,.<>?"
            unicode: "Hello 世界 🌍"
            multiline: |
              Line 1
              Line 2
              Line 3
            quoted: "Value with spaces and \"quotes\""
            """;

        var tempFile = CreateTempYamlFile(yamlContent);
        var source = new YamlConfigurationSource { Path = tempFile, Optional = false };
        var provider = new YamlConfigurationProvider(source);

        // Act
        provider.Load();

        // Assert
        provider.TryGet("specialChars", out var special).Should().BeTrue();
        special.Should().Be("!@#$%^&*()_+-=[]{}|;:,.<>?");

        provider.TryGet("unicode", out var unicode).Should().BeTrue();
        unicode.Should().Be("Hello 世界 🌍");

        provider.TryGet("multiline", out var multiline).Should().BeTrue();
        multiline.Should().Be("Line 1\nLine 2\nLine 3\n");

        provider.TryGet("quoted", out var quoted).Should().BeTrue();
        quoted.Should().Be("Value with spaces and \"quotes\"");
    }

    [Fact]
    public void Load_WithDifferentYamlExtensions_WorksCorrectly()
    {
        // Arrange
        var yamlContent = "test: value";
        var ymlFile = CreateTempYamlFile(yamlContent, ".yml");
        var source = new YamlConfigurationSource { Path = ymlFile, Optional = false };
        var provider = new YamlConfigurationProvider(source);

        // Act
        provider.Load();

        // Assert
        provider.TryGet("test", out var value).Should().BeTrue();
        value.Should().Be("value");
    }

    #endregion

    #region ConfigurationProvider Integration Tests

    [Fact]
    public void Provider_ImplementsConfigurationProvider()
    {
        // Arrange
        var source = new YamlConfigurationSource();

        // Act
        var provider = new YamlConfigurationProvider(source);

        // Assert
        provider.Should().BeAssignableTo<ConfigurationProvider>();
        provider.Should().BeAssignableTo<IConfigurationProvider>();
    }

    [Fact]
    public void Provider_IntegratesWithConfigurationBuilder()
    {
        // Arrange
        var yamlContent = """
            app:
              name: "Test App"
              version: "1.0.0"
            """;
        var tempFile = CreateTempYamlFile(yamlContent);
        
        var builder = new ConfigurationBuilder();
        builder.Add(new YamlConfigurationSource { Path = tempFile, Optional = false });

        // Act
        var configuration = builder.Build();

        // Assert
        configuration["app:name"].Should().Be("Test App");
        configuration["app:version"].Should().Be("1.0.0");
    }

    [Fact]
    public void Provider_SupportsGetChildKeys()
    {
        // Arrange
        var yamlContent = """
            database:
              host: localhost
              port: 5432
            api:
              url: "https://api.com"
              timeout: 5000
            """;
        var tempFile = CreateTempYamlFile(yamlContent);
        // Act
        var builder = new ConfigurationBuilder();
        builder.Add(new YamlConfigurationSource { Path = tempFile, Optional = false });
        var configuration = builder.Build();

        var databaseSection = configuration.GetSection("database");
        var databaseChildren = databaseSection.GetChildren().Select(c => c.Key);

        // Assert
        databaseChildren.Should().Contain(["host", "port"]);
    }

    #endregion
}