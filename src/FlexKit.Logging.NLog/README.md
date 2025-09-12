# FlexKit.Logging.NLog

High-performance NLog integration for FlexKit.Logging providing strategic performance optimization with **94% faster execution** for performance-critical paths through intelligent selective instrumentation. This extension bridges FlexKit.Logging's capabilities with NLog's powerful target ecosystem while enabling dramatic performance improvements where they matter most.

## 🚀 Performance Highlights

- **94% faster execution** for performance-critical paths with NoLog attributes
- **Strategic optimization framework** rather than universal performance improvement
- **Zero-allocation core operations** for log entry creation and complex data handling
- **Selective instrumentation** enabling both performance and comprehensive observability
- **Negligible async overhead** with enhanced logging capabilities

## 📦 Installation

```bash
dotnet add package FlexKit.Logging.NLog
```

## 🔧 Quick Start

### Basic Setup

```csharp
using FlexKit.Configuration.Core;

var host = Host.CreateDefaultBuilder(args)
    .AddFlexConfig()  // Auto-detects and configures NLog
    .Build();

// All interface methods automatically logged to NLog targets
var orderService = host.Services.GetService<IOrderService>();
orderService.ProcessOrder("ORDER-001");
```

**NLog Output:**
```
2025-09-11 01:54:02.5294|INFO|Console|Method "FlexKitLoggingConsoleApp.IOrderService"."ProcessOrder" executed with success: true
```

## 🎯 Core Features

### 1. Automatic Target Detection

FlexKit.NLog automatically detects available NLog targets and configures them based on your FlexKit configuration:

```csharp
// Automatically detects and configures:
// - Console target
// - File target
// - Database target
// - Mail target
// - Network target
// - Custom targets from referenced assemblies
```

### 2. Strategic Performance Optimization

```csharp
public interface IPerformanceService
{
    void ProcessBusinessCriticalData(string data);
    string GenerateCacheKey(int id);
}

public class PerformanceService : IPerformanceService
{
    [LogBoth] // 2% faster than native NLog
    public void ProcessBusinessCriticalData(string data)
    {
        // Business logic with full logging
    }

    [NoLog] // 94% faster than native NLog
    public string GenerateCacheKey(int id)
    {
        // High-frequency method optimized for performance
        return $"cache:key:{id}";
    }
}
```

**Performance Impact:**
- `ProcessBusinessCriticalData`: Comprehensive logging with 2% performance improvement
- `GenerateCacheKey`: 94% faster execution, 92% less memory usage

### 3. Advanced Formatter Integration

FlexKit formatters work seamlessly with NLog's structured logging capabilities:

**JSON Formatter Output:**
```
2025-09-11 01:54:02.5831|INFO|Console|{"Id":"849c11c7-516c-4b03-ab8f-bff5ad05a0cc", "MethodName":"ProcessComplexWorkflowAsync", "TypeName":"FlexKitLoggingConsoleApp.ComplexService", "Success":true, "ActivityId":"00-9b9ef85b6ae0c5059defe11be7f92183-ee0f7bd9103f6dd5-00", "ThreadId":1, "InputParameters":[{"RequestId":"WF-001", "WorkflowType":"Payment", "Amount":1500.75}], "OutputValue":{"Success":true, "ProcessedAt":"2025-09-10T22:54:02.914147Z"}, "Duration":332.08, "DurationSeconds":0.332, "Target":"Console", "Formatter":"Json"}
```

**CustomTemplate Formatter Output:**
```
2025-09-11 01:54:02.6109|INFO|Console|🔧 WORKFLOW: "ValidateRequest" step completed → NULL
```

**Hybrid Formatter Output:**
```
2025-09-11 01:54:02.9330|INFO|Console|Method "ProcessWithHybridFormatter" completed | META:  {"TypeName":"FlexKitLoggingConsoleApp.IFormattingService","Success":true,"ThreadId":6,"Timestamp":"2025-09-10 22:54:02 +00:00","Id":"e660124d-4357-4592-8ca4-f1ab88eaef38","ActivityId":null,"Duration":0.17,"DurationSeconds":0.0,"InputParameters":[{"Name":"data", "Type":"String", "Value":"hybrid data"}],"OutputValue":"Hybrid processed: hybrid data"}
```

### 4. Structured Logging with Properties

NLog's structured logging capabilities are fully utilized:

```csharp
public async Task<ProcessingResult> ProcessWorkflowAsync(WorkflowRequest request)
{
    using var activity = Activity.StartActivity("ProcessWorkflow");
    
    // FlexKit automatically adds structured properties to NLog
    var result = await ProcessInternalAsync(request);
    return result;
}
```

