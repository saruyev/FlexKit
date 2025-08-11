// ReSharper disable MethodTooLong
// ReSharper disable ClassNeverInstantiated.Global

namespace FlexKit.Configuration.Providers.Azure.IntegrationTests.Utils;

/// <summary>
/// Model for Azure test data configuration.
/// Represents the structure of test data JSON files used in integration tests.
/// </summary>
public class AzureTestDataModel
{
    /// <summary>
    /// Key Vault secrets data.
    /// </summary>
    public Dictionary<string, string>? KeyVaultSecrets { get; init; }

    /// <summary>
    /// App Configuration settings data.
    /// </summary>
    public Dictionary<string, string>? AppConfigurationSettings { get; init; }

    /// <summary>
    /// App Configuration settings with labels.
    /// </summary>
    public Dictionary<string, Dictionary<string, string>>? LabeledAppConfigurationSettings { get; init; }

    /// <summary>
    /// Feature flags configuration.
    /// </summary>
    public Dictionary<string, bool>? FeatureFlags { get; init; }

    /// <summary>
    /// JSON secrets that should be processed as hierarchical configuration.
    /// </summary>
    public Dictionary<string, object>? JsonSecrets { get; init; }
}
