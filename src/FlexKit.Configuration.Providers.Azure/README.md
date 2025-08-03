# FlexKit.Configuration.Providers.Azure

Azure configuration providers for FlexKit.Configuration, enabling seamless integration with Azure Key Vault and App Configuration. Store your application configuration securely in Azure while maintaining all FlexKit capabilities including dynamic access, strongly-typed binding, and automatic reloading.

## Features

- **🔐 Azure Key Vault Integration**: Secure secret storage with hierarchical organization and JSON processing
- **⚙️ Azure App Configuration Support**: Centralized configuration management with labels and key filtering
- **📋 JSON Processing**: Automatic flattening of JSON secrets into configuration hierarchies
- **🔄 Automatic Reloading**: Periodic refresh of configuration data from Azure services
- **🎯 Selective Processing**: Apply JSON processing only to specific secrets for optimal performance
- **🔑 Secure by Default**: Uses Azure credential resolution chain and Azure RBAC for access control
- **⚡ FlexKit Integration**: Full compatibility with dynamic access, type conversion, and RegisterConfig
- **🏷️ Label Support**: Environment and version-specific configuration with App Configuration labels

## Installation

```bash
dotnet add package FlexKit.Configuration.Providers.Azure
```

## Quick Start

### Basic Key Vault Usage

```csharp
using FlexKit.Configuration.Core;
using FlexKit.Configuration.Providers.Azure.Extensions;

var config = new FlexConfigurationBuilder()
    .AddAzureKeyVault("https://myapp-vault.vault.azure.net/")
    .Build();

// Dynamic access
dynamic settings = config;
var dbHost = settings.myapp.database.host; // From secret: myapp--database--host
var apiKey = settings.myapp.api.key;       // From secret: myapp--api--key

// Direct access
var connectionString = config["myapp:database:connectionstring"];
```

### Basic App Configuration Usage

```csharp
var config = new FlexConfigurationBuilder()
    .AddAzureAppConfiguration("https://myapp-config.azconfig.io")
    .Build();

// Dynamic access
dynamic settings = config;
var feature = settings.myapp.features.caching;

// Direct access
var apiTimeout = config["myapp:api:timeout"];
```

### Advanced Configuration

```csharp
var config = new FlexConfigurationBuilder()
    .AddAzureKeyVault(options =>
    {
        options.VaultUri = "https://prod-vault.vault.azure.net/";
        options.Optional = false; // Required in production
        options.JsonProcessor = true;
        options.JsonProcessorSecrets = new[] 
        { 
            "database-config",
            "cache-config"
        };
        options.ReloadAfter = TimeSpan.FromMinutes(15);
        options.Credential = new DefaultAzureCredential();
        options.OnLoadException = ex => logger.LogError(ex, "Key Vault failed");
    })
    .AddAzureAppConfiguration(options =>
    {
        options.ConnectionString = "https://prod-config.azconfig.io";
        options.Optional = false;
        options.KeyFilter = "myapp:*";
        options.Label = "production";
        options.ReloadAfter = TimeSpan.FromMinutes(5);
        options.OnLoadException = ex => logger.LogError(ex, "App Configuration failed");
    })
    .Build();
```

## Key Vault Configuration

### Secret Naming Convention

Key Vault uses double hyphens (`--`) to represent hierarchy:

```
# Key Vault secrets:
myapp--database--host = "localhost"
myapp--database--port = "5432"
myapp--features--caching = "true"
myapp--api--keys--external = "abc123"
```

Results in configuration keys:
```
myapp:database:host = "localhost"
myapp:database:port = "5432"
myapp:features:caching = "true"
myapp:api:keys:external = "abc123"
```

### JSON Secret Processing

Store complex configuration as JSON in a single secret:

```
# Secret: database-config
# Value: {"host": "localhost", "port": 5432, "ssl": true, "pool": {"min": 5, "max": 20}}
```

With `JsonProcessor = true`, this becomes:
```
database-config:host = "localhost"
database-config:port = "5432"
database-config:ssl = "true"
database-config:pool:min = "5"
database-config:pool:max = "20"
```

### Secret Types

**Simple String Secrets:**
```
# Secret: api-key
# Value: "secret-api-key-12345"
```
Results in:
```
api:key = "secret-api-key-12345"
```

**JSON Secrets (Database Credentials):**
```
# Secret: database-credentials
# Value: {"host": "db.example.com", "port": 5432, "username": "app", "password": "secret123"}
```
Results in:
```
database:credentials:host = "db.example.com"
database:credentials:port = "5432"
database:credentials:username = "app"
database:credentials:password = "secret123"
```

## App Configuration

### Key Filtering

Load only specific configuration keys:

```csharp
var config = new FlexConfigurationBuilder()
    .AddAzureAppConfiguration(options =>
    {
        options.ConnectionString = "https://myapp-config.azconfig.io";
        options.KeyFilter = "myapp:database:*"; // Only database-related keys
    })
    .Build();
```

### Label-Based Environment Management

Use labels for environment-specific configuration:

```csharp
// Production configuration
var config = new FlexConfigurationBuilder()
    .AddAzureAppConfiguration(options =>
    {
        options.ConnectionString = "https://myapp-config.azconfig.io";
        options.Label = "production";
        options.KeyFilter = "myapp:*";
    })
    .Build();

// Development configuration
var devConfig = new FlexConfigurationBuilder()
    .AddAzureAppConfiguration(options =>
    {
        options.ConnectionString = "https://myapp-config.azconfig.io";
        options.Label = "development";
        options.KeyFilter = "myapp:*";
    })
    .Build();
```

**App Configuration Structure:**
```
Key: myapp:database:host
├── Label: production → Value: "prod-db.example.com"
├── Label: development → Value: "dev-db.example.com"
└── Label: staging → Value: "staging-db.example.com"

Key: myapp:features:caching
├── Label: production → Value: "true"
├── Label: development → Value: "false"
└── Label: staging → Value: "true"
```

### Connection String vs Endpoint

**Full Connection String:**
```csharp
options.ConnectionString = "Endpoint=https://myapp-config.azconfig.io;Id=xxx;Secret=yyy";
```

**Endpoint with Credential:**
```csharp
options.ConnectionString = "https://myapp-config.azconfig.io";
options.Credential = new DefaultAzureCredential();
```

## Strongly-Typed Configuration

### With RegisterConfig (Key Vault JSON)

```csharp
// Secret: database-config (JSON format)
// {"host": "db.example.com", "port": 5432, "username": "app", "password": "secret", "pool": {"min": 5, "max": 20}}

public class DatabaseConfig
{
    public string Host { get; set; }
    public int Port { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public PoolConfig Pool { get; set; }
}

public class PoolConfig
{
    public int Min { get; set; }
    public int Max { get; set; }
}

// Register strongly-typed configuration
containerBuilder
    .AddFlexConfig(config => config
        .AddAzureKeyVault(options =>
        {
            options.VaultUri = "https://prod-vault.vault.azure.net/";
            options.JsonProcessor = true;
            options.JsonProcessorSecrets = new[] { "database-config" };
        }))
    .RegisterConfig<DatabaseConfig>("database:config");

// Use in services
public class DatabaseService
{
    public DatabaseService(DatabaseConfig dbConfig)
    {
        var connectionString = BuildConnectionString(dbConfig);
        // Configure connection pool with dbConfig.Pool.Min and dbConfig.Pool.Max
    }
}
```

### With RegisterConfig (App Configuration)

```csharp
// App Configuration:
// myapp:database:host = "localhost" (Label: production)
// myapp:database:port = "5432" (Label: production)
// myapp:database:ssl = "true" (Label: production)

public class DatabaseConfig
{
    public string Host { get; set; }
    public int Port { get; set; }
    public bool Ssl { get; set; }
}

// Register strongly-typed configuration
containerBuilder
    .AddFlexConfig(config => config
        .AddAzureAppConfiguration(options =>
        {
            options.ConnectionString = "https://prod-config.azconfig.io";
            options.Label = "production";
            options.KeyFilter = "myapp:database:*";
        }))
    .RegisterConfig<DatabaseConfig>("myapp:database");

// Use in services
public class DatabaseService
{
    public DatabaseService(DatabaseConfig dbConfig)
    {
        var connectionString = $"Host={dbConfig.Host};Port={dbConfig.Port};SSL={dbConfig.Ssl}";
    }
}
```

## Azure Authentication

The providers automatically use the Azure credential resolution chain:

1. **Environment Variables**: `AZURE_CLIENT_ID`, `AZURE_CLIENT_SECRET`, `AZURE_TENANT_ID`
2. **Managed Identity**: When running on Azure resources (VM, App Service, etc.)
3. **Azure CLI**: `az login` credentials
4. **Visual Studio**: Signed-in user credentials
5. **Azure PowerShell**: `Connect-AzAccount` credentials

### Custom Azure Configuration

```csharp
var config = new FlexConfigurationBuilder()
    .AddAzureKeyVault(options =>
    {
        options.VaultUri = "https://prod-vault.vault.azure.net/";
        options.Credential = new ClientSecretCredential(
            tenantId: "tenant-id",
            clientId: "client-id", 
            clientSecret: "client-secret");
    })
    .AddAzureAppConfiguration(options =>
    {
        options.ConnectionString = "https://prod-config.azconfig.io";
        options.Credential = new ManagedIdentityCredential(); // Use system-assigned identity
    })
    .Build();
```

## Performance Optimization

### Selective JSON Processing

Process JSON only for specific secrets:

```csharp
var config = new FlexConfigurationBuilder()
    .AddAzureKeyVault(options =>
    {
        options.VaultUri = "https://prod-vault.vault.azure.net/";
        options.JsonProcessor = true;
        options.JsonProcessorSecrets = new[]
        {
            "database-config",  // Process as JSON
            "cache-config"      // Process as JSON
            // api-key remains as simple string
        };
    })
    .Build();
```

### Automatic Reloading

Configure periodic refresh with appropriate intervals:

```csharp
var config = new FlexConfigurationBuilder()
    .AddAzureKeyVault(options =>
    {
        options.VaultUri = "https://prod-vault.vault.azure.net/";
        options.ReloadAfter = TimeSpan.FromMinutes(15); // Key Vault reload
    })
    .AddAzureAppConfiguration(options =>
    {
        options.ConnectionString = "https://prod-config.azconfig.io";
        options.ReloadAfter = TimeSpan.FromMinutes(5); // App Configuration reload
    })
    .Build();
```

**Recommended Reload Intervals:**

| Service | Configuration Type | Development | Production |
|---------|-------------------|-------------|------------|
| Key Vault | Secrets | 5-10 minutes | 15-30 minutes |
| App Configuration | Configuration | 1-2 minutes | 5-15 minutes |
| App Configuration | Feature flags | 30 seconds | 1-5 minutes |

## Error Handling

### Optional vs Required Sources

```csharp
// Optional sources (development/testing)
var config = new FlexConfigurationBuilder()
    .AddAzureKeyVault(options =>
    {
        options.VaultUri = "https://dev-vault.vault.azure.net/";
        options.Optional = true; // Won't fail if vault is unavailable
        options.OnLoadException = ex => logger.LogWarning(ex, "Dev Key Vault unavailable");
    })
    .AddAzureAppConfiguration(options =>
    {
        options.ConnectionString = "https://dev-config.azconfig.io";
        options.Optional = true; // Won't fail if App Configuration is unavailable
        options.OnLoadException = ex => logger.LogWarning(ex, "Dev App Configuration unavailable");
    })
    .Build();

// Required sources (production)
var config = new FlexConfigurationBuilder()
    .AddAzureKeyVault(options =>
    {
        options.VaultUri = "https://prod-vault.vault.azure.net/";
        options.Optional = false; // Will fail if vault can't be accessed
    })
    .AddAzureAppConfiguration(options =>
    {
        options.ConnectionString = "https://prod-config.azconfig.io";
        options.Optional = false; // Will fail if App Configuration can't be accessed
    })
    .Build();
```

### Custom Error Handling

```csharp
var config = new FlexConfigurationBuilder()
    .AddAzureKeyVault(options =>
    {
        options.VaultUri = "https://prod-vault.vault.azure.net/";
        options.Optional = true;
        options.OnLoadException = exception =>
        {
            // Log the error
            logger.LogError(exception.InnerException, 
                "Failed to load Key Vault secrets from {VaultUri}", 
                exception.Source);
            
            // Send metrics
            metrics.Increment("config.keyvault.failures", 
                new[] { ("vault", options.VaultUri) });
        };
    })
    .AddAzureAppConfiguration(options =>
    {
        options.ConnectionString = "https://prod-config.azconfig.io";
        options.Optional = true;
        options.OnLoadException = exception =>
        {
            // Log the error
            logger.LogError(exception.InnerException, 
                "Failed to load App Configuration from {ConnectionString}", 
                exception.Source);
            
            // Send alert for production failures
            if (options.Label == "production")
            {
                alertService.SendAlert("Production Configuration Loading Failed", exception.Message);
            }
        };
    })
    .Build();
```

## Integration with ASP.NET Core

```csharp
using Autofac;
using Autofac.Extensions.DependencyInjection;
using FlexKit.Configuration.Core;
using FlexKit.Configuration.Providers.Azure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{
    containerBuilder.AddFlexConfig(config => config
        .AddJsonFile("appsettings.json")
        .AddAzureKeyVault(options =>
        {
            options.VaultUri = $"https://{builder.Environment.EnvironmentName.ToLower()}-myapp-vault.vault.azure.net/";
            options.Optional = builder.Environment.IsDevelopment();
            options.JsonProcessor = true;
            options.ReloadAfter = builder.Environment.IsDevelopment() 
                ? TimeSpan.FromMinutes(5) 
                : TimeSpan.FromMinutes(30);
        })
        .AddAzureAppConfiguration(options =>
        {
            options.ConnectionString = "https://myapp-config.azconfig.io";
            options.Label = builder.Environment.EnvironmentName.ToLower();
            options.KeyFilter = "myapp:*";
            options.Optional = builder.Environment.IsDevelopment();
            options.ReloadAfter = TimeSpan.FromMinutes(builder.Environment.IsDevelopment() ? 1 : 10);
        })
        .AddEnvironmentVariables());
});

var app = builder.Build();
```

