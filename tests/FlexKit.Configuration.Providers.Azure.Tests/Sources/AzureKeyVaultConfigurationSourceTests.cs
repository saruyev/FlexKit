using Azure.Core;
using FlexKit.Configuration.Providers.Azure.Sources;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Xunit;

namespace FlexKit.Configuration.Providers.Azure.Tests.Sources;

/// <summary>
/// Unit tests for <see cref="AzureKeyVaultConfigurationSource"/>.
/// Tests cover property initialization, validation, and provider creation functionality.
/// </summary>
public class AzureKeyVaultConfigurationSourceTests
{
    #region Property Tests

    [Fact]
    public void Constructor_CreatesInstanceWithDefaultValues()
    {
        // Act
        var source = new AzureKeyVaultConfigurationSource();

        // Assert
        source.VaultUri.Should().BeEmpty();
        source.Optional.Should().BeTrue();
        source.ReloadAfter.Should().BeNull();
        source.Credential.Should().BeNull();
        source.JsonProcessor.Should().BeFalse();
        source.JsonProcessorSecrets.Should().BeNull();
        source.SecretProcessor.Should().BeNull();
        source.OnLoadException.Should().BeNull();
    }

    [Fact]
    public void VaultUri_CanBeSetAndRetrieved()
    {
        // Arrange
        var source = new AzureKeyVaultConfigurationSource();
        const string vaultUri = "https://test-vault.vault.azure.net/";

        // Act
        source.VaultUri = vaultUri;

        // Assert
        source.VaultUri.Should().Be(vaultUri);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Optional_CanBeSetAndRetrieved(bool optional)
    {
        // Arrange
        var source = new AzureKeyVaultConfigurationSource
        {
            // Act
            Optional = optional,
        };

        // Assert
        source.Optional.Should().Be(optional);
    }

    [Fact]
    public void ReloadAfter_CanBeSetAndRetrieved()
    {
        // Arrange
        var source = new AzureKeyVaultConfigurationSource();
        var reloadAfter = TimeSpan.FromMinutes(20);

        // Act
        source.ReloadAfter = reloadAfter;

        // Assert
        source.ReloadAfter.Should().Be(reloadAfter);
    }

    [Fact]
    public void Credential_CanBeSetAndRetrieved()
    {
        // Arrange
        var source = new AzureKeyVaultConfigurationSource();
        var credential = Substitute.For<TokenCredential>();

        // Act
        source.Credential = credential;

        // Assert
        source.Credential.Should().BeSameAs(credential);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void JsonProcessor_CanBeSetAndRetrieved(bool jsonProcessor)
    {
        // Arrange
        var source = new AzureKeyVaultConfigurationSource
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
        var source = new AzureKeyVaultConfigurationSource();
        var secrets = new[] { "database-config", "cache-config" };

        // Act
        source.JsonProcessorSecrets = secrets;

        // Assert
        source.JsonProcessorSecrets.Should().BeEquivalentTo(secrets);
    }

    [Fact]
    public void SecretProcessor_CanBeSetAndRetrieved()
    {
        // Arrange
        var source = new AzureKeyVaultConfigurationSource();
        var processor = Substitute.For<IKeyVaultSecretProcessor>();

        // Act
        source.SecretProcessor = processor;

        // Assert
        source.SecretProcessor.Should().BeSameAs(processor);
    }

    [Fact]
    public void OnLoadException_CanBeSetAndRetrieved()
    {
        // Arrange
        var source = new AzureKeyVaultConfigurationSource();
        var exceptionHandler = new Action<KeyVaultConfigurationProviderException>(_ => { });

        // Act
        source.OnLoadException = exceptionHandler;

        // Assert
        source.OnLoadException.Should().BeSameAs(exceptionHandler);
    }

    #endregion

    #region IConfigurationSource Tests

    [Fact]
    public void Build_WithNullBuilder_ThrowsArgumentNullException()
    {
        // Arrange
        var source = new AzureKeyVaultConfigurationSource();

        // Act & Assert
        var action = () => source.Build(null!);
        action.Should().Throw<ArgumentNullException>().WithParameterName("builder");
    }

    [Fact]
    public void Build_WithValidBuilder_ReturnsProvider()
    {
        // Arrange
        var source = new AzureKeyVaultConfigurationSource
        {
            VaultUri = "https://test-vault.vault.azure.net/"
        };
        var builder = Substitute.For<IConfigurationBuilder>();

        // Act
        var provider = source.Build(builder);

        // Assert
        provider.Should().NotBeNull();
        provider.Should().BeOfType<AzureKeyVaultConfigurationProvider>();
    }

    [Fact]
    public void Build_CalledMultipleTimes_ReturnsNewInstances()
    {
        // Arrange
        var source = new AzureKeyVaultConfigurationSource
        {
            VaultUri = "https://test-vault.vault.azure.net/"
        };
        var builder = Substitute.For<IConfigurationBuilder>();

        // Act
        var provider1 = source.Build(builder);
        var provider2 = source.Build(builder);

        // Assert
        provider1.Should().NotBeSameAs(provider2);
        provider1.Should().BeOfType<AzureKeyVaultConfigurationProvider>();
        provider2.Should().BeOfType<AzureKeyVaultConfigurationProvider>();
    }

    [Fact]
    public void IConfigurationSource_ImplementsInterface()
    {
        // Arrange & Act
        var source = new AzureKeyVaultConfigurationSource();

        // Assert
        source.Should().BeAssignableTo<IConfigurationSource>();
    }

    [Fact]
    public void IConfigurationSource_Build_CallsPublicBuildMethod()
    {
        // Arrange
        IConfigurationSource source = new AzureKeyVaultConfigurationSource
        {
            VaultUri = "https://test-vault.vault.azure.net/"
        };
        var builder = Substitute.For<IConfigurationBuilder>();

        // Act
        var provider = source.Build(builder);

        // Assert
        provider.Should().NotBeNull();
        provider.Should().BeOfType<AzureKeyVaultConfigurationProvider>();
    }

    #endregion

    #region Equality and State Tests

    [Fact]
    public void TwoInstances_WithSameProperties_AreNotEqual()
    {
        // Arrange
        var source1 = new AzureKeyVaultConfigurationSource
        {
            VaultUri = "https://test-vault.vault.azure.net/",
            Optional = true,
            JsonProcessor = true
        };

        var source2 = new AzureKeyVaultConfigurationSource
        {
            VaultUri = "https://test-vault.vault.azure.net/",
            Optional = true,
            JsonProcessor = true
        };

        // Act & Assert
        source1.Should().NotBeSameAs(source2);
        source1.Should().NotBe(source2); // Reference equality, not value equality
    }

    [Fact]
    public void PropertyModification_AfterCreation_UpdatesProperty()
    {
        // Arrange
        var source = new AzureKeyVaultConfigurationSource
        {
            VaultUri = "https://original-vault.vault.azure.net/"
        };

        // Act
        source.VaultUri = "https://modified-vault.vault.azure.net/";

        // Assert
        source.VaultUri.Should().Be("https://modified-vault.vault.azure.net/");
    }

    #endregion

    #region Complex Scenarios Tests

    [Fact]
    public void Build_WithComplexConfiguration_CreatesProviderSuccessfully()
    {
        // Arrange
        var processor = Substitute.For<IKeyVaultSecretProcessor>();
        var credential = Substitute.For<TokenCredential>();

        var source = new AzureKeyVaultConfigurationSource
        {
            VaultUri = "https://complex-vault.vault.azure.net/",
            Optional = false,
            JsonProcessor = true,
            JsonProcessorSecrets = ["database-config", "cache-config"],
            ReloadAfter = TimeSpan.FromMinutes(10),
            Credential = credential,
            SecretProcessor = processor,
            OnLoadException = ex => _ = ex
        };

        var builder = Substitute.For<IConfigurationBuilder>();

        // Act
        var provider = source.Build(builder);

        // Assert
        provider.Should().NotBeNull();
        provider.Should().BeOfType<AzureKeyVaultConfigurationProvider>();
    }

    [Fact]
    public void PropertyChaining_SupportsFluentConfiguration()
    {
        // Arrange & Act
        var source = new AzureKeyVaultConfigurationSource
        {
            VaultUri = "https://test-vault.vault.azure.net/",
            Optional = true,
            JsonProcessor = true,
            JsonProcessorSecrets = ["config1", "config2"],
            ReloadAfter = TimeSpan.FromMinutes(5),
            Credential = Substitute.For<TokenCredential>(),
            SecretProcessor = Substitute.For<IKeyVaultSecretProcessor>(),
            OnLoadException = _ => { }
        };

        // Assert
        source.VaultUri.Should().Be("https://test-vault.vault.azure.net/");
        source.Optional.Should().BeTrue();
        source.JsonProcessor.Should().BeTrue();
        source.JsonProcessorSecrets.Should().BeEquivalentTo(new[] { "config1", "config2" });
        source.ReloadAfter.Should().Be(TimeSpan.FromMinutes(5));
        source.Credential.Should().NotBeNull();
        source.SecretProcessor.Should().NotBeNull();
        source.OnLoadException.Should().NotBeNull();
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public void JsonProcessorSecrets_WithEmptyArray_SetsEmptyArray()
    {
        // Arrange
        var source = new AzureKeyVaultConfigurationSource
        {
            // Act
            JsonProcessorSecrets = []
        };

        // Assert
        source.JsonProcessorSecrets.Should().BeEmpty();
    }

    [Fact]
    public void ReloadAfter_WithZeroTimeSpan_SetsZeroTimeSpan()
    {
        // Arrange
        var source = new AzureKeyVaultConfigurationSource
        {
            // Act
            ReloadAfter = TimeSpan.Zero
        };

        // Assert
        source.ReloadAfter.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void Build_WithEmptyVaultUri_ThrowsInvalidOperationException()
    {
        // Arrange
        var source = new AzureKeyVaultConfigurationSource
        {
            VaultUri = string.Empty 
        };
        var builder = Substitute.For<IConfigurationBuilder>();

        // Act & Assert
        // Empty VaultUri should cause provider creation to fail with InvalidOperationException
        var action = () => source.Build(builder);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Failed to create Azure Key Vault client. Ensure Azure credentials are properly configured.");
    }

    #endregion

    #region Provider Creation Edge Cases

    [Fact]
    public void Build_WithNullCredential_CreatesProviderWithNullCredential()
    {
        // Arrange
        var source = new AzureKeyVaultConfigurationSource
        {
            VaultUri = "https://test-vault.vault.azure.net/",
            Credential = null
        };
        var builder = Substitute.For<IConfigurationBuilder>();

        // Act
        var provider = source.Build(builder);

        // Assert
        provider.Should().NotBeNull();
        provider.Should().BeOfType<AzureKeyVaultConfigurationProvider>();
    }

    [Fact]
    public void Build_WithNullJsonProcessorSecrets_CreatesProvider()
    {
        // Arrange
        var source = new AzureKeyVaultConfigurationSource
        {
            VaultUri = "https://test-vault.vault.azure.net/",
            JsonProcessor = true,
            JsonProcessorSecrets = null
        };
        var builder = Substitute.For<IConfigurationBuilder>();

        // Act
        var provider = source.Build(builder);

        // Assert
        provider.Should().NotBeNull();
        provider.Should().BeOfType<AzureKeyVaultConfigurationProvider>();
    }

    #endregion

    #region State Consistency Tests

    [Fact]
    public void MultipleBuilds_WithSameSource_CreateIndependentProviders()
    {
        // Arrange
        var source = new AzureKeyVaultConfigurationSource
        {
            VaultUri = "https://test-vault.vault.azure.net/",
            JsonProcessor = true
        };
        var builder = Substitute.For<IConfigurationBuilder>();

        // Act
        var provider1 = source.Build(builder);
        source.JsonProcessor = false; // Modify source after first build
        var provider2 = source.Build(builder);

        // Assert
        provider1.Should().NotBeSameAs(provider2);
        provider1.Should().BeOfType<AzureKeyVaultConfigurationProvider>();
        provider2.Should().BeOfType<AzureKeyVaultConfigurationProvider>();
    }

    [Fact]
    public void Source_AfterBuild_CanBeModified()
    {
        // Arrange
        var source = new AzureKeyVaultConfigurationSource
        {
            VaultUri = "https://original-vault.vault.azure.net/"
        };
        var builder = Substitute.For<IConfigurationBuilder>();

        // Act
        var provider = source.Build(builder);
        source.VaultUri = "https://modified-vault.vault.azure.net/";

        // Assert
        provider.Should().NotBeNull();
        source.VaultUri.Should().Be("https://modified-vault.vault.azure.net/");
    }

    #endregion
}