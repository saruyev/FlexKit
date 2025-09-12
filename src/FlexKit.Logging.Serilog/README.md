# FlexKit.Logging.Serilog

High-performance Serilog integration for FlexKit.Logging providing **near-native performance compatibility** with comprehensive auto-instrumentation and **99.3% faster execution** for performance-critical paths through intelligent selective optimization. This extension seamlessly bridges FlexKit.Logging's capabilities with Serilog's powerful structured logging ecosystem while maintaining excellent performance characteristics.

## 🚀 Performance Highlights

- **Near-native performance** with only 2-3% overhead for comprehensive auto-instrumentation
- **99.3% faster execution** for performance-critical paths with NoLog attributes
- **Perfect async compatibility** with zero overhead enhancement
- **Zero-allocation core operations** for log entry creation and complex data handling
- **Strategic optimization framework** providing best-of-both-worlds approach

## 📦 Installation

```bash
dotnet add package FlexKit.Logging.Serilog
```

## 🔧 Quick Start

### Basic Setup

```csharp
using FlexKit.Configuration.Core;

var host = Host.CreateDefaultBuilder(args)
    .AddFlexConfig()  // Auto-detects and configures Serilog
    .Build();

// All interface methods automatically logged to Serilog sinks
var orderService = host.Services.GetService<IOrderService>();
orderService.ProcessOrder("ORDER-001");
```

**Serilog Console Output:**
```
[02:05:23 INF] Method FlexKitLoggingConsoleApp.IOrderService.ProcessOrder executed with success: True
```

## 🎯 Core Features

### 1. Automatic Sink Detection

FlexKit.Serilog automatically detects available Serilog sinks and configures them based on your FlexKit configuration:

```csharp
// Automatically detects and configures:
// - Console sink
// - File sink  
// - Seq sink
// - Elasticsearch sink
// - Database sink
// - Custom sinks from referenced assemblies
```

### 2. Perfect Performance Compatibility

```csharp
public interface IServiceComparison
{
    void StandardMethod(string data);
    string OptimizedMethod(int id);
}

public class ServiceComparison : IServiceComparison
{
    [LogBoth] // 2% overhead vs native Serilog - comprehensive logging
    public void StandardMethod(string data)
    {
        // Business logic with full auto-instrumentation
    }

    [NoLog] // 99.3% faster than native Serilog - performance-critical path
    public string OptimizedMethod(int id)
    {
        // High-frequency method optimized for maximum performance
        return $"result:{id}";
    }
}
```

**Performance Comparison:**
- `StandardMethod`: Near-native Serilog performance with comprehensive auto-instrumentation
- `OptimizedMethod`: Dramatic 99.3% performance improvement over native Serilog

### 3. Structured Logging Excellence

FlexKit.Serilog leverages Serilog's powerful structured logging capabilities with automatic enhancements:

**JSON Formatter Output:**
```
[02:05:23 INF] {"Id": "2d79172e-5b32-4d00-99e6-77b685bed4eb", "MethodName": "ProcessComplexWorkflowAsync", "TypeName": "FlexKitLoggingConsoleApp.ComplexService", "Success": true, "ActivityId": "00-f664db13ab4db56b38e06547ed0722b8-0362bb47765f02e7-00", "ThreadId": 1, "InputParameters": [{"RequestId": "WF-001", "WorkflowType": "Payment", "Amount": 1500.75}], "OutputValue": {"Success": true, "ProcessedAt": "2025-09-10T23:05:23.5828690Z"}, "Duration": 316.06, "DurationSeconds": 0.316, "Target": "Console", "Formatter": "Json", "$type": "LogEntry"}
```

**CustomTemplate Formatter Output:**
```
[02:05:23 INF] 🔧 WORKFLOW: ValidateRequest step completed → null
```

**Hybrid Formatter Output:**
```
[02:05:23 INF] Method ProcessWithHybridFormatter completed | META:  {"TypeName": "FlexKitLoggingConsoleApp.IFormattingService", "Success": true, "ThreadId": 6, "Duration": 0.2, "DurationSeconds": 0, "InputParameters": [{"Name": "data", "Type": "String", "Value": "hybrid data"}], "OutputValue": "Hybrid processed: hybrid data"}
```

