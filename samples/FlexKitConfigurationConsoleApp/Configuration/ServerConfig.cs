namespace FlexKitConfigurationConsoleApp.Configuration;

public class ServerConfig
{
    public string Name { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public bool IsActive { get; set; }
}