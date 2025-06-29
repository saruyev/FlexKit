# FlexKit.Configuration

A modern, flexible configuration library for .NET 9 applications with dynamic access capabilities, Autofac dependency injection integration, and comprehensive configuration source support.

## 🚀 Features

- **🔧 Dynamic Configuration Access**: Access configuration values using natural C# syntax with runtime member resolution
- **📦 Autofac Integration**: Seamless integration with Autofac dependency injection container with automatic module discovery
- **🔍 Assembly Scanning**: Intelligent assembly discovery and registration with configurable filtering
- **🔄 Type Conversion**: Built-in support for converting configuration values to various types with culture-invariant parsing
- **🌍 Multiple Configuration Sources**: Support for JSON files, environment variables, and .env files
- **⚙️ Environment Support**: Environment-specific configuration file loading with precedence handling
- **⚡ Modern C# 13**: Leverages latest language features including file-scoped namespaces and primary constructors
- **🛡️ Null Safety**: Full nullable reference type support with comprehensive null-safety annotations
- **📋 Property Injection**: Automatic injection of FlexConfig into services through property injection

## 🏗️ Architecture

FlexKit.Configuration is organized into focused namespaces:

- **`FlexKit.Configuration.Core`**: Core configuration classes including `FlexConfiguration`, `IFlexConfig`, and builders
- **`FlexKit.Configuration.Assembly`**: Assembly scanning and module discovery functionality
- **`FlexKit.Configuration.Conversion`**: Type conversion utilities and extension methods
- **`FlexKit.Configuration.Sources`**: Custom configuration sources including .env file support

## 📦 Installation

```bash
dotnet add package FlexKit.Configuration
```

## 🚀 Quick Start

### 1. Basic Setup with Autofac

```csharp
using Autofac;
using Autofac.Extensions.DependencyInjection;
using FlexKit.Configuration.Core;
using FlexKit.Configuration.Assembly;

var builder = WebApplication.CreateBuilder(args);

// Configure Autofac as the service provider
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

// Configure FlexConfig with multiple sources
builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{
    // Method 1: Using FlexConfigurationBuilder (Recommended)
    containerBuilder.AddFlexConfig(config => config
        .AddJsonFile("appsettings.json", optional: false)
        .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
        .AddDotEnvFile(".env", optional: true)
        .AddEnvironmentVariables());
    
    // Register modules from assemblies
    containerBuilder.AddModules(builder.Configuration);
});

var app = builder.Build();
```

### 2. Dynamic Configuration Access

```csharp
public class ApiService(IFlexConfig config)
{
    public async Task<string> CallExternalApiAsync()
    {
        // Dynamic access with natural syntax
        dynamic settings = config;
        var apiKey = settings.External.Api.Key;
        var timeout = settings.External.Api.Timeout;
        var baseUrl = settings.External.Api.BaseUrl;
        
        // Traditional indexer access
        var retryCount = config["External:Api:RetryCount"];
        
        // Type-safe conversion
        var timeoutMs = config["External:Api:Timeout"].ToType<int>();
        var isEnabled = config["External:Api:Enabled"].ToType<bool>();
        
        // Array/collection support
        var allowedHosts = config["External:Api:AllowedHosts"].GetCollection<string>();
        
        // Use in HTTP client setup
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromMilliseconds(timeoutMs);
        client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
        
        return await client.GetStringAsync($"{baseUrl}/data");
    }
}
```

### 3. Strongly Typed Configuration

FlexKit.Configuration provides excellent support for strongly typed configuration classes:

```csharp
// Define your configuration classes
public class DatabaseConfig
{
    public string ConnectionString { get; set; } = string.Empty;
    public int CommandTimeout { get; set; } = 30;
    public int MaxRetryCount { get; set; } = 3;
    public bool EnableLogging { get; set; }
}

public class ApiConfig
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public int Timeout { get; set; } = 5000;
    public bool EnableCompression { get; set; } = true;
}

// Register them with sections
builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{
    containerBuilder.AddFlexConfig(config => config
        .AddJsonFile("appsettings.json")
        .AddEnvironmentVariables())
        .RegisterConfig<DatabaseConfig>("Database")
        .RegisterConfig<ApiConfig>("External:Api")
        .RegisterConfig<AppConfig>(); // Binds to root configuration
});

// Inject into services
public class DatabaseService(DatabaseConfig config)
{
    public async Task ConnectAsync()
    {
        // Type-safe access to configuration
        var connectionString = config.ConnectionString;
        var timeout = config.CommandTimeout;
        var retryCount = config.MaxRetryCount;
        
        // Use in database connection...
    }
}

public class ApiService(ApiConfig config)
{
    public async Task<string> CallApiAsync()
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromMilliseconds(config.Timeout);
        client.DefaultRequestHeaders.Add("X-API-Key", config.ApiKey);
        
        return await client.GetStringAsync($"{config.BaseUrl}/data");
    }
}
```

### 4. Property Injection Support

```csharp
public class BackgroundService
{
    // Automatically injected by ConfigurationModule
    public IFlexConfig? FlexConfiguration { get; set; }
    
    public async Task ProcessDataAsync()
    {
        if (FlexConfiguration == null) return;
        
        // Use injected configuration
        dynamic config = FlexConfiguration;
        var batchSize = config.Processing.BatchSize;
        var intervalMs = config.Processing.IntervalMs;
        
        // Traditional access still available
        var connectionString = FlexConfiguration["ConnectionStrings:DefaultConnection"];
    }
}
```

## 🔧 Configuration Sources

### Strongly Typed Configuration Classes

Create dedicated configuration classes for different areas of your application:

```json
{
  "Database": {
    "ConnectionString": "Server=localhost;Database=MyApp;",
    "CommandTimeout": 30,
    "MaxRetryCount": 3,
    "EnableLogging": true
  },
  "External": {
    "Api": {
      "BaseUrl": "https://api.example.com",
      "ApiKey": "your-secret-key",
      "Timeout": 5000
    }
  },
  "Features": {
    "EnableCaching": true,
    "MaxCacheSize": 1000
  }
}
```

```csharp
// Configure and register multiple strongly typed configurations
containerBuilder.AddFlexConfig(config => config
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables())
    .RegisterConfig<DatabaseConfig>("Database")
    .RegisterConfig<ApiConfig>("External:Api")
    .RegisterConfig<FeatureConfig>("Features")
    .RegisterConfig<AppConfig>(); // Root binding

// Batch registration for multiple configurations
var configurations = new[]
{
    (typeof(DatabaseConfig), "Database"),
    (typeof(ApiConfig), "External:Api"),
    (typeof(FeatureConfig), "Features")
};
containerBuilder.RegisterConfigs(configurations);
```

### JSON Files

Standard JSON configuration files with environment-specific overrides:

```csharp
builder.AddFlexConfig(config => config
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile($"appsettings.{environment}.json", optional: true));
```

### Environment Variables

Environment variables have the highest precedence and override all file-based configuration:

```csharp
builder.AddFlexConfig(config => config
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()); // Highest precedence

// Set environment variables:
// Windows PowerShell: $env:DATABASE__CONNECTIONSTRING="Server=localhost"
// Linux/macOS: export DATABASE__CONNECTIONSTRING="Server=localhost"
// Docker: -e DATABASE__CONNECTIONSTRING="Server=localhost"
```

### .env Files

FlexKit.Configuration includes native .env file support with comments, quoted values, and escape sequences:

```csharp
builder.AddFlexConfig(config => config
    .AddJsonFile("appsettings.json")
    .AddDotEnvFile(".env", optional: true)
    .AddDotEnvFile($".env.{environment}", optional: true)
    .AddEnvironmentVariables());
```

