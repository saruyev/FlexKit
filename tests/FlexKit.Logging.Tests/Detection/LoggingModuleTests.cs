using Autofac;
using FlexKit.Logging.Configuration;
using FlexKit.Logging.Core;
using FlexKit.Logging.Detection;
using FlexKit.Logging.Formatting.Core;
using FlexKit.Logging.Formatting.Formatters;
using FlexKit.Logging.Interception;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;
// ReSharper disable TooManyDeclarations

namespace FlexKit.Logging.Tests.Detection;

/// <summary>
/// Tests for LoggingModule to verify proper registration of logging infrastructure components.
/// </summary>
public class LoggingModuleTests
{
    [Fact]
    public void LoadModule_RegistersAllExpectedTypes()
    {
        // Arrange
        var builder = new ContainerBuilder();
        
        // Register minimal dependencies
        builder.Register(_ => new ConfigurationBuilder().Build())
               .As<IConfiguration>()
               .SingleInstance();
        
        builder.RegisterModule<LoggingModule>();

        // Act
        var container = builder.Build();

        // Assert - Core infrastructure types
        container.IsRegistered<LoggingConfig>().Should().BeTrue();
        container.IsRegistered<InterceptionDecisionCache>().Should().BeTrue();
        container.IsRegistered<MethodLoggingInterceptor>().Should().BeTrue();
        container.IsRegistered<IFlexKitLogger>().Should().BeTrue();

        // Assert - Message formatting types
        container.IsRegistered<IMessageFormatterFactory>().Should().BeTrue();
        container.IsRegistered<IEnumerable<IMessageFormatter>>().Should().BeTrue();
        
        // Verify specific formatters are registered
        var formatters = container.Resolve<IEnumerable<IMessageFormatter>>().ToList();
        formatters.Should().Contain(f => f is CustomTemplateFormatter);
        formatters.Should().Contain(f => f is HybridFormatter);
        formatters.Should().Contain(f => f is JsonFormatter);
        formatters.Should().Contain(f => f is StandardStructuredFormatter);
        formatters.Should().Contain(f => f is SuccessErrorFormatter);

        container.IsRegistered<IBackgroundLog>().Should().BeTrue();
        container.IsRegistered<ILogEntryProcessor>().Should().BeTrue();
        container.IsRegistered<ILoggerFactory>().Should().BeTrue();
    }

    [Fact]
    public void LoadModule_ConfiguresInterceptionDecisionCache_WithCandidateTypes()
    {
        // Arrange
        var builder = new ContainerBuilder();
        
        builder.Register(_ => new ConfigurationBuilder().Build())
               .As<IConfiguration>()
               .SingleInstance();
        
        builder.RegisterModule<LoggingModule>();

        // Act
        var container = builder.Build();
        var cache = container.Resolve<InterceptionDecisionCache>();

        // Assert
        cache.Should().NotBeNull();
        cache.Config.Should().NotBeNull();
    }

    [Fact]
    public void LoadModule_RegistersLoggingConfigFromConfiguration()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            {"FlexKit:Logging:AutoIntercept", "false"},
            {"FlexKit:Logging:ActivitySourceName", "TestActivity"}
        };
        
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var builder = new ContainerBuilder();
        builder.RegisterInstance(configuration).As<IConfiguration>();
        builder.RegisterModule<LoggingModule>();

        // Act
        var container = builder.Build();
        var loggingConfig = container.Resolve<LoggingConfig>();

        // Assert
        loggingConfig.Should().NotBeNull();
        loggingConfig.AutoIntercept.Should().BeFalse();
        loggingConfig.ActivitySourceName.Should().Be("TestActivity");
    }

    [Fact]
    public void LoadModule_UsesDefaultConfiguration_WhenNoConfigurationSection()
    {
        // Arrange
        var builder = new ContainerBuilder();
        
        builder.Register(_ => new ConfigurationBuilder().Build())
               .As<IConfiguration>()
               .SingleInstance();
        
        builder.RegisterModule<LoggingModule>();

        // Act
        var container = builder.Build();
        var loggingConfig = container.Resolve<LoggingConfig>();

        // Assert
        loggingConfig.Should().NotBeNull();
        loggingConfig.AutoIntercept.Should().BeTrue(); // Default value
    }

    [Fact]
    public void LoadModule_RegistersComponentsWithCorrectLifetimes()
    {
        // Arrange
        var builder = new ContainerBuilder();
        
        builder.Register(_ => new ConfigurationBuilder().Build())
               .As<IConfiguration>()
               .SingleInstance();
        
        builder.RegisterModule<LoggingModule>();

        // Act
        var container = builder.Build();

        // Assert - Singleton components
        var config1 = container.Resolve<LoggingConfig>();
        var config2 = container.Resolve<LoggingConfig>();
        config1.Should().BeSameAs(config2);

        var cache1 = container.Resolve<InterceptionDecisionCache>();
        var cache2 = container.Resolve<InterceptionDecisionCache>();
        cache1.Should().BeSameAs(cache2);

        var factory1 = container.Resolve<IMessageFormatterFactory>();
        var factory2 = container.Resolve<IMessageFormatterFactory>();
        factory1.Should().BeSameAs(factory2);

        // Assert - Instance per lifetime scope components
        var interceptor1 = container.Resolve<MethodLoggingInterceptor>();
        var interceptor2 = container.Resolve<MethodLoggingInterceptor>();
        interceptor1.Should().BeSameAs(interceptor2); // Same scope

        using var scope = container.BeginLifetimeScope();
        var scopedInterceptor = scope.Resolve<MethodLoggingInterceptor>();
        scopedInterceptor.Should().NotBeSameAs(interceptor1); // Different scope
    }
}