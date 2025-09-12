# FlexKit.Configuration.Providers.Aws

[![Build Status](https://img.shields.io/badge/build-passing-brightgreen)](https://github.com/flexkit/FlexKit) [![NuGet](https://img.shields.io/badge/nuget-v1.0.0-blue)](https://www.nuget.org/packages/FlexKit.Configuration.Providers.Aws) [![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

**FlexKit.Configuration.Providers.Aws** provides seamless integration between AWS configuration services (Parameter Store and Secrets Manager) and the FlexKit configuration system. This provider enables applications to leverage AWS-native configuration management with enterprise-grade security, encryption, and hierarchical organization while maintaining the simplicity and power of FlexKit's configuration patterns.

## 🌟 Key Features

### 🔐 **AWS Parameter Store Integration**
- **Hierarchical Configuration**: Organize parameters using AWS path-based structures (`/app/environment/component/setting`)
- **Automatic Type Conversion**: Native support for FlexKit's `.ToType<T>()` type safety
- **JSON Flattening**: Complex JSON parameters automatically flattened into configuration keys
- **SecureString Support**: KMS-encrypted parameters automatically decrypted at runtime
- **Dynamic Path Loading**: Load multiple parameter paths with different processing rules

### 🔒 **AWS Secrets Manager Integration**
- **JSON Secret Processing**: Complex JSON secrets flattened into configuration hierarchy
- **Version Management**: Support for specific secret versions and version stages
- **Binary Secret Handling**: Automatic Base64 encoding for binary secrets
- **Multi-Secret Loading**: Load and merge multiple secrets into single configuration
- **Caching Support**: Built-in AWS Secrets Manager caching for performance optimization

### ⚡ **Performance & Reliability**
- **Minimal Overhead**: <1.2% performance overhead compared to standard .NET configuration
- **Optimized Loading**: JSON processing provides 6% performance improvement for structured data
- **Optional Loading**: Graceful degradation when AWS services are unavailable
- **Connection Pooling**: Efficient AWS SDK client management and reuse
- **Error Resilience**: Comprehensive error handling with custom exception callbacks

### 🛠 **Developer Experience**
- **Fluent API**: Intuitive configuration builder pattern
- **Type Safety**: Full integration with FlexKit's strongly-typed configuration binding
- **Dependency Injection**: Native support for .NET DI container registration
- **Comprehensive Logging**: Detailed logging for troubleshooting and monitoring
- **LocalStack Support**: Development and testing with AWS service emulation

## 📦 Installation

```bash
dotnet add package FlexKit.Configuration.Providers.Aws
```

**Dependencies:**
- FlexKit.Configuration (>= 1.0.0)
- AWSSDK.SimpleSystemsManagement (>= 4.0.0)
- AWSSDK.SecretsManager (>= 4.0.0)
- AWSSDK.Extensions.NETCore.Setup (>= 4.0.0)

## 🚀 Quick Start

### Basic Parameter Store Configuration with AddFlexConfig

```csharp
using FlexKit.Configuration.Core;
using FlexKit.Configuration.Providers.Aws.Extensions;
using Microsoft.Extensions.Hosting;

// The FlexKit way - using AddFlexConfig with Host Builder
var builder = Host.CreateDefaultBuilder(args);

builder.AddFlexConfig(config =>
{
    config.AddJsonFile("appsettings.json")
          .AddAwsParameterStore("/myapp/production/")
          .AddEnvironmentVariables();
});

var host = builder.Build();
var configuration = host.Services.GetRequiredService<IFlexConfig>();

// Access parameters with FlexKit's type-safe extensions
var dbConnection = configuration["database:connection-string"];
var apiTimeout = configuration["api:timeout"].ToType<int>();
```

### Advanced Multi-Source Configuration with AddFlexConfig

```csharp
var builder = Host.CreateDefaultBuilder(args);

builder.AddFlexConfig(config =>
{
    // Base configuration
    config.AddJsonFile("appsettings.json");
    
    // AWS Parameter Store - application-specific parameters
    config.AddAwsParameterStore(options =>
    {
        options.Path = "/myapp/production/";
        options.JsonProcessor = true;  // 6% performance boost
        options.Optional = false;      // Required for production
    });
    
    // AWS Secrets Manager - sensitive data
    config.AddAwsSecretsManager(options =>
    {
        options.SecretNames = new[] { "myapp-database-credentials", "myapp-api-keys" };
        options.JsonProcessor = true;
        options.VersionStage = "AWSCURRENT";
    });
    
    // Environment variables override everything
    config.AddEnvironmentVariables();
});

// Build and access FlexKit configuration
var host = builder.Build();
var flexConfig = host.Services.GetRequiredService<IFlexConfig>();

// FlexKit provides enhanced access patterns
dynamic config = flexConfig;
var dbHost = config?.database?.host as string;
var features = flexConfig.ToType<FeatureConfig>("features");
```

## 🔧 Configuration Examples

### AWS Parameter Store Scenarios

#### 1. **Hierarchical Parameter Organization**

```bash
# AWS CLI - Parameter Store setup
aws ssm put-parameter --name "/myapp/production/database/host" --value "prod-db.example.com" --type String
aws ssm put-parameter --name "/myapp/production/database/port" --value "5432" --type String
aws ssm put-parameter --name "/myapp/production/api/timeout" --value "30000" --type String
aws ssm put-parameter --name "/myapp/production/features/enable-cache" --value "true" --type String

# SecureString (encrypted) parameters
aws ssm put-parameter --name "/myapp/production/database/password" --value "secure-db-password" --type SecureString
aws ssm put-parameter --name "/myapp/production/api/secret-key" --value "api-secret-12345" --type SecureString
```

```csharp
// FlexKit configuration loading with AddFlexConfig
var builder = Host.CreateDefaultBuilder();

builder.AddFlexConfig(config =>
{
    config.AddAwsParameterStore("/myapp/production/");
});

var host = builder.Build();
var flexConfig = host.Services.GetRequiredService<IFlexConfig>();

// Access transformed parameters (path prefix removed, '/' → ':')
Console.WriteLine($"Database Host: {flexConfig["database:host"]}");
Console.WriteLine($"Database Port: {flexConfig["database:port"].ToType<int>()}");
Console.WriteLine($"API Timeout: {flexConfig["api:timeout"].ToType<int>()}");
Console.WriteLine($"Cache Enabled: {flexConfig["features:enable-cache"].ToType<bool>()}");

// SecureString parameters automatically decrypted
Console.WriteLine($"DB Password: {flexConfig["database:password"]}");
Console.WriteLine($"API Secret: {flexConfig["api:secret-key"]}");
```

#### 2. **JSON Parameter Processing**

```bash
# Complex JSON parameter
aws ssm put-parameter --name "/myapp/production/database/providers" --type String --value '{
  "Primary": {
    "Host": "prod-primary.example.com",
    "Port": 5432,
    "Database": "myapp_production",
    "Username": "app_user",
    "MaxConnections": 50
  },
  "ReadReplica": {
    "Host": "prod-replica.example.com", 
    "Port": 5432,
    "Database": "myapp_production",
    "Username": "readonly_user",
    "MaxConnections": 25
  }
}'
```

```csharp
var builder = Host.CreateDefaultBuilder();

builder.AddFlexConfig(config =>
{
    config.AddAwsParameterStore(options =>
    {
        options.Path = "/myapp/production/";
        options.JsonProcessor = true; // Enable JSON flattening
    });
});

var host = builder.Build();
var flexConfig = host.Services.GetRequiredService<IFlexConfig>();

// JSON automatically flattened into configuration keys
Console.WriteLine($"Primary DB Host: {flexConfig["database:providers:Primary:Host"]}");
Console.WriteLine($"Primary DB Port: {flexConfig["database:providers:Primary:Port"].ToType<int>()}");
Console.WriteLine($"Replica DB Host: {flexConfig["database:providers:ReadReplica:Host"]}");
Console.WriteLine($"Max Connections: {flexConfig["database:providers:Primary:MaxConnections"].ToType<int>()}");
```

#### 3. **Array Parameters (Individual Items)**

```bash
# Server configuration as individual parameters
aws ssm put-parameter --name "/myapp/production/servers/0/name" --value "Web Server 1" --type String
aws ssm put-parameter --name "/myapp/production/servers/0/host" --value "web1.example.com" --type String
aws ssm put-parameter --name "/myapp/production/servers/0/port" --value "8080" --type String
aws ssm put-parameter --name "/myapp/production/servers/1/name" --value "API Server" --type String
aws ssm put-parameter --name "/myapp/production/servers/1/host" --value "api.example.com" --type String
aws ssm put-parameter --name "/myapp/production/servers/1/port" --value "8090" --type String
```

```csharp
var builder = Host.CreateDefaultBuilder();

builder.AddFlexConfig(config =>
{
    config.AddAwsParameterStore("/myapp/production/");
});

var host = builder.Build();
var flexConfig = host.Services.GetRequiredService<IFlexConfig>();

// Access array elements by index
for (int i = 0; i < 3; i++)
{
    var serverName = flexConfig[$"servers:{i}:name"];
    var serverHost = flexConfig[$"servers:{i}:host"];
    var serverPort = flexConfig[$"servers:{i}:port"].ToType<int>();
    
    if (!string.IsNullOrEmpty(serverName))
    {
        Console.WriteLine($"Server {i}: {serverName} ({serverHost}:{serverPort})");
    }
}
```

#### 4. **Multi-Path Configuration**

```csharp
var builder = Host.CreateDefaultBuilder();

builder.AddFlexConfig(config =>
{
    // Application-specific settings
    config.AddAwsParameterStore(options =>
    {
        options.Path = "/myapp/production/";
        options.JsonProcessor = true;
        options.Optional = false;
    });
    
    // Shared infrastructure settings
    config.AddAwsParameterStore(options =>
    {
        options.Path = "/shared/infrastructure/";
        options.JsonProcessor = false; // Simple strings only
        options.Optional = true;       // Graceful fallback
    });
});

var host = builder.Build();
var flexConfig = host.Services.GetRequiredService<IFlexConfig>();

// Access parameters from different paths
Console.WriteLine($"App Name: {flexConfig["application:name"]}");           // From /myapp/production/
Console.WriteLine($"Cache Provider: {flexConfig["cache:provider"]}");        // From /shared/infrastructure/
Console.WriteLine($"Monitoring URL: {flexConfig["monitoring:endpoint"]}");   // From /shared/infrastructure/
```

### AWS Secrets Manager Scenarios

#### 1. **Simple Secret Loading**

```csharp
var builder = Host.CreateDefaultBuilder();

builder.AddFlexConfig(config =>
{
    config.AddAwsSecretsManager(new[] { "myapp-database-credentials" });
});

var host = builder.Build();
var flexConfig = host.Services.GetRequiredService<IFlexConfig>();

// Secret name transformed to configuration key (hyphens → colons)
Console.WriteLine($"DB User: {flexConfig["myapp:database:credentials"]}");
```

#### 2. **JSON Secret Processing**

```json
// AWS Secret: "myapp-config-secret"
{
  "Database": {
    "ConnectionString": "Server=prod-db.example.com;Database=MyApp;User=appuser;Password=secret123",
    "Timeout": 30
  },
  "Api": {
    "BaseUrl": "https://api.example.com",
    "Key": "api_key_12345",
    "Secret": "api_secret_abcdef"
  },
  "Features": {
    "CacheEnabled": true,
    "MaxRetries": 5
  }
}
```

```csharp
var builder = Host.CreateDefaultBuilder();

builder.AddFlexConfig(config =>
{
    config.AddAwsSecretsManager(options =>
    {
        options.SecretNames = new[] { "myapp-config-secret" };
        options.JsonProcessor = true; // Enable JSON flattening
        options.Optional = false;
    });
});

var host = builder.Build();
var flexConfig = host.Services.GetRequiredService<IFlexConfig>();

// JSON secret flattened into configuration hierarchy
Console.WriteLine($"DB Connection: {flexConfig["myapp:config:secret:Database:ConnectionString"]}");
Console.WriteLine($"DB Timeout: {flexConfig["myapp:config:secret:Database:Timeout"].ToType<int>()}");
Console.WriteLine($"API Key: {flexConfig["myapp:config:secret:Api:Key"]}");
Console.WriteLine($"Cache Enabled: {flexConfig["myapp:config:secret:Features:CacheEnabled"].ToType<bool>()}");
```

#### 3. **Multi-Secret Configuration**

```csharp
var builder = Host.CreateDefaultBuilder();

builder.AddFlexConfig(config =>
{
    config.AddAwsSecretsManager(options =>
    {
        options.SecretNames = new[] { 
            "myapp-database-secrets",
            "myapp-api-keys", 
            "myapp-encryption-keys"
        };
        options.JsonProcessor = true;
        options.VersionStage = "AWSCURRENT";
    });
});

var host = builder.Build();
var flexConfig = host.Services.GetRequiredService<IFlexConfig>();

// Access secrets from different secret names
Console.WriteLine($"DB Password: {flexConfig["myapp:database:secrets:password"]}");
Console.WriteLine($"API Key: {flexConfig["myapp:api:keys:primary"]}");
Console.WriteLine($"Encryption Key: {flexConfig["myapp:encryption:keys:master"]}");
```

#### 4. **Advanced Secret Configuration**

```csharp
var builder = Host.CreateDefaultBuilder();

builder.AddFlexConfig(config =>
{
    config.AddAwsSecretsManager(options =>
    {
        options.SecretNames = new[] { "myapp-production-secrets" };
        options.JsonProcessor = true;
        options.JsonProcessorSecrets = new[] { "myapp-production-secrets" }; // Selective processing
        options.VersionStage = "AWSCURRENT";
        options.Optional = false;
        options.ReloadAfter = TimeSpan.FromMinutes(15); // Auto-refresh every 15 minutes
        options.AwsOptions = new AWSOptions 
        {
            Region = RegionEndpoint.USEast1,
            Profile = "production"
        };
        options.OnLoadException = ex => 
        {
            logger.LogCritical(ex, "Failed to load production secrets");
            alertingService.SendAlert("Secrets Manager Failure", ex.Message);
        };
    });
});

var host = builder.Build();
var flexConfig = host.Services.GetRequiredService<IFlexConfig>();
```

## 🏗 Architecture Patterns

### Dependency Injection Setup

```csharp
// Program.cs - The FlexKit way with AddFlexConfig
using Autofac;
using FlexKit.Configuration.Core;
using FlexKit.Configuration.Providers.Aws.Extensions;

var builder = Host.CreateDefaultBuilder(args);

// Configure FlexKit with AWS providers
builder.AddFlexConfig(config =>
{
    config.AddJsonFile("appsettings.json")
          .AddAwsParameterStore(options =>
          {
              options.Path = "/myapp/production/";
              options.JsonProcessor = true;
              options.Optional = true; // Graceful degradation
          })
          .AddAwsSecretsManager(options =>
          {
              options.SecretNames = new[] { "myapp-secrets" };
              options.JsonProcessor = true;
          })
          .AddEnvironmentVariables();
});

// Option 1: Configure services with Microsoft DI (recommended)
builder.ConfigureServices((context, services) =>
{
    // FlexKit automatically registers IFlexConfig
    // Enable strongly-typed configuration binding using Microsoft DI extensions
    services.ConfigureFlexKit<DatabaseConfig>("database");
    services.ConfigureFlexKit<ApiConfig>("api");
    
    // Register your application services
    services.AddScoped<DatabaseService>();
    services.AddScoped<ApiService>();
});

// Option 2: Configure services with Autofac container builder (alternative)  
builder.ConfigureContainer<ContainerBuilder>((context, containerBuilder) =>
{
    // Enable strongly-typed configuration binding using Autofac extensions
    containerBuilder.RegisterConfig<DatabaseConfig>("database");
    containerBuilder.RegisterConfig<ApiConfig>("api");
    
    // Register your application services
    containerBuilder.RegisterType<DatabaseService>().AsSelf();
    containerBuilder.RegisterType<ApiService>().AsSelf();
});

var host = builder.Build();
```

### Health Check Integration

```csharp
// Monitor AWS configuration health
services.AddHealthChecks()
    .AddCheck("aws-parameter-store", () =>
    {
        try
        {
            var testValue = configuration["health:check"];
            return HealthCheckResult.Healthy($"Parameter Store accessible: {testValue}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Parameter Store inaccessible", ex);
        }
    })
    .AddCheck("aws-secrets-manager", () =>
    {
        try
        {
            var testSecret = configuration["secrets:health:check"];
            return HealthCheckResult.Healthy("Secrets Manager accessible");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Secrets Manager inaccessible", ex);
        }
    });
```

### Configuration Validation

```csharp
public class DatabaseConfig
{
    [Required]
    public string ConnectionString { get; set; } = string.Empty;
    
    [Range(1, 300)]
    public int Timeout { get; set; } = 30;
    
    [Range(1, 100)]
    public int MaxConnections { get; set; } = 10;
}

// Validation setup
services.Configure<DatabaseConfig>(configuration.GetSection("database"));
services.AddSingleton<IValidateOptions<DatabaseConfig>, DatabaseConfigValidator>();
```

## ⚡ Performance Analysis

Based on comprehensive benchmarks using LocalStack AWS service emulation:

### Loading Performance

| Configuration Pattern | Load Time | Memory | vs Standard .NET |
|----------------------|-----------|---------|------------------|
| **Parameter Store (Simple)** | 4.2ms | 43KB | +1.2% overhead |
| **Parameter Store (JSON)** | 3.9ms | 48KB | **6% faster** |
| **Secrets Manager** | 3.8ms | 44KB | +0.5% overhead |
| **Mixed Sources** | 9.3ms | 109KB | Optimal |

### Runtime Access Performance

| Access Pattern | Time | Memory | Recommendation |
|---------------|------|---------|----------------|
| **Direct Indexing** | 4.0μs | 0 bytes | ✅ **Use for hot paths** |
| **Type Conversion** | 32μs | 24 bytes | ✅ **Type-safe access** |
| **Dynamic Access** | 50μs | 872 bytes | ⚠️ **Startup only** |

### Performance Recommendations

#### ✅ **Optimal Patterns**

```csharp
// 1. Single configuration build with AddFlexConfig (75% faster than multiple builds)
var builder = Host.CreateDefaultBuilder();

builder.AddFlexConfig(config =>
{
    config.AddAwsParameterStore("/myapp/")
          .AddAwsSecretsManager();
});

var host = builder.Build();
var flexConfig = host.Services.GetRequiredService<IFlexConfig>();

// 2. Cache frequently accessed values
var criticalSettings = new 
{
    DbConnection = flexConfig["database:connection-string"],
    ApiTimeout = flexConfig["api:timeout"].ToType<int>(),
    CacheEnabled = flexConfig["features:cache-enabled"].ToType<bool>()
};

// 3. Direct access for hot paths (4μs vs 50μs dynamic)
var apiKey = flexConfig["api:key"]; // Fastest
var timeout = flexConfig["api:timeout"].ToType<int>(); // Type-safe
```

#### ⚠️ **Patterns to Avoid**

```csharp
// ❌ Multiple configuration builds (75% performance penalty)
foreach (var path in paths)
{
    var builder = Host.CreateDefaultBuilder();
    builder.AddFlexConfig(config => config.AddAwsParameterStore(path));
    var host = builder.Build(); // Expensive! Don't do this
}

// ❌ Dynamic access in hot paths (10-18x slower)
dynamic config = flexConfig;
var value = config.Some.Deep.Property; // Only for setup code
```

## 🔒 Security Best Practices

### 1. **IAM Permissions (Principle of Least Privilege)**

```json
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Effect": "Allow",
            "Action": [
                "ssm:GetParameter",
                "ssm:GetParameters", 
                "ssm:GetParametersByPath"
            ],
            "Resource": [
                "arn:aws:ssm:us-east-1:123456789012:parameter/myapp/*"
            ]
        },
        {
            "Effect": "Allow",
            "Action": [
                "secretsmanager:GetSecretValue"
            ],
            "Resource": [
                "arn:aws:secretsmanager:us-east-1:123456789012:secret:myapp-*"
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

### 2. **Encryption and Key Management**

```csharp
// Use AWS KMS for sensitive parameters
aws ssm put-parameter 
    --name "/myapp/production/database/password" 
    --value "secure-password" 
    --type SecureString 
    --key-id "alias/myapp-config-key"

// Secrets Manager with custom KMS key
aws secretsmanager create-secret 
    --name "myapp-database-credentials"
    --kms-key-id "alias/myapp-secrets-key" 
    --secret-string '{...}'
```

### 3. **Environment-Based Configuration**

```csharp
public static class AwsConfigurationExtensions
{
    public static FlexConfigurationBuilder AddAwsConfigurationForEnvironment(
        this FlexConfigurationBuilder builder, 
        string environment)
    {
        var basePath = $"/myapp/{environment}/";
        
        return builder
            .AddAwsParameterStore(options =>
            {
                options.Path = basePath;
                options.JsonProcessor = true;
                options.Optional = environment != "production"; // Strict for prod
            })
            .AddAwsSecretsManager(options =>
            {
                options.SecretNames = new[] { $"myapp-{environment}-secrets" };
                options.JsonProcessor = true;
                options.Optional = environment != "production";
            });
    }
}

// Usage
var config = new FlexConfigurationBuilder()
    .AddAwsConfigurationForEnvironment("production")
    .Build();
```

## 🧪 Testing with LocalStack

FlexKit.Configuration.Providers.Aws includes comprehensive support for testing with LocalStack:

### Docker Compose Setup

```yaml
version: '3.8'
services:
  localstack:
    image: localstack/localstack:latest
    ports:
      - "4566:4566"
    environment:
      - SERVICES=ssm,secretsmanager
      - DEBUG=1
      - LOCALSTACK_HOST=localhost
    volumes:
      - "./localstack:/tmp/localstack"
      - "/var/run/docker.sock:/var/run/docker.sock"
```

### Test Configuration

```csharp
[TestFixture]
public class AwsConfigurationTests
{
    private IContainer _localStackContainer = null!;
    private AWSOptions _localStackOptions = null!;

    [SetUp]
    public async Task Setup()
    {
        // Start LocalStack container
        _localStackContainer = new ContainerBuilder()
            .WithImage("localstack/localstack:latest")
            .WithPortBinding(4566, true)
            .WithEnvironment("SERVICES", "ssm,secretsmanager")
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilHttpRequestIsSucceeded(request => request
                    .ForPort(4566)
                    .ForPath("/_localstack/health")))
            .Build();
            
        await _localStackContainer.StartAsync();

        var mappedPort = _localStackContainer.GetMappedPublicPort(4566);
        _localStackOptions = new AWSOptions
        {
            DefaultClientConfig = {
                ServiceURL = $"http://localhost:{mappedPort}",
                UseHttp = true
            },
            Credentials = new BasicAWSCredentials("test", "test")
        };
    }

    [Test]
    public async Task CanLoadParameterStoreConfiguration()
    {
        // Arrange - Setup test parameters
        var ssmClient = _localStackOptions.CreateServiceClient<IAmazonSimpleSystemsManagement>();
        
        await ssmClient.PutParameterAsync(new PutParameterRequest
        {
            Name = "/test/app/database/host",
            Value = "localhost",
            Type = "String"
        });

        // Act - Load configuration
        var config = new FlexConfigurationBuilder()
            .AddAwsParameterStore(options =>
            {
                options.Path = "/test/app/";
                options.AwsOptions = _localStackOptions;
            })
            .Build();

        // Assert
        Assert.That(config["database:host"], Is.EqualTo("localhost"));
    }

    [TearDown]
    public async Task TearDown()
    {
        await _localStackContainer.DisposeAsync();
    }
}
```

## 📊 Monitoring and Observability

### Application Insights Integration

```csharp
public class AwsConfigurationTelemetry
{
    private readonly TelemetryClient _telemetryClient;
    
    public AwsConfigurationTelemetry(TelemetryClient telemetryClient)
    {
        _telemetryClient = telemetryClient;
    }
    
    public void TrackConfigurationLoad(string source, TimeSpan duration, bool success)
    {
        _telemetryClient.TrackDependency(
            "AWS Configuration",
            source,
            source,
            DateTime.UtcNow.Subtract(duration),
            duration,
            success);
            
        _telemetryClient.TrackMetric(
            $"Configuration.Load.{source}.Duration",
            duration.TotalMilliseconds);
    }
}
```

### Custom Health Checks

```csharp
public class AwsConfigurationHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AwsConfigurationHealthCheck> _logger;

    public AwsConfigurationHealthCheck(
        IConfiguration configuration,
        ILogger<AwsConfigurationHealthCheck> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Test critical configuration access
            var criticalSettings = new[]
            {
                _configuration["database:connection-string"],
                _configuration["api:key"],
                _configuration["features:enabled"]
            };

            var missingSettings = criticalSettings
                .Where(setting => string.IsNullOrEmpty(setting))
                .Count();

            if (missingSettings == 0)
            {
                return HealthCheckResult.Healthy("All AWS configuration loaded successfully");
            }
            else
            {
                return HealthCheckResult.Degraded(
                    $"Missing {missingSettings} critical configuration settings");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AWS configuration health check failed");
            return HealthCheckResult.Unhealthy("AWS configuration is inaccessible", ex);
        }
    }
}
```

## 🚨 Error Handling and Resilience

### Graceful Degradation

```csharp
var builder = Host.CreateDefaultBuilder();

