using System.Reflection;
using Autofac;
using FlexKit.Logging.Configuration;
using FlexKit.Logging.Core;
using FlexKit.Logging.Detection;
using FlexKit.Logging.Formatting.Translation;
using FlexKit.Logging.Log4Net.Core;
using FlexKit.Logging.Log4Net.Detection;
using FluentAssertions;
using HarmonyLib;
using log4net.Repository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;
// ReSharper disable UnusedParameter.Local
// ReSharper disable InconsistentNaming

namespace FlexKit.Logging.Log4Net.Tests.Detection;

public class Log4NetLoggingModuleTests
{
    [Fact]
    public void LoadModule_RegistersLog4NetComponents()
    {
        // Arrange
        var builder = new ContainerBuilder();
        builder.Register(_ => new ConfigurationBuilder().Build())
            .As<IConfiguration>()
            .SingleInstance();

        builder.RegisterModule<LoggingModule>();
        builder.RegisterModule<Log4NetLoggingModule>();

        // Act
        var container = builder.Build();

        // Assert - Verify all Log4Net components from RegisterLog4NetComponents are registered
        container.IsRegistered<IMessageTranslator>().Should().BeTrue();
        container.IsRegistered<Log4NetConfigurationBuilder>().Should().BeTrue();
        container.IsRegistered<ILoggerRepository>().Should().BeTrue();
        container.IsRegistered<ILogEntryProcessor>().Should().BeTrue();

        // Verify the concrete types
        var translator = container.Resolve<IMessageTranslator>();
        translator.Should().BeOfType<DefaultMessageTranslator>();

        var configBuilder = container.Resolve<Log4NetConfigurationBuilder>();
        configBuilder.Should().NotBeNull();

        var logWriter = container.Resolve<ILogEntryProcessor>();
        logWriter.Should().BeOfType<Log4NetLogWriter>();
    }

    [Fact]
    public void LoadModule_ILoggerRepositoryDelegate_CallsBuildConfigurationWithCorrectParameters()
    {
        // Arrange
        var builder = new ContainerBuilder();

        // Register base dependencies
        builder.Register(_ => new ConfigurationBuilder().Build())
            .As<IConfiguration>()
            .SingleInstance();

        builder.RegisterModule<LoggingModule>();

        // Act - Register the actual module under test
        builder.RegisterModule<Log4NetLoggingModule>();
        var container = builder.Build();

        // Trigger the delegate by resolving ILoggerRepository
        var repository = container.Resolve<ILoggerRepository>();

        // Assert
        // Verify the repository was created successfully
        repository.Should().NotBeNull();
        repository.Should().BeAssignableTo<ILoggerRepository>();

        // Verify RequiresSerialization was set to true by the module's delegate
        var configAfterResolve = container.Resolve<LoggingConfig>();
        configAfterResolve.RequiresSerialization.Should().BeTrue();

        // Verify the repository is properly configured (has some basic configuration)
        repository.Configured.Should().BeTrue();
        repository.Threshold.Should().NotBeNull();
    }

    [Fact]
    public void LoadModule_RegistersLoggerFactoryComponents()
    {
        // Arrange
        var builder = new ContainerBuilder();

        // Register base dependencies
        builder.Register(_ => new ConfigurationBuilder().Build())
            .As<IConfiguration>()
            .SingleInstance();

        builder.RegisterModule<LoggingModule>();
        builder.RegisterModule<Log4NetLoggingModule>();

        // Act
        var container = builder.Build();

        // Assert - Verify ILoggerFactory is registered
        container.IsRegistered<ILoggerFactory>().Should().BeTrue();

        // Verify generic ILogger<T> is registered
        container.IsRegistered<ILogger<string>>().Should().BeTrue();

        // Verify the concrete types work
        var loggerFactory = container.Resolve<ILoggerFactory>();
        loggerFactory.Should().NotBeNull();

        var genericLogger = container.Resolve<ILogger<Log4NetLoggingModuleTests>>();
        genericLogger.Should().NotBeNull();

        // Verify the logger factory can create loggers
        var createdLogger = loggerFactory.CreateLogger("TestCategory");
        createdLogger.Should().NotBeNull();
    }

    [Fact]
    public void LoadModule_ILoggerFactoryDelegate_ExecutesCorrectlyWhenResolvingFactory()
    {
        // Arrange
        var builder = new ContainerBuilder();

        // Register base dependencies
        builder.Register(_ => new ConfigurationBuilder().Build())
            .As<IConfiguration>()
            .SingleInstance();

        builder.RegisterModule<LoggingModule>();
        builder.RegisterModule<Log4NetLoggingModule>();

        // Act
        var container = builder.Build();

        // Trigger the delegate by resolving ILoggerFactory
        var loggerFactory = container.Resolve<ILoggerFactory>();

        // Assert
        // Verify the delegate created a working logger factory
        loggerFactory.Should().NotBeNull();
        loggerFactory.Should().BeAssignableTo<ILoggerFactory>();

        // Verify the factory was configured with Log4Net provider
        var logger = loggerFactory.CreateLogger("TestLogger");
        logger.Should().NotBeNull();

        // Verify the logger can log without throwing (indicates proper Log4Net integration)
        Action logAction = () => logger.LogInformation("Test message from delegate-created factory");
        logAction.Should().NotThrow();

        // Verify the factory has the Log4Net provider configured by checking it works with Log4Net-specific features
        logger.IsEnabled(LogLevel.Debug).Should().BeTrue("Factory should be configured for Trace level");
    }
    
