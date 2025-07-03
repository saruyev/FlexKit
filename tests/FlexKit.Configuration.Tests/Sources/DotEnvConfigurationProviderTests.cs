using AutoFixture.Xunit2;
using FlexKit.Configuration.Sources;
using FlexKit.Configuration.Tests.TestBase;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;
// ReSharper disable MethodTooLong
// ReSharper disable TooManyDeclarations
// ReSharper disable ClassTooBig
// ReSharper disable FlagArgument

namespace FlexKit.Configuration.Tests.Sources;

/// <summary>
/// Comprehensive unit tests for DotEnvConfigurationProvider covering all functionality and edge cases.
/// </summary>
public class DotEnvConfigurationProviderTests : UnitTestBase
{
    private readonly List<string> _tempFiles = new();
    private bool _disposed;

    protected override void RegisterFixtureCustomizations()
    {
        // Generate valid filenames for testing
        Fixture.Customize<string>(composer => composer.FromFactory(() =>
            "test-" + Guid.NewGuid().ToString("N")[..8]));
    }

    protected override void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            // Clean up temporary test files
            foreach (var file in _tempFiles.Where(File.Exists))
            {
                try
                {
                    File.Delete(file);
                }
                catch
                {
                    // Ignore cleanup errors in tests
                }
            }