### 4. Enhanced Template Processing

FlexKit.Serilog automatically enhances templates with Serilog-specific features:

```csharp
// FlexKit template: "{MethodName} processed {InputParameters} in {Duration}ms"
// Enhanced to:     "{MethodName} processed {@InputParameters} in {Duration:N2}ms"
//                   ↑ Structured logging      ↑ Format specifier
```

**Template Enhancements:**
- `{InputParameters}` → `{@InputParameters}` (destructured objects)
- `{OutputValue}` → `{@OutputValue}` (structured serialization)
- `{Duration}` → `{Duration:N2}` (numeric formatting)
- `{DurationSeconds}` → `{DurationSeconds:N2}` (precision formatting)

### 5. Intelligent Sink Routing

FlexKit.Serilog creates sophisticated filtering for precise message targeting:

```json
{
  "FlexKit": {
    "Logging": {
      "DefaultTarget": "Console",
      "Services": {
        "MyApp.PaymentService": {
          "Target": "File",
          "Formatter": "Json"
        },
        "MyApp.SecurityService": {
          "Target": "Seq",
          "Level": "Warning"
        }
      }
    }
  }
}
```

This creates Serilog sub-loggers that:
- Filter FlexKit logs with `Target` property to specific sinks
- Route ASP.NET Core framework logs to all configured sinks
- Apply appropriate enrichment and formatting per target

### 6. Exception Handling

Comprehensive exception logging with Serilog's structured approach:

```
[02:05:23 ERR] Method FlexKitLoggingConsoleApp.IErrorService.ProcessWithException executed with success: False
```

For detailed exception information with JSON formatter:
```json
{
  "MethodName": "ProcessWithException",
  "Success": false,
  "ExceptionType": "ArgumentException", 
  "ExceptionMessage": "Invalid data provided",
  "StackTrace": "   at FlexKitLoggingConsoleApp.ErrorService.ProcessWithException...",
  "ActivityId": "00-f664db13ab4db56b38e06547ed0722b8-0362bb47765f02e7-00"
}
```

## ⚙️ Configuration Examples

### Complete Serilog Configuration

```json
{
  "FlexKit": {
    "Logging": {
      "AutoIntercept": true,
      "DefaultFormatter": "CustomTemplate",
      "DefaultTarget": "Console",
      "MaxBatchSize": 1000,
      "BatchTimeout": "00:00:02",
      
      "Targets": {
        "Console": {
          "Type": "Console",
          "Enabled": true,
          "Properties": {
            "OutputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
            "Theme": "Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme::Code, Serilog.Sinks.Console"
          }
        },
        "File": {
          "Type": "File",
          "Enabled": true,
          "Properties": {
            "Path": "logs/app-.txt",
            "RollingInterval": "Day",
            "RollOnFileSizeLimit": true,
            "FileSizeLimitBytes": 10485760,
            "RetainedFileCountLimit": 31,
            "OutputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"
          }
        },
        "Seq": {
          "Type": "Seq",
          "Enabled": true,
          "Properties": {
            "ServerUrl": "http://localhost:5341",
            "ApiKey": "your-api-key-here",
            "BufferBaseFilename": "logs/seq-buffer"
          }
        },
        "Elasticsearch": {
          "Type": "Elasticsearch",
          "Enabled": false,
          "Properties": {
            "NodeUris": "http://localhost:9200",
            "IndexFormat": "flexkit-logs-{0:yyyy.MM.dd}",
            "TypeName": "_doc",
            "BatchAction": "IndexManyAsync"
          }
        }
      },

      "Services": {
        "MyApp.PaymentService": {
          "LogBoth": true,
          "Level": "Information",
          "Target": "Seq",
          "Formatter": "Json"
        },
        "MyApp.PerformanceService": {
          "ExcludeMethodPatterns": ["Get*", "*Cache*", "ToString"],
          "Target": "File"
        },
        "MyApp.HighFrequency.*": {
          "LogInput": false,
          "Level": "Warning"
        }
      },

      "Formatters": {
        "Json": {
          "PrettyPrint": false,
          "CustomPropertyNames": {
            "MethodName": "method_name",
            "Duration": "execution_time_ms"
          }
        },
        "CustomTemplate": {
          "DefaultTemplate": "Method {TypeName}.{MethodName} executed with success: {Success}",
          "ServiceTemplates": {
            "PaymentService": "💰 PAYMENT: {MethodName} completed in {Duration:N2}ms → {@OutputValue}",
            "ComplexService": "🔧 WORKFLOW: {MethodName} step completed → {@OutputValue}"
          }
        }
      }
    }
  }
}
```

