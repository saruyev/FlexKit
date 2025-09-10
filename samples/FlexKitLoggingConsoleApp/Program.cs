using FlexKit.Configuration.Core;
using FlexKit.Logging.Configuration;
using FlexKit.Logging.Core;
using FlexKit.Logging.Interception.Attributes;
using FlexKit.Logging.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable MethodTooLong

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace FlexKitLoggingConsoleApp;

static class Program
{
    static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.InputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("=== FlexKit.Logging Console Application - All Use Cases ===\n");

        var host = Host.CreateDefaultBuilder(args)
            .AddFlexConfig()  // Zero configuration setup
            .Build();

        // Test all cases from the README
        await TestZeroConfigurationSetup(host);
        await TestAttributeBasedLogging(host);
        await TestConfigurationBasedLogging(host);
        await TestManualLogging(host);
        await TestDataMasking(host);
        await TestDifferentFormatters(host);
        await TestErrorHandling(host);
        await TestPerformanceOptimization(host);
        
        Console.WriteLine("\n=== All test cases completed successfully! ===");
    }

    static async Task TestZeroConfigurationSetup(IHost host)
    {
        Console.WriteLine("🚀 === ZERO CONFIGURATION SETUP ===");
        
        // Services with interfaces get automatic logging
        var orderService = host.Services.GetService<IOrderService>();
        orderService?.ProcessOrder("ORDER-001");
        _ = orderService?.GetOrderStatus("ORDER-001");
        
        Console.WriteLine("✅ Zero configuration setup completed\n");
    }

    static async Task TestAttributeBasedLogging(IHost host)
    {
        Console.WriteLine("📋 === ATTRIBUTE-BASED LOGGING ===");
        
        // Test different attribute combinations
        var paymentService = host.Services.GetService<IPaymentService>();
        paymentService?.ProcessPayment(new PaymentRequest { Amount = 100.50m, Currency = "USD" });
        
        var notificationService = host.Services.GetService<INotificationService>();
        notificationService?.SendEmail("user@example.com", "Test Subject", "Test Body");
        notificationService?.SendSms("123-456-7890", "Test SMS");
        notificationService?.ValidateEmailFormat("user@example.com"); // Won't be logged (no [LogInput] attribute and not virtual)
        
        Console.WriteLine("✅ Attribute-based logging completed\n");
    }

    static async Task TestConfigurationBasedLogging(IHost host)
    {
        Console.WriteLine("⚙️ === CONFIGURATION-BASED LOGGING ===");
        
        // These services are configured via appsettings.json patterns
        var userService = host.Services.GetService<IUserService>();
        _ = userService?.CreateUser("john_doe", "john@example.com", "123-456-7890");
        
        Console.WriteLine("✅ Configuration-based logging completed\n");
    }

    static async Task TestManualLogging(IHost host)
    {
        Console.WriteLine("🔧 === MANUAL LOGGING ===");
        
        var complexService = host.Services.GetService<IComplexService>()!;
        await complexService.ProcessComplexWorkflowAsync(new WorkflowRequest 
        { 
            Id = "WF-001", 
            Type = "Payment",
            Amount = 1500.75m 
        });
        
        Console.WriteLine("✅ Manual logging completed\n");
    }

    static async Task TestDataMasking(IHost host)
    {
        Console.WriteLine("🔒 === DATA MASKING ===");
        
        var authService = host.Services.GetService<IAuthenticationService>();
        authService?.ValidateUser("admin", "secret123", "corporate");
        
        var configService = host.Services.GetService<IConfigurationService>();
        configService?.LoadConfig(new SecretConfiguration 
        { 
            DatabasePassword = "super_secret_password",
            EncryptionKey = "encryption_key_123",
            ApiSecret = "api_secret_xyz"
        });
        
        Console.WriteLine("✅ Data masking completed\n");
    }

    static async Task TestDifferentFormatters(IHost host)
    {
        Console.WriteLine("🎨 === DIFFERENT FORMATTERS ===");
        
        var formattingService = host.Services.GetService<IFormattingService>();
        formattingService?.ProcessWithJsonFormatter("test data");
        formattingService?.ProcessWithCustomTemplate("template data");
        formattingService?.ProcessWithHybridFormatter("hybrid data");
        
        Console.WriteLine("✅ Different formatters completed\n");
    }

    static async Task TestErrorHandling(IHost host)
    {
        Console.WriteLine("⚠️ === ERROR HANDLING ===");
        
        var errorService = host.Services.GetService<IErrorService>();
        
        try
        {
            errorService?.ProcessWithSuccess("valid data");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error: {ex.Message}");
        }
        
        try
        {
            errorService?.ProcessWithException("invalid");
        }
        catch (Exception)
        {
            Console.WriteLine("Expected exception caught and logged");
        }
        
        Console.WriteLine("✅ Error handling completed\n");
    }

    static async Task TestPerformanceOptimization(IHost host)
    {
        Console.WriteLine("⚡ === PERFORMANCE OPTIMIZATION ===");
        
        var perfService = host.Services.GetService<IPerformanceService>();
        
        // Business-critical method - logged
        perfService?.ProcessBusinessCriticalData("important data");
        
        // High-frequency utility method - not logged for performance
        for (int i = 0; i < 1000; i++)
        {
            perfService?.GenerateCacheKey(i);
        }
        
        Console.WriteLine("✅ Performance optimization completed\n");
    }
}

