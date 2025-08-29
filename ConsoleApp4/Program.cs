using System.Diagnostics;
using FlexKit.Configuration.Core;
using FlexKit.Logging.Core;
using FlexKit.Logging.Interception.Attributes;
using FlexKit.Logging.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
// ReSharper disable NullableWarningSuppressionIsUsed
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable ArrangeTypeMemberModifiers
// ReSharper disable MethodTooLong
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedVariable
// ReSharper disable SpecifyACultureInStringConversionExplicitly
// ReSharper disable BaseObjectEqualsIsObjectEquals
// ReSharper disable ConvertToPrimaryConstructor
// ReSharper disable RedundantOverriddenMember
// ReSharper disable BaseObjectGetHashCodeCallInGetHashCode

namespace LoggingTestConsole
{

    class Program
    {
        static async Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .AddFlexConfig()
                .Build();

            Console.WriteLine("=== FLEXKIT LOGGING THREE-TIER INTERCEPTION TEST ===\n");
            
            TestAttributeBasedInterception(host);
            TestConfigurationBasedInterception(host);
            TestAutoInterception(host);
            TestPrecedenceRules(host);
            
            TestServices(host);
            var s = host.Services.GetService<TestService>();
            _ = s?.GetCustomer(123);
            s?.ProcessPayment(new PaymentRequest(100, "USD", 123));
            
            var a = host.Services.GetService<IMixedAttributesService>()!;
            _ = await a.ProcessAsyncData("test");
            TestExclusionPatterns(host);
            
            await TestManualLogging(host);
            Console.WriteLine("\n=== ALL TESTS COMPLETED ===");
        }

        static async Task TestManualLogging(IHost host)
        {
            Console.WriteLine("🔧 === MANUAL LOGGING TESTS ===");

            var manualOrderService = host.Services.GetService<ManualLogging.IManualOrderService>()!;
            var order = new ManualLogging.Order("ORD-001", 100.50m, "Test Product");
    
            Console.WriteLine("--- Testing Manual Order Processing (should NOT be auto-intercepted) ---");
            var result = manualOrderService.ProcessOrderAsync(order).Result;
            Console.WriteLine($"Order result: {result}");
    
            Console.WriteLine("--- Testing Manual Calculation with Custom Template ---");
            var total = manualOrderService.CalculateTotal(order);
            Console.WriteLine($"Total: ${total}");
    
            Console.WriteLine("--- Testing Manual Validation with Activity ---");
            try
            {
                manualOrderService.ValidateOrder(order);
                Console.WriteLine("Validation passed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Validation failed: {ex.Message}");
            }
            
            Console.WriteLine("--- Testing Default Template Usage ---");
            var defaultTemplateService = host.Services.GetService<ManualLogging.IDefaultTemplateService>()!;
    
            var defaultResult = defaultTemplateService.ProcessWithDefaultTemplate("test input");
            Console.WriteLine($"Default template result: {defaultResult}");
    
            defaultTemplateService.ProcessVoidWithDefaultTemplate("void test");
            
            // Test exception handling
            Console.WriteLine("--- Testing Exception Handling ---");
            var exceptionService = host.Services.GetService<ManualLogging.IExceptionTestService>()!;

// Test a successful case
            var successResult = exceptionService.ProcessWithException("success");
            Console.WriteLine($"Success result: {successResult}");

// Test exception case
            try
            {
                exceptionService.ProcessWithException("throw");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Caught expected exception: {ex.Message}");
            }

// Test async exception
            try
            {
                var asyncResult = await exceptionService.ProcessAsyncWithException("async-success");
                Console.WriteLine($"Async success: {asyncResult}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Async error: {ex.Message}");
            }

            try
            {
                await exceptionService.ProcessAsyncWithException("async-throw");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Caught async exception: {ex.Message}");
            }

            Console.WriteLine("✅ Manual logging tests completed\n");
        }
        
        static void TestAttributeBasedInterception(IHost host)
        {
            Console.WriteLine("🏷️  === TIER 1: ATTRIBUTE-BASED INTERCEPTION ===");

            var regularService = host.Services.GetService<IRegularService>()!;
            _ = regularService.DoWork("test input");
            _ = regularService.Calculate(10, 5);

            var noLogService = host.Services.GetService<INoLogService>()!;
            _ = noLogService.SecretOperation("sensitive data");

            var logInputService = host.Services.GetService<ILogInputService>()!;
            _ = logInputService.ProcessData("input data");

            var logOutputService = host.Services.GetService<ILogOutputService>()!;
            _ = logOutputService.GenerateData(42);

            var logBothService = host.Services.GetService<ILogBothService>()!;
            _ = logBothService.Transform("transform me", 3);

            var mixedService = host.Services.GetService<IMixedAttributesService>()!;
            _ = mixedService.LogInputMethod("input only");
            _ = mixedService.LogOutputMethod("output only");
            _ = mixedService.LogBothMethod("both input and output");

            var targetedService = host.Services.GetService<ITargetedLoggingService>()!;
            _ = targetedService.LogToConsoleSimple("console simple target");
            _ = targetedService.LogToConsoleJson("console json target");
            _ = targetedService.LogToDefaultTarget("default target");
            _ = targetedService.LogWithBothTargets("both targets input", 123);

            // Test exception handling
            try
            {
                mixedService.ThrowingMethod("will cause exception");
            }
            catch (Exception)
            {
                // Expected exception - logging should have occurred
            }

            try
            {
                targetedService.ThrowingMethodWithTarget("will cause exception with target");
            }
            catch (Exception)
            {
                // Expected exception - logging should have occurred
            }

            Console.WriteLine("✅ Attribute-based interception tests completed\n");
        }

