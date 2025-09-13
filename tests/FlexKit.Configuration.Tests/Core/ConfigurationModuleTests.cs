using System.Reflection;
using Autofac;
using Autofac.Core.Resolving.Pipeline;
using AutoFixture.Xunit2;
using FlexKit.Configuration.Core;
using FlexKit.Configuration.Tests.TestBase;
using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration.Memory;
using NSubstitute;
using Xunit;
// ReSharper disable NullableWarningSuppressionIsUsed

namespace FlexKit.Configuration.Tests.Core;

/// <summary>
/// Unit tests for ConfigurationModule covering property injection functionality.
/// Tests the actual behavior through Autofac container resolution rather than directly calling protected methods.
/// </summary>
public class ConfigurationModuleTests : UnitTestBase
{
    private readonly IFlexConfig _mockFlexConfig;

    public ConfigurationModuleTests()
    {
        _mockFlexConfig = CreateMock<IFlexConfig>();
    }

    [Fact]
    public void Module_WithServiceHavingFlexConfigurationProperty_InjectsProperty()
    {
        // Arrange
        var containerBuilder = new ContainerBuilder();
        containerBuilder.RegisterInstance(_mockFlexConfig).As<IFlexConfig>();
        containerBuilder.RegisterType<TestServiceWithFlexConfigProperty>().AsSelf();
        containerBuilder.RegisterModule<ConfigurationModule>();

        // Act
        using var container = containerBuilder.Build();
        var service = container.Resolve<TestServiceWithFlexConfigProperty>();

        // Assert
        service.FlexConfiguration.Should().NotBeNull();
        service.FlexConfiguration.Should().BeSameAs(_mockFlexConfig);
    }

    [Fact]
    public void Module_WithServiceHavingNoFlexConfigProperty_DoesNotThrow()
    {
        // Arrange
        var containerBuilder = new ContainerBuilder();
        containerBuilder.RegisterInstance(_mockFlexConfig).As<IFlexConfig>();
        containerBuilder.RegisterType<TestServiceWithoutFlexConfigProperty>().AsSelf();
        containerBuilder.RegisterModule<ConfigurationModule>();

        // Act & Assert - Should not throw
        using var container = containerBuilder.Build();
        var service = container.Resolve<TestServiceWithoutFlexConfigProperty>();

        service.Should().NotBeNull();
        service.SomeOtherProperty.Should().BeNull();
    }

    [Fact]
    public void Module_WithServiceHavingWrongPropertyType_DoesNotInject()
    {
        // Arrange
        var containerBuilder = new ContainerBuilder();
        containerBuilder.RegisterInstance(_mockFlexConfig).As<IFlexConfig>();
        containerBuilder.RegisterType<TestServiceWithWrongPropertyType>().AsSelf();
        containerBuilder.RegisterModule<ConfigurationModule>();

        // Act
        using var container = containerBuilder.Build();
        var service = container.Resolve<TestServiceWithWrongPropertyType>();

        // Assert
        service.FlexConfiguration.Should().BeNull(); // Wrong type, should not be injected
    }

    [Fact]
    public void Module_WithServiceHavingReadOnlyProperty_DoesNotInject()
    {
        // Arrange
        var containerBuilder = new ContainerBuilder();
        containerBuilder.RegisterInstance(_mockFlexConfig).As<IFlexConfig>();
        containerBuilder.RegisterType<TestServiceWithReadOnlyFlexConfig>().AsSelf();
        containerBuilder.RegisterModule<ConfigurationModule>();

        // Act
        using var container = containerBuilder.Build();
        var service = container.Resolve<TestServiceWithReadOnlyFlexConfig>();

        // Assert
        service.FlexConfiguration.Should().BeNull(); // Read-only property cannot be set
    }

