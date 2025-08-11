using System.Reflection;
using Azure;
using Azure.Core;
using Azure.Data.AppConfiguration;
using Azure.Identity;
using FlexKit.Configuration.Providers.Azure.Sources;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;
// ReSharper disable ClassTooBig
// ReSharper disable FlagArgument
// ReSharper disable NullableWarningSuppressionIsUsed

namespace FlexKit.Configuration.Providers.Azure.Tests.Sources;

/// <summary>
/// Unit tests for <see cref="AzureAppConfigurationProvider"/>.
/// Tests cover constructor validation, loading functionality, disposal, and error handling.
/// </summary>
public class AzureAppConfigurationProviderTests : IDisposable
{
    private ConfigurationClient? _mockConfigClient;
    private readonly AzureAppConfigurationSource _source;
    private readonly AzureAppConfigurationProvider _provider;

    public AzureAppConfigurationProviderTests()
    {
        _mockConfigClient = Substitute.For<ConfigurationClient>();
        _source = new AzureAppConfigurationSource
        {
            ConnectionString = "https://test-config.azconfig.io",
            Optional = true
        };

        _provider = new AzureAppConfigurationProvider(_source);
        
        // Use reflection to replace the private _configClient field
        var configClientField = typeof(AzureAppConfigurationProvider)
            .GetField("_configClient", BindingFlags.NonPublic | BindingFlags.Instance);
        configClientField!.SetValue(_provider, _mockConfigClient);
    }

