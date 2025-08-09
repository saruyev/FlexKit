using FlexKit.Configuration.Providers.Azure.Sources;
using FluentAssertions;
using Xunit;
// ReSharper disable NullableWarningSuppressionIsUsed

namespace FlexKit.Configuration.Providers.Azure.Tests.Sources;

/// <summary>
/// Unit tests for exception classes in the Azure configuration providers.
/// Tests cover exception creation, properties, serialization, and inheritance.
/// </summary>
public class ExceptionTests
{
    #region AppConfigurationProviderException Tests

    [Fact]
    public void AppConfigurationProviderException_Constructor_SetsPropertiesCorrectly()
    {
        // Arrange
        var source = new AzureAppConfigurationSource
        {
            ConnectionString = "https://test-config.azconfig.io"
        };
        var innerException = new InvalidOperationException("Inner exception message");

        // Act
        var exception = new AppConfigurationProviderException(source, innerException);

        // Assert
        exception.Source.Should().Be("https://test-config.azconfig.io");
        exception.InnerException.Should().BeSameAs(innerException);
        exception.Message.Should().Contain("Failed to load configuration from Azure App Configuration source");
        exception.Message.Should().Contain("https://test-config.azconfig.io");
    }

    [Fact]
    public void AppConfigurationProviderException_WithNullConnectionString_HandlesCorrectly()
    {
        // Arrange
        var source = new AzureAppConfigurationSource
        {
            ConnectionString = null!
        };
        var innerException = new InvalidOperationException("Inner exception message");

        // Act
        var exception = new AppConfigurationProviderException(source, innerException);

        // Assert
        exception.Source.Should().BeNull();
        exception.InnerException.Should().BeSameAs(innerException);
        exception.Message.Should().Contain("Failed to load configuration from Azure App Configuration source");
    }

    [Fact]
    public void AppConfigurationProviderException_WithEmptyConnectionString_HandlesCorrectly()
    {
        // Arrange
        var source = new AzureAppConfigurationSource
        {
            ConnectionString = string.Empty
        };
        var innerException = new InvalidOperationException("Inner exception message");

        // Act
        var exception = new AppConfigurationProviderException(source, innerException);

        // Assert
        exception.Source.Should().BeEmpty();
        exception.InnerException.Should().BeSameAs(innerException);
    }

    [Fact]
    public void AppConfigurationProviderException_InheritsFromException()
    {
        // Arrange
        var source = new AzureAppConfigurationSource
        {
            ConnectionString = "https://test-config.azconfig.io"
        };
        var innerException = new InvalidOperationException("Inner exception message");

        // Act
        var exception = new AppConfigurationProviderException(source, innerException);

        // Assert
        exception.Should().BeAssignableTo<Exception>();
    }

    [Fact]
    public void AppConfigurationProviderException_WithComplexConnectionString_ParsesCorrectly()
    {
        // Arrange
        var source = new AzureAppConfigurationSource
        {
            ConnectionString = "Endpoint=https://complex-config.azconfig.io;Id=test-id;Secret=test-secret"
        };
        var innerException = new InvalidOperationException("Inner exception message");

        // Act
        var exception = new AppConfigurationProviderException(source, innerException);

        // Assert
        exception.Source.Should().Be("Endpoint=https://complex-config.azconfig.io;Id=test-id;Secret=test-secret");
        exception.Message.Should().Contain("Endpoint=https://complex-config.azconfig.io;Id=test-id;Secret=test-secret");
    }

    #endregion

    #region KeyVaultConfigurationProviderException Tests

