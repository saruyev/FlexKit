using FlexKit.Configuration.Core;

namespace FlexKitConfigurationConsoleApp.Services;

public class AzureTestService
{
    private readonly IFlexConfig _flexConfig;

    public AzureTestService(IFlexConfig flexConfig)
    {
        _flexConfig = flexConfig;
    }

    public void TestAzureKeyVaultIntegration()
    {
        Console.WriteLine("=== Testing Azure Key Vault Integration ===");

        // Test basic secrets loaded from Key Vault
        var dbHost = _flexConfig["flexkit:database:host"];
        var dbPort = _flexConfig["flexkit:database:port"];
        var dbUsername = _flexConfig["flexkit:database:username"];
        var dbPassword = _flexConfig["flexkit:database:password"];

        Console.WriteLine($"✓ Key Vault Secret - Database Host: {dbHost}");
        Console.WriteLine($"✓ Key Vault Secret - Database Port: {dbPort}");
        Console.WriteLine($"✓ Key Vault Secret - Database Username: {dbUsername}");
        Console.WriteLine($"✓ Key Vault Secret - Database Password: {(string.IsNullOrEmpty(dbPassword) ? "NOT LOADED" : "***LOADED***")}");

        // Test raw JSON secrets (should be flattened but aren't)
        var featuresConfigRaw = _flexConfig["flexkit:features:config"];
        var apiConfigRaw = _flexConfig["flexkit:api:config"];
        
        Console.WriteLine($"✓ Raw JSON Secret - Features Config: {featuresConfigRaw ?? "NOT LOADED"}");
        Console.WriteLine($"✓ Raw JSON Secret - API Config: {apiConfigRaw ?? "NOT LOADED"}");

        // Test flattened JSON keys (these should exist if JSON processing works)
        var cachingEnabled = _flexConfig["flexkit:features:config:caching:enabled"];
        var cachingTtl = _flexConfig["flexkit:features:config:caching:ttl"];
        var loggingLevel = _flexConfig["flexkit:features:config:logging:level"];
        var enableConsole = _flexConfig["flexkit:features:config:logging:enableConsole"];

        Console.WriteLine($"✓ Flattened JSON - Caching Enabled: {cachingEnabled ?? "NOT FLATTENED"}");
        Console.WriteLine($"✓ Flattened JSON - Caching TTL: {cachingTtl ?? "NOT FLATTENED"}");
        Console.WriteLine($"✓ Flattened JSON - Logging Level: {loggingLevel ?? "NOT FLATTENED"}");
        Console.WriteLine($"✓ Flattened JSON - Enable Console: {enableConsole ?? "NOT FLATTENED"}");

        var apiBaseUrl = _flexConfig["flexkit:api:config:baseUrl"];
        var apiTimeout = _flexConfig["flexkit:api:config:timeout"];
        var apiRetryCount = _flexConfig["flexkit:api:config:retryCount"];

        Console.WriteLine($"✓ Flattened JSON - API Base URL: {apiBaseUrl ?? "NOT FLATTENED"}");
        Console.WriteLine($"✓ Flattened JSON - API Timeout: {apiTimeout ?? "NOT FLATTENED"}");
        Console.WriteLine($"✓ Flattened JSON - API Retry Count: {apiRetryCount ?? "NOT FLATTENED"}");
        Console.WriteLine();
    }

    public void TestAzureAppConfigurationIntegration()
    {
        Console.WriteLine("=== Testing Azure App Configuration Integration ===");

        // Test basic App Configuration values (these keys exist)
        var appConfigDbHost = _flexConfig["flexkit:database:host"];
        var appConfigDbPort = _flexConfig["flexkit:database:port"];
        var appConfigApiUrl = _flexConfig["flexkit:api:baseUrl"];

        Console.WriteLine($"✓ App Config - Database Host: {appConfigDbHost ?? "NOT LOADED"}");
        Console.WriteLine($"✓ App Config - Database Port: {appConfigDbPort ?? "NOT LOADED"}");
        Console.WriteLine($"✓ App Config - API Base URL: {appConfigApiUrl ?? "NOT LOADED"}");

        // Test raw JSON App Configuration value
        var appConfigFeaturesRaw = _flexConfig["flexkit:appconfig:features"];
        Console.WriteLine($"✓ App Config Raw JSON: {appConfigFeaturesRaw ?? "NOT LOADED"}");
        
        // DEBUG: Let's test the JSON validation logic manually
        if (!string.IsNullOrEmpty(appConfigFeaturesRaw))
        {
            var isValidJson = appConfigFeaturesRaw.Trim().StartsWith('{') && appConfigFeaturesRaw.Trim().EndsWith('}');
            Console.WriteLine($"  -> JSON validation (basic check): {isValidJson}");
            Console.WriteLine($"  -> JSON length: {appConfigFeaturesRaw.Length}");
            Console.WriteLine($"  -> First 50 chars: {appConfigFeaturesRaw.Substring(0, Math.Min(50, appConfigFeaturesRaw.Length))}");
        }

        // Test flattened JSON App Configuration values (if JSON processing works)
        var enableNewUI = _flexConfig["flexkit:appconfig:features:featureFlags:enableNewUI"];
        var enableBeta = _flexConfig["flexkit:appconfig:features:featureFlags:enableBetaFeatures"];
        var maxUsers = _flexConfig["flexkit:appconfig:features:limits:maxUsers"];
        var maxRequests = _flexConfig["flexkit:appconfig:features:limits:maxRequests"];

        Console.WriteLine($"✓ Flattened JSON - Enable New UI: {enableNewUI ?? "NOT FLATTENED"}");
        Console.WriteLine($"✓ Flattened JSON - Enable Beta: {enableBeta ?? "NOT FLATTENED"}");
        Console.WriteLine($"✓ Flattened JSON - Max Users: {maxUsers ?? "NOT FLATTENED"}");
        Console.WriteLine($"✓ Flattened JSON - Max Requests: {maxRequests ?? "NOT FLATTENED"}");

        // Test labeled configuration (development label configured)
        var loggingLevel = _flexConfig["flexkit:logging:level"];
        Console.WriteLine($"✓ Labeled Config - Logging Level: {loggingLevel ?? "NOT LOADED"}");

        Console.WriteLine();
    }

    public void VerifyAzureIntegration()
    {
        // Verify Key Vault secrets are loaded
        var kvSecrets = new[]
        {
            "flexkit:database:host",
            "flexkit:database:port", 
            "flexkit:database:username",
            "flexkit:database:password"
        };

        foreach (var key in kvSecrets)
        {
            var value = _flexConfig[key];
            if (string.IsNullOrEmpty(value))
            {
                Console.WriteLine($"✗ FAILED: Key Vault secret '{key}' not loaded");
            }
        }

        // Verify JSON processing
        var jsonKeys = new[]
        {
            "flexkit:features:caching:enabled",
            "flexkit:api:baseUrl"
        };

        foreach (var key in jsonKeys)
        {
            var value = _flexConfig[key];
            if (string.IsNullOrEmpty(value))
            {
                Console.WriteLine($"✗ FAILED: JSON processing for '{key}' not working");
            }
        }

        // Verify App Configuration
        var appConfigKeys = new[]
        {
            "flexkit:database:host",
            "flexkit:logging:level"
        };

        foreach (var key in appConfigKeys)
        {
            var value = _flexConfig[key];
            if (string.IsNullOrEmpty(value))
            {
                Console.WriteLine($"✗ FAILED: App Configuration key '{key}' not loaded");
            }
        }
    }
}