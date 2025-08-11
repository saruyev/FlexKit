// <copyright file="AzureKeyVaultConfigurationSource.cs" company="FlexKit">
// Copyright (c) FlexKit. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

using Azure.Core;
using Azure.Security.KeyVault.Secrets;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;

namespace FlexKit.Configuration.Providers.Azure.Sources;

/// <summary>
/// Configuration source that represents Azure Key Vault in the configuration system.
/// Implements IConfigurationSource to integrate Key Vault support with the standard .NET configuration
/// infrastructure, enabling secret data to be used alongside other configuration sources.
/// </summary>
/// <remarks>
/// This class serves as the factory and metadata container for Azure Key Vault configuration providers.
/// It follows the standard .NET configuration pattern where sources are responsible for creating their
/// corresponding providers and defining configuration parameters.
///
/// <para>
/// <strong>Role in Configuration Pipeline:</strong>
/// <list type="number">
/// <item>Defines the Key Vault URI and loading options</item>
/// <item>Integrates with ConfigurationBuilder through IConfigurationSource</item>
/// <item>Creates AzureKeyVaultConfigurationProvider instances when requested</item>
/// <item>Provides metadata about the Key Vault source to the configuration system</item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Integration with FlexKit:</strong>
/// This source is designed to work seamlessly with FlexConfigurationBuilder and other FlexKit
/// configuration components, providing Azure Key Vault support as a first-class configuration
/// source option that maintains all FlexKit capabilities, including dynamic access and type conversion.
/// </para>
/// </remarks>
public class AzureKeyVaultConfigurationSource : IConfigurationSource
{
    /// <summary>
    /// Gets or sets the Key Vault URI to load secrets from.
    /// This URI defines the specific Key Vault instance from which to retrieve secrets.
    /// </summary>
    /// <value>
    /// The Key Vault URI (e.g., "https://myapp-vault.vault.azure.net/").
    /// Must be a valid HTTPS URI pointing to an Azure Key Vault.
    /// </value>
    public string VaultUri { get; [UsedImplicitly] set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the Key Vault source is optional.
    /// When true, failures to load secrets will not cause configuration building to fail.
    /// </summary>
    /// <value>
    /// True if the Key Vault source is optional; false if it's required.
    /// Default is true.
    /// </value>
    public bool Optional { get; [UsedImplicitly] set; } = true;

    /// <summary>
    /// Gets or sets the automatic reload interval for Key Vault data.
    /// When set, the provider will periodically refresh secrets from Key Vault.
    /// </summary>
    /// <value>
    /// The interval at which to reload secrets or null to disable automatic reloading.
    /// Default is null.
    /// </value>
    public TimeSpan? ReloadAfter { get; [UsedImplicitly] set; }

    /// <summary>
    /// Gets or sets the Azure credential for Key Vault access.
    /// Provides control over Azure authentication and credential management.
    /// </summary>
    /// <value>
    /// The Azure credential to use for Key Vault access, or null to use defaults.
    /// Default is null (uses the Azure credential resolution chain).
    /// </value>
    public TokenCredential? Credential { get; [UsedImplicitly] set; }

    /// <summary>
    /// Gets or sets a value indicating whether JSON secrets should be automatically flattened.
    /// When enabled, secret values containing valid JSON will be processed into hierarchical configuration keys.
    /// </summary>
    /// <value>
    /// True to enable JSON processing; false to treat all secrets as simple strings.
    /// Default is false.
    /// </value>
    public bool JsonProcessor { get; [UsedImplicitly] set; }

    /// <summary>
    /// Gets or sets the specific secret names that should have JSON processing applied.
    /// When specified, only secrets matching these names will be processed as JSON.
    /// </summary>
    /// <value>
    /// An array of secret names for selective JSON processing,
    /// or null to apply to all secrets when JsonProcessor is enabled.
    /// </value>
    public string[]? JsonProcessorSecrets { get; [UsedImplicitly] set; }

    /// <summary>
    /// Gets or sets a custom secret processor for transforming secret names.
    /// Allows for application-specific secret name transformation logic.
    /// </summary>
    /// <value>
    /// A custom secret processor implementation, or null for default processing.
    /// </value>
    public IKeyVaultSecretProcessor? SecretProcessor { get; [UsedImplicitly] set; }

    /// <summary>
    /// Gets or sets the error handling callback for optional configuration loading failures.
    /// Invoked when secret loading fails and the source is marked as optional.
    /// </summary>
    /// <value>
    /// An action that handles configuration loading exceptions, or null for default handling.
    /// </value>
    public Action<KeyVaultConfigurationProviderException>? OnLoadException { get; [UsedImplicitly] set; }

    /// <summary>
    /// Gets or sets a pre-configured Azure Key Vault SecretClient for testing scenarios.
    /// When provided, this client will be used instead of creating a new one from the vault URI and credentials.
    /// </summary>
    /// <value>
    /// A configured SecretClient instance, or null to create a new client using the vault URI and credentials.
    /// Default is null.
    /// </value>
    public SecretClient? SecretClient { get; [UsedImplicitly] set; }

    /// <summary>
    /// Creates a new Azure Key Vault configuration provider that will load data from this source.
    /// This method is called by the .NET configuration system when building the configuration
    /// from all registered sources.
    /// </summary>
    /// <param name="builder">
    /// The configuration builder that is requesting the provider.
    /// This parameter is required by the IConfigurationSource interface but is not used by this implementation.
    /// </param>
    /// <returns>
    /// A new <see cref="AzureKeyVaultConfigurationProvider"/> instance configured to load
    /// secrets from Azure Key Vault according to this source's properties.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return SecretClient != null
            ? new AzureKeyVaultConfigurationProvider(this, SecretClient)
            : new AzureKeyVaultConfigurationProvider(this);
    }
}