### Serilog Sink Auto-Detection

FlexKit.Serilog automatically detects available sinks by scanning assemblies:

```csharp
// Detected sinks include:
// - Serilog.Sinks.Console
// - Serilog.Sinks.File
// - Serilog.Sinks.Seq
// - Serilog.Sinks.Elasticsearch
// - Serilog.Sinks.MSSqlServer
// - Serilog.Sinks.Email
// - Custom sinks from referenced assemblies
```

### Advanced Configuration Patterns

**Performance-Optimized Configuration:**
```json
{
  "FlexKit": {
    "Logging": {
      "Services": {
        "MyApp.HighFrequency.*": {
          "ExcludeMethodPatterns": ["Get*", "*Cache*", "*Health*", "ToString", "Equals", "GetHashCode"]
        },
        "MyApp.CriticalPath.*": {
          "LogInput": false,
          "Level": "Error"
        }
      }
    }
  }
}
```

**Multi-Sink Structured Logging:**
```json
{
  "FlexKit": {
    "Logging": {
      "Targets": {
        "Console": {
          "Type": "Console",
          "Properties": {
            "OutputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
          }
        },
        "StructuredFile": {
          "Type": "File",
          "Properties": {
            "Path": "logs/structured-.json",
            "Formatter": "Serilog.Formatting.Compact.CompactJsonFormatter, Serilog.Formatting.Compact"
          }
        },
        "Seq": {
          "Type": "Seq",
          "Properties": {
            "ServerUrl": "http://localhost:5341"
          }
        }
      }
    }
  }
}
```

## 🔗 Integration with Existing Serilog

### Using Existing Serilog Configuration

FlexKit.Serilog can work alongside traditional Serilog configurations:

```csharp
// Existing Serilog setup
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/app.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

// FlexKit will detect and integrate with existing Serilog setup
var host = Host.CreateDefaultBuilder(args)
    .AddFlexConfig()  // Enhances existing Serilog configuration
    .Build();
```

### Enricher Integration

FlexKit.Serilog automatically configures common enrichers:

```csharp
// Automatically enabled enrichers:
// - FromLogContext (built-in)
// - WithThreadId
// - WithMachineName  
// - WithEnvironmentName
// - WithProcessId
// - WithProcessName
```

### Manual IFlexKitLogger Usage

For fine-grained control, use the manual logging API:

```csharp
public class ComplexService(IFlexKitLogger logger)
{
    public async Task<ProcessingResult> ProcessWorkflowAsync(WorkflowRequest request)
    {
        using var activity = logger.StartActivity("ProcessWorkflow");

        var startEntry = LogEntry.CreateStart(nameof(ProcessWorkflowAsync), GetType().FullName!)
            .WithInput(new { RequestId = request.Id, WorkflowType = request.Type })
            .WithFormatter(FormatterType.Json)
            .WithTarget("Seq");

        logger.Log(startEntry);

        try
        {
            var result = await ProcessInternalAsync(request);
            
            var completionEntry = startEntry
                .WithCompletion(success: true)
                .WithOutput(new { result.Success, result.ProcessedAt });

            logger.Log(completionEntry);
            return result;
        }
        catch (Exception ex)
        {
            var errorEntry = startEntry.WithCompletion(success: false, exception: ex);
            logger.Log(errorEntry);
            throw;
        }
    }
}
```

**Serilog Structured Output:**
```json
{
  "Id": "2d79172e-5b32-4d00-99e6-77b685bed4eb",
  "MethodName": "ProcessWorkflowAsync",
  "ActivityId": "00-f664db13ab4db56b38e06547ed0722b8-0362bb47765f02e7-00",
  "InputParameters": [{"RequestId": "WF-001", "WorkflowType": "Payment"}],
  "OutputValue": {"Success": true, "ProcessedAt": "2025-09-10T23:05:23.5828690Z"},
  "Duration": 316.06,
  "@t": "2025-09-10T23:05:23.5828690Z",
  "@l": "Information"
}
```

