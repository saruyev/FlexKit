# FlexKit.Logging

A high-performance, zero-configuration automatic method logging framework for .NET 9 applications with comprehensive interception capabilities, flexible formatting, and enterprise-grade data masking.

## Overview

FlexKit.Logging transforms method logging from a manual, performance-intensive task into an automatic, high-performance capability that requires zero configuration to get started. Built on the FlexKit philosophy of "install it and use it," the framework delivers **100x better runtime performance** compared to native Microsoft.Extensions.Logging while providing comprehensive method instrumentation.

### Key Features

- **Zero Configuration Setup**: Call `AddFlexConfig()` and automatic method interception begins immediately
- **Exceptional Performance**: 100x faster than native MEL (2.7μs vs 270μs), 81x faster than Log4Net
- **Automatic Interception**: Interface and virtual method detection with intelligent registration
- **Flexible Control**: Attribute-based (`[LogInput]`, `[LogBoth]`, `[NoLog]`) and configuration-based control
- **Enterprise Masking**: Comprehensive data protection for PII, payment data, and sensitive information
- **Multiple Formatters**: Five built-in formatters including JSON, structured, hybrid, and custom templates
- **Provider Support**: Works with MEL, Serilog, NLog, Log4Net, and ZLogger
- **Async Compatible**: Near-zero overhead for async operations with full distributed tracing support
- **Background Processing**: Non-blocking log processing with configurable queuing and batching

### Performance Highlights

Based on comprehensive benchmarks across multiple logging providers:

| Scenario | Native Framework | FlexKit | Improvement |
|----------|------------------|---------|-------------|
| MEL Manual Logging | 270.65 μs | 2.72 μs | **100x faster** |
| Log4Net Logging | 114.60 μs | 1.41 μs | **81x faster** |
| Serilog Active Logging | 136.1 μs | 140.5 μs | **Near-native** |
| ZLogger Enhancement | 21.78 μs | 18.35 μs | **16% faster** |
| NoLog Optimization | N/A | <1 μs | **94-99% faster** |

### Core Philosophy

FlexKit.Logging follows the FlexKit ecosystem principle: **maximum capability with minimal effort**. The framework automatically:

- Detects and registers Autofac modules
- Configures service provider factory
- Scans assemblies for interceptable services
- Applies intelligent interception strategies
- Integrates appsettings.json configuration
- Provides production-ready defaults

This approach eliminates the typical complexity of logging framework setup while delivering enterprise-grade performance and functionality.

## Quick Start

### Installation

```bash
dotnet add package FlexKit.Logging
```

### Basic Setup

```csharp
using FlexKit.Configuration.Core;
using Microsoft.Extensions.Hosting;

// Minimal setup - that's it!
var host = Host.CreateDefaultBuilder(args)
    .AddFlexConfig()  // Enables automatic method logging
    .Build();

await host.RunAsync();
```

### Automatic Method Logging

Once FlexKit.Logging is installed, all services with interfaces or virtual methods are automatically intercepted:

```csharp
// Interface-based service (automatically intercepted)
public interface IOrderService
{
    void ProcessOrder(string orderId);
    OrderResult GetOrderStatus(string orderId);
}

public class OrderService : IOrderService
{
    // Both methods automatically logged with input parameters
    public void ProcessOrder(string orderId) 
    {
        Console.WriteLine($"Processing order: {orderId}");
    }

    public OrderResult GetOrderStatus(string orderId) 
    {
        return new OrderResult { Id = orderId, Status = "Completed" };
    }
}

// Register in your DI container (done automatically by FlexKit)
// Usage generates automatic log entries:
// [INFO] Method OrderService.ProcessOrder called with: orderId="12345"
// [INFO] Method OrderService.GetOrderStatus called with: orderId="12345"
```

### Immediate Benefits

- **Zero configuration required** - logging starts immediately
- **100x performance improvement** over manual logging approaches
- **Comprehensive coverage** - all public methods automatically instrumented
- **Production ready** - intelligent defaults with enterprise-grade performance

## Core Concepts

### Interception Strategies

FlexKit.Logging uses intelligent interception based on service characteristics:

#### 1. Interface Interception (Recommended)
```csharp
public interface IPaymentService
{
    bool ProcessPayment(decimal amount);
}

public class PaymentService : IPaymentService  // Automatically intercepted
{
    public bool ProcessPayment(decimal amount) { return amount > 0; }
}
```

#### 2. Class Interception (Virtual Methods)
```csharp
public class PaymentService  // Automatically intercepted
{
    public virtual bool ProcessPayment(decimal amount) { return amount > 0; }
}
```

#### 3. Manual Logging (Full Control)
```csharp
public class PaymentService : IPaymentService
{
    private readonly IFlexKitLogger _logger;

    public PaymentService(IFlexKitLogger logger) // Excludes class from auto-interception
    {
        _logger = logger;
    }

    public bool ProcessPayment(decimal amount)
    {
        var entry = LogEntry.CreateStart(nameof(ProcessPayment), GetType().FullName!)
            .WithInput(new { amount });
        _logger.Log(entry);
        
        var result = amount > 0;
        _logger.Log(entry.WithCompletion(success: true).WithOutput(result));
        return result;
    }
}
```

### Configuration Philosophy

FlexKit.Logging follows a **layered configuration approach** with clear precedence:

1. **Attribute Configuration** (Highest Priority)
   ```csharp
   [LogBoth(target: "Console", level: LogLevel.Warning)]
   public void CriticalOperation() { }
   ```

2. **Service Pattern Configuration** (Medium Priority)
   ```json
   {
     "FlexKit": {
       "Logging": {
         "Services": {
           "MyApp.Services.*": { "LogInput": true, "Level": "Information" }
         }
       }
     }
   }
   ```

3. **Auto-Interception Defaults** (Lowest Priority)
    - LogInput behavior with Information level
    - Applies to all interceptable services

### Interception Requirements

**Can Be Intercepted:**
- Services implementing interfaces
- Classes with public virtual methods
- Services registered in the DI container

**Cannot Be Intercepted:**
- Sealed classes without interfaces
- Classes with only non-virtual methods
- Static classes
- Classes with `IFlexKitLogger` constructor injection (automatically excluded)

**Example of Non-Interceptable Service:**
```csharp
public class UtilityService  // Cannot be intercepted
{
    public void ProcessData(string data) { }  // Not virtual, no interface
}

// Solution: Add interface or make virtual
public interface IUtilityService
{
    void ProcessData(string data);
}

public class UtilityService : IUtilityService  // Now interceptable
{
    public void ProcessData(string data) { }
}
```

### Performance Characteristics

FlexKit.Logging achieves exceptional performance through:

- **Intelligent caching** of interception decisions
- **Background queue processing** for log entry handling
- **Minimal memory allocation** during method execution
- **Optimized reflection usage** with comprehensive caching
- **NoLog attribute** providing 94-99% performance improvement for critical paths

The framework is designed to have **negligible impact** on application performance while providing comprehensive observability.

## Configuration

FlexKit.Logging supports five configuration approaches, from zero-configuration to fully customized setups. All approaches can be combined for maximum flexibility.

### 1. Zero Configuration

The simplest approach - FlexKit automatically detects and logs all interceptable services:

```csharp
// Program.cs
var host = Host.CreateDefaultBuilder(args)
    .AddFlexConfig()  // AutoIntercept: true by default
    .Build();

// Services with interfaces or virtual methods get automatic logging
public interface IOrderService
{
    void ProcessOrder(string orderId);
}

public class OrderService : IOrderService
{
    public void ProcessOrder(string orderId) { } // Auto-logged with LogInput + Information level
}
```

**Default Behavior:**
- `AutoIntercept: true`
- `LogInput` behavior with `Information` level
- All public methods automatically intercepted
- Services with `IFlexKitLogger` injection automatically excluded

### 2. Attribute-Only Configuration

Use attributes to control logging behavior without any JSON configuration:

```csharp
// Program.cs - same minimal setup
var host = Host.CreateDefaultBuilder(args)
    .AddFlexConfig()
    .Build();

// Services with explicit attributes
public interface IPaymentService
{
    void ProcessPayment(PaymentRequest request);
    PaymentResult GetPaymentStatus(string id);
    string GetCacheKey(int id);
}

[LogBoth] // Class-level: applies to all methods
public class PaymentService : IPaymentService
{
    [LogInput] // Method-level: overrides class-level
    public void ProcessPayment(PaymentRequest request) { }

    [LogOutput]
    public PaymentResult GetPaymentStatus(string id) { return new(); }

    [NoLog] // Exclude from logging (94% performance gain)
    public string GetCacheKey(int id) => $"payment:{id}";
}
```

**Available Attributes:**
- `[LogInput]` - Log method parameters only
- `[LogOutput]` - Log return values only
- `[LogBoth]` - Log both parameters and return values
- `[NoLog]` - Exclude specific methods from logging
- `[NoAutoLog]` - Disable auto-interception but allow manual logging

### 3. JSON-Only Configuration

Control logging behavior entirely through configuration without code changes:

```csharp
// Program.cs - same minimal setup
var host = Host.CreateDefaultBuilder(args)
    .AddFlexConfig()
    .Build();
```

```json
// appsettings.json (automatically detected)
{
  "FlexKit": {
    "Logging": {
      "DefaultFormatter": "Json",
      "DefaultTarget": "Console",
      "Services": {
        "MyApp.Services.*": {
          "LogInput": true,
          "Level": "Information",
          "ExcludeMethodPatterns": ["Get*", "*Cache"]
        },
        "MyApp.Services.PaymentService": {
          "LogBoth": true,
          "Level": "Warning",
          "Target": "Debug",
          "MaskParameterPatterns": ["*card*", "*password*"]
        }
      }
    }
  }
}
```

**Configuration Features:**
- **Service Patterns**: Exact matches (`PaymentService`) and wildcards (`MyApp.Services.*`)
- **Method Exclusion**: Skip specific methods using patterns
- **Masking**: Protect sensitive data automatically
- **Targets**: Route logs to different outputs
- **Levels**: Control log verbosity per service

### 4. Mixed Configuration (Attributes + JSON)

Combine attributes with JSON configuration for maximum flexibility:

```csharp
public interface IUserService
{
    void CreateUser(string username, string password);
    UserInfo GetUser(int id);
}

public class UserService : IUserService
{
    [LogBoth] // Attribute overrides JSON config
    public void CreateUser(string username, string password) { }

    // Uses JSON configuration
    public UserInfo GetUser(int id) { return new(); }
}
```

```json
// appsettings.json
{
  "FlexKit": {
    "Logging": {
      "Services": {
        "MyApp.Services.UserService": {
          "LogInput": true,  // Overridden by [LogBoth] attribute
          "Level": "Information"
        }
      }
    }
  }
}
```

**Precedence Order:**
1. **Attributes** (highest priority)
2. **JSON Configuration** (medium priority)
3. **Auto-Intercept Defaults** (lowest priority)

### 5. External Sources Configuration

#### Custom JSON Files
```csharp
// Program.cs
var host = Host.CreateDefaultBuilder(args)
    .AddFlexConfig(config => config
        .AddJsonFile("logging-config.json", optional: true)
        .AddJsonFile("services-config.json", optional: true))
    .Build();
```

```json
// logging-config.json
{
  "FlexKit": {
    "Logging": {
      "Services": {
        "MyApp.Critical.*": {
          "LogBoth": true,
          "Level": "Error"
        }
      }
    }
  }
}
```

#### Environment Variables
```csharp
// Program.cs
var host = Host.CreateDefaultBuilder(args)
    .AddFlexConfig(config => config
        .AddEnvironmentVariables())
    .Build();
```

```bash
# Environment variables
FLEXKIT__LOGGING__DEFAULTFORMATTER=Json
FLEXKIT__LOGGING__SERVICES__MYAPP_SERVICES__LOGINPUT=true
FLEXKIT__LOGGING__SERVICES__MYAPP_SERVICES__LEVEL=Warning
```

