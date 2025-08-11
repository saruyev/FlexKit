using Autofac;
using AutoFixture.Xunit2;
using FlexKit.Configuration.Core;
using FlexKit.Configuration.Tests.TestBase;
using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Xunit;
// ReSharper disable TooManyDeclarations
// ReSharper disable NullableWarningSuppressionIsUsed

namespace FlexKit.Configuration.Tests.Core;

/// <summary>
/// Unit tests for ConfigurationExtensions class covering all extension methods.
/// </summary>
public class ConfigurationExtensionsTests : UnitTestBase
{
    protected override void RegisterFixtureCustomizations()
    {
        // Customize string generation to avoid null values in configuration keys
        Fixture.Customize<string>(composer => composer.FromFactory(() => "test-string-" + Guid.NewGuid().ToString("N")[..8]));
    }

    [Fact]
    public void AddFlexConfig_WithValidBuilder_RegistersAllComponents()
    {
        // Arrange
        var containerBuilder = new ContainerBuilder();
        var testData = ConfigurationTestDataBuilder.CreateConfigurationDictionary();

        // Act
        var result = containerBuilder.AddFlexConfig(config => config
            .AddSource(new MemoryConfigurationSource { InitialData = testData! }));

        // Assert
        result.Should().BeSameAs(containerBuilder);

        // Build container and verify registrations
        using var container = containerBuilder.Build();
   
        var configuration = container.Resolve<IConfiguration>();
        configuration.Should().NotBeNull();
   
        var flexConfig = container.Resolve<IFlexConfig>();
        flexConfig.Should().NotBeNull();
    }

