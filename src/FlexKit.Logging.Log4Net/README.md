# FlexKit.Logging.Log4Net

High-performance Log4Net integration for FlexKit.Logging providing comprehensive auto-instrumentation with **81x performance improvement** over native Log4Net. This extension bridges FlexKit.Logging's capabilities with Log4Net's robust appender ecosystem while maintaining exceptional runtime performance.

## 🚀 Performance Highlights

- **81x faster execution** than native Log4Net (1.41μs vs 114.6μs)
- **Zero-configuration auto-instrumentation** with minimal overhead
- **Sub-microsecond response times** for method interception
- **Superior memory efficiency** with optimal allocation patterns
- **Production-ready scalability** with excellent queue performance

## 📦 Installation

```bash
dotnet add package FlexKit.Logging.Log4Net
```

## 🔧 Quick Start

### Basic Setup

```csharp
using FlexKit.Configuration.Core;

var host = Host.CreateDefaultBuilder(args)
    .AddFlexConfig()  // Auto-detects and configures Log4Net
    .Build();

// All interface methods automatically logged to Log4Net appenders
var orderService = host.Services.GetService<IOrderService>();
orderService.ProcessOrder("ORDER-001");
```

**Console Output (Log4Net PatternLayout):**
```
2025-09-11 01:33:03,620 [5] INFO  Console - Method FlexKitLoggingConsoleApp.IOrderService.ProcessOrder executed with success: True
```

## 🎯 Core Features

### 1. Automatic Appender Detection

FlexKit.Log4Net automatically detects available Log4Net appenders and configures them based on your FlexKit configuration:

```csharp
// Automatically detects and configures:
// - ConsoleAppender
// - FileAppender  
// - RollingFileAppender
// - DebugAppender
// - And any custom appenders
```

### 2. Zero-Configuration Auto-Instrumentation

```csharp
public interface IPaymentService
{
    PaymentResult ProcessPayment(PaymentRequest request);
}

public class PaymentService : IPaymentService
{
    public PaymentResult ProcessPayment(PaymentRequest request)
    {
        // No logging code needed - automatically instrumented
        return new PaymentResult { Success = true };
    }
}
```

**Log4Net Output:**
```
2025-09-11 01:33:03,648 [5] INFO  Console - Method FlexKitLoggingConsoleApp.IPaymentService.ProcessPayment executed with success: True
```

### 3. Intelligent Target Routing