**Structured Output:**
```json
{
  "MethodName": "ProcessWorkflowAsync",
  "ActivityId": "00-9b9ef85b6ae0c5059defe11be7f92183-ee0f7bd9103f6dd5-00",
  "InputParameters": [{"RequestId": "WF-001", "WorkflowType": "Payment"}],
  "OutputValue": {"Success": true, "ProcessedAt": "2025-09-10T22:54:02.914147Z"},
  "Duration": 332.08
}
```

### 5. Intelligent Target Routing

FlexKit.NLog creates sophisticated routing rules for precise message targeting:

```json
{
  "FlexKit": {
    "Logging": {
      "DefaultTarget": "Console",
      "Services": {
        "MyApp.PaymentService": {
          "Target": "AuditFile",
          "Formatter": "Json"
        },
        "MyApp.SecurityService": {
          "Target": "SecurityFile",
          "Level": "Warning"
        }
      }
    }
  }
}
```

This creates NLog rules that:
- Route FlexKit logs with `Target` property to specific targets
- Bridge ASP.NET Core framework logs to all targets
- Apply appropriate filtering and levels per target

### 6. Exception Handling

Comprehensive exception logging with structured data:

```
2025-09-11 01:54:02.9498|ERROR|Console|Method "FlexKitLoggingConsoleApp.IErrorService"."ProcessWithException" executed with success: false
```

For detailed exception information, use JSON formatter:
```json
{
  "MethodName": "ProcessWithException",
  "Success": false,
  "ExceptionType": "ArgumentException",
  "ExceptionMessage": "Invalid data provided",
  "StackTrace": "   at FlexKitLoggingConsoleApp.ErrorService.ProcessWithException..."
}
```

## ⚙️ Configuration Examples

### Complete NLog Configuration

```json
{
  "FlexKit": {
    "Logging": {
      "AutoIntercept": true,
      "DefaultFormatter": "CustomTemplate",
      "DefaultTarget": "Console",
      "MaxBatchSize": 100,
      "BatchTimeout": "00:00:01",
      
      "Targets": {
        "Console": {
          "Type": "Console",
          "Enabled": true,
          "Properties": {
            "Layout": "${date:format=yyyy-MM-dd HH\\:mm\\:ss.ffff}|${level:uppercase=true}|${logger}|${message} ${exception:format=tostring}"
          }
        },
        "File": {
          "Type": "File",
          "Enabled": true,
          "Properties": {
            "FileName": "logs/app-${shortdate}.log",
            "Layout": "${longdate} [${level:uppercase=true}] ${logger} - ${message} ${exception:format=tostring}",
            "ArchiveEvery": "Day",
            "ArchiveNumbering": "Rolling",
            "MaxArchiveFiles": 7
          }
        },
        "Database": {
          "Type": "Database",
          "Enabled": false,
          "Properties": {
            "ConnectionString": "Server=localhost;Database=Logs;Trusted_Connection=true;",
            "CommandText": "INSERT INTO Logs(Timestamp, Level, Logger, Message) VALUES(@timestamp, @level, @logger, @message)",
            "Parameters": [
              {
                "Name": "@timestamp",
                "Layout": "${date}"
              },
              {
                "Name": "@level", 
                "Layout": "${level}"
              },
              {
                "Name": "@logger",
                "Layout": "${logger}"
              },
              {
                "Name": "@message",
                "Layout": "${message}"
              }
            ]
          }
        }
      },

      "Services": {
        "MyApp.PaymentService": {
          "LogBoth": true,
          "Level": "Information",
          "Target": "Database",
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
          "DefaultTemplate": "Method \"{TypeName}\".\"{MethodName}\" executed with success: {Success}",
          "ServiceTemplates": {
            "PaymentService": "💰 PAYMENT: \"{MethodName}\" completed in {Duration}ms → {OutputValue}",
            "ComplexService": "🔧 WORKFLOW: \"{MethodName}\" step completed → {OutputValue}"
          }
        }
      }
    }
  }
}
```

### NLog Target Auto-Detection

FlexKit.NLog automatically detects available targets by scanning assemblies:

```csharp
// Detected targets include:
// - NLog.Targets.ConsoleTarget
// - NLog.Targets.FileTarget
// - NLog.Targets.DatabaseTarget
// - NLog.Targets.MailTarget
// - NLog.Targets.NetworkTarget
// - NLog.Targets.WebServiceTarget
// - Custom targets from referenced assemblies
```

### Advanced Configuration Patterns

