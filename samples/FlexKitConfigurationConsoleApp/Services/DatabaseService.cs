using FlexKit.Configuration.Core;
using FlexKitConfigurationConsoleApp.Configuration;

namespace FlexKitConfigurationConsoleApp.Services;

public interface IDatabaseService
{
    Task<string> GetConnectionStringAsync();
    Task<bool> TestConnectionAsync();
    void DisplayConfiguration();
}

// Demonstrates constructor injection with strongly typed configuration
public class DatabaseService : IDatabaseService
{
    private readonly DatabaseConfig _config;

    public DatabaseService(DatabaseConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public Task<string> GetConnectionStringAsync()
    {
        return Task.FromResult(_config.ConnectionString);
    }

    public Task<bool> TestConnectionAsync()
    {
        Console.WriteLine($"[DatabaseService] Testing connection with timeout: {_config.CommandTimeout}s");
        Console.WriteLine($"[DatabaseService] Connection string: {_config.ConnectionString}");
        Console.WriteLine($"[DatabaseService] Max retries: {_config.MaxRetries}");
        
        // Simulate connection test
        return Task.FromResult(true);
    }

    public void DisplayConfiguration()
    {
        Console.WriteLine("=== Database Service Configuration (Constructor Injection) ===");
        Console.WriteLine($"Connection String: {_config.ConnectionString}");
        Console.WriteLine($"Command Timeout: {_config.CommandTimeout}");
        Console.WriteLine($"Max Retries: {_config.MaxRetries}");
        Console.WriteLine($"Providers Count: {_config.Providers.Count}");
        
        foreach (var provider in _config.Providers)
        {
            Console.WriteLine($"  Provider '{provider.Key}': {provider.Value.Host}:{provider.Value.Port}/{provider.Value.Database}");
        }
        Console.WriteLine();
    }
}