## ⚡ Performance Optimization

### Compatibility-First Performance

For detailed performance analysis, see: [FlexKit.Logging.Serilog Performance Tests](../../benchmarks/FlexKit.Logging.Serilog.PerformanceTests/README.md)

**Key Performance Characteristics:**

| Scenario | Performance Impact | Use Case |
|----------|-------------------|----------|
| **Near-Native Logging** | 2-3% overhead | Comprehensive auto-instrumentation |
| **NoLog Optimization** | 99.3% faster than native | Performance-critical paths |
| **Perfect Async** | 0% overhead | Async method enhancement |
| **Zero-Allocation Core** | 0 B allocated | Log entry creation |

### Optimization Strategies

**High-Performance Applications:**
```csharp
// Strategic optimization - 99.3% performance improvement
[NoLog] 
public string GenerateCacheKey(int id) => $"cache:key:{id}";

// Balanced performance - 2% overhead with full logging
[LogBoth]
public PaymentResult ProcessPayment(PaymentRequest request) { ... }

// Standard instrumentation - 2% overhead
// Auto-detection provides excellent balance
public void ProcessBusinessData(BusinessData data) { ... }
```

**Production Configuration:**
```json
{
  "FlexKit": {
    "Logging": {
      "MaxBatchSize": 1000,
      "BatchTimeout": "00:00:02",
      "Services": {
        "MyApp.HighFrequency.*": {
          "ExcludeMethodPatterns": ["Get*", "*Cache*", "*Health*", "ToString"]
        },
        "MyApp.CriticalPath.*": {
          "LogInput": false,
          "Level": "Warning"
        }
      }
    }
  }
}
```

### Memory Optimization

**Zero-Allocation Scenarios:**
- Log entry creation: 92.37 ns, 0 B allocated
- Complex data handling: 90.41 ns, 0 B allocated
- Structured logging with perfect Serilog compatibility

**Sustained Load Performance:**
- NoLog optimization: 794 μs per 1K operations (99.3% faster than native)
- Standard logging: 111.4 ms per 1K operations (1% faster than native)
- Excellent memory efficiency maintaining Serilog patterns

## 🏗️ Architecture

### Component Overview

```
FlexKit.Logging.Serilog Architecture:

┌─────────────────────────────────────┐
│          Application Layer          │
├─────────────────────────────────────┤
│      FlexKit.Logging.Core          │  ← Formatters, Interception, Config
├─────────────────────────────────────┤
│    FlexKit.Logging.Serilog         │  ← Bridge Components
│  ┌─────────────────────────────┐    │
│  │   SerilogLogger            │    │  ← MEL → Serilog Bridge
│  │   SerilogLoggerProvider    │    │
│  │   SerilogLogWriter         │    │  ← FlexKit → Serilog Writer
│  │   SerilogMessageTranslator │    │  ← Template Enhancement
│  │   SerilogConfigBuilder     │    │  ← Auto-Configuration
│  │   SerilogComponentDetector │    │  ← Dynamic Detection
│  │   SerilogBackgroundLog     │    │  ← Optimized Processing
│  └─────────────────────────────┘    │
├─────────────────────────────────────┤
│           Serilog Core              │  ← Sinks, Enrichers, Formatters
└─────────────────────────────────────┘
```

### Key Components

**SerilogLogger**: Bridges Microsoft.Extensions.Logging to Serilog with structured property support and context enrichment using `ForContext()`.

**SerilogMessageTranslator**: Enhances FlexKit templates with Serilog-specific features like destructuring operators (`{@Property}`) and format specifiers (`{Value:N2}`).

**SerilogConfigurationBuilder**: Dynamically detects available Serilog sinks and creates filtered sub-loggers for precise message routing with automatic enricher configuration.

**SerilogComponentDetector**: Scans loaded assemblies for Serilog sink and enricher implementations, extracting method signatures for automatic configuration.

**SerilogBackgroundLog**: Leverages Serilog's built-in batching and async processing capabilities, eliminating the need for additional queuing mechanisms.

## 🔧 Advanced Usage

### Custom Sink Integration

