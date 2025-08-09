using System.Net;
using Azure.Data.AppConfiguration;
using Azure.Identity;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Newtonsoft.Json;
// ReSharper disable ComplexConditionExpression

namespace FlexKit.Configuration.Providers.Azure.PerformanceTests.Utils;

public class AppConfigurationEmulatorContainer : IAsyncDisposable
{
    private IContainer _container;
    private ConfigurationClient? _configurationClient;
    
    /// <summary>
    /// Gets the ConfigurationClient configured for the emulator.
    /// This can be injected into FlexKit configuration for testing.
    /// </summary>
    public ConfigurationClient ConfigurationClient 
    {
        get
        {
            if (_configurationClient == null)
            {
                var connectionString = GetConnectionString();
                _configurationClient = new ConfigurationClient(connectionString);
            }
            return _configurationClient;
        }
    }

    public AppConfigurationEmulatorContainer()
    {
        _container = new ContainerBuilder()
            .WithImage("tnc1997/azure-app-configuration-emulator:latest")
            .WithPortBinding(8080, true)
            .WithEnvironment("ASPNETCORE_HTTP_PORTS", "8080")
            .WithEnvironment("Authentication__Schemes__Hmac__Credential", "abcd")
            .WithEnvironment("Authentication__Schemes__Hmac__Secret", "c2VjcmV0")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(8080))
            .Build();
    }

    public async Task StartAsync()
    {
        await _container.StartAsync();
        Console.WriteLine($"App Configuration Emulator started at {GetConnectionString()}");
    }

    public async ValueTask DisposeAsync()
    {
        if (_configurationClient != null)
        {
            // ConfigurationClient doesn't implement IDisposable, but we should null it
            _configurationClient = null;
        }
        
        await _container.StopAsync();
        await _container.DisposeAsync();
        Console.WriteLine("App Configuration Emulator stopped.");
    }
    
    public string GetConnectionString()
    {
        var host = _container.Hostname;
        var port = _container.GetMappedPublicPort(8080);
        return $"Endpoint=http://{host}:{port};Id=abcd;Secret=c2VjcmV0";
    }
    
    public async Task SetConfigurationAsync(string key, string value, string? label = null)
    {
        var setting = new ConfigurationSetting(key, value, label);
        await ConfigurationClient.SetConfigurationSettingAsync(setting);
        Console.WriteLine($"Configuration '{key}' set with value '{value}'" + 
                         (label != null ? $" and label '{label}'" : ""));
    }

    public async Task<string?> GetConfigurationAsync(string key, string? label = null)
    {
        try
        {
            var response = await ConfigurationClient.GetConfigurationSettingAsync(key, label);
            Console.WriteLine($"Configuration '{key}' retrieved with value '{response.Value.Value}'");
            return response.Value.Value;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to retrieve configuration '{key}': {ex.Message}");
            return null;
        }
    }
    
    public async Task CreateTestDataAsync(string configFilePath)
    {
        var jsonContent = await File.ReadAllTextAsync(configFilePath);
        var json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonContent);
        await CreateAppConfigurationSettingsAsync(json!);
    }
    
    private async Task CreateAppConfigurationSettingsAsync(Dictionary<string, object> settings)
    {
        foreach (var setting in settings)
        {
            // Handle hierarchical settings by converting nested objects to flat keys
            await ProcessSettingAsync(setting.Key, setting.Value);
        }
    }
    
    private async Task ProcessSettingAsync(string key, object value, string? label = null)
    {
        if (value is Dictionary<string, object> nestedSettings)
        {
            // Handle nested configuration
            foreach (var nested in nestedSettings)
            {
                var nestedKey = $"{key}:{nested.Key}";
                await ProcessSettingAsync(nestedKey, nested.Value, label);
            }
        }
        else
        {
            await SetConfigurationAsync(key, value.ToString()!, label);
        }
    }
}