        static void TestConfigurationBasedInterception(IHost host)
        {
            Console.WriteLine("⚙️  === TIER 2: CONFIGURATION-BASED INTERCEPTION ===");

            // Test wildcard pattern matching: "LoggingTestConsole.ConfigBased.*"
            var wildcardService = host.Services.GetService<ConfigBased.IWildcardService>()!;
            _ = wildcardService.ProcessWildcardData("wildcard input");
            _ = wildcardService.CalculateWildcard(100, 25);

            // Test the exact match overriding wildcard: "LoggingTestConsole.ConfigBased.PaymentService"
            var configPaymentService = host.Services.GetService<ConfigBased.IPaymentService>()!;
            _ = configPaymentService.ProcessPayment("card123", 250.75m);
            _ = configPaymentService.ValidatePayment("validation data");

            // Test different configuration: "LoggingTestConsole.ConfigBased.AdminService"
            var adminService = host.Services.GetService<ConfigBased.IAdminService>()!;
            _ = adminService.CreateUser("john_doe", "admin");
            _ = adminService.DeleteUser("old_user");

            // Test configuration with exceptions
            try
            {
                configPaymentService.ProcessFailingPayment("invalid_payment");
            }
            catch (Exception)
            {
                // Expected exception - should log at the configured exception level
            }

            Console.WriteLine("✅ Configuration-based interception tests completed\n");
        }

        static void TestAutoInterception(IHost host)
        {
            Console.WriteLine("🤖 === TIER 3: AUTO-INTERCEPTION ===");

            // Test auto-interception for classes without attributes or configuration
            var autoService = host.Services.GetService<AutoIntercept.IAutoService>()!;
            _ = autoService.AutoMethod1("auto input 1");
            _ = autoService.AutoMethod2(42, "auto input 2");

            var autoBusinessService = host.Services.GetService<AutoIntercept.IBusinessService>()!;
            _ = autoBusinessService.ProcessOrder("order123", 150.50m);
            _ = autoBusinessService.GetOrderStatus("order123");

            // Test auto-interception with a disabled class (should not log)
            var disabledAutoService = host.Services.GetService<AutoIntercept.IDisabledAutoService>()!;
            _ = disabledAutoService.ShouldNotBeLogged("this should not appear in logs");

            // Test auto-interception with method-level disabler
            var mixedAutoService = host.Services.GetService<AutoIntercept.IMixedAutoService>()!;
            _ = mixedAutoService.ShouldBeAutoLogged("this should be auto-logged");
            _ = mixedAutoService.ShouldNotBeLogged("this should NOT be logged");

            Console.WriteLine("✅ Auto-interception tests completed\n");
        }

        static void TestPrecedenceRules(IHost host)
        {
            Console.WriteLine("🏆 === PRECEDENCE RULES TESTING ===");

            // Test: Attribute overrides configuration
            var precedenceService = host.Services.GetService<Precedence.IPrecedenceService>()!;
            _ = precedenceService.AttributeOverridesConfig("attribute wins over config");
            _ = precedenceService.ConfigurationBased("config-based logging");
            _ = precedenceService.DisabledOverridesEverything("should not be logged despite config");

            // Test: Configuration overrides auto-intercept
            var configOverrideService = host.Services.GetService<Precedence.IConfigOverrideAutoService>()!;
            _ = configOverrideService.ConfigOverridesAuto("config beats auto");

            Console.WriteLine("✅ Precedence rules tests completed\n");
        }

        static void TestServices(IHost host)
        {
            Console.WriteLine("📊 === ORIGINAL TESTS ===");

            var regularService = host.Services.GetService<IRegularService>()!;
            _ = regularService.DoWork("test input");
            _ = regularService.Calculate(10, 5);

            var noLogService = host.Services.GetService<INoLogService>()!;
            _ = noLogService.SecretOperation("sensitive data");

            var logInputService = host.Services.GetService<ILogInputService>()!;
            _ = logInputService.ProcessData("input data");

            var logOutputService = host.Services.GetService<ILogOutputService>()!;
            _ = logOutputService.GenerateData(42);

            var logBothService = host.Services.GetService<ILogBothService>()!;
            _ = logBothService.Transform("transform me", 3);

            var mixedService = host.Services.GetService<IMixedAttributesService>()!;
            _ = mixedService.LogInputMethod("input only");
            _ = mixedService.LogOutputMethod("output only");
            _ = mixedService.LogBothMethod("both input and output");

            var targetedService = host.Services.GetService<ITargetedLoggingService>()!;
            _ = targetedService.LogToConsoleSimple("console simple target");
            _ = targetedService.LogToConsoleJson("console json target");
            _ = targetedService.LogToDefaultTarget("default target");
            _ = targetedService.LogWithBothTargets("both targets input", 123);

            try
            {
                mixedService.ThrowingMethod("will cause exception");
            }
            catch (Exception)
            {
                // Expected exception - logging should have occurred
            }

            try
            {
                targetedService.ThrowingMethodWithTarget("will cause exception with target");
            }
            catch (Exception)
            {
                // Expected exception - logging should have occurred
            }

            Console.WriteLine("✅ Original tests completed\n");
        }

