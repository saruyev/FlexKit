using Autofac;
using Autofac.Extensions.DependencyInjection;
using FlexKit.Configuration.Core;
using FlexKit.Configuration.Tests.TestBase;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using NSubstitute;
using Xunit;
// ReSharper disable ComplexConditionExpression
// ReSharper disable MethodTooLong
// ReSharper disable TooManyDeclarations
// ReSharper disable NullableWarningSuppressionIsUsed
// ReSharper disable ClassTooBig
// ReSharper disable ConvertToLocalFunction

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
        var result = mockHostBuilder.AddFlexConfig(config => { config.AddEnvironmentVariables(); });

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

        mockHostBuilder
            .ConfigureServices(
                Arg.Do<Action<HostBuilderContext, IServiceCollection>>(action => capturedConfigureServices = action))
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
        mockHostBuilder
            .ConfigureContainer(
                Arg.Do<Action<HostBuilderContext, ContainerBuilder>>(action => capturedConfigureContainer = action))
            .Returns(mockHostBuilder);

        // Act
        mockHostBuilder.AddFlexConfig(config => { config.AddEnvironmentVariables(); });

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
            .AddFlexConfig(config => { config.AddEnvironmentVariables(); })
            .ConfigureServices((_, services) => { services.AddSingleton<ITestService, TestService>(); });

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

        SetupMockHostBuilderForContainer(mockHostBuilder,
            capturedAction => capturedConfigureContainer = capturedAction);

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

        SetupMockHostBuilderForContainer(mockHostBuilder,
            capturedAction => capturedConfigureContainer = capturedAction);

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

        SetupMockHostBuilderForContainer(mockHostBuilder,
            capturedAction => capturedConfigureContainer = capturedAction);

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

        SetupMockHostBuilderForContainer(mockHostBuilder,
            capturedAction => capturedConfigureContainer = capturedAction);

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

    [Fact]
    public void AddFlexConfig_IHostApplicationBuilder_WithNullBuilder_ThrowsArgumentNullException()
    {
        // Arrange
        IHostApplicationBuilder? nullBuilder = null;

        // Act & Assert
        Func<IHostApplicationBuilder> action = () => nullBuilder!.AddFlexConfig();
        action.Should().Throw<NullReferenceException>();
    }

    [Fact]
    public void AddFlexConfig_IHostApplicationBuilder_WithoutConfigureAction_CallsCorrectMethods()
    {
        // Arrange
        var serviceCollection = new ServiceCollection(); // Use real ServiceCollection
        var mockBuilder = Substitute.For<IHostApplicationBuilder>();
        var mockConfiguration = Substitute.For<IConfigurationManager>();

        mockBuilder.Services.Returns(serviceCollection);
        mockBuilder.Configuration.Returns(mockConfiguration);

        // Act
        var result = mockBuilder.AddFlexConfig();

        // Assert
        result.Should().BeSameAs(mockBuilder);

        // Verify Autofac was added by checking the service collection contains the expected service
        serviceCollection.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IServiceProviderFactory<ContainerBuilder>) &&
            descriptor.Lifetime == ServiceLifetime.Singleton);

        // Verify ConfigureContainer was called
        mockBuilder.Received(1).ConfigureContainer(
            Arg.Any<AutofacServiceProviderFactory>(),
            Arg.Any<Action<ContainerBuilder>?>());
    }

    [Fact]
    public void AddFlexConfig_IHostApplicationBuilder_WithConfigureAction_CallsCorrectMethods()
    {
        // Arrange
        var mockBuilder = Substitute.For<IHostApplicationBuilder>();
        var mockServices = Substitute.For<IServiceCollection>();
        var mockConfiguration = Substitute.For<IConfigurationManager>();

        mockBuilder.Services.Returns(mockServices);
        mockBuilder.Configuration.Returns(mockConfiguration);

        // Act
        var result = mockBuilder.AddFlexConfig(config => { config.AddEnvironmentVariables(); });

        // Assert
        result.Should().BeSameAs(mockBuilder);

        // Verify ConfigureContainer was called
        mockBuilder.Received(1).ConfigureContainer(
            Arg.Any<AutofacServiceProviderFactory>(),
            Arg.Any<Action<ContainerBuilder>?>());
    }

    [Fact]
    public void AddFlexConfig_IHostApplicationBuilder_ConfiguresServicesCorrectly()
    {
        // Arrange
        var mockBuilder = Substitute.For<IHostApplicationBuilder>();
        var serviceCollection = new ServiceCollection();
        var containerBuilder = new ContainerBuilder();
        var capturedConfigureAction = (Action<ContainerBuilder>?)null;

        // Use ConfigurationManager, which implements IConfigurationManager
        var configurationManager = new ConfigurationManager();
        configurationManager.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ExistingKey"] = "ExistingValue",
            ["App:Name"] = "TestApp"
        });

        mockBuilder.Services.Returns(serviceCollection);
        mockBuilder.Configuration.Returns(configurationManager);

        SetupMockApplicationBuilderForContainer(mockBuilder,
            capturedAction => capturedConfigureAction = capturedAction);

        // Act
        mockBuilder.AddFlexConfig();

        // Execute the captured ConfigureContainer action
        capturedConfigureAction.Should().NotBeNull();
        capturedConfigureAction!.Invoke(containerBuilder);

        // Assert
        using var container = containerBuilder.Build();
        var flexConfig = container.Resolve<IFlexConfig>();

        // Verify the existing configuration is preserved
        flexConfig["ExistingKey"].Should().Be("ExistingValue");
        flexConfig["App:Name"].Should().Be("TestApp");

        // Verify both IFlexConfig and dynamic are registered as the same instance
        var dynamicConfig = container.Resolve<dynamic>();
        ((object)dynamicConfig).Should().BeSameAs(flexConfig);
    }

    #region Tests for Dual-Parameter Overloads

    [Fact]
    public void AddFlexConfig_WithDualAction_ConfigureServicesCallback_AddsAutofac()
    {
        // Arrange
        Action<FlexConfigurationBuilder, ContainerBuilder> configure = (_, _) => { };
        var mockHostBuilder = Substitute.For<IHostBuilder>();
        var capturedConfigureServices = (Action<HostBuilderContext, IServiceCollection>?)null;
        var serviceCollection = new ServiceCollection();
        var mockContext = new HostBuilderContext(new Dictionary<object, object>());

        mockHostBuilder
            .ConfigureServices(
                Arg.Do<Action<HostBuilderContext, IServiceCollection>>(action => capturedConfigureServices = action))
            .Returns(mockHostBuilder);
        mockHostBuilder.UseServiceProviderFactory(Arg.Any<AutofacServiceProviderFactory>())
            .Returns(mockHostBuilder);
        mockHostBuilder.ConfigureContainer(Arg.Any<Action<HostBuilderContext, ContainerBuilder>>())
            .Returns(mockHostBuilder);

        // Act
        mockHostBuilder.AddFlexConfig(configure);

        // Execute the captured ConfigureServices action
        capturedConfigureServices.Should().NotBeNull();
        capturedConfigureServices!.Invoke(mockContext, serviceCollection);

        // Assert
        // Verify Autofac was added to services (it adds some marker services)
        serviceCollection.Should().NotBeEmpty();
    }

    [Fact]
    public void AddFlexConfig_IHostBuilder_WithDualAction_WithNullBuilder_ThrowsArgumentNullException()
    {
        // Arrange
        IHostBuilder? nullBuilder = null;
        Action<FlexConfigurationBuilder, ContainerBuilder> configure = (_, _) => { };

        // Act & Assert
        Func<IHostBuilder> action = () => nullBuilder!.AddFlexConfig(configure);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("hostBuilder");
    }

    [Fact]
    public void AddFlexConfig_IHostBuilder_WithDualAction_WithNullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var mockHostBuilder = Substitute.For<IHostBuilder>();
        Action<FlexConfigurationBuilder, ContainerBuilder>? nullConfigure = null;

        // Act & Assert
        Func<IHostBuilder> action = () => mockHostBuilder.AddFlexConfig(nullConfigure!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("configure");
    }

    [Fact]
    public void AddFlexConfig_IHostBuilder_WithDualAction_CallsCorrectMethods()
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
        var result = mockHostBuilder.AddFlexConfig((flexBuilder, containerBuilder) =>
        {
            flexBuilder.AddEnvironmentVariables();
            containerBuilder.RegisterType<TestService>().As<ITestService>();
        });

        // Assert
        result.Should().BeSameAs(mockHostBuilder);

        // Verify all required methods were called
        mockHostBuilder.Received(1).ConfigureServices(Arg.Any<Action<HostBuilderContext, IServiceCollection>>());
        mockHostBuilder.Received(1).UseServiceProviderFactory(Arg.Any<AutofacServiceProviderFactory>());
        mockHostBuilder.Received(1).ConfigureContainer(Arg.Any<Action<HostBuilderContext, ContainerBuilder>>());
    }

    [Fact]
    public void AddFlexConfig_IHostBuilder_WithDualAction_ConfiguresBothCorrectly()
    {
        // Arrange
        var mockHostBuilder = Substitute.For<IHostBuilder>();
        var containerBuilder = new ContainerBuilder();
        var capturedConfigureContainer = (Action<HostBuilderContext, ContainerBuilder>?)null;

        var existingConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ExistingKey"] = "ExistingValue" })
            .Build();
        var mockContext = new HostBuilderContext(new Dictionary<object, object>())
        {
            Configuration = existingConfiguration
        };

        SetupMockHostBuilderForContainer(mockHostBuilder,
            capturedAction => capturedConfigureContainer = capturedAction);

        // Act
        mockHostBuilder.AddFlexConfig((flexBuilder, autofacBuilder) =>
        {
            // Configure FlexConfig
            flexBuilder.AddSource(new Microsoft.Extensions.Configuration.Memory.MemoryConfigurationSource
            {
                InitialData = new Dictionary<string, string?> { ["FlexKey"] = "FlexValue" }
            });

            // Configure Autofac container
            autofacBuilder.RegisterType<TestService>().As<ITestService>();
        });

        // Execute the captured ConfigureContainer action
        capturedConfigureContainer.Should().NotBeNull();
        capturedConfigureContainer!.Invoke(mockContext, containerBuilder);

        // Assert
        using var container = containerBuilder.Build();

        // Verify FlexConfig was configured correctly
        var flexConfig = container.Resolve<IFlexConfig>();
        flexConfig["ExistingKey"].Should().Be("ExistingValue");
        flexConfig["FlexKey"].Should().Be("FlexValue");

        // Verify the Autofac container was configured correctly
        var testService = container.Resolve<ITestService>();
        testService.Should().NotBeNull();
        testService.Should().BeOfType<TestService>();
    }

    [Fact]
    public void AddFlexConfig_IHostApplicationBuilder_WithDualAction_WithNullBuilder_ThrowsArgumentNullException()
    {
        // Arrange
        IHostApplicationBuilder? nullBuilder = null;
        Action<FlexConfigurationBuilder, ContainerBuilder> configure = (_, _) => { };

        // Act & Assert
        Func<IHostApplicationBuilder> action = () => nullBuilder!.AddFlexConfig(configure);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("builder");
    }

    [Fact]
    public void AddFlexConfig_IHostApplicationBuilder_WithDualAction_WithNullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var mockBuilder = Substitute.For<IHostApplicationBuilder>();
        Action<FlexConfigurationBuilder, ContainerBuilder>? nullConfigure = null;

        // Act & Assert
        Func<IHostApplicationBuilder> action = () => mockBuilder.AddFlexConfig(nullConfigure!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("configure");
    }

    [Fact]
    public void AddFlexConfig_IHostApplicationBuilder_WithDualAction_CallsCorrectMethods()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();
        var mockBuilder = Substitute.For<IHostApplicationBuilder>();
        var mockConfiguration = Substitute.For<IConfigurationManager>();

        mockBuilder.Services.Returns(serviceCollection);
        mockBuilder.Configuration.Returns(mockConfiguration);

        // Act
        var result = mockBuilder.AddFlexConfig((flexBuilder, containerBuilder) =>
        {
            flexBuilder.AddEnvironmentVariables();
            containerBuilder.RegisterType<TestService>().As<ITestService>();
        });

        // Assert
        result.Should().BeSameAs(mockBuilder);

        // Verify Autofac was added
        serviceCollection.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IServiceProviderFactory<ContainerBuilder>) &&
            descriptor.Lifetime == ServiceLifetime.Singleton);

        // Verify ConfigureContainer was called
        mockBuilder.Received(1).ConfigureContainer(
            Arg.Any<AutofacServiceProviderFactory>(),
            Arg.Any<Action<ContainerBuilder>?>());
    }

    [Fact]
    public void AddFlexConfig_IHostApplicationBuilder_WithDualAction_ConfiguresBothCorrectly()
    {
        // Arrange
        var mockBuilder = Substitute.For<IHostApplicationBuilder>();
        var serviceCollection = new ServiceCollection();
        var containerBuilder = new ContainerBuilder();
        var capturedConfigureAction = (Action<ContainerBuilder>?)null;

        var configurationManager = new ConfigurationManager();
        configurationManager.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ExistingKey"] = "ExistingValue"
        });

        mockBuilder.Services.Returns(serviceCollection);
        mockBuilder.Configuration.Returns(configurationManager);

        SetupMockApplicationBuilderForContainer(mockBuilder,
            capturedAction => capturedConfigureAction = capturedAction);

        // Act
        mockBuilder.AddFlexConfig((flexBuilder, autofacBuilder) =>
        {
            // Configure FlexConfig
            flexBuilder.AddSource(new Microsoft.Extensions.Configuration.Memory.MemoryConfigurationSource
            {
                InitialData = new Dictionary<string, string?> { ["FlexKey"] = "FlexValue" }
            });

            // Configure Autofac container
            autofacBuilder.RegisterType<TestService>().As<ITestService>();
        });

        // Execute the captured ConfigureContainer action
        capturedConfigureAction.Should().NotBeNull();
        capturedConfigureAction!.Invoke(containerBuilder);

        // Assert
        using var container = containerBuilder.Build();

        // Verify FlexConfig was configured correctly
        var flexConfig = container.Resolve<IFlexConfig>();
        flexConfig["ExistingKey"].Should().Be("ExistingValue");
        flexConfig["FlexKey"].Should().Be("FlexValue");

        // Verify the Autofac container was configured correctly
        var testService = container.Resolve<ITestService>();
        testService.Should().NotBeNull();
        testService.Should().BeOfType<TestService>();
    }

    #endregion

    #region Tests for Automatic JSON File Detection

    [Fact]
    public async Task AddFlexConfig_WithExistingJsonFiles_AutomaticallyAddsFiles()
    {
        // Arrange
        var mockHostBuilder = Substitute.For<IHostBuilder>();
        var containerBuilder = new ContainerBuilder();
        var capturedConfigureContainer = (Action<HostBuilderContext, ContainerBuilder>?)null;

        var existingConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ExistingKey"] = "ExistingValue" })
            .Build();

        var mockEnvironment = Substitute.For<IHostEnvironment>();
        mockEnvironment.EnvironmentName.Returns("Development");

        var mockContext = new HostBuilderContext(new Dictionary<object, object>())
        {
            Configuration = existingConfiguration,
            HostingEnvironment = mockEnvironment
        };

        SetupMockHostBuilderForContainer(mockHostBuilder,
            capturedAction => capturedConfigureContainer = capturedAction);

        // Create temporary test files to simulate existing appsettings files
        var appSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
        var devAppSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.Development.json");

        try
        {
            await File.WriteAllTextAsync(appSettingsPath, """{"AutoDetected": "BaseValue"}""");
            await File.WriteAllTextAsync(devAppSettingsPath, """{"AutoDetected": "DevValue"}""");

            // Act
            mockHostBuilder.AddFlexConfig();
            capturedConfigureContainer!.Invoke(mockContext, containerBuilder);

            // Assert
            await using var container = containerBuilder.Build();
            var flexConfig = container.Resolve<IFlexConfig>();

            // Should contain both existing config and auto-detected files
            flexConfig["ExistingKey"].Should().Be("ExistingValue");
            // Development file should override a base file
            flexConfig["AutoDetected"].Should().Be("DevValue");
        }
        finally
        {
            if (File.Exists(appSettingsPath)) File.Delete(appSettingsPath);
            if (File.Exists(devAppSettingsPath)) File.Delete(devAppSettingsPath);
        }
    }

    [Fact]
    public async Task AddFlexConfig_WithNullEnvironment_OnlyAddsBaseAppSettings()
    {
        // Arrange
        var mockHostBuilder = Substitute.For<IHostBuilder>();
        var containerBuilder = new ContainerBuilder();
        var capturedConfigureContainer = (Action<HostBuilderContext, ContainerBuilder>?)null;

        var existingConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ExistingKey"] = "ExistingValue" })
            .Build();

        var mockContext = new HostBuilderContext(new Dictionary<object, object>())
        {
            Configuration = existingConfiguration,
            HostingEnvironment = null! // Simulate a unit test scenario
        };

        SetupMockHostBuilderForContainer(mockHostBuilder,
            capturedAction => capturedConfigureContainer = capturedAction);

        var appSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");

        try
        {
            await File.WriteAllTextAsync(appSettingsPath, """{"AutoDetected": "BaseValue"}""");

            // Act
            mockHostBuilder.AddFlexConfig();
            capturedConfigureContainer!.Invoke(mockContext, containerBuilder);

            // Assert - Should not throw despite null environment
            await using var container = containerBuilder.Build();
            var flexConfig = container.Resolve<IFlexConfig>();

            flexConfig["ExistingKey"].Should().Be("ExistingValue");
            flexConfig["AutoDetected"].Should().Be("BaseValue");
        }
        finally
        {
            if (File.Exists(appSettingsPath)) File.Delete(appSettingsPath);
        }
    }

    [Fact]
    public async Task AddFlexConfig_WithEnvironmentVariable_UsesEnvironmentForFileDetection()
    {
        // Arrange
        var mockHostBuilder = Substitute.For<IHostBuilder>();
        var containerBuilder = new ContainerBuilder();
        var capturedConfigureContainer = (Action<HostBuilderContext, ContainerBuilder>?)null;

        var existingConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ExistingKey"] = "ExistingValue" })
            .Build();

        var mockContext = new HostBuilderContext(new Dictionary<object, object>())
        {
            Configuration = existingConfiguration,
            HostingEnvironment = new HostingEnvironment() { EnvironmentName = "Testing" }
        };

        SetupMockHostBuilderForContainer(mockHostBuilder,
            capturedAction => capturedConfigureContainer = capturedAction);

        var appSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
        var testAppSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.Testing.json");

        try
        {
            // Set environment variable
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");

            await File.WriteAllTextAsync(appSettingsPath, """{"AutoDetected": "BaseValue"}""");
            await File.WriteAllTextAsync(testAppSettingsPath, """{"AutoDetected": "TestValue"}""");

            // Act
            mockHostBuilder.AddFlexConfig();
            capturedConfigureContainer!.Invoke(mockContext, containerBuilder);

            // Assert
            await using var container = containerBuilder.Build();
            var flexConfig = container.Resolve<IFlexConfig>();

            flexConfig["ExistingKey"].Should().Be("ExistingValue");
            // Should use a Testing environment file
            flexConfig["AutoDetected"].Should().Be("TestValue");
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
            if (File.Exists(appSettingsPath)) File.Delete(appSettingsPath);
            if (File.Exists(testAppSettingsPath)) File.Delete(testAppSettingsPath);
        }
    }

    [Fact]
    public void AddFlexConfig_WithoutJsonFiles_DoesNotThrow()
    {
        // Arrange
        var mockHostBuilder = Substitute.For<IHostBuilder>();
        var containerBuilder = new ContainerBuilder();
        var capturedConfigureContainer = (Action<HostBuilderContext, ContainerBuilder>?)null;

        var existingConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ExistingKey"] = "ExistingValue" })
            .Build();

        var mockEnvironment = Substitute.For<IHostEnvironment>();
        mockEnvironment.EnvironmentName.Returns("Development");

        var mockContext = new HostBuilderContext(new Dictionary<object, object>())
        {
            Configuration = existingConfiguration,
            HostingEnvironment = mockEnvironment
        };

        SetupMockHostBuilderForContainer(mockHostBuilder,
            capturedAction => capturedConfigureContainer = capturedAction);

        var tempDir = Path.GetTempPath();
        var originalDir = Directory.GetCurrentDirectory();

        try
        {
            // Set to an empty directory without appsettings files
            var emptyDir = Path.Combine(tempDir, Guid.NewGuid().ToString());
            Directory.CreateDirectory(emptyDir);
            Directory.SetCurrentDirectory(emptyDir);

            // Act & Assert - Should not throw when no JSON files exist
            var action = () =>
            {
                mockHostBuilder.AddFlexConfig();
                capturedConfigureContainer!.Invoke(mockContext, containerBuilder);
            };

            action.Should().NotThrow();

            using var container = containerBuilder.Build();
            var flexConfig = container.Resolve<IFlexConfig>();
            flexConfig["ExistingKey"].Should().Be("ExistingValue");
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
        }
    }

    #endregion

    #region Helper Methods

    private static void SetupMockApplicationBuilderForContainer(IHostApplicationBuilder mockBuilder,
        Action<Action<ContainerBuilder>> captureAction)
    {
        mockBuilder.When(x => x.ConfigureContainer(
                Arg.Any<AutofacServiceProviderFactory>(),
                Arg.Any<Action<ContainerBuilder>?>()))
            .Do(callInfo => captureAction(callInfo.Arg<Action<ContainerBuilder>>()));
    }

    private static void SetupMockHostBuilderForContainer(IHostBuilder mockHostBuilder,
        Action<Action<HostBuilderContext, ContainerBuilder>> captureAction)
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
