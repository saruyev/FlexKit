using Azure.Core;
using FlexKit.Configuration.Providers.Azure.Sources;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Xunit;
// ReSharper disable ClassTooBig

namespace FlexKit.Configuration.Providers.Azure.Tests.Sources;

/// <summary>
/// Unit tests for <see cref="AzureAppConfigurationSource"/>.
/// Tests cover property initialization, validation, and provider creation functionality.
/// </summary>
public class AzureAppConfigurationSourceTests
{
    #region Property Tests

    [Fact]
    public void Constructor_CreatesInstanceWithDefaultValues()
    {
        // Act
        var source = new AzureAppConfigurationSource();

        // Assert
        source.ConnectionString.Should().BeEmpty();
        source.Optional.Should().BeTrue();
        source.KeyFilter.Should().BeNull();
        source.Label.Should().BeNull();
        source.ReloadAfter.Should().BeNull();
        source.Credential.Should().BeNull();
        source.OnLoadException.Should().BeNull();
    }

    [Fact]
    public void ConnectionString_CanBeSetAndRetrieved()
    {
        // Arrange
        var source = new AzureAppConfigurationSource();
        const string connectionString = "https://test-config.azconfig.io";

        // Act
        source.ConnectionString = connectionString;

        // Assert
        source.ConnectionString.Should().Be(connectionString);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Optional_CanBeSetAndRetrieved(bool optional)
    {
        // Arrange
        var source = new AzureAppConfigurationSource
        {
            // Act
            Optional = optional,
        };

        // Assert
        source.Optional.Should().Be(optional);
    }

    [Fact]
    public void KeyFilter_CanBeSetAndRetrieved()
    {
        // Arrange
        var source = new AzureAppConfigurationSource();
        const string keyFilter = "myapp:*";

        // Act
        source.KeyFilter = keyFilter;

        // Assert
        source.KeyFilter.Should().Be(keyFilter);
    }

    [Fact]
    public void Label_CanBeSetAndRetrieved()
    {
        // Arrange
        var source = new AzureAppConfigurationSource();
        const string label = "production";

        // Act
        source.Label = label;

        // Assert
        source.Label.Should().Be(label);
    }

    [Fact]
    public void ReloadAfter_CanBeSetAndRetrieved()
    {
        // Arrange
        var source = new AzureAppConfigurationSource();
        var reloadAfter = TimeSpan.FromMinutes(10);

        // Act
        source.ReloadAfter = reloadAfter;

        // Assert
        source.ReloadAfter.Should().Be(reloadAfter);
    }

    [Fact]
    public void Credential_CanBeSetAndRetrieved()
    {
        // Arrange
        var source = new AzureAppConfigurationSource();
        var credential = Substitute.For<TokenCredential>();

        // Act
        source.Credential = credential;

        // Assert
        source.Credential.Should().BeSameAs(credential);
    }

    [Fact]
    public void OnLoadException_CanBeSetAndRetrieved()
    {
        // Arrange
        var source = new AzureAppConfigurationSource();
        var exceptionHandler = new Action<AppConfigurationProviderException>(_ => { });

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
        var source = new AzureAppConfigurationSource();

        // Act & Assert
        var action = () => source.Build(null!);
        action.Should().Throw<ArgumentNullException>().WithParameterName("builder");
    }

    [Fact]
    public void Build_WithValidBuilder_ReturnsProvider()
    {
        // Arrange
        var source = new AzureAppConfigurationSource
        {
            ConnectionString = "https://test-config.azconfig.io"
        };
        var builder = Substitute.For<IConfigurationBuilder>();

        // Act
        var provider = source.Build(builder);

        // Assert
        provider.Should().NotBeNull();
        provider.Should().BeOfType<AzureAppConfigurationProvider>();
    }

    [Fact]
    public void Build_CalledMultipleTimes_ReturnsNewInstances()
    {
        // Arrange
        var source = new AzureAppConfigurationSource
        {
            ConnectionString = "https://test-config.azconfig.io"
        };
        var builder = Substitute.For<IConfigurationBuilder>();

        // Act
        var provider1 = source.Build(builder);
        var provider2 = source.Build(builder);

        // Assert
        provider1.Should().NotBeSameAs(provider2);
        provider1.Should().BeOfType<AzureAppConfigurationProvider>();
        provider2.Should().BeOfType<AzureAppConfigurationProvider>();
    }

    [Fact]
    public void IConfigurationSource_ImplementsInterface()
    {
        // Arrange & Act
        var source = new AzureAppConfigurationSource();

        // Assert
        source.Should().BeAssignableTo<IConfigurationSource>();
    }

    [Fact]
    public void IConfigurationSource_Build_CallsPublicBuildMethod()
    {
        // Arrange
        IConfigurationSource source = new AzureAppConfigurationSource
        {
            ConnectionString = "https://test-config.azconfig.io"
        };
        var builder = Substitute.For<IConfigurationBuilder>();

        // Act
        var provider = source.Build(builder);

        // Assert
        provider.Should().NotBeNull();
        provider.Should().BeOfType<AzureAppConfigurationProvider>();
    }

    #endregion

    #region Equality and State Tests

    [Fact]
    public void TwoInstances_WithSameProperties_AreNotEqual()
    {
        // Arrange
        var source1 = new AzureAppConfigurationSource
        {
            ConnectionString = "https://test-config.azconfig.io",
            Optional = true,
            KeyFilter = "myapp:*"
        };

        var source2 = new AzureAppConfigurationSource
        {
            ConnectionString = "https://test-config.azconfig.io",
            Optional = true,
            KeyFilter = "myapp:*"
        };

        // Act & Assert
        source1.Should().NotBeSameAs(source2);
        source1.Should().NotBe(source2); // Reference equality, not value equality
    }

    [Fact]
    public void PropertyModification_AfterCreation_UpdatesProperty()
    {
        // Arrange
        var source = new AzureAppConfigurationSource
        {
            ConnectionString = "https://original-config.azconfig.io"
        };

        // Act
        source.ConnectionString = "https://modified-config.azconfig.io";

        // Assert
        source.ConnectionString.Should().Be("https://modified-config.azconfig.io");
    }

    #endregion

    #region Complex Scenarios Tests

    [Fact]
    public void Build_WithComplexConfiguration_CreatesProviderSuccessfully()
    {
        // Arrange
        var credential = Substitute.For<TokenCredential>();

        var source = new AzureAppConfigurationSource
        {
            ConnectionString = "https://complex-config.azconfig.io",
            Optional = false,
            KeyFilter = "myapp:*",
            Label = "production",
            ReloadAfter = TimeSpan.FromMinutes(5),
            Credential = credential,
            OnLoadException = ex => _ = ex
        };

        var builder = Substitute.For<IConfigurationBuilder>();

        // Act
        var provider = source.Build(builder);

        // Assert
        provider.Should().NotBeNull();
        provider.Should().BeOfType<AzureAppConfigurationProvider>();
    }

    [Fact]
    public void PropertyChaining_SupportsFluentConfiguration()
    {
        // Arrange & Act
        var source = new AzureAppConfigurationSource
        {
            ConnectionString = "https://test-config.azconfig.io",
            Optional = true,
            KeyFilter = "myapp:*",
            Label = "staging",
            ReloadAfter = TimeSpan.FromMinutes(3),
            Credential = Substitute.For<TokenCredential>(),
            OnLoadException = _ => { }
        };

        // Assert
        source.ConnectionString.Should().Be("https://test-config.azconfig.io");
        source.Optional.Should().BeTrue();
        source.KeyFilter.Should().Be("myapp:*");
        source.Label.Should().Be("staging");
        source.ReloadAfter.Should().Be(TimeSpan.FromMinutes(3));
        source.Credential.Should().NotBeNull();
        source.OnLoadException.Should().NotBeNull();
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public void KeyFilter_WithEmptyString_SetsEmptyString()
    {
        // Arrange
        var source = new AzureAppConfigurationSource
        {
            // Act
            KeyFilter = string.Empty
        };

        // Assert
        source.KeyFilter.Should().BeEmpty();
    }

    [Fact]
    public void Label_WithEmptyString_SetsEmptyString()
    {
        // Arrange
        var source = new AzureAppConfigurationSource
        {
            // Act
            Label = string.Empty
        };

        // Assert
        source.Label.Should().BeEmpty();
    }

    [Fact]
    public void ReloadAfter_WithZeroTimeSpan_SetsZeroTimeSpan()
    {
        // Arrange
        var source = new AzureAppConfigurationSource
        {
            // Act
            ReloadAfter = TimeSpan.Zero
        };

        // Assert
        source.ReloadAfter.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void Build_WithEmptyConnectionString_ThrowsInvalidOperationException()
    {
        // Arrange
        var source = new AzureAppConfigurationSource
        {
            ConnectionString = string.Empty
        };
        var builder = Substitute.For<IConfigurationBuilder>();

        // Act & Assert
        // Empty connection string should cause provider creation to fail
        var action = () => source.Build(builder);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Failed to create Azure App Configuration client. Ensure connection string or credentials are properly configured.");
    }

    #endregion

    #region Provider Creation Edge Cases

    [Fact]
    public void Build_WithNullCredential_CreatesProviderWithNullCredential()
    {
        // Arrange
        var source = new AzureAppConfigurationSource
        {
            ConnectionString = "https://test-config.azconfig.io",
            Credential = null
        };
        var builder = Substitute.For<IConfigurationBuilder>();

        // Act
        var provider = source.Build(builder);

        // Assert
        provider.Should().NotBeNull();
        provider.Should().BeOfType<AzureAppConfigurationProvider>();
    }

    [Fact]
    public void Build_WithNullKeyFilter_CreatesProvider()
    {
        // Arrange
        var source = new AzureAppConfigurationSource
        {
            ConnectionString = "https://test-config.azconfig.io",
            KeyFilter = null
        };
        var builder = Substitute.For<IConfigurationBuilder>();

        // Act
        var provider = source.Build(builder);

        // Assert
        provider.Should().NotBeNull();
        provider.Should().BeOfType<AzureAppConfigurationProvider>();
    }

    [Fact]
    public void Build_WithNullLabel_CreatesProvider()
    {
        // Arrange
        var source = new AzureAppConfigurationSource
        {
            ConnectionString = "https://test-config.azconfig.io",
            Label = null
        };
        var builder = Substitute.For<IConfigurationBuilder>();

        // Act
        var provider = source.Build(builder);

        // Assert
        provider.Should().NotBeNull();
        provider.Should().BeOfType<AzureAppConfigurationProvider>();
    }

    #endregion

    #region State Consistency Tests

    [Fact]
    public void MultipleBuilds_WithSameSource_CreateIndependentProviders()
    {
        // Arrange
        var source = new AzureAppConfigurationSource
        {
            ConnectionString = "https://test-config.azconfig.io",
            KeyFilter = "original:*"
        };
        var builder = Substitute.For<IConfigurationBuilder>();

        // Act
        var provider1 = source.Build(builder);
        source.KeyFilter = "modified:*"; // Modify source after first build
        var provider2 = source.Build(builder);

        // Assert
        provider1.Should().NotBeSameAs(provider2);
        provider1.Should().BeOfType<AzureAppConfigurationProvider>();
        provider2.Should().BeOfType<AzureAppConfigurationProvider>();
    }

    [Fact]
    public void Source_AfterBuild_CanBeModified()
    {
        // Arrange
        var source = new AzureAppConfigurationSource
        {
            ConnectionString = "https://original-config.azconfig.io"
        };
        var builder = Substitute.For<IConfigurationBuilder>();

        // Act
        var provider = source.Build(builder);
        source.ConnectionString = "https://modified-config.azconfig.io";

        // Assert
        provider.Should().NotBeNull();
        source.ConnectionString.Should().Be("https://modified-config.azconfig.io");
    }

    #endregion

    #region Connection String Format Tests

    [Theory]
    [InlineData("https://test-config.azconfig.io")]
    [InlineData("Endpoint=https://test-config.azconfig.io;Id=test;Secret=secret")]
    public void ConnectionString_WithValidFormats_SetsValue(string connectionString)
    {
        // Arrange
        var source = new AzureAppConfigurationSource
        {
            // Act
            ConnectionString = connectionString
        };

        // Assert
        source.ConnectionString.Should().Be(connectionString);
    }

    [Fact]
    public void Build_WithFullConnectionString_ThrowsInvalidOperationException()
    {
        // Arrange
        var source = new AzureAppConfigurationSource
        {
            ConnectionString = "Endpoint=https://test-config.azconfig.io;Id=test-id;Secret=test-secret"
        };
        var builder = Substitute.For<IConfigurationBuilder>();

        // Act & Assert
        // Even with a well-formed connection string, provider creation will fail without valid Azure credentials
        var action = () => source.Build(builder);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Failed to create Azure App Configuration client. Ensure connection string or credentials are properly configured.");
    }

    [Fact]
    public void Build_WithEndpointOnlyConnectionString_CreatesProvider()
    {
        // Arrange
        var source = new AzureAppConfigurationSource
        {
            ConnectionString = "https://test-config.azconfig.io"
        };
        var builder = Substitute.For<IConfigurationBuilder>();

        // Act
        var provider = source.Build(builder);

        // Assert
        provider.Should().NotBeNull();
        provider.Should().BeOfType<AzureAppConfigurationProvider>();
    }

    #endregion

    #region Label and KeyFilter Combination Tests

    [Fact]
    public void Build_WithBothLabelAndKeyFilter_CreatesProvider()
    {
        // Arrange
        var source = new AzureAppConfigurationSource
        {
            ConnectionString = "https://test-config.azconfig.io",
            KeyFilter = "myapp:*",
            Label = "production"
        };
        var builder = Substitute.For<IConfigurationBuilder>();

        // Act
        var provider = source.Build(builder);

        // Assert
        provider.Should().NotBeNull();
        provider.Should().BeOfType<AzureAppConfigurationProvider>();
    }

    [Fact]
    public void Build_WithComplexKeyFilter_CreatesProvider()
    {
        // Arrange
        var source = new AzureAppConfigurationSource
        {
            ConnectionString = "https://test-config.azconfig.io",
            KeyFilter = "myapp:database:*,myapp:api:*"
        };
        var builder = Substitute.For<IConfigurationBuilder>();

        // Act
        var provider = source.Build(builder);

        // Assert
        provider.Should().NotBeNull();
        provider.Should().BeOfType<AzureAppConfigurationProvider>();
    }

    #endregion
}