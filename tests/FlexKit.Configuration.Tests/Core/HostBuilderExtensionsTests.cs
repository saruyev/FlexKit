using Autofac;
using Autofac.Extensions.DependencyInjection;
using FlexKit.Configuration.Core;
using FlexKit.Configuration.Tests.TestBase;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Xunit;
// ReSharper disable ComplexConditionExpression
// ReSharper disable MethodTooLong
// ReSharper disable TooManyDeclarations
// ReSharper disable NullableWarningSuppressionIsUsed

namespace FlexKit.Configuration.Tests.Core;

/// <summary>
/// Unit tests for HostBuilderExtensions class covering all extension methods and scenarios.
/// Tests the integration of FlexKit.Configuration with Microsoft.Extensions.Hosting.
/// </summary>
public class HostBuilderExtensionsTests : UnitTestBase
{
    [Fact]
    public void AddFlexConfig_WithNullHostBuilder_ThrowsArgumentNullException()
    {
        // Arrange
        IHostBuilder? nullBuilder = null;

        // Act & Assert
        Func<IHostBuilder> action = () => nullBuilder!.AddFlexConfig();
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("hostBuilder");
    }

    [Fact]
    public void AddFlexConfig_WithoutConfigureAction_CallsCorrectMethods()
    {
        // Arrange
        var mockHostBuilder = Substitute.For<IHostBuilder>();
        mockHostBuilder.ConfigureServices(Arg.Any<Action<HostBuilderContext, IServiceCollection>>())
            .Returns(mockHostBuilder);
        mockHostBuilder.UseServiceProviderFactory(Arg.Any<AutofacServiceProviderFactory>())
            .Returns(mockHostBuilder);
        mockHostBuilder.ConfigureContainer(Arg.Any<Action<HostBuilderContext, ContainerBuilder>>())
            .Returns(mockHostBuilder);

        // Act
        var result = mockHostBuilder.AddFlexConfig();

        // Assert
        result.Should().BeSameAs(mockHostBuilder);
        
        // Verify all required methods were called
        mockHostBuilder.Received(1).ConfigureServices(Arg.Any<Action<HostBuilderContext, IServiceCollection>>());
        mockHostBuilder.Received(1).UseServiceProviderFactory(Arg.Any<AutofacServiceProviderFactory>());
        mockHostBuilder.Received(1).ConfigureContainer(Arg.Any<Action<HostBuilderContext, ContainerBuilder>>());
    }

    [Fact]
    public void AddFlexConfig_WithConfigureAction_CallsCorrectMethods()
    {
        // Arrange
        var mockHostBuilder = Substitute.For<IHostBuilder>();
        mockHostBuilder.ConfigureServices(Arg.Any<Action<HostBuilderContext, IServiceCollection>>())
            .Returns(mockHostBuilder);
        mockHostBuilder.UseServiceProviderFactory(Arg.Any<AutofacServiceProviderFactory>())
            .Returns(mockHostBuilder);
        mockHostBuilder.ConfigureContainer(Arg.Any<Action<HostBuilderContext, ContainerBuilder>>())
            .Returns(mockHostBuilder);

        // Act
        var result = mockHostBuilder.AddFlexConfig(config =>
        {
            config.AddEnvironmentVariables();
        });

        // Assert
        result.Should().BeSameAs(mockHostBuilder);
        
        // Verify all required methods were called
        mockHostBuilder.Received(1).ConfigureServices(Arg.Any<Action<HostBuilderContext, IServiceCollection>>());
        mockHostBuilder.Received(1).UseServiceProviderFactory(Arg.Any<AutofacServiceProviderFactory>());
        mockHostBuilder.Received(1).ConfigureContainer(Arg.Any<Action<HostBuilderContext, ContainerBuilder>>());
    }