/// <summary>
/// Interface for custom Key Vault secret name processing logic.
/// Implementations can transform Key Vault secret names into configuration keys
/// according to application-specific requirements.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Purpose:</strong>
/// This interface allows applications to customize how Azure Key Vault secret names
/// are converted to .NET configuration keys. This is useful for organizations with specific
/// naming conventions or requirements for secret name transformation.
/// </para>
///
/// <para>
/// <strong>Implementation Guidelines:</strong>
/// <list type="bullet">
/// <item>Implementations should be stateless and thread-safe</item>
/// <item>Secret name transformations should be deterministic and reversible where possible</item>
/// <item>Consider caching expensive transformations if they involve complex logic</item>
/// <item>Validate that transformed keys are valid .NET configuration key formats</item>
/// </list>
/// </para>
/// </remarks>
public interface IKeyVaultSecretProcessor
{
    /// <summary>
    /// Processes a secret name to transform it into a configuration key.
    /// This method is called for each secret retrieved from Key Vault
    /// after the default name transformation has been applied.
    /// </summary>
    /// <param name="configKey">
    /// The configuration key after default processing (double hyphens converted to colons).
    /// </param>
    /// <param name="originalSecretName">
    /// The original Key Vault secret name before any processing.
    /// </param>
    /// <returns>
    /// The final configuration key to use for this secret in the .NET configuration system.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <strong>Processing Order:</strong>
    /// This method is called after the default secret name transformation:
    /// <list type="number">
    /// <item>Double hyphens (--) are converted to colons (:) for hierarchical keys</item>
    /// <item>This method is called with the transformed key</item>
    /// <item>The result becomes the final configuration key</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// <strong>Return Value Requirements:</strong>
    /// <list type="bullet">
    /// <item>Must return a valid .NET configuration key (non-null, non-empty)</item>
    /// <item>Should use colons (:) as hierarchy separators</item>
    /// <item>Should avoid characters that might cause configuration binding issues</item>
    /// <item>Should be consistent and deterministic for the same input</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when the transformation results in an invalid configuration key.</exception>
    string ProcessSecretName(string configKey, string originalSecretName);
}

/// <summary>
/// Represents an exception that occurs during Key Vault configuration provider loading.
/// Used to provide context about configuration loading failures for error handling and logging.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="KeyVaultConfigurationProviderException"/> class.
/// </remarks>
/// <param name="source">The configuration source that caused the exception.</param>
/// <param name="innerException">The exception that is the cause of the current exception.</param>
public class KeyVaultConfigurationProviderException(AzureKeyVaultConfigurationSource source, Exception innerException) : Exception($"Failed to load configuration from Azure Key Vault source: {source.VaultUri}", innerException)
{
    /// <summary>
    /// The configuration source that caused the exception.
    /// </summary>
    private readonly string _source = source.VaultUri;

    /// <summary>
    /// Gets the source of the exception (the Key Vault URI).
    /// </summary>
    public override string Source => _source;
}
