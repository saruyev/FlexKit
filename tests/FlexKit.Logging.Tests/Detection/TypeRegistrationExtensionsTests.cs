using System.Diagnostics;
using Autofac;
using FlexKit.Logging.Configuration;
using FlexKit.Logging.Core;
using FlexKit.Logging.Detection;
using FlexKit.Logging.Interception;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;
// ReSharper disable ClassWithVirtualMembersNeverInherited.Global

namespace FlexKit.Logging.Tests.Detection;

/// <summary>
/// Unit tests for TypeRegistrationExtensions.RegisterTypeWithLogging method covering three registration scenarios.
/// </summary>
public class TypeRegistrationExtensionsTests
{
    [Fact]
    public void RegisterTypeWithLogging_WithTypeHavingUserInterfaces_UsesInterfaceInterception()
    {
        // Arrange
        var containerBuilder = new ContainerBuilder();
        SetupDependencies(containerBuilder);

        // Act
        containerBuilder.RegisterTypesWithLogging([typeof(ServiceWithInterface)]);

        // Assert
        using var container = containerBuilder.Build();
        var service = container.Resolve<ITestService>();
        
        service.Should().NotBeNull();
        // Service is proxied, so check the underlying type
        service.GetType().Name.Should().Contain("ITestServiceProxy");
    }

    [Fact] 
    public void RegisterTypeWithLogging_WithClassHavingVirtualMethods_UsesClassInterception()
    {
        // Arrange
        var containerBuilder = new ContainerBuilder();
        SetupDependencies(containerBuilder);

        // Act
        containerBuilder.RegisterTypesWithLogging([typeof(ServiceWithVirtualMethods)]);

        // Assert
        using var container = containerBuilder.Build();
        var service = container.Resolve<ServiceWithVirtualMethods>();
        
        service.Should().NotBeNull();
    }

    [Fact]
    public void RegisterTypeWithLogging_WithSealedClassNoInterfaces_RegistersWithoutInterception()
    {
        // Arrange
        var containerBuilder = new ContainerBuilder();
        SetupDependencies(containerBuilder);
        var traceOutput = new List<string>();
        var testListener = new TestTraceListener(traceOutput);
        Trace.Listeners.Add(testListener);

        try
        {
            // Act
            containerBuilder.RegisterTypesWithLogging([typeof(SealedServiceWithoutInterfaces)]);

            // Assert
            using var container = containerBuilder.Build();
            var service = container.Resolve<SealedServiceWithoutInterfaces>();
            
            service.Should().NotBeNull();
            traceOutput.Should().Contain(msg => msg.Contains("Warning: Cannot intercept") && 
                                              msg.Contains("SealedServiceWithoutInterfaces"));
        }
        finally
        {
            Trace.Listeners.Remove(testListener);
        }
    }

    private static void SetupDependencies(ContainerBuilder builder)
    {
        // Register configuration
        builder.Register(_ => new ConfigurationBuilder().Build())
               .As<IConfiguration>()
               .SingleInstance();

        // Register logging config
        builder.Register(_ => new LoggingConfig())
               .As<LoggingConfig>()
               .SingleInstance();

        // Register decision cache
        builder.Register(c => new InterceptionDecisionCache(c.Resolve<LoggingConfig>()))
               .AsSelf()
               .SingleInstance();

        // Register background log
        builder.RegisterType<BackgroundLog>()
               .As<IBackgroundLog>()
               .SingleInstance();

        // Register logger factory and logger
        builder.RegisterType<LoggerFactory>()
               .As<ILoggerFactory>()
               .SingleInstance();

        builder.RegisterGeneric(typeof(Logger<>))
               .As(typeof(ILogger<>))
               .SingleInstance();

        // Register interceptor
        builder.RegisterType<MethodLoggingInterceptor>()
               .AsSelf()
               .InstancePerLifetimeScope();
    }
}

// Test types for the scenarios
public interface ITestService
{
    void DoWork();
}

public class ServiceWithInterface : ITestService
{
    public void DoWork() { }
}

public class ServiceWithVirtualMethods
{
    public virtual void ProcessData() { }
    public virtual string GetResult() => "result";
}

public sealed class SealedServiceWithoutInterfaces  
{
    public void Execute() { }
    public string GetStatus() => "status";
}

// Helper class to capture Trace output
public class TestTraceListener(List<string> output) : TraceListener
{
    public override void Write(string? message)
    {
        if (message != null)
            output.Add(message);
    }

    public override void WriteLine(string? message)
    {
        if (message != null)
            output.Add(message);
    }
}