        static void TestExclusionPatterns(IHost host)
        {
            Console.WriteLine("🚫 === EXCLUSION PATTERNS TESTING ===");

            // Test exact method exclusions
            Console.WriteLine("--- Testing Exact Method Exclusions ---");
            var exactService = host.Services.GetService<ExclusionPatterns.IExactExclusionService>()!;
            _ = exactService.ProcessData("should be logged");
            _ = exactService.ToString(); // Should NOT be logged
            _ = exactService.GetHashCode(); // Should NOT be logged
            _ = exactService.Equals(null); // Should NOT be logged
            _ = exactService.SomeOtherMethod("should be logged");

            // Test prefix pattern exclusions
            Console.WriteLine("--- Testing Prefix Pattern Exclusions (Get*, Set*, Validate*) ---");
            var prefixService = host.Services.GetService<ExclusionPatterns.IPrefixExclusionService>()!;
            _ = prefixService.ProcessOrder("should be logged");
            _ = prefixService.GetUserName(123); // Should NOT be logged (Get*)
            prefixService.SetUserStatus(123, "active"); // Should NOT be logged (Set*)
            _ = prefixService.ValidateInput("test"); // Should NOT be logged (Validate*)
            _ = prefixService.CalculateTotal(100m); // Should be logged
            _ = prefixService.GetOrderDetails("order1"); // Should NOT be logged (Get*)

            // Test suffix pattern exclusions
            Console.WriteLine("--- Testing Suffix Pattern Exclusions (*Internal, *Helper, *Cache) ---");
            var suffixService = host.Services.GetService<ExclusionPatterns.ISuffixExclusionService>()!;
            _ = suffixService.ProcessPublicData("should be logged");
            _ = suffixService.ProcessInternal("should NOT be logged"); // Should NOT be logged (*Internal)
            _ = suffixService.FormatHelper("test"); // Should NOT be logged (*Helper)
            _ = suffixService.ClearCache(); // Should NOT be logged (*Cache)
            _ = suffixService.ExecuteBusinessLogic("should be logged");
            suffixService.UpdateCache("key", "value"); // Should NOT be logged (*Cache)

            // Test contains pattern exclusions
            Console.WriteLine("--- Testing Contains Pattern Exclusions (*Temp*, *Debug*, *Test*) ---");
            var containsService = host.Services.GetService<ExclusionPatterns.IContainsExclusionService>()!;
            _ = containsService.ProcessProduction("should be logged");
            _ = containsService.CreateTempFile("content"); // Should NOT be logged (*Temp*)
            containsService.LogDebugInfo("debug msg"); // Should NOT be logged (*Debug*)
            _ = containsService.RunTestScenario("scenario"); // Should NOT be logged (*Test*)
            _ = containsService.GenerateReport("should be logged");
            containsService.DeleteTempFiles(); // Should NOT be logged (*Temp*)
            _ = containsService.WriteDebugOutput("output"); // Should NOT be logged (*Debug*)

            // Test mixed patterns
            Console.WriteLine("--- Testing Mixed Pattern Exclusions ---");
            var mixedService = host.Services.GetService<ExclusionPatterns.IMixedExclusionService>()!;
            _ = mixedService.ProcessMainData("should be logged");
            _ = mixedService.ToString(); // Should NOT be logged (exact)
            _ = mixedService.GetCurrentStatus(); // Should NOT be logged (Get*)
            _ = mixedService.ProcessInternal("data"); // Should NOT be logged (*Internal)
            mixedService.LogDebugInfo("info"); // Should NOT be logged (*Debug*)
            _ = mixedService.ValidateUser(123); // Should NOT be logged (Validate*)
            mixedService.ClearCache(); // Should NOT be logged (*Cache)
            _ = mixedService.ExecuteMainWorkflow("should be logged");
            _ = mixedService.GetUserData(456); // Should NOT be logged (Get*)

            // Test empty exclusion patterns (should log everything)
            Console.WriteLine("--- Testing Empty Exclusion Patterns (should log all) ---");
            var emptyService = host.Services.GetService<ExclusionPatterns.IEmptyExclusionService>()!;
            _ = emptyService.Method1("should be logged");
            _ = emptyService.GetSomething(); // Should be logged (no exclusions)
            emptyService.SetSomething("value"); // Should be logged (no exclusions)
            _ = emptyService.ValidateSomething(); // Should be logged (no exclusions)
            _ = emptyService.ProcessInternal(); // Should be logged (no exclusions)
            emptyService.LogDebugStuff(); // Should be logged (no exclusions)

            // Test wildcard pattern with exclusions
            Console.WriteLine("--- Testing Wildcard Pattern Override ---");
            var wildcardService = host.Services.GetService<ExclusionPatterns.IWildcardOverrideService>()!;
            _ = wildcardService.PublicMethod("should be logged");
            _ = wildcardService.InternalMethod("should NOT be logged"); // Should NOT be logged (Internal*)
            _ = wildcardService.ProcessPrivateData("should NOT be logged"); // Should NOT be logged (*Private*)
            _ = wildcardService.ExecutePublicWorkflow("should be logged");

            Console.WriteLine("✅ Exclusion patterns tests completed\n");
        }
    }