#### YAML Configuration
```csharp
var host = Host.CreateDefaultBuilder(args)
    .AddFlexConfig(config => config
        .AddYamlFile("logging.yaml", optional: true))
    .Build();
```

```yaml
# logging.yaml
FlexKit:
  Logging:
    DefaultFormatter: StandardStructured
    Services:
      "MyApp.Services.*":
        LogInput: true
        Level: Information
        ExcludeMethodPatterns:
          - "ToString"
          - "Get*"
          - "*Internal"
```

#### Cloud Configuration Sources
```csharp
// AWS Parameter Store
var host = Host.CreateDefaultBuilder(args)
    .AddFlexConfig(config => config
        .AddAwsParameterStore("/myapp/logging/", optional: true))
    .Build();

// Azure Key Vault  
var host = Host.CreateDefaultBuilder(args)
    .AddFlexConfig(config => config
        .AddAzureKeyVault(vaultUri, credential))
    .Build();
```

### Complete Configuration Reference

```json
{
  "FlexKit": {
    "Logging": {
      "AutoIntercept": true,
      "DefaultFormatter": "StandardStructured",
      "DefaultTarget": "Console", 
      "MaxBatchSize": 1,
      "BatchTimeout": "00:00:01",
      "EnableFallbackFormatting": true,
      "FallbackTemplate": "Method {TypeName}.{MethodName} - Status: {Success}",
      
      "Services": {
        "MyApp.Services.*": {
          "LogInput": true,
          "Level": "Information",
          "Target": "Console",
          "Formatter": "Json",
          "ExcludeMethodPatterns": ["Get*", "*Cache", "ToString"],
          "MaskParameterPatterns": ["*password*", "*key*"],
          "MaskPropertyPatterns": ["Token", "Secret"],
          "MaskReplacement": "[REDACTED]"
        }
      },
      
      "Targets": {
        "Console": {
          "Type": "Console",
          "Enabled": true,
          "Properties": {
            "IncludeScopes": true,
            "TimestampFormat": "yyyy-MM-dd HH:mm:ss.fff"
          }
        },
        "Debug": {
          "Type": "Debug", 
          "Enabled": true
        }
      },
      
      "Templates": {
        "PaymentService": {
          "SuccessTemplate": "💰 PAYMENT: {MethodName} completed in {Duration}ms → {OutputValue}",
          "ErrorTemplate": "💥 PAYMENT FAILED: {MethodName} | {ExceptionType}: {ExceptionMessage}"
        }
      },
      
      "Formatters": {
        "Json": {
          "PrettyPrint": false,
          "IncludeTimingInfo": true
        },
        "Hybrid": {
          "MessageTemplate": "Method {MethodName} completed",
          "MetadataSeparator": " | "
        }
      }
    }
  }
}
```

### Configuration Best Practices

1. **Start Simple**: Begin with zero configuration, add complexity as needed
2. **Use Wildcards**: Apply consistent patterns across service namespaces
3. **Leverage Attributes**: For service-specific overrides that should stay with code
4. **Environment-Specific**: Use different configurations per environment
5. **Performance Tuning**: Use `[NoLog]` for high-frequency, non-business-critical methods

## Formatting & Templates

FlexKit.Logging provides five powerful formatters and a comprehensive template system for customizing log output. All template variables from the `LogEntry` class are available for use in templates.

### Available Template Variables

FlexKit extracts these variables from method execution for use in templates:

**Core Method Variables:**
- `{Id}` - Unique identifier (Guid) for the log entry
- `{MethodName}` - Name of the method being logged
- `{TypeName}` - Full name of the type containing the method
- `{Success}` - Boolean indicating successful execution
- `{InputParameters}` - Raw input parameters (if LogInput enabled)
- `{OutputValue}` - Raw output value (if LogOutput enabled)

**Timing & Performance:**
- `{Timestamp}` - Method execution timestamp
- `{Duration}` - Duration in milliseconds
- `{DurationSeconds}` - Duration in seconds (3 decimal places)

**Exception Information (when Success = false):**
- `{ExceptionType}` - Exception type name
- `{ExceptionMessage}` - Exception message text
- `{StackTrace}` - Full exception stack trace

**Context Information:**
- `{ThreadId}` - Thread ID where method executed
- `{ActivityId}` - Activity ID for distributed tracing

### 1. StandardStructured Formatter

Basic structured formatting with direct placeholder replacement:

```csharp
// Configuration
[LogBoth(formatter: "StandardStructured")]
public string ProcessPayment(decimal amount) => $"Payment: {amount}";
```

```json
{
  "FlexKit": {
    "Logging": {
      "DefaultFormatter": "StandardStructured",
      "Templates": {
        "PaymentService": {
          "SuccessTemplate": "Method {TypeName}.{MethodName}({InputParameters}) → {OutputValue} | Duration: {Duration}ms"
        }
      }
    }
  }
}
```

**Output Example:**
```
Method PaymentService.ProcessPayment(100.50) → Payment: 100.50 | Duration: 450ms
```

### 2. Json Formatter

Full JSON serialization with customizable property names:

```csharp
[LogBoth(formatter: "Json")]
public PaymentResult ProcessPayment(PaymentData data) => new() { Success = true };
```

```json
{
  "FlexKit": {
    "Logging": {
      "Formatters": {
        "Json": {
          "PrettyPrint": false,
          "CustomPropertyNames": {
            "MethodName": "method_name",
            "Duration": "execution_time_ms"
          }
        }
      }
    }
  }
}
```

**Output Example:**
```json
{"method_name": "ProcessPayment", "execution_time_ms": 450, "success": true, "input_parameters": {"amount": 100.50}, "output_value": {"Success": true}}
```

### 3. CustomTemplate Formatter

Service-specific templates with advanced validation:

```csharp
[LogBoth(formatter: "CustomTemplate")]
public PaymentResult ProcessPayment(PaymentData data) => new() { Success = true };
```

```json
{
  "FlexKit": {
    "Logging": {
      "Formatters": {
        "CustomTemplate": {
          "StrictValidation": true,
          "CacheTemplates": true,
          "DefaultTemplate": "Method {TypeName}.{MethodName} executed",
          "ServiceTemplates": {
            "PaymentService": "PaymentTemplate",
            "OrderService": "OrderTemplate"
          }
        }
      },
      "Templates": {
        "PaymentTemplate": {
          "SuccessTemplate": "Payment {MethodName} processed: Amount={InputParameters.Amount}, Result={OutputValue.Success}",
          "ErrorTemplate": "Payment {MethodName} failed: {ExceptionMessage}"
        }
      }
    }
  }
}
```

**Template Resolution Order:**
1. Service-specific template (by type name)
2. Method-specific template
3. Default template from configuration
4. Fallback template from settings

### 4. Hybrid Formatter

Combines structured message with JSON metadata:

```csharp
[LogBoth(formatter: "Hybrid")]
public PaymentResult ProcessPayment(PaymentData data) => new() { Success = true };
```

```json
{
  "FlexKit": {
    "Logging": {
      "Formatters": {
        "Hybrid": {
          "MessageTemplate": "Payment processing: {MethodName}",
          "MetadataSeparator": " | META: "
        }
      }
    }
  }
}
```

**Output Example:**
```
Payment processing: ProcessPayment | META: {"duration": 450, "thread_id": 12, "success": true}
```

### 5. SuccessError Formatter

Different templates for successful vs failed executions:

```csharp
[LogBoth(formatter: "SuccessError")]
public PaymentResult ProcessPayment(PaymentData data)
{
    if (data.Amount <= 0) throw new ArgumentException("Invalid amount");
    return new() { Success = true };
}
```

```json
{
  "FlexKit": {
    "Logging": {
      "Templates": {
        "PaymentService": {
          "SuccessTemplate": "💰 PAYMENT: {MethodName} completed in {Duration}ms → {OutputValue}",
          "ErrorTemplate": "💥 PAYMENT FAILED: {MethodName} | {ExceptionType}: {ExceptionMessage}"
        }
      }
    }
  }
}
```

**Success Output:**
```
💰 PAYMENT: ProcessPayment completed in 450ms → {"Success": true}
```

**Error Output:**
```
💥 PAYMENT FAILED: ProcessPayment | ArgumentException: Invalid amount
```

### Manual Logging with Formatters

When using `IFlexKitLogger` for manual control, you can specify formatters per log entry:

```csharp
public class PaymentService : IPaymentService
{
    private readonly IFlexKitLogger _logger;

    public PaymentService(IFlexKitLogger logger) // Excludes class from auto-interception
    {
        _logger = logger;
    }

    public PaymentResult ProcessComplexPayment(PaymentRequest request)
    {
        // Start with JSON formatter for detailed input logging
        var startEntry = LogEntry.CreateStart(nameof(ProcessComplexPayment), GetType().FullName!)
            .WithInput(new { RequestId = request.Id, Amount = request.Amount })
            .WithFormatter(FormatterType.Json)
            .WithTarget("Console");

        _logger.Log(startEntry);

        try
        {
            // Business logic
            var result = new PaymentResult { Success = true };

            // Complete with custom template for business-friendly output
            var endEntry = startEntry
                .WithCompletion(success: true)
                .WithOutput(new { Success = result.Success })
                .WithFormatter(FormatterType.CustomTemplate)
                .WithTemplate("PaymentService");

            _logger.Log(endEntry);
            return result;
        }
        catch (Exception ex)
        {
            // Error with hybrid formatter showing both message and metadata
            var errorEntry = startEntry
                .WithCompletion(success: false, exception: ex)
                .WithFormatter(FormatterType.Hybrid);
            _logger.Log(errorEntry);
            throw;
        }
    }
}
```

### Distributed Tracing Integration

Manual logging supports Activity management for distributed tracing:

```csharp
public async Task<string> ProcessAsyncOperation(string input)
{
    // Start distributed tracing activity
    using var activity = _logger.StartActivity("ProcessAsyncOperation");

    var startEntry = LogEntry.CreateStart(nameof(ProcessAsyncOperation), GetType().FullName!)
        .WithInput(input)
        .WithTemplate("ComplexService")
        .WithFormatter(FormatterType.CustomTemplate);

    _logger.Log(startEntry);

    try
    {
        await Task.Delay(100);
        var result = $"Processed: {input}";

        var endEntry = startEntry
            .WithCompletion(success: true)
            .WithOutput(result);

        _logger.Log(endEntry);
        return result;
    }
    catch (Exception ex)
    {
        var errorEntry = startEntry.WithCompletion(success: false, exception: ex);
        _logger.Log(errorEntry);
        throw;
    }
}
```

### Advanced Template Configuration

#### Per-Service Template Customization
```json
{
  "FlexKit": {
    "Logging": {
      "Templates": {
        "PaymentService": {
          "Enabled": true,
          "SuccessTemplate": "💰 PAYMENT: {MethodName}({InputParameters}) → {OutputValue}",
          "ErrorTemplate": "💥 PAYMENT FAILED: {MethodName} | {ExceptionType}"
        },
        "OrderCalculation": {
          "Enabled": true,
          "SuccessTemplate": "🧮 CALCULATION: {MethodName} → Total: {OutputValue}",
          "ErrorTemplate": "⚠ CALCULATION FAILED: {MethodName} | {ExceptionMessage}"
        }
      }
    }
  }
}
```

#### Template Validation and Compilation
```json
{
  "FlexKit": {
    "Logging": {
      "Formatters": {
        "CustomTemplate": {
          "StrictValidation": true,        // Validate template syntax at startup
          "CacheTemplates": true,          // Compile and cache templates for performance
          "EnableFallbackFormatting": true // Use fallback if template fails
        }
      }
    }
  }
}
```