Example `.env` file:
```env
# Database Configuration
DATABASE_URL=postgresql://localhost:5432/myapp
DATABASE_POOL_SIZE=10

# API Configuration  
API_KEY="your-secret-api-key-here"
API_TIMEOUT=5000
API_BASE_URL=https://api.example.com

# Feature Flags
ENABLE_CACHING=true
DEBUG_MODE=false

# Quoted values with escape sequences
WELCOME_MESSAGE="Welcome to our application!\nPlease enjoy your stay."
```

## 🔍 Assembly Scanning Configuration

Control which assemblies are scanned for Autofac modules using the `MappingConfig`:

```json
{
  "Application": {
    "Mapping": {
      "Prefix": "MyCompany",
      "Names": ["MyCompany.Services", "MyCompany.Data", "MyCompany.Infrastructure"]
    }
  }
}
```

### Assembly Scanning Options

```csharp
// Option 1: Prefix-based scanning (recommended for consistent naming)
{
  "Application": {
    "Mapping": {
      "Prefix": "Acme"  // Scans: Acme.Services, Acme.Data, Acme.Core, etc.
    }
  }
}

// Option 2: Explicit assembly names
{
  "Application": {
    "Mapping": {
      "Names": [
        "MyCompany.Services",
        "MyCompany.Data", 
        "MyCompany.Infrastructure"
      ]
    }
  }
}

// Option 3: Combined approach
{
  "Application": {
    "Mapping": {
      "Prefix": "MyCompany",
      "Names": ["ThirdParty.Extensions"]
    }
  }
}
```

## 🔄 Type Conversion

FlexKit.Configuration provides robust type conversion with culture-invariant parsing:

### Basic Type Conversion

```csharp
// Primitive types
var port = config["Server:Port"].ToType<int>();
var isEnabled = config["Features:NewFeature"].ToType<bool>();
var timeout = config["Api:Timeout"].ToType<double>();
var startDate = config["Deployment:StartDate"].ToType<DateTime>();

// Enums
var logLevel = config["Logging:Level"].ToType<LogLevel>();

// Nullable types
var optionalPort = config["Server:BackupPort"].ToType<int?>();
```

### Collection Support

```csharp
// String collections (comma-separated by default)
var allowedHosts = config["Security:AllowedHosts"].GetCollection<string>();
// Input: "localhost,127.0.0.1,::1"
// Output: ["localhost", "127.0.0.1", "::1"]

// Custom separators
var servers = config["LoadBalancer:Servers"].GetCollection<string>(';');
// Input: "server1:8080;server2:8080;server3:8080"
// Output: ["server1:8080", "server2:8080", "server3:8080"]

// Integer collections
var ports = config["Monitoring:Ports"].GetCollection<int>();
// Input: "8080,8081,8082"
// Output: [8080, 8081, 8082]
```

### Dictionary Conversion

```csharp
// From configuration section to dictionary
var connectionStrings = config.Configuration
    .GetSection("ConnectionStrings")
    .GetChildren()
    .ToDictionary<string>();

// Typed dictionaries
var retrySettings = config.Configuration
    .GetSection("RetrySettings")
    .GetChildren()
    .ToDictionary<int>();
```

## 🎯 Usage Patterns

### Strongly Typed vs Dynamic Access

FlexKit.Configuration provides multiple approaches to suit different scenarios:

```csharp
public class PaymentService
{
    // Option 1: Constructor injection of strongly typed config (Recommended)
    public PaymentService(ApiConfig apiConfig)
    {
        var baseUrl = apiConfig.BaseUrl;
        var timeout = apiConfig.Timeout;
        // Compile-time safety, IntelliSense support
    }
    
    // Option 2: FlexConfig with dynamic access
    public PaymentService(IFlexConfig config)
    {
        dynamic settings = config;
        var baseUrl = settings.External.Api.BaseUrl;
        var timeout = settings.External.Api.Timeout;
        // Runtime flexibility, natural syntax
    }
    
    // Option 3: Traditional configuration access
    public PaymentService(IConfiguration config)
    {
        var baseUrl = config["External:Api:BaseUrl"];
        var timeout = config["External:Api:Timeout"].ToType<int>();
        // Explicit, familiar to existing code
    }
}
```