    [Fact]
    public void AddFlexConfig_ConfigureServicesCallback_AddsAutofac()
    {
        // Arrange
        var mockHostBuilder = Substitute.For<IHostBuilder>();
        var capturedConfigureServices = (Action<HostBuilderContext, IServiceCollection>?)null;
        var serviceCollection = new ServiceCollection();
        var mockContext = new HostBuilderContext(new Dictionary<object, object>());
        
        mockHostBuilder.ConfigureServices(Arg.Do<Action<HostBuilderContext, IServiceCollection>>(action => capturedConfigureServices = action))
            .Returns(mockHostBuilder);
        mockHostBuilder.UseServiceProviderFactory(Arg.Any<AutofacServiceProviderFactory>())
            .Returns(mockHostBuilder);
        mockHostBuilder.ConfigureContainer(Arg.Any<Action<HostBuilderContext, ContainerBuilder>>())
            .Returns(mockHostBuilder);

        // Act
        mockHostBuilder.AddFlexConfig();
        
        // Execute the captured ConfigureServices action
        capturedConfigureServices.Should().NotBeNull();
        capturedConfigureServices!.Invoke(mockContext, serviceCollection);

        // Assert
        // Verify Autofac was added to services (it adds some marker services)
        serviceCollection.Should().NotBeEmpty();
    }