**Performance-Optimized Configuration:**
```json
{
  "FlexKit": {
    "Logging": {
      "Services": {
        "MyApp.HighFrequency.*": {
          "ExcludeMethodPatterns": ["Get*", "*Cache*", "ToString", "Equals", "GetHashCode"]
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

**Multi-Environment Configuration:**
```json
{
  "FlexKit": {
    "Logging": {
      "Targets": {
        "Console": {
          "Type": "Console",
          "Enabled": true
        },
        "File": {
          "Type": "File", 
          "Enabled": true,
          "Properties": {
            "FileName": "logs/${environment}-${shortdate}.log"
          }
        }
      }
    }
  },
  "Production": {
    "FlexKit": {
      "Logging": {
        "Services": {
          "*": {
            "Level": "Warning",
            "ExcludeMethodPatterns": ["Get*", "*Health*", "*Ping*"]
          }
        }
      }
    }
  }
}
```

## 🔗 Integration with Existing NLog

### Using Existing NLog.config

FlexKit.NLog can work alongside traditional NLog.config files:

```xml
<!-- NLog.config -->
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
      
  <targets>
    <target xsi:type="File" name="logfile" 
            fileName="logs/${shortdate}.log"
            layout="${longdate} ${level:uppercase=true} ${logger} ${message} ${exception:format=tostring}" />
    
    <target xsi:type="Console" name="console"
            layout="${time} [${level:uppercase=true}] ${message} ${exception:format=tostring}" />
  </targets>

  <rules>
    <logger name="*" minlevel="Info" writeTo="logfile" />
    <logger name="*" minlevel="Debug" writeTo="console" />
  </rules>
</nlog>
```

FlexKit will detect and integrate with these targets automatically.

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
            .WithTarget("File");

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

**NLog Output:**
```json
{
  "Id": "849c11c7-516c-4b03-ab8f-bff5ad05a0cc",
  "MethodName": "ProcessWorkflowAsync",
  "ActivityId": "00-9b9ef85b6ae0c5059defe11be7f92183-ee0f7bd9103f6dd5-00",
  "InputParameters": [{"RequestId": "WF-001", "WorkflowType": "Payment"}],
  "OutputValue": {"Success": true, "ProcessedAt": "2025-09-10T22:54:02.914147Z"},
  "Duration": 332.08
}
```

## ⚡ Performance Optimization

### Strategic Performance Framework

For detailed performance analysis, see: [FlexKit.Logging.NLog Performance Tests](../../benchmarks/FlexKit.Logging.NLog.PerformanceTests/README.md)

**Key Performance Characteristics:**

| Scenario | Performance Impact | Use Case |
|----------|-------------------|----------|
| **NoLog Attribute** | 94% faster than native NLog | Performance-critical paths |
| **Auto Detection** | 6% faster than native NLog | Balanced observability |
| **LogBoth Attribute** | 2% faster than native NLog | Comprehensive logging |
| **Full Instrumentation** | 40x overhead vs native | Development/debugging |

### Optimization Strategies

**High-Performance Applications:**
```csharp
// Hot path optimization - 94% performance improvement
[NoLog] 
public string GenerateCacheKey(int id) => $"cache:key:{id}";

// Business logic with balanced performance - 6% improvement
[LogInput] // Auto-detection provides good balance
public void ProcessBusinessData(BusinessData data) { ... }

// Critical operations with full visibility - 2% improvement
[LogBoth]
public PaymentResult ProcessPayment(PaymentRequest request) { ... }
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
- Log entry creation: 93.58 ns, 0 B allocated
- Complex data handling: 91.86 ns, 0 B allocated
- Background queue operations: 11.94 μs with controlled allocation

**Sustained Load Performance:**
- NoLog optimization: 833 μs per 1K operations (94% faster than native)
- Standard logging: 16,248 μs per 1K operations (comparable to native)
- Excellent memory efficiency with NoLog optimization

## 🏗️ Architecture

### Component Overview

```
FlexKit.Logging.NLog Architecture:

┌─────────────────────────────────────┐
│          Application Layer          │
├─────────────────────────────────────┤
│      FlexKit.Logging.Core          │  ← Formatters, Interception, Config
├─────────────────────────────────────┤
│      FlexKit.Logging.NLog          │  ← Bridge Components
│  ┌─────────────────────────────┐    │
│  │   NLogLogger               │    │  ← MEL → NLog Bridge
│  │   NLogLoggerProvider       │    │
│  │   NLogLogWriter            │    │  ← FlexKit → NLog Writer
│  │   NLogMessageTranslator    │    │  ← Template Translation
│  │   NLogConfigurationBuilder │    │  ← Auto-Configuration
│  │   NLogTargetDetector       │    │  ← Dynamic Detection
│  │   NLogBackgroundLog        │    │  ← Optimized Queuing
│  └─────────────────────────────┘    │
├─────────────────────────────────────┤
│             NLog Core               │  ← Targets, Layouts, Rules
└─────────────────────────────────────┘
```