// ================================
// EXISTING SERVICES (Your original code)
// ================================

    public interface IRegularService
    {
        string DoWork(string input);
        int Calculate(int a, int b);
    }

    public class RegularService : IRegularService
    {
        public string DoWork(string input)
        {
            return $"Processed: {input}";
        }

        public int Calculate(int a, int b)
        {
            return a + b;
        }
    }

    public interface INoLogService
    {
        string SecretOperation(string data);
    }

    [NoLog]
    public class NoLogService : INoLogService
    {
        public string SecretOperation(string data)
        {
            return $"Secret result for: {data}";
        }
    }

    public interface ILogInputService
    {
        string ProcessData(string input);
    }

    [LogInput]
    public class LogInputService : ILogInputService
    {
        public string ProcessData(string input)
        {
            return $"Processed: {input.ToUpper()}";
        }
    }

    public interface ILogOutputService
    {
        string GenerateData(int seed);
    }

    [LogOutput]
    public class LogOutputService : ILogOutputService
    {
        public string GenerateData(int seed)
        {
            return $"Generated data with seed {seed}: {Guid.NewGuid()}";
        }
    }

    public interface ILogBothService
    {
        string Transform(string input, int multiplier);
    }

    [LogBoth]
    public class LogBothService : ILogBothService
    {
        public string Transform(string input, int multiplier)
        {
            return string.Join("-", Enumerable.Repeat(input, multiplier));
        }
    }

    public interface IMixedAttributesService
    {
        string LogInputMethod(string input);
        string LogOutputMethod(string input);
        string LogBothMethod(string input);
        string NoLogMethod(string input);
        void ThrowingMethod(string input);
        Task<string> ProcessAsyncData(string input);
    }

    public class MixedAttributesService : IMixedAttributesService
    {
        [LogInput]
        public string LogInputMethod(string input)
        {
            return $"Input method result: {input}";
        }

        [LogOutput]
        public string LogOutputMethod(string input)
        {
            return $"Output method result: {input}";
        }

        [LogBoth(level: LogLevel.Warning)]
        public string LogBothMethod(string input)
        {
            return $"Both method result: {input}";
        }

        [NoLog]
        public string NoLogMethod(string input)
        {
            return $"No log result: {input}";
        }

        [LogBoth]
        public void ThrowingMethod(string input)
        {
            throw new InvalidOperationException($"Intentional exception with input: {input}");
        }
        
        [LogBoth]
        public async Task<string> ProcessAsyncData(string input)
        {
            await Task.Delay(100);
            return $"Async result: {input.ToUpper()}";
        }
    }

    public interface ITargetedLoggingService
    {
        string LogToConsoleSimple(string input);
        string LogToConsoleJson(string input);
        string LogToDefaultTarget(string input);
        string LogWithBothTargets(string input, int value);
        void ThrowingMethodWithTarget(string input);
    }

    public class TargetedLoggingService : ITargetedLoggingService
    {
        [LogInput(target: "Console", level: LogLevel.Information)]
        public string LogToConsoleSimple(string input)
        {
            return $"Console Simple: {input}";
        }

        [LogOutput(target: "Debug", level: LogLevel.Debug)]
        public string LogToConsoleJson(string input)
        {
            return $"Console JSON: {input.ToUpper()}";
        }

        [LogBoth]
        public string LogToDefaultTarget(string input)
        {
            return $"Default Target: {input}";
        }

        [LogBoth(target: "Console", level: LogLevel.Warning)]
        public string LogWithBothTargets(string input, int value)
        {
            return $"Both Targets: {input} - {value}";
        }

        [LogBoth(target: "Debug", exceptionLevel: LogLevel.Critical)]
        public void ThrowingMethodWithTarget(string input)
        {
            throw new InvalidOperationException($"Intentional exception with target: {input}");
        }
    }

    [LogBoth]
    public class TestService
    {
        public virtual PaymentResult ProcessPayment(PaymentRequest request)
        {
            Thread.Sleep(Random.Shared.Next(10, 100));

            if (request.Amount <= 0)
            {
                throw new ArgumentException("Amount must be positive", nameof(request.Amount));
            }

            return new PaymentResult(
                Guid.NewGuid().ToString(),
                request.Amount,
                "Completed"
            );
        }

        [LogBoth]
        public virtual CustomerInfo GetCustomer(int id, string type = "premium")
        {
            if (id <= 0)
            {
                throw new InvalidOperationException($"Invalid customer ID: {id}");
            }

            return new CustomerInfo(
                id,
                $"Customer {id}",
                type,
                Random.Shared.Next(0, 10000)
            );
        }
    }