    [Fact]
    public void Module_WhenFlexConfigNotRegistered_DoesNotThrow()
    {
        // Arrange - Don't register IFlexConfig
        var containerBuilder = new ContainerBuilder();
        containerBuilder.RegisterType<TestServiceWithFlexConfigProperty>().AsSelf();
        containerBuilder.RegisterModule<ConfigurationModule>();

        // Act & Assert - Should not throw even when IFlexConfig is not available
        using var container = containerBuilder.Build();
        var service = container.Resolve<TestServiceWithFlexConfigProperty>();

        service.Should().NotBeNull();
        service.FlexConfiguration.Should().BeNull(); // Cannot inject what's not registered
    }

    [Fact]
    public void Module_WithMultipleProperties_InjectsOnlyFlexConfigurationProperty()
    {
        // Arrange
        var containerBuilder = new ContainerBuilder();
        containerBuilder.RegisterInstance(_mockFlexConfig).As<IFlexConfig>();
        containerBuilder.RegisterType<TestServiceWithMultipleProperties>().AsSelf();
        containerBuilder.RegisterModule<ConfigurationModule>();

        // Act
        using var container = containerBuilder.Build();
        var service = container.Resolve<TestServiceWithMultipleProperties>();

        // Assert
        service.FlexConfiguration.Should().NotBeNull();
        service.FlexConfiguration.Should().BeSameAs(_mockFlexConfig);
        service.OtherProperty.Should().BeNull(); // Should not be injected
        service.ReadOnlyProperty.Should().BeNull(); // Should not be injected (read-only)
    }

    [Theory]
    [AutoData]
    public void Module_WithInheritedFlexConfigProperty_InjectsSuccessfully(string testValue)
    {
        // Arrange
        var containerBuilder = new ContainerBuilder();
        containerBuilder.RegisterInstance(_mockFlexConfig).As<IFlexConfig>();
        containerBuilder.RegisterType<DerivedServiceWithFlexConfig>().AsSelf();
        containerBuilder.RegisterModule<ConfigurationModule>();

        _mockFlexConfig["test:value"].Returns(testValue);

        // Act
        using var container = containerBuilder.Build();
        var service = container.Resolve<DerivedServiceWithFlexConfig>();

        // Assert
        service.FlexConfiguration.Should().NotBeNull();
        service.FlexConfiguration.Should().BeSameAs(_mockFlexConfig);
        service.GetConfigValue().Should().Be(testValue);
    }

    [Fact]
    public void Module_WithMultipleServicesWithFlexConfig_InjectsIntoAll()
    {
        // Arrange
        var containerBuilder = new ContainerBuilder();
        containerBuilder.RegisterInstance(_mockFlexConfig).As<IFlexConfig>();
        containerBuilder.RegisterType<TestServiceWithFlexConfigProperty>().AsSelf();
        containerBuilder.RegisterType<AnotherServiceWithFlexConfig>().AsSelf();
        containerBuilder.RegisterModule<ConfigurationModule>();

        // Act
        using var container = containerBuilder.Build();
        var service1 = container.Resolve<TestServiceWithFlexConfigProperty>();
        var service2 = container.Resolve<AnotherServiceWithFlexConfig>();

        // Assert
        service1.FlexConfiguration.Should().NotBeNull();
        service1.FlexConfiguration.Should().BeSameAs(_mockFlexConfig);

        service2.FlexConfiguration.Should().NotBeNull();
        service2.FlexConfiguration.Should().BeSameAs(_mockFlexConfig);
    }

    [Fact]
    public void Module_WithComplexServiceGraph_InjectsIntoAllLevels()
    {
        // Arrange
        var testData = ConfigurationTestDataBuilder.CreateConfigurationDictionary();
        var containerBuilder = new ContainerBuilder();

        containerBuilder.AddFlexConfig(config => config
            .AddSource(new MemoryConfigurationSource { InitialData = testData! }));

        containerBuilder.RegisterType<ServiceWithDependencyAndFlexConfig>().AsSelf();
        containerBuilder.RegisterType<TestServiceWithFlexConfigProperty>().AsSelf();

        // Act
        using var container = containerBuilder.Build();
        var service = container.Resolve<ServiceWithDependencyAndFlexConfig>();

        // Assert
        service.FlexConfiguration.Should().NotBeNull();
        service.FlexConfiguration.Should().BeOfType<FlexConfiguration>();

        service.Dependency.Should().NotBeNull();
        service.Dependency.FlexConfiguration.Should().NotBeNull();
        service.Dependency.FlexConfiguration.Should().BeOfType<FlexConfiguration>();

        // Both services should have the same IFlexConfig instance
        service.FlexConfiguration.Should().BeSameAs(service.Dependency.FlexConfiguration);
    }

