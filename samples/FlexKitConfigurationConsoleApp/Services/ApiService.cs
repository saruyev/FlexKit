using FlexKit.Configuration.Core;
using FlexKit.Configuration.Conversion;

namespace FlexKitConfigurationConsoleApp.Services;

public interface IApiService
{
    Task<string> CallApiAsync();
    void DisplayConfiguration();
}

// Demonstrates FlexConfig dynamic access with type conversion
public class ApiService : IApiService
{
    private readonly IFlexConfig _flexConfig;

    public ApiService(IFlexConfig flexConfig)
    {
        _flexConfig = flexConfig ?? throw new ArgumentNullException(nameof(flexConfig));
    }

    public Task<string> CallApiAsync()
    {
        // Demonstrate dynamic access
        dynamic config = _flexConfig;
        
        var baseUrl = config.api.BaseUrl.ToString();
        var timeout = ((string?)config.Api.Timeout)?.ToType<int>() ?? 30000;
        
        Console.WriteLine($"[ApiService] Calling API at: {baseUrl}");
        Console.WriteLine($"[ApiService] Using timeout: {timeout}ms");
        
        return Task.FromResult($"API Response from {baseUrl}");
    }

    public void DisplayConfiguration()
    {
        Console.WriteLine("=== API Service Configuration (FlexConfig Dynamic Access) ===");
        
        // Demonstrate dynamic access
        dynamic config = _flexConfig;
        
        Console.WriteLine($"Base URL: {config.Api.BaseUrl}");
        Console.WriteLine($"API Key: {config.Api.ApiKey ?? "Not Set"}");
        Console.WriteLine($"Timeout: {config.Api.Timeout}ms");
        Console.WriteLine($"Retry Count: {config.Api.RetryCount}");
        Console.WriteLine($"Enable Logging: {config.Api.EnableLogging}");
        
        // Demonstrate nested dynamic access
        Console.WriteLine("Features:");
        Console.WriteLine($"  Caching: {config.Api.Features.Caching}");
        Console.WriteLine($"  Compression: {config.Api.Features.Compression}");
        Console.WriteLine($"  Auth Type: {config.Api.Features.Authentication.Type}");
        Console.WriteLine($"  Token Expiry: {config.Api.Features.Authentication.TokenExpiry}s");
        
        // Demonstrate direct key access with type conversion
        Console.WriteLine("\nDirect Key Access with Type Conversion:");
        Console.WriteLine($"Timeout (converted to int): {_flexConfig["Api:Timeout"].ToType<int>()}");
        Console.WriteLine($"Enable Logging (converted to bool): {_flexConfig["Api:EnableLogging"].ToType<bool>()}");
        Console.WriteLine($"Retry Count (converted to int): {_flexConfig["Api:RetryCount"].ToType<int>()}");
        Console.WriteLine();
    }
}