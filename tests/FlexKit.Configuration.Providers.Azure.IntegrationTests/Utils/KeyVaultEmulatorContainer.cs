using Azure.Security.KeyVault.Secrets;
using AzureKeyVaultEmulator.TestContainers;
using AzureKeyVaultEmulator.TestContainers.Helpers;
using FlexKit.Configuration.Providers.Azure.Sources;
using Newtonsoft.Json.Linq;

// ReSharper disable NullableWarningSuppressionIsUsed
// ReSharper disable HollowTypeName

namespace FlexKit.Configuration.Providers.Azure.IntegrationTests.Utils;

public class KeyVaultEmulatorContainer : IAsyncDisposable
{
    private readonly AzureKeyVaultEmulatorContainer _container = new(
        certificatesDirectory: Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "certs"),
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

    public async Task SetSecretAsync(string name, string value, string prefix)
    {
        try
        {
            await _container.GetSecretClient().SetSecretAsync($"{prefix}:{name}", value);
            Console.WriteLine($"Secret '{prefix}:{name}' set.");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Secret '{prefix}:{name}' failed.");
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task<string?> GetSecretAsync(string name)
    {
        KeyVaultSecret secret = await _container.GetSecretClient().GetSecretAsync(name);
        Console.WriteLine($"Secret '{name}' retrieved.");
        return secret.Value;
    }

    public async Task CreateTestDataAsync(string configFilePath, string prefix)
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

    private async Task CreateKeyVaultSecretsAsync(Dictionary<string, object> secrets, string prefix)
    {
        foreach (var secret in secrets)
        {
            if (secret.Value is Dictionary<string, object> nestedSecrets)
            {
                await CreateKeyVaultSecretsAsync(nestedSecrets, $"{prefix}:{secret.Key}");
            }
            else
            {
                await SetSecretAsync(secret.Key, secret.Value.ToString()!, prefix);
            }
        }
    }
}

/// <summary>
/// Custom secret processor that filters secrets by scenario prefix and removes the prefix
/// from the configuration keys to maintain clean key names in tests.
/// </summary>
public class ScenarioPrefixSecretProcessor(string scenarioPrefix) : IKeyVaultSecretProcessor
{
    public string ProcessSecretName(string transformedName, string originalName)
    {
        // Only process secrets that start with our scenario prefix
        if (!originalName.StartsWith($"{scenarioPrefix}:", StringComparison.OrdinalIgnoreCase))
        {
            // Return null or empty to skip secrets that don't belong to this scenario
            return string.Empty;
        }

        // Remove the scenario prefix and return the clean key name
        var withoutPrefix = originalName.Substring($"{scenarioPrefix}:".Length);

        // Apply the standard Key Vault transformation (-- to :)
        return withoutPrefix.Replace("--", ":");
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