    [Fact]
    public void Module_WithRealFlexConfiguration_InjectsWorkingInstance()
    {
        // Arrange
        var testData = ConfigurationTestDataBuilder.CreateConfigurationDictionary();
        var containerBuilder = new ContainerBuilder();

        containerBuilder.AddFlexConfig(config => config
            .AddSource(new MemoryConfigurationSource { InitialData = testData! }));

        containerBuilder.RegisterType<TestServiceWithFlexConfigProperty>().AsSelf();

        // Act
        using var container = containerBuilder.Build();
        var service = container.Resolve<TestServiceWithFlexConfigProperty>();

        // Assert
        service.FlexConfiguration.Should().NotBeNull();
        service.FlexConfiguration.Should().BeOfType<FlexConfiguration>();
        service.FlexConfiguration["Application:Name"].Should().Be(testData["Application:Name"]);
    }

    [Fact]
    public void Module_DoesNotProcessIFlexConfigRegistrationItself()
    {
        // Arrange - Register IFlexConfig and the module
        var containerBuilder = new ContainerBuilder();
        containerBuilder.RegisterInstance(_mockFlexConfig).As<IFlexConfig>();
        containerBuilder.RegisterModule<ConfigurationModule>();

        // Act & Assert - Should build without issues (no infinite loops or errors)
        using var container = containerBuilder.Build();
        var flexConfig = container.Resolve<IFlexConfig>();

        flexConfig.Should().BeSameAs(_mockFlexConfig);
    }

    [Fact]
    public void Module_WithDelegateRegistration_DoesNotInject()
    {
        // Arrange - Use delegate registration (not ReflectionActivator)
        var containerBuilder = new ContainerBuilder();
        containerBuilder.RegisterInstance(_mockFlexConfig).As<IFlexConfig>();
        containerBuilder.Register(_ => new TestServiceWithFlexConfigProperty()).AsSelf();
        containerBuilder.RegisterModule<ConfigurationModule>();

        // Act
        using var container = containerBuilder.Build();
        var service = container.Resolve<TestServiceWithFlexConfigProperty>();

        // Assert - Property injection only works with ReflectionActivator
        service.FlexConfiguration.Should().BeNull();
    }

    [Fact]
    public void Module_WithInstanceRegistration_DoesNotInject()
    {
        // Arrange - Use instance registration (not ReflectionActivator)
        var containerBuilder = new ContainerBuilder();
        containerBuilder.RegisterInstance(_mockFlexConfig).As<IFlexConfig>();

        var serviceInstance = new TestServiceWithFlexConfigProperty();
        containerBuilder.RegisterInstance(serviceInstance).AsSelf();
        containerBuilder.RegisterModule<ConfigurationModule>();

        // Act
        using var container = containerBuilder.Build();
        var service = container.Resolve<TestServiceWithFlexConfigProperty>();

        // Assert - Property injection only works with ReflectionActivator
        service.FlexConfiguration.Should().BeNull();
        service.Should().BeSameAs(serviceInstance);
    }

    [Fact]
    public void Module_Performance_HandlesMultipleResolutions()
    {
        // Arrange
        var containerBuilder = new ContainerBuilder();
        containerBuilder.RegisterInstance(_mockFlexConfig).As<IFlexConfig>();
        containerBuilder.RegisterType<TestServiceWithFlexConfigProperty>().AsSelf();
        containerBuilder.RegisterModule<ConfigurationModule>();

        using var container = containerBuilder.Build();

        // Act & Assert - Multiple resolutions should work efficiently
        for (int i = 0; i < 100; i++)
        {
            var service = container.Resolve<TestServiceWithFlexConfigProperty>();
            service.FlexConfiguration.Should().BeSameAs(_mockFlexConfig);
        }
    }

