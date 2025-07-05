# FlexKit.Configuration.Providers.Aws

AWS configuration providers for FlexKit.Configuration, enabling seamless integration with AWS Parameter Store and Secrets Manager. Store your application configuration securely in AWS while maintaining all FlexKit capabilities including dynamic access, strongly-typed binding, and automatic reloading.

## Features

- **🔐 AWS Parameter Store Integration**: Hierarchical configuration with String, StringList, and SecureString support
- **🛡️ AWS Secrets Manager Support**: Secure storage with automatic rotation, JSON processing, and binary data handling
- **📋 JSON Processing**: Automatic flattening of JSON parameters/secrets into configuration hierarchies
- **🔄 Automatic Reloading**: Periodic refresh of configuration data from AWS services
- **🎯 Selective Processing**: Apply JSON processing only to specific parameters/secrets for optimal performance
- **🔑 Secure by Default**: Uses AWS credential resolution chain and IAM for access control
- **⚡ FlexKit Integration**: Full compatibility with dynamic access, type conversion, and RegisterConfig
- **📦 Base64 Binary Support**: Handle certificates, keystores, and other binary secrets

## Installation

```bash
dotnet add package FlexKit.Configuration.Providers.Aws
```

## Quick Start

### Basic Parameter Store Usage

```csharp
using FlexKit.Configuration.Core;
using FlexKit.Configuration.Providers.Aws.Extensions;

var config = new FlexConfigurationBuilder()
    .AddAwsParameterStore("/myapp/")
    .Build();

// Dynamic access
dynamic settings = config;
var dbHost = settings.myapp.database.host;
var apiKey = settings.myapp.api.key;

// Direct access
var caching = config["myapp:features:caching"];
```

### Basic Secrets Manager Usage

```csharp
var config = new FlexConfigurationBuilder()
    .AddAwsSecretsManager(new[] { "myapp-database", "myapp-api-keys" })
    .Build();

// Dynamic access
dynamic secrets = config;
var dbPassword = secrets.myapp.database; // If it's a JSON secret

// Direct access
var apiKey = config["myapp:api:keys"];
```

### Advanced Configuration

```csharp
var config = new FlexConfigurationBuilder()
    .AddAwsParameterStore(options =>
    {
        options.Path = "/prod/myapp/";
        options.Optional = false; // Required in production
        options.JsonProcessor = true;
        options.JsonProcessorPaths = new[] 
        { 
            "/prod/myapp/database/",
            "/prod/myapp/cache/"
        };
        options.ReloadAfter = TimeSpan.FromMinutes(10);
        options.AwsOptions = new AWSOptions
        {
            Region = RegionEndpoint.USEast1,
            Profile = "production"
        };
        options.OnLoadException = ex => logger.LogError(ex, "Parameter Store failed");
    })
    .AddAwsSecretsManager(options =>
    {
        options.SecretNames = new[] { "prod-myapp/*" }; // Pattern loading
        options.Optional = false;
        options.JsonProcessor = true;
        options.VersionStage = "AWSCURRENT";
        options.ReloadAfter = TimeSpan.FromMinutes(15);
        options.OnLoadException = ex => logger.LogError(ex, "Secrets Manager failed");
    })
    .Build();
```

## Parameter Store Configuration

### Parameter Types

**String Parameters:**
```
/myapp/database/host = "localhost"
/myapp/database/port = "5432"
/myapp/features/caching = "true"
```

**StringList Parameters:**
```
/myapp/allowed-origins = "http://localhost:3000,https://app.com,https://admin.com"
```
Results in:
```
myapp:allowed-origins:0 = "http://localhost:3000"
myapp:allowed-origins:1 = "https://app.com"
myapp:allowed-origins:2 = "https://admin.com"
```

**SecureString Parameters (Encrypted):**
```
/myapp/database/password = "encrypted-password"  # Automatically decrypted
/myapp/api/secret = "encrypted-api-secret"
```

### JSON Parameter Processing

Store complex configuration as JSON in a single parameter:

```
# Parameter: /myapp/database/config
# Value: {"host": "localhost", "port": 5432, "ssl": true, "pool": {"min": 5, "max": 20}}
```

With `JsonProcessor = true`, this becomes:
```
myapp:database:config:host = "localhost"
myapp:database:config:port = "5432"
myapp:database:config:ssl = "true"
myapp:database:config:pool:min = "5"
myapp:database:config:pool:max = "20"
```

## Secrets Manager Configuration

### Secret Types