FlexKit.Log4Net creates target-specific loggers for precise message routing:

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
            "Pattern": "%date [%thread] %-5level %logger - %message%newline"
          }
        },
        "File": {
          "Type": "File",
          "Enabled": true,
          "Properties": {
            "File": "logs/application.log",
            "Pattern": "%date [%thread] %-5level %logger - %message%newline"
          }
        }
      }
    }
  }
}
```

### 4. Advanced Formatter Support

FlexKit formatters work seamlessly with Log4Net's string-based logging:

**JSON Formatter Output:**
```
2025-09-11 01:33:03,672 [5] INFO  Console - {"id":"90907fbd-40c9-4a27-8b69-b4d1a22fc4e0","method_name":"ProcessComplexWorkflowAsync","type_name":"FlexKitLoggingConsoleApp.ComplexService","activity_id":"00-8178b6860ac31f6221658110a783c546-ccb6ab694928f9a8-00","thread_id":1,"input_parameters":{"request_id":"WF-001","workflow_type":"Payment","amount":1500.75},"output_value":{"success":true,"processed_at":"2025-09-10T22:33:03.867319Z"},"timestamp":"2025-09-10T22:33:03.5640726+00:00","duration":303.29,"duration_seconds":0.303}
```

**CustomTemplate Formatter Output:**
```
2025-09-11 01:33:03,672 [5] INFO  Console - 🔧 WORKFLOW: ValidateRequest step completed → null
```

**Hybrid Formatter Output:**
```
2025-09-11 01:33:03,892 [8] INFO  Console - Method ProcessWithHybridFormatter completed | META: {"MethodName":"ProcessWithHybridFormatter","TypeName":"FlexKitLoggingConsoleApp.IFormattingService","Success":true,"ThreadId":5,"Timestamp":"2025-09-10T22:33:03.8783589+00:00","Id":"9f899d4d-1861-40f3-b1bb-855743c806e9","ActivityId":null,"Duration":0.21,"DurationSeconds":0,"InputParameters":[{"name":"data","type":"String","value":"hybrid data"}],"OutputValue":"Hybrid processed: hybrid data"}
```

### 5. Exception Handling

Comprehensive exception logging with full stack traces:

```
2025-09-11 01:33:03,895 [8] ERROR Console - Method FlexKitLoggingConsoleApp.IErrorService.ProcessWithException executed with success: False
```

For more detailed exception information, use JSON formatter:
```json
{
  "method_name": "ProcessWithException",
  "exception_type": "ArgumentException",
  "exception_message": "Invalid data provided",
  "stack_trace": "   at FlexKitLoggingConsoleApp.ErrorService.ProcessWithException(String data)...",
  "success": false
}
```

## ⚙️ Configuration Examples

### Complete Log4Net Configuration

```json
{
  "FlexKit": {
    "Logging": {
      "AutoIntercept": true,
      "DefaultFormatter": "CustomTemplate",
      "DefaultTarget": "Console",
      "MaxBatchSize": 1,
      "BatchTimeout": "00:00:01",
      
      "Targets": {
        "Console": {
          "Type": "Console",
          "Enabled": true,
          "Properties": {
            "Pattern": "%date [%thread] %-5level %logger - %message%newline"
          }
        },
        "File": {
          "Type": "RollingFile",
          "Enabled": true,
          "Properties": {
            "File": "logs/application.log",
            "RollingStyle": "Size",
            "MaxSizeRollBackups": 10,
            "MaximumFileSize": "10MB",
            "Pattern": "%date [%thread] %-5level %logger{1} - %message%newline"
          }
        },
        "Debug": {
          "Type": "Debug",
          "Enabled": true,
          "Properties": {
            "LogLevel": "Debug",
            "Pattern": "%logger{1}: %message%newline"
          }
        }
      },

      "Services": {
        "MyApp.PaymentService": {
          "LogBoth": true,
          "Level": "Information",
          "Target": "File",
          "Formatter": "Json"
        },
        "MyApp.HighFrequencyService": {
          "ExcludeMethodPatterns": ["Get*", "*Cache*"],
          "Target": "Debug"
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
            "PaymentService": "💰 PAYMENT: {MethodName} completed in {Duration}ms → {OutputValue}"
          }
        }
      }
    }
  }
}
```

### Log4Net Appender Auto-Detection

FlexKit.Log4Net automatically detects available appenders by scanning assemblies:

```csharp
// Detected appenders include:
// - log4net.Appender.ConsoleAppender
// - log4net.Appender.FileAppender
// - log4net.Appender.RollingFileAppender
// - log4net.Appender.DebugAppender
// - log4net.Appender.EventLogAppender
// - Custom appenders from referenced assemblies
```

### Advanced Configuration Patterns

**Conditional Logging:**
```json
{
  "FlexKit": {
    "Logging": {
      "Services": {
        "MyApp.*": {
          "LogInput": true,
          "Level": "Information",
          "ExcludeMethodPatterns": ["ToString", "GetHashCode", "Equals"]
        },
        "MyApp.PerformanceCritical.*": {
          "LogInput": false,
          "Level": "Warning"
        }
      }
    }
  }
}
```

**Multi-Target Routing:**
```json
{
  "FlexKit": {
    "Logging": {
      "Services": {
        "MyApp.SecurityService": {
          "Target": "SecurityFile",
          "Level": "Debug"
        },
        "MyApp.PaymentService": {
          "Target": "AuditFile", 
          "Level": "Information"
        }
      },
      "Targets": {
        "SecurityFile": {
          "Type": "RollingFile",
          "Properties": {
            "File": "logs/security.log",
            "Pattern": "%date [SECURITY] %message%newline"
          }
        },
        "AuditFile": {
          "Type": "RollingFile", 
          "Properties": {
            "File": "logs/audit.log",
            "Pattern": "%date [AUDIT] %message%newline"
          }
        }
      }
    }
  }
}
```

## 🔗 Integration with Existing Log4Net

### Using Existing log4net.config

FlexKit.Log4Net can work alongside traditional log4net.config files:

```xml
<!-- log4net.config -->
<log4net>
  <appender name="ConsoleAppender" type="log4net.Appender.ConsoleAppender">
    <layout type="log4net.Layout.PatternLayout">
      <conversionPattern value="%date [%thread] %-5level %logger - %message%newline" />
    </layout>
  </appender>
  
  <root>
    <level value="INFO" />
    <appender-ref ref="ConsoleAppender" />
  </root>
