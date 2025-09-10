using FlexKit.Configuration.Core;
using FlexKit.Logging.Core;
using FlexKit.Logging.Interception.Attributes;
using FlexKit.Logging.Models;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
// ReSharper disable HollowTypeName

namespace FlexKitLoggingAspNetApp;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // FlexKit.Logging integration with ASP.NET Core
        builder.AddFlexConfig();

        // Add services to the container
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        // Configure the HTTP request pipeline
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();

        // Demonstrate all FlexKit.Logging features
        Console.WriteLine("=== FlexKit.Logging ASP.NET Core Application Started ===");
        Console.WriteLine("Navigate to /swagger to test the API endpoints");
        Console.WriteLine("Each endpoint demonstrates different FlexKit.Logging features\n");

        app.Run();
    }
}

// ============================================================================
// CONTROLLERS - Each demonstrates different FlexKit.Logging features
// ============================================================================

[ApiController]
[Route("api/[controller]")]
public class OrdersController(IOrderService orderService) : ControllerBase
{
    [HttpPost]
    [LogBoth] // Automatic request/response logging
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        try
        {
            var result = await orderService.ProcessOrderAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{orderId}")]
    [LogOutput] // Only log the response
    public async Task<IActionResult> GetOrder(string orderId)
    {
        var order = await orderService.GetOrderAsync(orderId);
        if (order == null)
            return NotFound();

        return Ok(order);
    }

    [HttpGet("{orderId}/status")]
    [NoLog] // High frequency endpoint - no logging for performance
    public IActionResult GetOrderStatus(string orderId)
    {
        var status = orderService.GetOrderStatus(orderId);
        return Ok(new { orderId, status });
    }
}

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost("process")]
    [LogBoth(level: LogLevel.Warning)] // Critical operation - log at Warning level
    public async Task<IActionResult> ProcessPayment([FromBody] PaymentRequest request)
    {
        var result = await _paymentService.ProcessPaymentAsync(request);
        return Ok(result);
    }

    [HttpPost("validate")]
    [LogInput] // Only log request for validation
    public IActionResult ValidatePayment([FromBody] PaymentValidationRequest request)
    {
        var isValid = _paymentService.ValidatePayment(request);
        return Ok(new { isValid });
    }
}

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        // This service is configured via appsettings.json for logging
        var result = await _userService.CreateUserAsync(request);
        return Ok(result);
    }

    [HttpPost("{userId}/password")]
    public async Task<IActionResult> ChangePassword(int userId, [FromBody] ChangePasswordRequest request)
    {
        var result = await _userService.ChangePasswordAsync(userId, request);
        return Ok(result);
    }
}

[ApiController]
[Route("api/[controller]")]
public class WorkflowController : ControllerBase
{
    private readonly IComplexWorkflowService _workflowService;

    public WorkflowController(IComplexWorkflowService workflowService)
    {
        _workflowService = workflowService;
    }

    [HttpPost("execute")]
    public async Task<IActionResult> ExecuteWorkflow([FromBody] WorkflowExecutionRequest request)
    {
        // This service uses manual logging for complex scenarios
        var result = await _workflowService.ExecuteComplexWorkflowAsync(request);
        return Ok(result);
    }

    [HttpGet("{workflowId}/status")]
    public async Task<IActionResult> GetWorkflowStatus(string workflowId)
    {
        var status = await _workflowService.GetWorkflowStatusAsync(workflowId);
        return Ok(status);
    }
}

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authService;

    public AuthController(IAuthenticationService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // Demonstrates data masking for sensitive information
        var result = await _authService.AuthenticateAsync(request);
        return Ok(result);
    }

    [HttpPost("token/refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var result = await _authService.RefreshTokenAsync(request);
        return Ok(result);
    }
}

[ApiController]
[Route("api/[controller]")]
public class FormattingController(IFormattingDemoService formattingService) : ControllerBase
{
    [HttpPost("json")]
    public IActionResult DemoJsonFormatter([FromBody] SampleData data)
    {
        var result = formattingService.ProcessWithJsonFormatter(data);
        return Ok(result);
    }

    [HttpPost("template")]
    public IActionResult DemoCustomTemplate([FromBody] SampleData data)
    {
        var result = formattingService.ProcessWithCustomTemplate(data);
        return Ok(result);
    }

    [HttpPost("hybrid")]
    public IActionResult DemoHybridFormatter([FromBody] SampleData data)
    {
        var result = formattingService.ProcessWithHybridFormatter(data);
        return Ok(result);
    }
}

// ============================================================================
// SERVICE INTERFACES AND IMPLEMENTATIONS
// ============================================================================

public interface IOrderService
{
    Task<OrderResult> ProcessOrderAsync(CreateOrderRequest request);
    Task<Order?> GetOrderAsync(string orderId);
    string GetOrderStatus(string orderId);
}

