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

### 3. Property Injection Support

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

### 4. Creating Configuration Modules

```csharp
using Autofac;
using FlexKit.Configuration.Core;

public class DatabaseModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Register services with configuration-based setup
        builder.Register(c =>
        {
            var config = c.Resolve<IFlexConfig>();
            dynamic dbConfig = config;
            
            var connectionString = dbConfig.Database.ConnectionString;
            var commandTimeout = dbConfig.Database.CommandTimeout;
            var maxRetryCount = dbConfig.Database.MaxRetryCount;
            
            return new DatabaseContext(connectionString)
            {
                CommandTimeout = TimeSpan.FromSeconds(commandTimeout),
                MaxRetryCount = maxRetryCount
            };
        }).As<IDatabaseContext>().InstancePerLifetimeScope();
        
        // Register repositories
        builder.RegisterType<UserRepository>().As<IUserRepository>();
        builder.RegisterType<ProductRepository>().As<IProductRepository>();
    }
}

public class ExternalServicesModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.Register(c =>
        {
            var config = c.Resolve<IFlexConfig>();
            
            // Type-safe configuration access
            var apiKey = config["External:PaymentService:ApiKey"];
            var baseUrl = config["External:PaymentService:BaseUrl"];
            var timeout = config["External:PaymentService:Timeout"].ToType<int>();
            
            return new PaymentServiceClient(baseUrl, apiKey)
            {
                Timeout = TimeSpan.FromMilliseconds(timeout)
            };
        }).As<IPaymentServiceClient>().SingleInstance();
    }
}
```

## 🔧 Configuration Sources

### JSON Files

```csharp
builder.AddFlexConfig(config => config
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{environment}.json", optional: true));
```

### Environment Variables

```csharp
// Environment variables override all file-based configuration
builder.AddFlexConfig(config => config
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()); // Highest precedence

// Set environment variables:
// Windows PowerShell: $env:DATABASE__CONNECTIONSTRING="Server=localhost"
// Linux/macOS: export DATABASE__CONNECTIONSTRING="Server=localhost"
// Docker: -e DATABASE__CONNECTIONSTRING="Server=localhost"
```

### .env Files

FlexKit.Configuration includes native .env file support:

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

Control which assemblies are scanned for Autofac modules:

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

// Option 2: Explicit assembly names (fine-grained control)
{
  "Application": {
    "Mapping": {
      "Names": ["Acme.Services", "Acme.Data", "ThirdParty.Extensions"]
    }
  }
}

// Option 3: No configuration (defaults to FlexKit assemblies)
// Automatically includes assemblies containing "Module" or starting with "FlexKit"
```

## 🔄 Type Conversion Features

### Built-in Type Conversions

```csharp
// Primitive types
var port = config["Server:Port"].ToType<int>();
var isEnabled = config["Features:NewFeature"].ToType<bool>();
var timeout = config["Api:Timeout"].ToType<double>();
var maxSize = config["Upload:MaxSize"].ToType<long>();

// Enums
var logLevel = config["Logging:Level"].ToType<LogLevel>();

// Nullable types
var optionalPort = config["Server:OptionalPort"].ToType<int?>();

// Collections from delimited strings
var allowedHosts = config["AllowedHosts"].GetCollection<string>(); // Comma-separated by default
var ports = config["LoadBalancer:Ports"].GetCollection<int>(';'); // Semicolon-separated

// Arrays from configuration sections
var servers = config.GetSection("Servers").GetChildren()
    .Select(c => c.Value).ToArray(typeof(string[])) as string[];
```

### Complex Type Binding

```csharp
// Still compatible with standard .NET configuration binding
var dbOptions = config.Configuration.GetSection("Database").Get<DatabaseOptions>();
var apiSettings = config.Configuration.GetSection("Api").Get<ApiSettings>();