### Multiple Access Methods

FlexKit.Configuration supports multiple ways to access the same configuration:

```csharp
public class DatabaseService(IFlexConfig config)
{
    private readonly string _connectionString = GetConnectionString(config);
    
    private static string GetConnectionString(IFlexConfig config)
    {
        // Method 1: Dynamic access
        dynamic settings = config;
        return settings.ConnectionStrings.DefaultConnection;
        
        // Method 2: Traditional indexer
        // return config["ConnectionStrings:DefaultConnection"];
        
        // Method 3: Direct IConfiguration access
        // return config.Configuration.GetConnectionString("DefaultConnection");
    }
}
```

### Array/Collection Configuration

```csharp
// Configuration (appsettings.json)
{
  "Servers": [
    { "Name": "Server1", "Port": 8080, "IsActive": true },
    { "Name": "Server2", "Port": 8081, "IsActive": false }
  ]
}

// Access methods
public class LoadBalancer(IFlexConfig config)
{
    public void ConfigureServers()
    {
        // Get servers section
        var serversSection = config.Configuration.GetSection("Servers");
        var serversConfig = serversSection.GetFlexConfiguration();
        
        // Access by index
        var firstServer = serversConfig[0];
        var secondServer = serversConfig[1];
        
        // Dynamic access on indexed elements
        if (firstServer != null)
        {
            dynamic server1 = firstServer;
            var name = server1.Name;
            var port = server1.Port.ToType<int>();
            var isActive = server1.IsActive.ToType<bool>();
        }
    }
}
```

## 🏃‍♂️ Performance Considerations

### Dynamic Access Performance

Dynamic access has runtime overhead due to reflection and dynamic dispatch:

```csharp
// ✅ Good: Cache frequently accessed values
public class PerformanceCriticalService(IFlexConfig config)
{
    private readonly int _batchSize = config["Processing:BatchSize"].ToType<int>();
    private readonly string _apiKey = ((dynamic)config).External.Api.Key;
    
    public void ProcessBatch()
    {
        // Use cached values instead of repeated dynamic access
        for (int i = 0; i < _batchSize; i++)
        {
            // Process with cached _apiKey
        }
    }
}

// ❌ Avoid: Repeated dynamic access in hot paths
public void SlowMethod(IFlexConfig config)
{
    for (int i = 0; i < 1000; i++)
    {
        dynamic settings = config;  // Repeated dynamic overhead
        var value = settings.Some.Nested.Property;
    }
}
```

### Recommended Patterns

```csharp
// ✅ Configuration at startup/initialization
public class EmailService(IFlexConfig config)
{
    private readonly EmailConfig _emailConfig = LoadEmailConfig(config);
    
    private static EmailConfig LoadEmailConfig(IFlexConfig config)
    {
        dynamic settings = config;
        return new EmailConfig
        {
            SmtpHost = settings.Email.Smtp.Host,
            SmtpPort = settings.Email.Smtp.Port.ToType<int>(),
            ApiKey = settings.Email.SendGrid.ApiKey,
            EnableSsl = settings.Email.Smtp.EnableSsl.ToType<bool>()
        };
    }
}

// ✅ Traditional access for simple cases
var connectionString = config["ConnectionStrings:DefaultConnection"];

// ✅ Type conversion for known types
var timeout = config["Api:Timeout"].ToType<int>();
```

## 🔒 Security Best Practices

### .env File Security

```csharp
// ✅ Use .env files for development only
builder.AddFlexConfig(config => config
    .AddJsonFile("appsettings.json")
    .AddJsonFile($"appsettings.{environment}.json", optional: true)
    .AddDotEnvFile(".env", optional: true)  // Development secrets
    .AddEnvironmentVariables());            // Production overrides

// ✅ Never commit .env files with sensitive data
// Add to .gitignore:
// .env
// .env.local
// .env.production
```