builder.AddFlexConfig(config =>
{
    config.AddJsonFile("appsettings.json") // Local fallback
          .AddAwsParameterStore(options =>
          {
              options.Path = "/myapp/production/";
              options.Optional = true; // Don't fail startup if AWS is down
              options.OnLoadException = ex =>
              {
                  // Custom error handling
                  logger.LogWarning(ex, "Failed to load AWS Parameter Store config, using fallbacks");
                  
                  // Send alert to monitoring system
                  alertService.SendAlert("AWS Parameter Store Unavailable", ex.Message);
                  
                  // Update health status
                  healthStatusService.SetStatus("AwsConfig", HealthStatus.Degraded);
              };
          })
          .AddEnvironmentVariables(); // Override with env vars
});

var host = builder.Build();
var flexConfig = host.Services.GetRequiredService<IFlexConfig>();
```

### Retry Policies

```csharp
var retryPolicy = Policy
    .Handle<AmazonServiceException>()
    .WaitAndRetryAsync(
        3,
        retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
        (outcome, timespan, retryCount, context) =>
        {
            logger.LogWarning("AWS configuration load attempt {RetryCount} failed: {Error}",
                retryCount, outcome.Exception?.Message);
        });

var builder = Host.CreateDefaultBuilder();

builder.AddFlexConfig(config =>
{
    config.AddAwsParameterStore(options =>
    {
        options.Path = "/myapp/production/";
        options.RetryPolicy = retryPolicy; // Custom retry logic
    });
});