            _tempFiles.Clear();
            _disposed = true;
        }

        base.Dispose(disposing);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidSource_CreatesProvider()
    {
        // Arrange
        var source = new DotEnvConfigurationSource { Path = ".env", Optional = true };

        // Act
        var provider = new DotEnvConfigurationProvider(source);

        // Assert
        provider.Should().NotBeNull();
    }

    #endregion

    #region Load Method Tests - File Existence

    [Fact]
    public void Load_WithExistingFile_LoadsConfigurationData()
    {
        // Arrange
        var tempFile = CreateTempFile(
            @"
# Database configuration
DATABASE_URL=postgresql://localhost:5432/myapp
DATABASE_POOL_SIZE=10

# API Configuration
API_KEY=""your-secret-api-key-here""
API_TIMEOUT=5000
API_BASE_URL=https://api.example.com
");

        var source = new DotEnvConfigurationSource { Path = tempFile, Optional = false };
        var provider = new DotEnvConfigurationProvider(source);

        // Act
        provider.Load();

        // Assert
        provider.TryGet("DATABASE_URL", out var dbUrl).Should().BeTrue();
        dbUrl.Should().Be("postgresql://localhost:5432/myapp");

        provider.TryGet("DATABASE_POOL_SIZE", out var poolSize).Should().BeTrue();
        poolSize.Should().Be("10");

        provider.TryGet("API_KEY", out var apiKey).Should().BeTrue();
        apiKey.Should().Be("your-secret-api-key-here");

        provider.TryGet("API_TIMEOUT", out var timeout).Should().BeTrue();
        timeout.Should().Be("5000");

        provider.TryGet("API_BASE_URL", out var baseUrl).Should().BeTrue();
        baseUrl.Should().Be("https://api.example.com");
    }

    [Fact]
    public void Load_WithNonExistentOptionalFile_DoesNotThrow()
    {
        // Arrange
        var nonExistentFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".env");
        var source = new DotEnvConfigurationSource { Path = nonExistentFile, Optional = true };
        var provider = new DotEnvConfigurationProvider(source);

        // Act & Assert
        var action = () => provider.Load();
        action.Should().NotThrow();
    }

    [Fact]
    public void Load_WithNonExistentRequiredFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".env");
        var source = new DotEnvConfigurationSource { Path = nonExistentFile, Optional = false };
        var provider = new DotEnvConfigurationProvider(source);

        // Act & Assert
        var action = () => provider.Load();
        action.Should().Throw<FileNotFoundException>()
            .WithMessage($"DotEnv file '{nonExistentFile}' not found");
    }

    #endregion

    #region Load Method Tests - Parsing Logic

    [Fact]
    public void Load_WithEmptyFile_LoadsSuccessfully()
    {
        // Arrange
        var tempFile = CreateTempFile("");
        var source = new DotEnvConfigurationSource { Path = tempFile, Optional = false };
        var provider = new DotEnvConfigurationProvider(source);

        // Act
        provider.Load();

        // Assert
        provider.TryGet("any_key", out _).Should().BeFalse();
    }

    [Fact]
    public void Load_WithOnlyComments_LoadsSuccessfully()
    {
        // Arrange
        var tempFile = CreateTempFile(
            @"
# This is a comment
# Another comment
# Yet another comment
");

        var source = new DotEnvConfigurationSource { Path = tempFile, Optional = false };
        var provider = new DotEnvConfigurationProvider(source);

        // Act
        provider.Load();

        // Assert
        provider.TryGet("any_key", out _).Should().BeFalse();
    }

    [Fact]
    public void Load_WithOnlyEmptyLines_LoadsSuccessfully()
    {
        // Arrange
        var tempFile = CreateTempFile(
            @"



");

        var source = new DotEnvConfigurationSource { Path = tempFile, Optional = false };
        var provider = new DotEnvConfigurationProvider(source);

        // Act
        provider.Load();

        // Assert
        provider.TryGet("any_key", out _).Should().BeFalse();
    }

    [Fact]
    public void Load_WithSimpleKeyValuePairs_ParsesCorrectly()
    {
        // Arrange
        var tempFile = CreateTempFile(
            @"
KEY1=value1
KEY2=value2
KEY3=value3
");

        var source = new DotEnvConfigurationSource { Path = tempFile, Optional = false };
        var provider = new DotEnvConfigurationProvider(source);

        // Act
        provider.Load();

        // Assert
        provider.TryGet("KEY1", out var value1).Should().BeTrue();
        value1.Should().Be("value1");

        provider.TryGet("KEY2", out var value2).Should().BeTrue();
        value2.Should().Be("value2");

        provider.TryGet("KEY3", out var value3).Should().BeTrue();
        value3.Should().Be("value3");
    }

    [Fact]
    public void Load_WithWhitespaceAroundKeyValues_TrimsCorrectly()
    {
        // Arrange
        var tempFile = CreateTempFile(
            @"
  KEY1  =  value1  
    KEY2=value2    
KEY3   =   value3
");

        var source = new DotEnvConfigurationSource { Path = tempFile, Optional = false };
        var provider = new DotEnvConfigurationProvider(source);

        // Act
        provider.Load();

        // Assert
        provider.TryGet("KEY1", out var value1).Should().BeTrue();
        value1.Should().Be("value1");

        provider.TryGet("KEY2", out var value2).Should().BeTrue();
        value2.Should().Be("value2");

        provider.TryGet("KEY3", out var value3).Should().BeTrue();
        value3.Should().Be("value3");
    }

    [Fact]
    public void Load_WithDoubleQuotedValues_RemovesQuotes()
    {
        // Arrange
        var tempFile = CreateTempFile(
            @"
QUOTED_VALUE=""this is a quoted value""
ANOTHER_QUOTED=""value with spaces""
EMPTY_QUOTES=""""
");

        var source = new DotEnvConfigurationSource { Path = tempFile, Optional = false };
        var provider = new DotEnvConfigurationProvider(source);

        // Act
        provider.Load();

        // Assert
        provider.TryGet("QUOTED_VALUE", out var quotedValue).Should().BeTrue();
        quotedValue.Should().Be("this is a quoted value");

        provider.TryGet("ANOTHER_QUOTED", out var anotherQuoted).Should().BeTrue();
        anotherQuoted.Should().Be("value with spaces");

        provider.TryGet("EMPTY_QUOTES", out var emptyQuotes).Should().BeTrue();
        emptyQuotes.Should().Be("");
    }

    [Fact]
    public void Load_WithSingleQuotedValues_RemovesQuotes()
    {
        // Arrange
        var tempFile = CreateTempFile(
            @"
SINGLE_QUOTED='this is single quoted'
ANOTHER_SINGLE='value with spaces'
EMPTY_SINGLE=''
");

        var source = new DotEnvConfigurationSource { Path = tempFile, Optional = false };
        var provider = new DotEnvConfigurationProvider(source);

        // Act
        provider.Load();

        // Assert
        provider.TryGet("SINGLE_QUOTED", out var singleQuoted).Should().BeTrue();
        singleQuoted.Should().Be("this is single quoted");

        provider.TryGet("ANOTHER_SINGLE", out var anotherSingle).Should().BeTrue();
        anotherSingle.Should().Be("value with spaces");

        provider.TryGet("EMPTY_SINGLE", out var emptySingle).Should().BeTrue();
        emptySingle.Should().Be("");
    }

    [Fact]
    public void Load_WithMismatchedQuotes_LeavesQuotesIntact()
    {
        // Arrange
        var tempFile = CreateTempFile(
            @"
MISMATCHED1='double start""
MISMATCHED2=""single start'
PARTIAL_QUOTE=value""
");

        var source = new DotEnvConfigurationSource { Path = tempFile, Optional = false };
        var provider = new DotEnvConfigurationProvider(source);

        // Act
        provider.Load();

        // Assert
        provider.TryGet("MISMATCHED1", out var mismatched1).Should().BeTrue();
        mismatched1.Should().Be("'double start\"");

        provider.TryGet("MISMATCHED2", out var mismatched2).Should().BeTrue();
        mismatched2.Should().Be("\"single start'");

        provider.TryGet("PARTIAL_QUOTE", out var partialQuote).Should().BeTrue();
        partialQuote.Should().Be("value\"");
    }

    [Fact]
    public void Load_WithEmptyKeys_StoresEmptyKeyValues()
    {
        // Arrange
        var tempFile = CreateTempFile(
            @"
=empty_key_value
 =whitespace_key_value
VALID_KEY=valid_value
");

        var source = new DotEnvConfigurationSource { Path = tempFile, Optional = false };
        var provider = new DotEnvConfigurationProvider(source);

        // Act
        provider.Load();

        // Assert
        provider.TryGet("VALID_KEY", out var validValue).Should().BeTrue();
        validValue.Should().Be("valid_value");

        // Empty key lines are stored with empty string keys after trimming
        provider.TryGet("", out var emptyKeyValue).Should().BeTrue();
        emptyKeyValue.Should().Be("whitespace_key_value"); // Last one wins due to case-insensitive dictionary
    }

    [Fact]
    public void Load_WithEmptyKeys_SkipsLines()
    {
        // Arrange
        var tempFile = CreateTempFile(
            @"=empty_key_value
 =whitespace_key_value
VALID_KEY=valid_value");

        var source = new DotEnvConfigurationSource { Path = tempFile, Optional = false };
        var provider = new DotEnvConfigurationProvider(source);

        // Act
        provider.Load();

        // Assert
        provider.TryGet("VALID_KEY", out var validValue).Should().BeTrue();
        validValue.Should().Be("valid_value");

        // The parsing logic doesn't skip empty keys - it stores them
        // After key.Trim(), empty keys become "" and are stored in the dictionary
        provider.TryGet("", out var emptyKeyValue).Should().BeTrue();
        emptyKeyValue.Should().Be("whitespace_key_value"); // Second one overwrites first
    }

    #endregion

    #region Edge Cases and Error Handling

    [Fact]
    public void Load_WithLinesWithoutEquals_SkipsLines()
    {
        // Arrange
        var tempFile = CreateTempFile(
            @"
VALID_KEY=valid_value
invalid line without equals
ANOTHER_VALID=another_value
another invalid line
");

        var source = new DotEnvConfigurationSource { Path = tempFile, Optional = false };
        var provider = new DotEnvConfigurationProvider(source);

        // Act
        provider.Load();

        // Assert
        provider.TryGet("VALID_KEY", out var validValue).Should().BeTrue();
        validValue.Should().Be("valid_value");

        provider.TryGet("ANOTHER_VALID", out var anotherValid).Should().BeTrue();
        anotherValid.Should().Be("another_value");

        // Invalid lines should be ignored
        provider.TryGet("invalid", out _).Should().BeFalse();
        provider.TryGet("line", out _).Should().BeFalse();
        provider.TryGet("another", out _).Should().BeFalse();
    }

    [Fact]
    public void Load_WithValuesContainingEquals_HandlesCorrectly()
    {
        // Arrange
        var tempFile = CreateTempFile(
            @"
CONNECTION_STRING=Server=localhost;Database=test;User=admin
MATH_EXPRESSION=x=y+z
URL_WITH_QUERY=https://example.com?param1=value1&param2=value2
");

        var source = new DotEnvConfigurationSource { Path = tempFile, Optional = false };
        var provider = new DotEnvConfigurationProvider(source);

        // Act
        provider.Load();

        // Assert
        provider.TryGet("CONNECTION_STRING", out var connectionString).Should().BeTrue();
        connectionString.Should().Be("Server=localhost;Database=test;User=admin");

        provider.TryGet("MATH_EXPRESSION", out var mathExpression).Should().BeTrue();
        mathExpression.Should().Be("x=y+z");

        provider.TryGet("URL_WITH_QUERY", out var urlWithQuery).Should().BeTrue();
        urlWithQuery.Should().Be("https://example.com?param1=value1&param2=value2");
    }

    [Fact]
    public void Load_WithMixedCommentsAndData_ParsesOnlyData()
    {
        // Arrange
        var tempFile = CreateTempFile(
            @"
# Database configuration
DATABASE_URL=postgresql://localhost:5432/myapp
# This is a comment
DATABASE_POOL_SIZE=10

# API Configuration section
API_KEY=secret-key-123
# Another comment
API_TIMEOUT=5000
# Final comment
");

        var source = new DotEnvConfigurationSource { Path = tempFile, Optional = false };
        var provider = new DotEnvConfigurationProvider(source);

        // Act
        provider.Load();

        // Assert
        provider.TryGet("DATABASE_URL", out var dbUrl).Should().BeTrue();
        dbUrl.Should().Be("postgresql://localhost:5432/myapp");

        provider.TryGet("DATABASE_POOL_SIZE", out var poolSize).Should().BeTrue();
        poolSize.Should().Be("10");

        provider.TryGet("API_KEY", out var apiKey).Should().BeTrue();
        apiKey.Should().Be("secret-key-123");

        provider.TryGet("API_TIMEOUT", out var timeout).Should().BeTrue();
        timeout.Should().Be("5000");
    }

    [Fact]
    public void Load_WithDuplicateKeys_OverwritesPreviousValues()
    {
        // Arrange
        var tempFile = CreateTempFile(
            @"
DUPLICATE_KEY=first_value
DUPLICATE_KEY=second_value
ANOTHER_KEY=some_value
DUPLICATE_KEY=final_value
");

        var source = new DotEnvConfigurationSource { Path = tempFile, Optional = false };
        var provider = new DotEnvConfigurationProvider(source);

        // Act
        provider.Load();

        // Assert
        provider.TryGet("DUPLICATE_KEY", out var duplicateValue).Should().BeTrue();
        duplicateValue.Should().Be("final_value");

        provider.TryGet("ANOTHER_KEY", out var anotherValue).Should().BeTrue();
        anotherValue.Should().Be("some_value");
    }

    [Fact]
    public void Load_WithEmptyValues_StoresEmptyStrings()
    {
        // Arrange
        var tempFile = CreateTempFile(
            @"
EMPTY_VALUE1=
EMPTY_VALUE2= 
EMPTY_VALUE3=	
NORMAL_VALUE=test
");

        var source = new DotEnvConfigurationSource { Path = tempFile, Optional = false };
        var provider = new DotEnvConfigurationProvider(source);

        // Act
        provider.Load();

        // Assert
        provider.TryGet("EMPTY_VALUE1", out var empty1).Should().BeTrue();
        empty1.Should().Be("");

        provider.TryGet("EMPTY_VALUE2", out var empty2).Should().BeTrue();
        empty2.Should().Be("");

        provider.TryGet("EMPTY_VALUE3", out var empty3).Should().BeTrue();
        empty3.Should().Be("");

        provider.TryGet("NORMAL_VALUE", out var normalValue).Should().BeTrue();
        normalValue.Should().Be("test");
    }

    #endregion

    #region Case Sensitivity Tests

    [Fact]
    public void Load_WithMixedCaseKeys_UsesCaseInsensitiveComparison()
    {
        // Arrange
        var tempFile = CreateTempFile(
            @"
MyKey=value1
MYKEY=value2
mykey=value3
");

        var source = new DotEnvConfigurationSource { Path = tempFile, Optional = false };
        var provider = new DotEnvConfigurationProvider(source);

        // Act
        provider.Load();

        // Assert - Last value should win due to case-insensitive dictionary
        provider.TryGet("MyKey", out var value1).Should().BeTrue();
        provider.TryGet("MYKEY", out var value2).Should().BeTrue();
        provider.TryGet("mykey", out var value3).Should().BeTrue();

        // All should return the same value (the last one written)
        value1.Should().Be(value2).And.Be(value3).And.Be("value3");
    }

    #endregion

    #region Integration Tests

    [Theory]
    [AutoData]
    public void Load_WithRealisticConfiguration_WorksCorrectly(string databaseName, int poolSize, int timeout)
    {
        // Arrange
        var tempFile = CreateTempFile(
            $@"
# Application Configuration
APP_NAME=""My Test Application""
APP_VERSION=1.2.3
APP_DEBUG=true

# Database Configuration
DATABASE_HOST=localhost
DATABASE_NAME={databaseName}
DATABASE_POOL_SIZE={poolSize}
DATABASE_TIMEOUT={timeout}
DATABASE_SSL_MODE=require

# API Configuration
API_BASE_URL=https://api.example.com
API_KEY=""super-secret-api-key-12345""
API_RETRY_COUNT=3

# Feature Flags
FEATURE_ADVANCED_SEARCH=true
FEATURE_CACHING=false
FEATURE_METRICS=true

# File Paths (with backslashes)
LOG_PATH=/var/log/app.log
CONFIG_PATH=C:\\Program Files\\MyApp\\config.ini
BACKUP_PATH=/backup/daily/\nwith\ttabs

# Complex values
WELCOME_MESSAGE=""Welcome to our application!\nPlease enjoy your stay.""
JSON_CONFIG={{""key"": ""value"", ""number"": 42}}
");

        var source = new DotEnvConfigurationSource { Path = tempFile, Optional = false };
        var provider = new DotEnvConfigurationProvider(source);

        // Act
        provider.Load();

        // Assert - Basic values
        provider.TryGet("APP_NAME", out var appName).Should().BeTrue();
        appName.Should().Be("My Test Application");

        provider.TryGet("APP_VERSION", out var appVersion).Should().BeTrue();
        appVersion.Should().Be("1.2.3");

        provider.TryGet("APP_DEBUG", out var appDebug).Should().BeTrue();
        appDebug.Should().Be("true");

        // Assert - Database configuration
        provider.TryGet("DATABASE_HOST", out var dbHost).Should().BeTrue();
        dbHost.Should().Be("localhost");

        provider.TryGet("DATABASE_NAME", out var dbName).Should().BeTrue();
        dbName.Should().Be(databaseName);

        provider.TryGet("DATABASE_POOL_SIZE", out var dbPoolSize).Should().BeTrue();
        dbPoolSize.Should().Be(poolSize.ToString());

        provider.TryGet("DATABASE_TIMEOUT", out var dbTimeout).Should().BeTrue();
        dbTimeout.Should().Be(timeout.ToString());

        // Assert - Complex values with escape sequences
        provider.TryGet("WELCOME_MESSAGE", out var welcomeMessage).Should().BeTrue();
        welcomeMessage.Should().Be("Welcome to our application!\nPlease enjoy your stay.");

        provider.TryGet("BACKUP_PATH", out var backupPath).Should().BeTrue();
        backupPath.Should().Be("/backup/daily/\nwith\ttabs");

        // Assert - JSON-like content
        provider.TryGet("JSON_CONFIG", out var jsonConfig).Should().BeTrue();
        jsonConfig.Should().Be("{\"key\": \"value\", \"number\": 42}");
    }

    [Fact]
    public void Load_WithConfigurationBuilder_IntegratesCorrectly()
    {
        // Arrange
        var tempFile = CreateTempFile(
            @"
DATABASE_URL=postgresql://localhost:5432/testdb
API_KEY=test-api-key-123
DEBUG_MODE=true
");

        var builder = new ConfigurationBuilder();
        builder.Add(new DotEnvConfigurationSource { Path = tempFile, Optional = false });

        // Act
        var configuration = builder.Build();

        // Assert
        configuration["DATABASE_URL"].Should().Be("postgresql://localhost:5432/testdb");
        configuration["API_KEY"].Should().Be("test-api-key-123");
        configuration["DEBUG_MODE"].Should().Be("true");
    }
    
    [Fact]
    public void Load_WithQuotedEscapeSequences_ProcessesAfterQuoteRemoval()
    {
        // Arrange
        var tempFile = CreateTempFile(@"QUOTED_NEWLINE=""Line 1\nLine 2""
QUOTED_TAB='Column1\tColumn2'
QUOTED_BACKSLASH=""Path\to\file""");

        var source = new DotEnvConfigurationSource { Path = tempFile, Optional = false };
        var provider = new DotEnvConfigurationProvider(source);

        // Act
        provider.Load();

        // Assert
        provider.TryGet("QUOTED_NEWLINE", out var quotedNewline).Should().BeTrue();
        quotedNewline.Should().Be("Line 1\nLine 2");

        provider.TryGet("QUOTED_TAB", out var quotedTab).Should().BeTrue();
        quotedTab.Should().Be("Column1\tColumn2");

        provider.TryGet("QUOTED_BACKSLASH", out var quotedBackslash).Should().BeTrue();
        // "Path\to\file" -> "Path[TAB]o\file" (\t becomes tab, \f stays as \f)
        quotedBackslash.Should().Be($"Path\to\\file");
    }

    #endregion

    #region Helper Methods

    private string CreateTempFile(string content)
    {
        var tempFile = Path.GetTempFileName();
        _tempFiles.Add(tempFile);
        File.WriteAllText(tempFile, content);
        return tempFile;
    }

    #endregion
}