### Configuration Precedence

Understand configuration source precedence (last wins):

1. **JSON files** (appsettings.json, appsettings.{environment}.json)
2. **.env files** (.env, .env.{environment})
3. **Environment variables** (highest precedence)

```csharp
// Production deployment should rely on environment variables
// for sensitive configuration like connection strings and API keys
```

## 🧪 Testing Support

FlexKit.Configuration works seamlessly with testing frameworks and supports both strongly typed and dynamic access:

```csharp
[Fact]
public void PaymentService_WithStronglyTypedConfig_ProcessesCorrectly()
{
    // Arrange - Strongly typed configuration
    var apiConfig = new ApiConfig
    {
        BaseUrl = "https://test-api.com",
        ApiKey = "test-key-123",
        Timeout = 1000
    };
    
    var service = new PaymentService(apiConfig);
    
    // Act & Assert
    var result = service.ProcessPayment();
    result.Should().NotBeNull();
}

[Fact]
public void Service_WithFlexConfig_ProcessesCorrectly()
{
    // Arrange - Dynamic configuration  
    var testConfig = new Dictionary<string, string?>
    {
        ["Processing:BatchSize"] = "100",
        ["Processing:Timeout"] = "5000",
        ["External:Api:Key"] = "test-api-key"
    };
    
    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(testConfig)
        .Build();
        
    var flexConfig = configuration.GetFlexConfiguration();
    var service = new ProcessingService(flexConfig);
    
    // Act & Assert
    var result = service.Process();
    result.Should().NotBeNull();
}

[Fact]
public void AutofacContainer_WithRegisteredConfigs_InjectsCorrectly()
{
    // Arrange
    var testData = new Dictionary<string, string?>
    {
        ["Database:ConnectionString"] = "Server=test;",
        ["External:Api:BaseUrl"] = "https://test-api.com"
    };

    var containerBuilder = new ContainerBuilder();
    containerBuilder.AddFlexConfig(config => config
        .AddInMemoryCollection(testData))
        .RegisterConfig<DatabaseConfig>("Database")
        .RegisterConfig<ApiConfig>("External:Api");

    containerBuilder.RegisterType<DatabaseService>().AsSelf();

    using var container = containerBuilder.Build();

    // Act
    var service = container.Resolve<DatabaseService>();

    // Assert
    service.Config.ConnectionString.Should().Be("Server=test;");
}
```

# Performance Characteristics

FlexKit.Configuration provides multiple access patterns with different performance trade-offs. Understanding these characteristics helps you choose the right pattern for your use case.

## Performance Summary

| Access Pattern | Performance Impact | Memory Impact | Use Case |
|---|---|---|---|
| **Indexer Access** | ~1% overhead | Minimal | ✅ Production hot paths (scales linearly) |
| **ToType Conversion** | 20-40% overhead | 24-368 bytes | ✅ Configuration setup |
| **RegisterConfig** | 10-105x slower | 808-8,408 bytes | ⚠️ Startup/initialization only |
| **Dynamic Access** | 16-6,000x slower | 2KB-55KB | ❌ Development only (exponential scaling) |
| **Section Navigation** | 14-176x slower | 4KB-60KB | ❌ Avoid in production |

## Detailed Benchmark Results

### Configuration Access Patterns

```
Method                                 Mean        Ratio    Allocated
StandardConfigurationAccess           27.02 ns    1.00x    -
FlexConfigurationIndexerAccess        27.39 ns    1.01x    -
FlexConfigurationDynamicAccess        2,028 ns    75.07x   2,784 B
FlexConfigurationChainedDynamicAccess 1,486 ns    55.00x   -
```

**Key Insight**: Indexer access (`flexConfig["Database:ConnectionString"]`) has virtually no performance penalty compared to standard IConfiguration.

### Type Conversion Overhead

