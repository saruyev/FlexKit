using Azure.Security.KeyVault.Secrets;
using AzureKeyVaultEmulator.TestContainers;
using AzureKeyVaultEmulator.TestContainers.Helpers;
using Newtonsoft.Json.Linq;

// ReSharper disable NullableWarningSuppressionIsUsed
// ReSharper disable HollowTypeName

namespace FlexKit.Configuration.Providers.Azure.PerformanceTests.Utils;

public class KeyVaultEmulatorContainer : IAsyncDisposable
{
    private readonly AzureKeyVaultEmulatorContainer _container = new(
        certificatesDirectory : "/Users/michaels/certs",
        persist: false,
        generateCertificates: true,
        forceCleanupCertificates: false);
    
    // Static lock to synchronize access across all instances
    private static readonly SemaphoreSlim SecretCreationLock = new(1, 1);
    
    /// <summary>
    /// Gets the SecretClient configured for the emulator.
    /// This can be injected into the FlexKit configuration for testing.
    /// </summary>
    public SecretClient SecretClient => _container.GetSecretClient();

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
    
    public async Task SetSecretAsync(string name, string value, string? prefix = null)
    {
        try
        {
            await _container.GetSecretClient().SetSecretAsync(prefix == null? name : $"{prefix}:{name}", value);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Secret '{prefix}:{name}' failed.");
            Console.WriteLine(e);
            throw;
        }
    }
    
    public async Task CreateTestDataAsync(string configFilePath, string? prefix = null)
    {
        // Use the static semaphore to ensure only one feature can create secrets at a time
        await SecretCreationLock.WaitAsync();
        try
        {
            Console.WriteLine($"[{prefix}] Acquiring secret creation lock...");
            
            var jsonContent = await File.ReadAllTextAsync(configFilePath);
            var json = (Dictionary<string, object>)JsonHelper.Deserialize(jsonContent);
            await CreateKeyVaultSecretsAsync(json, prefix);
            
            Console.WriteLine($"[{prefix}] Secret creation completed successfully.");
        }
        finally
        {
            Console.WriteLine($"[{prefix}] Releasing secret creation lock...");
            SecretCreationLock.Release();
        }
    }
    
    private async Task CreateKeyVaultSecretsAsync(Dictionary<string, object> secrets, string? prefix)
    {
        foreach (var secret in secrets)
        {
            if (secret.Value is Dictionary<string, object> nestedSecrets)
            {
                await CreateKeyVaultSecretsAsync(nestedSecrets, prefix == null? secret.Key : $"{prefix}:{secret.Key}");
            }
            else
            {
                await SetSecretAsync(secret.Key, secret.Value.ToString()!, prefix);
            }
        }
    }
}

public static class JsonHelper
{
    public static object Deserialize(string json)
    {
        return ToObject(JToken.Parse(json));
    }

    private static object ToObject(JToken token)
    {
        switch (token.Type)
        {
            case JTokenType.Object:
                return token.Children<JProperty>()
                    .ToDictionary(prop => prop.Name,
                        prop => ToObject(prop.Value));

            case JTokenType.Array:
                return token.Select(ToObject).ToList();

            default:
                return ((JValue)token).Value!;
        }
    }
}