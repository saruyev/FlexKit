using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using Azure;
using Azure.Core;
using Azure.Data.AppConfiguration;
using Azure.Security.KeyVault.Secrets;
using FlexKit.Configuration.Core;
using FlexKit.Configuration.Providers.Azure.Extensions;
using FlexKit.Configuration.Providers.Azure.Sources;
using FlexKit.Configuration.Providers.Azure.Tests.Sources;
using FluentAssertions;
using NSubstitute;
using Xunit;
// ReSharper disable MethodTooLong
// ReSharper disable ComplexConditionExpression
// ReSharper disable ClassTooBig
// ReSharper disable NullableWarningSuppressionIsUsed

namespace FlexKit.Configuration.Providers.Azure.Tests.Extensions;

/// <summary>
/// Unit tests for AzureExtensions class covering all public extension methods.
/// 
/// NOTE: To achieve 100% code coverage including internal methods like FlattenJsonValue,
/// you need to add the following to FlexKit.Configuration.Providers.Azure.csproj:
/// 
/// <code>
/// &lt;ItemGroup&gt;
///   &lt;InternalsVisibleTo Include="FlexKit.Configuration.Providers.Azure.Tests" /&gt;
/// &lt;/ItemGroup&gt;
/// </code>
/// 
/// Without InternalsVisibleTo, this test class only covers public extension methods
/// and tests internal functionality indirectly through integration testing.
/// </summary>
public class AzureExtensionsTests
{
    #region AddAzureKeyVault Tests