```
Method                           Mean      Ratio    Allocated
StandardConfigurationIntParsing  25.87 ns  1.00x    -
FlexConfigurationToTypeInt       35.43 ns  1.37x    -
FlexConfigurationToTypeBool      30.92 ns  1.20x    24 B
FlexConfigurationToTypeDouble    62.79 ns  2.43x    24 B
```

**Key Insight**: `ToType<T>()` methods add 20-40% overhead for culture-invariant safety and better error handling.

### Deep Configuration Access

```
Method                              Mean        Ratio    Allocated
StandardConfigurationDeepAccess    29.52 ns    1.00x    -
FlexConfigurationIndexerDeepAccess  29.95 ns    1.01x    -
FlexConfigurationDynamicDeepAccess  2,953 ns    100.03x  4,000 B
```

**Key Insight**: Use colon notation (`config["Level1:Level2:Level3:Property"]`) instead of dynamic navigation for deep access.

### Memory Allocation Patterns

```
Method                             Mean        Ratio    Allocated
StandardConfigurationAllocations   203.8 ns    1.00x    -
FlexConfigurationIndexerAllocations 234.6 ns    1.15x    368 B
FlexConfigurationDynamicAllocations 3,393 ns    16.65x   5,712 B
RepeatedDynamicAccess              8,237 ns    40.42x   13,368 B
```

**Key Insight**: Dynamic access creates significant garbage collection pressure. Never use in loops or hot paths.

### Strongly-Typed Configuration (RegisterConfig vs IOptions)

```
Method                                    Mean        Ratio    Allocated
StandardIOptionsResolution                18.10 ns    1.00x    -
FlexKitRegisterConfigResolution          181.79 ns    10.04x   808 B
StandardIOptionsMultipleAccess            51.78 ns    2.86x    128 B
FlexKitRegisterConfigMultipleAccess      558.97 ns    30.87x   2,480 B
StandardIOptionsRepeatedAccess           205.89 ns    11.37x   328 B
FlexKitRegisterConfigRepeatedAccess    1,908.50 ns   105.42x  8,408 B
CreateStandardIOptionsContainer        3,835 ns     211.84x  12,360 B
CreateFlexKitRegisterConfigContainer  14,402 ns     795.51x  32,651 B
```

**Key Insight**: RegisterConfig has significant performance overhead due to Autofac resolution. Use IOptions for production hot paths.

### Enterprise-Scale Configuration (580+ Keys, 5 Levels Deep)

```
Method                            Mean           Ratio     Allocated
LoadStandardConfiguration         249,977 ns     1.00x     392,130 B
LoadFlexConfiguration            255,951 ns     1.02x     392,154 B
StandardConfigDeepAccess              53 ns     1.00x     -
FlexConfigIndexerDeepAccess           54 ns     1.02x     -
FlexConfigDynamicShallowAccess   157,020 ns  2,962.64x    55,384 B
FlexConfigSectionNavigation      176,243 ns  3,326.28x    60,080 B
StandardConfigEnumerateAllKeys    10.7 sec      1.00x     808,144 B
FlexConfigEnumerateAllKeys        11.9 sec      1.12x     808,144 B
```

**Key Insight**: Configuration size amplifies FlexKit overhead exponentially. Dynamic access becomes 6,000x+ slower with enterprise-scale configs.

## Performance Guidelines

### ✅ Recommended Patterns

**Production Hot Paths (Any Scale):**
```csharp
// Scales linearly - 1-2% overhead regardless of config size
var connectionString = flexConfig["Database:ConnectionString"];
var port = int.Parse(flexConfig["Server:Port"] ?? "8080");

// Deep access - same performance as standard IConfiguration
var deepValue = flexConfig["Level1:Level2:Level3:Level4:Property"];

// Or for strongly-typed: use IOptions pattern
public class DatabaseService
{
    private readonly DatabaseConfig _config;
    
    public DatabaseService(IOptions<DatabaseConfig> config)
    {
        _config = config.Value; // Cache the value
    }
}
```