// ============================================================================
// SERVICE INTERFACES AND IMPLEMENTATIONS
// ============================================================================

// Zero Configuration Services
public interface IOrderService
{
    void ProcessOrder(string orderId);
    string GetOrderStatus(string orderId);
}

public class OrderService : IOrderService
{
    public void ProcessOrder(string orderId)
    {
        Console.WriteLine($"[OrderService] Processing order: {orderId}");
        Thread.Sleep(50); // Simulate processing
    }

    public string GetOrderStatus(string orderId)
    {
        Console.WriteLine($"[OrderService] Getting status for order: {orderId}");
        return "Completed";
    }
}

// Attribute-Based Services
public interface IPaymentService
{
    PaymentResult ProcessPayment(PaymentRequest request);
}

[LogBoth] // Class-level attribute affects all methods
public class PaymentService : IPaymentService
{
    public PaymentResult ProcessPayment(PaymentRequest request)
    {
        Console.WriteLine($"[PaymentService] Processing payment: {request.Amount} {request.Currency}");
        return new PaymentResult { Success = true, TransactionId = $"TXN-{DateTime.UtcNow.Ticks}" };
    }
}

public interface INotificationService
{
    void SendEmail(string recipient, string subject, string body);
    void SendSms(string phoneNumber, string message);
    void ValidateEmailFormat(string email);
}

[LogInput] // Class-level logging configuration
public class NotificationService : INotificationService
{
    public virtual void SendEmail(string recipient, string subject, string body)
    {
        Console.WriteLine($"[NotificationService] Sending email to {recipient}: {subject}");
    }

    public virtual void SendSms(string phoneNumber, string message)
    {
        Console.WriteLine($"[NotificationService] Sending SMS to {phoneNumber}");
    }

    public void ValidateEmailFormat(string email) // Not virtual - won't be logged
    {
        Console.WriteLine($"[NotificationService] Validating email format: {email}");
    }
}

// Configuration-Based Services (configured via appsettings.json)
public interface IUserService
{
    UserResult CreateUser(string username, string email, string phoneNumber);
}

public class UserService : IUserService
{
    public UserResult CreateUser(string username, string email, string phoneNumber)
    {
        Console.WriteLine($"[UserService] Creating user: {username}");
        return new UserResult { Success = true, UserId = Guid.NewGuid().ToString() };
    }
}

// Manual Logging Service
public interface IComplexService
{
    Task<ProcessingResult> ProcessComplexWorkflowAsync(WorkflowRequest request);
}

