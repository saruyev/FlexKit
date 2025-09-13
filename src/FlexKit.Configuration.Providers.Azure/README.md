# FlexKit.Configuration.Providers.Azure

[![NuGet](https://img.shields.io/nuget/v/FlexKit.Configuration.Providers.Azure.svg)](https://www.nuget.org/packages/FlexKit.Configuration.Providers.Azure)
[![Downloads](https://img.shields.io/nuget/dt/FlexKit.Configuration.Providers.Azure.svg)](https://www.nuget.org/packages/FlexKit.Configuration.Providers.Azure)

Azure configuration providers for FlexKit, enabling seamless integration with Azure Key Vault and Azure App Configuration services. Store your application configuration and secrets securely in Azure while maintaining full compatibility with the FlexKit configuration system.

## Features

### 🔐 Azure Key Vault Integration
- **Secure secret management** - Store sensitive configuration data encrypted in Azure Key Vault
- **Hierarchical secret naming** - Transform Key Vault secret names (`my-app--database--host`) to .NET configuration keys (`my-app:database:host`)
- **JSON secret processing** - Automatically flatten complex JSON secrets into hierarchical configuration structure
- **Selective JSON processing** - Apply JSON processing only to specific secrets for optimal performance
- **Automatic reloading** - Periodically refresh secrets from Azure Key Vault
- **Azure credential integration** - Seamless authentication using Azure Identity and Managed Identity

### ⚙️ Azure App Configuration Integration
- **Centralized configuration management** - Store application settings in Azure App Configuration
- **Label-based environment management** - Use labels for environment-specific configuration (development, staging, production)
- **Key filtering** - Load only relevant configuration keys to reduce memory usage and improve performance
- **JSON configuration processing** - Automatically flatten JSON configuration values into hierarchical keys
- **Feature flag support** - Integrate with Azure App Configuration feature flags
- **Real-time configuration updates** - Automatic reloading with configurable intervals

### 🚀 Performance & Reliability
- **Optimized performance** - Static configuration access in 25-55ns, building in 1-3ms for App Configuration
- **Memory efficient** - Key filtering reduces memory usage by up to 88%
- **Optional source handling** - Graceful degradation when Azure services are unavailable
- **Comprehensive error handling** - Custom exception handling with detailed error information
- **Production-ready** - Battle-tested with comprehensive benchmarks and performance analysis

## Quick Start

### Installation

```bash
dotnet add package FlexKit.Configuration.Providers.Azure
```

### Basic Usage

```csharp
using FlexKit.Configuration.Core;
using FlexKit.Configuration.Providers.Azure.Extensions;

var builder = Host.CreateDefaultBuilder(args);

builder.AddFlexConfig(config =>
{
    .AddAzureKeyVault("https://myapp-vault.vault.azure.net/")
    .AddAzureAppConfiguration("https://myapp-config.azconfig.io")
    .Build();
}

var host = builder.Build();
var config = host.Services.GetRequiredService<IFlexConfig>();

// Access configuration values
var dbHost = config["myapp:database:host"];
var apiKey = config["myapp:api:key"];
```

### Advanced Configuration

```csharp
var builder = Host.CreateDefaultBuilder(args);

builder.AddFlexConfig(config =>
{
    // Azure Key Vault with JSON processing
    .AddAzureKeyVault(options =>
    {
        options.VaultUri = "https://production-vault.vault.azure.net/";
        options.Optional = false; // Required for production
        options.JsonProcessor = true;
        options.JsonProcessorSecrets = new[] { "database-config", "api-settings" };
        options.ReloadAfter = TimeSpan.FromMinutes(15);
    })
    // Azure App Configuration with labels and filtering
    .AddAzureAppConfiguration(options =>
    {
        options.ConnectionString = "https://production-config.azconfig.io";
        options.Optional = false;
        options.KeyFilter = "myapp:*"; // Load only myapp keys
        options.Label = "production"; // Environment-specific configuration
        options.JsonProcessor = true;
        options.ReloadAfter = TimeSpan.FromMinutes(5);
    }
}).Build();
```

## Configuration Examples

### Azure Key Vault Secrets

Create secrets in Azure Key Vault using the hierarchical naming convention:

```bash
# Basic secrets
az keyvault secret set --vault-name "myapp-vault" --name "myapp--database--host" --value "prod-db.example.com"
az keyvault secret set --vault-name "myapp-vault" --name "myapp--database--port" --value "5432"
az keyvault secret set --vault-name "myapp-vault" --name "myapp--api--key" --value "secret-api-key-12345"

# JSON secrets for complex configuration
az keyvault secret set --vault-name "myapp-vault" --name "myapp--features--config" \
    --value '{"caching": {"enabled": true, "ttl": 300}, "logging": {"level": "Information"}}'
```

**FlexKit Configuration Access:**
```csharp
// Basic secrets (automatically transformed from -- to :)
var dbHost = config["myapp:database:host"];        // "prod-db.example.com"
var dbPort = config["myapp:database:port"];        // "5432"
var apiKey = config["myapp:api:key"];              // "secret-api-key-12345"

// JSON secrets (automatically flattened when JsonProcessor = true)
var cachingEnabled = config["myapp:features:caching:enabled"]; // "true"
var cachingTtl = config["myapp:features:caching:ttl"];         // "300"
var loggingLevel = config["myapp:features:logging:level"];     // "Information"
```

### Azure App Configuration

Create configuration in Azure App Configuration:

```bash
# Basic configuration keys
az appconfig kv set --name "myapp-config" --key "myapp:database:host" --value "prod-db.example.com" --yes
az appconfig kv set --name "myapp-config" --key "myapp:api:baseurl" --value "https://api.production.com" --yes

# JSON configuration
az appconfig kv set --name "myapp-config" --key "myapp:features:config" \
    --value '{"featureFlags": {"enableNewUI": true, "enableBeta": false}, "limits": {"maxUsers": 1000}}' --yes

# Environment-specific configuration with labels
az appconfig kv set --name "myapp-config" --key "myapp:logging:level" --value "Debug" --label "development" --yes
az appconfig kv set --name "myapp-config" --key "myapp:logging:level" --value "Information" --label "production" --yes
```

**FlexKit Configuration Access:**
```csharp
// Basic configuration
var dbHost = config["myapp:database:host"];        // "prod-db.example.com"
var apiUrl = config["myapp:api:baseurl"];          // "https://api.production.com"

// JSON configuration (automatically flattened when JsonProcessor = true)
var enableNewUI = config["myapp:features:config:featureFlags:enableNewUI"]; // "true"
var maxUsers = config["myapp:features:config:limits:maxUsers"];             // "1000"

// Environment-specific (based on configured label)
var logLevel = config["myapp:logging:level"]; // "Information" (from production label)
```

## Integration with .NET Dependency Injection

### Strongly Typed Configuration

```csharp
public class DatabaseConfig
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class ApiConfig
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public int Timeout { get; set; }
}
```

**Configuration Setup:**
```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddFlexConfig(config =>
{
        config.AddAzureKeyVault("https://myapp-vault.vault.azure.net/")
        .AddAzureAppConfiguration("https://myapp-config.azconfig.io")
});

// Configure FlexKit with Azure providers
builder.ConfigureServices((context, services) =>
{
    // Register strongly typed configuration
    services.ConfigureFlexKit<DatabaseConfig>("myapp:database");
    services.ConfigureFlexKit<ApiConfig>("myapp:api");
});

var host = builder.Build();
```

**Usage in Services:**
```csharp
public class DatabaseService
{
    private readonly DatabaseConfig _config;

    public DatabaseService(IOptions<DatabaseConfig> config)
    {
        _config = config.Value;
    }

    public void Connect()
    {
        var connectionString = $"Host={_config.Host};Port={_config.Port};Username={_config.Username}";
        // Use configuration...
    }
}
```

## Performance Characteristics

Based on comprehensive benchmarks from [FlexKit.Configuration.Providers.Azure.PerformanceTests](../../benchmarks/FlexKit.Configuration.Providers.Azure.PerformanceTests/README.md):

### Configuration Building (Startup)
- **Azure App Configuration**: ~1.3-3.1ms initialization
- **Azure Key Vault**: ~135-158ms initialization (includes network security overhead)
- **Combined providers**: ~68-83ms (acceptable for production startup)

### Runtime Access Performance
- **Static access**: 25-55ns (excellent performance)
- **Dynamic access**: 1.3-57μs (50-2000x slower - use for non-critical paths only)

### Memory Optimization
- **Key filtering**: Reduces memory usage by up to 88%
- **Label filtering**: Reduces memory usage by up to 94%
- **Static configuration**: Zero allocation during runtime access

### Performance Recommendations

**✅ Recommended for High-Performance Applications:**
```csharp
// Strongly typed configuration (static access - 25-55ns)
services.Configure<DatabaseConfig>(config.GetSection("database"));

// Direct static access
var host = config["database:host"]; // 25-55ns
```

**⚠️ Use Carefully in Performance-Critical Paths:**
```csharp
// Dynamic access (1.3-57μs - 50-2000x slower)
var host = config.GetValue<string>("database:host");
```

## Security Best Practices

### Azure Key Vault
```csharp
.AddAzureKeyVault(options =>
{
    options.VaultUri = "https://production-vault.vault.azure.net/";
    options.Optional = false; // Fail fast if secrets unavailable
    options.Credential = new DefaultAzureCredential(); // Use Managed Identity in production
    options.JsonProcessor = true;
    options.JsonProcessorSecrets = new[] { "database-config" }; // Process only specific secrets
})
```

### Azure App Configuration
```csharp
.AddAzureAppConfiguration(options =>
{
    options.ConnectionString = "https://production-config.azconfig.io"; // Use endpoint + credential
    options.Credential = new DefaultAzureCredential(); // Avoid connection strings in production
    options.Label = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
    options.KeyFilter = "myapp:*"; // Load only application-specific keys
})
```

## Error Handling

### Optional Sources with Error Callbacks
```csharp
.AddAzureKeyVault(options =>
{
    options.VaultUri = "https://myapp-vault.vault.azure.net/";
    options.Optional = true; // Don't fail startup if unavailable
    options.OnLoadException = ex =>
    {
        logger.LogWarning(ex, "Failed to load Key Vault secrets - using fallback configuration");
        // Implement fallback logic or alerting
    };
})
```

### Required Sources for Critical Configuration
```csharp
.AddAzureAppConfiguration(options =>
{
    options.ConnectionString = "https://production-config.azconfig.io";
    options.Optional = false; // Fail startup if configuration unavailable
    // Application will not start if this configuration source fails
})
```

## Testing

### Unit Testing with Mock Clients
```csharp
public class ConfigurationTests
{
    [Test]
    public void TestAzureKeyVaultIntegration()
    {
        var mockSecretClient = new Mock<SecretClient>();
        
        var config = new FlexConfigurationBuilder()
            .AddAzureKeyVault(options =>
            {
                options.VaultUri = "https://test-vault.vault.azure.net/";
                options.SecretClient = mockSecretClient.Object; // Use mock for testing
                options.JsonProcessor = true;
            })
            .Build();

        // Test configuration access
        Assert.AreEqual("expected-value", config["test:key"]);
    }
}
```

### Integration Testing
```csharp
// Use test Azure resources for integration testing
var config = new FlexConfigurationBuilder()
    .AddAzureKeyVault("https://test-vault.vault.azure.net/")
    .AddAzureAppConfiguration("https://test-config.azconfig.io")
    .Build();

// Verify real Azure integration
var testValue = config["integration:test:value"];
Assert.IsNotNull(testValue);
```

## Migration Guide

### From Microsoft.Extensions.Configuration.AzureAppConfiguration
```csharp
// Before (Microsoft)
builder.Configuration.AddAzureAppConfiguration(options =>
{
    options.Connect(connectionString)
           .Select("myapp:*", "production")
           .ConfigureRefresh(refresh => refresh.Register("myapp:refresh", refreshAll: true));
});

// After (FlexKit)
builder.AddFlexConfig(config =>
{
    config.AddAzureAppConfiguration(options =>
    {
        options.ConnectionString = connectionString;
        options.KeyFilter = "myapp:*";
        options.Label = "production";
        options.ReloadAfter = TimeSpan.FromMinutes(5); // Automatic refresh
    });
});
```

### From Azure.Extensions.AspNetCore.Configuration.Secrets
```csharp
// Before (Microsoft)
builder.Configuration.AddAzureKeyVault(
    new Uri("https://myapp-vault.vault.azure.net/"), 
    new DefaultAzureCredential());

// After (FlexKit)
builder.AddFlexConfig(config =>
{
    config.AddAzureKeyVault(options =>
    {
        options.VaultUri = "https://myapp-vault.vault.azure.net/";
        options.Credential = new DefaultAzureCredential();
        options.JsonProcessor = true; // Additional FlexKit feature
    });
});
```

## Azure Resource Setup

### Azure Key Vault
```bash
# Create Key Vault
az keyvault create --name "myapp-vault" --resource-group "myapp-rg" --location "eastus"

# Set access policy for your application
az keyvault set-policy --name "myapp-vault" --object-id <app-object-id> --secret-permissions get list

# Create secrets
az keyvault secret set --vault-name "myapp-vault" --name "myapp--database--host" --value "prod-db.example.com"
```

### Azure App Configuration
```bash
# Create App Configuration
az appconfig create --name "myapp-config" --resource-group "myapp-rg" --location "eastus" --sku Free

# Assign access role
az role assignment create --assignee <app-object-id> --role "App Configuration Data Reader" --scope /subscriptions/<subscription-id>/resourceGroups/myapp-rg/providers/Microsoft.AppConfiguration/configurationStores/myapp-config

# Create configuration
az appconfig kv set --name "myapp-config" --key "myapp:database:host" --value "prod-db.example.com" --yes
```

## Contributing

We welcome contributions! Please see our [Contributing Guide](../../CONTRIBUTING.md) for details.

## License

This project is licensed under the MIT License - see the [LICENSE](../../LICENSE) file for details.

## Related Projects

- [FlexKit.Configuration](../FlexKit.Configuration/) - Core FlexKit configuration system
- [FlexKit.Configuration.Providers.Yaml](../FlexKit.Configuration.Providers.Yaml/) - YAML configuration provider
- [FlexKit.Configuration.Providers.Aws](../FlexKit.Configuration.Providers.Aws/) - AWS configuration providers
- [Performance Tests](../../benchmarks/FlexKit.Configuration.Providers.Azure.PerformanceTests/) - Comprehensive performance benchmarks
