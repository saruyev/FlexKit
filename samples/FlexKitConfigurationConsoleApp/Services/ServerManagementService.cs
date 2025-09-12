using FlexKit.Configuration.Core;
using FlexKit.Configuration.Conversion;

namespace FlexKitConfigurationConsoleApp.Services;

public interface IServerManagementService
{
    Task<List<string>> GetActiveServersAsync();
    void DisplayConfiguration();
}

// Demonstrates property injection by convention and array/collection access
public class ServerManagementService : IServerManagementService
{
    // Property injection - will be set by Autofac convention
    public IFlexConfig? FlexConfiguration { get; set; }

    public Task<List<string>> GetActiveServersAsync()
    {
        if (FlexConfiguration == null)
        {
            return Task.FromResult(new List<string>());
        }

        var activeServers = new List<string>();
        var serversConfig = FlexConfiguration.Configuration.CurrentConfig("Servers")!;

        // Demonstrate indexed access to configuration arrays
        for (int i = 0; i < 10; i++) // Loop until we find no more servers
        {
            var serverConfig = serversConfig[i];
            if (serverConfig == null) break;

            dynamic server = serverConfig;

            var isActive = (bool)server.IsActive;
            if (isActive)
            {
                var name = server?.Name as string ?? $"Server {i}";
                var host = server?.Host as string ?? "unknown";
                activeServers.Add($"{name} ({host})");
            }
        }

        return Task.FromResult(activeServers);
    }

    public void DisplayConfiguration()
    {
        Console.WriteLine("=== Server Management Service Configuration (Property Injection) ===");
        
        if (FlexConfiguration == null)
        {
            Console.WriteLine("FlexConfiguration is not injected!");
            return;
        }

        Console.WriteLine("Servers (using indexed access):");
        var serversConfig = FlexConfiguration.GetSection("Servers")!;
        
        // Demonstrate indexed access
        for (int i = 0; i < 10; i++) // Check up to 10 servers
        {
            var serverConfig = serversConfig[i];
            if (serverConfig == null) break;

            dynamic server = serverConfig;
            
            Console.WriteLine($"  [{i}] Name: {server?.Name}");
            Console.WriteLine($"      Host: {server?.Host}");
            Console.WriteLine($"      Port: {server?.Port}");
            Console.WriteLine($"      Active: {server?.IsActive}");
        }

        // Demonstrate collection parsing from configuration
        Console.WriteLine("\nFeatures AllowedHosts (collection access):");
        var allowedHosts = FlexConfiguration["Features:AllowedHosts"].GetCollection<string>();
        if (allowedHosts != null)
        {
            foreach (var host in allowedHosts)
            {
                Console.WriteLine($"  - {host}");
            }
        }
        Console.WriteLine();
    }
}