public class ComplexService(IFlexKitLogger logger) : IComplexService
{
    public async Task<ProcessingResult> ProcessComplexWorkflowAsync(WorkflowRequest request)
    {
        using var activity = logger.StartActivity("ProcessComplexWorkflow");
        
        var startEntry = LogEntry.CreateStart(nameof(ProcessComplexWorkflowAsync), GetType().FullName!)
            .WithInput(new { RequestId = request.Id, WorkflowType = request.Type, request.Amount })
            .WithFormatter(FormatterType.Json)
            .WithTarget("Console");

        logger.Log(startEntry);

        try
        {
            // Step 1: Validation
            var validationEntry = LogEntry.CreateStart("ValidateRequest", GetType().FullName!)
                .WithInput(request.Id)
                .WithTarget("Console");
            logger.Log(validationEntry);

            await Task.Delay(100); // Simulate validation
            logger.Log(validationEntry.WithCompletion(success: true));

            // Step 2: Processing
            if (request.Amount > 10000)
            {
                var auditEntry = LogEntry.CreateStart("HighValueTransaction", GetType().FullName!, LogLevel.Warning)
                    .WithInput(new { request.Amount, request.Type })
                    .WithTarget("Console");
                logger.Log(auditEntry);
            }

            await Task.Delay(200); // Simulate processing
            var result = new ProcessingResult { Success = true, ProcessedAt = DateTime.UtcNow };

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

// Data Masking Services
public interface IAuthenticationService
{
    bool ValidateUser(string username, [Mask] string password, string domain);
}

public class AuthenticationService : IAuthenticationService
{
    [LogBoth]
    public bool ValidateUser(string username, string password, string domain)
    {
        Console.WriteLine($"[AuthenticationService] Validating user: {username} in domain: {domain}");
        return username == "admin" && password == "secret123";
    }
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
        Console.WriteLine("[ConfigurationService] Loading security configuration");
    }
}

// Formatter Testing Services
public interface IFormattingService
{
    string ProcessWithJsonFormatter(string data);
    string ProcessWithCustomTemplate(string data);
    string ProcessWithHybridFormatter(string data);
}

public class FormattingService : IFormattingService
{
    [LogBoth(formatter: "Json")]
    public string ProcessWithJsonFormatter(string data)
    {
        Console.WriteLine($"[FormattingService] Processing with JSON formatter: {data}");
        return $"JSON processed: {data}";
    }

    [LogBoth(formatter: "CustomTemplate")]
    public string ProcessWithCustomTemplate(string data)
    {
        Console.WriteLine($"[FormattingService] Processing with custom template: {data}");
        return $"Template processed: {data}";
    }

    [LogBoth(formatter: "Hybrid")]
    public string ProcessWithHybridFormatter(string data)
    {
        Console.WriteLine($"[FormattingService] Processing with hybrid formatter: {data}");
        return $"Hybrid processed: {data}";
    }
}

// Error Handling Service
public interface IErrorService
{
    string ProcessWithSuccess(string data);
    string ProcessWithException(string data);
}

public class ErrorService : IErrorService
{
    [LogBoth]
    public string ProcessWithSuccess(string data)
    {
        Console.WriteLine($"[ErrorService] Processing successfully: {data}");
        return $"Success: {data}";
    }

    [LogBoth]
    public string ProcessWithException(string data)
    {
        Console.WriteLine($"[ErrorService] Processing with potential error: {data}");
        if (data == "invalid")
        {
            throw new ArgumentException("Invalid data provided");
        }
        return $"Success: {data}";
    }
}

// Performance Optimization Service
public interface IPerformanceService
{
    void ProcessBusinessCriticalData(string data);
    string GenerateCacheKey(int id);
}

public class PerformanceService : IPerformanceService
{
    [LogBoth]
    public void ProcessBusinessCriticalData(string data)
    {
        Console.WriteLine($"[PerformanceService] Processing business critical data: {data}");
    }

    [NoLog] // High-frequency method - 94% performance improvement
    public string GenerateCacheKey(int id)
    {
        return $"cache:key:{id}";
    }
}

// ============================================================================
// DATA TRANSFER OBJECTS
// ============================================================================

public class PaymentRequest
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
}

public class PaymentResult
{
    public bool Success { get; set; }
    public string TransactionId { get; set; } = string.Empty;
}

public class UserResult
{
    public bool Success { get; set; }
    public string UserId { get; set; } = string.Empty;
}

public class WorkflowRequest
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class ProcessingResult
{
    public bool Success { get; set; }
    public DateTime ProcessedAt { get; set; }
}

[Mask(Replacement = "[CLASSIFIED_CONFIG]")]
public class SecretConfiguration
{
    public string DatabasePassword { get; set; } = string.Empty;
    public string EncryptionKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
}