### Performance Characteristics

**Template Compilation:**
- Startup cost: ~1-5ms for typical applications
- Runtime cost: ~0.1μs for compiled templates
- Memory: ~100-500KB for template cache

**Formatter Performance (per log entry):**
- StandardStructured: ~200-500ns
- Json: ~1-10μs (includes serialization)
- CustomTemplate: ~0.1μs (compiled templates)
- Hybrid: ~1-5μs (message + metadata)
- SuccessError: ~0.1-1μs (template selection + formatting)

### Best Practices

1. **Use meaningful visual indicators** (emojis) for different service types
2. **Include essential context** (TypeName, MethodName, Duration) in templates
3. **Separate success/error templates** for clarity
4. **Keep templates concise** but informative
5. **Enable template caching** for production performance
6. **Test template resolution** during development

## Data Masking System

FlexKit.Logging provides comprehensive data protection through both attribute-based and configuration-based masking strategies. The system operates with minimal performance overhead using reflection caching and intelligent pattern matching.

### Masking Strategies & Precedence

FlexKit uses a clear precedence hierarchy for determining what to mask:

1. **Attribute-based masking** - `[Mask]` attributes on parameters, properties, or types (highest priority)
2. **Configuration-based patterns** - Wildcard patterns in `appsettings.json` (medium priority)
3. **No masking** - Default behavior (lowest priority)

### Interception Requirements for Parameter Masking

**Important**: The `[Mask]` attribute on method parameters only works with interceptable methods:

```csharp
// ✅ WORKS - Interface method with [Mask] attribute
public interface IAuthenticationService
{
    bool ValidateUser(string username, [Mask] string password, string domain);
}

public class AuthenticationService : IAuthenticationService
{
    public bool ValidateUser(string username, string password, string domain)
    {
        // password parameter will be masked in logs
        return password.Length > 8;
    }
}

// ✅ WORKS - Virtual method with [Mask] attribute  
public class AuthenticationService
{
    public virtual bool ValidateUser(string username, [Mask] string password, string domain)
    {
        // password parameter will be masked in logs
        return password.Length > 8;
    }
}

// ❌ DOESN'T WORK - Non-virtual method without interface
public class AuthenticationService
{
    public bool ValidateUser(string username, [Mask] string password, string domain)
    {
        // [Mask] attribute ignored - method not intercepted
        return password.Length > 8;
    }
}
```

### 1. Attribute-Based Masking

#### Parameter-Level Masking
```csharp
public interface IAuthenticationService
{
    // Parameter masking with custom replacement
    bool ValidateUser(string username, [Mask(Replacement = "[HIDDEN]")] string password, string domain);
    
    // Multiple masked parameters
    ApiResponse LoginUser([Mask] string email, [Mask] string password, string clientId);
}

public class AuthenticationService : IAuthenticationService
{
    [LogBoth]
    public bool ValidateUser(string username, string password, string domain)
    {
        Console.WriteLine($"Validating user: {username} in domain: {domain}");
        return password.Length > 8;
    }

    [LogBoth] 
    public ApiResponse LoginUser(string email, string password, string clientId)
    {
        Console.WriteLine($"Login attempt for client: {clientId}");
        return new ApiResponse { Success = true, SessionToken = "session_abc123" };
    }
}
```

#### Property-Level Masking
```csharp
public class UserCredentials
{
    public string Username { get; set; } = string.Empty;
    
    [Mask] // Uses default mask replacement
    public string Password { get; set; } = string.Empty;
    
    [Mask(Replacement = "[API_KEY_REDACTED]")]
    public string ApiKey { get; set; } = string.Empty;
    
    public string Email { get; set; } = string.Empty;
}

public interface IAuthenticationService
{
    ApiResponse LoginUser(UserCredentials credentials);
}

public class AuthenticationService : IAuthenticationService
{
    [LogBoth]
    public ApiResponse LoginUser(UserCredentials credentials)
    {
        // Password and ApiKey properties will be masked in logs
        return new ApiResponse 
        { 
            Success = true, 
            SessionToken = "session_abc123_secret"
        };
    }
}
```

#### Type-Level Masking
```csharp
// Entire type masked - all instances replaced with mask text
[Mask(Replacement = "[CLASSIFIED_CONFIG]")]
public class SecretConfiguration
{
    public string DatabasePassword { get; set; } = string.Empty;
    public string EncryptionKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
}

public interface IConfigurationService
{
    void LoadConfig(SecretConfiguration config);
}

public class ConfigurationService : IConfigurationService
{
    [LogInput]
    public void LoadConfig(SecretConfiguration config)
    {
        // Entire config object logged as "[CLASSIFIED_CONFIG]"
        Console.WriteLine("Loading security configuration");
    }
}
```

#### Output Value Masking
```csharp
public class ApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    
    [Mask] // Masked in return values
    public string SessionToken { get; set; } = string.Empty;
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public interface IAuthenticationService
{
    ApiResponse LoginUser(UserCredentials credentials);
}

public class AuthenticationService : IAuthenticationService
{
    [LogBoth]
    public ApiResponse LoginUser(UserCredentials credentials)
    {
        return new ApiResponse
        {
            Success = true,
            Message = "Login successful",
            SessionToken = "session_abc123_secret" // Masked in output logs
        };
    }
}
```

### 2. Configuration-Based Pattern Matching

Configure masking patterns in `appsettings.json` without code changes:

```json
{
  "FlexKit": {
    "Logging": {
      "Services": {
        "PaymentService": {
          "LogBoth": true,
          "MaskParameterPatterns": ["*card*", "*credit*", "cvv"],
          "MaskPropertyPatterns": ["CreditCardNumber", "CVV"],
          "MaskOutputPatterns": ["*token*", "*session*"],
          "MaskReplacement": "[PAYMENT_DATA_MASKED]"
        },
        "UserService": {
          "LogInput": true,
          "MaskParameterPatterns": ["*email*", "*phone*", "*password*"],
          "MaskPropertyPatterns": ["SessionToken", "RefreshToken"],
          "MaskReplacement": "[PII_REMOVED]"
        },
        "ConfigurationService": {
          "LogBoth": true,
          "MaskParameterPatterns": ["*password*", "*secret*", "*key*"],
          "MaskOutputPatterns": ["*connection*"],
          "MaskReplacement": "[CONFIG_HIDDEN]"
        }
      }
    }
  }
}
```

#### Pattern Matching Rules
- **Exact match**: `"password"` matches only `"password"` parameter/property name
- **Contains**: `"*email*"` matches `"userEmail"`, `"emailAddress"`, `"contactEmail"`
- **Starts with**: `"secret*"` matches `"secretKey"`, `"secretToken"`
- **Ends with**: `"*key"` matches `"apiKey"`, `"encryptionKey"`

#### Configuration-Based Usage Examples
```csharp
public interface IPaymentService
{
    bool ValidateCard(string cardNumber, string cvv, string expiryDate);
    bool ProcessPayment(PaymentData paymentData);
    ApiResponse ChargeCard(PaymentData payment, string transactionId);
}

public class PaymentService : IPaymentService
{
    [LogBoth]
    public bool ValidateCard(
        string cardNumber,    // Masked by pattern "*card*"
        string cvv,          // Masked by pattern "cvv"
        string expiryDate)   // Not masked
    {
        Console.WriteLine("Validating card details");
        return cardNumber.Length >= 16;
    }

    [LogInput]
    public bool ProcessPayment(PaymentData paymentData)
    {
        // Properties masked by MaskPropertyPatterns
        Console.WriteLine($"Processing payment of {paymentData.Amount}");
        return paymentData.Amount > 0;
    }

    [LogOutput]
    public ApiResponse ChargeCard(PaymentData payment, string transactionId)
    {
        return new ApiResponse
        {
            Success = true,
            Message = "Payment processed",
            SessionToken = "charge_session_xyz789" // Masked by pattern "*token*"
        };
    }
}

public class PaymentData
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string CreditCardNumber { get; set; } = string.Empty; // Masked by config
    public string CVV { get; set; } = string.Empty;             // Masked by config
    public string MerchantId { get; set; } = string.Empty;
}
```

### 3. Hybrid Approach (Attributes + Configuration)

Combine both strategies for maximum flexibility:

```csharp
public class PaymentData
{
    public decimal Amount { get; set; }
    
    [Mask(Replacement = "****-****-****-XXXX")] // Attribute with custom format
    public string CreditCardNumber { get; set; } = string.Empty;
    
    [Mask] // Attribute with default replacement
    public string CVV { get; set; } = string.Empty;
    
    public string MerchantId { get; set; } = string.Empty; // No masking unless config matches
}

public interface IUserService
{
    ApiResponse CreateUser(string username, string email, string phoneNumber);
}

public class UserService : IUserService
{
    [LogOutput]
    public ApiResponse CreateUser(
        string username,
        string email,      // Masked by config pattern "*email*"
        string phoneNumber) // Masked by config pattern "*phone*"
    {
        return new ApiResponse
        {
            Success = true,
            Message = "User created successfully",
            SessionToken = "new_user_session_123" // Masked by [Mask] attribute
        };
    }
}
```

### Real-World Usage Scenarios

#### Complete Payment Processing Example
```csharp
// Program.cs - Standard FlexKit setup
var host = Host.CreateDefaultBuilder(args)
    .AddFlexConfig()
    .Build();

// Service interfaces and implementations
public interface IPaymentService
{
    bool ProcessPayment(PaymentData paymentData);
    bool ValidateCard(string cardNumber, string cvv, string expiryDate);
    ApiResponse ChargeCard(PaymentData payment, string transactionId);
}

public interface IUserService
{
    ApiResponse CreateUser(string username, string email, string phoneNumber);
    bool UpdatePassword(int userId, [Mask] string oldPassword, [Mask] string newPassword);
}

public interface IConfigurationService
{
    void LoadConfig(SecretConfiguration config);
    string GetConnectionString(string server, string database, string password);
}

// Service implementations with masking
public class PaymentService : IPaymentService
{
    [LogInput]
    public bool ProcessPayment(PaymentData paymentData)
    {
        // CreditCardNumber and CVV masked by [Mask] attributes
        Console.WriteLine($"Processing payment of {paymentData.Amount} {paymentData.Currency}");
        return paymentData.Amount > 0;
    }

    [LogBoth]
    public bool ValidateCard(string cardNumber, string cvv, string expiryDate)
    {
        // cardNumber and cvv masked by configuration patterns
        Console.WriteLine("Validating card details");
        return cardNumber.Length >= 16;
    }

    [LogOutput]
    public ApiResponse ChargeCard(PaymentData payment, string transactionId)
    {
        return new ApiResponse
        {
            Success = true,
            Message = "Payment processed",
            SessionToken = "charge_session_xyz789" // Masked by [Mask] attribute
        };
    }
}
```

### Performance Characteristics

The masking system is designed for production performance:

- **Type reflection caching**: ~50-100ns after first lookup
- **Pattern matching**: ~10-50ns per pattern
- **Object cloning**: ~1-5μs for typical DTOs
- **Memory overhead**: ~100-500KB for reflection cache in typical applications

### Security Considerations

#### Default Mask Replacement Strategy
FlexKit determines mask replacement text through this hierarchy:
1. **Custom replacement** from `[Mask(Replacement = "...")]` attribute
2. **Service-specific replacement** from configuration `MaskReplacement`
3. **Global default** fallback: `"***MASKED***"`

#### Error Handling & Fallbacks
- **Graceful degradation**: When object cloning fails, logs original object without breaking application
- **Null safety**: Comprehensive null checks throughout the masking pipeline
- **Type safety**: Read/write capability checks during reflection operations

### Best Practices