// ================================
// NEW: CONFIGURATION-BASED INTERCEPTION TESTS
// ================================

    namespace ConfigBased
    {
        // These services will be intercepted based on configuration patterns
        // Pattern: "LoggingTestConsole.ConfigBased.*" -> LogInput: true, Level: Information

        public interface IWildcardService
        {
            string ProcessWildcardData(string input);
            int CalculateWildcard(int a, int b);
        }

        public class WildcardService : IWildcardService
        {
            // Should log INPUT only due to wildcard pattern configuration
            public string ProcessWildcardData(string input)
            {
                return $"Wildcard processed: {input.ToUpper()}";
            }

            public int CalculateWildcard(int a, int b)
            {
                return a * b + 10;
            }
        }

        // This service has an exact match configuration that overrides wildcard
        // Pattern: "LoggingTestConsole.ConfigBased.PaymentService" -> LogBoth: true, Level: Warning

        public interface IPaymentService
        {
            string ProcessPayment(string cardNumber, decimal amount);
            bool ValidatePayment(string data);
            void ProcessFailingPayment(string data);
        }

        public class PaymentService : IPaymentService
        {
            // Should log BOTH input and output due to the exact match configuration
            public string ProcessPayment(string cardNumber, decimal amount)
            {
                return $"Payment processed: {cardNumber[^4..]} for ${amount}";
            }

            public bool ValidatePayment(string data)
            {
                return data.Length > 5;
            }

            public void ProcessFailingPayment(string data)
            {
                throw new InvalidOperationException($"Payment failed for: {data}");
            }
        }

        // This service has a different configuration
        // Pattern: "LoggingTestConsole.ConfigBased.AdminService" -> LogOutput: true, Level: Debug

        public interface IAdminService
        {
            string CreateUser(string username, string role);
            bool DeleteUser(string username);
        }

        public class AdminService : IAdminService
        {
            // Should log OUTPUT only due to configuration
            public string CreateUser(string username, string role)
            {
                return $"User {username} created with role {role} - ID: {Guid.NewGuid()}";
            }

            public bool DeleteUser(string username)
            {
                return username != "admin"; // Can't delete admin
            }
        }
    }

// ================================
// NEW: AUTO-INTERCEPTION TESTS
// ================================

    namespace AutoIntercept
    {
        // These services have no attributes and no configuration
        // Should be auto-intercepted when AutoIntercept: true

        public interface IAutoService
        {
            string AutoMethod1(string input);
            int AutoMethod2(int value, string suffix);
        }

        public class AutoService : IAutoService
        {
            // Should be auto-intercepted (LogInput, Information level)
            public string AutoMethod1(string input)
            {
                return $"Auto processed: {input}";
            }

            public int AutoMethod2(int value, string suffix)
            {
                return $"{value}_{suffix}".Length;
            }
        }

        public interface IBusinessService
        {
            string ProcessOrder(string orderId, decimal amount);
            string GetOrderStatus(string orderId);
        }

        public class BusinessService : IBusinessService
        {
            // Should be auto-intercepted (LogInput, Information level)
            public string ProcessOrder(string orderId, decimal amount)
            {
                return $"Order {orderId} processed for ${amount}";
            }

            public string GetOrderStatus(string orderId)
            {
                return $"Order {orderId} status: Completed";
            }
        }

        // This service is disabled at class level - should NOT be auto-intercepted
        public interface IDisabledAutoService
        {
            string ShouldNotBeLogged(string input);
        }

        [NoAutoLog]
        public class DisabledAutoService : IDisabledAutoService
        {
            // Should NOT be logged due to the [NoAutoLog] attribute
            public string ShouldNotBeLogged(string input)
            {
                return $"This should not appear in logs: {input}";
            }
        }

        // This service mixes auto-interception with method-level control
        public interface IMixedAutoService
        {
            string ShouldBeAutoLogged(string input);
            string ShouldNotBeLogged(string input);
        }

        public class MixedAutoService : IMixedAutoService
        {
            // Should be auto-intercepted
            public string ShouldBeAutoLogged(string input)
            {
                return $"Auto-logged: {input}";
            }

            // Should NOT be logged due to method-level [NoLog]
            [NoLog]
            public string ShouldNotBeLogged(string input)
            {
                return $"Not logged: {input}";
            }
        }
    }