**Configuration Setup (Startup):**
```csharp
// 15-40% overhead, acceptable for safety and convenience
var settings = new AppSettings
{
    ConnectionString = flexConfig["Database:ConnectionString"],
    Port = flexConfig["Server:Port"].ToType<int>(),
    IsSecure = flexConfig["Server:IsSecure"].ToType<bool>()
};

// RegisterConfig acceptable for startup-only access
containerBuilder.RegisterConfig<DatabaseConfig>("Database");

// FlexConfig creation scales well - only 2% overhead with large configs
var flexConfig = new FlexConfiguration(enterpriseConfig);
```

**Deep Configuration Access:**
```csharp
// Use colon notation - same performance as standard IConfiguration
var apiKey = flexConfig["External:Services:Api:Key"];
```

### ⚠️ Use With Caution

**Dynamic Access (Development/Prototyping Only):**
```csharp
// Performance degrades exponentially with config size
// Small config: 75x slower, Large config: 6,000x slower
dynamic config = flexConfig;
var apiKey = config.External.Services.Api.Key;
```

**RegisterConfig in Services:**
```csharp
// Acceptable for startup initialization, avoid in request processing
public void ConfigureServices(IServiceCollection services)
{
    containerBuilder.RegisterConfig<DatabaseConfig>("Database"); // OK - one time
}

public class OrderService
{
    // Avoid resolving repeatedly - use IOptions instead
    public void ProcessOrder(IContainer container)
    {
        var config = container.Resolve<DatabaseConfig>(); // 10x+ slower than IOptions
    }
}
```

### ❌ Avoid in Production

**Dynamic Access with Large Configurations:**
```csharp
// NEVER use with enterprise-scale configs (580+ keys)
// Performance degrades from 75x to 6,000x+ slower
dynamic config = flexConfig; // 157,000ns + 55KB allocation per access
var value = config.Deep.Path.To.Value;
```

**Section Navigation at Scale:**
```csharp
// Becomes extremely expensive with large configs
var section = flexConfig.Configuration.CurrentConfig("Section"); // 176x slower
// Use indexer access instead: flexConfig["Section:SubSection:Value"]
```

**RegisterConfig in Hot Paths:**
```csharp
// 10-105x slower with significant memory allocation - avoid in services
public class OrderService
{
    public void ProcessOrder(IContainer container)
    {
        var config = container.Resolve<DatabaseConfig>(); // 808B allocation per call
        // Use IOptions instead
    }
}
```

**Dynamic Access in Loops:**
```csharp
// Exponentially worse with large configs - never do this
for (int i = 0; i < items.Count; i++)
{
    dynamic config = flexConfig;  // Creates massive overhead per iteration
    var value = config.SomeSection.SomeValue;
}
```

**Dynamic Deep Navigation:**
```csharp
// 100x slower, 4KB allocations - avoid completely
dynamic config = flexConfig;
var value = config.Level1.Level2.Level3.Property;  // Use config["Level1:Level2:Level3:Property"] instead
```

## When to Use Each Pattern

### FlexConfig Indexer Access
- **Performance**: ~1% overhead
- **Use for**: Production code, hot paths, frequent access
- **Example**: `flexConfig["Database:ConnectionString"]`

## When to Use Each Pattern

### FlexConfig Indexer Access
- **Performance**: ~1% overhead (scales linearly)
- **Use for**: Production code, hot paths, frequent access, any configuration size
- **Example**: `flexConfig["Database:ConnectionString"]`

### ToType Conversion
- **Performance**: 20-40% overhead
- **Use for**: Configuration setup, type safety, culture-invariant parsing
- **Example**: `flexConfig["Server:Port"].ToType<int>()`

### RegisterConfig (Strongly-Typed)
- **Performance**: 10-105x slower (depending on usage pattern)
- **Use for**: Startup/initialization, developer convenience, Autofac consistency
- **Avoid for**: Hot paths, frequent access, memory-sensitive scenarios
- **Example**: `containerBuilder.RegisterConfig<DatabaseConfig>("Database")`

