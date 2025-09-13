# FlexKit.Configuration

**Modern, flexible configuration management for .NET applications with dynamic access, multiple sources, and zero-ceremony setup.**

[![NuGet Version](https://img.shields.io/nuget/v/FlexKit.Configuration)](https://www.nuget.org/packages/FlexKit.Configuration)
[![Downloads](https://img.shields.io/nuget/dt/FlexKit.Configuration)](https://www.nuget.org/packages/FlexKit.Configuration)

## Why FlexKit.Configuration?

FlexKit.Configuration follows the **"Install and Use"** philosophy - minimal configuration, maximum productivity. While standard .NET configuration is powerful, it requires verbose setup and lacks modern developer experience features.

### The Problem with Standard Configuration

```csharp
// Standard .NET Configuration - Verbose and Error-Prone
var connectionString = configuration["Database:ConnectionString"];
var timeout = int.Parse(configuration["Database:CommandTimeout"] ?? "30");
var enableLogging = bool.Parse(configuration["Api:EnableLogging"] ?? "false");

// Type safety? Manual binding required
var dbConfig = new DatabaseConfig();
configuration.GetSection("Database").Bind(dbConfig);
```

### The FlexKit.Configuration Solution

```csharp
// FlexKit.Configuration - Clean and Intuitive
dynamic config = flexConfig;
var connectionString = config.Database.ConnectionString;
var timeout = config.Database.CommandTimeout.ToType<int>();
var enableLogging = config.Api.EnableLogging.ToType<bool>();

// Or strongly typed with automatic registration
public DatabaseService(DatabaseConfig dbConfig) { }
```

## Core Philosophy

- **🎯 Zero Ceremony**: One line to add, instant productivity
- **🚀 Developer First**: Natural syntax, intuitive access patterns  
- **⚡ Performance Aware**: Optimized runtime, willing to pay startup costs
- **🔧 Flexible Sources**: JSON, .env, environment variables, in-memory, custom sources
- **🏗️ Modern .NET**: Built for .NET 9+, async-first, nullable-aware

## Quick Start

### 1. Installation

```bash
dotnet add package FlexKit.Configuration
```

### 2. Basic Setup (Generic Host)

```csharp
var builder = Host.CreateDefaultBuilder(args);

// One line adds FlexKit.Configuration with all features
builder.AddFlexConfig(config => 
{
    config.AddDotEnvFile(".env", optional: true);
});

var host = builder.Build();

// Use dynamic configuration access
var flexConfig = host.Services.GetRequiredService<IFlexConfig>();
dynamic config = flexConfig;

var apiKey = config.Api.Key;           // Natural property access
var timeout = config.Api.Timeout.ToType<int>(); // Built-in type conversion
```

### 3. ASP.NET Core Setup

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddFlexConfig(config => 
{
    config.AddDotEnvFile(".env", optional: true)
          .AddYamlFile("features.yaml", optional: true);
});

var app = builder.Build();
```

## Features Overview

### ✨ Dynamic Configuration Access

Access configuration with natural property syntax:

```csharp
// Traditional approach
var host = configuration["Database:Providers:Primary:Host"];
var port = int.Parse(configuration["Database:Providers:Primary:Port"] ?? "5432");

// FlexKit approach
dynamic config = flexConfig;
var host = config.Database.Providers.Primary.Host;
var port = config.Database.Providers.Primary.Port.ToType<int>();
```

### 🎯 Multiple Configuration Sources

Support for all major configuration sources with precedence control:

```csharp
builder.AddFlexConfig(config =>
{
    // Sources are added in precedence order (later overrides earlier)
    config.AddJsonFile("appsettings.json")                    // Base settings
          .AddJsonFile($"appsettings.{env}.json", true)       // Environment overrides  
          .AddDotEnvFile(".env", optional: true)              // Local development
          .AddEnvironmentVariables()                          // Container/deployment
          .AddSource(customSource);                           // Custom sources
});
```

### 🔄 Seamless Type Conversion

Built-in type conversion with culture-invariant parsing:

```csharp
// All conversions are safe and culture-invariant
var port = config["Server:Port"].ToType<int>();
var timeout = config["Api:Timeout"].ToType<TimeSpan>();
var isEnabled = config["Features:NewUI"].ToType<bool>();
var retryCount = config["Resilience:MaxRetries"].ToType<int?>();

// Collection support
var allowedHosts = config["Security:AllowedHosts"].GetCollection<string>();
var ports = config["LoadBalancer:Ports"].GetCollection<int>(',');
```

### 🏗️ Automatic Dependency Injection

Strongly typed configuration with zero boilerplate:

```csharp
// Configuration classes
public class DatabaseConfig
{
    public string ConnectionString { get; set; } = "";
    public int CommandTimeout { get; set; } = 30;
    public Dictionary<string, DatabaseProvider> Providers { get; set; } = new();
}

// Automatic registration and injection
builder.AddFlexConfig(config => { /* sources */ });

// Services automatically receive strongly typed config
public class DatabaseService(DatabaseConfig dbConfig)
{
    public async Task<bool> TestConnection()
    {
        // Use dbConfig.ConnectionString, dbConfig.CommandTimeout, etc.
        return await ConnectAsync(dbConfig.ConnectionString, dbConfig.CommandTimeout);
    }
}
```

### 📦 Assembly Scanning & Module Discovery

Automatic discovery and registration of Autofac modules:

```csharp
// appsettings.json
{
  "Application": {
    "Mapping": {
      "Prefix": "MyCompany"  // Auto-discover MyCompany.* assemblies
    }
  }
}

// Modules are automatically discovered and registered
public class ServicesModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<DatabaseService>().AsImplementedInterfaces();
    }
}
```

### 🎛️ Flexible Access Patterns

Choose the right access pattern for your needs:

```csharp
public class ConfigurationService(IFlexConfig flexConfig, DatabaseConfig dbConfig)
{
    public void DemonstratePatterns()
    {
        // 1. Direct key access (fastest)
        var connectionString = flexConfig["Database:ConnectionString"];
        
        // 2. Dynamic access (most readable)
        dynamic config = flexConfig;
        var apiKey = config.Api.Key;
        
        // 3. Strongly typed (type safe)
        var timeout = dbConfig.CommandTimeout;
        
        // 4. Indexed access for arrays
        var firstServer = flexConfig[0]; // Gets Servers[0]
        dynamic server = firstServer;
        var serverName = server?.Name;
    }
}
```

## Configuration Sources

### JSON Files (appsettings.json)

```json
{
  "Database": {
    "ConnectionString": "Server=localhost;Database=MyApp;",
    "CommandTimeout": 30,
    "Providers": {
      "Primary": { "Host": "localhost", "Port": 5432 },
      "Secondary": { "Host": "backup.example.com", "Port": 5433 }
    }
  },
  "Api": {
    "BaseUrl": "https://api.example.com",
    "Features": {
      "Caching": true,
      "Authentication": { "Type": "Bearer", "TokenExpiry": 3600 }
    }
  },
  "Servers": [
    { "Name": "Web-1", "Host": "web1.example.com", "Port": 8080 },
    { "Name": "Web-2", "Host": "web2.example.com", "Port": 8081 }
  ]
}
```

### Environment Variables

```bash
# Hierarchical keys use double underscores
DATABASE__CONNECTIONSTRING="Server=prod;Database=MyApp;"
API__FEATURES__CACHING=false
SERVERS__0__PORT=9090
```

### .env Files

```bash
# .env file - perfect for local development
DATABASE_CONNECTIONSTRING=Server=dev;Database=MyApp_Dev;
API_APIKEY=dev-secret-key-12345
API_BASEURL=https://dev-api.example.com

# Feature flags
FEATURES_NEWUI=true
DEBUG_MODE=true
```

### In-Memory Sources

```csharp
config.AddSource(new MemoryConfigurationSource
{
    InitialData = new Dictionary<string, string?>
    {
        ["Testing:Mode"] = "true",
        ["Database:ConnectionString"] = "InMemory",
        ["Cache:Provider"] = "Memory"
    }
});
```

## Advanced Usage

### Property Injection by Convention

```csharp
public class BackgroundWorker
{
    // Automatically injected by Autofac convention
    public IFlexConfig? FlexConfiguration { get; set; }

    public async Task ProcessAsync()
    {
        if (FlexConfiguration != null)
        {
            dynamic config = FlexConfiguration;
            var batchSize = config.Processing.BatchSize.ToType<int>();
            var interval = config.Processing.IntervalMs.ToType<int>();
            
            // Process with configuration...
        }
    }
}

// Enable property injection
builder.RegisterType<BackgroundWorker>()
       .PropertiesAutowired()
       .SingleInstance();
```

### Custom Configuration Sources

```csharp
public class DatabaseConfigurationSource : IConfigurationSource
{
    private readonly string _connectionString;
    
    public DatabaseConfigurationSource(string connectionString)
    {
        _connectionString = connectionString;
    }
    
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new DatabaseConfigurationProvider(_connectionString);
    }
}

// Usage
config.AddSource(new DatabaseConfigurationSource(connectionString));
```

### Container Builder Integration

```csharp
// Advanced container configuration
builder.AddFlexConfig((configBuilder, containerBuilder) =>
{
    // Configure FlexKit sources
    configBuilder.AddDotEnvFile(".env", optional: true)
                 .AddYamlFile("services.yaml", optional: true);

    // Register additional services
    containerBuilder.RegisterType<CustomService>().AsImplementedInterfaces();
    containerBuilder.RegisterModule<ThirdPartyModule>();
});
```

## Performance Considerations

FlexKit.Configuration is designed with the philosophy: **"Pay startup costs, not runtime costs."**

### Performance Characteristics

Based on comprehensive benchmarks (see `/benchmarks/FlexKit.Configuration.PerformanceTests/`):

| Access Pattern | Performance | Memory | Use When |
|---|---|---|---|
| **Direct Key Access** | Baseline + 10% | Minimal | Frequently accessed config |
| **Dynamic Access** | Baseline + 260% | 120B per call | Readable, occasional access |
| **Strongly Typed** | Baseline + 5% | None | Type safety required |

### Recommendations

#### ✅ Use Dynamic Access When:
- Configuration is accessed infrequently (startup, initialization)
- Code readability and maintainability are priorities
- Rapid development and prototyping
- The natural syntax improves team productivity

```csharp
// Perfect for startup configuration
public async Task<IHost> ConfigureApplicationAsync()
{
    dynamic config = flexConfig;
    
    var dbConnection = config.Database.ConnectionString;
    var apiSettings = new ApiSettings
    {
        BaseUrl = config.Api.BaseUrl,
        Timeout = config.Api.Timeout.ToType<TimeSpan>(),
        RetryCount = config.Api.RetryCount.ToType<int>()
    };
    
    return configuredHost;
}
```

#### ⚡ Use Direct Key Access When:
- Configuration is accessed in hot code paths
- Memory allocation must be minimized
- Maximum performance is required

```csharp
// Perfect for frequently called methods
public async Task<HttpResponseMessage> CallApiAsync()
{
    // Direct access in hot path - no allocations
    var timeout = _flexConfig["Api:Timeout"].ToType<int>();
    var baseUrl = _flexConfig["Api:BaseUrl"];
    
    using var httpClient = CreateClient(baseUrl, timeout);
    return await httpClient.GetAsync(endpoint);
}
```

#### 🛡️ Use Strongly Typed When:
- Type safety and compile-time validation required
- Integration with existing DI containers
- Complex configuration objects

```csharp
public class PaymentService(PaymentConfig paymentConfig, ILogger logger)
{
    public async Task ProcessPaymentAsync(Payment payment)
    {
        // Strongly typed - compile time safety
        var provider = paymentConfig.Provider;
        var timeout = paymentConfig.Timeout;
        var retryCount = paymentConfig.RetryAttempts;
        
        // Implementation...
    }
}
```

## Best Practices

### 🏗️ Application Architecture

```csharp
// ✅ Configure once at startup
public static async Task Main(string[] args)
{
    var builder = Host.CreateDefaultBuilder(args);
    
    builder.AddFlexConfig(config =>
    {
        // Precedence: later sources override earlier ones
        config.AddJsonFile("appsettings.json")
              .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", true)
              .AddDotEnvFile(".env", optional: true)
              .AddEnvironmentVariables();
    });
    
    var host = builder.Build();
    await host.RunAsync();
}
```

### 📁 Configuration Organization

```json
{
  "Application": {
    "Name": "MyApp",
    "Mapping": { "Prefix": "MyApp" }
  },
  "Database": { /* database config */ },
  "Api": { /* external API config */ },
  "Features": { /* feature flags */ },
  "Services": { /* service-specific config */ }
}
```

### 🔒 Security Guidelines

```csharp
// ❌ Don't commit secrets to version control
// ✅ Use environment-specific sources for secrets

// .env (local development only - add to .gitignore)
API_KEY=dev-secret-key
DATABASE_PASSWORD=dev-password

// Production: use environment variables or secure vaults
Environment.SetEnvironmentVariable("API__KEY", productionApiKey);
```

### 🧪 Testing Strategies

```csharp
[Test]
public void Should_Load_Test_Configuration()
{
    // Arrange
    var testConfig = new FlexConfigurationBuilder()
        .AddSource(new MemoryConfigurationSource
        {
            InitialData = new Dictionary<string, string?>
            {
                ["Database:ConnectionString"] = "InMemoryTestDb",
                ["Api:BaseUrl"] = "https://test-api.example.com",
                ["Features:NewUI"] = "false"
            }
        })
        .Build();

    // Act
    dynamic config = testConfig;
    var connectionString = config.Database.ConnectionString;
    var isNewUIEnabled = config.Features.NewUI.ToType<bool>();

    // Assert
    Assert.Equal("InMemoryTestDb", connectionString);
    Assert.False(isNewUIEnabled);
}
```

## Migration Guide

### From Standard .NET Configuration

```csharp
// Before: Standard IConfiguration
public class DatabaseService(IConfiguration configuration)
{
    public async Task ConnectAsync()
    {
        var connectionString = configuration["Database:ConnectionString"];
        var timeout = int.Parse(configuration["Database:CommandTimeout"] ?? "30");
        // ...
    }
}

// After: FlexKit.Configuration
public class DatabaseService(IFlexConfig flexConfig)
{
    public async Task ConnectAsync()
    {
        dynamic config = flexConfig;
        var connectionString = config.Database.ConnectionString;
        var timeout = config.Database.CommandTimeout.ToType<int>();
        // ...
    }
}
```

### From Options Pattern

```csharp
// Before: Options pattern
services.Configure<DatabaseOptions>(configuration.GetSection("Database"));

public class DatabaseService(IOptions<DatabaseOptions> options)
{
    private readonly DatabaseOptions _options = options.Value;
}

// After: FlexKit.Configuration (both approaches work)
builder.AddFlexConfig(); // Automatically registers strongly typed configs

// Option 1: Direct registration
public class DatabaseService(DatabaseConfig config) { }

// Option 2: Keep flexibility with FlexConfig
public class DatabaseService(IFlexConfig flexConfig) 
{
    public void Method()
    {
        dynamic config = flexConfig;
        // Use dynamic or direct access as needed
    }
}
```

## Troubleshooting

### Common Issues

**Q: Dynamic properties return null**
```csharp
// ❌ Problem: Case sensitivity or missing keys
dynamic config = flexConfig;
var value = config.api.timeout; // null - wrong casing

// ✅ Solution: Match exact configuration structure
var value = config.Api.Timeout; // Works - matches JSON structure
```

**Q: Type conversion fails**
```csharp
// ❌ Problem: Invalid format or null values
var port = config.InvalidKey.ToType<int>(); // Exception

// ✅ Solution: Use safe conversion with defaults
var port = (config.Server?.Port?.ToType<int?>()) ?? 8080;
```

**Q: Assembly scanning not working**
```csharp
// ✅ Ensure mapping configuration is correct
{
  "Application": {
    "Mapping": {
      "Prefix": "YourCompany" // Must match assembly names
    }
  }
}
```

### Performance Debugging

```csharp
// Profile configuration access patterns
public class ConfigurationProfiler(IFlexConfig flexConfig)
{
    public void ProfileAccess()
    {
        var sw = Stopwatch.StartNew();
        
        // Test different access patterns
        var direct = flexConfig["Api:Timeout"]; // Fastest
        sw.Restart();
        
        dynamic config = flexConfig;
        var dynamic = config.Api.Timeout; // Slower but readable
        sw.Stop();
        
        Console.WriteLine($"Dynamic access took: {sw.ElapsedNanos}ns");
    }
}
```

## Examples & Samples

Complete working examples are available in the samples folder:

- **[FlexKitConfigurationConsoleApp](../../samples/FlexKitConfigurationConsoleApp/)**: Comprehensive demonstration of all features
- **Configuration Sources**: JSON, .env, environment variables, in-memory
- **Access Patterns**: Dynamic, strongly typed, direct key access, indexed access
- **Dependency Injection**: Constructor injection, property injection, assembly scanning
- **Type Conversion**: All supported types with examples

## Contributing

FlexKit.Configuration welcomes contributions! Areas of interest:

- **New Configuration Sources**: Custom providers for cloud services, databases
- **Performance Optimizations**: Runtime improvements while maintaining startup philosophy
- **Developer Experience**: Tools, analyzers, debugging support
- **Documentation**: Examples, tutorials, best practices

## License

This project is licensed under the MIT License - see the [LICENSE](../../LICENSE) file for details.

---

**FlexKit.Configuration**: Because configuration should be simple, flexible, and just work. ✨