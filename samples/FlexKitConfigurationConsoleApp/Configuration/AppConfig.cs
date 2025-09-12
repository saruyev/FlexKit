namespace FlexKitConfigurationConsoleApp.Configuration;

public class AppConfig
{
    public ApplicationInfo Application { get; set; } = new();
    public DatabaseConfig Database { get; set; } = new();
    public ApiConfig Api { get; set; } = new();
    public List<ServerConfig> Servers { get; set; } = new();
    public FeaturesConfig Features { get; set; } = new();
    public ServiceConfiguration ServiceConfiguration { get; set; } = new();
}

public class ApplicationInfo
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
}

public class FeaturesConfig
{
    public bool EnableNewDashboard { get; set; }
    public int MaxConcurrentUsers { get; set; }
    public int CacheExpiryMinutes { get; set; }
    public string AllowedHosts { get; set; } = string.Empty;
}

public class ServiceConfiguration
{
    public EmailServiceConfig EmailService { get; set; } = new();
    public PaymentServiceConfig PaymentService { get; set; } = new();
}

public class EmailServiceConfig
{
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; }
    public bool EnableSsl { get; set; }
    public string FromAddress { get; set; } = string.Empty;
}

public class PaymentServiceConfig
{
    public string Provider { get; set; } = string.Empty;
    public int Timeout { get; set; }
    public int RetryAttempts { get; set; }
}