    [Fact]
    public void KeyVaultConfigurationProviderException_Constructor_SetsPropertiesCorrectly()
    {
        // Arrange
        var source = new AzureKeyVaultConfigurationSource
        {
            VaultUri = "https://test-vault.vault.azure.net/"
        };
        var innerException = new InvalidOperationException("Inner exception message");

        // Act
        var exception = new KeyVaultConfigurationProviderException(source, innerException);

        // Assert
        exception.Source.Should().Be("https://test-vault.vault.azure.net/");
        exception.InnerException.Should().BeSameAs(innerException);
        exception.Message.Should().Contain("Failed to load configuration from Azure Key Vault source");
        exception.Message.Should().Contain("https://test-vault.vault.azure.net/");
    }

    [Fact]
    public void KeyVaultConfigurationProviderException_WithNullVaultUri_HandlesCorrectly()
    {
        // Arrange
        var source = new AzureKeyVaultConfigurationSource
        {
            VaultUri = null!
        };
        var innerException = new InvalidOperationException("Inner exception message");

        // Act
        var exception = new KeyVaultConfigurationProviderException(source, innerException);

        // Assert
        exception.Source.Should().BeNull();
        exception.InnerException.Should().BeSameAs(innerException);
        exception.Message.Should().Contain("Failed to load configuration from Azure Key Vault source");
    }

    [Fact]
    public void KeyVaultConfigurationProviderException_WithEmptyVaultUri_HandlesCorrectly()
    {
        // Arrange
        var source = new AzureKeyVaultConfigurationSource
        {
            VaultUri = string.Empty
        };
        var innerException = new InvalidOperationException("Inner exception message");

        // Act
        var exception = new KeyVaultConfigurationProviderException(source, innerException);

        // Assert
        exception.Source.Should().BeEmpty();
        exception.InnerException.Should().BeSameAs(innerException);
    }

    [Fact]
    public void KeyVaultConfigurationProviderException_InheritsFromException()
    {
        // Arrange
        var source = new AzureKeyVaultConfigurationSource
        {
            VaultUri = "https://test-vault.vault.azure.net/"
        };
        var innerException = new InvalidOperationException("Inner exception message");

        // Act
        var exception = new KeyVaultConfigurationProviderException(source, innerException);

        // Assert
        exception.Should().BeAssignableTo<Exception>();
    }

    #endregion

    #region Exception Message Tests

    [Fact]
    public void AppConfigurationProviderException_Message_ContainsRelevantInformation()
    {
        // Arrange
        var source = new AzureAppConfigurationSource
        {
            ConnectionString = "https://test-config.azconfig.io"
        };
        var innerException = new UnauthorizedAccessException("Access denied");

        // Act
        var exception = new AppConfigurationProviderException(source, innerException);

        // Assert
        exception.Message.Should().Contain("Failed to load configuration");
        exception.Message.Should().Contain("Azure App Configuration");
        exception.Message.Should().Contain("https://test-config.azconfig.io");
    }

    [Fact]
    public void KeyVaultConfigurationProviderException_Message_ContainsRelevantInformation()
    {
        // Arrange
        var source = new AzureKeyVaultConfigurationSource
        {
            VaultUri = "https://test-vault.vault.azure.net/"
        };
        var innerException = new UnauthorizedAccessException("Access denied");

        // Act
        var exception = new KeyVaultConfigurationProviderException(source, innerException);

        // Assert
        exception.Message.Should().Contain("Failed to load configuration");
        exception.Message.Should().Contain("Azure Key Vault");
        exception.Message.Should().Contain("https://test-vault.vault.azure.net/");
    }

    #endregion

    #region Exception Chaining Tests

    [Fact]
    public void AppConfigurationProviderException_WithNestedInnerExceptions_PreservesChain()
    {
        // Arrange
        var rootException = new ArgumentException("Root cause");
        var middleException = new InvalidOperationException("Middle exception", rootException);
        var source = new AzureAppConfigurationSource
        {
            ConnectionString = "https://test-config.azconfig.io"
        };

        // Act
        var exception = new AppConfigurationProviderException(source, middleException);

        // Assert
        exception.InnerException.Should().BeSameAs(middleException);
        exception.InnerException!.InnerException.Should().BeSameAs(rootException);
    }

