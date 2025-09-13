namespace FlexKitConfigurationConsoleApp.Configuration;

public class ApiConfig
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public int Timeout { get; set; }
    public int RetryCount { get; set; }
    public bool EnableLogging { get; set; }
    public ApiFeatures Features { get; set; } = new();
}

public class ApiFeatures
{
    public bool Caching { get; set; }
    public bool Compression { get; set; }
    public AuthenticationConfig Authentication { get; set; } = new();
}

public class AuthenticationConfig
{
    public string Type { get; set; } = string.Empty;
    public int TokenExpiry { get; set; }
}