    [Fact]
    public void AddFlexConfig_ConfigureContainerCallback_ConfiguresFlexConfig()
    {
        // Arrange
        var mockHostBuilder = Substitute.For<IHostBuilder>();
        var containerBuilder = new ContainerBuilder();
        var capturedConfigureContainer = (Action<HostBuilderContext, ContainerBuilder>?)null;
        
        // Set up a minimal configuration
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["test"] = "value" })
            .Build();
        var mockContext = new HostBuilderContext(new Dictionary<object, object>())
        {
            Configuration = configuration
        };
        
        mockHostBuilder.ConfigureServices(Arg.Any<Action<HostBuilderContext, IServiceCollection>>())
            .Returns(mockHostBuilder);
        mockHostBuilder.UseServiceProviderFactory(Arg.Any<AutofacServiceProviderFactory>())
            .Returns(mockHostBuilder);
        mockHostBuilder.ConfigureContainer(Arg.Do<Action<HostBuilderContext, ContainerBuilder>>(action => capturedConfigureContainer = action))
            .Returns(mockHostBuilder);

        // Act
        mockHostBuilder.AddFlexConfig(config =>
        {
            config.AddEnvironmentVariables();
        });
        
        // Execute the captured ConfigureContainer action
        capturedConfigureContainer.Should().NotBeNull();
        capturedConfigureContainer!.Invoke(mockContext, containerBuilder);

        // Assert
        // Build the container to verify FlexConfig was registered
        using var container = containerBuilder.Build();
        var flexConfig = container.Resolve<IFlexConfig>();
        flexConfig.Should().NotBeNull();
        
        var resolvedConfiguration = container.Resolve<IConfiguration>();
        resolvedConfiguration.Should().NotBeNull();
    }

    [Fact]
    public void AddFlexConfig_WithNullConfigureAction_DoesNotThrow()
    {
        // Arrange
        var mockHostBuilder = Substitute.For<IHostBuilder>();
        mockHostBuilder.ConfigureServices(Arg.Any<Action<HostBuilderContext, IServiceCollection>>())
            .Returns(mockHostBuilder);
        mockHostBuilder.UseServiceProviderFactory(Arg.Any<AutofacServiceProviderFactory>())
            .Returns(mockHostBuilder);
        mockHostBuilder.ConfigureContainer(Arg.Any<Action<HostBuilderContext, ContainerBuilder>>())
            .Returns(mockHostBuilder);
        
        Action<FlexConfigurationBuilder>? nullConfigure = null;

        // Act & Assert
        var action = () => mockHostBuilder.AddFlexConfig(nullConfigure);
        action.Should().NotThrow();
        
        var result = action();
        result.Should().BeSameAs(mockHostBuilder);
    }

    [Fact]
    public void AddFlexConfig_CanBeChainedWithOtherMethods()
    {
        // Arrange
        var mockHostBuilder = Substitute.For<IHostBuilder>();
        mockHostBuilder.ConfigureServices(Arg.Any<Action<HostBuilderContext, IServiceCollection>>())
            .Returns(mockHostBuilder);
        mockHostBuilder.UseServiceProviderFactory(Arg.Any<AutofacServiceProviderFactory>())
            .Returns(mockHostBuilder);
        mockHostBuilder.ConfigureContainer(Arg.Any<Action<HostBuilderContext, ContainerBuilder>>())
            .Returns(mockHostBuilder);

        // Act
        var result = mockHostBuilder
            .AddFlexConfig(config =>
            {
                config.AddEnvironmentVariables();
            })
            .ConfigureServices((_, services) =>
            {
                services.AddSingleton<ITestService, TestService>();
            });

        // Assert
        result.Should().BeSameAs(mockHostBuilder);
        
        // Verify both AddFlexConfig and additional ConfigureServices were called
        mockHostBuilder.Received().ConfigureServices(Arg.Any<Action<HostBuilderContext, IServiceCollection>>());
    }

    [Fact]
    public void AddFlexConfig_PreservesExistingConfiguration()
    {
        // Arrange
        var mockHostBuilder = Substitute.For<IHostBuilder>();
        var containerBuilder = new ContainerBuilder();
        var capturedConfigureContainer = (Action<HostBuilderContext, ContainerBuilder>?)null;
        
        // Set up the existing configuration with test data
        var existingConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ExistingKey"] = "ExistingValue",
                ["App:Name"] = "TestApp"
            })
            .Build();
        var mockContext = new HostBuilderContext(new Dictionary<object, object>())
        {
            Configuration = existingConfiguration
        };
        
        SetupMockHostBuilderForContainer(mockHostBuilder, capturedAction => capturedConfigureContainer = capturedAction);

        // Act
        mockHostBuilder.AddFlexConfig();
        
        // Execute the captured ConfigureContainer action
        capturedConfigureContainer.Should().NotBeNull();
        capturedConfigureContainer!.Invoke(mockContext, containerBuilder);

        // Assert
        using var container = containerBuilder.Build();
        var flexConfig = container.Resolve<IFlexConfig>();
        
        // Verify the existing configuration is preserved
        flexConfig["ExistingKey"].Should().Be("ExistingValue");
        flexConfig["App:Name"].Should().Be("TestApp");
    }

    [Fact]
    public void AddFlexConfig_WithAdditionalSources_MaintainsPrecedence()
    {
        // Arrange
        var mockHostBuilder = Substitute.For<IHostBuilder>();
        var containerBuilder = new ContainerBuilder();
        var capturedConfigureContainer = (Action<HostBuilderContext, ContainerBuilder>?)null;
        
        // Set up the existing configuration
        var existingConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SharedKey"] = "FromHost",
                ["HostOnlyKey"] = "HostValue"
            })
            .Build();
        var mockContext = new HostBuilderContext(new Dictionary<object, object>())
        {
            Configuration = existingConfiguration
        };
        
        SetupMockHostBuilderForContainer(mockHostBuilder, capturedAction => capturedConfigureContainer = capturedAction);

        // Act
        mockHostBuilder.AddFlexConfig(config =>
        {
            // Add a higher priority source
            config.AddSource(new Microsoft.Extensions.Configuration.Memory.MemoryConfigurationSource
            {
                InitialData = new Dictionary<string, string?>
                {
                    ["SharedKey"] = "FromFlexKit",
                    ["FlexKitOnlyKey"] = "FlexKitValue"
                }
            });
        });
        
        // Execute the captured ConfigureContainer action
        capturedConfigureContainer.Should().NotBeNull();
        capturedConfigureContainer!.Invoke(mockContext, containerBuilder);

        // Assert
        using var container = containerBuilder.Build();
        var flexConfig = container.Resolve<IFlexConfig>();
        
        // Higher priority source should override shared keys
        flexConfig["SharedKey"].Should().Be("FromFlexKit");
        
        // Both sources should contribute their unique keys
        flexConfig["HostOnlyKey"].Should().Be("HostValue");
        flexConfig["FlexKitOnlyKey"].Should().Be("FlexKitValue");
    }

    [Fact]
    public void AddFlexConfig_RegistersDynamicConfigurationService()
    {
        // Arrange
        var mockHostBuilder = Substitute.For<IHostBuilder>();
        var containerBuilder = new ContainerBuilder();
        var capturedConfigureContainer = (Action<HostBuilderContext, ContainerBuilder>?)null;
        
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["test"] = "value" })
            .Build();
        var mockContext = new HostBuilderContext(new Dictionary<object, object>())
        {
            Configuration = configuration
        };
        
        SetupMockHostBuilderForContainer(mockHostBuilder, capturedAction => capturedConfigureContainer = capturedAction);

        // Act
        mockHostBuilder.AddFlexConfig();
        capturedConfigureContainer!.Invoke(mockContext, containerBuilder);

        // Assert
        using var container = containerBuilder.Build();
        
        // Verify both IFlexConfig and dynamic are registered as the same instance
        var flexConfig = container.Resolve<IFlexConfig>();
        var dynamicConfig = container.Resolve<dynamic>();
        
        flexConfig.Should().NotBeNull();
        ((object)dynamicConfig).Should().NotBeNull();
        ((object)dynamicConfig).Should().BeSameAs(flexConfig);
    }

    [Fact]
    public void AddFlexConfig_WithComplexConfigurationSources_IntegratesCorrectly()
    {
        // Arrange
        var mockHostBuilder = Substitute.For<IHostBuilder>();
        
        var containerBuilder = new ContainerBuilder();
        var capturedConfigureContainer = (Action<HostBuilderContext, ContainerBuilder>?)null;
        
        var existingConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:ConnectionString"] = "Server=test;Database=TestDb;",
                ["Logging:LogLevel:Default"] = "Information"
            })
            .Build();
        var mockContext = new HostBuilderContext(new Dictionary<object, object>())
        {
            Configuration = existingConfiguration
        };
        
        SetupMockHostBuilderForContainer(mockHostBuilder, capturedAction => capturedConfigureContainer = capturedAction);

        // Act
        mockHostBuilder.AddFlexConfig(config =>
        {
            config.AddEnvironmentVariables();
            config.AddSource(new Microsoft.Extensions.Configuration.Memory.MemoryConfigurationSource
            {
                InitialData = new Dictionary<string, string?>
                {
                    ["Api:Key"] = "test-api-key",
                    ["Features:EnableCaching"] = "true"
                }
            });
        });
        
        capturedConfigureContainer!.Invoke(mockContext, containerBuilder);

        // Assert
        using var container = containerBuilder.Build();
        var flexConfig = container.Resolve<IFlexConfig>();
        
        // Verify all configuration sources are accessible
        flexConfig["Database:ConnectionString"].Should().Be("Server=test;Database=TestDb;");
        flexConfig["Logging:LogLevel:Default"].Should().Be("Information");
        flexConfig["Api:Key"].Should().Be("test-api-key");
        flexConfig["Features:EnableCaching"].Should().Be("true");
        
        // Verify dynamic access works by accessing the underlying configuration
        dynamic dynamicConfig = flexConfig;
        var apiSection = dynamicConfig.Api;
        var apiKeyFromConfig = apiSection?.Configuration?["Key"];
        (apiKeyFromConfig as string).Should().Be("test-api-key");
    }

    #region Helper Methods

    private static void SetupMockHostBuilderForContainer(IHostBuilder mockHostBuilder, Action<Action<HostBuilderContext, ContainerBuilder>> captureAction)
    {
        mockHostBuilder.ConfigureServices(Arg.Any<Action<HostBuilderContext, IServiceCollection>>())
            .Returns(mockHostBuilder);
        
        mockHostBuilder.UseServiceProviderFactory(Arg.Any<AutofacServiceProviderFactory>())
            .Returns(mockHostBuilder);
        
        mockHostBuilder.ConfigureContainer(Arg.Do(captureAction))
            .Returns(mockHostBuilder);
    }

    #endregion

    #region Test Services

    private interface ITestService
    {
    }

    private class TestService : ITestService
    {
    }

    #endregion
}