    [Fact]
    public void Module_WithPropertyNamedDifferently_DoesNotInject()
    {
        // Arrange
        var containerBuilder = new ContainerBuilder();
        containerBuilder.RegisterInstance(_mockFlexConfig).As<IFlexConfig>();
        containerBuilder.RegisterType<TestServiceWithDifferentlyNamedProperty>().AsSelf();
        containerBuilder.RegisterModule<ConfigurationModule>();

        // Act
        using var container = containerBuilder.Build();
        var service = container.Resolve<TestServiceWithDifferentlyNamedProperty>();

        // Assert
        service.Config.Should().BeNull(); // Property is not named "FlexConfiguration"
    }

    [Fact]
    public void InjectFlexConfigurationProperties_WithNullInstance_ReturnsEarly()
    {
        // Arrange
        var moduleType = typeof(ConfigurationModule);
        var method = moduleType.GetMethod("InjectFlexConfigurationProperties",
            BindingFlags.NonPublic | BindingFlags.Static);

        var mockContext = CreateMock<ResolveRequestContext>();
        mockContext.Instance.Returns((object?)null);

        // Act & Assert - Should not throw
        var action = () => method?.Invoke(null, [mockContext]);
        action.Should().NotThrow();
    }
}

#region Test Helper Classes

/// <summary>
/// Test service with a FlexConfiguration property for injection testing.
/// </summary>
public class TestServiceWithFlexConfigProperty
{
    public IFlexConfig? FlexConfiguration { get; [UsedImplicitly] set; }
}

/// <summary>
/// Test service with multiple properties to verify selective injection.
/// </summary>
public class TestServiceWithMultipleProperties
{
    public IFlexConfig? FlexConfiguration { get; [UsedImplicitly] set; }
    public string? OtherProperty { get; [UsedImplicitly] set; }
    [UsedImplicitly] public object? ReadOnlyProperty { get; } // Read-only property
}

/// <summary>
/// Test service without FlexConfiguration property.
/// </summary>
public class TestServiceWithoutFlexConfigProperty
{
    public string? SomeOtherProperty { get; [UsedImplicitly] set; }
}

/// <summary>
/// Test service with FlexConfiguration property of a wrong type.
/// </summary>
public class TestServiceWithWrongPropertyType
{
    public string? FlexConfiguration { get; [UsedImplicitly] set; } // Wrong type - should be IFlexConfig
}

/// <summary>
/// Base class for testing inheritance scenarios.
/// </summary>
public abstract class BaseServiceWithFlexConfig
{
    public IFlexConfig? FlexConfiguration { get; [UsedImplicitly] set; }
}

/// <summary>
/// Derived service to test property injection with inheritance.
/// </summary>
public class DerivedServiceWithFlexConfig : BaseServiceWithFlexConfig
{
    public string? GetConfigValue()
    {
        return FlexConfiguration?["test:value"];
    }
}

/// <summary>
/// Another service with FlexConfiguration for multiple service testing.
/// </summary>
public class AnotherServiceWithFlexConfig
{
    public IFlexConfig? FlexConfiguration { get; [UsedImplicitly] set; }
}

/// <summary>
/// Service with dependency and FlexConfiguration for complex graph testing.
/// </summary>
public class ServiceWithDependencyAndFlexConfig(TestServiceWithFlexConfigProperty dependency)
{
    public TestServiceWithFlexConfigProperty Dependency { get; } = dependency;
    public IFlexConfig? FlexConfiguration { get; [UsedImplicitly] set; }
}

/// <summary>
/// Service with read-only FlexConfiguration property.
/// </summary>
public class TestServiceWithReadOnlyFlexConfig
{
    public IFlexConfig? FlexConfiguration { get; } = null; // Read-only property
}

/// <summary>
/// Service with differently named property to test name-specific injection.
/// </summary>
public class TestServiceWithDifferentlyNamedProperty
{
    public IFlexConfig? Config { get; [UsedImplicitly] set; } // Not named "FlexConfiguration"
}

#endregion