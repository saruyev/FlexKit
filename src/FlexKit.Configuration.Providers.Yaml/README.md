# FlexKit.Configuration.Providers.Yaml

**Modern YAML configuration provider for .NET applications with comprehensive YAML support, comments, multi-line strings, and seamless FlexKit integration.**

[![NuGet Version](https://img.shields.io/nuget/v/FlexKit.Configuration.Providers.Yaml)](https://www.nuget.org/packages/FlexKit.Configuration.Providers.Yaml)
[![Downloads](https://img.shields.io/nuget/dt/FlexKit.Configuration.Providers.Yaml)](https://www.nuget.org/packages/FlexKit.Configuration.Providers.Yaml)

## Why YAML for Configuration?

YAML provides superior readability and maintainability compared to JSON, especially for complex application configurations. This provider brings full YAML support to FlexKit.Configuration with zero-ceremony setup.

### The Problem with JSON Configuration

```json
{
  "Database": {
    "ConnectionString": "Server=localhost;Database=MyApp;",
    "Providers": {
      "Primary": {
        "Host": "primary-db.example.com",
        "Port": 5432
      }
    }
  },
  "Features": {
    "EnableNewDashboard": true
  }
}
```

### The YAML Solution

```yaml
# Application Database Configuration
Database:
  # Primary connection (use environment variables for production)
  ConnectionString: "Server=localhost;Database=MyApp;"
  
  # Multiple database providers
  Providers:
    Primary:
      Host: "primary-db.example.com"
      Port: 5432
      
    Secondary:
      Host: "backup-db.example.com"  
      Port: 5433
      ReadOnly: true

# Feature flags with documentation
Features:
  EnableNewDashboard: true  # New React dashboard
  EnableAnalytics: true     # Google Analytics integration
  
  # Complex feature configuration
  Search:
    Provider: "Elasticsearch"
    IndexPrefix: "myapp_"
    MaxResults: 1000
```

## YAML Advantages

- **🗨️ Comments**: Document configuration directly in files
- **📖 Readability**: Clean, indented syntax without brackets and quotes
- **📝 Multi-line strings**: Perfect for descriptions, scripts, and templates
- **🏗️ Natural hierarchy**: Express complex structures intuitively
- **🎯 Less verbose**: Reduce configuration file size by 20-40%
- **👥 Team friendly**: Easier code reviews and collaboration

## Quick Start

### 1. Installation

```bash
dotnet add package FlexKit.Configuration.Providers.Yaml
```

### 2. Basic Setup

```csharp
using FlexKit.Configuration.Providers.Yaml.Extensions;

var builder = Host.CreateDefaultBuilder(args);

builder.AddFlexConfig(config =>
{
    // Add YAML files in order of precedence
    config.AddYamlFile("appsettings.yaml", optional: false)          // Base config
          .AddYamlFile($"appsettings.{env}.yaml", optional: true)    // Environment overrides
          .AddYamlFile("features.yaml", optional: true)              // Feature flags
          .AddEnvironmentVariables();                                // Highest precedence
});

var host = builder.Build();
```

### 3. Using YAML Configuration

```csharp
var flexConfig = host.Services.GetRequiredService<IFlexConfig>();

// Dynamic access to YAML configuration
dynamic config = flexConfig;
var dbHost = config.Database.Providers.Primary.Host;
var enableNewDashboard = config.Features.EnableNewDashboard;

// Direct key access with type conversion
var maxResults = flexConfig["Features:Search:MaxResults"].ToType<int>();
var isReadOnly = flexConfig["Database:Providers:Secondary:ReadOnly"].ToType<bool>();
```

## YAML Configuration Features

### 📝 Comments and Documentation

```yaml
# Application Configuration
# This file contains the main application settings
# Edit carefully as changes require application restart

Application:
  Name: "My Application"
  Version: "2.1.0"
  
  # Debug settings (development only)
  Debug:
    EnableVerboseLogging: false  # Set to true for detailed logs
    ShowExceptionDetails: true   # Display full stack traces
```

### 📄 Multi-line Strings

```yaml
Database:
  # Literal style preserves line breaks and formatting
  InitializationScript: |
    CREATE TABLE IF NOT EXISTS users (
      id SERIAL PRIMARY KEY,
      username VARCHAR(50) UNIQUE NOT NULL,
      email VARCHAR(255) UNIQUE NOT NULL,
      created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
    );
    
    CREATE INDEX IF NOT EXISTS idx_users_email ON users(email);
    CREATE INDEX IF NOT EXISTS idx_users_created_at ON users(created_at);

Application:
  # Folded style creates a single line with spaces
  Description: >
    This is a comprehensive application that demonstrates
    FlexKit.Configuration with YAML support. It includes
    advanced features like multi-line strings, complex
    hierarchies, and environment-specific overrides.
```

### 🏗️ Complex Hierarchical Structures

```yaml
# External API Configurations
ExternalApis:
  PaymentGateway:
    Name: "Stripe Payment Gateway"
    BaseUrl: "https://api.stripe.com/v1"
    Timeout: 30000
    RetryCount: 3
    
    # Authentication configuration
    Authentication:
      Type: "Bearer"
      TokenEndpoint: "https://connect.stripe.com/oauth/token"
      Scope: "payments:read payments:write"
    
    # Circuit breaker settings
    CircuitBreaker:
      FailureThreshold: 5
      RecoveryTimeoutMs: 30000
      MonitoringPeriodMs: 60000
      
    # Rate limiting
    RateLimit:
      RequestsPerMinute: 1000
      BurstLimit: 50

  NotificationService:
    Name: "SendGrid Email Service"
    BaseUrl: "https://api.sendgrid.com/v3"
    
    # Multiple delivery channels
    Channels:
      Email:
        Provider: "SendGrid"
        DefaultFromAddress: "noreply@example.com"
        Templates:
          Welcome: "d-abc123def456"
          PasswordReset: "d-def456ghi789"
          
      SMS:
        Provider: "Twilio"
        DefaultFromNumber: "+1-555-0123"
        
      Push:
        Provider: "Firebase"
        BatchSize: 1000
```

### 📊 Arrays and Collections

```yaml
# Server Configuration Array
Servers:
  - Name: "Web Server 1"
    Host: "web1.example.com"
    Port: 8080
    IsActive: true
    Environment: "Production"
    Tags: ["web", "primary", "load-balanced"]
    
    # Nested resource configuration
    Resources:
      Cpu: "4 cores"
      Memory: "8GB"
      Storage: "100GB SSD"
      
  - Name: "API Server"
    Host: "api.example.com"
    Port: 8090
    IsActive: true
    Environment: "Production"
    Tags: ["api", "microservices", "high-performance"]
    
    Resources:
      Cpu: "8 cores"
      Memory: "16GB"
      Storage: "200GB NVMe"

# Feature Flag Collections
FeatureFlags:
  # Core application features
  Core:
    - "EnableNewDashboard"
    - "EnableUserProfiles"
    - "EnableNotifications"
    
  # Experimental features (typically disabled)
  Experimental:
    - "EnableBetaUI"
    - "EnableMachineLearning"
    - "EnableRealTimeUpdates"

# Security Configuration
Security:
  Cors:
    AllowedOrigins:
      - "https://app.example.com"
      - "https://www.example.com"
      - "https://admin.example.com"
    AllowedMethods: ["GET", "POST", "PUT", "DELETE", "OPTIONS"]
    AllowedHeaders: ["Content-Type", "Authorization", "X-Requested-With"]
```

### 🔗 Anchors and Aliases for Reusability

```yaml
# Define reusable configurations with anchors
defaults: &default_database
  CommandTimeout: 30
  MaxRetries: 3
  EnableSSL: true

# Environment-specific database configurations
Database:
  Development:
    <<: *default_database          # Inherit defaults
    Host: "dev-db.example.com"
    Database: "myapp_dev"
    EnableSSL: false               # Override default
    
  Staging:
    <<: *default_database          # Inherit defaults
    Host: "staging-db.example.com"
    Database: "myapp_staging"
    CommandTimeout: 60             # Override default
    
  Production:
    <<: *default_database          # Inherit defaults
    Host: "prod-db.example.com"
    Database: "myapp_production"
    MaxRetries: 5                  # Override default
```

## FlexKit Integration Examples

### Dynamic Configuration Access

```csharp
public class ConfigurationService(IFlexConfig flexConfig)
{
    public void DemonstrateYamlAccess()
    {
        dynamic config = flexConfig;
        
        // Access YAML comments and descriptions
        var appDescription = config?.Application?.Description as string;
        Console.WriteLine($"App Description: {appDescription}");
        
        // Navigate complex hierarchical structures
        var paymentApiUrl = config?.ExternalApis?.PaymentGateway?.BaseUrl;
        var circuitBreakerThreshold = config?.ExternalApis?.PaymentGateway?.CircuitBreaker?.FailureThreshold;
        
        // Access array elements with natural syntax
        var firstServerName = config?.Servers?[0]?.Name;
        var firstServerTags = config?.Servers?[0]?.Tags;
        
        Console.WriteLine($"Payment API: {paymentApiUrl}");
        Console.WriteLine($"Circuit Breaker Threshold: {circuitBreakerThreshold}");
        Console.WriteLine($"First Server: {firstServerName}");
    }
}
```

### Strongly Typed Configuration

```csharp
// Configuration classes that map to YAML structure
public class DatabaseConfig
{
    public string ConnectionString { get; set; } = string.Empty;
    public int CommandTimeout { get; set; }
    public int MaxRetries { get; set; }
    public Dictionary<string, DatabaseProvider> Providers { get; set; } = new();
}

public class ExternalApiConfig
{
    public PaymentGatewayConfig PaymentGateway { get; set; } = new();
    public NotificationServiceConfig NotificationService { get; set; } = new();
}

public class PaymentGatewayConfig
{
    public string Name { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public int Timeout { get; set; }
    public AuthenticationConfig Authentication { get; set; } = new();
    public CircuitBreakerConfig CircuitBreaker { get; set; } = new();
    public RateLimitConfig RateLimit { get; set; } = new();
}

// Automatic registration and injection
builder.AddFlexConfig(config =>
{
    config.AddYamlFile("appsettings.yaml", optional: false)
          .AddYamlFile("features.yaml", optional: true);
});

public class PaymentService(ExternalApiConfig apiConfig)
{
    public async Task<PaymentResult> ProcessPaymentAsync(Payment payment)
    {
        var config = apiConfig.PaymentGateway;
        var timeout = TimeSpan.FromMilliseconds(config.Timeout);
        
        // Use strongly typed configuration
        using var httpClient = CreateHttpClient(config.BaseUrl, timeout);
        
        // Configure circuit breaker from YAML
        var circuitBreaker = new CircuitBreakerPolicy(
            config.CircuitBreaker.FailureThreshold,
            TimeSpan.FromMilliseconds(config.CircuitBreaker.RecoveryTimeoutMs)
        );
        
        return await circuitBreaker.ExecuteAsync(async () =>
        {
            return await ProcessPaymentWithApi(httpClient, payment);
        });
    }
}
```

### Type Conversion with YAML Values

```csharp
public void DemonstrateTypeConversion(IFlexConfig flexConfig)
{
    // Basic type conversions from YAML
    var timeout = flexConfig["ExternalApis:PaymentGateway:Timeout"].ToType<int>();
    var enableSSL = flexConfig["Database:Development:EnableSSL"].ToType<bool>();
    var retryCount = flexConfig["Database:Production:MaxRetries"].ToType<int>();
    
    // TimeSpan conversion
    var recoveryTimeout = flexConfig["ExternalApis:PaymentGateway:CircuitBreaker:RecoveryTimeoutMs"].ToType<TimeSpan>();
    
    // Array processing from YAML
    var allowedOrigins = flexConfig["Security:Cors:AllowedOrigins"].GetCollection<string>();
    var featureFlags = flexConfig["FeatureFlags:Core"].GetCollection<string>();
    
    // Safe nullable conversions
    var optionalSetting = flexConfig["NonExistent:Setting"].ToType<int?>();
    
    Console.WriteLine($"API Timeout: {timeout}ms");
    Console.WriteLine($"SSL Enabled: {enableSSL}");
    Console.WriteLine($"Recovery Timeout: {recoveryTimeout}");
    Console.WriteLine($"Allowed Origins: [{string.Join(", ", allowedOrigins ?? new string[0])}]");
    Console.WriteLine($"Optional Setting: {optionalSetting}");
}
```

## Environment-Specific Configuration

### Base Configuration (appsettings.yaml)

```yaml
# Production defaults
Application:
  Name: "My Application"
  Environment: "Production"

Database:
  ConnectionString: "Server=prod-db;Database=MyApp;Integrated Security=true;"
  CommandTimeout: 30
  MaxRetries: 3

Logging:
  LogLevel:
    Default: "Information"
    Microsoft: "Warning"
    
Features:
  EnableNewDashboard: true
  EnableAnalytics: true
  EnableAdvancedSearch: false
```

### Development Overrides (appsettings.Development.yaml)

```yaml
# Development environment overrides
Application:
  Environment: "Development"

Database:
  # Local development database
  ConnectionString: "Server=localhost;Database=MyApp_Dev;Integrated Security=true;"
  CommandTimeout: 120  # Longer timeout for debugging

Logging:
  # More verbose logging for development
  LogLevel:
    Default: "Debug"
    Microsoft: "Information"
    MyApplication: "Trace"

Features:
  # Enable all features in development
  EnableNewDashboard: true
  EnableAnalytics: false      # Disable analytics in development
  EnableAdvancedSearch: true  # Enable experimental features

# Development-specific settings
Development:
  EnableHotReload: true
  EnableDetailedErrors: true
  EnableTestData: true
  
  # Development tools
  Tools:
    EnableSwagger: true
    EnableMiniProfiler: true
    EnableSqlLogging: true
```

### Feature-Specific Configuration (features.yaml)

```yaml
# Feature Flags and Configuration
FeatureFlags:
  # Core features
  EnableUserProfiles: true
  EnableNotifications: true
  EnableReporting: true
  
  # Experimental features
  Experimental:
    EnableBetaUI: false
    EnableMachineLearning: false
    EnableRealTimeUpdates: true

# Feature-specific settings
Features:
  UserProfiles:
    EnableProfilePictures: true
    MaxProfilePictureSize: 5242880  # 5MB
    AllowedFormats: ["jpg", "jpeg", "png", "gif"]
    
  Notifications:
    DefaultChannel: "email"
    BatchSize: 100
    RetryCount: 3
    
    Channels:
      Email:
        Provider: "SendGrid"
        Templates:
          Welcome: "d-abc123"
          PasswordReset: "d-def456"
          
      Push:
        Provider: "Firebase"
        BatchSize: 1000
        
  Reporting:
    DefaultFormat: "pdf"
    MaxReportSize: 52428800  # 50MB
    RetentionDays: 90
    
    # Scheduled reports
    ScheduledReports:
      - Name: "Daily Summary"
        Schedule: "0 6 * * *"    # 6 AM daily
        Recipients: ["admin@example.com"]
        
      - Name: "Weekly Analytics"
        Schedule: "0 8 * * 1"    # 8 AM Mondays
        Recipients: ["analytics@example.com", "management@example.com"]
```

## Configuration Precedence

FlexKit processes YAML files in the order they're added, with later sources overriding earlier ones:

```csharp
builder.AddFlexConfig(config =>
{
    // 1. Base configuration (lowest precedence)
    config.AddYamlFile("appsettings.yaml", optional: false)
    
    // 2. Environment-specific overrides
          .AddYamlFile($"appsettings.{environment}.yaml", optional: true)
    
    // 3. Feature-specific configuration
          .AddYamlFile("features.yaml", optional: true)
          .AddYamlFile("database.yaml", optional: true)
    
    // 4. Environment variables (higher precedence)
          .AddEnvironmentVariables()
    
    // 5. Command line arguments (highest precedence)
          .AddCommandLine(args);
});
```

**Precedence Order (lowest to highest):**
1. **appsettings.yaml** - Base configuration
2. **appsettings.{Environment}.yaml** - Environment overrides
3. **Feature-specific YAML files** - Modular configuration
4. **Environment variables** - Container/deployment settings
5. **Command line arguments** - Runtime overrides

## Performance Considerations

FlexKit.Configuration.Providers.Yaml is optimized for **startup performance over runtime performance**, following the FlexKit philosophy.

### Performance Characteristics

Based on comprehensive benchmarks (see `/benchmarks/FlexKit.Configuration.Providers.Yaml.PerformanceTests/`):

| Configuration Type | YAML Loading Time | Memory Usage | vs JSON |
|---|---|---|---|
| **Simple** (< 50 keys) | ~266 μs | ~97KB | 25x slower |
| **Complex** (100+ keys) | ~823 μs | ~412KB | 78x slower |
| **Enterprise** (200+ keys) | ~2.5ms | ~1.3MB | 237x slower |

### Performance Recommendations

#### ✅ Use YAML When:
- **Application startup configuration** - One-time loading cost is acceptable
- **Development and staging** - Readability benefits outweigh performance costs
- **Complex hierarchical configs** - YAML's structure benefits justify overhead
- **Small to medium configs** - Performance impact is manageable (< 1ms)
- **Configuration with comments** - Documentation value is high

```csharp
// Perfect for application startup
public async Task<IHost> ConfigureApplicationAsync()
{
    var builder = Host.CreateDefaultBuilder();
    
    // YAML loading happens once at startup
    builder.AddFlexConfig(config =>
    {
        config.AddYamlFile("appsettings.yaml", optional: false)
              .AddYamlFile("features.yaml", optional: true)
              .AddYamlFile("database.yaml", optional: true);
    });
    
    return builder.Build();
}
```

#### ⚡ Optimize Performance:
- **Cache loaded configurations** - Avoid repeated YAML parsing
- **Use mixed approaches** - YAML for readability, JSON for large configs
- **Load during startup** - Parse YAML once during application initialization
- **Monitor memory usage** - Watch GC pressure with large YAML files

```csharp
// Optimal mixed approach
builder.AddFlexConfig(config =>
{
    // Human-readable YAML for core settings
    config.AddYamlFile("appsettings.yaml", optional: false)
          
    // JSON for large datasets and performance-critical configs
          .AddJsonFile("large-config.json", optional: true)
          
    // Environment variables for production overrides
          .AddEnvironmentVariables();
});
```

### YAML Feature Performance

| YAML Feature | Overhead | Memory Impact | Recommendation |
|---|---|---|---|
| **Comments** | 0% | None | Use freely for documentation |
| **Multi-line strings** | +9% | +18% | Safe for scripts and descriptions |
| **Anchors & aliases** | +5% | +42% | Efficient for configuration reuse |
| **Deep nesting** | +15% | +83% | Limit to 6 levels when possible |
| **Complex arrays** | +25% | +151% | Most expensive - use judiciously |
| **Mixed data types** | +16% | +89% | Use consistent types when possible |

## Best Practices

### 🏗️ Configuration Organization

```yaml
# Group related settings logically
Application:
  Name: "My Application"
  Version: "1.0.0"
  Environment: "Production"

# Use comments for complex sections
Database:
  # Primary database connection
  # Update the connection string for different environments
  ConnectionString: "Server=localhost;Database=MyApp;"
  
  # Connection behavior settings
  CommandTimeout: 30
  MaxRetries: 3
  
  # Multiple database providers for different purposes
  Providers:
    Primary:
      Host: "primary-db.example.com"
      Port: 5432
    
    ReadReplica:
      Host: "replica-db.example.com"
      Port: 5432
      ReadOnly: true

# Separate concerns into logical sections
ExternalServices:
  PaymentGateway: { /* payment config */ }
  EmailService: { /* email config */ }
  
Features:
  FeatureFlags: { /* feature toggles */ }
  Settings: { /* feature configuration */ }
```

### 🔒 Security Guidelines

```yaml
# ❌ Never commit secrets to version control
Database:
  # Use environment variables for sensitive data
  ConnectionString: "Server=${DB_HOST};Database=${DB_NAME};User=${DB_USER};Password=${DB_PASSWORD};"
  
# ✅ Document security requirements in comments
Api:
  # Set API_KEY environment variable in production
  # Development: use test key from developer portal
  # Production: use secure key from Azure Key Vault
  ApiKey: "${API_KEY}"
  
Security:
  # JWT secret must be at least 256 bits
  # Generate with: openssl rand -base64 32
  JwtSecret: "${JWT_SECRET}"
  
  # Allowed origins for CORS
  # Add production domains before deployment
  Cors:
    AllowedOrigins:
      - "https://myapp.com"
      - "https://www.myapp.com"
      - "https://admin.myapp.com"
```

### 📁 File Organization Strategies

#### Option 1: Single File Approach
```
appsettings.yaml              # All configuration in one file
appsettings.Development.yaml  # Development overrides
appsettings.Production.yaml   # Production overrides
```

#### Option 2: Modular Approach (Recommended)
```
appsettings.yaml           # Core application settings
database.yaml             # Database configuration
features.yaml             # Feature flags and settings
external-services.yaml    # Third-party integrations
security.yaml             # Security and authentication
monitoring.yaml           # Logging and monitoring

# Environment-specific overrides
appsettings.Development.yaml
database.Development.yaml
features.Development.yaml
```

#### Option 3: Domain-Driven Approach
```
core/
  application.yaml         # Core application settings
  database.yaml           # Data layer configuration
  
features/
  user-management.yaml    # User-related features
  reporting.yaml          # Reporting features
  analytics.yaml          # Analytics features
  
infrastructure/
  logging.yaml            # Logging configuration
  monitoring.yaml         # Health checks and metrics
  security.yaml           # Security settings
```

### 🧪 Testing Strategies

```csharp
[Test]
public void Should_Load_YAML_Configuration()
{
    // Arrange
    var yamlContent = """
        Database:
          ConnectionString: "TestConnectionString"
          CommandTimeout: 45
        
        Features:
          EnableNewDashboard: true
          EnableAnalytics: false
        
        # Test array configuration
        Servers:
          - Name: "Test Server"
            Host: "test.example.com"
            Port: 8080
            IsActive: true
        """;
    
    var config = new FlexConfigurationBuilder()
        .AddYamlStream(new MemoryStream(Encoding.UTF8.GetBytes(yamlContent)))
        .Build();

    // Act
    dynamic flexConfig = config;
    var connectionString = flexConfig?.Database?.ConnectionString as string;
    var commandTimeout = config["Database:CommandTimeout"].ToType<int>();
    var dashboardEnabled = config["Features:EnableNewDashboard"].ToType<bool>();
    
    // Assert
    Assert.Equal("TestConnectionString", connectionString);
    Assert.Equal(45, commandTimeout);
    Assert.True(dashboardEnabled);
}

[Test]
public void Should_Handle_Complex_YAML_Structures()
{
    var yamlContent = """
        ExternalApis:
          PaymentGateway:
            Authentication:
              Type: "Bearer"
              TokenEndpoint: "https://auth.example.com/token"
            RateLimit:
              RequestsPerMinute: 1000
              BurstLimit: 50
        """;
        
    var config = new FlexConfigurationBuilder()
        .AddYamlStream(new MemoryStream(Encoding.UTF8.GetBytes(yamlContent)))
        .Build();
        
    dynamic flexConfig = config;
    var authType = flexConfig?.ExternalApis?.PaymentGateway?.Authentication?.Type;
    var rateLimit = config["ExternalApis:PaymentGateway:RateLimit:RequestsPerMinute"].ToType<int>();
    
    Assert.Equal("Bearer", authType);
    Assert.Equal(1000, rateLimit);
}
```

## Migration from JSON to YAML

### Step 1: Convert Existing JSON

```bash
# Use online converters or tools like yq
yq eval -P '.Configuration' appsettings.json > appsettings.yaml
```

### Step 2: Add Comments and Improve Structure

```yaml
# Before (JSON-like YAML)
Database:
  ConnectionString: "Server=localhost;Database=MyApp;"
  CommandTimeout: 30

# After (Idiomatic YAML with comments)
Database:
  # Connection string for the primary database
  # Use environment variables in production: ${DB_CONNECTION_STRING}
  ConnectionString: "Server=localhost;Database=MyApp;"
  
  # Query timeout in seconds (default: 30)
  CommandTimeout: 30
  
  # Maximum number of retry attempts for failed connections
  MaxRetries: 3
```

### Step 3: Update Configuration Loading

```csharp
// Before (JSON only)
builder.AddFlexConfig(config =>
{
    config.AddJsonFile("appsettings.json")
          .AddJsonFile($"appsettings.{env}.json", optional: true);
});

// After (Mixed JSON and YAML)
builder.AddFlexConfig(config =>
{
    config.AddYamlFile("appsettings.yaml")                           // New YAML base
          .AddYamlFile($"appsettings.{env}.yaml", optional: true)    // YAML environment overrides
          .AddJsonFile("legacy-config.json", optional: true)         // Keep existing JSON if needed
          .AddEnvironmentVariables();
});
```

## Troubleshooting

### Common Issues

**YAML Parsing Errors**
```yaml
# ❌ Incorrect indentation
Database:
  ConnectionString: "..."
 CommandTimeout: 30  # Wrong indentation

# ✅ Correct indentation  
Database:
  ConnectionString: "..."
  CommandTimeout: 30     # Consistent 2-space indentation
```

**Quote Handling**
```yaml
# ❌ Inconsistent quoting
Database:
  Host: unquoted-host
  Port: "5432"         # Numbers don't need quotes
  
# ✅ Consistent approach
Database:  
  Host: "quoted-host"  # Quote strings for consistency
  Port: 5432           # Numbers without quotes
```

**Environment Variables**
```yaml
# ❌ YAML doesn't expand variables by default
Database:
  ConnectionString: "${DB_CONNECTION}"  # Won't expand

# ✅ Use environment variable configuration source
Database:
  ConnectionString: "default-connection"  # Default in YAML
  # Override with DB__CONNECTIONSTRING environment variable
```

**Multi-line Strings**
```yaml
# ❌ Incorrect multi-line syntax
Description: "This is a
long description"  # Will cause parsing error

# ✅ Correct multi-line syntax
Description: |
  This is a long description
  that spans multiple lines
  and preserves formatting.
```

### Performance Issues

**Large Configuration Files**
```yaml
# ❌ Very large single YAML file (slow to parse)
# appsettings.yaml with 500+ keys

# ✅ Split into smaller, focused files
# appsettings.yaml (core settings)
# database.yaml (database config)
# features.yaml (feature flags)
```

**Complex Nested Structures**
```yaml
# ❌ Overly deep nesting (impacts performance)
Level1:
  Level2:
    Level3:
      Level4:
        Level5:
          Level6:
            Value: "too deep"

# ✅ Flattened structure when possible
Database_Primary_Host: "host1"
Database_Primary_Port: 5432
Database_Backup_Host: "host2" 
Database_Backup_Port: 5433
```

## Integration Examples

Complete working examples are available in the samples:

- **[FlexKitConfigurationConsoleApp](../../../samples/FlexKitConfigurationConsoleApp/)**: Comprehensive YAML demonstration
- **appsettings.yaml**: Main application configuration with comments
- **features.yaml**: Feature flags and complex configuration structures
- **database.yaml**: Database configuration with multiple providers
- **services.Development.yaml**: Environment-specific YAML overrides

## Related Documentation

- **[FlexKit.Configuration](../FlexKit.Configuration/README.md)**: Core configuration features and concepts
- **[Performance Benchmarks](../../benchmarks/FlexKit.Configuration.Providers.Yaml.PerformanceTests/README.md)**: Detailed performance analysis
- **[FlexKit Philosophy](../../README.md)**: Overall FlexKit design principles

## Contributing

FlexKit.Configuration.Providers.Yaml welcomes contributions! Areas of interest:

- **YAML Feature Support**: Advanced YAML features and syntax patterns
- **Performance Optimizations**: Parsing and memory usage improvements
- **Developer Tools**: YAML validation, schema generation, IDE integration
- **Documentation**: Examples, tutorials, and migration guides

## License

This project is licensed under the MIT License - see the [LICENSE](../../LICENSE) file for details.

---

**FlexKit.Configuration.Providers.Yaml**: Because configuration should be readable, maintainable, and just work. ✨