1. **Use attribute masking** for data that should never appear in logs (passwords, keys, tokens)
2. **Use configuration patterns** for PII and business-sensitive data that may vary by environment
3. **Combine both approaches** for comprehensive data protection
4. **Test masking effectiveness** in development and staging environments
5. **Document masking requirements** for compliance and audit purposes
6. **Use meaningful replacement text** to aid debugging while protecting data

### Common Patterns by Data Type

**Authentication Data:**
```json
{
  "MaskParameterPatterns": ["*password*", "*secret*", "*key*", "*token*"],
  "MaskPropertyPatterns": ["Password", "ApiKey", "SessionToken", "RefreshToken"]
}
```

**Payment Data:**
```json
{
  "MaskParameterPatterns": ["*card*", "*credit*", "cvv", "*account*"],
  "MaskPropertyPatterns": ["CreditCardNumber", "CVV", "AccountNumber", "RoutingNumber"]
}
```

**Personal Information:**
```json
{
  "MaskParameterPatterns": ["*email*", "*phone*", "*ssn*", "*tax*"],
  "MaskPropertyPatterns": ["Email", "PhoneNumber", "SocialSecurityNumber", "TaxId"]
}
```

## Targeting System

FlexKit.Logging's targeting system provides automatic detection and configuration of logging providers, allowing logs to be routed to different destinations with framework-independent configuration.

### Auto-Detection and Registration

FlexKit automatically detects available logging providers and assigns keys for configuration:

**Automatic Detection Process:**
1. **Framework Detection** - Scans for available logging providers (MEL Console, Serilog File, NLog Debug, etc.)
2. **Key Assignment** - Each provider gets a key based on its type name ("Console", "File", "Debug", etc.)
3. **Configuration Lookup** - Checks for matching target configuration in `FlexKit.Logging.Targets`
4. **Property Mapping** - Converts properties to framework-expected types and passes in correct order

### Target Selection Priority

FlexKit uses a clear hierarchy for determining which target to use:

1. **Attribute target** - `[LogInput(target: "Debug")]` (highest priority)
2. **Service configuration target** - In JSON service config (medium priority)
3. **DefaultTarget** - Global fallback setting (low priority)
4. **Console fallback** - If no providers found or configured (lowest priority)

### Framework-Specific Examples

#### Microsoft Extensions Logging (MEL)
```json
{
  "FlexKit": {
    "Logging": {
      "Targets": {
        "Console": {
          "Type": "Console",
          "Enabled": true,
          "Properties": {
            "FormatterType": "Simple",
            "IncludeScopes": true,
            "TimestampFormat": "yyyy-MM-dd HH:mm:ss.fff ",
            "ColorBehavior": "Enabled",
            "SingleLine": false
          }
        },
        "Debug": {
          "Type": "Debug",
          "Enabled": true,
          "Properties": {
            "LogLevel": "Information"
          }
        }
      }
    }
  }
}
```

#### Serilog
```json
{
  "FlexKit": {
    "Logging": {
      "Targets": {
        "Console": {
          "Type": "Console",
          "Properties": {
            "Theme": "Ansi",
            "OutputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}",
            "RestrictedToMinimumLevel": "Information"
          }
        },
        "File": {
          "Type": "File",
          "Properties": {
            "Path": "logs/app-.log",
            "RollingInterval": "Day",
            "RetainedFileCountLimit": 7
          }
        }
      }
    }
  }
}
```

#### NLog
```json
{
  "FlexKit": {
    "Logging": {
      "Targets": {
        "Console": {
          "Type": "Console",
          "Properties": {
            "Layout": "${time} ${level:uppercase=true} ${logger} ${message} ${exception}",
            "Encoding": "utf-8"
          }
        },
        "File": {
          "Type": "File",
          "Properties": {
            "FileName": "logs/app-${shortdate}.log",
            "Layout": "${longdate} ${level:uppercase=true} ${logger} ${message}",
            "MaxArchiveFiles": 7
          }
        }
      }
    }
  }
}
```

#### ZLogger
```json
{
  "FlexKit": {
    "Logging": {
      "Targets": {
        "Console": {
          "Type": "Console",
          "Properties": {
            "UseJsonFormatter": false,
            "IncludeScopes": true
          }
        },
        "File": {
          "Type": "File",
          "Properties": {
            "FilePathSelector": "logs/app-.log",
            "UseJsonFormatter": true
          }
        }
      }
    }
  }
}
```

### Usage Examples

#### Attribute-Based Targeting
```csharp
public interface IPaymentService
{
    void ProcessPayment(PaymentData data);
    PaymentResult GetStatus(string id);
}

public class PaymentService : IPaymentService
{
    [LogInput(target: "Console", level: LogLevel.Information)]
    public void ProcessPayment(PaymentData data) 
    {
        Console.WriteLine($"Processing payment: {data.Amount}");
    }

    [LogOutput(target: "File", level: LogLevel.Debug)]
    public PaymentResult GetStatus(string id) 
    { 
        return new PaymentResult { Success = true, Id = id };
    }
}
```

#### Configuration-Based Targeting
```json
{
  "FlexKit": {
    "Logging": {
      "DefaultTarget": "Console",
      "Services": {
        "MyApp.Services.PaymentService": {
          "LogBoth": true,
          "Target": "File",
          "Level": "Warning"
        },
        "MyApp.Services.AuditService": {
          "LogInput": true,
          "Target": "Debug",
          "Level": "Information"
        }
      }
    }
  }
}
```

### Manual Logging with Targets

When using `IFlexKitLogger`, you can specify targets per log entry:

```csharp
public class PaymentService : IPaymentService
{
    private readonly IFlexKitLogger _logger;

    public PaymentService(IFlexKitLogger logger)
    {
        _logger = logger;
    }

    public PaymentResult ProcessComplexPayment(PaymentRequest request)
    {
        // Start entry to File target for detailed logging
        var startEntry = LogEntry.CreateStart(nameof(ProcessComplexPayment), GetType().FullName!)
            .WithInput(new { RequestId = request.Id, Amount = request.Amount })
            .WithTarget("File")
            .WithFormatter(FormatterType.Json);

        _logger.Log(startEntry);

        try
        {
            // High-value transactions to audit target
            if (request.Amount > 10000)
            {
                var auditEntry = LogEntry.CreateStart("HighValueTransaction", GetType().FullName!)
                    .WithInput(new { Amount = request.Amount, MerchantId = request.MerchantId })
                    .WithTarget("Debug")
                    .WithLevel(LogLevel.Warning);
                _logger.Log(auditEntry);
            }

            var result = new PaymentResult { Success = true };

            // Completion to Console for visibility
            var endEntry = startEntry
                .WithCompletion(success: true)
                .WithOutput(new { Success = result.Success })
                .WithTarget("Console");

            _logger.Log(endEntry);
            return result;
        }
        catch (Exception ex)
        {
            // Errors to Debug target for investigation
            var errorEntry = startEntry
                .WithCompletion(success: false, exception: ex)
                .WithTarget("Debug");
            _logger.Log(errorEntry);
            throw;
        }
    }
}
```

### Multiple Target Configuration

Configure multiple targets for different scenarios:

```json
{
  "FlexKit": {
    "Logging": {
      "DefaultTarget": "Console",
      "Targets": {
        "Console": {
          "Type": "Console",
          "Enabled": true,
          "Properties": {
            "FormatterType": "Simple",
            "IncludeScopes": true,
            "TimestampFormat": "yyyy-MM-dd HH:mm:ss.fff"
          }
        },
        "File": {
          "Type": "File",
          "Enabled": true,
          "Properties": {
            "Path": "logs/application-.log",
            "RollingInterval": "Day",
            "RetainedFileCountLimit": 30
          }
        },
        "Debug": {
          "Type": "Debug",
          "Enabled": true,
          "Properties": {
            "LogLevel": "Debug"
          }
        },
        "EventLog": {
          "Type": "EventLog",
          "Enabled": false,
          "Properties": {
            "LogName": "Application",
            "SourceName": "MyApplication"
          }
        }
      },
      "Services": {
        "MyApp.Services.PaymentService": {
          "LogBoth": true,
          "Target": "File",
          "Level": "Information"
        },
        "MyApp.Services.AuditService": {
          "LogInput": true,
          "Target": "EventLog",
          "Level": "Warning"
        },
        "MyApp.Controllers.*": {
          "LogOutput": true,
          "Target": "Console",
          "Level": "Information"
        }
      }
    }
  }
}
```

### Target Fallback Behavior

FlexKit provides graceful fallback when targets are unavailable:

```csharp
public class OrderService : IOrderService
{
    private readonly IFlexKitLogger _logger;

    public OrderService(IFlexKitLogger logger) => _logger = logger;

    public void ProcessOrder(string orderId)
    {
        // No target specified - uses DefaultTarget or Console fallback
        var entry1 = LogEntry.CreateStart(nameof(ProcessOrder), GetType().FullName!)
            .WithInput(orderId);
        _logger.Log(entry1);

        // Target specified but doesn't exist - falls back to DefaultTarget
        var entry2 = LogEntry.CreateStart("CustomStep", GetType().FullName!)
            .WithTarget("NonExistentTarget")
            .WithInput("step data");
        _logger.Log(entry2);

        // Multiple entries with different targets
        var auditEntry = LogEntry.CreateStart("OrderAudit", GetType().FullName!)
            .WithTarget("File")
            .WithLevel(LogLevel.Warning);
        _logger.Log(auditEntry);
    }
}
```

### Environment-Specific Targeting

Configure different targets per environment:

```json
// appsettings.Production.json
{
  "FlexKit": {
    "Logging": {
      "DefaultTarget": "File",
      "Targets": {
        "File": {
          "Type": "File",
          "Enabled": true,
          "Properties": {
            "Path": "/var/log/myapp/app-.log",
            "RollingInterval": "Hour",
            "RetainedFileCountLimit": 168
          }
        },
        "Console": {
          "Type": "Console",
          "Enabled": false
        }
      }
    }
  }
}

// appsettings.Development.json
{
  "FlexKit": {
    "Logging": {
      "DefaultTarget": "Console",
      "Targets": {
        "Console": {
          "Type": "Console",
          "Enabled": true,
          "Properties": {
            "FormatterType": "Simple",
            "IncludeScopes": true
          }
        },
        "Debug": {
          "Type": "Debug",
          "Enabled": true
        }
      }
    }
  }
}
```

### Advanced Targeting Scenarios

#### Conditional Targeting Based on Log Level
```csharp
public class SecurityService : ISecurityService
{
    private readonly IFlexKitLogger _logger;

    public SecurityService(IFlexKitLogger logger) => _logger = logger;

    public bool AuthenticateUser(string username, string password)
    {
        var entry = LogEntry.CreateStart(nameof(AuthenticateUser), GetType().FullName!)
            .WithInput(new { username });

        // Normal authentication logs to standard target
        _logger.Log(entry.WithTarget("Console"));

        try
        {
            var isValid = ValidateCredentials(username, password);
            
            if (!isValid)
            {
                // Failed authentication to security target
                var failureEntry = LogEntry.CreateStart("AuthenticationFailure", GetType().FullName!)
                    .WithInput(new { username, Timestamp = DateTime.UtcNow })
                    .WithTarget("SecurityLog")
                    .WithLevel(LogLevel.Warning);
                _logger.Log(failureEntry);
            }

            _logger.Log(entry.WithCompletion(true).WithOutput(isValid));
            return isValid;
        }
        catch (Exception ex)
        {
            // Errors to both console and security log
            _logger.Log(entry.WithCompletion(false, ex).WithTarget("Console"));
            _logger.Log(entry.WithCompletion(false, ex).WithTarget("SecurityLog"));
            throw;
        }
    }
}
```

### Key Points

**Target Limitations:**
- One target per type: Cannot have "Console1" and "Console2" - they output to the same console
- Framework independence: Same configuration works across all supported frameworks
- Property validation: Invalid property keys are ignored, invalid values cause framework-specific exceptions
- Type conversion: FlexKit automatically converts JSON values to expected framework types