**JSON Secrets (Database Credentials):**
```
# Secret: myapp-database
# Value: {"host": "db.example.com", "port": 5432, "username": "app", "password": "secret123"}
```
Results in:
```
myapp:database:host = "db.example.com"
myapp:database:port = "5432"
myapp:database:username = "app"
myapp:database:password = "secret123"
```

**Simple String Secrets:**
```
# Secret: myapp-api-key
# Value: "secret-api-key-12345"
```
Results in:
```
myapp:api:key = "secret-api-key-12345"
```

**Binary Secrets (Certificates, Keystores):**
```
# Secret: myapp-ssl-certificate
# Value: [binary certificate data]
```
Results in:
```
myapp:ssl:certificate = "LS0tLS1CRUdJTiBDRVJUSUZJQ0FURS0tLS0t..." (Base64 encoded)
```

### Pattern-Based Loading

Load multiple related secrets with patterns:

```csharp
var config = new FlexConfigurationBuilder()
    .AddAwsSecretsManager(new[] { "myapp/*" }) // Loads all secrets starting with "myapp"
    .Build();

// Automatically loads:
// myapp-database → myapp:database
// myapp-cache → myapp:cache
// myapp-api-keys → myapp:api:keys
// myapp-certificates → myapp:certificates
```

### Version Control

Access specific versions of secrets:

```csharp
var config = new FlexConfigurationBuilder()
    .AddAwsSecretsManager(options =>
    {
        options.SecretNames = new[] { "myapp-database" };
        options.VersionStage = "AWSPREVIOUS"; // Rollback to previous version
    })
    .Build();

// Or use custom stages for gradual rollouts
options.VersionStage = "CANARY"; // Custom version stage
```

## Strongly-Typed Configuration

### With RegisterConfig (Parameter Store)

```csharp
// AWS Parameters:
// /myapp/database/Host = "localhost"
// /myapp/database/Port = "5432"
// /myapp/database/Ssl = "true"

public class DatabaseConfig
{
    public string Host { get; set; }
    public int Port { get; set; }
    public bool Ssl { get; set; }
}

// Register strongly-typed configuration
containerBuilder
    .AddFlexConfig(config => config
        .AddAwsParameterStore(options =>
        {
            options.Path = "/myapp/";
            options.JsonProcessor = false; // Simple key-value pairs
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

### With RegisterConfig (Secrets Manager JSON)

```csharp
// Secret: myapp-database (JSON format)
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
        .AddAwsSecretsManager(options =>
        {
            options.SecretNames = new[] { "myapp-database" };
            options.JsonProcessor = true; // Enable JSON flattening
        }))
    .RegisterConfig<DatabaseConfig>("myapp:database");

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

### Handling Binary Secrets

```csharp
// Secret: myapp-ssl-certificate (binary data)
public class SslConfig
{
    public string Certificate { get; set; } // Base64 encoded
}

containerBuilder.RegisterConfig<SslConfig>("myapp:ssl");

public class HttpsService
{
    public HttpsService(SslConfig sslConfig)
    {
        // Decode Base64 certificate data
        var certBytes = Convert.FromBase64String(sslConfig.Certificate);
        var certificate = new X509Certificate2(certBytes);
    }
}
```

## AWS Authentication

The providers automatically use the AWS credential resolution chain:

1. **Environment Variables**: `AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`, `AWS_SESSION_TOKEN`
2. **AWS Credentials File**: `~/.aws/credentials`
3. **IAM Instance Profile**: When running on EC2
4. **IAM Role for ECS Tasks**: When running on ECS
5. **IAM Role for Lambda**: When running on Lambda

### Custom AWS Configuration

```csharp
var config = new FlexConfigurationBuilder()
    .AddAwsParameterStore(options =>
    {
        options.Path = "/myapp/";
        options.AwsOptions = new AWSOptions
        {
            Region = RegionEndpoint.USWest2,
            Profile = "production" // Uses specific AWS profile
        };
    })
    .AddAwsSecretsManager(options =>
    {
        options.SecretNames = new[] { "myapp-secrets" };
        options.AwsOptions = new AWSOptions
        {
            Region = RegionEndpoint.USEast1, // Different region for secrets
            Profile = "secrets-profile"
        };
    })
    .Build();
```

## Performance Optimization

### Selective JSON Processing

Process JSON only for specific parameters/secrets:

```csharp
var config = new FlexConfigurationBuilder()
    .AddAwsParameterStore(options =>
    {
        options.Path = "/myapp/";
        options.JsonProcessor = true;
        options.JsonProcessorPaths = new[]
        {
            "/myapp/database/",  // Process database config as JSON
            "/myapp/cache/"      // Process cache config as JSON
            // Simple string parameters like /myapp/app/name remain as strings
        };
    })
    .AddAwsSecretsManager(options =>
    {
        options.SecretNames = new[] { "myapp/*" };
        options.JsonProcessor = true;
        options.JsonProcessorSecrets = new[]
        {
            "myapp-database",    // Process as JSON
            "myapp-cache"        // Process as JSON
            // myapp-api-key remains as simple string
        };
    })
    .Build();
```

