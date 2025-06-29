# FlexKit.Configuration.Providers.Yaml

YAML configuration provider for FlexKit.Configuration, enabling hierarchical YAML configuration files with full integration into the FlexKit configuration system.

## Features

- **🗂️ Hierarchical Configuration**: Full support for nested objects and arrays like JSON
- **📝 Human-Readable**: YAML's clean syntax with comment support
- **🔄 FlexKit Integration**: Seamless integration with FlexConfigurationBuilder
- **⚙️ Optional Files**: Support for both required and optional YAML files
- **🌍 Multi-Environment**: Environment-specific YAML file support
- **🛡️ Type Safety**: Works with FlexKit's strongly-typed configuration and dynamic access

## Installation

```bash
dotnet add package FlexKit.Configuration.Providers.Yaml
```

## Quick Start

### Basic Usage

```csharp
using FlexKit.Configuration.Core;
using FlexKit.Configuration.Providers.Yaml.Extensions;

var config = new FlexConfigurationBuilder()
    .AddYamlFile("appsettings.yaml")
    .Build();

// Dynamic access
dynamic settings = config;
var dbHost = settings.Database.Host;
var apiKey = settings.Api.Key;
```

### Integration with ASP.NET Core

```csharp
using Autofac;
using Autofac.Extensions.DependencyInjection;
using FlexKit.Configuration.Core;
using FlexKit.Configuration.Providers.Yaml.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{
    containerBuilder.AddFlexConfig(config => config
        .AddYamlFile("appsettings.yaml", optional: false)
        .AddYamlFile($"appsettings.{builder.Environment.EnvironmentName}.yaml", optional: true)
        .AddEnvironmentVariables());
});

var app = builder.Build();
```

### Example YAML Configuration

```yaml
# appsettings.yaml
application:
  name: "My Application"
  version: "1.0.0"
  debug: true

database:
  host: localhost
  port: 5432
  name: myapp_db
  pool:
    min: 5
    max: 20

api:
  baseUrl: "https://api.example.com"
  timeout: 5000
  retries: 3

features:
  - caching
  - logging
  - metrics

endpoints:
  auth:
    url: "https://auth.example.com"
    timeout: 3000
  payment:
    url: "https://payment.example.com"  
    timeout: 10000
```

### Accessing Configuration

```csharp
public class ApiService(IFlexConfig config)
{
    public async Task<string> CallApiAsync()
    {
        // Dynamic access
        dynamic settings = config;
        var baseUrl = settings.Api.BaseUrl;
        var timeout = settings.Api.Timeout;
        var retries = settings.Api.Retries;
        
        // Traditional access
        var authUrl = config["Endpoints:Auth:Url"];
        
        // Type-safe conversion
        var timeoutMs = config["Api:Timeout"].ToType<int>();
        var enabledFeatures = config["Features"].GetCollection<string>();
        
        // Use configuration...
    }
}
```

### Strongly Typed Configuration

```csharp
// Configuration classes
public class DatabaseConfig
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 5432;
    public string Name { get; set; } = string.Empty;
    public PoolConfig Pool { get; set; } = new();
}

public class PoolConfig
{
    public int Min { get; set; } = 5;
    public int Max { get; set; } = 20;
}

// Registration
builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{
    containerBuilder.AddFlexConfig(config => config
        .AddYamlFile("appsettings.yaml")
        .AddEnvironmentVariables())
        .RegisterConfig<DatabaseConfig>("Database");
});

// Usage
public class DatabaseService(DatabaseConfig dbConfig)
{
    public async Task ConnectAsync()
    {
        var connectionString = $"Host={dbConfig.Host};Port={dbConfig.Port};Database={dbConfig.Name}";
        var minPoolSize = dbConfig.Pool.Min;
        var maxPoolSize = dbConfig.Pool.Max;
        
        // Connect to database...
    }
}
```

## Configuration Precedence

YAML files follow standard FlexKit precedence rules:

1. **Base YAML files** (appsettings.yaml)
2. **Environment-specific YAML files** (appsettings.Development.yaml)
3. **Environment variables** (highest precedence)

```csharp
var config = new FlexConfigurationBuilder()
    .AddYamlFile("appsettings.yaml", optional: false)              // Base config
    .AddYamlFile($"appsettings.{environment}.yaml", optional: true) // Environment overrides
    .AddEnvironmentVariables()                                     // Highest precedence
    .Build();
```

## YAML Features Supported

- **Hierarchical structure**: Nested objects and configuration sections
- **Arrays/Lists**: Collections of values or objects
- **Data types**: Strings, numbers, booleans, null values
- **Comments**: Documentation within configuration files
- **Multi-line strings**: Using YAML literal (`|`) and folded (`>`) styles
- **Quoted strings**: For strings containing special characters

## Error Handling

```csharp
try 
{
    var config = new FlexConfigurationBuilder()
        .AddYamlFile("config.yaml", optional: false)  // Required file
        .Build();
}
catch (FileNotFoundException ex)
{
    // Handle missing required file
}
catch (InvalidDataException ex)
{
    // Handle YAML parsing errors
}
```

## Best Practices

1. **File Extensions**: Use `.yaml` extension for consistency
2. **Required vs Optional**: Set `optional: false` for critical config files
3. **Environment Files**: Use optional environment-specific overrides
4. **Comments**: Document complex configuration sections
5. **Validation**: Validate YAML syntax in CI/CD pipelines
6. **Security**: Never commit sensitive data in YAML files

## Comparison with JSON

| Feature | JSON | YAML |
|---------|------|------|
| Comments | ❌ | ✅ |
| Multi-line strings | ❌ | ✅ |
| Human readability | ⚠️ | ✅ |
| Parsing performance | ✅ | ⚠️ |
| Tooling support | ✅ | ✅ |

Choose YAML for configuration files that benefit from comments and improved readability, especially for complex hierarchical configurations.

## Integration with Other Providers

```csharp
// Mixed configuration sources
var config = new FlexConfigurationBuilder()
    .AddJsonFile("appsettings.json")          // JSON for compatibility
    .AddYamlFile("features.yaml")             // YAML for complex feature config
    .AddDotEnvFile(".env")                    // Environment variables from file
    .AddEnvironmentVariables()                // System environment variables
    .Build();
```

## Project Structure

The FlexKit.Configuration.Providers.Yaml project follows the same structure and patterns as the main FlexKit.Configuration project:

```
FlexKit.Configuration.Providers.Yaml/
├── FlexKit.Configuration.Providers.Yaml.csproj
├── Sources/
│   ├── YamlConfigurationSource.cs
│   └── YamlConfigurationProvider.cs
├── Extensions/
│   └── FlexConfigurationBuilderYamlExtensions.cs
└── README.md
```

## Implementation Notes

This implementation follows all the patterns established by the existing FlexKit.Configuration codebase:

1. **Same Exception Handling**: Uses the same exception types and patterns as DotEnvConfigurationProvider
2. **Consistent API**: Extension method follows the same signature pattern as `AddDotEnvFile()`
3. **Documentation Style**: Comprehensive XML documentation matching the existing codebase style
4. **Null Safety**: Full nullable reference type support with proper annotations
5. **Case Sensitivity**: Uses `StringComparer.OrdinalIgnoreCase` for configuration keys like other providers
6. **Type Conversion**: Handles boolean values consistently with JSON provider (lowercase true/false)
7. **Error Messages**: Consistent error message formatting and exception wrapping

## Dependencies

- **YamlDotNet 16.1.3**: Mature, actively maintained YAML parser for .NET
- **Microsoft.Extensions.Configuration 9.0.0**: Core configuration abstractions
- **FlexKit.Configuration**: Reference to the main FlexKit configuration library

The implementation is designed to be a drop-in addition to any existing FlexKit.Configuration setup without requiring changes to existing code.