**Performance Considerations:**
- Target resolution is cached for performance
- Framework translation overhead is minimal (~50-300ns)
- Multiple targets per method add processing overhead but maintain overall performance characteristics

**Best Practices:**
1. Use descriptive target names that reflect their purpose
2. Configure environment-specific targets for production vs development
3. Use fallback targets to ensure logs are never lost
4. Monitor target availability and performance in production environments

## Interception Requirements & Limitations

FlexKit.Logging uses Castle DynamicProxy for method interception, which requires specific service characteristics to function properly. Understanding these requirements is crucial for effective logging setup.

### What CAN Be Intercepted

#### 1. Interface-Based Services (Recommended)
```csharp
public interface IOrderService
{
    void ProcessOrder(string orderId);
    OrderResult GetOrderStatus(string orderId);
}

public class OrderService : IOrderService  // ✅ Can be intercepted
{
    public void ProcessOrder(string orderId) 
    {
        Console.WriteLine($"Processing order: {orderId}");
    }
    
    public OrderResult GetOrderStatus(string orderId) 
    {
        return new OrderResult { Id = orderId, Status = "Completed" };
    }
}
```

**Why This Works:**
- FlexKit registers the service using interface interception
- All interface methods are automatically interceptable
- No virtual keyword required on implementation methods
- Best performance characteristics

#### 2. Classes with Public Virtual Methods
```csharp
public class PaymentService  // ✅ Can be intercepted
{
    public virtual void ProcessPayment(PaymentData data) { }  // Virtual = interceptable
    public virtual PaymentResult GetStatus(string id) { return new(); }
    
    public void CalculateFees(decimal amount) { }  // ❌ Not virtual = not intercepted
}
```

**Why This Works:**
- FlexKit uses class interception for services without interfaces
- Only virtual methods can be intercepted
- Non-virtual methods in the same class are ignored

### What CANNOT Be Intercepted

#### 1. Classes Without Interfaces AND Without Public Virtual Methods
```csharp
public class OrderService  // ❌ CANNOT be intercepted
{
    public void ProcessOrder(string orderId) { }  // Not virtual, no interface
    public string GetOrderStatus(string id) { return ""; }  // Not virtual, no interface
}
```

**Solution - Add Interface:**
```csharp
public interface IOrderService
{
    void ProcessOrder(string orderId);
    string GetOrderStatus(string id);
}

public class OrderService : IOrderService  // ✅ Now interceptable
{
    public void ProcessOrder(string orderId) { }
    public string GetOrderStatus(string id) { return ""; }
}
```

**Solution - Make Methods Virtual:**
```csharp
public class OrderService  // ✅ Now interceptable
{
    public virtual void ProcessOrder(string orderId) { }
    public virtual string GetOrderStatus(string id) { return ""; }
}
```

#### 2. Sealed Classes
```csharp
public sealed class PaymentService  // ❌ CANNOT be intercepted
{
    public void ProcessPayment(PaymentData data) { }  // Sealed class = no interception
}
```

**Why This Fails:**
- Castle DynamicProxy cannot inherit from sealed classes
- No workaround available - sealed classes fundamentally incompatible with interception

#### 3. Static Classes
```csharp
public static class UtilityService  // ❌ CANNOT be intercepted
{
    public static string FormatData(string input) { return input; }  // Static = no interception
}
```

**Why This Fails:**
- Static methods cannot be intercepted by design
- No instance creation means no proxy generation possible

#### 4. Classes with Only Private/Internal Virtual Methods
```csharp
public class OrderService  // ❌ CANNOT be intercepted
{
    private virtual void ProcessOrder(string orderId) { }  // Private virtual = no interception
    internal virtual string GetStatus(string id) { return ""; }  // Internal virtual = no interception
}
```

**Why This Fails:**
- DynamicProxy only intercepts public methods
- Private and internal methods are not accessible to proxy

#### 5. Classes with IFlexKitLogger Injection (Automatic Exclusion)
```csharp
public interface IOrderService
{
    void ProcessOrder(string orderId);
}

public class OrderService : IOrderService  // ❌ Automatically excluded from interception
{
    private readonly IFlexKitLogger _logger;

    public OrderService(IFlexKitLogger logger)  // IFlexKitLogger injection = exclusion
    {
        _logger = logger;
    }

    public void ProcessOrder(string orderId)  // Will NOT be intercepted
    {
        // Must use manual logging
        var entry = LogEntry.CreateStart(nameof(ProcessOrder), GetType().FullName!)
            .WithInput(orderId);
        _logger.Log(entry);
        
        Console.WriteLine($"Processing order: {orderId}");
        
        _logger.Log(entry.WithCompletion(success: true));
    }
}
```

**Why This Happens:**
- FlexKit automatically detects IFlexKitLogger injection
- Assumes you want manual logging control
- Prevents conflicts between automatic and manual logging

### FlexKit's Automatic Registration Behavior

FlexKit scans assemblies and automatically determines the best registration strategy:

```csharp
// FlexKit automatically handles these cases during registration:

// Case 1: Has interface - uses interface interception
builder.RegisterType<OrderService>()
    .As<IOrderService>()
    .EnableInterfaceInterceptors()
    .InterceptedBy<MethodLoggingInterceptor>();

// Case 2: No interface but has virtual methods - uses class interception  
builder.RegisterType<PaymentService>()
    .AsSelf()
    .EnableClassInterceptors()
    .InterceptedBy<MethodLoggingInterceptor>();

// Case 3: Cannot be intercepted - registers without interception
builder.RegisterType<UtilityService>()
    .AsSelf();  // No interceptors added
```

### Warning Messages

FlexKit logs warnings for non-interceptable classes during startup:

```
Warning: Cannot intercept MyApp.Services.OrderService - class is sealed or has no virtual methods. 
Consider adding an interface or making methods virtual for logging to work.

Warning: Skipping interception for MyApp.Utilities.StaticHelper - static classes cannot be intercepted.

Info: Excluding MyApp.Services.ComplexService from auto-interception - IFlexKitLogger detected in constructor.
```

### Interface vs Class Interception Comparison

| Aspect | Interface Interception | Class Interception |
|--------|----------------------|-------------------|
| **Performance** | Better (proxy implements interface) | Good (proxy inherits from class) |
| **Method Requirements** | None (all interface methods work) | Must be virtual |
| **Flexibility** | High (clean separation) | Medium (inheritance constraints) |
| **Best Practice** | ✅ Recommended | ⚠️ Use when interfaces not possible |
| **Testability** | Excellent (easy mocking) | Good (virtual methods mockable) |

### Complete Examples

#### Good Design - Interface-Based
```csharp
// Program.cs
var host = Host.CreateDefaultBuilder(args)
    .AddFlexConfig()  // Automatic registration and interception
    .Build();

// Service definition
public interface IPaymentService
{
    bool ProcessPayment(decimal amount);
    PaymentResult GetPaymentStatus(string transactionId);
}

[LogBoth]  // All methods will log input and output
public class PaymentService : IPaymentService
{
    public bool ProcessPayment(decimal amount)
    {
        Console.WriteLine($"Processing payment: ${amount}");
        return amount > 0;
    }

    [NoLog]  // Override class-level attribute
    public PaymentResult GetPaymentStatus(string transactionId)
    {
        return new PaymentResult { Success = true, TransactionId = transactionId };
    }
}

// Usage - automatic logging occurs
var paymentService = host.Services.GetService<IPaymentService>();
paymentService.ProcessPayment(100.50m);  // Automatically logged
```

#### Alternative Design - Virtual Methods
```csharp
[LogInput]  // Class-level logging configuration
public class NotificationService
{
    public virtual void SendEmail(string recipient, string subject, string body)
    {
        Console.WriteLine($"Sending email to {recipient}: {subject}");
        // Email sending logic
    }

    public virtual void SendSms(string phoneNumber, string message)
    {
        Console.WriteLine($"Sending SMS to {phoneNumber}");
        // SMS sending logic
    }

    public void ValidateEmailFormat(string email)  // Not virtual - won't be logged
    {
        // Validation logic
    }
}
```

#### Manual Logging Design
```csharp
public interface IComplexService
{
    Task<ProcessingResult> ProcessComplexWorkflow(WorkflowRequest request);
}

public class ComplexService : IComplexService
{
    private readonly IFlexKitLogger _logger;

    public ComplexService(IFlexKitLogger logger)  // Excludes from auto-interception
    {
        _logger = logger;
    }

    public async Task<ProcessingResult> ProcessComplexWorkflow(WorkflowRequest request)
    {
        using var activity = _logger.StartActivity("ProcessComplexWorkflow");
        
        var startEntry = LogEntry.CreateStart(nameof(ProcessComplexWorkflow), GetType().FullName!)
            .WithInput(new { RequestId = request.Id, WorkflowType = request.Type })
            .WithFormatter(FormatterType.Json);

        _logger.Log(startEntry);

        try
        {
            // Step 1: Validation
            var validationEntry = LogEntry.CreateStart("ValidateRequest", GetType().FullName!)
                .WithInput(request.Id)
                .WithTarget("Debug");
            _logger.Log(validationEntry);

            await ValidateRequest(request);
            _logger.Log(validationEntry.WithCompletion(success: true));

            // Step 2: Processing  
            var result = await ProcessWorkflow(request);

            // Final completion
            var completionEntry = startEntry
                .WithCompletion(success: true)
                .WithOutput(new { Success = result.Success, ProcessedItems = result.ItemCount });

            _logger.Log(completionEntry);
            return result;
        }
        catch (Exception ex)
        {
            var errorEntry = startEntry.WithCompletion(success: false, exception: ex);
            _logger.Log(errorEntry);
            throw;
        }
    }
}
```

### Best Practices for Interception

1. **Prefer interfaces** for new services - better performance and flexibility
2. **Use virtual methods** when interfaces aren't feasible
3. **Avoid sealed classes** for services that need logging
4. **Design for interception** - consider logging needs during architecture
5. **Monitor startup warnings** - address non-interceptable services
6. **Choose manual logging** for complex scenarios requiring fine-grained control

## Manual Logging Deep Dive

Manual logging provides complete control over logging behavior when automatic interception isn't sufficient. FlexKit offers a comprehensive manual logging API through `IFlexKitLogger` with full integration into the targeting and formatting systems.

### When to Use Manual Logging

Choose manual logging when you need:
- **Fine-grained control** over what gets logged at each step
- **Conditional logging** based on business logic or data values
- **Complex workflows** with multiple decision points
- **Integration with existing logging** that conflicts with interception
- **Performance-critical sections** where you want explicit control
- **Distributed tracing** with custom activity management

### IFlexKitLogger Injection (Excludes Class from Interception)

**Key Rule**: When `IFlexKitLogger` is detected in a constructor, the entire class is automatically excluded from automatic interception.

```csharp
public interface IComplexService
{
    PaymentResult ProcessComplexPayment(PaymentRequest request);
    Task<string> ProcessAsyncOperation(string input);
}

public class ComplexService : IComplexService
{
    private readonly IFlexKitLogger _logger;

    // IFlexKitLogger injection = entire class excluded from interception
    public ComplexService(IFlexKitLogger logger)
    {
        _logger = logger;
    }

    public PaymentResult ProcessComplexPayment(PaymentRequest request)
    {
        // Manual logging with full control
        var startEntry = LogEntry.CreateStart(nameof(ProcessComplexPayment), GetType().FullName!)
            .WithInput(new { RequestId = request.Id, Amount = request.Amount })
            .WithFormatter(FormatterType.Json)
            .WithTarget("Console");

        _logger.Log(startEntry);

        try
        {
            // Business logic with conditional logging
            if (request.Amount > 10000)
            {
                var auditEntry = LogEntry.CreateStart("HighValueTransaction", GetType().FullName!)
                    .WithInput(new { Amount = request.Amount, MerchantId = request.MerchantId })
                    .WithTarget("AuditLog")
                    .WithLevel(LogLevel.Warning);
                _logger.Log(auditEntry);
            }

            var result = new PaymentResult { Success = true };

            var endEntry = startEntry
                .WithCompletion(success: true)
                .WithOutput(new { Success = result.Success });

            _logger.Log(endEntry);
            return result;
        }
        catch (Exception ex)
        {
            var errorEntry = startEntry.WithCompletion(success: false, exception: ex);
            _logger.Log(errorEntry);
            throw;
        }
    }
}
```