## Mixed Configuration Sources

Combine Key Vault, App Configuration, and other sources:

```csharp
var config = new FlexConfigurationBuilder()
    // Base configuration
    .AddJsonFile("appsettings.json")
    
    // Non-sensitive configuration from App Configuration
    .AddAzureAppConfiguration(options =>
    {
        options.ConnectionString = "https://myapp-config.azconfig.io";
        options.Label = "production";
        options.KeyFilter = "myapp:*";
    })
    
    // Sensitive configuration from Key Vault
    .AddAzureKeyVault(options =>
    {
        options.VaultUri = "https://prod-vault.vault.azure.net/";
        options.JsonProcessor = true;
    })
    
    // Environment variable overrides (highest precedence)
    .AddEnvironmentVariables()
    .Build();

// Configuration precedence (last wins):
// 1. appsettings.json (lowest)
// 2. App Configuration
// 3. Key Vault  
// 4. Environment variables (highest)
```

## Azure RBAC Permissions

### Key Vault Permissions

Assign the **Key Vault Secrets User** role or create a custom role:

```json
{
    "properties": {
        "roleName": "Key Vault Configuration Reader",
        "description": "Read secrets for application configuration",
        "permissions": [
            {
                "actions": [],
                "notActions": [],
                "dataActions": [
                    "Microsoft.KeyVault/vaults/secrets/getSecret/action"
                ],
                "notDataActions": []
            }
        ],
        "assignableScopes": [
            "/subscriptions/{subscription-id}/resourceGroups/{resource-group}/providers/Microsoft.KeyVault/vaults/{vault-name}"
        ]
    }
}
```

### App Configuration Permissions

Assign the **App Configuration Data Reader** role or create a custom role:

```json
{
    "properties": {
        "roleName": "App Configuration Reader",
        "description": "Read configuration data from App Configuration",
        "permissions": [
            {
                "actions": [],
                "notActions": [],
                "dataActions": [
                    "Microsoft.AppConfiguration/configurationStores/*/read"
                ],
                "notDataActions": []
            }
        ],
        "assignableScopes": [
            "/subscriptions/{subscription-id}/resourceGroups/{resource-group}/providers/Microsoft.AppConfiguration/configurationStores/{store-name}"
        ]
    }
}
```

## Best Practices

### Secret and Configuration Organization

**Key Vault Secrets:**
```
prod-vault.vault.azure.net:
├── database-config          # JSON: database credentials and settings
├── cache-config            # JSON: Redis/cache configuration
├── api-keys-external       # JSON: third-party API keys
├── certificates-ssl        # Binary: SSL certificates
└── encryption-keys         # Simple: application encryption keys
```

**App Configuration:**
```
myapp-config.azconfig.io:
myapp:database:timeout = "30" (Label: production)
myapp:database:pool-size = "20" (Label: production)
myapp:features:caching = "true" (Label: production)
myapp:features:analytics = "false" (Label: development)
myapp:api:base-url = "https://api.prod.com" (Label: production)
myapp:api:base-url = "https://api.dev.com" (Label: development)
```

### Environment Separation

Use environment-specific resources:

**Key Vault:**
- Development: `dev-myapp-vault.vault.azure.net`
- Staging: `staging-myapp-vault.vault.azure.net`
- Production: `prod-myapp-vault.vault.azure.net`

**App Configuration:**
- Development: Use labels (`development`, `dev`)
- Staging: Use labels (`staging`, `test`)
- Production: Use labels (`production`, `prod`)

### Security

1. **Use appropriate service for data type**:
    - **Key Vault**: Passwords, API keys, certificates, encryption keys
    - **App Configuration**: Non-sensitive configuration, feature flags, connection strings (non-sensitive parts)

2. **Apply least-privilege RBAC permissions** with specific resource scopes
3. **Use Managed Identity** when running on Azure resources
4. **Separate vaults/stores** for different environments and applications
5. **Enable diagnostic logging** for audit trails
6. **Use private endpoints** for enhanced security in production

### Cost Optimization

1. **Key Vault**: 
   - Group related secrets in JSON format to reduce API calls
   - Use appropriate secret renewal periods
   - Consider Key Vault pricing tiers for high-volume scenarios

2. **App Configuration**: 
   - Use key filtering to minimize data transfer
   - Set appropriate reload intervals to balance freshness with cost
   - Leverage the free tier for development/testing

3. **Set reasonable reload intervals** to balance configuration freshness with API costs
4. **Use selective JSON processing** to avoid unnecessary parsing overhead
5. **Implement proper caching** in application layers when possible

This comprehensive Azure integration enables you to leverage both Azure Key Vault and App Configuration while maintaining the familiar FlexKit configuration experience!