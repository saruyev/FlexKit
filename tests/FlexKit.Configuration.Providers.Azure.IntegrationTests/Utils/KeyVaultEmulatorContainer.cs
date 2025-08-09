using Azure.Security.KeyVault.Secrets;
using AzureKeyVaultEmulator.TestContainers;
using AzureKeyVaultEmulator.TestContainers.Helpers;
using Newtonsoft.Json;

namespace FlexKit.Configuration.Providers.Azure.PerformanceTests.Utils;


public class KeyVaultEmulatorContainer : IAsyncDisposable
{
    private readonly AzureKeyVaultEmulatorContainer _container;
    
    /// <summary>
    /// Gets the SecretClient configured for the emulator.
    /// This can be injected into FlexKit configuration for testing.
    /// </summary>
    public SecretClient SecretClient => _container.GetSecretClient();

    public KeyVaultEmulatorContainer()
    {
        _container = new AzureKeyVaultEmulatorContainer(
            certificatesDirectory : "/Users/michaels/certs",
            persist: false,
            generateCertificates: true,
            forceCleanupCertificates: false);
    }

    public async Task StartAsync()
    {
        await _container.StartAsync();
        Console.WriteLine($"Key Vault Emulator started at {_container.GetConnectionString()}");
    }

    public async ValueTask DisposeAsync()
    {
        await _container.StopAsync();
        Console.WriteLine("Key Vault Emulator stopped.");
    }
    
    public async Task SetSecretAsync(string name, string value)
    {
        await _container.GetSecretClient().SetSecretAsync(name, value);
        Console.WriteLine($"Secret '{name}' set.");
    }

    public async Task<string?> GetSecretAsync(string name)
    {
        KeyVaultSecret secret = await _container.GetSecretClient().GetSecretAsync(name);
        Console.WriteLine($"Secret '{name}' retrieved.");
        return secret.Value;
    }
    
    public async Task CreateTestDataAsync(string configFilePath)
    {
        var jsonContent = await File.ReadAllTextAsync(configFilePath);
        var json = JsonConvert.DeserializeObject<Dictionary<string, Object>>(jsonContent);
        await CreateKeyVaultSecretsAsync(json!);
    }
    
    private async Task CreateKeyVaultSecretsAsync(Dictionary<string, object> secrets)
    {
        foreach (var secret in secrets)
        {
            await SetSecretAsync(secret.Key, secret.Value.ToString()!);
        }
    }
}