### LogEntry Methods

#### CreateStart() - Begin Method Logging
```csharp
public LogEntry CreateStart(string methodName, string typeName)

// Basic usage
var entry = LogEntry.CreateStart(nameof(ProcessPayment), GetType().FullName!);

// With immediate configuration
var entry = LogEntry.CreateStart(nameof(ProcessPayment), GetType().FullName!)
    .WithInput(paymentData)
    .WithTarget("Console")
    .WithLevel(LogLevel.Information);
```

#### WithInput() - Add Input Parameters
```csharp
// Simple input
var entry = startEntry.WithInput(orderId);

// Complex object input
var entry = startEntry.WithInput(new { 
    RequestId = request.Id, 
    Amount = request.Amount,
    CustomerType = request.CustomerType 
});

// Multiple parameters
var entry = startEntry.WithInput(new { username, email, phoneNumber });
```

#### WithOutput() - Add Return Values
```csharp
// Simple return value
var completionEntry = startEntry.WithOutput(result);

// Complex object output
var completionEntry = startEntry.WithOutput(new {
    Success = result.Success,
    TransactionId = result.TransactionId,
    ProcessingTime = stopwatch.ElapsedMilliseconds
});

// Computed values
var completionEntry = startEntry.WithOutput(new {
    TotalProcessed = items.Count,
    SuccessfulCount = items.Count(i => i.Success),
    FailureCount = items.Count(i => !i.Success)
});
```

#### WithCompletion() - Mark Method Completion
```csharp
// Successful completion
var endEntry = startEntry.WithCompletion(success: true);

// Successful completion with output
var endEntry = startEntry
    .WithCompletion(success: true)
    .WithOutput(result);

// Failed completion with exception
var errorEntry = startEntry.WithCompletion(success: false, exception: ex);

// Manual failure without exception
var errorEntry = startEntry
    .WithCompletion(success: false)
    .WithOutput(new { ErrorCode = "VALIDATION_FAILED", Message = validationError });
```

#### Configuration Methods
```csharp
// Formatter specification
var entry = startEntry.WithFormatter(FormatterType.Json);

// Target specification  
var entry = startEntry.WithTarget("Debug");

// Log level specification
var entry = startEntry.WithLevel(LogLevel.Warning);

// Template specification
var entry = startEntry.WithTemplate("PaymentService");

// Method chaining
var entry = LogEntry.CreateStart(nameof(ProcessOrder), GetType().FullName!)
    .WithInput(orderData)
    .WithFormatter(FormatterType.Hybrid)
    .WithTarget("File")
    .WithLevel(LogLevel.Information);
```

### Activity Management for Distributed Tracing

FlexKit integrates with .NET's `System.Diagnostics.Activity` for distributed tracing:

```csharp
public async Task<string> ProcessAsyncOperation(string input)
{
    // Start distributed tracing activity
    using var activity = _logger.StartActivity("ProcessAsyncOperation");

    var startEntry = LogEntry.CreateStart(nameof(ProcessAsyncOperation), GetType().FullName!)
        .WithInput(input)
        .WithTemplate("ComplexService")
        .WithFormatter(FormatterType.CustomTemplate);

    _logger.Log(startEntry);

    try
    {
        // Step 1: Validation with separate activity
        using var validationActivity = _logger.StartActivity("ValidateInput");
        var validationEntry = LogEntry.CreateStart("ValidateInput", GetType().FullName!)
            .WithInput(input)
            .WithTarget("Debug");
        _logger.Log(validationEntry);

        if (string.IsNullOrEmpty(input))
        {
            throw new ArgumentException("Input cannot be empty");
        }

        _logger.Log(validationEntry.WithCompletion(success: true));

        // Step 2: Processing
        await Task.Delay(100);
        var result = $"Processed: {input}";

        var endEntry = startEntry
            .WithCompletion(success: true)
            .WithOutput(result);

        _logger.Log(endEntry);
        return result;
    }
    catch (Exception ex)
    {
        var errorEntry = startEntry.WithCompletion(success: false, exception: ex);
        _logger.Log(errorEntry);
        throw;
    }
}
```

### Complex Workflow Example

```csharp
public class OrderProcessingService : IOrderProcessingService
{
    private readonly IFlexKitLogger _logger;
    private readonly IPaymentService _paymentService;
    private readonly IInventoryService _inventoryService;

    public OrderProcessingService(
        IFlexKitLogger logger,
        IPaymentService paymentService,
        IInventoryService inventoryService)
    {
        _logger = logger;
        _paymentService = paymentService;
        _inventoryService = inventoryService;
    }

    public async Task<OrderResult> ProcessOrderAsync(OrderRequest request)
    {
        using var mainActivity = _logger.StartActivity("ProcessOrder");
        
        var startEntry = LogEntry.CreateStart(nameof(ProcessOrderAsync), GetType().FullName!)
            .WithInput(new { 
                OrderId = request.OrderId, 
                CustomerId = request.CustomerId,
                ItemCount = request.Items.Count,
                TotalAmount = request.TotalAmount 
            })
            .WithFormatter(FormatterType.Json)
            .WithTarget("OrderProcessing");

        _logger.Log(startEntry);

        try
        {
            // Step 1: Inventory Check
            var inventoryEntry = LogEntry.CreateStart("CheckInventory", GetType().FullName!)
                .WithInput(new { Items = request.Items.Select(i => new { i.ProductId, i.Quantity }) })
                .WithTarget("Inventory");
            
            _logger.Log(inventoryEntry);

            var inventoryResult = await _inventoryService.CheckAvailabilityAsync(request.Items);
            
            if (!inventoryResult.AllAvailable)
            {
                var unavailableItems = inventoryResult.UnavailableItems;
                _logger.Log(inventoryEntry
                    .WithCompletion(success: false)
                    .WithOutput(new { UnavailableItems = unavailableItems }));

                return new OrderResult 
                { 
                    Success = false, 
                    ErrorMessage = "Items not available",
                    UnavailableItems = unavailableItems
                };
            }

            _logger.Log(inventoryEntry.WithCompletion(success: true));

            // Step 2: Payment Processing (only for high-value orders)
            if (request.TotalAmount > 1000)
            {
                var paymentEntry = LogEntry.CreateStart("ProcessHighValuePayment", GetType().FullName!)
                    .WithInput(new { Amount = request.TotalAmount, PaymentMethod = request.PaymentMethod })
                    .WithTarget("Payment")
                    .WithLevel(LogLevel.Warning);

                _logger.Log(paymentEntry);

                var paymentResult = await _paymentService.ProcessPaymentAsync(request.Payment);
                
                if (!paymentResult.Success)
                {
                    _logger.Log(paymentEntry
                        .WithCompletion(success: false)
                        .WithOutput(new { ErrorCode = paymentResult.ErrorCode }));

                    return new OrderResult 
                    { 
                        Success = false, 
                        ErrorMessage = paymentResult.ErrorMessage 
                    };
                }

                _logger.Log(paymentEntry
                    .WithCompletion(success: true)
                    .WithOutput(new { TransactionId = paymentResult.TransactionId }));
            }

            // Step 3: Order Completion
            var orderResult = new OrderResult 
            { 
                Success = true, 
                OrderId = request.OrderId,
                ProcessedAt = DateTime.UtcNow
            };

            var completionEntry = startEntry
                .WithCompletion(success: true)
                .WithOutput(new { 
                    OrderId = orderResult.OrderId,
                    ProcessedAt = orderResult.ProcessedAt,
                    ProcessingTimeMs = mainActivity.Duration?.TotalMilliseconds
                });

            _logger.Log(completionEntry);
            return orderResult;
        }
        catch (Exception ex)
        {
            var errorEntry = startEntry
                .WithCompletion(success: false, exception: ex)
                .WithTarget("Errors");
            
            _logger.Log(errorEntry);
            throw;
        }
    }
}
```

### Performance Considerations

#### Manual vs Automatic Logging Trade-offs

| Aspect | Automatic Interception | Manual Logging |
|--------|----------------------|----------------|
| **Setup Effort** | Zero - works immediately | Higher - explicit implementation |
| **Performance** | Optimized (~2-3μs overhead) | Slightly higher (~5-10μs overhead) |
| **Control** | Limited to configuration | Complete programmatic control |
| **Flexibility** | Standard patterns only | Any logging pattern possible |
| **Maintenance** | Automatic with code changes | Requires manual updates |
| **Best Use** | 90% of scenarios | Complex workflows, conditional logic |

#### Optimization Tips for Manual Logging

```csharp
public class OptimizedService : IOptimizedService
{
    private readonly IFlexKitLogger _logger;
    private readonly bool _isDebugEnabled;

    public OptimizedService(IFlexKitLogger logger, IConfiguration config)
    {
        _logger = logger;
        _isDebugEnabled = config.GetValue<bool>("Logging:EnableDebug");
    }

    public void ProcessData(string data)
    {
        // Conditional logging based on configuration
        LogEntry? debugEntry = null;
        
        if (_isDebugEnabled)
        {
            debugEntry = LogEntry.CreateStart("ProcessData_Debug", GetType().FullName!)
                .WithInput(data)
                .WithTarget("Debug");
            _logger.Log(debugEntry);
        }

        try
        {
            // Main processing logic
            var result = ProcessDataInternal(data);

            // Always log completion for important operations
            var completionEntry = LogEntry.CreateStart(nameof(ProcessData), GetType().FullName!)
                .WithCompletion(success: true)
                .WithOutput(new { Success = result != null })
                .WithTarget("Console");

            _logger.Log(completionEntry);

            // Debug completion only if debug enabled
            if (_isDebugEnabled && debugEntry != null)
            {
                _logger.Log(debugEntry.WithCompletion(success: true).WithOutput(result));
            }
        }
        catch (Exception ex)
        {
            // Always log errors
            var errorEntry = LogEntry.CreateStart(nameof(ProcessData), GetType().FullName!)
                .WithCompletion(success: false, exception: ex);
            _logger.Log(errorEntry);
            
            throw;
        }
    }
}
```

### Integration with Automatic Interception

You can mix manual and automatic logging in the same application:

```csharp
// Automatic interception for standard services
public interface IUserService
{
    User GetUser(int userId);
}

[LogBoth]
public class UserService : IUserService
{
    public User GetUser(int userId)  // Automatically logged
    {
        return new User { Id = userId, Name = "Test User" };
    }
}

// Manual logging for complex services
public interface IOrderService
{
    Task<OrderResult> ProcessComplexOrder(OrderRequest request);
}

public class OrderService : IOrderService
{
    private readonly IFlexKitLogger _logger;
    private readonly IUserService _userService;  // Uses automatic logging

    public OrderService(IFlexKitLogger logger, IUserService userService)
    {
        _logger = logger;
        _userService = userService;
    }

    public async Task<OrderResult> ProcessComplexOrder(OrderRequest request)
    {
        // Manual logging for complex workflow
        var entry = LogEntry.CreateStart(nameof(ProcessComplexOrder), GetType().FullName!)
            .WithInput(request.OrderId);
        _logger.Log(entry);

        // Call to automatically logged service
        var user = _userService.GetUser(request.UserId);  // This call automatically logged

        // Continue with manual logging
        var result = await ProcessOrderInternal(request, user);
        _logger.Log(entry.WithCompletion(success: true).WithOutput(result));
        
        return result;
    }
}
```