    [Fact]
    public void AddFlexConfig_WithNullBuilder_ThrowsArgumentNullException()
    {
        // Arrange
        ContainerBuilder? nullBuilder = null;

        // Act & Assert
        var action = () => nullBuilder!.AddFlexConfig(config => config.AddEnvironmentVariables());
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddFlexConfig_WithNullConfigureAction_ThrowsNullReferenceException()
    {
        // Arrange
        var containerBuilder = new ContainerBuilder();

        // Act & Assert
        var action = () => containerBuilder.AddFlexConfig(null!);
        action.Should().Throw<NullReferenceException>();
    }

    [Fact]
    public void AddFlexConfig_WithComplexConfiguration_WorksCorrectly()
    {
        // Arrange
        var containerBuilder = new ContainerBuilder();
        var testData = ConfigurationTestDataBuilder.CreateConfigurationDictionary();

        // Act
        containerBuilder.AddFlexConfig(config => config
            .AddSource(new MemoryConfigurationSource { InitialData = testData! })
            .AddEnvironmentVariables());

        // Assert
        using var container = containerBuilder.Build();
        var flexConfig = container.Resolve<IFlexConfig>();
        
        // Verify configuration data is accessible
        var appName = flexConfig["Application:Name"];
        appName.Should().Be(testData["Application:Name"]);
    }

    [Fact]
    public void AddFlexConfig_RegistersConfigurationModule()
    {
        // Arrange
        var containerBuilder = new ContainerBuilder();
        var testData = ConfigurationTestDataBuilder.CreateConfigurationDictionary();

        // Register a service that should receive property injection
        containerBuilder.RegisterType<TestServiceWithPropertyInjection>().AsSelf();

        // Act
        containerBuilder.AddFlexConfig(config => config
            .AddSource(new MemoryConfigurationSource { InitialData = testData! }));

        // Assert
        using var container = containerBuilder.Build();
        var service = container.Resolve<TestServiceWithPropertyInjection>();
        
        // Verify property injection occurred
        service.FlexConfiguration.Should().NotBeNull();
        service.FlexConfiguration.Should().BeOfType<FlexConfiguration>();
    }

    [Fact]
    public void GetFlexConfiguration_WithValidConfiguration_ReturnsFlexConfiguration()
    {
        // Arrange
        var testData = ConfigurationTestDataBuilder.CreateConfigurationDictionary();
        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(testData!);
        var configuration = builder.Build();

        // Act
        var result = configuration.GetFlexConfiguration();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<FlexConfiguration>();
        result.Configuration.Should().BeSameAs(configuration);
    }

    [Fact]
    public void GetFlexConfiguration_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Arrange
        IConfiguration? nullConfiguration = null;

        // Act & Assert
        var action = () => nullConfiguration!.GetFlexConfiguration();
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetFlexConfiguration_PreservesConfigurationData()
    {
        // Arrange
        var testData = ConfigurationTestDataBuilder.CreateConfigurationDictionary();
        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(testData!);
        var configuration = builder.Build();

        // Act
        var flexConfig = configuration.GetFlexConfiguration();

        // Assert
        var originalValue = configuration["Application:Name"];
        var flexValue = flexConfig["Application:Name"];
        flexValue.Should().Be(originalValue);
    }

    [Theory]
    [AutoData]
    public void CurrentConfig_WithValidSectionName_ReturnsFlexConfig(string sectionName)
    {
        // Arrange
        var testData = new Dictionary<string, string?>
        {
            [$"{sectionName}:Property1"] = "Value1",
            [$"{sectionName}:Property2"] = "Value2",
            ["OtherSection:Property"] = "OtherValue"
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(testData);
        var configuration = builder.Build();

        // Act
        var result = configuration.CurrentConfig(sectionName);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<FlexConfiguration>();
        result["Property1"].Should().Be("Value1");
        result["Property2"].Should().Be("Value2");
    }

    [Fact]
    public void CurrentConfig_WithNullSectionName_ReturnsRootFlexConfig()
    {
        // Arrange
        var testData = ConfigurationTestDataBuilder.CreateConfigurationDictionary();
        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(testData!);
        var configuration = builder.Build();

        // Act
        var result = configuration.CurrentConfig();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<FlexConfiguration>();
        result.Configuration.Should().BeSameAs(configuration);
    }

    [Fact]
    public void CurrentConfig_WithEmptySectionName_ReturnsRootFlexConfig()
    {
        // Arrange
        var testData = ConfigurationTestDataBuilder.CreateConfigurationDictionary();
        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(testData!);
        var configuration = builder.Build();

        // Act
        var result = configuration.CurrentConfig("");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<FlexConfiguration>();
        result.Configuration.Should().BeSameAs(configuration);
    }

    [Theory]
    [AutoData]
    public void CurrentConfig_WithNonExistentSection_ReturnsNull(string nonExistentSection)
    {
        // Arrange
        var testData = new Dictionary<string, string?>
        {
            ["ExistingSection:Property"] = "Value"
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(testData);
        var configuration = builder.Build();

        // Act
        var result = configuration.CurrentConfig(nonExistentSection);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void CurrentConfig_WithCaseInsensitiveMatching_ReturnsFlexConfig()
    {
        // Arrange
        var testData = new Dictionary<string, string?>
        {
            ["Database:ConnectionString"] = "test-connection",
            ["Database:Timeout"] = "30"
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(testData);
        var configuration = builder.Build();

        // Act
        var result1 = configuration.CurrentConfig("Database");
        var result2 = configuration.CurrentConfig("database");
        var result3 = configuration.CurrentConfig("DATABASE");

        // Assert
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        result3.Should().NotBeNull();

        // All should point to the same section's data
        result1["ConnectionString"].Should().Be("test-connection");
        result2["ConnectionString"].Should().Be("test-connection");
        result3["ConnectionString"].Should().Be("test-connection");
    }

    [Fact]
    public void CurrentConfig_WithNullConfiguration_ThrowsNullReferenceException()
    {
        // Arrange
        IConfiguration? nullConfiguration = null;

        // Act & Assert
        var action = () => nullConfiguration!.CurrentConfig("AnySection");
        action.Should().Throw<NullReferenceException>();
    }

    [Fact]
    public void CurrentConfig_WithComplexHierarchy_NavigatesCorrectly()
    {
        // Arrange
        var testData = new Dictionary<string, string?>
        {
            ["Level1:Level2:Level3:Property"] = "DeepValue",
            ["Level1:Level2:OtherProperty"] = "MidValue",
            ["Level1:DirectProperty"] = "TopValue"
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(testData);
        var configuration = builder.Build();

        // Act
        var level1Config = configuration.CurrentConfig("Level1");
        var level2Config = level1Config?.Configuration.CurrentConfig("Level2");
        var level3Config = level2Config?.Configuration.CurrentConfig("Level3");

        // Assert
        level1Config.Should().NotBeNull();
        level1Config["DirectProperty"].Should().Be("TopValue");

        level2Config.Should().NotBeNull();
        level2Config["OtherProperty"].Should().Be("MidValue");

        level3Config.Should().NotBeNull();
        level3Config["Property"].Should().Be("DeepValue");
    }

    [Fact]
    public void Extension_Methods_Integration_WorksTogether()
    {
        // Arrange
        var containerBuilder = new ContainerBuilder();
        var testData = ConfigurationTestDataBuilder.CreateConfigurationDictionary();

        // Act - Use multiple extension methods together
        containerBuilder.AddFlexConfig(config => config
            .AddSource(new MemoryConfigurationSource { InitialData = testData! }));

        using var container = containerBuilder.Build();
        var configuration = container.Resolve<IConfiguration>();
        
        var flexConfigFromExtension = configuration.GetFlexConfiguration();
        var databaseSection = configuration.CurrentConfig("Database");
        var connectionStringsSection = configuration.CurrentConfig("ConnectionStrings");

        // Assert
        flexConfigFromExtension.Should().NotBeNull();
        flexConfigFromExtension["Application:Name"].Should().Be(testData["Application:Name"]);

        databaseSection.Should().NotBeNull();
        databaseSection["CommandTimeout"].Should().Be(testData["Database:CommandTimeout"]);

        connectionStringsSection.Should().NotBeNull();
        connectionStringsSection["DefaultConnection"].Should().Be(testData["ConnectionStrings:DefaultConnection"]);
    }
}

/// <summary>
/// Test service class to verify property injection behavior.
/// </summary>
public class TestServiceWithPropertyInjection
{
    public IFlexConfig? FlexConfiguration { get; [UsedImplicitly] set; }
}