    [Fact]
    public void AddAzureKeyVault_WithBuilder_ReturnsBuilderInstance()
    {
        // Arrange
        var builder = new FlexConfigurationBuilder();
        const string vaultUri = "https://test-vault.vault.azure.net/";

        // Act
        var result = builder.AddAzureKeyVault(vaultUri);

        // Assert
        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void AddAzureKeyVault_WithNullBuilder_ThrowsArgumentNullException()
    {
        // Arrange
        FlexConfigurationBuilder? nullBuilder = null;

        // Act & Assert
        var action = () => nullBuilder!.AddAzureKeyVault("https://test-vault.vault.azure.net/");

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("builder");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void AddAzureKeyVault_WithInvalidVaultUri_ThrowsArgumentException(string? vaultUri)
    {
        // Arrange
        var builder = new FlexConfigurationBuilder();

        // Act & Assert
        var action = () => builder.AddAzureKeyVault(vaultUri!);

        action.Should().Throw<ArgumentException>()
            .WithParameterName("vaultUri");
    }

    [Fact]
    public void AddAzureKeyVault_WithDefaultOptional_CreatesOptionalSource()
    {
        // Arrange
        var builder = new FlexConfigurationBuilder();
        const string vaultUri = "https://test-vault.vault.azure.net/";

        // Act
        var result = builder.AddAzureKeyVault(vaultUri);

        // Assert
        result.Should().BeSameAs(builder);

        // Use reflection to access the sources to verify the configuration
        var sourcesField = typeof(FlexConfigurationBuilder)
            .GetField("_sources", BindingFlags.NonPublic | BindingFlags.Instance);
        var sources = (List<Microsoft.Extensions.Configuration.IConfigurationSource>)sourcesField!.GetValue(builder)!;

        sources.Should().HaveCount(1);
        var source = sources[0].Should().BeOfType<AzureKeyVaultConfigurationSource>().Subject;
        source.VaultUri.Should().Be(vaultUri);
        source.Optional.Should().BeTrue(); // Default value
    }

    [Fact]
    public void AddAzureKeyVault_WithOptionalTrue_CreatesOptionalSource()
    {
        // Arrange
        var builder = new FlexConfigurationBuilder();
        const string vaultUri = "https://test-vault.vault.azure.net/";

        // Act
        var result = builder.AddAzureKeyVault(vaultUri, optional: true);

        // Assert
        result.Should().BeSameAs(builder);

        // Use reflection to access the sources to verify the configuration
        var sourcesField = typeof(FlexConfigurationBuilder)
            .GetField("_sources", BindingFlags.NonPublic | BindingFlags.Instance);
        var sources = (List<Microsoft.Extensions.Configuration.IConfigurationSource>)sourcesField!.GetValue(builder)!;

        sources.Should().HaveCount(1);
        var source = sources[0].Should().BeOfType<AzureKeyVaultConfigurationSource>().Subject;
        source.VaultUri.Should().Be(vaultUri);
        source.Optional.Should().BeTrue();
    }

    [Fact]
    public void AddAzureKeyVault_WithOptionalFalse_CreatesRequiredSource()
    {
        // Arrange
        var builder = new FlexConfigurationBuilder();
        const string vaultUri = "https://test-vault.vault.azure.net/";

        // Act
        var result = builder.AddAzureKeyVault(vaultUri, optional: false);

        // Assert
        result.Should().BeSameAs(builder);

        // Use reflection to access the sources to verify the configuration
        var sourcesField = typeof(FlexConfigurationBuilder)
            .GetField("_sources", BindingFlags.NonPublic | BindingFlags.Instance);
        var sources = (List<Microsoft.Extensions.Configuration.IConfigurationSource>)sourcesField!.GetValue(builder)!;

        sources.Should().HaveCount(1);
        var source = sources[0].Should().BeOfType<AzureKeyVaultConfigurationSource>().Subject;
        source.VaultUri.Should().Be(vaultUri);
        source.Optional.Should().BeFalse();
    }

    [Fact]
    public void AddAzureKeyVault_WithOptionsAction_ConfiguresSource()
    {
        // Arrange
        var builder = new FlexConfigurationBuilder();
        var credential = Substitute.For<TokenCredential>();
        var secretProcessor = Substitute.For<IKeyVaultSecretProcessor>();

        // Act
        var result = builder.AddAzureKeyVault(options =>
        {
            options.VaultUri = "https://configured-vault.vault.azure.net/";
            options.Optional = false;
            options.JsonProcessor = true;
            options.JsonProcessorSecrets = ["database-config", "cache-config"];
            options.ReloadAfter = TimeSpan.FromMinutes(15);
            options.Credential = credential;
            options.SecretProcessor = secretProcessor;
            options.OnLoadException = ex => _ = ex;
        });

        // Assert
        result.Should().BeSameAs(builder);

        // Use reflection to access the sources to verify the configuration
        var sourcesField = typeof(FlexConfigurationBuilder)
            .GetField("_sources", BindingFlags.NonPublic | BindingFlags.Instance);
        var sources = (List<Microsoft.Extensions.Configuration.IConfigurationSource>)sourcesField!.GetValue(builder)!;

        sources.Should().HaveCount(1);
        var source = sources[0].Should().BeOfType<AzureKeyVaultConfigurationSource>().Subject;

        source.VaultUri.Should().Be("https://configured-vault.vault.azure.net/");
        source.Optional.Should().BeFalse();
        source.JsonProcessor.Should().BeTrue();
        source.JsonProcessorSecrets.Should().BeEquivalentTo(new[] { "database-config", "cache-config" });
        source.ReloadAfter.Should().Be(TimeSpan.FromMinutes(15));
        source.Credential.Should().BeSameAs(credential);
        source.SecretProcessor.Should().BeSameAs(secretProcessor);
        source.OnLoadException.Should().NotBeNull();
    }

    [Fact]
    public void AddAzureKeyVault_WithNullOptionsAction_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new FlexConfigurationBuilder();

        // Act & Assert
        var action = () => builder.AddAzureKeyVault(null!);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("configure");
    }
    
    [Fact]
    public void AddAzureKeyVault_WithInjectedSecretClient_LoadsSecretsFromMockClient()
    {
        // Arrange
        var builder = new FlexConfigurationBuilder();
    
        // Setup mock client with test data
        var secretProperties = new[] { new SecretProperties("test--secret") { Enabled = true } };
        var pageable = AsyncPageable<SecretProperties>.FromPages([
            Page<SecretProperties>.FromValues(secretProperties, null, Substitute.For<Response>())
        ]);
        var secrets = new Dictionary<string, KeyVaultSecret>
        {
            ["test--secret"] = new("test--secret", "test-value")
        };
        var mockClient = new MockSecretClient(pageable, secrets);

        // Act
        var config = builder.AddAzureKeyVault(options =>
            {
                options.VaultUri = "https://test-vault.vault.azure.net/";
                options.SecretClient = mockClient;
                options.Optional = false;
            })
            .Build();

        // Assert
        var value = config["test:secret"];
        value.Should().Be("test-value");
    }

    #endregion

    #region AddAzureAppConfiguration Tests

    [Fact]
    public void AddAzureAppConfiguration_WithBuilder_ReturnsBuilderInstance()
    {
        // Arrange
        var builder = new FlexConfigurationBuilder();
        const string connectionString = "https://test-config.azconfig.io";

        // Act
        var result = builder.AddAzureAppConfiguration(connectionString);

        // Assert
        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void AddAzureAppConfiguration_WithNullBuilder_ThrowsArgumentNullException()
    {
        // Arrange
        FlexConfigurationBuilder? nullBuilder = null;

        // Act & Assert
        var action = () => nullBuilder!.AddAzureAppConfiguration("https://test-config.azconfig.io");

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("builder");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void AddAzureAppConfiguration_WithInvalidConnectionString_ThrowsArgumentException(string? connectionString)
    {
        // Arrange
        var builder = new FlexConfigurationBuilder();

        // Act & Assert
        var action = () => builder.AddAzureAppConfiguration(connectionString!);

        action.Should().Throw<ArgumentException>()
            .WithParameterName("connectionString");
    }

    [Fact]
    public void AddAzureAppConfiguration_WithDefaultOptional_CreatesOptionalSource()
    {
        // Arrange
        var builder = new FlexConfigurationBuilder();
        const string connectionString = "https://test-config.azconfig.io";

        // Act
        var result = builder.AddAzureAppConfiguration(connectionString);

        // Assert
        result.Should().BeSameAs(builder);

        // Use reflection to access the sources to verify the configuration
        var sourcesField = typeof(FlexConfigurationBuilder)
            .GetField("_sources", BindingFlags.NonPublic | BindingFlags.Instance);
        var sources = (List<Microsoft.Extensions.Configuration.IConfigurationSource>)sourcesField!.GetValue(builder)!;

        sources.Should().HaveCount(1);
        var source = sources[0].Should().BeOfType<AzureAppConfigurationSource>().Subject;
        source.ConnectionString.Should().Be(connectionString);
        source.Optional.Should().BeTrue(); // Default value
    }

    [Fact]
    public void AddAzureAppConfiguration_WithOptionalTrue_CreatesOptionalSource()
    {
        // Arrange
        var builder = new FlexConfigurationBuilder();
        const string connectionString = "https://test-config.azconfig.io";

        // Act
        var result = builder.AddAzureAppConfiguration(connectionString, optional: true);

        // Assert
        result.Should().BeSameAs(builder);

        // Use reflection to access the sources to verify the configuration
        var sourcesField = typeof(FlexConfigurationBuilder)
            .GetField("_sources", BindingFlags.NonPublic | BindingFlags.Instance);
        var sources = (List<Microsoft.Extensions.Configuration.IConfigurationSource>)sourcesField!.GetValue(builder)!;

        sources.Should().HaveCount(1);
        var source = sources[0].Should().BeOfType<AzureAppConfigurationSource>().Subject;
        source.ConnectionString.Should().Be(connectionString);
        source.Optional.Should().BeTrue();
    }

    [Fact]
    public void AddAzureAppConfiguration_WithOptionalFalse_CreatesRequiredSource()
    {
        // Arrange
        var builder = new FlexConfigurationBuilder();
        const string connectionString = "https://test-config.azconfig.io";

        // Act
        var result = builder.AddAzureAppConfiguration(connectionString, optional: false);

        // Assert
        result.Should().BeSameAs(builder);

        // Use reflection to access the sources to verify the configuration
        var sourcesField = typeof(FlexConfigurationBuilder)
            .GetField("_sources", BindingFlags.NonPublic | BindingFlags.Instance);
        var sources = (List<Microsoft.Extensions.Configuration.IConfigurationSource>)sourcesField!.GetValue(builder)!;

        sources.Should().HaveCount(1);
        var source = sources[0].Should().BeOfType<AzureAppConfigurationSource>().Subject;
        source.ConnectionString.Should().Be(connectionString);
        source.Optional.Should().BeFalse();
    }

    [Fact]
    public void AddAzureAppConfiguration_WithOptionsAction_ConfiguresSource()
    {
        // Arrange
        var builder = new FlexConfigurationBuilder();
        var credential = Substitute.For<TokenCredential>();

        // Act
        var result = builder.AddAzureAppConfiguration(options =>
        {
            options.ConnectionString = "https://configured-config.azconfig.io";
            options.Optional = false;
            options.KeyFilter = "myapp:*";
            options.Label = "production";
            options.ReloadAfter = TimeSpan.FromMinutes(5);
            options.Credential = credential;
            options.OnLoadException = ex => _ = ex;
        });

        // Assert
        result.Should().BeSameAs(builder);

        // Use reflection to access the sources to verify the configuration
        var sourcesField = typeof(FlexConfigurationBuilder)
            .GetField("_sources", BindingFlags.NonPublic | BindingFlags.Instance);
        var sources = (List<Microsoft.Extensions.Configuration.IConfigurationSource>)sourcesField!.GetValue(builder)!;

        sources.Should().HaveCount(1);
        var source = sources[0].Should().BeOfType<AzureAppConfigurationSource>().Subject;

        source.ConnectionString.Should().Be("https://configured-config.azconfig.io");
        source.Optional.Should().BeFalse();
        source.KeyFilter.Should().Be("myapp:*");
        source.Label.Should().Be("production");
        source.ReloadAfter.Should().Be(TimeSpan.FromMinutes(5));
        source.Credential.Should().BeSameAs(credential);
        source.OnLoadException.Should().NotBeNull();
    }

    [Fact]
    public void AddAzureAppConfiguration_WithNullOptionsAction_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new FlexConfigurationBuilder();

        // Act & Assert
        var action = () => builder.AddAzureAppConfiguration(null!);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("configure");
    }
    
    [Fact]
    public void AddAzureAppConfiguration_WithInjectedSecretClient_LoadsSecretsFromMockClient()
    {
        // Arrange
        var builder = new FlexConfigurationBuilder();
        var mockClient = Substitute.For<ConfigurationClient>();
        var firstSetting = new ConfigurationSetting("test:secret", "test-value");
        var firstSettings = AsyncPageable<ConfigurationSetting>.FromPages([
            Page<ConfigurationSetting>.FromValues([firstSetting], null, Substitute.For<Response>())
        ]);

        // First load
        mockClient?.GetConfigurationSettingsAsync(Arg.Any<SettingSelector>(), CancellationToken.None)
            .Returns(firstSettings);

        // Act
        var config = builder.AddAzureAppConfiguration(options =>
            {
                options.ConnectionString = "https://test-vault.vault.azure.net/";
                options.ConfigurationClient = mockClient;
                options.Optional = false;
            })
            .Build();

        // Assert
        var value = config["test:secret"];
        value.Should().Be("test-value");
    }

    #endregion

    #region Multiple Source Tests

    [Fact]
    public void AddAzureKeyVault_CalledMultipleTimes_AddsMultipleSources()
    {
        // Arrange
        var builder = new FlexConfigurationBuilder();

        // Act
        builder.AddAzureKeyVault("https://vault1.vault.azure.net/")
            .AddAzureKeyVault("https://vault2.vault.azure.net/");

        // Assert
        var sourcesField = typeof(FlexConfigurationBuilder)
            .GetField("_sources", BindingFlags.NonPublic | BindingFlags.Instance);
        var sources = (List<Microsoft.Extensions.Configuration.IConfigurationSource>)sourcesField!.GetValue(builder)!;

        sources.Should().HaveCount(2);
        sources.Should().AllBeOfType<AzureKeyVaultConfigurationSource>();

        var keyVaultSources = sources.Cast<AzureKeyVaultConfigurationSource>().ToList();
        keyVaultSources[0].VaultUri.Should().Be("https://vault1.vault.azure.net/");
        keyVaultSources[1].VaultUri.Should().Be("https://vault2.vault.azure.net/");
    }

    [Fact]
    public void AddAzureAppConfiguration_CalledMultipleTimes_AddsMultipleSources()
    {
        // Arrange
        var builder = new FlexConfigurationBuilder();

        // Act
        builder.AddAzureAppConfiguration("https://config1.azconfig.io")
            .AddAzureAppConfiguration("https://config2.azconfig.io");

        // Assert
        var sourcesField = typeof(FlexConfigurationBuilder)
            .GetField("_sources", BindingFlags.NonPublic | BindingFlags.Instance);
        var sources = (List<Microsoft.Extensions.Configuration.IConfigurationSource>)sourcesField!.GetValue(builder)!;

        sources.Should().HaveCount(2);
        sources.Should().AllBeOfType<AzureAppConfigurationSource>();

        var appConfigSources = sources.Cast<AzureAppConfigurationSource>().ToList();
        appConfigSources[0].ConnectionString.Should().Be("https://config1.azconfig.io");
        appConfigSources[1].ConnectionString.Should().Be("https://config2.azconfig.io");
    }

    [Fact]
    public void MixedAzureSources_CanBeAddedTogether()
    {
        // Arrange
        var builder = new FlexConfigurationBuilder();

        // Act
        builder.AddAzureKeyVault("https://vault.vault.azure.net/")
            .AddAzureAppConfiguration("https://config.azconfig.io");

        // Assert
        var sourcesField = typeof(FlexConfigurationBuilder)
            .GetField("_sources", BindingFlags.NonPublic | BindingFlags.Instance);
        var sources = (List<Microsoft.Extensions.Configuration.IConfigurationSource>)sourcesField!.GetValue(builder)!;

        sources.Should().HaveCount(2);
        sources[0].Should().BeOfType<AzureKeyVaultConfigurationSource>();
        sources[1].Should().BeOfType<AzureAppConfigurationSource>();
    }

    #endregion

    #region Edge Cases and Validation Tests

    [Fact]
    public void AddAzureKeyVault_WithWhitespaceVaultUri_ThrowsArgumentException()
    {
        // Arrange
        var builder = new FlexConfigurationBuilder();

        // Act & Assert
        var action = () => builder.AddAzureKeyVault("   ");

        action.Should().Throw<ArgumentException>()
            .WithParameterName("vaultUri");
    }

    [Fact]
    public void AddAzureAppConfiguration_WithWhitespaceConnectionString_ThrowsArgumentException()
    {
        // Arrange
        var builder = new FlexConfigurationBuilder();

        // Act & Assert
        var action = () => builder.AddAzureAppConfiguration("   ");

        action.Should().Throw<ArgumentException>()
            .WithParameterName("connectionString");
    }

    [Fact]
    public void AddAzureKeyVault_WithEmptyJsonProcessorSecrets_ConfiguresCorrectly()
    {
        // Arrange
        var builder = new FlexConfigurationBuilder();

        // Act
        builder.AddAzureKeyVault(options =>
        {
            options.VaultUri = "https://test-vault.vault.azure.net/";
            options.JsonProcessor = true;
            options.JsonProcessorSecrets = [];
        });

        // Assert
        var sourcesField = typeof(FlexConfigurationBuilder)
            .GetField("_sources", BindingFlags.NonPublic | BindingFlags.Instance);
        var sources = (List<Microsoft.Extensions.Configuration.IConfigurationSource>)sourcesField!.GetValue(builder)!;

        var source = sources[0].Should().BeOfType<AzureKeyVaultConfigurationSource>().Subject;
        source.JsonProcessor.Should().BeTrue();
        source.JsonProcessorSecrets.Should().BeEmpty();
    }

    [Fact]
    public void AddAzureAppConfiguration_WithNullKeyFilter_ConfiguresCorrectly()
    {
        // Arrange
        var builder = new FlexConfigurationBuilder();

        // Act
        builder.AddAzureAppConfiguration(options =>
        {
            options.ConnectionString = "https://test-config.azconfig.io";
            options.KeyFilter = null;
            options.Label = null;
        });

        // Assert
        var sourcesField = typeof(FlexConfigurationBuilder)
            .GetField("_sources", BindingFlags.NonPublic | BindingFlags.Instance);
        var sources = (List<Microsoft.Extensions.Configuration.IConfigurationSource>)sourcesField!.GetValue(builder)!;

        var source = sources[0].Should().BeOfType<AzureAppConfigurationSource>().Subject;
        source.KeyFilter.Should().BeNull();
        source.Label.Should().BeNull();
    }

    #endregion

    #region JSON Processing Tests (if InternalsVisibleTo is configured)

    // Add to AzureExtensionsTests.cs - these tests require InternalsVisibleTo

    #region Internal Method Tests (requires InternalsVisibleTo)

    [Theory]
    [InlineData("{\"key\": \"value\"}", true)]
    [InlineData("{\"nested\": {\"key\": \"value\"}}", true)]
    [InlineData("[]", true)]
    [InlineData("[{\"key\": \"value\"}]", true)]
    [InlineData("", false)]
    [InlineData("not json", false)]
    [InlineData("{", false)]
    [InlineData("}", false)]
    [InlineData("{\"unclosed\": \"value\"", false)]
    [InlineData("123", false)]
    [InlineData("\"simple string\"", false)]
    [InlineData("true", false)]
    [InlineData("null", false)]
    public void IsValidJson_WithVariousInputs_ReturnsExpectedResult(string input, bool expected)
    {
        // Act
        var result = input.IsValidJson();

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void FlattenJsonValue_WithSimpleObject_FlattensCorrectly()
    {
        // Arrange
        var json = "{\"host\": \"localhost\", \"port\": 5432}";
        var prefix = "database";
        var result = new ConcurrentDictionary<string, string?>();

        // Act
        json.FlattenJsonValue(result, prefix);

        // Assert
        result.Should().HaveCount(2);
        result["database:host"].Should().Be("localhost");
        result["database:port"].Should().Be("5432");
    }

    [Fact]
    public void FlattenJsonValue_WithNestedObject_FlattensCorrectly()
    {
        // Arrange
        var json = "{\"database\": {\"host\": \"localhost\", \"port\": 5432}}";
        var prefix = "config";
        var result = new ConcurrentDictionary<string, string?>();

        // Act
        json.FlattenJsonValue(result, prefix);

        // Assert
        result.Should().HaveCount(2);
        result["config:database:host"].Should().Be("localhost");
        result["config:database:port"].Should().Be("5432");
    }

    [Fact]
    public void FlattenJsonValue_WithArray_FlattensWithIndexes()
    {
        // Arrange
        var json = "{\"items\": [\"first\", \"second\", \"third\"]}";
        var prefix = "config";
        var result = new ConcurrentDictionary<string, string?>();

        // Act
        json.FlattenJsonValue(result, prefix);

        // Assert
        result.Should().HaveCount(3);
        result["config:items:0"].Should().Be("first");
        result["config:items:1"].Should().Be("second");
        result["config:items:2"].Should().Be("third");
    }

    [Fact]
    public void FlattenJsonValue_WithComplexNestedStructure_FlattensCorrectly()
    {
        // Arrange
        var json = """
                   {
                       "database": {
                           "connections": [
                               {"host": "host1", "port": 5432},
                               {"host": "host2", "port": 5433}
                           ],
                           "options": {
                               "ssl": true,
                               "timeout": 30
                           }
                       }
                   }
                   """;
        var prefix = "config";
        var result = new ConcurrentDictionary<string, string?>();

        // Act
        json.FlattenJsonValue(result, prefix);

        // Assert
        result["config:database:connections:0:host"].Should().Be("host1");
        result["config:database:connections:0:port"].Should().Be("5432");
        result["config:database:connections:1:host"].Should().Be("host2");
        result["config:database:connections:1:port"].Should().Be("5433");
        result["config:database:options:ssl"].Should().Be("true");
        result["config:database:options:timeout"].Should().Be("30");
    }

    [Fact]
    public void FlattenJsonValue_WithEmptyObject_AddsNothing()
    {
        // Arrange
        var json = "{}";
        var prefix = "config";
        var result = new ConcurrentDictionary<string, string?>();

        // Act
        json.FlattenJsonValue(result, prefix);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void FlattenJsonValue_WithEmptyArray_AddsNothing()
    {
        // Arrange
        var json = "{\"items\": []}";
        var prefix = "config";
        var result = new ConcurrentDictionary<string, string?>();

        // Act
        json.FlattenJsonValue(result, prefix);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void FlattenJsonValue_WithNullValues_HandlesCorrectly()
    {
        // Arrange
        var json = "{\"key1\": null, \"key2\": \"value\"}";
        var prefix = "config";
        var result = new ConcurrentDictionary<string, string?>();

        // Act
        json.FlattenJsonValue(result, prefix);

        // Assert
        result.Should().HaveCount(2);
        result["config:key1"].Should().BeNull();
        result["config:key2"].Should().Be("value");
    }

    [Fact]
    public void FlattenJsonValue_WithInvalidJson_StoresAsSimpleValue()
    {
        // Arrange
        var json = "{ invalid json";
        var prefix = "config";
        var result = new ConcurrentDictionary<string, string?>();

        // Act
        json.FlattenJsonValue(result, prefix);

        // Assert
        result.Should().HaveCount(1);
        result["config"].Should().Be("{ invalid json");
    }

    [Theory]
    [InlineData("true", "true")]
    [InlineData("false", "false")]
    [InlineData("123", "123")]
    [InlineData("123.45", "123.45")]
    [InlineData("\"text\"", "text")]
    public void FlattenJsonValue_WithPrimitiveTypes_ConvertsCorrectly(string jsonValue, string expectedValue)
    {
        // Arrange
        var json = $"{{\"key\": {jsonValue}}}";
        var prefix = "config";
        var result = new ConcurrentDictionary<string, string?>();

        // Act
        json.FlattenJsonValue(result, prefix);

        // Assert
        result["config:key"].Should().Be(expectedValue);
    }
    
    [Fact]
    public void CanParseAsJson_WithInvalidJson_ReturnsFalse()
    {
        // Act
        var result = "{ invalid json".IsValidJson();

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("{")]
    [InlineData("}")]
    [InlineData("{\"unclosed\": \"value\"")]
    [InlineData("not json at all")]
    [InlineData("{\"key\": }")]
    public void CanParseAsJson_WithVariousInvalidJson_ReturnsFalse(string invalidJson)
    {
        // Act
        var result = invalidJson.IsValidJson();

        // Assert
        result.Should().BeFalse();
    }
    
    [Fact]
    public void FlattenJsonValue_WithUndefinedValueKind_SkipsValue()
    {
        // Arrange
        var prefix = "config";
        var result = new ConcurrentDictionary<string, string?>();
    
        // Create a JsonElement with Undefined ValueKind using reflection
        var jsonElement = default(JsonElement); // This creates a JsonElement with ValueKind.Undefined
    
        // Act
        var processMethod = typeof(AzureExtensions).GetMethod("FlattenJsonElement", 
            BindingFlags.NonPublic | BindingFlags.Static);
    
        processMethod?.Invoke(null, [jsonElement, result, prefix]);
    
        // Assert
        result.Should().BeEmpty(); // Undefined values should be skipped
    }

    #endregion

    #endregion

    #region Source Builder Chain Tests

    [Fact]
    public void SourceBuilderChain_MaintainsBuilderReference()
    {
        // Arrange
        var builder = new FlexConfigurationBuilder();

        // Act
        var result = builder
            .AddAzureKeyVault("https://vault.vault.azure.net/")
            .AddAzureAppConfiguration("https://config.azconfig.io");

        // Assert
        result.Should().BeSameAs(builder);
    }

    #endregion
}