</log4net>
```

FlexKit will detect and use these appenders automatically.

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

**Log4Net Output:**
```
2025-09-11 01:33:03,873 [8] INFO  Console - {"id":"90907fbd-40c9-4a27-8b69-b4d1a22fc4e0","method_name":"ProcessComplexWorkflowAsync","activity_id":"00-8178b6860ac31f6221658110a783c546-ccb6ab694928f9a8-00","input_parameters":{"request_id":"WF-001","workflow_type":"Payment"},"output_value":{"success":true,"processed_at":"2025-09-10T22:33:03.867319Z"},"duration":303.29}
```

## ⚡ Performance Optimization

### Performance Best Practices

For detailed performance analysis, see: [FlexKit.Logging.Log4Net Performance Tests](../../benchmarks/FlexKit.Logging.Log4Net.PerformanceTests/README.md)

**Key Performance Highlights:**

1. **Runtime Performance**: 81x faster than native Log4Net
2. **Memory Efficiency**: Superior allocation patterns with zero-allocation log entry creation
3. **Async Compatibility**: No performance degradation with async methods
4. **Queue Performance**: Sub-45ns latency for background processing

### Optimization Strategies

**High-Performance Applications:**
```csharp
[NoLog] // 45% faster, 31% less memory
public string GenerateCacheKey(int id) => $"cache:key:{id}";

[LogInput] // Equivalent baseline performance
public void ProcessCriticalData(string data) { ... }

[LogBoth] // 20% faster than baseline
public PaymentResult ProcessPayment(PaymentRequest request) { ... }
```

**Production Configuration:**
```json
{
  "FlexKit": {
    "Logging": {
      "MaxBatchSize": 100,
      "BatchTimeout": "00:00:00.500",
      "Services": {
        "MyApp.HighFrequency.*": {
          "ExcludeMethodPatterns": ["Get*", "*Cache*", "ToString"]
        }
      }
    }
  }
}
```

### Memory Optimization

**Zero-Allocation Scenarios:**
- Log entry creation: 89.05 ns, 0 B allocated
- Complex data handling: 93.80 ns, 0 B allocated
- Background queue operations: 40.27 ns, 1 B allocated

**Sustained Load Performance:**
- 1K iterations: 851 μs (60% faster than native Log4Net)
- 10K iterations: 8,322 μs (60% faster than native Log4Net)
- Excellent memory efficiency across all scenarios

## 🏗️ Architecture

### Component Overview

```
FlexKit.Logging.Log4Net Architecture:

┌─────────────────────────────────────┐
│          Application Layer          │
├─────────────────────────────────────┤
│      FlexKit.Logging.Core          │  ← Formatters, Interception, Config
├─────────────────────────────────────┤
│    FlexKit.Logging.Log4Net         │  ← Bridge Components
│  ┌─────────────────────────────┐    │
│  │   Log4NetLogger            │    │  ← MEL → Log4Net Bridge
│  │   Log4NetLoggerProvider    │    │
│  │   Log4NetLogWriter         │    │  ← FlexKit → Log4Net Writer
│  │   Log4NetConfigBuilder     │    │  ← Auto-Configuration
│  │   Log4NetAppenderDetector  │    │  ← Dynamic Detection
│  └─────────────────────────────┘    │
├─────────────────────────────────────┤
│           Log4Net Core              │  ← Appenders, Layouts, Loggers
└─────────────────────────────────────┘
```

### Key Components

**Log4NetLogger**: Bridges Microsoft.Extensions.Logging to Log4Net, routing framework logs to Log4Net appenders with proper level conversion.

**Log4NetLogWriter**: Processes FlexKit LogEntry objects and writes them to Log4Net using the configured message translator and target routing.

**Log4NetConfigurationBuilder**: Dynamically detects available Log4Net appenders and configures them based on FlexKit configuration, creating target-specific loggers.

**Log4NetAppenderDetector**: Scans loaded assemblies for IAppender implementations and extracts configurable properties for automatic setup.

## 🔧 Advanced Usage

### Custom Appender Integration

FlexKit.Log4Net automatically detects custom appenders:

```csharp
public class DatabaseAppender : AppenderSkeleton
{
    public string ConnectionString { get; set; }
    public string TableName { get; set; }
    
    protected override void Append(LoggingEvent loggingEvent)
    {
        // Custom database logging implementation
    }
}
```

Configure via FlexKit:
```json
{
  "Targets": {
    "Database": {
      "Type": "Database",
      "Properties": {
        "ConnectionString": "Server=localhost;Database=Logs;",
        "TableName": "ApplicationLogs"
      }
    }
  }
}
```

### Distributed Tracing Integration

FlexKit.Log4Net supports distributed tracing with Activity correlation:

```csharp
public async Task<ProcessingResult> ProcessWorkflowAsync(WorkflowRequest request)
{
    using var activity = Activity.StartActivity("ProcessWorkflow");
    
    // FlexKit automatically captures Activity.Current.Id
    var result = await ProcessInternalAsync(request);
    return result;
}
```

**Output:**
```json
{
  "method_name": "ProcessWorkflowAsync",
  "activity_id": "00-8178b6860ac31f6221658110a783c546-ccb6ab694928f9a8-00",
  "trace_id": "8178b6860ac31f6221658110a783c546",
  "span_id": "ccb6ab694928f9a8"
}
```

### Integration with ASP.NET Core

FlexKit.Log4Net automatically bridges all ASP.NET Core framework logs:

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // All ASP.NET Core logs automatically routed to Log4Net
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
2025-09-11 01:33:03,620 [5] INFO  Microsoft.AspNetCore.Hosting.Diagnostics - Request starting HTTP/1.1 GET http://localhost:5000/api/orders
2025-09-11 01:33:03,625 [5] INFO  Microsoft.AspNetCore.Routing.EndpointMiddleware - Executing endpoint 'OrderController.GetOrders'
```

## 📊 Monitoring and Observability

### Production Metrics

Track these key performance indicators:

**Performance Metrics:**
- Average method interception time: Target <2μs
- Queue processing latency: Target <50ns per operation  
- Cache hit ratio: Target >95% for frequently called methods
- Memory allocation rate: Monitor sustained allocation patterns

**Operational Metrics:**
- Background processing throughput
- Log4Net appender health status
- Configuration cache effectiveness
- Exception handling performance

### Health Checks

```csharp
public class LoggingHealthCheck : IHealthCheck
{
    private readonly ILoggerRepository _repository;
    
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        var isHealthy = _repository.Configured && 
                       _repository.GetCurrentLoggers().Length > 0;
                       
        return Task.FromResult(isHealthy 
            ? HealthCheckResult.Healthy("Log4Net is configured and operational")
            : HealthCheckResult.Unhealthy("Log4Net configuration issue detected"));
    }
}
```

## 🚀 Migration from Native Log4Net

### Migration Strategy