// Use with Options pattern
services.Configure<DatabaseOptions>(config.Configuration.GetSection("Database"));
```

## 🏗️ Advanced Usage

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

// Use custom source
builder.AddFlexConfig(config => config
    .AddJsonFile("appsettings.json")
    .AddSource(new DatabaseConfigurationSource(connectionString))
    .AddEnvironmentVariables());
```

### Configuration Validation

```csharp
public class ApiModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.Register(c =>
        {
            var config = c.Resolve<IFlexConfig>();
            
            // Validate required configuration at startup
            var apiKey = config["External:Api:Key"] 
                ?? throw new InvalidOperationException("API key is required");
            var baseUrl = config["External:Api:BaseUrl"]
                ?? throw new InvalidOperationException("API base URL is required");
            
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
                throw new InvalidOperationException($"Invalid API base URL: {baseUrl}");
            
            return new ApiClient(uri, apiKey);
        }).As<IApiClient>().SingleInstance();
    }
}
```

### Environment-Specific Module Registration

```csharp
public class LoggingModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.Register(c =>
        {
            var config = c.Resolve<IFlexConfig>();
            dynamic logging = config;
            
            var environment = logging.Environment ?? "Production";
            var logLevel = logging.Logging.LogLevel.Default ?? "Information";
            
            // Configure different logging based on environment
            return environment switch
            {
                "Development" => new ConsoleLogger(logLevel),
                "Staging" => new FileLogger(logLevel, "/var/log/app-staging.log"),
                "Production" => new StructuredLogger(logLevel, logging.Logging.ElasticSearch.Endpoint),
                _ => new ConsoleLogger("Warning")
            };
        }).As<ILogger>().SingleInstance();
    }
}
```

## 📋 Configuration File Examples

### Complete appsettings.json

```json
{
  "Application": {
    "Mapping": {
      "Prefix": "MyCompany"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=MyApp;Trusted_Connection=true;",
    "Redis": "localhost:6379"
  },
  "Database": {
    "CommandTimeout": 30,
    "MaxRetryCount": 3,
    "EnableSensitiveDataLogging": false
  },
  "External": {
    "Api": {
      "Key": "development-api-key",
      "BaseUrl": "https://api-dev.example.com",
      "Timeout": 5000,
      "RetryCount": 3,
      "AllowedHosts": "localhost,127.0.0.1,api.example.com"
    },
    "PaymentService": {
      "ApiKey": "payment-dev-key",
      "BaseUrl": "https://payments-dev.example.com",
      "Timeout": 10000
    }
  },
  "Features": {
    "EnableCaching": true,
    "EnableMetrics": true,
    "NewUIEnabled": false
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "Servers": [
    {
      "Name": "Primary",
      "Host": "primary.example.com",
      "Port": 443,
      "Ssl": true
    },
    {
      "Name": "Secondary", 
      "Host": "secondary.example.com",
      "Port": 80,
      "Ssl": false
    }
  ],
  "Processing": {
    "BatchSize": 100,
    "IntervalMs": 5000,
    "MaxConcurrency": 4
  }
}
```

### Environment-Specific Override (appsettings.Production.json)

```json
{
  "External": {
    "Api": {
      "Key": "{{API_KEY_FROM_VAULT}}",
      "BaseUrl": "https://api.example.com",
      "Timeout": 3000
    },
    "PaymentService": {
      "ApiKey": "{{PAYMENT_KEY_FROM_VAULT}}",
      "BaseUrl": "https://payments.example.com"
    }
  },
  "Database": {
    "CommandTimeout": 60,
    "EnableSensitiveDataLogging": false
  },
  "Features": {
    "EnableMetrics": true,
    "NewUIEnabled": true
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "MyCompany": "Information"
    }
  }
}
```

## 🧪 Testing Support

### Unit Testing with FlexConfig