### Dynamic Access
- **Performance**: 16x-6,000x slower (exponential scaling with config size)
- **Use for**: Development, prototyping, one-time configuration reading, small configs only
- **Avoid for**: Production code, large configurations (580+ keys), any frequent access
- **Example**: `config.Database.ConnectionString`

### Standard IConfiguration
- **Performance**: Fastest possible
- **Use for**: Maximum performance requirements, existing codebases
- **Example**: `configuration["Database:ConnectionString"]`

### Standard IOptions Pattern
- **Performance**: Fastest for strongly-typed configuration
- **Use for**: Production services, high-frequency access, ASP.NET Core integration
- **Example**: `IOptions<DatabaseConfig>` dependency injection

## Configuration Scale Impact

### Small Configurations (< 50 keys)
- All patterns perform reasonably well
- Dynamic access: 75x slower but usable for convenience
- Choose based on developer experience vs performance needs

### Enterprise Configurations (500+ keys, 5+ levels deep)
- **Indexer access scales linearly** - maintains ~1% overhead
- **Dynamic access becomes unusable** - 6,000x+ slower with massive allocations
- **Section navigation expensive** - 176x+ slower
- **Configuration loading overhead minimal** - 2% impact regardless of size

**Critical Insight**: Configuration size amplifies FlexKit's overhead patterns. What works with small configs may become unusable at enterprise scale.

## Benchmark Environment

Benchmarks performed using BenchmarkDotNet v0.15.2 on:
- **Hardware**: AMD Ryzen 9 5900HX, 16 logical cores
- **Runtime**: .NET 9.0.6, X64 RyuJIT AVX2
- **OS**: Windows 11 24H2

*Results may vary on different hardware and runtime configurations.*

## 📊 Benefits

### Compared to Standard IConfiguration

| Feature | Standard IConfiguration | FlexKit.Configuration |
|---------|-------------------------|----------------------|
| Access Syntax | `config["Nested:Property"]` | `config.Nested.Property` (dynamic) |
| Type Safety | Manual casting required | Built-in `ToType<T>()` |
| Strongly Typed | Manual binding with `GetSection().Get<T>()` | `RegisterConfig<T>()` with DI |
| Null Safety | No null checking | Comprehensive null annotations |
| Collection Support | Manual parsing | `GetCollection<T>()` |
| .env Files | Requires additional packages | Native support |
| Dynamic Access | Not supported | Full dynamic object support |
| Autofac Integration | Manual setup | Automatic with `AddFlexConfig()` |
| Assembly Scanning | Not included | Intelligent filtering with `AddModules()` |
| Property Injection | Manual configuration | Automatic via `ConfigurationModule` |

### Development Experience

- **IntelliSense Support**: Dynamic access provides IntelliSense where possible
- **Compile-time Safety**: Generic type conversion with compile-time checking
- **Reduced Boilerplate**: Less manual casting and null checking
- **Flexible Access**: Choose between dynamic and traditional access per use case
- **Modern C# Features**: Built with C# 13 and nullable reference types

## 📚 Additional Resources

- **Integration Tests**: See `FlexKit.Configuration.IntegrationTests` for comprehensive usage examples
- **Unit Tests**: See `FlexKit.Configuration.Tests` for detailed API usage patterns
- **Performance Tests**: Benchmark comparisons available in test projects
- **Migration Guide**: Documentation for migrating from standard IConfiguration

## 🛠️ Requirements

- **.NET 9.0** or later
- **C# 13.0** or later
- **Autofac 8.3.0** or later
- **Microsoft.Extensions.Configuration 9.0** or later

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🤝 Contributing

Contributions are welcome! Please feel free to submit pull requests, create issues, or suggest improvements.

---

*FlexKit.Configuration is part of the broader FlexKit ecosystem, providing a solid foundation for enterprise .NET applications with modern configuration management.*