// ================================
// NEW: PRECEDENCE TESTING
// ================================

    namespace Precedence
    {
        // Test that attributes override configuration

        public interface IPrecedenceService
        {
            string AttributeOverridesConfig(string input);
            string ConfigurationBased(string input);
            string DisabledOverridesEverything(string input);
        }

        // This class matches the "LoggingTestConsole.ConfigBased.*" pattern (LogInput config)
        // but has method-level attributes that should override
        public class PrecedenceService : IPrecedenceService
        {
            // Attribute should override configuration (LogBoth vs. config LogInput)
            [LogBoth(level: LogLevel.Critical)]
            public string AttributeOverridesConfig(string input)
            {
                return $"Attribute override: {input}";
            }

            // Should use configuration (LogInput from a wildcard pattern)
            public string ConfigurationBased(string input)
            {
                return $"Config-based: {input}";
            }

            // [NoLog] should override both configuration and auto-intercept
            [NoLog]
            public string DisabledOverridesEverything(string input)
            {
                return $"Should not be logged: {input}";
            }
        }

        // Test that configuration overrides auto-intercept

        public interface IConfigOverrideAutoService
        {
            string ConfigOverridesAuto(string input);
        }

        // This matches the "LoggingTestConsole.AutoIntercept.*" config pattern
        // Should use config LogInput instead of default auto-interception
        public class ConfigOverrideAutoService : IConfigOverrideAutoService
        {
            public string ConfigOverridesAuto(string input)
            {
                return $"Config beats auto: {input}";
            }
        }
    }

    namespace ExclusionPatterns
    {
        // Test exact method name exclusions
        public interface IExactExclusionService
        {
            string ProcessData(string input); // Should be logged
            string ToString(); // Should NOT be logged (exact match)
            int GetHashCode(); // Should NOT be logged (exact match)  
            bool Equals(object? obj); // Should NOT be logged (exact match)
            string SomeOtherMethod(string data); // Should be logged
        }

        public class ExactExclusionService : IExactExclusionService
        {
            public string ProcessData(string input) => $"Processed: {input}";
            public override string ToString() => "ExactExclusionService";
            public override int GetHashCode() => base.GetHashCode();
            public override bool Equals(object? obj) => base.Equals(obj);
            public string SomeOtherMethod(string data) => $"Other: {data}";
        }

        // Test prefix pattern exclusions (Get*, Set*, Validate*)
        public interface IPrefixExclusionService
        {
            string ProcessOrder(string orderId); // Should be logged
            string GetUserName(int userId); // Should NOT be logged (Get*)
            void SetUserStatus(int userId, string status); // Should NOT be logged (Set*)
            bool ValidateInput(string input); // Should NOT be logged (Validate*)
            string CalculateTotal(decimal amount); // Should be logged
            string GetOrderDetails(string id); // Should NOT be logged (Get*)
        }

        public class PrefixExclusionService : IPrefixExclusionService
        {
            public string ProcessOrder(string orderId) => $"Order {orderId} processed";
            public string GetUserName(int userId) => $"User_{userId}";

            public void SetUserStatus(int userId, string status)
            {
                /* set status */
            }

            public bool ValidateInput(string input) => !string.IsNullOrEmpty(input);
            public string CalculateTotal(decimal amount) => $"Total: ${amount}";
            public string GetOrderDetails(string id) => $"Details for {id}";
        }

        // Test suffix pattern exclusions (*Internal, *Helper, *Cache)
        public interface ISuffixExclusionService
        {
            string ProcessPublicData(string data); // Should be logged
            string ProcessInternal(string data); // Should NOT be logged (*Internal)
            string FormatHelper(string input); // Should NOT be logged (*Helper)
            string ClearCache(); // Should NOT be logged (*Cache)
            string ExecuteBusinessLogic(string data); // Should be logged
            void UpdateCache(string key, string value); // Should NOT be logged (*Cache)
        }

        public class SuffixExclusionService : ISuffixExclusionService
        {
            public string ProcessPublicData(string data) => $"Public: {data}";
            public string ProcessInternal(string data) => $"Internal: {data}";
            public string FormatHelper(string input) => input.ToUpper();
            public string ClearCache() => "Cache cleared";
            public string ExecuteBusinessLogic(string data) => $"Business: {data}";

            public void UpdateCache(string key, string value)
            {
                /* update cache */
            }
        }

        // Test contains pattern exclusions (*Temp*, *Debug*, *Test*)
        public interface IContainsExclusionService
        {
            string ProcessProduction(string data); // Should be logged
            string CreateTempFile(string content); // Should NOT be logged (*Temp*)
            void LogDebugInfo(string message); // Should NOT be logged (*Debug*)
            bool RunTestScenario(string scenario); // Should NOT be logged (*Test*)
            string GenerateReport(string data); // Should be logged
            void DeleteTempFiles(); // Should NOT be logged (*Temp*)
            string WriteDebugOutput(string output); // Should NOT be logged (*Debug*)
        }

        public class ContainsExclusionService : IContainsExclusionService
        {
            public string ProcessProduction(string data) => $"Prod: {data}";
            public string CreateTempFile(string content) => $"temp_file_{Guid.NewGuid()}";

            public void LogDebugInfo(string message)
            {
                /* debug logging */
            }

            public bool RunTestScenario(string scenario) => true;
            public string GenerateReport(string data) => $"Report: {data}";

            public void DeleteTempFiles()
            {
                /* cleanup */
            }

            public string WriteDebugOutput(string output) => $"DEBUG: {output}";
        }

        // Test mixed patterns (all pattern types combined)
        public interface IMixedExclusionService
        {
            string ProcessMainData(string data); // Should be logged
            string ToString(); // Should NOT be logged (exact: ToString)
            string GetCurrentStatus(); // Should NOT be logged (prefix: Get*)
            string ProcessInternal(string data); // Should NOT be logged (suffix: *Internal)
            void LogDebugInfo(string info); // Should NOT be logged (contains: *Debug*)
            bool ValidateUser(int userId); // Should NOT be logged (prefix: Validate*)
            void ClearCache(); // Should NOT be logged (suffix: *Cache)
            string ExecuteMainWorkflow(string input); // Should be logged
            string GetUserData(int id); // Should NOT be logged (prefix: Get*)
        }

        public class MixedExclusionService : IMixedExclusionService
        {
            public string ProcessMainData(string data) => $"Main: {data}";
            public override string ToString() => "MixedExclusionService";
            public string GetCurrentStatus() => "Active";
            public string ProcessInternal(string data) => $"Internal: {data}";

            public void LogDebugInfo(string info)
            {
                /* debug */
            }

            public bool ValidateUser(int userId) => userId > 0;

            public void ClearCache()
            {
                /* clear */
            }

            public string ExecuteMainWorkflow(string input) => $"Workflow: {input}";
            public string GetUserData(int id) => $"Data for {id}";
        }

        // Test empty exclusion patterns (should log everything)
        public interface IEmptyExclusionService
        {
            string Method1(string input);
            string GetSomething();
            void SetSomething(string value);
            bool ValidateSomething();
            string ProcessInternal();
            void LogDebugStuff();
        }

        public class EmptyExclusionService : IEmptyExclusionService
        {
            public string Method1(string input) => $"Method1: {input}";
            public string GetSomething() => "Something";

            public void SetSomething(string value)
            {
                /* set */
            }

            public bool ValidateSomething() => true;
            public string ProcessInternal() => "Internal process";

            public void LogDebugStuff()
            {
                /* debug */
            }
        }

        // Test wildcard pattern override (falls under LoggingTestConsole.ExclusionPatterns.*)
        public interface IWildcardOverrideService
        {
            string PublicMethod(string input); // Should be logged
            string InternalMethod(string data); // Should NOT be logged (Internal*)
            string ProcessPrivateData(string data); // Should NOT be logged (*Private*)
            string ExecutePublicWorkflow(string input); // Should be logged
        }

        public class WildcardOverrideService : IWildcardOverrideService
        {
            public string PublicMethod(string input) => $"Public: {input}";
            public string InternalMethod(string data) => $"Internal: {data}";
            public string ProcessPrivateData(string data) => $"Private: {data}";
            public string ExecutePublicWorkflow(string input) => $"Workflow: {input}";
        }

        // ================================
// MANUAL LOGGING TESTS
// ================================

        
    }

    namespace ManualLogging
    {
        // Test services with IFlexKitLogger injection
        public interface IManualOrderService
        {
            Task<string> ProcessOrderAsync(Order order);
            decimal CalculateTotal(Order order);
            void ValidateOrder(Order order);
        }

        public class ManualOrderService : IManualOrderService
        {
            private readonly IFlexKitLogger _logger;

            public ManualOrderService(IFlexKitLogger logger)
            {
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            }

            // These methods should NOT be auto-intercepted due to IFlexKitLogger injection
            public async Task<string> ProcessOrderAsync(Order order)
            {
                var startEntry = LogEntry.CreateStart(nameof(ProcessOrderAsync), GetType().FullName!)
                    .WithInput(order)
                    .WithTemplate("ManualOrderStart") // Start template
                    .WithTarget("Console");

                _logger.Log(startEntry);

                try
                {
                    await Task.Delay(50);
                    var result = $"Order {order.Id} processed for ${order.Amount}";

                    var endEntry = startEntry
                        .WithCompletion(true)
                        .WithTemplate("ManualOrderService") // Completion template
                        .WithOutput(new
                            { OrderId = order.Id, Status = "Completed" });

                    _logger.Log(endEntry);
                    return result;
                }
                catch (Exception ex)
                {
                    var errorEntry = startEntry
                        .WithCompletion(false, ex)
                        .WithTemplate("ManualOrderService"); // Error template
                    _logger.Log(errorEntry);
                    throw;
                }
            }

            public decimal CalculateTotal(Order order)
            {
                var startEntry = LogEntry.CreateStart(nameof(CalculateTotal), GetType().FullName!)
                    .WithInput(order)
                    .WithTemplate("OrderCalculationStart") // Different start template
                    .WithTarget("Debug");

                _logger.Log(startEntry);

                try
                {
                    var total = order.Amount * 1.1m;

                    var endEntry = startEntry
                        .WithCompletion(true)
                        .WithTemplate("OrderCalculation") // Completion template
                        .WithOutput(new
                            { Total = total, Tax = order.Amount * 0.1m });

                    _logger.Log(endEntry);
                    return total;
                }
                catch (Exception ex)
                {
                    var errorEntry = startEntry
                        .WithCompletion(false, ex)
                        .WithTemplate("OrderCalculation"); // Error template
                    _logger.Log(errorEntry);
                    throw;
                }
            }

            public void ValidateOrder(Order order)
            {
                var activity = _logger.StartActivity("ValidateOrder");
                var startEntry = LogEntry.CreateStart(nameof(ValidateOrder), GetType().FullName!)
                    .WithInput(order)
                    .WithTemplate("ManualOrderStart"); // Start template

                _logger.Log(startEntry);

                try
                {
                    activity?.SetTag("order.id", order.Id);
                    activity?.SetTag("order.amount", order.Amount.ToString());

                    if (order.Amount <= 0)
                    {
                        throw new ArgumentException("Order amount must be positive");
                    }

                    var endEntry = startEntry
                        .WithCompletion(true)
                        .WithTemplate("ManualOrderService"); // Success template
                    _logger.Log(endEntry);
                }
                catch (Exception ex)
                {
                    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                    var errorEntry = startEntry
                        .WithCompletion(false, ex)
                        .WithTemplate("ManualOrderService"); // Error template
                    _logger.Log(errorEntry);
                    throw;
                }
                finally
                {
                    activity?.Dispose();
                }
            }
        }

        // Add to ManualLogging namespace in Program.cs
        public interface IDefaultTemplateService
        {
            string ProcessWithDefaultTemplate(string input);
            void ProcessVoidWithDefaultTemplate(string input);
        }

        public class DefaultTemplateService : IDefaultTemplateService
        {
            private readonly IFlexKitLogger _logger;

            public DefaultTemplateService(IFlexKitLogger logger)
            {
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            }

            public string ProcessWithDefaultTemplate(string input)
            {
                // No WithTemplate() call - should use automatic template resolution
                var startEntry = LogEntry.CreateStart(nameof(ProcessWithDefaultTemplate), GetType().FullName!)
                    .WithInput(input)
                    .WithTarget("Console");

                _logger.Log(startEntry);

                try
                {
                    var result = $"Default processed: {input.ToUpper()}";

                    var endEntry = startEntry
                        .WithCompletion(true)
                        .WithOutput(result);

                    _logger.Log(endEntry);
                    return result;
                }
                catch (Exception ex)
                {
                    var errorEntry = startEntry.WithCompletion(false, ex);
                    _logger.Log(errorEntry);
                    throw;
                }
            }

            public void ProcessVoidWithDefaultTemplate(string input)
            {
                var startEntry = LogEntry.CreateStart(nameof(ProcessVoidWithDefaultTemplate), GetType().FullName!)
                    .WithInput(input)
                    .WithTarget("Console");

                _logger.Log(startEntry);

                try
                {
                    // Simulate processing
                    Thread.Sleep(10);

                    var endEntry = startEntry.WithCompletion(true);
                    _logger.Log(endEntry);
                }
                catch (Exception ex)
                {
                    var errorEntry = startEntry.WithCompletion(false, ex);
                    _logger.Log(errorEntry);
                    throw;
                }
            }
        }

        // Add to ManualLogging namespace
        public interface IExceptionTestService
        {
            string ProcessWithException(string input);
            Task<string> ProcessAsyncWithException(string input);
        }

        public class ExceptionTestService : IExceptionTestService
        {
            private readonly IFlexKitLogger _logger;

            public ExceptionTestService(IFlexKitLogger logger)
            {
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            }

            public string ProcessWithException(string input)
            {
                var startEntry = LogEntry.CreateStart(nameof(ProcessWithException), GetType().FullName!)
                    .WithInput(input)
                    .WithTemplate("ExceptionTestStart")
                    .WithTarget("Console");

                _logger.Log(startEntry);

                try
                {
                    if (input == "throw")
                    {
                        throw new InvalidOperationException("Intentional test exception");
                    }

                    var result = $"Success: {input}";

                    var endEntry = startEntry
                        .WithCompletion(true)
                        .WithTemplate("ExceptionTestService")
                        .WithOutput(result);

                    _logger.Log(endEntry);
                    return result;
                }
                catch (Exception ex)
                {
                    var errorEntry = startEntry
                        .WithCompletion(false, ex)
                        .WithTemplate("ExceptionTestService");
                    _logger.Log(errorEntry);
                    throw;
                }
            }

            public async Task<string> ProcessAsyncWithException(string input)
            {
                using var activity = _logger.StartActivity("ProcessAsyncWithException");

                var startEntry = LogEntry.CreateStart(nameof(ProcessAsyncWithException), GetType().FullName!)
                    .WithInput(input)
                    .WithTemplate("ExceptionTestStart")
                    .WithTarget("Debug");

                _logger.Log(startEntry);

                try
                {
                    activity?.SetTag("input.value", input);

                    await Task.Delay(20);

                    if (input == "async-throw")
                    {
                        throw new TimeoutException("Async test exception");
                    }

                    var result = $"Async success: {input}";

                    var endEntry = startEntry
                        .WithCompletion(true)
                        .WithTemplate("ExceptionTestService")
                        .WithOutput(result);

                    _logger.Log(endEntry);
                    return result;
                }
                catch (Exception ex)
                {
                    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                    var errorEntry = startEntry
                        .WithCompletion(false, ex)
                        .WithTemplate("ExceptionTestService");
                    _logger.Log(errorEntry);
                    throw;
                }
            }
        }

        public record Order(string Id, decimal Amount, string ProductName);
    }

// ================================
// SHARED RECORDS
// ================================

    public record PaymentRequest(decimal Amount, string Currency, int CustomerId);

    public record PaymentResult(string TransactionId, decimal Amount, string Status);

    public record CustomerInfo(int Id, string Name, string Type, decimal Balance);
}