```csharp
[Test]
public void ServiceShouldUseConfigurationCorrectly()
{
    // Arrange
    var testConfig = new FlexConfigurationBuilder()
        .AddSource(new MemoryConfigurationSource
        {
            InitialData = new Dictionary<string, string>
            {
                ["External:Api:Key"] = "test-api-key",
                ["External:Api:BaseUrl"] = "https://test-api.example.com",
                ["External:Api:Timeout"] = "1000"
            }
        })
        .Build();
    
    var containerBuilder = new ContainerBuilder();
    containerBuilder.RegisterInstance(testConfig).As<IFlexConfig>();
    containerBuilder.RegisterType<ApiService>().As<IApiService>();
    
    var container = containerBuilder.Build();
    
    // Act
    var service = container.Resolve<IApiService>();
    
    // Assert
    Assert.That(service, Is.Not.Null);
    // Additional service-specific assertions...
}
```

### Integration Testing

```csharp
public class IntegrationTestFixture
{
    protected IContainer Container { get; private set; }
    
    [OneTimeSetUp]
    public void SetUp()
    {
        var builder = new ContainerBuilder();
        
        // Override configuration for testing
        builder.AddFlexConfig(config => config
            .AddJsonFile("appsettings.Test.json")
            .AddSource(new MemoryConfigurationSource
            {
                InitialData = new Dictionary<string, string>
                {
                    ["ConnectionStrings:DefaultConnection"] = "InMemoryDatabase",
                    ["External:Api:Key"] = "test-key",
                    ["Features:EnableMetrics"] = "false"
                }
            }));
        
        builder.AddModules(/* test configuration */);
        
        Container = builder.Build();
    }
}
```

## 🔧 Migration from Legacy Systems

### From StructureMap

```csharp
// Old StructureMap Registry
public class OldRegistry : Registry
{
    public OldRegistry()
    {
        For<IRepository>().Use<SqlRepository>().Singleton();
        For<IService>().Use<Service>().HttpContextScoped();
    }
}

// New FlexKit Module
public class NewModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<SqlRepository>().As<IRepository>().SingleInstance();
        builder.RegisterType<Service>().As<IService>().InstancePerLifetimeScope();
    }
}
```

### From Microsoft.Extensions.Configuration Only

```csharp
// Before: Standard configuration
services.Configure<ApiOptions>(Configuration.GetSection("Api"));

// After: FlexKit with dynamic access
public class ApiService(IFlexConfig config)
{
    public void CallApi()
    {
        dynamic api = config;
        var key = api.Api.Key;          // Direct dynamic access
        var url = api.Api.BaseUrl;      // No need for Options pattern
        var timeout = api.Api.Timeout;  // Natural property syntax
    }
}
```

## 📊 Performance Considerations

### Configuration Access Patterns

```csharp
public class PerformanceOptimizedService
{
    private readonly string _apiKey;
    private readonly string _baseUrl;
    private readonly int _timeout;
    
    // Best Practice: Cache configuration values in constructor
    public PerformanceOptimizedService(IFlexConfig config)
    {
        // Dynamic access has runtime overhead - cache frequently used values
        dynamic settings = config;
        _apiKey = settings.External.Api.Key;
        _baseUrl = settings.External.Api.BaseUrl;
        _timeout = settings.External.Api.Timeout;
    }
    
    public async Task CallApiAsync()
    {
        // Use cached values in performance-critical paths
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromMilliseconds(_timeout);
        client.DefaultRequestHeaders.Add("X-API-Key", _apiKey);
        
        await client.GetStringAsync($"{_baseUrl}/data");
    }
}
```

### Assembly Scanning Optimization

```csharp
// Specific assembly names for faster startup
{
  "Application": {
    "Mapping": {
      "Names": ["MyCompany.Services", "MyCompany.Data"] // Faster than prefix scanning
    }
  }
}

// Or use prefix for broader but still targeted scanning
{
  "Application": {
    "Mapping": {
      "Prefix": "MyCompany" // Good balance of convenience and performance
    }
  }
}
```