FlexKit.Serilog automatically detects custom sinks:

```csharp
public static class LoggerSinkConfigurationExtensions
{
    public static LoggerConfiguration MyCustomSink(
        this LoggerSinkConfiguration sinkConfiguration,
        string connectionString,
        string tableName = "Logs",
        LogEventLevel restrictedToMinimumLevel = LogEventLevel.Verbose)
    {
        // Custom sink implementation
        return sinkConfiguration.Sink(new MyCustomSink(connectionString, tableName), restrictedToMinimumLevel);
    }
}
```

Configure via FlexKit:
```json
{
  "Targets": {
    "CustomDatabase": {
      "Type": "MyCustomSink",
      "Properties": {
        "ConnectionString": "Server=localhost;Database=Logs;",
        "TableName": "ApplicationLogs"
      }
    }
  }
}
```

### Template Enhancement Features

FlexKit.Serilog automatically enhances templates while preserving existing Serilog syntax:

```csharp
// Input template
"Processing {MethodName} with {InputParameters} in {Duration}ms"

// Enhanced template (automatic)
"Processing {MethodName} with {@InputParameters} in {Duration:N2}ms"
//                              ↑ Destructuring    ↑ Format specifier

// Existing Serilog syntax preserved
"User {UserId} processed {@OrderData:j}" // Remains unchanged
```

### Context Enrichment

FlexKit.Serilog automatically enriches log context:

```csharp
public async Task<ProcessingResult> ProcessWorkflowAsync(WorkflowRequest request)
{
    using var activity = Activity.StartActivity("ProcessWorkflow");
    
    // FlexKit automatically enriches context with:
    // - SourceContext (logger category)
    // - ActivityId (distributed tracing)
    // - ThreadId (execution context)
    // - Custom properties from LogEntry
    
    var result = await ProcessInternalAsync(request);
    return result;
}
```

**Enriched Output:**
```json
{
  "@t": "2025-09-10T23:05:23.5828690Z",
  "@l": "Information", 
  "SourceContext": "FlexKitLoggingConsoleApp.ComplexService",
  "ActivityId": "00-f664db13ab4db56b38e06547ed0722b8-0362bb47765f02e7-00",
  "ThreadId": 1,
  "MethodName": "ProcessWorkflowAsync",
  "InputParameters": [{"RequestId": "WF-001"}]
}
```

### Integration with ASP.NET Core

FlexKit.Serilog automatically bridges all ASP.NET Core framework logs:

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // All ASP.NET Core logs automatically routed to Serilog
        services.AddControllers();
    }
    
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseRouting();
        app.UseEndpoints(endpoints => endpoints.MapControllers());
    }
}
```

**Framework Log Output:**
```
[02:05:23 INF] Request starting HTTP/1.1 GET http://localhost:5000/api/orders
[02:05:23 INF] Executing endpoint 'OrderController.GetOrders'
```

## 📊 Performance Monitoring

### Compatibility Metrics

Track these key performance indicators for optimal results:

**Compatibility Metrics:**
- Serilog performance ratio: Target <105% of native performance
- NoLog optimization coverage: Target >50% for performance-critical applications
- Cache hit ratio: Target >95% for frequently called methods
- Async overhead: Target 0% for async operations

**Optimization Metrics:**
- Hot path performance improvement: Monitor NoLog effectiveness
- Memory allocation patterns: Ensure zero-allocation core operations
- Structured logging efficiency: Track destructuring performance
- Context enrichment overhead: Monitor automatic enrichment impact

### Health Checks

```csharp
public class SerilogCompatibilityHealthCheck : IHealthCheck
{
    private readonly LoggingConfig _config;
    private readonly global::Serilog.ILogger _serilogLogger;
    
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        var isHealthy = _serilogLogger != null && 
                       IsSerilogConfigured();
                       
        var metrics = new Dictionary<string, object>
        {
            ["SerilogConfigured"] = isHealthy,
            ["NoLogOptimization"] = CalculateNoLogCoverage(),
            ["PerformanceCompatibility"] = CalculateCompatibilityRatio()
        };
                       