### Automatic Reloading

Configure periodic refresh with appropriate intervals:

```csharp
var config = new FlexConfigurationBuilder()
    .AddAwsParameterStore(options =>
    {
        options.Path = "/myapp/";
        options.ReloadAfter = TimeSpan.FromMinutes(5); // Parameter Store reload
    })
    .AddAwsSecretsManager(options =>
    {
        options.SecretNames = new[] { "myapp-database" };
        options.ReloadAfter = TimeSpan.FromMinutes(15); // Secrets Manager reload (less frequent)
    })
    .Build();
```

**Recommended Reload Intervals:**

| Service | Secret Type | Development | Production |
|---------|-------------|-------------|------------|
| Parameter Store | Configuration | 1-2 minutes | 5-15 minutes |
| Secrets Manager | Database (auto-rotated) | 2-5 minutes | 10-30 minutes |
| Secrets Manager | API keys (manual) | 5-10 minutes | 30-60 minutes |
| Secrets Manager | Certificates | 1-4 hours | 4-24 hours |

## Error Handling

### Optional vs Required Sources

```csharp
// Optional sources (development/testing)
var config = new FlexConfigurationBuilder()
    .AddAwsParameterStore(options =>
    {
        options.Path = "/dev/myapp/";
        options.Optional = true; // Won't fail if parameters don't exist
        options.OnLoadException = ex => logger.LogWarning(ex, "Dev parameters unavailable");
    })
    .AddAwsSecretsManager(options =>
    {
        options.SecretNames = new[] { "dev-myapp-database" };
        options.Optional = true; // Won't fail if secret doesn't exist
        options.OnLoadException = ex => logger.LogWarning(ex, "Dev secrets unavailable");
    })
    .Build();

// Required sources (production)
var config = new FlexConfigurationBuilder()
    .AddAwsParameterStore(options =>
    {
        options.Path = "/prod/myapp/";
        options.Optional = false; // Will fail if parameters can't be loaded
    })
    .AddAwsSecretsManager(options =>
    {
        options.SecretNames = new[] { "prod-myapp-database" };
        options.Optional = false; // Will fail if secret can't be loaded
    })
    .Build();
```

### Custom Error Handling

```csharp
var config = new FlexConfigurationBuilder()
    .AddAwsParameterStore(options =>
    {
        options.Path = "/myapp/";
        options.Optional = true;
        options.OnLoadException = exception =>
        {
            // Log the error
            logger.LogError(exception.InnerException, 
                "Failed to load Parameter Store config from {Path}", 
                exception.Source);
            
            // Send metrics
            metrics.Increment("config.parameterstore.failures", 
                new[] { ("path", options.Path) });
        };
    })
    .AddAwsSecretsManager(options =>
    {
        options.SecretNames = new[] { "myapp-database", "myapp-cache" };
        options.Optional = true;
        options.OnLoadException = exception =>
        {
            // Log the error
            logger.LogError(exception.InnerException, 
                "Failed to load Secrets Manager config: {Secrets}", 
                string.Join(",", options.SecretNames));
            
            // Send alert for critical secrets
            if (options.SecretNames.Any(s => s.Contains("database")))
            {
                alertService.SendAlert("Critical Secret Loading Failed", exception.Message);
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
using FlexKit.Configuration.Providers.Aws.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{
    containerBuilder.AddFlexConfig(config => config
        .AddJsonFile("appsettings.json")
        .AddAwsParameterStore(options =>
        {
            options.Path = $"/{builder.Environment.EnvironmentName.ToLower()}/myapp/";
            options.Optional = builder.Environment.IsDevelopment();
            options.JsonProcessor = true;
            options.ReloadAfter = builder.Environment.IsDevelopment() 
                ? TimeSpan.FromMinutes(1) 
                : TimeSpan.FromMinutes(15);
        })
        .AddAwsSecretsManager(options =>
        {
            options.SecretNames = new[] { $"{builder.Environment.EnvironmentName.ToLower()}-myapp/*" };
            options.Optional = builder.Environment.IsDevelopment();
            options.JsonProcessor = true;
            options.ReloadAfter = TimeSpan.FromMinutes(builder.Environment.IsDevelopment() ? 2 : 20);
        })
        .AddEnvironmentVariables());
});

var app = builder.Build();
```

## Mixed Configuration Sources

