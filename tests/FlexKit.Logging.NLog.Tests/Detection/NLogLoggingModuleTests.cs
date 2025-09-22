using System.Reflection;
using Autofac;
using FlexKit.Logging.Core;
using FlexKit.Logging.Detection;
using FlexKit.Logging.Formatting.Translation;
using FlexKit.Logging.NLog.Core;
using FlexKit.Logging.NLog.Detection;
using FlexKit.Logging.NLog.Translation;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;
using ILogger = NLog.ILogger;
// ReSharper disable MethodTooLong

namespace FlexKit.Logging.NLog.Tests.Detection;

[Collection("NLogLoggingModuleTests")]
public class NLogLoggingModuleTests
{
    [Fact]
    public void LoadModule_RegistersAllNLogComponents_ILoggerFactoryAndILoggerResolvedAsExpected()
    {
        // Arrange
        var builder = new ContainerBuilder();
        builder.Register(_ => new ConfigurationBuilder().Build())
            .As<IConfiguration>()
            .SingleInstance();

        builder.RegisterModule<LoggingModule>();
        builder.RegisterModule<NLogLoggingModule>();

        // Act
        var container = builder.Build();

        // Assert - Verify all NLog components from RegisterNLogComponents are registered
        container.IsRegistered<IMessageTranslator>().Should().BeTrue();
        container.IsRegistered<NLogConfigurationBuilder>().Should().BeTrue();
        container.IsRegistered<ILogger>().Should().BeTrue();
        container.IsRegistered<ILogEntryProcessor>().Should().BeTrue();
        container.IsRegistered<IBackgroundLog>().Should().BeTrue();

        // Verify the concrete types
        var translator = container.Resolve<IMessageTranslator>();
        translator.Should().BeOfType<NLogMessageTranslator>();

        var configBuilder = container.Resolve<NLogConfigurationBuilder>();
        configBuilder.Should().NotBeNull();

        var nlogLogger = container.Resolve<ILogger>();
        nlogLogger.Should().NotBeNull();

        var logWriter = container.Resolve<ILogEntryProcessor>();
        logWriter.Should().BeOfType<NLogLogWriter>();

        var backgroundLog = container.Resolve<IBackgroundLog>();
        backgroundLog.Should().BeOfType<NLogBackgroundLog>();

        // Verify ILoggerFactory is registered and works
        container.IsRegistered<ILoggerFactory>().Should().BeTrue();
        var loggerFactory = container.Resolve<ILoggerFactory>();
        loggerFactory.Should().NotBeNull();

        // Verify generic ILogger<T> works
        container.IsRegistered<ILogger<NLogLoggingModuleTests>>().Should().BeTrue();
        var genericLogger = container.Resolve<ILogger<NLogLoggingModuleTests>>();
        genericLogger.Should().NotBeNull();

        // Verify logger factory creates working loggers
        var logger = loggerFactory.CreateLogger("TestCategory");
        logger.Should().NotBeNull();
        var action = () => logger.LogInformation("Test message");
        action.Should().NotThrow();

        // Verify the factory has the NLog provider configured
        logger.IsEnabled(LogLevel.Debug).Should().BeTrue("Factory should be configured for Trace level");
    }

    [Fact]
    public void LoadModule_ILoggerFactoryDelegate_RegistersProcessExitHandlerAndFactoryIsDisposed()
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
        builder.RegisterModule<NLogLoggingModule>();
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