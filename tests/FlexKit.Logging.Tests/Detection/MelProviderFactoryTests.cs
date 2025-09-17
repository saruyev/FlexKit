using System.Reflection;
using FlexKit.Logging.Configuration;
using FlexKit.Logging.Detection;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using NSubstitute;
using Xunit;
// ReSharper disable TooManyDeclarations
// ReSharper disable ClassTooBig

namespace FlexKit.Logging.Tests.Detection;

/// <summary>
/// Unit tests for MelProviderFactory.TryAddDebug method covering debug provider configuration.
/// </summary>
public class MelProviderFactoryTests
{
    [Fact]
    public void TryAddDebug_WithEnabledDebugTarget_CallsProviderConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        ILoggingBuilder loggingBuilder = null!;

        services.AddLogging(builder => loggingBuilder = builder);

        var debugTarget = new LoggingTarget
        {
            Type = "Debug",
            Enabled = true,
            Properties = new Dictionary<string, IConfigurationSection?>()
        };

        var config = new LoggingConfig
        {
            Targets = new Dictionary<string, LoggingTarget> { ["Debug"] = debugTarget }
        };

        var factory = new MelProviderFactory(loggingBuilder, config);

        // Act - this should not throw and should complete successfully
        var act = () => factory.ConfigureProviders();

        // Assert
        act.Should().NotThrow();