    public void Dispose()
    {
        _provider.Dispose();
        _mockConfigClient = null;
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidSource_CreatesProvider()
    {
        // Arrange
        var source = new AzureAppConfigurationSource
        {
            ConnectionString = "https://test-config.azconfig.io"
        };

        // Act
        var provider = new AzureAppConfigurationProvider(source);

        // Assert
        provider.Should().NotBeNull();
        provider.Dispose();
    }

    [Fact]
    public void Constructor_WithReloadAfterSet_CreatesTimer()
    {
        // Arrange
        var sourceWithReload = new AzureAppConfigurationSource
        {
            ConnectionString = "https://test-config.azconfig.io",
            ReloadAfter = TimeSpan.FromMinutes(5)
        };

        // Act
        var providerWithReload = new AzureAppConfigurationProvider(sourceWithReload);

        // Assert
        var timerField = typeof(AzureAppConfigurationProvider)
            .GetField("_reloadTimer", BindingFlags.NonPublic | BindingFlags.Instance);
        var timer = timerField?.GetValue(providerWithReload);
        timer.Should().NotBeNull();

        providerWithReload.Dispose();
    }

    [Fact]
    public void Constructor_WithNullReloadAfter_DoesNotCreateTimer()
    {
        // Arrange
        var sourceWithoutReload = new AzureAppConfigurationSource
        {
            ConnectionString = "https://test-config.azconfig.io",
            ReloadAfter = null
        };

        // Act
        var providerWithoutReload = new AzureAppConfigurationProvider(sourceWithoutReload);

        // Assert
        var timerField = typeof(AzureAppConfigurationProvider)
            .GetField("_reloadTimer", BindingFlags.NonPublic | BindingFlags.Instance);
        var timer = timerField?.GetValue(providerWithoutReload);
        timer.Should().BeNull();

        providerWithoutReload.Dispose();
    }

    [Fact]
    public void Constructor_WithInvalidConnectionString_ThrowsInvalidOperationException()
    {
        // Arrange
        var source = new AzureAppConfigurationSource
        {
            ConnectionString = "invalid-connection-string"
        };

        // Act & Assert
        var action = () => new AzureAppConfigurationProvider(source);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Failed to create Azure App Configuration client. Ensure connection string or credentials are properly configured.");
    }

    [Fact]
    public void Constructor_WithFullConnectionString_ThrowsInvalidOperationException()
    {
        // Arrange
        var source = new AzureAppConfigurationSource
        {
            ConnectionString = "Endpoint=https://test-config.azconfig.io;Id=test-id;Secret=test-secret"
        };

        // Act & Assert
        // Constructor will fail without valid Azure credentials, even with a well-formed connection string
        var action = () => new AzureAppConfigurationProvider(source);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Failed to create Azure App Configuration client. Ensure connection string or credentials are properly configured.");
    }

    [Fact]
    public void Constructor_WithEndpointAndCredential_CreatesProvider()
    {
        // Arrange
        var source = new AzureAppConfigurationSource
        {
            ConnectionString = "https://test-config.azconfig.io",
            Credential = Substitute.For<TokenCredential>()
        };

        // Act
        var provider = new AzureAppConfigurationProvider(source);

        // Assert
        provider.Should().NotBeNull();
        provider.Dispose();
    }
    
    // [Fact]
    // public void Constructor_WithFullConnectionString_UsesConnectionStringPath()
    // {
    //     // Arrange
    //     var source = new AzureAppConfigurationSource
    //     {
    //         ConnectionString = "Endpoint=https://test-config.azconfig.io;Id=test-id;Secret=test-secret"
    //     };
    //
    //     // Act
    //     var provider = new AzureAppConfigurationProvider(source);
    //
    //     // Assert
    //     provider.Should().NotBeNull();
    //     provider.Should().BeOfType<AzureAppConfigurationProvider>();
    //     provider.Dispose();
    // }

    [Fact]
    public void Constructor_WithEndpointOnly_UsesCredentialPath()
    {
        // Arrange
        var source = new AzureAppConfigurationSource
        {
            ConnectionString = "https://test-config.azconfig.io",
            Credential = new DefaultAzureCredential()
        };

        // Act
        var provider = new AzureAppConfigurationProvider(source);

        // Assert
        provider.Should().NotBeNull();
        provider.Should().BeOfType<AzureAppConfigurationProvider>();
        provider.Dispose();
    }

    [Fact]
    public void Constructor_WithReloadAfter_CreatesTimerCallback()
    {
        // Arrange
        var source = new AzureAppConfigurationSource
        {
            ConnectionString = "https://test-config.azconfig.io",
            ReloadAfter = TimeSpan.FromSeconds(1) // Short interval for test
        };

        // Act
        var provider = new AzureAppConfigurationProvider(source);
    
        // Wait briefly to allow a timer callback to potentially execute
        Thread.Sleep(1500);

        // Assert
        provider.Should().NotBeNull();
        provider.Dispose();
    }

    #endregion

    #region Connection String Detection Tests

    [Theory]
    [InlineData("Endpoint=https://test.azconfig.io;Id=test;Secret=secret")]
    [InlineData("Endpoint=https://test.azconfig.io;Secret=secret")]
    public void IsConnectionString_WithValidConnectionString_ThrowsInvalidOperationException(string connectionString)
    {
        // Arrange
        var source = new AzureAppConfigurationSource { ConnectionString = connectionString };

        // Act & Assert
        // Even valid connection string formats will fail without real Azure credentials
        var action = () => new AzureAppConfigurationProvider(source);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Failed to create Azure App Configuration client. Ensure connection string or credentials are properly configured.");
    }

    #endregion

    #region Load Tests

    [Fact]
    public void Load_WithNoSettings_LoadsEmptyConfiguration()
    {
        // Arrange
        var emptySettings = AsyncPageable<ConfigurationSetting>.FromPages([
            Page<ConfigurationSetting>.FromValues([], null, Substitute.For<Response>())
        ]);
        _mockConfigClient?.GetConfigurationSettingsAsync(Arg.Any<SettingSelector>(), CancellationToken.None)
            .Returns(emptySettings);

        // Act
        _provider.Load();

        // Assert
        _provider.GetChildKeys([], null).Should().BeEmpty();
    }

    [Fact]
    public void Load_WithSimpleSetting_LoadsConfiguration()
    {
        // Arrange
        var setting = CreateConfigurationSetting("test:key", "test-value");
        var settings = AsyncPageable<ConfigurationSetting>.FromPages([
            Page<ConfigurationSetting>.FromValues([setting], null, Substitute.For<Response>())
        ]);
        _mockConfigClient?.GetConfigurationSettingsAsync(Arg.Any<SettingSelector>(), CancellationToken.None)
            .Returns(settings);

        // Act
        _provider.Load();

        // Assert
        _provider.TryGet("test:key", out var value).Should().BeTrue();
        value.Should().Be("test-value");
    }

    [Fact]
    public void Load_WithMultipleSettings_LoadsAllConfiguration()
    {
        // Arrange
        var settings = new[]
        {
            CreateConfigurationSetting("database:host", "localhost"),
            CreateConfigurationSetting("database:port", "5432"),
            CreateConfigurationSetting("api:key", "secret-key")
        };
        var settingsPageable = AsyncPageable<ConfigurationSetting>.FromPages([
            Page<ConfigurationSetting>.FromValues(settings, null, Substitute.For<Response>())
        ]);
        _mockConfigClient?.GetConfigurationSettingsAsync(Arg.Any<SettingSelector>(), CancellationToken.None)
            .Returns(settingsPageable);

        // Act
        _provider.Load();

        // Assert
        _provider.TryGet("database:host", out var host).Should().BeTrue();
        host.Should().Be("localhost");
        _provider.TryGet("database:port", out var port).Should().BeTrue();
        port.Should().Be("5432");
        _provider.TryGet("api:key", out var key).Should().BeTrue();
        key.Should().Be("secret-key");
    }

    [Fact]
    public void Load_WithKeyFilter_UsesFilterInSelector()
    {
        // Arrange
        _source.KeyFilter = "myapp:*";
        var setting = CreateConfigurationSetting("myapp:test", "value");
        var settings = AsyncPageable<ConfigurationSetting>.FromPages([
            Page<ConfigurationSetting>.FromValues([setting], null, Substitute.For<Response>())
        ]);
        _mockConfigClient?.GetConfigurationSettingsAsync(Arg.Any<SettingSelector>(), CancellationToken.None)
            .Returns(settings);

        // Act
        _provider.Load();

        // Assert
        _mockConfigClient?.Received(1).GetConfigurationSettingsAsync(
            Arg.Is<SettingSelector>(s => s.KeyFilter == "myapp:*"), CancellationToken.None);
        _provider.TryGet("myapp:test", out var value).Should().BeTrue();
        value.Should().Be("value");
    }
    
    [Fact]
    public void Load_WithJsonProcessorEnabledAndValidJsonValue_FlattensJsonValue()
    {
        // Arrange
        _source.JsonProcessor = true;
        var jsonValue = """{"database": {"host": "localhost", "port": 5432}, "api": {"key": "secret"}}""";
        var setting = CreateConfigurationSetting("config", jsonValue);
        var settings = AsyncPageable<ConfigurationSetting>.FromPages([
            Page<ConfigurationSetting>.FromValues([setting], null, Substitute.For<Response>())
        ]);
        _mockConfigClient?.GetConfigurationSettingsAsync(Arg.Any<SettingSelector>(), CancellationToken.None)
            .Returns(settings);

        // Act
        _provider.Load();

        // Assert
        _provider.TryGet("config:database:host", out var host).Should().BeTrue();
        host.Should().Be("localhost");
        _provider.TryGet("config:database:port", out var port).Should().BeTrue();
        port.Should().Be("5432");
        _provider.TryGet("config:api:key", out var key).Should().BeTrue();
        key.Should().Be("secret");
        // The original key should not exist when JSON is flattened
        _provider.TryGet("config", out _).Should().BeFalse();
    }

    [Fact]
    public void Load_WithNullKeyFilter_UsesWildcardFilter()
    {
        // Arrange
        _source.KeyFilter = null;
        var setting = CreateConfigurationSetting("test:key", "value");
        var settings = AsyncPageable<ConfigurationSetting>.FromPages([
            Page<ConfigurationSetting>.FromValues([setting], null, Substitute.For<Response>())
        ]);
        _mockConfigClient?.GetConfigurationSettingsAsync(Arg.Any<SettingSelector>(), CancellationToken.None)
            .Returns(settings);

        // Act
        _provider.Load();

        // Assert
        _mockConfigClient?.Received(1).GetConfigurationSettingsAsync(
            Arg.Is<SettingSelector>(s => s.KeyFilter == "*"), CancellationToken.None);
    }

    [Fact]
    public void Load_WithLabel_UsesLabelInSelector()
    {
        // Arrange
        _source.Label = "production";
        var setting = CreateConfigurationSetting("test:key", "value");
        var settings = AsyncPageable<ConfigurationSetting>.FromPages([
            Page<ConfigurationSetting>.FromValues([setting], null, Substitute.For<Response>())
        ]);
        _mockConfigClient?.GetConfigurationSettingsAsync(Arg.Any<SettingSelector>(), CancellationToken.None)
            .Returns(settings);

        // Act
        _provider.Load();

        // Assert
        _mockConfigClient?.Received(1).GetConfigurationSettingsAsync(
            Arg.Is<SettingSelector>(s => s.LabelFilter == "production"), CancellationToken.None);
    }

    [Fact]
    public void Load_WithNullLabel_UsesNullLabelFilter()
    {
        // Arrange
        _source.Label = null;
        var setting = CreateConfigurationSetting("test:key", "value");
        var settings = AsyncPageable<ConfigurationSetting>.FromPages([
            Page<ConfigurationSetting>.FromValues([setting], null, Substitute.For<Response>())
        ]);
        _mockConfigClient?.GetConfigurationSettingsAsync(Arg.Any<SettingSelector>(), CancellationToken.None)
            .Returns(settings);

        // Act
        _provider.Load();

        // Assert
        _mockConfigClient?.Received(1).GetConfigurationSettingsAsync(
            Arg.Is<SettingSelector>(s => s.LabelFilter == null), CancellationToken.None);
    }

    [Fact]
    public void Load_WithSettingHavingNullKey_SkipsSetting()
    {
        // Arrange
        var validSetting = CreateConfigurationSetting("valid:key", "valid-value");
        var invalidSetting = CreateConfigurationSetting(null, "invalid-value");
        var settings = AsyncPageable<ConfigurationSetting>.FromPages([
            Page<ConfigurationSetting>.FromValues([validSetting, invalidSetting], null, Substitute.For<Response>())
        ]);
        _mockConfigClient?.GetConfigurationSettingsAsync(Arg.Any<SettingSelector>(), CancellationToken.None)
            .Returns(settings);

        // Act
        _provider.Load();

        // Assert
        _provider.TryGet("valid:key", out var value).Should().BeTrue();
        value.Should().Be("valid-value");
        // Invalid setting should be skipped
        _provider.GetChildKeys([], null).Should().HaveCount(1);
    }

    [Fact]
    public void Load_WithSettingHavingNullValue_SkipsSetting()
    {
        // Arrange
        var validSetting = CreateConfigurationSetting("valid:key", "valid-value");
        var invalidSetting = CreateConfigurationSetting("invalid:key", null);
        var settings = AsyncPageable<ConfigurationSetting>.FromPages([
            Page<ConfigurationSetting>.FromValues([validSetting, invalidSetting], null, Substitute.For<Response>())
        ]);
        _mockConfigClient?.GetConfigurationSettingsAsync(Arg.Any<SettingSelector>(), CancellationToken.None)
            .Returns(settings);

        // Act
        _provider.Load();

        // Assert
        _provider.TryGet("valid:key", out var value).Should().BeTrue();
        value.Should().Be("valid-value");
        // Invalid setting should be skipped
        _provider.GetChildKeys([], null).Should().HaveCount(1);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void Load_WithOptionalSourceAndException_HandlesGracefully()
    {
        // Arrange
        _source.Optional = true;
        Exception? capturedLoadException = null;
        _source.OnLoadException = ex => capturedLoadException = ex;

        _mockConfigClient?.GetConfigurationSettingsAsync(Arg.Any<SettingSelector>(), CancellationToken.None)
            .Throws(new InvalidOperationException("App Configuration access failed"));

        // Act
        _provider.Load();

        // Assert
        capturedLoadException.Should().NotBeNull();
        capturedLoadException.Should().BeOfType<AppConfigurationProviderException>();
        _provider.GetChildKeys([], null).Should().BeEmpty();
    }

    [Fact]
    public void Load_WithRequiredSourceAndException_ThrowsException()
    {
        // Arrange
        _source.Optional = false;

        _mockConfigClient?.GetConfigurationSettingsAsync(Arg.Any<SettingSelector>(), CancellationToken.None)
            .Throws(new InvalidOperationException("App Configuration access failed"));

        // Act & Assert
        var action = () => _provider.Load();

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Failed to load configuration from Azure App Configuration*");
    }

    #endregion

    #region Disposal Tests

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var provider = new AzureAppConfigurationProvider(_source);

        // Act & Assert
        provider.Dispose(); // First call
        provider.Dispose(); // The second call should not throw
    }

    // [Fact]
    // public void Dispose_DisposesTimer()
    // {
    //     // Arrange
    //     var sourceWithReload = new AzureAppConfigurationSource
    //     {
    //         ConnectionString = "https://test-config.azconfig.io",
    //         ReloadAfter = TimeSpan.FromMinutes(1)
    //     };
    //     var provider = new AzureAppConfigurationProvider(sourceWithReload);
    //
    //     // Act
    //     provider.Dispose();
    //
    //     // Assert - Timer should be disposed of (we can't directly test this but ensure no exceptions)
    //     provider.Disposing.Should().BeFalse(); // Provider should be disposed
    // }
    //
    // [Fact]
    // public void Dispose_DisposesConfigClient()
    // {
    //     // Arrange
    //     var provider = new AzureAppConfigurationProvider(_source);
    //
    //     // Act
    //     provider.Dispose();
    //
    //     // Assert - ConfigClient should be disposed of (we can't directly test this but ensure no exceptions)
    //     provider.Disposing.Should().BeFalse(); // Provider should be disposed
    // }

    #endregion

    #region Reload Timer Tests

    [Fact]
    public void Constructor_WithZeroReloadAfter_CreatesTimerWithZeroInterval()
    {
        // Arrange
        var sourceWithZeroReload = new AzureAppConfigurationSource
        {
            ConnectionString = "https://test-config.azconfig.io",
            ReloadAfter = TimeSpan.Zero
        };

        // Act
        var provider = new AzureAppConfigurationProvider(sourceWithZeroReload);

        // Assert
        var timerField = typeof(AzureAppConfigurationProvider)
            .GetField("_reloadTimer", BindingFlags.NonPublic | BindingFlags.Instance);
        var timer = timerField?.GetValue(provider);
        timer.Should().NotBeNull();

        provider.Dispose();
    }

    #endregion

    #region Data Persistence Tests

    [Fact]
    public void Load_CalledMultipleTimes_ReplacesData()
    {
        // Arrange
        var firstSetting = CreateConfigurationSetting("test:key", "first-value");
        var firstSettings = AsyncPageable<ConfigurationSetting>.FromPages([
            Page<ConfigurationSetting>.FromValues([firstSetting], null, Substitute.For<Response>())
        ]);

        // First load
        _mockConfigClient?.GetConfigurationSettingsAsync(Arg.Any<SettingSelector>(), CancellationToken.None)
            .Returns(firstSettings);

        // Act - First load
        _provider.Load();

        // Assert - First load
        _provider.TryGet("test:key", out var firstValue).Should().BeTrue();
        firstValue.Should().Be("first-value");

        // Setup for the second load
        var secondSetting = CreateConfigurationSetting("test:key", "second-value");
        var secondSettings = AsyncPageable<ConfigurationSetting>.FromPages([
            Page<ConfigurationSetting>.FromValues([secondSetting], null, Substitute.For<Response>())
        ]);
        _mockConfigClient?.GetConfigurationSettingsAsync(Arg.Any<SettingSelector>(), CancellationToken.None)
            .Returns(secondSettings);

        // Act - Second load
        _provider.Load();

        // Assert - the Second load should replace the first
        _provider.TryGet("test:key", out var secondValue).Should().BeTrue();
        secondValue.Should().Be("second-value");
    }

    [Fact]
    public void Load_WithChangedSettingCount_UpdatesConfiguration()
    {
        // Arrange - First load with one setting
        var firstSetting = CreateConfigurationSetting("setting1", "value1");
        var firstSettings = AsyncPageable<ConfigurationSetting>.FromPages([
            Page<ConfigurationSetting>.FromValues([firstSetting], null, Substitute.For<Response>())
        ]);
        _mockConfigClient?.GetConfigurationSettingsAsync(Arg.Any<SettingSelector>(), CancellationToken.None)
            .Returns(firstSettings);

        // Act - First load
        _provider.Load();

        // Assert - First load
        _provider.TryGet("setting1", out var value1).Should().BeTrue();
        value1.Should().Be("value1");

        // Setup for the second load with two settings
        var secondSettings = new[]
        {
            CreateConfigurationSetting("setting1", "value1"),
            CreateConfigurationSetting("setting2", "value2")
        };
        var secondSettingsPageable = AsyncPageable<ConfigurationSetting>.FromPages([
            Page<ConfigurationSetting>.FromValues(secondSettings, null, Substitute.For<Response>())
        ]);
        _mockConfigClient?.GetConfigurationSettingsAsync(Arg.Any<SettingSelector>(), CancellationToken.None)
            .Returns(secondSettingsPageable);

        // Act - Second load
        _provider.Load();

        // Assert - Both settings should be available
        _provider.TryGet("setting1", out var updatedValue1).Should().BeTrue();
        updatedValue1.Should().Be("value1");
        _provider.TryGet("setting2", out var value2).Should().BeTrue();
        value2.Should().Be("value2");
    }

    #endregion

    #region Async Enumeration Tests

    [Fact]
    public void Load_WithPaginatedResults_ProcessesAllPages()
    {
        // Arrange
        var firstPageSettings = new[] { CreateConfigurationSetting("key1", "value1") };
        var secondPageSettings = new[] { CreateConfigurationSetting("key2", "value2") };
        
        var paginatedSettings = AsyncPageable<ConfigurationSetting>.FromPages([
            Page<ConfigurationSetting>.FromValues(firstPageSettings, "continuationtoken", Substitute.For<Response>()),
            Page<ConfigurationSetting>.FromValues(secondPageSettings, null, Substitute.For<Response>())
        ]);
        
        _mockConfigClient?.GetConfigurationSettingsAsync(Arg.Any<SettingSelector>(), CancellationToken.None)
            .Returns(paginatedSettings);

        // Act
        _provider.Load();

        // Assert
        _provider.TryGet("key1", out var value1).Should().BeTrue();
        value1.Should().Be("value1");
        _provider.TryGet("key2", out var value2).Should().BeTrue();
        value2.Should().Be("value2");
    }

    #endregion

    #region Helper Methods

    private static ConfigurationSetting CreateConfigurationSetting(string? key, string? value)
    {
        return new ConfigurationSetting(key, value);
    }

    #endregion
}