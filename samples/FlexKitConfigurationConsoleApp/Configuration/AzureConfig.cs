namespace FlexKitConfigurationConsoleApp.Configuration;

public class AzureConfig
{
    public KeyVaultConfig KeyVault { get; set; } = new();
    public AppConfigurationConfig AppConfiguration { get; set; } = new();
}

public class KeyVaultConfig
{
    public string VaultUri { get; set; } = string.Empty;
    public bool Optional { get; set; } = true;
    public bool JsonProcessor { get; set; }
    public string[] JsonProcessorSecrets { get; set; } = Array.Empty<string>();
    public int ReloadIntervalMinutes { get; set; }
}

public class AppConfigurationConfig
{
    public string ConnectionString { get; set; } = string.Empty;
    public bool Optional { get; set; } = true;
    public string KeyFilter { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool JsonProcessor { get; set; }
    public int ReloadIntervalMinutes { get; set; }
}

public class AzureFeaturesConfig
{
    public CachingConfig Caching { get; set; } = new();
    public LoggingConfig Logging { get; set; } = new();
    public FeatureFlagsConfig FeatureFlags { get; set; } = new();
    public LimitsConfig Limits { get; set; } = new();
}

public class CachingConfig
{
    public bool Enabled { get; set; }
    public int Ttl { get; set; }
}

public class LoggingConfig
{
    public string Level { get; set; } = string.Empty;
    public bool EnableConsole { get; set; }
}

public class FeatureFlagsConfig
{
    public bool EnableNewUI { get; set; }
    public bool EnableBetaFeatures { get; set; }
}

public class LimitsConfig
{
    public int MaxUsers { get; set; }
    public int MaxRequests { get; set; }
}