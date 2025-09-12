namespace FlexKitConfigurationConsoleApp.Configuration;

public class DatabaseConfig
{
    public string ConnectionString { get; set; } = string.Empty;
    public int CommandTimeout { get; set; }
    public int MaxRetries { get; set; }
    public Dictionary<string, DatabaseProvider> Providers { get; set; } = new();
}

public class DatabaseProvider
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Database { get; set; } = string.Empty;
}