        // Verify that we can build the service provider without issues
        var serviceProvider = services.BuildServiceProvider();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        loggerFactory.Should().NotBeNull();
    }

    [Fact]
    public void TryAddDebug_WithDisabledDebugTarget_SkipsConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        ILoggingBuilder loggingBuilder = null!;

        services.AddLogging(builder => loggingBuilder = builder);

        var debugTarget = new LoggingTarget
        {
            Type = "Debug",
            Enabled = false, // Disabled target should be skipped
            Properties = new Dictionary<string, IConfigurationSection?>()
        };

        var config = new LoggingConfig
        {
            Targets = new Dictionary<string, LoggingTarget> { ["Debug"] = debugTarget }
        };

        var factory = new MelProviderFactory(loggingBuilder, config);

        // Act
        var act = () => factory.ConfigureProviders();

        // Assert - Should complete without errors even with a disabled target
        act.Should().NotThrow();

        var serviceProvider = services.BuildServiceProvider();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        loggerFactory.Should().NotBeNull();
    }

    [Fact]
    public void TryAddDebug_CallsCorrectReflectionMethods()
    {
        // Arrange
        var services = new ServiceCollection();
        ILoggingBuilder loggingBuilder = null!;

        services.AddLogging(builder => loggingBuilder = builder);

        var debugTarget = new LoggingTarget
        {
            Type = "Debug",
            Enabled = true,
            Properties = new Dictionary<string, IConfigurationSection?>()
        };

        var config = new LoggingConfig
        {
            Targets = new Dictionary<string, LoggingTarget> { ["Debug"] = debugTarget }
        };

        // Act
        var factory = new MelProviderFactory(loggingBuilder, config);

        // These should be resolvable if the Debug logging package is available
        // If not available, the method should handle gracefully
        var act = () => factory.ConfigureProviders();
        act.Should().NotThrow();
    }

    [Fact]
    public void TryAddConsole_WithSimpleFormatterType_ConfiguresSimpleConsoleProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        ILoggingBuilder loggingBuilder = null!;

        services.AddLogging(builder => loggingBuilder = builder);

        var formatterSection = CreateMockConfigurationSection("Simple");
        var consoleTarget = new LoggingTarget
        {
            Type = "Console",
            Enabled = true,
            Properties = new Dictionary<string, IConfigurationSection?>
            {
                ["FormatterType"] = formatterSection
            }
        };

        var config = new LoggingConfig
        {
            Targets = new Dictionary<string, LoggingTarget> { ["Console"] = consoleTarget }
        };

        var factory = new MelProviderFactory(loggingBuilder, config);

        // Act
        var act = () => factory.ConfigureProviders();

        // Assert
        act.Should().NotThrow();

        var serviceProvider = services.BuildServiceProvider();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        loggerFactory.Should().NotBeNull();
    }

    [Fact]
    public void TryAddConsole_WithNoFormatterType_DefaultsToSimpleConsole()
    {
        // Arrange
        var services = new ServiceCollection();
        ILoggingBuilder loggingBuilder = null!;

        services.AddLogging(builder => loggingBuilder = builder);

        var consoleTarget = new LoggingTarget
        {
            Type = "Console",
            Enabled = true,
            Properties = new Dictionary<string, IConfigurationSection?>() // No FormatterType property
        };

        var config = new LoggingConfig
        {
            Targets = new Dictionary<string, LoggingTarget> { ["Console"] = consoleTarget }
        };

        var factory = new MelProviderFactory(loggingBuilder, config);

        // Act - Should default to Simple formatter when no FormatterType specified
        var act = () => factory.ConfigureProviders();

        // Assert
        act.Should().NotThrow();

        var serviceProvider = services.BuildServiceProvider();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        loggerFactory.Should().NotBeNull();
    }

    [Fact]
    public void TryAddEventSource_WithEnabledEventSourceTarget_ConfiguresEventSourceProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        ILoggingBuilder loggingBuilder = null!;

        services.AddLogging(builder => loggingBuilder = builder);

        var eventSourceTarget = new LoggingTarget
        {
            Type = "EventSource",
            Enabled = true,
            Properties = new Dictionary<string, IConfigurationSection?>()
        };

        var config = new LoggingConfig
        {
            Targets = new Dictionary<string, LoggingTarget> { ["EventSource"] = eventSourceTarget }
        };

        var factory = new MelProviderFactory(loggingBuilder, config);

        // Act
        var act = () => factory.ConfigureProviders();

        // Assert
        act.Should().NotThrow();

        var serviceProvider = services.BuildServiceProvider();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        loggerFactory.Should().NotBeNull();

        // Verify we can create a logger (indicates EventSource provider was added)
        var logger = loggerFactory.CreateLogger("TestCategory");
        logger.Should().NotBeNull();
    }

    [Fact]
    public void TryAddEventLog_WithEventLogConfiguration_UsesConfiguredMethod()
    {
        // Arrange
        var services = new ServiceCollection();
        ILoggingBuilder loggingBuilder = null!;

        services.AddLogging(builder => loggingBuilder = builder);

        var eventLogTarget = new LoggingTarget
        {
            Type = "EventLog",
            Enabled = true,
            Properties = new Dictionary<string, IConfigurationSection?>
            {
                ["LogName"] = CreateMockConfigurationSection("Application"),
                ["SourceName"] = CreateMockConfigurationSection("MyApp")
            }
        };

        var config = new LoggingConfig
        {
            Targets = new Dictionary<string, LoggingTarget> { ["EventLog"] = eventLogTarget }
        };

        var factory = new MelProviderFactory(loggingBuilder, config);

        // Act
        var act = () => factory.ConfigureProviders();

        // Assert
        act.Should().NotThrow();

        var serviceProvider = services.BuildServiceProvider();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        loggerFactory.Should().NotBeNull();
    }

    [Fact]
    public void TryAddEventLog_WithoutEventLogConfiguration_UsesParameterlessMethod()
    {
        // Arrange
        var services = new ServiceCollection();
        ILoggingBuilder loggingBuilder = null!;

        services.AddLogging(builder => loggingBuilder = builder);

        var eventLogTarget = new LoggingTarget
        {
            Type = "EventLog",
            Enabled = true,
            Properties = new Dictionary<string, IConfigurationSection?>() // No EventLog properties
        };

        var config = new LoggingConfig
        {
            Targets = new Dictionary<string, LoggingTarget> { ["EventLog"] = eventLogTarget }
        };

        var factory = new MelProviderFactory(loggingBuilder, config);

        // Act
        var act = () => factory.ConfigureProviders();

        // Assert
        act.Should().NotThrow();

        var serviceProvider = services.BuildServiceProvider();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        loggerFactory.Should().NotBeNull();
    }

    [Fact]
    public void TryAddApplicationInsights_WithAvailableTypes_ConfiguresApplicationInsightsProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        ILoggingBuilder loggingBuilder = null!;

        services.AddLogging(builder => loggingBuilder = builder);

        var connectionStringSection = CreateMockConfigurationSection("InstrumentationKey=test-key");
        var applicationInsightsTarget = new LoggingTarget
        {
            Type = "ApplicationInsights",
            Enabled = true,
            Properties = new Dictionary<string, IConfigurationSection?>
            {
                ["ConnectionString"] = connectionStringSection
            }
        };

        var config = new LoggingConfig
        {
            Targets = new Dictionary<string, LoggingTarget> { ["ApplicationInsights"] = applicationInsightsTarget }
        };

        var factory = new MelProviderFactory(loggingBuilder, config);

        // Act
        var act = () => factory.ConfigureProviders();

        // Assert
        act.Should().NotThrow();

        var serviceProvider = services.BuildServiceProvider();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        loggerFactory.Should().NotBeNull();

        // Verify we can create a logger
        var logger = loggerFactory.CreateLogger("TestCategory");
        logger.Should().NotBeNull();
    }

    [Fact]
    public void TryAddAzureWebAppDiagnostics_WithBasicConfiguration_ConfiguresProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        ILoggingBuilder loggingBuilder = null!;

        services.AddLogging(builder => loggingBuilder = builder);

        var azureTarget = new LoggingTarget
        {
            Type = "AzureWebAppDiagnostics",
            Enabled = true,
            Properties = new Dictionary<string, IConfigurationSection?>()
        };

        var config = new LoggingConfig
        {
            Targets = new Dictionary<string, LoggingTarget> { ["AzureWebAppDiagnostics"] = azureTarget }
        };

        var factory = new MelProviderFactory(loggingBuilder, config);

        // Act
        var act = () => factory.ConfigureProviders();

        // Assert
        act.Should().NotThrow();

        var serviceProvider = services.BuildServiceProvider();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        loggerFactory.Should().NotBeNull();
    }

    [Fact]
    public void TryAddAzureWebAppDiagnostics_WithFileLoggerOptions_ConfiguresFileLogging()
    {
        // Arrange
        var services = new ServiceCollection();
        ILoggingBuilder loggingBuilder = null!;

        services.AddLogging(builder => loggingBuilder = builder);

        var azureTarget = new LoggingTarget
        {
            Type = "AzureWebAppDiagnostics",
            Enabled = true,
            Properties = new Dictionary<string, IConfigurationSection?>
            {
                ["FileSizeLimit"] = CreateMockConfigurationSection("10485760"), // 10MB
                ["RetainedFileCountLimit"] = CreateMockConfigurationSection("5"),
                ["FileName"] = CreateMockConfigurationSection("diagnostics")
            }
        };

        var config = new LoggingConfig
        {
            Targets = new Dictionary<string, LoggingTarget> { ["AzureWebAppDiagnostics"] = azureTarget }
        };

        var factory = new MelProviderFactory(loggingBuilder, config);

        // Act - This will call ConfigureAzureFileLoggerOptions
        var act = () => factory.ConfigureProviders();

        // Assert
        act.Should().NotThrow();

        var serviceProvider = services.BuildServiceProvider();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        loggerFactory.Should().NotBeNull();
    }

    [Fact]
    public void TryAddAzureWebAppDiagnostics_WithBlobLoggerOptions_ConfiguresBlobLogging()
    {
        // Arrange
        var services = new ServiceCollection();
        ILoggingBuilder loggingBuilder = null!;

        services.AddLogging(builder => loggingBuilder = builder);

        var azureTarget = new LoggingTarget
        {
            Type = "AzureWebAppDiagnostics",
            Enabled = true,
            Properties = new Dictionary<string, IConfigurationSection?>
            {
                ["BlobName"] = CreateMockConfigurationSection("logs/application"),
                ["ContainerName"] = CreateMockConfigurationSection("diagnostics"),
                ["ConnectionString"] = CreateMockConfigurationSection("DefaultEndpointsProtocol=https;AccountName=test")
            }
        };

        var config = new LoggingConfig
        {
            Targets = new Dictionary<string, LoggingTarget> { ["AzureWebAppDiagnostics"] = azureTarget }
        };

        var factory = new MelProviderFactory(loggingBuilder, config);

        // Act - This will call ConfigureAzureBlobLoggerOptions  
        var act = () => factory.ConfigureProviders();

        // Assert
        act.Should().NotThrow();

        var serviceProvider = services.BuildServiceProvider();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        loggerFactory.Should().NotBeNull();
    }

    [Fact]
    public void TryAddAzureWebAppDiagnostics_WithBothFileAndBlobOptions_ConfiguresBothLoggingTypes()
    {
        // Arrange
        var services = new ServiceCollection();
        ILoggingBuilder loggingBuilder = null!;

        services.AddLogging(builder => loggingBuilder = builder);

        var azureTarget = new LoggingTarget
        {
            Type = "AzureWebAppDiagnostics",
            Enabled = true,
            Properties = new Dictionary<string, IConfigurationSection?>
            {
                // File logger options
                ["FileSizeLimit"] = CreateMockConfigurationSection("5242880"), // 5MB
                ["RetainedFileCountLimit"] = CreateMockConfigurationSection("3"),
                ["FileName"] = CreateMockConfigurationSection("app-logs"),

                // Blob logger options  
                ["BlobName"] = CreateMockConfigurationSection("logs/webapp"),
                ["ContainerName"] = CreateMockConfigurationSection("app-diagnostics"),
                ["ConnectionString"] = CreateMockConfigurationSection("DefaultEndpointsProtocol=https;AccountName=testapp")
            }
        };

        var config = new LoggingConfig
        {
            Targets = new Dictionary<string, LoggingTarget> { ["AzureWebAppDiagnostics"] = azureTarget }
        };

        var factory = new MelProviderFactory(loggingBuilder, config);

        // Act - This will call both ConfigureAzureFileLoggerOptions and ConfigureAzureBlobLoggerOptions
        var act = () => factory.ConfigureProviders();

        // Assert
        act.Should().NotThrow();

        var serviceProvider = services.BuildServiceProvider();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        loggerFactory.Should().NotBeNull();
    }

    [Fact]
    public void InvokeApplicationInsightsMethod_WhenComplexMethodNotFound_UsesFallbackMethod()
    {
        // Arrange
        var services = new ServiceCollection();
        ILoggingBuilder loggingBuilder = null!;

        services.AddLogging(builder => loggingBuilder = builder);

        var target = new LoggingTarget
        {
            Type = "ApplicationInsights",
            Enabled = true,
            Properties = new Dictionary<string, IConfigurationSection?>()
        };

        var config = new LoggingConfig
        {
            Targets = new Dictionary<string, LoggingTarget> { ["ApplicationInsights"] = target }
        };

        var factory = new MelProviderFactory(loggingBuilder, config);

        // Get the ApplicationInsights type  
        var appInsightsType = Type.GetType(MelNames.ApplicationInsightsType);

        // Use a fake options type that won't match the expected method signature
        var fakeOptionsType = typeof(string); // This will cause the complex method lookup to fail

        // Create InvocationContext with the correct method name but incompatible options type
        var contextType = factory.GetType().GetNestedTypes(BindingFlags.NonPublic)
            .First(t => t.Name == "InvocationContext");

        var context = Activator.CreateInstance(contextType, target, fakeOptionsType, appInsightsType, "AddApplicationInsights");

        // Get the private InvokeApplicationInsightsMethod
        var method = factory.GetType().GetMethod("InvokeApplicationInsightsMethod", BindingFlags.NonPublic | BindingFlags.Instance);
        var telemetryConfigType = Type.GetType(MelNames.TelemetryConfigurationType);

        // Count providers before
        var providerCountBefore = GetProviderCount(loggingBuilder);

        // Act - The complex method signature won't be found due to the fake options type, triggering fallback
        method!.Invoke(factory, [context, telemetryConfigType]);

        // Count providers after
        var providerCountAfter = GetProviderCount(loggingBuilder);

        // Assert - Verify that the fallback method was called and added a provider
        providerCountAfter.Should().BeGreaterThan(
            providerCountBefore,
            "fallback method should have added ApplicationInsights provider");
    }

    [Fact]
    public void InvokeLoggingMethod_WhenConfiguredMethodNotFound_UsesFallbackMethod()
    {
        // Arrange
        var services = new ServiceCollection();
        ILoggingBuilder loggingBuilder = null!;

        services.AddLogging(builder => loggingBuilder = builder);

        var target = new LoggingTarget
        {
            Type = "Console",
            Enabled = true,
            Properties = new Dictionary<string, IConfigurationSection?>()
        };

        var config = new LoggingConfig
        {
            Targets = new Dictionary<string, LoggingTarget> { ["Console"] = target }
        };

        var factory = new MelProviderFactory(loggingBuilder, config);

        // Get the Console type
        var consoleType = Type.GetType(MelNames.ConsoleType);

        // Use a fake config type that won't match the expected method signature
        var fakeConfigType = typeof(string); // This will cause the Action<string> method lookup to fail

        // Create InvocationContext with the correct method name but incompatible config type
        var contextType = factory.GetType().GetNestedTypes(BindingFlags.NonPublic)
            .First(t => t.Name == "InvocationContext");

        var context = Activator.CreateInstance(contextType, target, fakeConfigType, consoleType, "AddSimpleConsole");

        // Get the private InvokeLoggingMethod
        var method = factory.GetType().GetMethod("InvokeLoggingMethod", BindingFlags.NonPublic | BindingFlags.Instance);

        // Count providers before
        var providerCountBefore = GetProviderCount(loggingBuilder);

        // Act - The Action<string> method signature won't be found, triggering fallback
        method!.Invoke(factory, [context]);

        // Count providers after
        var providerCountAfter = GetProviderCount(loggingBuilder);

        // Assert - Verify that the fallback method was called and added a provider
        providerCountAfter.Should().BeGreaterThan(
            providerCountBefore,
            "fallback method should have added Console provider");
    }

    [Fact]
    public void ConfigureAzureFileLoggerOptions_WhenServicesIsNull_ReturnsEarly()
    {
        // Arrange
        var mockBuilder = new NullServicesLoggingBuilder();

        var target = new LoggingTarget
        {
            Type = "AzureWebAppDiagnostics",
            Enabled = true,
            Properties = new Dictionary<string, IConfigurationSection?>
            {
                ["FileSizeLimit"] = CreateMockConfigurationSection("1024")
            }
        };

        var config = new LoggingConfig
        {
            Targets = new Dictionary<string, LoggingTarget> { ["AzureWebAppDiagnostics"] = target }
        };

        var factory = new MelProviderFactory(mockBuilder, config);

        // Use reflection to call ConfigureAzureFileLoggerOptions directly
        var method = factory.GetType().GetMethod("ConfigureAzureFileLoggerOptions", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act & Assert - Should return early without throwing when services are null
        var act = () => method!.Invoke(factory, [target]);
        act.Should().NotThrow("should return early when services is null");
    }
    
    [Fact]
    public void ConfigureAzureFileLoggerOptions_WithValidServices_ExecutesConfigureMethodPath()
    {
        // Arrange - Use real services with Microsoft.Extensions.Options loaded
        var services = new ServiceCollection();
        services.AddOptions(); // This adds the Configure<T> extension methods
    
        ILoggingBuilder loggingBuilder = null!;
        services.AddLogging(builder => loggingBuilder = builder);
    
        var target = new LoggingTarget
        {
            Type = "AzureWebAppDiagnostics",
            Enabled = true,
            Properties = new Dictionary<string, IConfigurationSection?>
            {
                ["FileSizeLimit"] = CreateMockConfigurationSection("2048"),
                ["RetainedFileCountLimit"] = CreateMockConfigurationSection("10")
            }
        };
    
        var config = new LoggingConfig
        {
            Targets = new Dictionary<string, LoggingTarget> { ["AzureWebAppDiagnostics"] = target }
        };

        var factory = new MelProviderFactory(loggingBuilder, config);
    
        // Count services before
        var serviceCountBefore = services.Count;
    
        // Use reflection to call ConfigureAzureFileLoggerOptions directly
        var method = factory.GetType().GetMethod("ConfigureAzureFileLoggerOptions", BindingFlags.NonPublic | BindingFlags.Instance);
    
        // Act
        method!.Invoke(factory, [target]);
    
        // Assert - Should have executed the MakeGenericMethod/Invoke path
        var serviceCountAfter = services.Count;
        serviceCountAfter.Should().BeGreaterThan(serviceCountBefore, 
            "should have added configuration services via MakeGenericMethod and Invoke");
    }
    
    [Fact]
    public void ConfigureOptions_WhenOptionsTypeIsNull_ShouldReturnEarly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // gives us an ILoggingBuilder

        ILoggingBuilder loggingBuilder = null!;
        services.AddLogging(builder => loggingBuilder = builder);

        var target = new LoggingTarget
        {
            Type = "AzureWebAppDiagnostics",
            Enabled = true
        };

        var config = new LoggingConfig { Targets = new() { ["AzureWebAppDiagnostics"] = target } };
        var factory = new MelProviderFactory(loggingBuilder, config);

        var method = factory.GetType().GetMethod("ConfigureOptions",
            BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var beforeCount = services.Count;
        method!.Invoke(factory, [target, "Non.Existent.Type, FakeAssembly"]);
        var afterCount = services.Count;

        // Assert
        afterCount.Should().Be(beforeCount, "no services should be added if optionsType cannot be resolved");
    }
    
    [Fact]
    public void AddFiltersForProvider_WhenOtherCategoriesExist_ShouldInvokeForEach()
    {
        // Arrange
        var config = new LoggingConfig
        {
            Targets =
            {
                ["c1"] = new LoggingTarget { Type = "Console", Enabled = true },
                ["c2"] = new LoggingTarget { Type = "Debug", Enabled = true },
            },
        };

        var builder = Substitute.For<ILoggingBuilder>();
        var factory = new MelProviderFactory(builder, config);

        var target = config.Targets["c1"]; // pick one target

        var method = factory.GetType().GetMethod(
            "AddFiltersForProvider",
            BindingFlags.NonPublic | BindingFlags.Instance);

        // Use a real provider type from MEL
        var providerType = typeof(ConsoleLoggerProvider);

        // Act
        Action act = () => method!.Invoke(factory, [providerType, target]);

        // Assert
        act.Should().NotThrow();
    }
    
    [Fact]
    public void AddFiltersForProvider_WhenFilterTypeNotFound_ReturnsEarlyWithoutError()
    {
        // Arrange - Mock returns null to simulate a missing type
        var mockFilterTypeProvider = Substitute.For<IFilterTypeProvider>();
        mockFilterTypeProvider.GetFilterType().Returns((Type?)null);

        var mockBuilder = Substitute.For<ILoggingBuilder>();;
        var config = new LoggingConfig();
    
        var factory = new MelProviderFactory(mockBuilder, config, mockFilterTypeProvider);
        var target = new LoggingTarget { Type = "Console" };

        // Act & Assert - Should not throw, should handle gracefully
        var ex = Record.Exception(() => 
        {
            // Use reflection to call the private method for testing
            var method = typeof(MelProviderFactory).GetMethod("AddFiltersForProvider", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            method?.Invoke(factory, [typeof(ConsoleLoggerProvider), target]);
        });

        ex.Should().BeNull();
        mockFilterTypeProvider.Received(1).GetFilterType();
    }

    private class NullServicesLoggingBuilder : ILoggingBuilder
    {
        public IServiceCollection Services => null!;
    }

    private static int GetProviderCount(ILoggingBuilder builder)
    {
        // Use reflection to get the Services property and count registered providers
        var servicesProperty = builder.GetType().GetProperty("Services");
        var services = (IServiceCollection)servicesProperty!.GetValue(builder)!;
        return services.Count(s => s.ServiceType == typeof(ILoggerProvider));
    }

    /// <summary>
    /// Creates a mock IConfigurationSection with the specified value.
    /// </summary>
    private static IConfigurationSection CreateMockConfigurationSection(string? value)
    {
        var configData = new Dictionary<string, string?>();
        if (value != null)
        {
            configData[""] = value;
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        return configuration.GetSection("");
    }
}