### Key Components

**NLogLogger**: Bridges Microsoft.Extensions.Logging to NLog with structured property support and proper level conversion.

**NLogMessageTranslator**: Converts FlexKit template syntax `{Property}` to NLog layout renderer syntax `${@Property}` for seamless integration.

**NLogConfigurationBuilder**: Dynamically detects available NLog targets and creates sophisticated routing rules with filtering for FlexKit vs. framework logs.

**NLogTargetDetector**: Scans loaded assemblies for NLog target implementations and extracts configurable properties for automatic setup.

**NLogBackgroundLog**: Leverages NLog's built-in async processing and batching capabilities, eliminating the need for additional queuing mechanisms.

## 🔧 Advanced Usage

### Custom Target Integration

FlexKit.NLog automatically detects custom targets:

```csharp
[Target("MyCustomTarget")]
public class CustomBusinessTarget : TargetWithLayout
{
    public string BusinessUnit { get; set; }
    public string ApplicationName { get; set; }
    
    protected override void Write(LogEventInfo logEvent)
    {
        // Custom business logging implementation
    }
}
```

Configure via FlexKit:
```json
{
  "Targets": {
    "Business": {
      "Type": "MyCustomTarget",
      "Properties": {
        "BusinessUnit": "Sales",
        "ApplicationName": "CRM",
        "Layout": "${date} [${BusinessUnit}] ${message}"
      }
    }
  }
}
```

### Layout Renderer Integration

FlexKit.NLog works seamlessly with NLog's layout renderers:

```json
{
  "Targets": {
    "File": {
      "Type": "File",
      "Properties": {
        "FileName": "logs/${event-properties:Target:whenEmpty=general}-${shortdate}.log",
        "Layout": "${longdate} [${event-properties:ThreadId}] ${level:uppercase=true} ${logger} - ${message} ${exception:format=tostring}"
      }
    }
  }
}
```

### Async Target Optimization

FlexKit.NLog automatically wraps synchronous targets with AsyncWrapper for optimal performance:

```csharp
// Automatically wrapped with AsyncWrapper:
// - FileTarget → AsyncWrapper(FileTarget)
// - DatabaseTarget → AsyncWrapper(DatabaseTarget)
// - ConsoleTarget → AsyncWrapper(ConsoleTarget)

// Targets that handle async internally are not wrapped:
// - NetworkTarget (already async)
// - WebServiceTarget (already async)
```

### Integration with ASP.NET Core

FlexKit.NLog automatically bridges all ASP.NET Core framework logs:

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // All ASP.NET Core logs automatically routed to NLog
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
2025-09-11 01:54:02.5294|INFO|Microsoft.AspNetCore.Hosting.Diagnostics|Request starting HTTP/1.1 GET http://localhost:5000/api/orders
2025-09-11 01:54:02.5699|INFO|Microsoft.AspNetCore.Routing.EndpointMiddleware|Executing endpoint 'OrderController.GetOrders'
```

## 📊 Performance Monitoring

### Strategic Performance Metrics

Track these key performance indicators for optimal results:

**Performance Optimization Metrics:**
- NoLog coverage ratio: Target >70% for performance-critical applications
- Cache hit ratio: Target >95% for frequently called methods
- Average method interception time: Target <20μs for standard operations
- Memory allocation patterns during sustained load

**Operational Metrics:**
- NLog target health and throughput
- Background processing efficiency
- Exception handling performance
- Configuration cache effectiveness

### Performance Health Checks

```csharp
public class NLogPerformanceHealthCheck : IHealthCheck
{
    private readonly LoggingConfig _config;
    
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        var isHealthy = LogManager.Configuration != null && 
                       LogManager.Configuration.AllTargets.Any();
                       
        var metrics = new Dictionary<string, object>
        {
            ["ConfiguredTargets"] = LogManager.Configuration?.AllTargets.Count() ?? 0,
            ["NoLogOptimization"] = CalculateNoLogCoverage(),
            ["CacheHitRatio"] = GetCacheEfficiency()
        };
                       