[LogBoth] // Class-level attribute for comprehensive logging
public class OrderService : IOrderService
{
    public async Task<OrderResult> ProcessOrderAsync(CreateOrderRequest request)
    {
        Console.WriteLine($"[OrderService] Processing order for customer: {request.CustomerId}");

        // Simulate processing time
        await Task.Delay(Random.Shared.Next(100, 500));

        if (request.Items?.Count == 0)
            throw new ArgumentException("Order must contain at least one item");

        var orderId = $"ORD-{DateTime.UtcNow.Ticks}";
        return new OrderResult
        {
            OrderId = orderId,
            Success = true,
            TotalAmount = request.Items?.Sum(i => i.Price * i.Quantity) ?? 0,
            ProcessedAt = DateTime.UtcNow
        };
    }

    public async Task<Order?> GetOrderAsync(string orderId)
    {
        Console.WriteLine($"[OrderService] Retrieving order: {orderId}");

        // Simulate database lookup
        await Task.Delay(50);

        return new Order
        {
            OrderId = orderId,
            Status = "Completed",
            CreatedAt = DateTime.UtcNow.AddMinutes(-30),
            TotalAmount = 150.75m
        };
    }

    public string GetOrderStatus(string orderId)
    {
        // High frequency method - [NoLog] applied via controller method
        return "Completed";
    }
}

public interface IPaymentService
{
    Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request);
    bool ValidatePayment(PaymentValidationRequest request);
}

public class PaymentService : IPaymentService
{
    [LogBoth(level: LogLevel.Warning, formatter: "Json")]
    public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
    {
        Console.WriteLine($"[PaymentService] Processing payment: {request.Amount} {request.Currency}");

        // Simulate payment processing
        await Task.Delay(Random.Shared.Next(200, 800));

        if (request.Amount <= 0)
            throw new ArgumentException("Payment amount must be greater than zero");

        if (request.Amount > 10000)
        {
            // High value transaction - additional logging handled automatically
            Console.WriteLine($"[PaymentService] High value transaction detected: {request.Amount}");
        }

        return new PaymentResult
        {
            Success = true,
            TransactionId = $"TXN-{DateTime.UtcNow.Ticks}",
            ProcessedAmount = request.Amount,
            ProcessedAt = DateTime.UtcNow
        };
    }

    [LogInput(level: LogLevel.Information)]
    public bool ValidatePayment(PaymentValidationRequest request)
    {
        Console.WriteLine($"[PaymentService] Validating payment for card: {request.CardNumber?[^4..]}");

        // Simple validation logic
        return !string.IsNullOrEmpty(request.CardNumber) &&
               request.CardNumber.Length >= 16 &&
               !string.IsNullOrEmpty(request.CVV) &&
               request.CVV.Length == 3;
    }
}

public interface IUserService
{
    Task<UserResult> CreateUserAsync(CreateUserRequest request);
    Task<PasswordChangeResult> ChangePasswordAsync(int userId, ChangePasswordRequest request);
}

// This service is configured via appsettings.json - no attributes needed
public class UserService : IUserService
{
    public async Task<UserResult> CreateUserAsync(CreateUserRequest request)
    {
        Console.WriteLine($"[UserService] Creating user: {request.Username}");

        // Simulate user creation
        await Task.Delay(Random.Shared.Next(100, 300));

        if (string.IsNullOrEmpty(request.Username))
            throw new ArgumentException("Username is required");

        return new UserResult
        {
            Success = true,
            UserId = Guid.NewGuid().ToString(),
            Username = request.Username,
            CreatedAt = DateTime.UtcNow
        };
    }

    public async Task<PasswordChangeResult> ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        Console.WriteLine($"[UserService] Changing password for user: {userId}");

        // Simulate password change
        await Task.Delay(100);

        return new PasswordChangeResult
        {
            Success = true,
            ChangedAt = DateTime.UtcNow
        };
    }
}

public interface IComplexWorkflowService
{
    Task<WorkflowResult> ExecuteComplexWorkflowAsync(WorkflowExecutionRequest request);
    Task<WorkflowStatus> GetWorkflowStatusAsync(string workflowId);
}

// Manual logging service for complex scenarios
public class ComplexWorkflowService : IComplexWorkflowService
{
    private readonly IFlexKitLogger _logger;

    public ComplexWorkflowService(IFlexKitLogger logger)
    {
        _logger = logger;
    }