## 🛠️ Best Practices

### 1. Configuration Organization

```csharp
// Group related settings
{
  "Database": {
    "ConnectionString": "...",
    "CommandTimeout": 30,
    "MaxRetryCount": 3
  },
  "External": {
    "PaymentApi": { /* payment settings */ },
    "NotificationApi": { /* notification settings */ }
  },
  "Features": {
    "EnableCaching": true,
    "EnableMetrics": true
  }
}
```

### 2. Environment-Specific Configuration

```csharp
// Use hierarchical overrides
// appsettings.json (base)
// appsettings.Development.json (dev overrides)
// appsettings.Production.json (prod overrides)
// Environment variables (highest priority)

builder.AddFlexConfig(config => config
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile($"appsettings.{env}.json", optional: true)
    .AddDotEnvFile(".env", optional: true)
    .AddEnvironmentVariables()); // Highest precedence
```

### 3. Security Best Practices

```csharp
// ❌ Don't store secrets in appsettings.json
{
  "Api": {
    "Key": "secret-api-key-123" // Bad - committed to source control
  }
}

// ✅ Use environment variables or secure providers
{
  "Api": {
    "Key": "{{API_KEY}}" // Placeholder, actual value from environment
  }
}

// Environment variable: API_KEY=actual-secret-key
```

### 4. Validation and Error Handling

```csharp
public class ValidatedModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.Register(c =>
        {
            var config = c.Resolve<IFlexConfig>();
            
            // Validate configuration at startup
            var connectionString = config["ConnectionStrings:DefaultConnection"]
                ?? throw new InvalidOperationException("Database connection string is required");
            
            var timeout = config["Database:CommandTimeout"].ToType<int>();
            if (timeout <= 0)
                throw new InvalidOperationException("Database timeout must be positive");
            
            return new DatabaseService(connectionString, TimeSpan.FromSeconds(timeout));
        }).As<IDatabaseService>().SingleInstance();
    }
}
```

## 🔗 Integration with ASP.NET Core

### Startup Configuration

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Configure Autofac
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

// Configure FlexConfig
builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{
    containerBuilder.AddFlexConfig(config => config
        .AddJsonFile("appsettings.json", optional: false)
        .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
        .AddDotEnvFile(".env", optional: true)
        .AddEnvironmentVariables());
    
    containerBuilder.AddModules(builder.Configuration);
});

var app = builder.Build();

// Use FlexConfig in middleware
app.Use(async (context, next) =>
{
    var config = context.RequestServices.GetRequiredService<IFlexConfig>();
    dynamic settings = config;
    
    if (settings.Features.EnableRequestLogging)
    {
        // Log request details
    }
    
    await next();
});
```

### Controller Integration

```csharp
[ApiController]
[Route("api/[controller]")]
public class ConfigController(IFlexConfig config) : ControllerBase
{
    [HttpGet("features")]
    public IActionResult GetFeatures()
    {
        dynamic features = config;
        
        return Ok(new
        {
            Caching = features.Features.EnableCaching,
            Metrics = features.Features.EnableMetrics,
            NewUI = features.Features.NewUIEnabled
        });
    }
    
    [HttpGet("health")]
    public IActionResult Health()
    {
        // Use configuration for health check endpoints
        var endpoints = config["External:HealthChecks:Endpoints"]
            .GetCollection<string>();
        
        return Ok(new { Endpoints = endpoints });
    }
}
```

## 📚 Requirements

- **.NET 9.0** or later
- **C# 13.0** or later
- **Autofac 8.3.0** or later
- **Microsoft.Extensions.Configuration 9.0** or later

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request. For major changes, please open an issue first to discuss what you would like to change.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Built on top of Microsoft.Extensions.Configuration
- Integrated with Autofac dependency injection container
- Inspired by twelve-factor app configuration principles
- Follows FlexKit coding standards and best practices