### Best Practices for Manual Logging

1. **Use sparingly** - Prefer automatic interception for most scenarios
2. **Consistent patterns** - Establish team conventions for manual logging structure
3. **Activity management** - Use `StartActivity` for distributed tracing scenarios
4. **Error handling** - Always log exceptions in try-catch blocks
5. **Performance awareness** - Consider conditional logging for high-frequency operations
6. **Target selection** - Route different log types to appropriate targets
7. **Input/Output logging** - Be consistent about what constitutes input vs output data

## Microsoft Extensions Logging Integration

FlexKit.Logging uses Microsoft.Extensions.Logging (MEL) as its default provider when no specific logging framework extension is installed. This provides seamless integration with .NET's standard logging infrastructure with zero configuration required.

### Zero Configuration Setup

FlexKit.Logging works immediately with MEL - no additional setup required:

```csharp
// Program.cs - That's it!
var host = Host.CreateDefaultBuilder(args)
    .AddFlexConfig()  // MEL integration automatic
    .Build();

await host.RunAsync();
```

**What Happens Automatically:**
- FlexKit detects available MEL providers (Console, Debug, EventSource)
- Configures automatic method interception
- Routes FlexKit logs through MEL infrastructure
- Preserves all existing MEL configuration and providers

### Default MEL Provider Detection

FlexKit automatically detects and integrates with standard MEL providers:

```csharp
// Standard .NET hosting setup
var builder = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.AddDebug();
        logging.AddEventSourceLogger();
    })
    .AddFlexConfig();  // Automatically uses configured MEL providers

var host = builder.Build();
```

**Detected Providers:**
- **Console** - `ILogger` output to console window
- **Debug** - Output to debug window in development
- **EventSource** - ETW (Event Tracing for Windows) integration
- **Custom** - Any other MEL providers registered

### MEL Configuration Integration

FlexKit respects existing MEL configuration and adds its targeting system on top:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "System": "Warning"
    },
    "Console": {
      "IncludeScopes": true,
      "TimestampFormat": "yyyy-MM-dd HH:mm:ss ",
      "FormatterName": "simple"
    }
  },
  "FlexKit": {
    "Logging": {
      "AutoIntercept": true,
      "DefaultTarget": "Console",
      "Services": {
        "MyApp.Services.*": {
          "LogInput": true,
          "Level": "Information"
        }
      },
      "Targets": {
        "Console": {
          "Type": "Console",
          "Enabled": true,
          "Properties": {
            "FormatterType": "Simple",
            "IncludeScopes": true,
            "TimestampFormat": "yyyy-MM-dd HH:mm:ss.fff"
          }
        }
      }
    }
  }
}
```

### Framework Extension Philosophy

FlexKit follows a "zero code change" philosophy for framework extensions:

#### Adding Serilog Support
```bash
# Install extension
dotnet add package FlexKit.Logging.Serilog

# No code changes needed - FlexKit automatically detects and uses Serilog
```

#### Adding NLog Support
```bash
# Install extension
dotnet add package FlexKit.Logging.NLog

# No code changes needed - FlexKit automatically detects and uses NLog
```

#### Adding ZLogger Support
```bash
# Install extension
dotnet add package FlexKit.Logging.ZLogger

# No code changes needed - FlexKit automatically detects and uses ZLogger
```

**Key Principle:** FlexKit extensions automatically replace MEL as the underlying provider while maintaining all FlexKit configuration and functionality.

### MEL Target Configuration

Configure MEL-specific targets through FlexKit's targeting system:

```json
{
  "FlexKit": {
    "Logging": {
      "Targets": {
        "Console": {
          "Type": "Console",
          "Enabled": true,
          "Properties": {
            "FormatterType": "Simple",
            "IncludeScopes": true,
            "TimestampFormat": "yyyy-MM-dd HH:mm:ss.fff ",
            "ColorBehavior": "Enabled",
            "SingleLine": false
          }
        },
        "Debug": {
          "Type": "Debug",
          "Enabled": true,
          "Properties": {
            "LogLevel": "Information"
          }
        },
        "EventSource": {
          "Type": "EventSource",
          "Enabled": true,
          "Properties": {
            "Keywords": "All"
          }
        }
      }
    }
  }
}
```

### MEL Formatter Integration

FlexKit formatters work seamlessly with MEL structured logging:

```csharp
public interface IOrderService
{
    void ProcessOrder(OrderData orderData);
}

public class OrderService : IOrderService
{
    [LogBoth(formatter: "Json")]
    public void ProcessOrder(OrderData orderData)
    {
        Console.WriteLine($"Processing order: {orderData.OrderId}");
        // MEL receives structured log data from FlexKit JSON formatter
    }
}
```

**MEL Output with FlexKit JSON Formatter:**
```json
{
  "timestamp": "2025-01-20T10:30:45.123Z",
  "level": "Information", 
  "method_name": "ProcessOrder",
  "type_name": "OrderService",
  "success": true,
  "duration": 45,
  "input_parameters": {"OrderId": "12345", "Amount": 100.50},
  "output_value": null
}
```

### MEL Scopes Integration

FlexKit integrates with MEL's scope system for correlation:

```csharp
public class PaymentService : IPaymentService
{
    private readonly ILogger<PaymentService> _melLogger;

    public PaymentService(ILogger<PaymentService> melLogger)
    {
        _melLogger = melLogger;
    }

    [LogBoth]
    public bool ProcessPayment(decimal amount)
    {
        // MEL scope for correlation
        using var scope = _melLogger.BeginScope("PaymentProcessing_{PaymentId}", Guid.NewGuid());
        
        // FlexKit logging automatically includes scope information
        Console.WriteLine($"Processing payment: {amount}");
        return amount > 0;
    }
}
```

### Performance with MEL

FlexKit dramatically improves MEL performance:

**Native MEL Manual Logging:**
```csharp
public class SlowOrderService
{
    private readonly ILogger<SlowOrderService> _logger;

    public SlowOrderService(ILogger<SlowOrderService> logger)
    {
        _logger = logger;
    }

    public void ProcessOrder(string orderId)
    {
        // Manual MEL logging - ~270μs overhead
        _logger.LogInformation("Starting order processing for {OrderId}", orderId);
        
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Business logic
            Console.WriteLine($"Processing order: {orderId}");
            
            stopwatch.Stop();
            _logger.LogInformation("Completed order processing for {OrderId} in {Duration}ms", 
                orderId, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Failed to process order {OrderId} after {Duration}ms", 
                orderId, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
```

**FlexKit with MEL - 100x Faster:**
```csharp
public interface IFastOrderService
{
    void ProcessOrder(string orderId);
}

public class FastOrderService : IFastOrderService
{
    [LogBoth] // ~2.7μs overhead - 100x faster
    public void ProcessOrder(string orderId)
    {
        // Same business logic, automatic comprehensive logging
        Console.WriteLine($"Processing order: {orderId}");
    }
}
```

### MEL Provider Configuration Examples

#### Console Provider with Custom Formatting
```json
{
  "Logging": {
    "Console": {
      "FormatterName": "json",
      "FormatterOptions": {
        "SingleLine": true,
        "IncludeScopes": true,
        "TimestampFormat": "yyyy-MM-dd HH:mm:ss.fff",
        "UseUtcTimestamp": true,
        "JsonWriterOptions": {
          "Indented": false
        }
      }
    }
  },
  "FlexKit": {
    "Logging": {
      "DefaultFormatter": "StandardStructured",
      "Targets": {
        "Console": {
          "Type": "Console",
          "Properties": {
            "FormatterType": "Json"
          }
        }
      }
    }
  }
}
```

#### Debug Provider for Development
```json
{
  "FlexKit": {
    "Logging": {
      "Targets": {
        "Debug": {
          "Type": "Debug",
          "Enabled": true,
          "Properties": {
            "LogLevel": "Debug"
          }
        }
      },
      "Services": {
        "MyApp.Development.*": {
          "LogBoth": true,
          "Target": "Debug",
          "Level": "Debug"
        }
      }
    }
  }
}
```

### Custom MEL Provider Integration

FlexKit works with custom MEL providers without modification:

```csharp
// Custom MEL provider registration
public void ConfigureServices(IServiceCollection services)
{
    services.AddLogging(builder =>
    {
        builder.ClearProviders();
        builder.AddConsole();
        builder.AddProvider(new CustomLoggerProvider()); // Custom provider
    });
}

// FlexKit automatically detects and uses custom providers
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(ConfigureServices)
    .AddFlexConfig() // Works with custom providers automatically
    .Build();
```

### Migration from Manual MEL

Converting existing manual MEL logging to FlexKit is straightforward:

**Before - Manual MEL:**
```csharp
public class UserService
{
    private readonly ILogger<UserService> _logger;

    public UserService(ILogger<UserService> logger)
    {
        _logger = logger;
    }

    public User CreateUser(string username, string email)
    {
        _logger.LogInformation("Creating user {Username} with email {Email}", username, email);
        
        try
        {
            var user = new User { Username = username, Email = email };
            _logger.LogInformation("Successfully created user {UserId}", user.Id);
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create user {Username}", username);
            throw;
        }
    }
}
```

**After - FlexKit with MEL:**
```csharp
public interface IUserService
{
    User CreateUser(string username, string email);
}

public class UserService : IUserService
{
    [LogBoth] // Automatic comprehensive logging
    public User CreateUser(string username, string email)
    {
        // Same business logic, automatic logging to MEL
        var user = new User { Username = username, Email = email };
        return user;
    }
}
```

**Benefits of Migration:**
- **100x performance improvement** over manual MEL logging
- **Reduced code complexity** - no manual logging statements
- **Comprehensive coverage** - automatic input/output/timing/exception logging
- **Consistent formatting** - standardized across all methods
- **Zero maintenance** - logging updates automatically with code changes

### Integration with ASP.NET Core

FlexKit works seamlessly with ASP.NET Core's built-in MEL integration:

```csharp
// Program.cs for ASP.NET Core
var builder = WebApplication.CreateBuilder(args);

// Standard ASP.NET Core logging setup
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// FlexKit integration - no additional setup needed
builder.Host.AddFlexConfig();

var app = builder.Build();

// Controllers automatically get method logging
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpPost]
    [LogBoth] // Automatic request/response logging
    public async Task<IActionResult> CreateOrder([FromBody] OrderRequest request)
    {
        var result = await _orderService.ProcessOrderAsync(request);
        return Ok(result);
    }
}
```

### Framework Extension Transition

When adding a FlexKit logging extension, the transition is seamless:

```csharp
// Day 1: Using MEL
var host = Host.CreateDefaultBuilder(args)
    .AddFlexConfig() // Uses MEL automatically
    .Build();

// Day 2: Add Serilog (no code changes)
// PM> Install-Package FlexKit.Logging.Serilog
// FlexKit automatically switches to Serilog

// Day 3: Switch to NLog (no code changes)  
// PM> Uninstall-Package FlexKit.Logging.Serilog
// PM> Install-Package FlexKit.Logging.NLog
// FlexKit automatically switches to NLog
```

**Zero Migration Cost:**
- All FlexKit configuration remains unchanged
- All attributes continue working
- All targeting configuration preserved
- All formatting configuration preserved
- All masking configuration preserved

This approach allows teams to experiment with different logging frameworks without code changes, making FlexKit the universal logging abstraction for .NET applications.

## Best Practices & Troubleshooting

This section provides practical guidance for optimal FlexKit.Logging usage, common configuration patterns, and solutions to frequently encountered issues.

### Performance Optimization Guidelines

#### 1. Strategic Use of NoLog Attribute
Apply `[NoLog]` to high-frequency, non-business-critical methods for dramatic performance gains:

```csharp
public class DataService : IDataService
{
    [LogBoth]
    public DataResult ProcessBusinessData(BusinessRequest request)
    {
        // Important business method - keep logging
        return ProcessData(request);
    }