    public async Task<WorkflowResult> ExecuteComplexWorkflowAsync(WorkflowExecutionRequest request)
    {
        using var activity = _logger.StartActivity("ExecuteComplexWorkflow");

        var startEntry = LogEntry.CreateStart(nameof(ExecuteComplexWorkflowAsync), GetType().FullName!)
            .WithInput(new
            {
                WorkflowId = request.WorkflowId,
                WorkflowType = request.WorkflowType,
                Priority = request.Priority,
                EstimatedDuration = request.EstimatedDuration
            })
            .WithFormatter(FlexKit.Logging.Configuration.FormatterType.Json)
            .WithTarget("Console");

        _logger.Log(startEntry);

        try
        {
            // Step 1: Validation
            var validationEntry = LogEntry.CreateStart("ValidateWorkflowRequest", GetType().FullName!)
                .WithInput(new { WorkflowId = request.WorkflowId, Type = request.WorkflowType })
                .WithTarget("Console");
            _logger.Log(validationEntry);

            if (string.IsNullOrEmpty(request.WorkflowId))
                throw new ArgumentException("WorkflowId is required");

            await Task.Delay(50);
            _logger.Log(validationEntry.WithCompletion(success: true));

            // Step 2: Resource Allocation
            var resourceEntry = LogEntry.CreateStart("AllocateResources", GetType().FullName!)
                .WithInput(new { Priority = request.Priority })
                .WithTarget("Console");
            _logger.Log(resourceEntry);

            await Task.Delay(100);
            _logger.Log(resourceEntry.WithCompletion(success: true)
                .WithOutput(new { AllocatedCores = 4, AllocatedMemory = "2GB" }));

            // Step 3: Execution
            var executionEntry = LogEntry.CreateStart("ExecuteWorkflowSteps", GetType().FullName!)
                .WithInput(new { Steps = request.Steps?.Count ?? 0 })
                .WithTarget("Console");
            _logger.Log(executionEntry);

            // Simulate workflow execution
            await Task.Delay(Random.Shared.Next(500, 1500));

            // High priority workflows get additional logging
            if (request.Priority == "High")
            {
                var priorityEntry = LogEntry.CreateStart("HighPriorityProcessing", GetType().FullName!, LogLevel.Warning)
                    .WithInput(new { WorkflowId = request.WorkflowId })
                    .WithTarget("Console");
                _logger.Log(priorityEntry.WithCompletion(success: true));
            }

            var result = new WorkflowResult
            {
                WorkflowId = request.WorkflowId,
                Success = true,
                ExecutedSteps = request.Steps?.Count ?? 0,
                ExecutionTime = TimeSpan.FromMilliseconds(Random.Shared.Next(500, 1500)),
                CompletedAt = DateTime.UtcNow
            };

            _logger.Log(executionEntry.WithCompletion(success: true)
                .WithOutput(new { ExecutedSteps = result.ExecutedSteps, ExecutionTime = result.ExecutionTime }));

            var completionEntry = startEntry
                .WithCompletion(success: true)
                .WithOutput(new
                {
                    Success = result.Success,
                    ExecutedSteps = result.ExecutedSteps,
                    TotalExecutionTime = result.ExecutionTime
                });

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

    public async Task<WorkflowStatus> GetWorkflowStatusAsync(string workflowId)
    {
        var entry = LogEntry.CreateStart(nameof(GetWorkflowStatusAsync), GetType().FullName!)
            .WithInput(workflowId)
            .WithTarget("Console");
        _logger.Log(entry);

        try
        {
            // Simulate status lookup
            await Task.Delay(25);

            var status = new WorkflowStatus
            {
                WorkflowId = workflowId,
                Status = "Completed",
                Progress = 100,
                LastUpdated = DateTime.UtcNow.AddMinutes(-5)
            };

            _logger.Log(entry.WithCompletion(success: true).WithOutput(status));
            return status;
        }
        catch (Exception ex)
        {
            _logger.Log(entry.WithCompletion(success: false, exception: ex));
            throw;
        }
    }
}

public interface IAuthenticationService
{
    Task<AuthenticationResult> AuthenticateAsync(LoginRequest request);
    Task<TokenRefreshResult> RefreshTokenAsync(RefreshTokenRequest request);
}

public class AuthenticationService : IAuthenticationService
{
    [LogBoth]
    public async Task<AuthenticationResult> AuthenticateAsync(LoginRequest request)
    {
        Console.WriteLine($"[AuthenticationService] Authenticating user: {request.Username}");

        // Simulate authentication
        await Task.Delay(Random.Shared.Next(100, 300));

        if (request.Username == "admin" && request.Password == "secret123")
        {
            return new AuthenticationResult
            {
                Success = true,
                Token = $"jwt_token_{DateTime.UtcNow.Ticks}",
                RefreshToken = $"refresh_{DateTime.UtcNow.Ticks}",
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            };
        }

        return new AuthenticationResult { Success = false, ErrorMessage = "Invalid credentials" };
    }

    [LogInput]
    public async Task<TokenRefreshResult> RefreshTokenAsync(RefreshTokenRequest request)
    {
        Console.WriteLine("[AuthenticationService] Refreshing authentication token");

        // Simulate token refresh
        await Task.Delay(50);

        return new TokenRefreshResult
        {
            Success = true,
            NewToken = $"jwt_token_{DateTime.UtcNow.Ticks}",
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };
    }
}

public interface IFormattingDemoService
{
    string ProcessWithJsonFormatter(SampleData data);
    string ProcessWithCustomTemplate(SampleData data);
    string ProcessWithHybridFormatter(SampleData data);
}

public class FormattingDemoService : IFormattingDemoService
{
    [LogBoth(formatter: "Json")]
    public string ProcessWithJsonFormatter(SampleData data)
    {
        Console.WriteLine($"[FormattingDemoService] Processing with JSON formatter: {data.Name}");
        return $"JSON processed: {data.Name} (Value: {data.Value})";
    }

    [LogBoth(formatter: "CustomTemplate")]
    public string ProcessWithCustomTemplate(SampleData data)
    {
        Console.WriteLine($"[FormattingDemoService] Processing with custom template: {data.Name}");
        return $"Template processed: {data.Name} (Value: {data.Value})";
    }

    [LogBoth(formatter: "Hybrid")]
    public string ProcessWithHybridFormatter(SampleData data)
    {
        Console.WriteLine($"[FormattingDemoService] Processing with hybrid formatter: {data.Name}");
        return $"Hybrid processed: {data.Name} (Value: {data.Value})";
    }
}

// ============================================================================
// DATA TRANSFER OBJECTS AND MODELS
// ============================================================================

public class CreateOrderRequest
{
    [Required]
    public string CustomerId { get; set; } = string.Empty;

    public List<OrderItem>? Items { get; set; }

    public string? Notes { get; set; }
}

public class OrderItem
{
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

public class OrderResult
{
    public string OrderId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime ProcessedAt { get; set; }
}

public class Order
{
    public string OrderId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public decimal TotalAmount { get; set; }
}

public class PaymentRequest
{
    [Required]
    public decimal Amount { get; set; }

    [Required]
    public string Currency { get; set; } = "USD";

    [Mask] // Sensitive data masking
    public string CardNumber { get; set; } = string.Empty;

    [Mask]
    public string CVV { get; set; } = string.Empty;

    public string ExpiryDate { get; set; } = string.Empty;
}

public class PaymentValidationRequest
{
    [Mask] // Mask in logs
    public string CardNumber { get; set; } = string.Empty;

    [Mask]
    public string CVV { get; set; } = string.Empty;

    public string ExpiryDate { get; set; } = string.Empty;
}

public class PaymentResult
{
    public bool Success { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public decimal ProcessedAmount { get; set; }
    public DateTime ProcessedAt { get; set; }
    public string? ErrorMessage { get; set; }
}

public class CreateUserRequest
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Email { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    [Mask] // Password masking
    public string Password { get; set; } = string.Empty;
}

public class ChangePasswordRequest
{
    [Mask]
    public string CurrentPassword { get; set; } = string.Empty;

    [Mask]
    public string NewPassword { get; set; } = string.Empty;
}

public class UserResult
{
    public bool Success { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class PasswordChangeResult
{
    public bool Success { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? ErrorMessage { get; set; }
}

public class WorkflowExecutionRequest
{
    [Required]
    public string WorkflowId { get; set; } = string.Empty;

    [Required]
    public string WorkflowType { get; set; } = string.Empty;

    public string Priority { get; set; } = "Normal";

    public TimeSpan EstimatedDuration { get; set; }

    public List<WorkflowStep>? Steps { get; set; }
}

public class WorkflowStep
{
    public string StepId { get; set; } = string.Empty;
    public string StepType { get; set; } = string.Empty;
    public Dictionary<string, object>? Parameters { get; set; }
}

public class WorkflowResult
{
    public string WorkflowId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public int ExecutedSteps { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    public DateTime CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
}

public class WorkflowStatus
{
    public string WorkflowId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Progress { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class LoginRequest
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    [Mask] // Mask password in logs
    public string Password { get; set; } = string.Empty;
}

public class RefreshTokenRequest
{
    [Required]
    [Mask] // Mask refresh token
    public string RefreshToken { get; set; } = string.Empty;
}

public class AuthenticationResult
{
    public bool Success { get; set; }

    [Mask] // Mask token in output logs
    public string Token { get; set; } = string.Empty;

    [Mask] // Mask refresh token
    public string RefreshToken { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
    public string? ErrorMessage { get; set; }
}

public class TokenRefreshResult
{
    public bool Success { get; set; }

    [Mask] // Mask new token
    public string NewToken { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
    public string? ErrorMessage { get; set; }
}

public class SampleData
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}