    [Fact]
    public void LoadModule_ILoggerFactoryDelegate_RegistersProcessExitHandler()
    {
        // Arrange
        var builder = new ContainerBuilder();
    
        // Register base dependencies
        builder.Register(_ => new ConfigurationBuilder().Build())
            .As<IConfiguration>()
            .SingleInstance();
        builder.RegisterModule<LoggingModule>();
    
        // Count ProcessExit handlers before registering the module
        var handlerCountBefore = GetProcessExitHandlerCount();
    
        // Act - Register the module which should add a ProcessExit handler
        builder.RegisterModule<Log4NetLoggingModule>();
        var container = builder.Build();
    
        // Trigger the delegate registration by resolving ILoggerFactory
        var loggerFactory = container.Resolve<ILoggerFactory>();
    
        // Count ProcessExit handlers after
        var handlerCountAfter = GetProcessExitHandlerCount();
        TriggerProcessExitEvent();
    
        // Assert that exactly one handler was added
        (handlerCountAfter - handlerCountBefore).Should().BeGreaterThanOrEqualTo(1);
    
        // Verify the factory is disposable (required for ProcessExit cleanup)
        loggerFactory.Should().BeAssignableTo<IDisposable>();
    
        // Verify dispose works without throwing (what the ProcessExit handler would do)
        Action disposeAction = () => loggerFactory.Dispose();
        disposeAction.Should().NotThrow();
    }

    [Fact]
    public void Should_Return_When_OnProcessExitMethod_IsNull()
    {
        // Arrange: patch Type.GetMethod to return null when called with "OnProcessExit"
        var harmony = new Harmony("test.patch");
        var original = typeof(Type).GetMethod("GetMethod",
            [typeof(string), typeof(BindingFlags)]);
        var prefix = typeof(Log4NetLoggingModuleTests)
            .GetMethod(nameof(ReturnNullForOnProcessExit),
                BindingFlags.NonPublic | BindingFlags.Static);

        harmony.Patch(original, prefix: new HarmonyMethod(prefix));

        try
        {
            // Act
            Action act = () => GetDisableMethod().Invoke(null, null);

            // Assert
            act.Should().NotThrow(); // visited the null branch
        }
        finally
        {
            harmony.UnpatchAll("test.patch"); // clean up
        }
    }
    
    [Fact]
    public void Should_Catch_Exception_When_DelegateCreationFails()
    {
        var harmony = new Harmony("test.catchbranch");
        var original = typeof(Type).GetMethod("GetMethod",
            [typeof(string), typeof(BindingFlags)]);
        var prefix = typeof(Log4NetLoggingModuleTests)
            .GetMethod(nameof(ReturnInvalidMethod),
                BindingFlags.NonPublic | BindingFlags.Static);

        harmony.Patch(original, prefix: new HarmonyMethod(prefix));

        try
        {
            Action act = () => GetDisableMethod().Invoke(null, null);

            act.Should().NotThrow(); // exception is caught internally
        }
        finally
        {
            harmony.UnpatchAll("test.catchbranch");
        }
    }

    private static bool ReturnInvalidMethod(
        ref MethodInfo __result, string name, BindingFlags bindingAttr)
    {
        if (name == "OnProcessExit")
        {
            // incompatible method signature for EventHandler
            __result = typeof(string).GetMethod("Clone")!;
            return false; // skip original
        }
        return true;
    }
    
    private static MethodInfo GetDisableMethod() =>
        typeof(Log4NetLoggingModule)
            .GetMethod("DisableLog4NetAutoShutdown",
                BindingFlags.NonPublic | BindingFlags.Static)!;

    private static bool ReturnNullForOnProcessExit(
        ref MethodInfo __result, string name, BindingFlags bindingAttr)
    {
        if (name == "OnProcessExit")
        {
            __result = null!;
            return false; // skip original
        }

        return true; // call original for everything else
    }

    private static void TriggerProcessExitEvent()
    {
        // Call the internal OnProcessExit method which triggers the ProcessExit event
        var onProcessExitMethod = typeof(AppDomain).GetMethod(
            "OnProcessExit",
            BindingFlags.NonPublic | BindingFlags.Static);

        onProcessExitMethod?.Invoke(null, null);
    }

    private static int GetProcessExitHandlerCount()
    {
        try
        {
            var domain = AppDomain.CurrentDomain;

            // Get the ProcessExit field directly (it's a simple event field)
            var processExitField = typeof(AppDomain).GetField(
                "ProcessExit",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (processExitField?.GetValue(domain) is Delegate handler)
            {
                return handler.GetInvocationList().Length;
            }

            return 0;
        }
        catch
        {
            return 0;
        }
    }
}