        return Task.FromResult(isHealthy 
            ? HealthCheckResult.Healthy("Serilog compatibility excellent", metrics)
            : HealthCheckResult.Unhealthy("Serilog configuration issue detected"));
    }
}
```

## 🚀 Migration from Native Serilog

### Migration Strategy

**Step 1: Install FlexKit.Logging.Serilog**
```bash
dotnet add package FlexKit.Logging.Serilog
```

**Step 2: Update Startup Configuration**
```csharp
// Before: Native Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/app.txt")
    .CreateLogger();

// After: FlexKit.Serilog (maintains existing config)
var host = Host.CreateDefaultBuilder(args)
    .AddFlexConfig()  // Enhances existing Serilog
    .Build();
```

**Step 3: Optional Performance Optimization**
```csharp
// Before: Manual Serilog calls
public class PaymentService
{
    private static readonly ILogger Logger = Log.ForContext<PaymentService>();
    
    public PaymentResult ProcessPayment(PaymentRequest request)
    {
        Logger.Information("Processing payment: {@Request}", request);
        try
        {
            var result = ProcessInternal(request);
            Logger.Information("Payment processed: {@Result}", result);
            return result;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Payment processing failed");
            throw;
        }
    }
}

// After: Zero-configuration FlexKit with optimization
public class PaymentService : IPaymentService
{
    [LogBoth] // 2% overhead for critical operations
    public PaymentResult ProcessPayment(PaymentRequest request)
    {
        // No logging code needed - automatically instrumented
        var result = ProcessInternal(request);
        return result;
    }
    
    [NoLog] // 99.3% performance improvement for hot paths
    public string GenerateTransactionId() => Guid.NewGuid().ToString();
}
```

### Performance Comparison

**Before Migration (Native Serilog):**
- Manual logging calls: ~136.1μs per operation
- Manual instrumentation required
- Excellent structured logging capabilities
- Manual performance optimization needed

**After Migration (FlexKit.Serilog):**
- Auto-instrumentation: ~140.5μs per operation (3% overhead)
- Zero manual logging code required
- Enhanced structured logging with automatic improvements  
- Strategic optimization available (99.3% improvement with NoLog)

## 🔍 Troubleshooting

### Common Issues

**Issue**: Serilog sinks not detected
```csharp
// Solution: Ensure Serilog sink packages are referenced
// Check assembly loading
var logger = new LoggerConfiguration().CreateLogger(); // Forces loading
```

**Issue**: Template enhancements not applied
```csharp
// Solution: Verify FlexKit isn't detecting existing Serilog syntax
// Check for existing {@Property} or {Property:format} patterns
```

**Issue**: Performance not as expected
```csharp
// Solution: Apply NoLog to high-frequency methods
[NoLog]
public string GetCacheKey(int id) => $"cache:{id}";
```

### Debug Information

Enable Serilog self-logging for troubleshooting:

```csharp
// Enable Serilog internal debugging
Serilog.Debugging.SelfLog.Enable(Console.WriteLine);
```

Check detected sinks:
```csharp
var detector = new SerilogComponentDetector();
var sinks = detector.DetectAvailableSinks();
foreach (var sink in sinks)
{
    Console.WriteLine($"Detected: {sink.Key} - {sink.Value.Method}");
}
```

## 📚 Related Documentation

For comprehensive FlexKit.Logging documentation, see:

- **[Core Documentation](../FlexKit.Logging/README.md)**: Complete FlexKit.Logging features and configuration
- **[Configuration Guide](../FlexKit.Logging/README.md#configuration-based-logging)**: Service-level configuration patterns  
- **[Formatting System](../FlexKit.Logging/README.md#formatting--templates)**: Available formatters and templates
- **[Data Masking](../FlexKit.Logging/README.md#data-masking-system)**: PII protection and sensitive data handling
- **[Performance Benchmarks](../../benchmarks/FlexKit.Logging.Serilog.PerformanceTests/README.md)**: Detailed performance analysis and optimization strategies
- **[Targeting System](../FlexKit.Logging/README.md#targeting-system)**: Multi-destination logging configuration

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](../../LICENSE) file for details.

---

**FlexKit.Logging.Serilog** - Enhance your Serilog applications with near-native performance compatibility and strategic optimization opportunities. Perfect for maintaining Serilog's excellent structured logging performance while gaining dramatic optimization potential through intelligent selective enhancement.