        return Task.FromResult(isHealthy 
            ? HealthCheckResult.Healthy("NLog performance optimized", metrics)
            : HealthCheckResult.Unhealthy("NLog configuration issue detected"));
    }
}
```

## 🚀 Migration from Native NLog

### Migration Strategy

**Step 1: Install FlexKit.Logging.NLog**
```bash
dotnet add package FlexKit.Logging.NLog
```

**Step 2: Update Startup Configuration**
```csharp
// Before: Native NLog
LogManager.LoadConfiguration("NLog.config");
var logger = LogManager.GetCurrentClassLogger();

// After: FlexKit.NLog
var host = Host.CreateDefaultBuilder(args)
    .AddFlexConfig()  // Automatically configures NLog
    .Build();
```

**Step 3: Remove Manual Logging Code**
```csharp
// Before: Manual NLog calls
public class PaymentService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    
    public PaymentResult ProcessPayment(PaymentRequest request)
    {
        Logger.Info("Processing payment: {Amount}", request.Amount);
        try
        {
            var result = ProcessInternal(request);
            Logger.Info("Payment processed successfully: {TransactionId}", result.TransactionId);
            return result;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Payment processing failed");
            throw;
        }
    }
}

// After: Zero-configuration FlexKit
public class PaymentService : IPaymentService // Interface required for auto-instrumentation
{
    public PaymentResult ProcessPayment(PaymentRequest request)
    {
        // No logging code needed - automatically instrumented
        var result = ProcessInternal(request);
        return result;
    }
}
```

**Step 4: Optimize Performance (Optional)**
```csharp
public class PaymentService : IPaymentService
{
    [LogBoth] // Full instrumentation for critical operations
    public PaymentResult ProcessPayment(PaymentRequest request)
    {
        var result = ProcessInternal(request);
        return result;
    }
    
    [NoLog] // 94% performance improvement for hot paths
    public string GenerateTransactionId() => Guid.NewGuid().ToString();
}
```

### Performance Comparison

**Before Migration (Native NLog):**
- Manual logging calls: ~402ns per operation
- Manual instrumentation required
- No performance optimization strategies
- Consistent overhead regardless of method importance

**After Migration (FlexKit.NLog):**
- Strategic optimization: 94% faster for hot paths, 6% faster for standard operations
- Zero manual logging code
- Intelligent performance optimization
- Selective overhead based on method criticality

## 🔍 Troubleshooting

### Common Issues

**Issue**: NLog targets not detected
```csharp
// Solution: Ensure NLog assemblies are loaded
var _ = LogManager.Configuration; // Forces target discovery
```

**Issue**: Performance degradation
```csharp
// Solution: Apply NoLog to high-frequency methods
[NoLog]
public string GetCacheKey(int id) => $"cache:{id}";
```

**Issue**: Configuration not applied
```csharp
// Solution: Verify FlexKit configuration structure
{
  "FlexKit": { // Must be "FlexKit", not "NLog"
    "Logging": {
      "Targets": {
        // Configuration here
      }
    }
  }
}
```

### Debug Information

Enable debug output for troubleshooting:

```csharp
// Enable NLog internal logging
LogManager.Configuration.Variables["nlog.internalLogLevel"] = "Debug";
LogManager.Configuration.Variables["nlog.internalLogFile"] = "nlog-internal.log";
```

Check detected targets:
```csharp
var detector = new NLogTargetDetector();
var targets = detector.DetectAvailableTargets();
foreach (var target in targets)
{
    Console.WriteLine($"Detected: {target.Key} - {target.Value.TargetType}");
}
```

## 📚 Related Documentation

For comprehensive FlexKit.Logging documentation, see:

- **[Core Documentation](../FlexKit.Logging/README.md)**: Complete FlexKit.Logging features and configuration
- **[Configuration Guide](../FlexKit.Logging/README.md#configuration-based-logging)**: Service-level configuration patterns  
- **[Formatting System](../FlexKit.Logging/README.md#formatting--templates)**: Available formatters and templates
- **[Data Masking](../FlexKit.Logging/README.md#data-masking-system)**: PII protection and sensitive data handling
- **[Performance Benchmarks](../../benchmarks/FlexKit.Logging.NLog.PerformanceTests/README.md)**: Detailed performance analysis and optimization strategies
- **[Targeting System](../FlexKit.Logging/README.md#targeting-system)**: Multi-destination logging configuration

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](../../LICENSE) file for details.

---

**FlexKit.Logging.NLog** - Transform your NLog applications with strategic performance optimization and zero-configuration auto-instrumentation. Perfect for applications requiring both exceptional performance and comprehensive observability through intelligent selective optimization.