using System.Diagnostics.CodeAnalysis;
using Azure.Core;
using FlexKit.Configuration.Providers.Azure.Options;
using FlexKit.Configuration.Providers.Azure.Sources;
using FluentAssertions;
using NSubstitute;
using Xunit;
// ReSharper disable ComplexConditionExpression
// ReSharper disable ClassTooBig

namespace FlexKit.Configuration.Providers.Azure.Tests.Options;

/// <summary>
/// Unit tests for Azure configuration options classes.
/// Tests cover property initialization, validation, and default values.
/// </summary>
public class AzureOptionsTests
{
    #region AzureKeyVaultOptions Tests

    [Fact]
    public void AzureKeyVaultOptions_Constructor_CreatesInstanceWithDefaultValues()
    {
        // Act
        var options = new AzureKeyVaultOptions();

        // Assert
        options.VaultUri.Should().BeNull();
        options.Optional.Should().BeTrue();
        options.JsonProcessor.Should().BeFalse();
        options.JsonProcessorSecrets.Should().BeNull();
        options.ReloadAfter.Should().BeNull();
        options.Credential.Should().BeNull();
        options.SecretProcessor.Should().BeNull();
        options.OnLoadException.Should().BeNull();
    }

    [Fact]
    public void AzureKeyVaultOptions_VaultUri_CanBeSetAndRetrieved()
    {
        // Arrange
        var options = new AzureKeyVaultOptions();
        const string vaultUri = "https://test-vault.vault.azure.net/";

        // Act
        options.VaultUri = vaultUri;

        // Assert
        options.VaultUri.Should().Be(vaultUri);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AzureKeyVaultOptions_Optional_CanBeSetAndRetrieved(bool optional)
    {
        // Arrange
        var options = new AzureKeyVaultOptions
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
    public void AzureKeyVaultOptions_JsonProcessor_CanBeSetAndRetrieved(bool jsonProcessor)
    {
        // Arrange
        var options = new AzureKeyVaultOptions
        {
            // Act
            JsonProcessor = jsonProcessor,
        };

        // Assert
        options.JsonProcessor.Should().Be(jsonProcessor);
    }

    [Fact]
    public void AzureKeyVaultOptions_JsonProcessorSecrets_CanBeSetAndRetrieved()
    {
        // Arrange
        var options = new AzureKeyVaultOptions();
        var secrets = new[] { "database-config", "cache-config" };

        // Act
        options.JsonProcessorSecrets = secrets;

        // Assert
        options.JsonProcessorSecrets.Should().BeEquivalentTo(secrets);
    }

    [Fact]
    public void AzureKeyVaultOptions_ReloadAfter_CanBeSetAndRetrieved()
    {
        // Arrange
        var options = new AzureKeyVaultOptions();
        var reloadAfter = TimeSpan.FromMinutes(15);

        // Act
        options.ReloadAfter = reloadAfter;

        // Assert
        options.ReloadAfter.Should().Be(reloadAfter);
    }

    [Fact]
    public void AzureKeyVaultOptions_Credential_CanBeSetAndRetrieved()
    {
        // Arrange
        var options = new AzureKeyVaultOptions();
        var credential = Substitute.For<TokenCredential>();

        // Act
        options.Credential = credential;

        // Assert
        options.Credential.Should().BeSameAs(credential);
    }

    [Fact]
    public void AzureKeyVaultOptions_SecretProcessor_CanBeSetAndRetrieved()
    {
        // Arrange
        var options = new AzureKeyVaultOptions();
        var processor = Substitute.For<IKeyVaultSecretProcessor>();

        // Act
        options.SecretProcessor = processor;

        // Assert
        options.SecretProcessor.Should().BeSameAs(processor);
    }

    [Fact]
    public void AzureKeyVaultOptions_OnLoadException_CanBeSetAndRetrieved()
    {
        // Arrange
        var options = new AzureKeyVaultOptions();
        var exceptionHandler = new Action<KeyVaultConfigurationProviderException>(_ => { });

        // Act
        options.OnLoadException = exceptionHandler;

        // Assert
        options.OnLoadException.Should().BeSameAs(exceptionHandler);
    }

    [Fact]
    public void AzureKeyVaultOptions_AllProperties_CanBeSetTogether()
    {
        // Arrange
        var credential = Substitute.For<TokenCredential>();
        var processor = Substitute.For<IKeyVaultSecretProcessor>();

        // Act
        var options = new AzureKeyVaultOptions
        {
            VaultUri = "https://test-vault.vault.azure.net/",
            Optional = false,
            JsonProcessor = true,
            JsonProcessorSecrets = ["config1", "config2"],
            ReloadAfter = TimeSpan.FromMinutes(10),
            Credential = credential,
            SecretProcessor = processor,
            OnLoadException = ex => _ = ex
        };

        // Assert
        options.VaultUri.Should().Be("https://test-vault.vault.azure.net/");
        options.Optional.Should().BeFalse();
        options.JsonProcessor.Should().BeTrue();
        options.JsonProcessorSecrets.Should().BeEquivalentTo(new[] { "config1", "config2" });
        options.ReloadAfter.Should().Be(TimeSpan.FromMinutes(10));
        options.Credential.Should().BeSameAs(credential);
        options.SecretProcessor.Should().BeSameAs(processor);
        options.OnLoadException.Should().NotBeNull();
    }

    [Fact]
    public void AzureKeyVaultOptions_JsonProcessorSecrets_WithEmptyArray_SetsEmptyArray()
    {
        // Arrange
        var options = new AzureKeyVaultOptions
        {
            // Act
            JsonProcessorSecrets = []
        };

        // Assert
        options.JsonProcessorSecrets.Should().BeEmpty();
    }

    [Fact]
    public void AzureKeyVaultOptions_ReloadAfter_WithZeroTimeSpan_SetsZeroTimeSpan()
    {
        // Arrange
        var options = new AzureKeyVaultOptions
        {
            // Act
            ReloadAfter = TimeSpan.Zero
        };

        // Assert
        options.ReloadAfter.Should().Be(TimeSpan.Zero);
    }

    #endregion

    #region AzureAppConfigurationOptions Tests

    [Fact]
    public void AzureAppConfigurationOptions_Constructor_CreatesInstanceWithDefaultValues()
    {
        // Act
        var options = new AzureAppConfigurationOptions();

        // Assert
        options.ConnectionString.Should().BeNull();
        options.Optional.Should().BeTrue();
        options.KeyFilter.Should().BeNull();
        options.Label.Should().BeNull();
        options.ReloadAfter.Should().BeNull();
        options.Credential.Should().BeNull();
        options.OnLoadException.Should().BeNull();
    }

    [Fact]
    public void AzureAppConfigurationOptions_ConnectionString_CanBeSetAndRetrieved()
    {
        // Arrange
        var options = new AzureAppConfigurationOptions();
        const string connectionString = "https://test-config.azconfig.io";

        // Act
        options.ConnectionString = connectionString;

        // Assert
        options.ConnectionString.Should().Be(connectionString);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AzureAppConfigurationOptions_Optional_CanBeSetAndRetrieved(bool optional)
    {
        // Arrange
        var options = new AzureAppConfigurationOptions
        {
            // Act
            Optional = optional,
        };

        // Assert
        options.Optional.Should().Be(optional);
    }

    [Fact]
    public void AzureAppConfigurationOptions_KeyFilter_CanBeSetAndRetrieved()
    {
        // Arrange
        var options = new AzureAppConfigurationOptions();
        const string keyFilter = "myapp:*";

        // Act
        options.KeyFilter = keyFilter;

        // Assert
        options.KeyFilter.Should().Be(keyFilter);
    }

    [Fact]
    public void AzureAppConfigurationOptions_Label_CanBeSetAndRetrieved()
    {
        // Arrange
        var options = new AzureAppConfigurationOptions();
        const string label = "production";

        // Act
        options.Label = label;

        // Assert
        options.Label.Should().Be(label);
    }

    [Fact]
    public void AzureAppConfigurationOptions_ReloadAfter_CanBeSetAndRetrieved()
    {
        // Arrange
        var options = new AzureAppConfigurationOptions();
        var reloadAfter = TimeSpan.FromMinutes(5);

        // Act
        options.ReloadAfter = reloadAfter;

        // Assert
        options.ReloadAfter.Should().Be(reloadAfter);
    }

    [Fact]
    public void AzureAppConfigurationOptions_Credential_CanBeSetAndRetrieved()
    {
        // Arrange
        var options = new AzureAppConfigurationOptions();
        var credential = Substitute.For<TokenCredential>();

        // Act
        options.Credential = credential;

        // Assert
        options.Credential.Should().BeSameAs(credential);
    }

    [Fact]
    public void AzureAppConfigurationOptions_OnLoadException_CanBeSetAndRetrieved()
    {
        // Arrange
        var options = new AzureAppConfigurationOptions();
        var exceptionHandler = new Action<AppConfigurationProviderException>(_ => { });

        // Act
        options.OnLoadException = exceptionHandler;

        // Assert
        options.OnLoadException.Should().BeSameAs(exceptionHandler);
    }

    [Fact]
    public void AzureAppConfigurationOptions_AllProperties_CanBeSetTogether()
    {
        // Arrange
        var credential = Substitute.For<TokenCredential>();

        // Act
        var options = new AzureAppConfigurationOptions
        {
            ConnectionString = "https://test-config.azconfig.io",
            Optional = false,
            KeyFilter = "myapp:*",
            Label = "production",
            ReloadAfter = TimeSpan.FromMinutes(5),
            Credential = credential,
            OnLoadException = ex => _ = ex
        };

        // Assert
        options.ConnectionString.Should().Be("https://test-config.azconfig.io");
        options.Optional.Should().BeFalse();
        options.KeyFilter.Should().Be("myapp:*");
        options.Label.Should().Be("production");
        options.ReloadAfter.Should().Be(TimeSpan.FromMinutes(5));
        options.Credential.Should().BeSameAs(credential);
        options.OnLoadException.Should().NotBeNull();
    }

    [Fact]
    public void AzureAppConfigurationOptions_KeyFilter_WithEmptyString_SetsEmptyString()
    {
        // Arrange
        var options = new AzureAppConfigurationOptions
        {
            // Act
            KeyFilter = string.Empty
        };

        // Assert
        options.KeyFilter.Should().BeEmpty();
    }

    [Fact]
    public void AzureAppConfigurationOptions_Label_WithEmptyString_SetsEmptyString()
    {
        // Arrange
        var options = new AzureAppConfigurationOptions
        {
            // Act
            Label = string.Empty
        };

        // Assert
        options.Label.Should().BeEmpty();
    }

    [Fact]
    public void AzureAppConfigurationOptions_ReloadAfter_WithZeroTimeSpan_SetsZeroTimeSpan()
    {
        // Arrange
        var options = new AzureAppConfigurationOptions
        {
            // Act
            ReloadAfter = TimeSpan.Zero
        };

        // Assert
        options.ReloadAfter.Should().Be(TimeSpan.Zero);
    }

    #endregion

    #region Connection String Format Tests

    [Theory]
    [InlineData("https://test-config.azconfig.io")]
    [InlineData("Endpoint=https://test-config.azconfig.io;Id=test;Secret=secret")]
    [InlineData("")]
    [InlineData(null)]
    public void AzureAppConfigurationOptions_ConnectionString_AcceptsValidFormats(string? connectionString)
    {
        // Arrange
        var options = new AzureAppConfigurationOptions
        {
            // Act
            ConnectionString = connectionString
        };

        // Assert
        options.ConnectionString.Should().Be(connectionString);
    }

    [Theory]
    [InlineData("https://vault1.vault.azure.net/")]
    [InlineData("https://my-app-vault.vault.azure.net/")]
    [InlineData("")]
    [InlineData(null)]
    public void AzureKeyVaultOptions_VaultUri_AcceptsValidFormats(string? vaultUri)
    {
        // Arrange
        var options = new AzureKeyVaultOptions
        {
            // Act
            VaultUri = vaultUri
        };

        // Assert
        options.VaultUri.Should().Be(vaultUri);
    }

    #endregion

    #region Edge Cases and Validation Tests

    [Fact]
    public void AzureKeyVaultOptions_JsonProcessorSecrets_WithNullValues_HandlesCorrectly()
    {
        // Arrange
        var options = new AzureKeyVaultOptions();
        string?[] secretsWithNull = ["valid-secret", null, "another-secret"];

        // Act
        options.JsonProcessorSecrets = secretsWithNull!;

        // Assert
        options.JsonProcessorSecrets.Should().BeEquivalentTo(secretsWithNull);
    }

    [Fact]
    public void AzureAppConfigurationOptions_KeyFilter_WithWildcardPatterns_SetsCorrectly()
    {
        // Arrange
        var options = new AzureAppConfigurationOptions
        {
            // Act & Assert
            KeyFilter = "*"
        };

        options.KeyFilter.Should().Be("*");

        options.KeyFilter = "myapp:*";
        options.KeyFilter.Should().Be("myapp:*");

        options.KeyFilter = "myapp:database:*,myapp:api:*";
        options.KeyFilter.Should().Be("myapp:database:*,myapp:api:*");
    }

    [Fact]
    public void AzureAppConfigurationOptions_Label_WithSpecialCharacters_SetsCorrectly()
    {
        // Arrange
        var options = new AzureAppConfigurationOptions
        {
            // Act & Assert
            Label = "production-v1.0"
        };

        options.Label.Should().Be("production-v1.0");

        options.Label = "staging_2024";
        options.Label.Should().Be("staging_2024");

        options.Label = "development.local";
        options.Label.Should().Be("development.local");
    }

    [Fact]
    public void AzureKeyVaultOptions_ReloadAfter_WithNegativeTimeSpan_SetsValue()
    {
        // Arrange
        var options = new AzureKeyVaultOptions();
        var negativeTimeSpan = TimeSpan.FromMinutes(-5);

        // Act
        options.ReloadAfter = negativeTimeSpan;

        // Assert
        options.ReloadAfter.Should().Be(negativeTimeSpan);
    }

    [Fact]
    public void AzureAppConfigurationOptions_ReloadAfter_WithLargeTimeSpan_SetsValue()
    {
        // Arrange
        var options = new AzureAppConfigurationOptions();
        var largeTimeSpan = TimeSpan.FromDays(365); // 1 year

        // Act
        options.ReloadAfter = largeTimeSpan;

        // Assert
        options.ReloadAfter.Should().Be(largeTimeSpan);
    }

    #endregion

    #region Property Independence Tests

    [Fact]
    public void AzureKeyVaultOptions_PropertyChanges_DoNotAffectOtherInstances()
    {
        // Arrange
        var options1 = new AzureKeyVaultOptions
        {
            VaultUri = "https://vault1.vault.azure.net/",
            Optional = true
        };

        var options2 = new AzureKeyVaultOptions
        {
            VaultUri = "https://vault2.vault.azure.net/",
            Optional = false
        };

        // Act
        options1.JsonProcessor = true;
        options2.JsonProcessor = false;

        // Assert
        options1.VaultUri.Should().Be("https://vault1.vault.azure.net/");
        options1.Optional.Should().BeTrue();
        options1.JsonProcessor.Should().BeTrue();

        options2.VaultUri.Should().Be("https://vault2.vault.azure.net/");
        options2.Optional.Should().BeFalse();
        options2.JsonProcessor.Should().BeFalse();
    }

    [Fact]
    public void AzureAppConfigurationOptions_PropertyChanges_DoNotAffectOtherInstances()
    {
        // Arrange
        var options1 = new AzureAppConfigurationOptions
        {
            ConnectionString = "https://config1.azconfig.io",
            KeyFilter = "app1:*"
        };

        var options2 = new AzureAppConfigurationOptions
        {
            ConnectionString = "https://config2.azconfig.io",
            KeyFilter = "app2:*"
        };

        // Act
        options1.Label = "production";
        options2.Label = "staging";

        // Assert
        options1.ConnectionString.Should().Be("https://config1.azconfig.io");
        options1.KeyFilter.Should().Be("app1:*");
        options1.Label.Should().Be("production");

        options2.ConnectionString.Should().Be("https://config2.azconfig.io");
        options2.KeyFilter.Should().Be("app2:*");
        options2.Label.Should().Be("staging");
    }

    #endregion

    #region Complex Configuration Scenarios

    [Fact]
    public void AzureKeyVaultOptions_ProductionScenario_ConfiguresCorrectly()
    {
        // Arrange & Act
        var options = new AzureKeyVaultOptions
        {
            VaultUri = "https://prod-vault.vault.azure.net/",
            Optional = false, // Required in production
            JsonProcessor = true,
            JsonProcessorSecrets = ["database-config", "api-keys", "feature-flags"],
            ReloadAfter = TimeSpan.FromMinutes(15), // Reasonable production interval
            Credential = Substitute.For<TokenCredential>(),
            SecretProcessor = Substitute.For<IKeyVaultSecretProcessor>(),
            OnLoadException = _ => { /* Log error */ }
        };

        // Assert
        options.VaultUri.Should().Be("https://prod-vault.vault.azure.net/");
        options.Optional.Should().BeFalse();
        options.JsonProcessor.Should().BeTrue();
        options.JsonProcessorSecrets.Should().HaveCount(3);
        options.ReloadAfter.Should().Be(TimeSpan.FromMinutes(15));
        options.Credential.Should().NotBeNull();
        options.SecretProcessor.Should().NotBeNull();
        options.OnLoadException.Should().NotBeNull();
    }

    [Fact]
    public void AzureAppConfigurationOptions_DevelopmentScenario_ConfiguresCorrectly()
    {
        // Arrange & Act
        var options = new AzureAppConfigurationOptions
        {
            ConnectionString = "https://dev-config.azconfig.io",
            Optional = true, // Optional in development
            KeyFilter = "myapp:*", // Only load app-specific keys
            Label = "development", // Development-specific configuration
            ReloadAfter = TimeSpan.FromMinutes(1), // Fast reload for development
            Credential = Substitute.For<TokenCredential>(),
            OnLoadException = _ => { /* Log warning */ }
        };

        // Assert
        options.ConnectionString.Should().Be("https://dev-config.azconfig.io");
        options.Optional.Should().BeTrue();
        options.KeyFilter.Should().Be("myapp:*");
        options.Label.Should().Be("development");
        options.ReloadAfter.Should().Be(TimeSpan.FromMinutes(1));
        options.Credential.Should().NotBeNull();
        options.OnLoadException.Should().NotBeNull();
    }

    [Fact]
    public void AzureKeyVaultOptions_MinimalConfiguration_UsesDefaults()
    {
        // Arrange & Act
        var options = new AzureKeyVaultOptions
        {
            VaultUri = "https://minimal-vault.vault.azure.net/"
            // All other properties use defaults
        };

        // Assert
        options.VaultUri.Should().Be("https://minimal-vault.vault.azure.net/");
        options.Optional.Should().BeTrue(); // Default
        options.JsonProcessor.Should().BeFalse(); // Default
        options.JsonProcessorSecrets.Should().BeNull(); // Default
        options.ReloadAfter.Should().BeNull(); // Default
        options.Credential.Should().BeNull(); // Default
        options.SecretProcessor.Should().BeNull(); // Default
        options.OnLoadException.Should().BeNull(); // Default
    }

    [Fact]
    public void AzureAppConfigurationOptions_MinimalConfiguration_UsesDefaults()
    {
        // Arrange & Act
        var options = new AzureAppConfigurationOptions
        {
            ConnectionString = "https://minimal-config.azconfig.io"
            // All other properties use defaults
        };

        // Assert
        options.ConnectionString.Should().Be("https://minimal-config.azconfig.io");
        options.Optional.Should().BeTrue(); // Default
        options.KeyFilter.Should().BeNull(); // Default
        options.Label.Should().BeNull(); // Default
        options.ReloadAfter.Should().BeNull(); // Default
        options.Credential.Should().BeNull(); // Default
        options.OnLoadException.Should().BeNull(); // Default
    }

    #endregion

    #region Exception Handler Tests

    [Fact]
    public void AzureKeyVaultOptions_OnLoadException_CanCaptureException()
    {
        // Arrange
        var options = new AzureKeyVaultOptions();
        Exception? capturedException = null;

        options.OnLoadException = ex => capturedException = ex;

        // Act
        var testException = new KeyVaultConfigurationProviderException(
            new AzureKeyVaultConfigurationSource { VaultUri = "https://test.vault.azure.net/" },
            new InvalidOperationException("Test error"));

        options.OnLoadException?.Invoke(testException);

        // Assert
        capturedException.Should().NotBeNull();
        capturedException.Should().BeSameAs(testException);
    }

    [Fact]
    public void AzureAppConfigurationOptions_OnLoadException_CanCaptureException()
    {
        // Arrange
        var options = new AzureAppConfigurationOptions();
        Exception? capturedException = null;

        options.OnLoadException = ex => capturedException = ex;

        // Act
        var testException = new AppConfigurationProviderException(
            new AzureAppConfigurationSource { ConnectionString = "https://test.azconfig.io" },
            new InvalidOperationException("Test error"));

        options.OnLoadException?.Invoke(testException);

        // Assert
        capturedException.Should().NotBeNull();
        capturedException.Should().BeSameAs(testException);
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    [SuppressMessage("Usage", "xUnit1031:Do not use blocking task operations in test method")]
    public void AzureKeyVaultOptions_ConcurrentPropertyAccess_IsThreadSafe()
    {
        // Arrange
        var options = new AzureKeyVaultOptions();
        var exceptions = new List<Exception>();

        // Act
        var tasks = Enumerable.Range(0, 100).Select(i => Task.Run(() =>
        {
            try
            {
                options.VaultUri = $"https://vault{i}.vault.azure.net/";
                options.Optional = i % 2 == 0;
                options.JsonProcessor = i % 3 == 0;
            }
            catch (Exception ex)
            {
                lock (exceptions)
                {
                    exceptions.Add(ex);
                }
            }
        })).ToArray();

        Task.WaitAll(tasks);

        // Assert
        exceptions.Should().BeEmpty(); // No threading exceptions should occur
        options.VaultUri.Should().NotBeNull(); // Should have some final value
    }

    [Fact]
    [SuppressMessage("Usage", "xUnit1031:Do not use blocking task operations in test method")]
    public void AzureAppConfigurationOptions_ConcurrentPropertyAccess_IsThreadSafe()
    {
        // Arrange
        var options = new AzureAppConfigurationOptions();
        var exceptions = new List<Exception>();

        // Act
        var tasks = Enumerable.Range(0, 100).Select(i => Task.Run(() =>
        {
            try
            {
                options.ConnectionString = $"https://config{i}.azconfig.io";
                options.KeyFilter = $"app{i}:*";
                options.Label = $"env{i}";
            }
            catch (Exception ex)
            {
                lock (exceptions)
                {
                    exceptions.Add(ex);
                }
            }
        })).ToArray();

        Task.WaitAll(tasks);

        // Assert
        exceptions.Should().BeEmpty(); // No threading exceptions should occur
        options.ConnectionString.Should().NotBeNull(); // Should have some final value
    }

    #endregion
}