    [Fact]
    public void KeyVaultConfigurationProviderException_WithNestedInnerExceptions_PreservesChain()
    {
        // Arrange
        var rootException = new ArgumentException("Root cause");
        var middleException = new InvalidOperationException("Middle exception", rootException);
        var source = new AzureKeyVaultConfigurationSource
        {
            VaultUri = "https://test-vault.vault.azure.net/"
        };

        // Act
        var exception = new KeyVaultConfigurationProviderException(source, middleException);

        // Assert
        exception.InnerException.Should().BeSameAs(middleException);
        exception.InnerException!.InnerException.Should().BeSameAs(rootException);
    }

    #endregion

    #region Exception Source Override Tests

    [Fact]
    public void AppConfigurationProviderException_SourceProperty_OverridesBaseSource()
    {
        // Arrange
        var source = new AzureAppConfigurationSource
        {
            ConnectionString = "https://override-test-config.azconfig.io"
        };
        var innerException = new InvalidOperationException("Inner exception");

        // Act
        var exception = new AppConfigurationProviderException(source, innerException);

        // Assert
        exception.Source.Should().Be("https://override-test-config.azconfig.io");
        // The base Exception.Source property should be overridden
    }

    [Fact]
    public void KeyVaultConfigurationProviderException_SourceProperty_OverridesBaseSource()
    {
        // Arrange
        var source = new AzureKeyVaultConfigurationSource
        {
            VaultUri = "https://override-test-vault.vault.azure.net/"
        };
        var innerException = new InvalidOperationException("Inner exception");

        // Act
        var exception = new KeyVaultConfigurationProviderException(source, innerException);

        // Assert
        exception.Source.Should().Be("https://override-test-vault.vault.azure.net/");
        // The base Exception.Source property should be overridden
    }

    #endregion

    #region Exception Serialization Tests

    [Fact]
    public void AppConfigurationProviderException_ToString_ContainsAllRelevantInformation()
    {
        // Arrange
        var source = new AzureAppConfigurationSource
        {
            ConnectionString = "https://test-config.azconfig.io"
        };
        var innerException = new InvalidOperationException("Inner exception message");
        var exception = new AppConfigurationProviderException(source, innerException);

        // Act
        var stringRepresentation = exception.ToString();

        // Assert
        stringRepresentation.Should().Contain("AppConfigurationProviderException");
        stringRepresentation.Should().Contain("Failed to load configuration");
        stringRepresentation.Should().Contain("https://test-config.azconfig.io");
        stringRepresentation.Should().Contain("Inner exception message");
    }

    [Fact]
    public void KeyVaultConfigurationProviderException_ToString_ContainsAllRelevantInformation()
    {
        // Arrange
        var source = new AzureKeyVaultConfigurationSource
        {
            VaultUri = "https://test-vault.vault.azure.net/"
        };
        var innerException = new InvalidOperationException("Inner exception message");
        var exception = new KeyVaultConfigurationProviderException(source, innerException);

        // Act
        var stringRepresentation = exception.ToString();

        // Assert
        stringRepresentation.Should().Contain("KeyVaultConfigurationProviderException");
        stringRepresentation.Should().Contain("Failed to load configuration");
        stringRepresentation.Should().Contain("https://test-vault.vault.azure.net/");
        stringRepresentation.Should().Contain("Inner exception message");
    }

    #endregion

    #region Exception Equality Tests

    [Fact]
    public void AppConfigurationProviderException_TwoInstancesWithSameData_AreNotEqual()
    {
        // Arrange
        var source = new AzureAppConfigurationSource
        {
            ConnectionString = "https://test-config.azconfig.io"
        };
        var innerException = new InvalidOperationException("Inner exception");

        // Act
        var exception1 = new AppConfigurationProviderException(source, innerException);
        var exception2 = new AppConfigurationProviderException(source, innerException);

        // Assert
        exception1.Should().NotBe(exception2);
        exception1.Should().NotBeSameAs(exception2);
    }