    [NoLog] // 94% performance improvement
    public string FormatCacheKey(int id) => $"data:{id}";

    [NoLog] // Skip utility methods
    public bool ValidateChecksum(byte[] data) => data.Sum(b => b) % 256 == 0;

    [LogOutput] // Log results but not frequent inputs
    public CacheResult GetCachedData(string key)
    {
        return _cache.Get(key);
    }
}
```

#### 2. Optimize Configuration Patterns
Use specific patterns instead of broad wildcards for better cache performance:

```json
{
  "FlexKit": {
    "Logging": {
      "Services": {
        // ✅ Good - specific patterns
        "MyApp.Services.PaymentService": {
          "LogBoth": true,
          "Level": "Information"
        },
        "MyApp.Services.OrderService": {
          "LogInput": true,
          "Level": "Information"  
        },
        
        // ⚠️ Use sparingly - broad wildcards
        "MyApp.Services.*": {
          "LogInput": true,
          "ExcludeMethodPatterns": ["Get*", "*Cache*", "ToString"]
        }
      }
    }
  }
}
```

#### 3. Method Exclusion for Performance
Exclude common utility methods that don't provide business value:

```json
{
  "FlexKit": {
    "Logging": {
      "Services": {
        "MyApp.Services.*": {
          "LogInput": true,
          "ExcludeMethodPatterns": [
            "ToString",
            "GetHashCode", 
            "Equals",
            "Get*Cache*",
            "*Internal",
            "*Helper",
            "Validate*Format"
          ]
        }
      }
    }
  }
}
```

### Configuration Best Practices

#### 1. Environment-Specific Configuration
Structure configuration for different deployment environments:

```json
// appsettings.json (base configuration)
{
  "FlexKit": {
    "Logging": {
      "AutoIntercept": true,
      "DefaultFormatter": "StandardStructured",
      "Services": {
        "MyApp.Services.*": {
          "LogInput": true,
          "Level": "Information"
        }
      }
    }
  }
}

// appsettings.Production.json (production overrides)
{
  "FlexKit": {
    "Logging": {
      "DefaultTarget": "File",
      "DefaultFormatter": "Json",
      "Services": {
        "MyApp.Services.*": {
          "Level": "Warning",
          "ExcludeMethodPatterns": ["Get*", "*Cache*"]
        }
      }
    }
  }
}

// appsettings.Development.json (development overrides)  
{
  "FlexKit": {
    "Logging": {
      "DefaultTarget": "Console",
      "DefaultFormatter": "SuccessError",
      "Services": {
        "MyApp.Services.*": {
          "LogBoth": true,
          "Level": "Debug"
        }
      }
    }
  }
}
```

#### 2. Service Grouping Strategy
Organize services by functional area for consistent logging:

```json
{
  "FlexKit": {
    "Logging": {
      "Services": {
        // Core business services - comprehensive logging
        "MyApp.Business.*": {
          "LogBoth": true,
          "Level": "Information",
          "Target": "Business"
        },
        
        // Data access - input logging only
        "MyApp.Data.*": {
          "LogInput": true,
          "Level": "Debug",
          "Target": "DataAccess"
        },
        
        // External integrations - full logging with masking
        "MyApp.External.*": {
          "LogBoth": true,
          "Level": "Information",
          "Target": "External",
          "MaskParameterPatterns": ["*key*", "*token*", "*password*"]
        },
        
        // Utilities - minimal logging
        "MyApp.Utilities.*": {
          "LogOutput": true,
          "Level": "Warning",
          "ExcludeMethodPatterns": ["Get*", "*Format*", "*Parse*"]
        }
      }
    }
  }
}
```

### Common Issues & Solutions

#### Issue 1: Logging Not Working
**Symptoms:** Methods not being logged despite configuration

**Diagnostic Steps:**
1. Check if service implements interface or has virtual methods
2. Verify service is registered in DI container
3. Look for startup warnings about non-interceptable services

**Solutions:**
```csharp
// ❌ Problem: No interface, no virtual methods
public class OrderService
{
    public void ProcessOrder(string orderId) { } // Not intercepted
}

// ✅ Solution 1: Add interface
public interface IOrderService
{
    void ProcessOrder(string orderId);
}

public class OrderService : IOrderService
{
    public void ProcessOrder(string orderId) { } // Now intercepted
}

// ✅ Solution 2: Make methods virtual
public class OrderService
{
    public virtual void ProcessOrder(string orderId) { } // Now intercepted
}
```

#### Issue 2: Attribute Configuration Not Applied
**Symptoms:** `[LogInput]`, `[LogBoth]` attributes seem ignored

**Common Causes:**
- Service not registered correctly in DI
- Class has `IFlexKitLogger` injection (automatic exclusion)
- Sealed class or non-virtual methods

**Diagnostic Code:**
```csharp
// Check if service is properly registered
public void ValidateRegistration(IServiceProvider services)
{
    var orderService = services.GetService<IOrderService>();
    Console.WriteLine($"Service registered: {orderService != null}");
    
    // Check if it's a proxy (intercepted)
    var proxyType = orderService?.GetType();
    Console.WriteLine($"Is proxy: {proxyType?.FullName?.Contains("Castle.Proxies")}");
}
```

#### Issue 3: Performance Issues
**Symptoms:** Application slower after adding FlexKit.Logging

**Diagnostic Steps:**
1. Identify high-frequency methods using profiling
2. Check for broad wildcard patterns in configuration
3. Review method exclusion patterns

**Solutions:**
```csharp
// ❌ Problem: High-frequency method with logging
public class CacheService : ICacheService
{
    [LogBoth] // Called thousands of times per second
    public string GetCacheKey(int id) => $"cache:{id}";
}

// ✅ Solution: Add NoLog attribute  
public class CacheService : ICacheService
{
    [NoLog] // 94% performance improvement
    public string GetCacheKey(int id) => $"cache:{id}";
    
    [LogInput] // Keep logging for business methods
    public CacheResult GetData(string key) => _cache.Get(key);
}
```

#### Issue 4: Masking Not Working
**Symptoms:** Sensitive data appearing in logs despite masking configuration

**Common Causes:**
- Parameter masking on non-intercepted methods
- Pattern not matching parameter/property names
- Incorrect precedence understanding

**Solutions:**
```csharp
// ❌ Problem: Non-intercepted method with [Mask]
public class AuthService
{
    public bool ValidateUser(string username, [Mask] string password) // Not intercepted
    {
        return password.Length > 8;
    }
}

// ✅ Solution: Make interceptable
public interface IAuthService
{
    bool ValidateUser(string username, [Mask] string password);
}

public class AuthService : IAuthService
{
    public bool ValidateUser(string username, string password) // Now works
    {
        return password.Length > 8;
    }
}
```

#### Issue 5: Configuration Not Loading
**Symptoms:** Default behavior instead of configured behavior

**Diagnostic Steps:**
1. Verify JSON syntax and structure
2. Check configuration file is included in build
3. Confirm section names match exactly

**Configuration Validation:**
```json
{
  "FlexKit": {  // ✅ Correct section name
    "Logging": {  // ✅ Correct subsection
      "Services": {  // ✅ Correct property name
        "MyApp.Services.PaymentService": {  // ✅ Full type name
          "LogBoth": true,  // ✅ Boolean, not string
          "Level": "Information"  // ✅ Valid log level
        }
      }
    }
  }
}
```

### Production Deployment Checklist

#### 1. Performance Review
- [ ] Applied `[NoLog]` to high-frequency utility methods
- [ ] Configured method exclusion patterns for common methods
- [ ] Tested performance impact under load
- [ ] Verified cache hit ratios > 90%

#### 2. Security Review
- [ ] Sensitive data masked with attributes or patterns
- [ ] PII protection configured appropriately
- [ ] Payment/authentication data not logged
- [ ] Configuration reviewed for data exposure

#### 3. Configuration Review
- [ ] Environment-specific configuration files created
- [ ] Log levels appropriate for production (Warning/Error)
- [ ] Target configuration points to production logging infrastructure
- [ ] Fallback targets configured for reliability

#### 4. Monitoring Setup
- [ ] Application startup warnings reviewed
- [ ] Log volume monitoring configured
- [ ] Error rate monitoring for FlexKit components
- [ ] Performance metrics baseline established

### Development Workflow Recommendations

#### 1. Service Design Guidelines
```csharp
// ✅ Recommended: Interface-based design
public interface IOrderService
{
    Task<OrderResult> ProcessOrderAsync(OrderRequest request);
    OrderStatus GetOrderStatus(string orderId);
}

[LogBoth] // Class-level configuration
public class OrderService : IOrderService
{
    public async Task<OrderResult> ProcessOrderAsync(OrderRequest request)
    {
        // Automatically logged with input/output
        return await ProcessOrderInternal(request);
    }

    [NoLog] // Override for utility method
    public OrderStatus GetOrderStatus(string orderId)
    {
        return _cache.Get<OrderStatus>($"order_status:{orderId}");
    }
}
```

#### 2. Testing Strategy
```csharp
[TestFixture]
public class OrderServiceTests
{
    [Test]
    public void ProcessOrder_ValidRequest_LogsInputAndOutput()
    {
        // Arrange
        var mockLogger = new Mock<IFlexKitLogger>();
        var service = new OrderService(mockLogger.Object);
        var request = new OrderRequest { OrderId = "12345" };

        // Act
        var result = service.ProcessOrder(request);

        // Assert
        mockLogger.Verify(l => l.Log(It.IsAny<LogEntry>()), Times.AtLeastOnce);
        Assert.That(result.Success, Is.True);
    }
}
```

#### 3. Configuration Testing
Create configuration validation tests:

```csharp
[TestFixture] 
public class LoggingConfigurationTests
{
    [Test]
    public void Configuration_AllServicesConfigured()
    {
        // Arrange
        var configuration = LoadTestConfiguration();
        
        // Act
        var loggingConfig = configuration.GetSection("FlexKit:Logging")
            .Get<LoggingConfig>();

        // Assert
        Assert.That(loggingConfig.Services, Contains.Key("MyApp.Services.*"));
        Assert.That(loggingConfig.AutoIntercept, Is.True);
    }
}
```

### Monitoring & Observability

#### 1. Key Metrics to Track
- **Interception cache hit ratio** - Target > 90%
- **Average method interception time** - Target < 3μs
- **Queue processing latency** - Target < 50ns
- **Memory allocation rate** - Monitor for sustained growth
- **Log entry processing throughput** - Ensure queue doesn't accumulate

#### 2. Alerting Recommendations
```json
{
  "alerts": {
    "flexkit_performance_degradation": {
      "condition": "avg_interception_time > 10μs",
      "action": "investigate_performance_bottlenecks"
    },
    "flexkit_queue_backlog": {
      "condition": "log_queue_size > 1000",
      "action": "check_target_availability"
    },
    "flexkit_cache_miss_rate": {
      "condition": "cache_hit_ratio < 85%",
      "action": "review_service_patterns"
    }
  }
}
```

### Migration Strategies

#### From Manual Logging
1. **Identify logging patterns** in existing code
2. **Add interfaces** to services requiring logging
3. **Configure FlexKit** to match existing log levels
4. **Gradually remove** manual logging statements
5. **Validate output** matches previous logging behavior

#### Between Logging Frameworks
1. **Install new FlexKit extension** (e.g., FlexKit.Logging.Serilog)
2. **Test configuration** in staging environment
3. **Deploy without code changes** - framework automatically switches
4. **Monitor performance** and log output
5. **Adjust configuration** as needed for new framework

This systematic approach ensures smooth adoption and optimal performance of FlexKit.Logging in production environments.
