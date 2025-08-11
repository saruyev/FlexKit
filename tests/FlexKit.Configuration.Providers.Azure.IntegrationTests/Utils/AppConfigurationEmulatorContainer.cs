using Azure.Data.AppConfiguration;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Newtonsoft.Json;
// ReSharper disable NullableWarningSuppressionIsUsed
// ReSharper disable TooManyArguments

// ReSharper disable ComplexConditionExpression

namespace FlexKit.Configuration.Providers.Azure.IntegrationTests.Utils;

public class AppConfigurationEmulatorContainer : IAsyncDisposable
{
    private readonly IContainer _container = new ContainerBuilder()
        .WithImage("tnc1997/azure-app-configuration-emulator:latest")
        .WithPortBinding(8080, true)
        .WithEnvironment("ASPNETCORE_HTTP_PORTS", "8080")
        .WithEnvironment("Authentication__Schemes__Hmac__Credential", "abcd")
        .WithEnvironment("Authentication__Schemes__Hmac__Secret", "c2VjcmV0")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(8080))
        .Build();
    private ConfigurationClient? _configurationClient;
    
    /// <summary>
    /// Gets the ConfigurationClient configured for the emulator.
    /// This can be injected into the FlexKit configuration for testing.
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

    public async Task StartAsync()
    {
        await _container.StartAsync();
        Console.WriteLine($"App Configuration Emulator started at {GetConnectionString()}");
    }

    public async ValueTask DisposeAsync()
    {
        _configurationClient = null;
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
    
    public async Task CreateTestDataAsync(string configFilePath, string prefix)
    {
        var jsonContent = await File.ReadAllTextAsync(configFilePath);
        var json = (Dictionary<string, object>)JsonHelper.Deserialize(jsonContent);
        await CreateAppConfigurationSettingsAsync(json!, prefix);
    }
    
    private async Task CreateAppConfigurationSettingsAsync(Dictionary<string, object> settings, string prefix)
    {
        foreach (var setting in settings)
        {
            await ProcessSettingAsync(setting.Key, setting.Value, prefix);
        }
    }
    
    private async Task ProcessSettingAsync(string key, object value, string prefix, string? label = null)
    {
        if (value is Dictionary<string, object> nestedSettings)
        {
            foreach (var nested in nestedSettings)
            {
                var nestedKey = $"{key}:{nested.Key}";
                await ProcessSettingAsync(nestedKey, nested.Value, prefix, label);
            }
        }
        else
        {
            var prefixedKey =$"{prefix}:{key}";
            await SetConfigurationAsync(prefixedKey, value.ToString()!, label);
        }
    }
}