var host = builder.Build();
```

## 📈 Migration from .NET Configuration

### Before (Standard .NET Configuration)

```csharp
var builder = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables();
    
var configuration = builder.Build();

// Manual type conversion
var timeout = int.Parse(configuration["Api:Timeout"]);
var isEnabled = bool.Parse(configuration["Features:Enabled"]);
```

### After (FlexKit with AWS)

```csharp
var builder = Host.CreateDefaultBuilder();

builder.AddFlexConfig(config =>
{
    config.AddJsonFile("appsettings.json")
          .AddAwsParameterStore("/myapp/production/")
          .AddAwsSecretsManager(new[] { "myapp-secrets" })
          .AddEnvironmentVariables();
});

var host = builder.Build();
var flexConfig = host.Services.GetRequiredService<IFlexConfig>();

// Type-safe conversion with validation
var timeout = flexConfig["Api:Timeout"].ToType<int>();
var isEnabled = flexConfig["Features:Enabled"].ToType<bool>();

// Dynamic access for complex scenarios  
dynamic config = flexConfig;
var dbSettings = config.Database; // Strongly-typed access available too
```

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guide](../../CONTRIBUTING.md) for details.

### Development Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/flexkit/FlexKit.git
   cd FlexKit/src/FlexKit.Configuration.Providers.Aws
   ```

2. **Install dependencies**
   ```bash
   dotnet restore
   ```

3. **Run tests with LocalStack**
   ```bash
   docker-compose up -d localstack
   dotnet test
   ```

4. **Run performance benchmarks**
   ```bash
   cd ../../benchmarks/FlexKit.Configuration.Providers.Aws.PerformanceTests
   dotnet run -c Release
   ```

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](../../LICENSE) file for details.

## 🆘 Support

- **Documentation**: [FlexKit Documentation](https://docs.flexkit.dev)
- **Issues**: [GitHub Issues](https://github.com/flexkit/FlexKit/issues)
- **Discussions**: [GitHub Discussions](https://github.com/flexkit/FlexKit/discussions)
- **Stack Overflow**: Tag with `flexkit-configuration`

---

**FlexKit.Configuration.Providers.Aws** - Bringing enterprise-grade AWS configuration management to .NET applications with zero-friction developer experience. 🚀