    [Fact]
    public void KeyVaultConfigurationProviderException_TwoInstancesWithSameData_AreNotEqual()
    {
        // Arrange
        var source = new AzureKeyVaultConfigurationSource
        {
            VaultUri = "https://test-vault.vault.azure.net/"
        };
        var innerException = new InvalidOperationException("Inner exception");

        // Act
        var exception1 = new KeyVaultConfigurationProviderException(source, innerException);
        var exception2 = new KeyVaultConfigurationProviderException(source, innerException);

        // Assert
        exception1.Should().NotBe(exception2);
        exception1.Should().NotBeSameAs(exception2);
    }

    #endregion

    #region Exception Data Tests

    [Fact]
    public void AppConfigurationProviderException_CanStoreAdditionalData()
    {
        // Arrange
        var source = new AzureAppConfigurationSource
        {
            ConnectionString = "https://test-config.azconfig.io"
        };
        var innerException = new InvalidOperationException("Inner exception");
        var exception = new AppConfigurationProviderException(source, innerException)
        {
            Data =
            {
                // Act
                ["CustomKey"] = "CustomValue",
                ["Timestamp"] = DateTime.UtcNow
            }
        };

        // Assert
        exception.Data["CustomKey"].Should().Be("CustomValue");
        exception.Data["Timestamp"].Should().BeOfType<DateTime>();
        exception.Data.Count.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void KeyVaultConfigurationProviderException_CanStoreAdditionalData()
    {
        // Arrange
        var source = new AzureKeyVaultConfigurationSource
        {
            VaultUri = "https://test-vault.vault.azure.net/"
        };
        var innerException = new InvalidOperationException("Inner exception");
        var exception = new KeyVaultConfigurationProviderException(source, innerException)
        {
            Data =
            {
                // Act
                ["CustomKey"] = "CustomValue",
                ["OperationId"] = Guid.NewGuid()
            }
        };

        // Assert
        exception.Data["CustomKey"].Should().Be("CustomValue");
        exception.Data["OperationId"].Should().BeOfType<Guid>();
        exception.Data.Count.Should().BeGreaterThanOrEqualTo(2);
    }

    #endregion

    #region Exception Handling Integration Tests

    [Fact]
    public void ExceptionHandling_WithAppConfigurationProviderException_CanBeHandledAsGeneralException()
    {
        // Arrange
        var source = new AzureAppConfigurationSource
        {
            ConnectionString = "https://test-config.azconfig.io"
        };
        var innerException = new InvalidOperationException("Inner exception");
        var exception = new AppConfigurationProviderException(source, innerException);

        // Act & Assert
        bool handledAsException;
        try
        {
            throw exception;
        }
        catch (Exception ex)
        {
            handledAsException = true;
            ex.Should().BeOfType<AppConfigurationProviderException>();
            ex.Source.Should().Be("https://test-config.azconfig.io");
        }

        handledAsException.Should().BeTrue();
    }

    [Fact]
    public void ExceptionHandling_WithKeyVaultConfigurationProviderException_CanBeHandledAsGeneralException()
    {
        // Arrange
        var source = new AzureKeyVaultConfigurationSource
        {
            VaultUri = "https://test-vault.vault.azure.net/"
        };
        var innerException = new InvalidOperationException("Inner exception");
        var exception = new KeyVaultConfigurationProviderException(source, innerException);

        // Act & Assert
        bool handledAsException;
        try
        {
            throw exception;
        }
        catch (Exception ex)
        {
            handledAsException = true;
            ex.Should().BeOfType<KeyVaultConfigurationProviderException>();
            ex.Source.Should().Be("https://test-vault.vault.azure.net/");
        }

        handledAsException.Should().BeTrue();
    }

    #endregion
}