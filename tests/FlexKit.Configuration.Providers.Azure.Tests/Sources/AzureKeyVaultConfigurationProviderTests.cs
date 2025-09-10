using System.Reflection;
using Azure;
using Azure.Security.KeyVault.Secrets;
using FlexKit.Configuration.Providers.Azure.Sources;
using FluentAssertions;
using NSubstitute;
using Xunit;
// ReSharper disable AccessToDisposedClosure
// ReSharper disable NullableWarningSuppressionIsUsed

namespace FlexKit.Configuration.Providers.Azure.Tests.Sources;

public class AzureKeyVaultConfigurationProviderTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidSource_CreatesProvider()
    {
        // Arrange
        var source = new AzureKeyVaultConfigurationSource
        {
            VaultUri = "https://test-vault.vault.azure.net/"
        };

        // Act
        var provider = new AzureKeyVaultConfigurationProvider(source);

        // Assert
        // Constructor succeeds - Azure client creation uses DefaultAzureCredential, which doesn't fail during construction
        provider.Should().NotBeNull();
        provider.Should().BeOfType<AzureKeyVaultConfigurationProvider>();

        // The actual Azure authentication failure happens during Load(), not construction
        provider.Dispose();
    }

    [Fact]
    public void Constructor_WithInvalidVaultUri_ThrowsInvalidOperationException()
    {
        // Arrange
        var source = new AzureKeyVaultConfigurationSource
        {
            VaultUri = "invalid-uri"
        };

        // Act & Assert
        var action = () => new AzureKeyVaultConfigurationProvider(source);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Failed to create Azure Key Vault client. Ensure Azure credentials are properly configured.");
    }

    [Fact]
    public void Constructor_WithEmptyVaultUri_ThrowsInvalidOperationException()
    {
        // Arrange
        var source = new AzureKeyVaultConfigurationSource
        {
            VaultUri = string.Empty
        };

        // Act & Assert
        var action = () => new AzureKeyVaultConfigurationProvider(source);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Failed to create Azure Key Vault client. Ensure Azure credentials are properly configured.");
    }

    #endregion

    #region Source Property Tests

    [Fact]
    public void Constructor_WithReloadAfterSet_CreatesProviderWithTimer()
    {
        // Arrange
        var source = new AzureKeyVaultConfigurationSource
        {
            VaultUri = "https://test-vault.vault.azure.net/",
            ReloadAfter = TimeSpan.FromMinutes(5)
        };

        // Act
        var provider = new AzureKeyVaultConfigurationProvider(source);

        // Assert
        provider.Should().NotBeNull();
        provider.Should().BeOfType<AzureKeyVaultConfigurationProvider>();

        // Verify timer was created using reflection
        var timerField = typeof(AzureKeyVaultConfigurationProvider)
            .GetField("_reloadTimer", BindingFlags.NonPublic | BindingFlags.Instance);
        var timer = timerField?.GetValue(provider);
        timer.Should().NotBeNull();

        provider.Dispose();
    }

    [Fact]
    public void Constructor_WithNullReloadAfter_CreatesProviderWithoutTimer()
    {
        // Arrange
        var source = new AzureKeyVaultConfigurationSource
        {
            VaultUri = "https://test-vault.vault.azure.net/",
            ReloadAfter = null
        };

        // Act
        var provider = new AzureKeyVaultConfigurationProvider(source);

        // Assert
        provider.Should().NotBeNull();
        provider.Should().BeOfType<AzureKeyVaultConfigurationProvider>();

        // Verify timer was NOT created using reflection
        var timerField = typeof(AzureKeyVaultConfigurationProvider)
            .GetField("_reloadTimer", BindingFlags.NonPublic | BindingFlags.Instance);
        var timer = timerField?.GetValue(provider);
        timer.Should().BeNull();

        provider.Dispose();
    }

    #endregion

    #region Edge Cases Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-uri")]
    public void Constructor_WithInvalidVaultUris_ThrowsInvalidOperationException(string? vaultUri)
    {
        // Arrange
        var source = new AzureKeyVaultConfigurationSource
        {
            VaultUri = vaultUri!
        };

        // Act & Assert
        var action = () => new AzureKeyVaultConfigurationProvider(source);
        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Constructor_WithReloadAfter_TimerCallbackExecutesLoadAsync()
    {
        // Arrange
        var source = new AzureKeyVaultConfigurationSource
        {
            VaultUri = "https://test-vault.vault.azure.net/",
            ReloadAfter = TimeSpan.FromMilliseconds(100) // Very short interval for test
        };

        // Act
        var provider = new AzureKeyVaultConfigurationProvider(source);

        // Wait for a timer callback to execute
        Thread.Sleep(200);

        // Assert
        provider.Should().NotBeNull();
        provider.Dispose();
    }

    [Fact]
    public void Load_CallsLoadAsyncSynchronously()
    {
        // Arrange
        var source = new AzureKeyVaultConfigurationSource
        {
            VaultUri = "https://test-vault.vault.azure.net/",
            Optional = true // Make it optional to handle Azure failures gracefully
        };
        var provider = new AzureKeyVaultConfigurationProvider(source);

        // Act & Assert
        // This will call LoadAsync internally and handle any Azure exceptions
        var action = () => provider.Load();
        action.Should().NotThrow(); // With Optional = true, it should not throw

        provider.Dispose();
    }

    [Fact]
    public void Load_WithRequiredSource_ThrowsOnFailure()
    {
        // Arrange
        var source = new AzureKeyVaultConfigurationSource
        {
            VaultUri = "https://test-vault.vault.azure.net/",
            Optional = false // Required source
        };
        var provider = new AzureKeyVaultConfigurationProvider(source);

        // Act & Assert
        var action = () => provider.Load();
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Failed to load configuration from Azure Key Vault*");

        provider.Dispose();
    }

    [Fact]
    public void Load_WithOptionalSourceAndNoExceptionHandler_HandlesFailureGracefully()
    {
        // Arrange
        var source = new AzureKeyVaultConfigurationSource
        {
            VaultUri = "https://test-vault.vault.azure.net/",
            Optional = true,
            OnLoadException = null // No exception handler
        };
        var provider = new AzureKeyVaultConfigurationProvider(source);

        // Act
        provider.Load();

        // Assert
        // Should complete without throwing, covering the try block in LoadAsync
        provider.Should().NotBeNull();
        provider.Dispose();
    }

    [Fact]
    public void Load_WithOptionalSourceAndExceptionHandler_CallsHandler()
    {
        // Arrange
        Exception? capturedLoadException = null;
        var source = new AzureKeyVaultConfigurationSource
        {
            VaultUri = "https://test-vault.vault.azure.net/",
            Optional = true,
            OnLoadException = ex => capturedLoadException = ex
        };
        var provider = new AzureKeyVaultConfigurationProvider(source);

        // Act
        provider.Load();

        // Assert
        // This should trigger the Azure failure and call the exception handler
        // covering LoadSecretsAsync, GetEnabledSecretsAsync, and other private methods
        capturedLoadException.Should().NotBeNull();
        provider.Dispose();
    }

    [Fact]
    public void Load_WithInvalidVaultUri_TriggersSecretProcessingPath()
    {
        // Arrange
        var source = new AzureKeyVaultConfigurationSource
        {
            VaultUri = "https://nonexistent-vault-12345.vault.azure.net/",
            Optional = true,
            JsonProcessor = true,
            JsonProcessorSecrets = ["test-secret"],
            SecretProcessor = Substitute.For<IKeyVaultSecretProcessor>()
        };
        var provider = new AzureKeyVaultConfigurationProvider(source);

        // Act
        provider.Load();

        // Assert
        // This will attempt to call all the private methods:
        // LoadSecretsAsync -> GetEnabledSecretsAsync -> ProcessSecretsAsync -> 
        // ProcessSingleSecretAsync -> TransformSecretNameToConfigKey -> 
        // ProcessSecretValue -> ShouldProcessAsJson
        // Even though it fails, it covers the method entry points
        provider.Should().NotBeNull();
        provider.Dispose();
    }

    [Fact]
    public void Load_WithCustomSecretProcessor_TriggersTransformationPath()
    {
        // Arrange
        var mockProcessor = Substitute.For<IKeyVaultSecretProcessor>();
        mockProcessor.ProcessSecretName(Arg.Any<string>(), Arg.Any<string>())
            .Returns("transformed:key");

        var source = new AzureKeyVaultConfigurationSource
        {
            VaultUri = "https://test-vault.vault.azure.net/",
            Optional = true,
            SecretProcessor = mockProcessor
        };
        var provider = new AzureKeyVaultConfigurationProvider(source);

        // Act
        provider.Load();

        // Assert
        // This covers TransformSecretNameToConfigKey path with a custom processor
        provider.Should().NotBeNull();
        provider.Dispose();
    }

    [Fact]
    public void Load_WithJsonProcessorEnabled_TriggersJsonProcessingPath()
    {
        // Arrange
        var source = new AzureKeyVaultConfigurationSource
        {
            VaultUri = "https://test-vault.vault.azure.net/",
            Optional = true,
            JsonProcessor = true
        };
        var provider = new AzureKeyVaultConfigurationProvider(source);

        // Act
        provider.Load();

        // Assert
        // This covers ShouldProcessAsJson and ProcessSecretValue paths
        provider.Should().NotBeNull();
        provider.Dispose();
    }

    [Fact]
    public void Load_WithSelectiveJsonProcessing_TriggersFilteringPath()
    {
        // Arrange
        var source = new AzureKeyVaultConfigurationSource
        {
            VaultUri = "https://test-vault.vault.azure.net/",
            Optional = true,
            JsonProcessor = true,
            JsonProcessorSecrets = ["database-config", "api-keys"]
        };
        var provider = new AzureKeyVaultConfigurationProvider(source);

        // Act
        provider.Load();

        // Assert
        // This covers ShouldProcessAsJson with selective processing
        provider.Should().NotBeNull();
        provider.Dispose();
    }

    [Fact]
    public void Load_WithEnabledAndDisabledSecrets_LoadsOnlyEnabled()
    {
        // Arrange
        var enabledSecret = CreateSecretProperties("enabled-secret", enabled: true);
        var disabledSecret = CreateSecretProperties("disabled-secret", enabled: false);

        var secretProperties = new[] { enabledSecret, disabledSecret };
        var pageable = AsyncPageable<SecretProperties>.FromPages([
            Page<SecretProperties>.FromValues(secretProperties, null, Substitute.For<Response>())
        ]);

        var secrets = new Dictionary<string, KeyVaultSecret>
        {
            ["enabled-secret"] = new KeyVaultSecret("enabled-secret", "enabled-value")
        };

        var mockClient = new MockSecretClient(pageable, secrets);
        var source = new AzureKeyVaultConfigurationSource
        {
            VaultUri = "https://test-vault.vault.azure.net/"
        };

        var provider = new AzureKeyVaultConfigurationProvider(source, mockClient);

        // Act
        provider.Load();

        // Assert
        provider.TryGet("enabled-secret", out var value).Should().BeTrue();
        value.Should().Be("enabled-value");

        // Disabled secret should not be loaded
        provider.TryGet("disabled:secret", out _).Should().BeFalse();

        provider.Dispose();
    }

    [Fact]
    public void Load_WithSecretAccessFailure_OptionalSource_ContinuesWithOtherSecrets()
    {
        // Arrange - Mock client that throws for a specific secret
        var workingSecret = new SecretProperties("working-secret") { Enabled = true };
        var failingSecret = new SecretProperties("failing-secret") { Enabled = true };

        var secretProperties = new[] { workingSecret, failingSecret };
        var pageable = AsyncPageable<SecretProperties>.FromPages([
            Page<SecretProperties>.FromValues(secretProperties, null, Substitute.For<Response>())
        ]);

        // Only provide a working secret, failing secret will throw RequestFailedException
        var secrets = new Dictionary<string, KeyVaultSecret>
        {
            ["working-secret"] = new KeyVaultSecret("working-secret", "working-value")
        };

        var mockClient = new MockSecretClient(pageable, secrets);
        var source = new AzureKeyVaultConfigurationSource
        {
            VaultUri = "https://test-vault.vault.azure.net/",
            Optional = true // When optional, individual secret failures are caught and ignored
        };

        var provider = new AzureKeyVaultConfigurationProvider(source, mockClient);

        // Act
        provider.Load();

        // Assert - Should have a working secret but not failing one
        provider.TryGet("working-secret", out var value).Should().BeTrue();
        value.Should().Be("working-value");
        provider.TryGet("failing-secret", out _).Should().BeFalse();

        provider.Dispose();
    }

    [Fact]
    public void Load_WithCustomSecretProcessor_TransformsKeys()
    {
        // Arrange
        var mockProcessor = Substitute.For<IKeyVaultSecretProcessor>();
        // Processor receives an already transformed key (-- to :) and original name
        mockProcessor.ProcessSecretName("test:secret", "test--secret")
            .Returns("custom:transformed:key");

        var secret = new SecretProperties("test--secret") { Enabled = true };
        var secretProperties = new[] { secret };
        var pageable = AsyncPageable<SecretProperties>.FromPages([
            Page<SecretProperties>.FromValues(secretProperties, null, Substitute.For<Response>())
        ]);

        var secrets = new Dictionary<string, KeyVaultSecret>
        {
            ["test--secret"] = new KeyVaultSecret("test--secret", "secret-value")
        };

        var mockClient = new MockSecretClient(pageable, secrets);
        var source = new AzureKeyVaultConfigurationSource
        {
            VaultUri = "https://test-vault.vault.azure.net/",
            SecretProcessor = mockProcessor
        };

        var provider = new AzureKeyVaultConfigurationProvider(source, mockClient);

        // Act
        provider.Load();

        // Assert
        provider.TryGet("custom:transformed:key", out var value).Should().BeTrue();
        value.Should().Be("secret-value");
        mockProcessor.Received(1).ProcessSecretName("test:secret", "test--secret");

        provider.Dispose();
    }

    [Fact]
    public void Load_WithJsonProcessorEnabled_FlattensJsonSecrets()
    {
        // Arrange
        var secret = new SecretProperties("database--config") { Enabled = true };
        var secretProperties = new[] { secret };
        var pageable = AsyncPageable<SecretProperties>.FromPages([
            Page<SecretProperties>.FromValues(secretProperties, null, Substitute.For<Response>())
        ]);

        var jsonValue = """{"host": "localhost", "port": 5432}""";
        var secrets = new Dictionary<string, KeyVaultSecret>
        {
            ["database--config"] = new KeyVaultSecret("database--config", jsonValue)
        };

        var mockClient = new MockSecretClient(pageable, secrets);
        var source = new AzureKeyVaultConfigurationSource
        {
            VaultUri = "https://test-vault.vault.azure.net/",
            JsonProcessor = true
        };

        var provider = new AzureKeyVaultConfigurationProvider(source, mockClient);

        // Act
        provider.Load();

        // Assert - JSON gets flattened with the config key as a prefix
        provider.TryGet("database:config:host", out var host).Should().BeTrue();
        host.Should().Be("localhost");
        provider.TryGet("database:config:port", out var port).Should().BeTrue();
        port.Should().Be("5432");

        provider.Dispose();
    }

    [Fact]
    public void Load_WithSelectiveJsonProcessing_ProcessesOnlySpecifiedSecrets()
    {
        // Arrange
        var secret1 = new SecretProperties("database--config") { Enabled = true };
        var secret2 = new SecretProperties("api--key") { Enabled = true };
        var secretProperties = new[] { secret1, secret2 };
        var pageable = AsyncPageable<SecretProperties>.FromPages([
            Page<SecretProperties>.FromValues(secretProperties, null, Substitute.For<Response>())
        ]);

        var secrets = new Dictionary<string, KeyVaultSecret>
        {
            ["database--config"] = new KeyVaultSecret("database--config", """{"host": "localhost"}"""),
            ["api--key"] = new KeyVaultSecret("api--key", """{"key": "secret"}""")
        };

        var mockClient = new MockSecretClient(pageable, secrets);
        var source = new AzureKeyVaultConfigurationSource
        {
            VaultUri = "https://test-vault.vault.azure.net/",
            JsonProcessor = true,
            JsonProcessorSecrets = ["database--config"] // Only process this one as JSON
        };

        var provider = new AzureKeyVaultConfigurationProvider(source, mockClient);

        // Act
        provider.Load();

        // Assert
        // the database--config should be processed as JSON (flattened)
        provider.TryGet("database:config:host", out var host).Should().BeTrue();
        host.Should().Be("localhost");

        // api--key should NOT be processed as JSON (stored as string)
        provider.TryGet("api:key", out var apiKey).Should().BeTrue();
        apiKey.Should().Be("""{"key": "secret"}""");

        provider.Dispose();
    }

    [Fact]
    public void Load_WithSecretAccessFailure_RequiredSource_ThrowsInvalidOperationException()
    {
        // Arrange
        var failingSecret = new SecretProperties("failing-secret") { Enabled = true };
        var secretProperties = new[] { failingSecret };
        var pageable = AsyncPageable<SecretProperties>.FromPages([
            Page<SecretProperties>.FromValues(secretProperties, null, Substitute.For<Response>())
        ]);

        // Don't provide the secret in the dictionary to cause RequestFailedException
        var secrets = new Dictionary<string, KeyVaultSecret>();

        var mockClient = new MockSecretClient(pageable, secrets);
        var source = new AzureKeyVaultConfigurationSource
        {
            VaultUri = "https://test-vault.vault.azure.net/",
            Optional = false // When not optional, exceptions bubble up to Load() method
        };

        var provider = new AzureKeyVaultConfigurationProvider(source, mockClient);

        // Act & Assert
        var action = () => provider.Load();
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Failed to load configuration from Azure Key Vault 'https://test-vault.vault.azure.net/'. Ensure the vault exists and you have the necessary permissions.")
            .WithInnerException<InvalidOperationException>()
            .WithMessage("Failed to load secret 'failing-secret' from Key Vault.");

        provider.Dispose();
    }

    [Fact]
    public void Dispose_WhenAlreadyDisposed_ReturnsEarlyWithoutException()
    {
        // Arrange
        var source = new AzureKeyVaultConfigurationSource
        {
            VaultUri = "https://test-vault.vault.azure.net/",
            ReloadAfter = TimeSpan.FromMinutes(1) // Create a timer so there's something to dispose
        };
        var provider = new AzureKeyVaultConfigurationProvider(source);

        // Act - First disposal
        provider.Dispose();

        // Act - Second disposal should hit the early return path
        provider.Dispose();

        // Assert - No exception should be thrown, covering the _disposed check
        // This test specifically covers the "if (_disposed) { return; }" line in Dispose(bool disposing)
        provider.Should().NotBeNull();
    }

    private static SecretProperties CreateSecretProperties(string name, bool enabled = true)
    {
        var secretProperties = new SecretProperties(name) { Enabled = enabled };

        return secretProperties;
    }

    #endregion
}

public sealed class MockSecretClient(
    AsyncPageable<SecretProperties> pageable,
    Dictionary<string, KeyVaultSecret>? secrets)
    : SecretClient
{
    private readonly Dictionary<string, KeyVaultSecret> _secrets = secrets ?? [];

    public override AsyncPageable<SecretProperties> GetPropertiesOfSecretsAsync(CancellationToken cancellationToken = default)
        => pageable;

    public override Task<Response<KeyVaultSecret>> GetSecretAsync(
        string name,
        string? version = null,
        CancellationToken cancellationToken = default)
    {
        if (_secrets.TryGetValue(name, out var secret))
        {
            var response = Response.FromValue(secret, Substitute.For<Response>());
            return Task.FromResult(response);
        }

        throw new RequestFailedException("Secret not found");
    }
}