Combine Parameter Store, Secrets Manager, and other sources:

```csharp
var config = new FlexConfigurationBuilder()
    // Base configuration
    .AddJsonFile("appsettings.json")
    
    // Non-sensitive configuration from Parameter Store
    .AddAwsParameterStore(options =>
    {
        options.Path = "/myapp/config/";
        options.JsonProcessor = true;
    })
    
    // Sensitive configuration from Secrets Manager
    .AddAwsSecretsManager(options =>
    {
        options.SecretNames = new[] { "myapp-database", "myapp-api-keys" };
        options.JsonProcessor = true;
    })
    
    // Environment variable overrides (highest precedence)
    .AddEnvironmentVariables()
    .Build();

// Configuration precedence (last wins):
// 1. appsettings.json (lowest)
// 2. Parameter Store
// 3. Secrets Manager  
// 4. Environment variables (highest)
```

## IAM Permissions

### Parameter Store Permissions

```json
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Effect": "Allow",
            "Action": [
                "ssm:GetParametersByPath",
                "ssm:GetParameter",
                "ssm:GetParameters"
            ],
            "Resource": [
                "arn:aws:ssm:us-east-1:123456789012:parameter/myapp/*"
            ]
        },
        {
            "Effect": "Allow",
            "Action": [
                "kms:Decrypt"
            ],
            "Resource": [
                "arn:aws:kms:us-east-1:123456789012:key/12345678-1234-1234-1234-123456789012"
            ]
        }
    ]
}
```

### Secrets Manager Permissions

```json
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Effect": "Allow",
            "Action": [
                "secretsmanager:GetSecretValue",
                "secretsmanager:ListSecrets"
            ],
            "Resource": [
                "arn:aws:secretsmanager:us-east-1:123456789012:secret:myapp-*"
            ]
        }
    ]
}
```

## Best Practices

### Parameter Organization

```
AWS Parameter Store:
/prod/myapp/
├── database/
│   ├── host
│   ├── port
│   └── config          # JSON parameter with complex config
├── cache/
│   ├── redis-url
│   └── config          # JSON parameter
├── api/
│   ├── base-url
│   └── timeout
└── features/
    ├── caching
    └── analytics

AWS Secrets Manager:
prod-myapp-database     # JSON: database credentials
prod-myapp-cache        # JSON: cache credentials  
prod-myapp-api-keys     # JSON: external API keys
prod-myapp-certificates # Binary: SSL certificates
```

### Environment Separation

Use environment-specific prefixes and names:

**Parameter Store:**
- Development: `/dev/myapp/`
- Staging: `/staging/myapp/`
- Production: `/prod/myapp/`

**Secrets Manager:**
- Development: `dev-myapp-*`
- Staging: `staging-myapp-*`
- Production: `prod-myapp-*`

### Security

1. **Use appropriate service for data type**:
    - **Parameter Store**: Non-sensitive configuration, feature flags
    - **Secrets Manager**: Passwords, API keys, certificates

2. **Use SecureString/encryption** for sensitive Parameter Store data
3. **Apply least-privilege IAM policies** with specific resource restrictions
4. **Use separate KMS keys** for different environments
5. **Audit access** using CloudTrail
6. **Enable automatic rotation** for Secrets Manager secrets where possible

### Cost Optimization

1. **Parameter Store**: Use Standard parameters (free tier) for most configuration
2. **Secrets Manager**: Group related secrets in JSON format to reduce API calls
3. **Set appropriate reload intervals** to balance freshness with cost
4. **Use pattern loading** to minimize individual API calls
5. **Apply selective JSON processing** to avoid unnecessary parsing overhead

This comprehensive AWS integration enables you to leverage both AWS Parameter Store and Secrets Manager while maintaining the familiar
FlexKit configuration experience!

### Environment Separation

Use environment-specific prefixes:
- Development: `/dev/myapp/`
- Staging: `/staging/myapp/`
- Production: `/prod/myapp/`

### Security

1. **Use SecureString** for sensitive data like passwords and API keys
2. **Apply least-privilege IAM policies** with specific parameter path restrictions
3. **Use separate KMS keys** for different environments
4. **Audit parameter access** using CloudTrail
5. **Rotate secrets regularly** using Secrets Manager for databases

### Cost Optimization

1. **Use Standard parameters** for most configuration (free tier available)
2. **Group related configuration** in JSON parameters to reduce API calls
3. **Set appropriate reload intervals** to balance freshness with cost
4. **Use hierarchical organization** to minimize parameter count

This provider enables you to leverage AWS's robust parameter management capabilities while maintaining the familiar FlexKit configuration experience!