**Step 1: Install FlexKit.Logging.Log4Net**
```bash
dotnet add package FlexKit.Logging.Log4Net
```

**Step 2: Update Startup Configuration**
```csharp
// Before: Native Log4Net
var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));

// After: FlexKit.Log4Net
var host = Host.CreateDefaultBuilder(args)
    .AddFlexConfig()  // Automatically configures Log4Net
    .Build();
```

**Step 3: Remove Manual Logging Code**
```csharp
// Before: Manual Log4Net calls
public class PaymentService
{
    private static readonly ILog Logger = LogManager.GetLogger(typeof(PaymentService));
    
    public PaymentResult ProcessPayment(PaymentRequest request)
    {
        Logger.InfoFormat("Processing payment: {0}", request.Amount);
        try
        {
            var result = ProcessInternal(request);
            Logger.InfoFormat("Payment processed successfully: {0}", result.TransactionId);
            return result;
        }
        catch (Exception ex)
        {
            Logger.Error("Payment processing failed", ex);
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

**Step 4: Configure FlexKit (Optional)**
```json
{
  "FlexKit": {
    "Logging": {
      "DefaultFormatter": "CustomTemplate",
      "Services": {
        "MyApp.PaymentService": {
          "LogBoth": true,
          "Formatter": "Json"
        }
      }
    }
  }
}
```

### Performance Comparison

**Before Migration (Native Log4Net):**
- Manual logging calls: ~114.6μs per operation
- Manual instrumentation required
- Error-prone exception handling
- Inconsistent logging patterns

**After Migration (FlexKit.Log4Net):**  
- Automatic instrumentation: ~1.41μs per operation (81x faster)
- Zero manual logging code
- Comprehensive exception handling
- Consistent, configurable logging patterns

## 🔍 Troubleshooting

### Common Issues

**Issue**: Log4Net appenders not detected
```csharp
// Solution: Ensure Log4Net assemblies are loaded
var logRepository = LogManager.GetRepository(); // Forces assembly loading
```

**Issue**: Configuration not applied
```csharp
// Solution: Verify FlexKit configuration structure
{
  "FlexKit": { // Must be "FlexKit", not "Logging"
    "Logging": {
      "Targets": {
        // Configuration here
      }
    }
  }
}
```

**Issue**: Performance degradation
```csharp
// Solution: Use NoLog for high-frequency methods
[NoLog]
public string GetCacheKey(int id) => $"cache:{id}";
```

### Debug Information

Enable debug output for troubleshooting:

```csharp
// Add to Program.cs for startup diagnostics
Debug.WriteLine("FlexKit.Log4Net: Starting configuration");
```

Check detected appenders:
```csharp
var detector = new Log4NetAppenderDetector();
var appenders = detector.DetectAvailableAppenders();
foreach (var appender in appenders)
{
    Console.WriteLine($"Detected: {appender.Key} - {appender.Value.AppenderType}");
}
```

## 📚 Related Documentation

For comprehensive FlexKit.Logging documentation, see:

- **[Core Documentation](../FlexKit.Logging/README.md)**: Complete FlexKit.Logging features and configuration
- **[Configuration Guide](../FlexKit.Logging/README.md#configuration-based-logging)**: Service-level configuration patterns  
- **[Formatting System](../FlexKit.Logging/README.md#formatting--templates)**: Available formatters and templates
- **[Data Masking](../FlexKit.Logging/README.md#data-masking-system)**: PII protection and sensitive data handling
- **[Performance Benchmarks](../../benchmarks/FlexKit.Logging.Log4Net.PerformanceTests/README.md)**: Detailed performance analysis and optimization strategies
- **[Targeting System](../FlexKit.Logging/README.md#targeting-system)**: Multi-destination logging configuration

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](../../LICENSE) file for details.

---

**FlexKit.Logging.Log4Net** - Transform your Log4Net applications with zero-configuration auto-instrumentation and exceptional performance. Perfect for modernizing existing Log4Net implementations while maintaining all existing appender configurations and patterns.