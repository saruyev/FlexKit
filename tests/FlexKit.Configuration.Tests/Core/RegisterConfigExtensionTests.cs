using Autofac;
using Autofac.Core;
using FlexKit.Configuration.Core;
using FlexKit.Configuration.Tests.TestBase;
using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration.Memory;
using Xunit;
// ReSharper disable TooManyDeclarations
// ReSharper disable MethodTooLong
// ReSharper disable NullableWarningSuppressionIsUsed

namespace FlexKit.Configuration.Tests.Core;

/// <summary>
/// Test configuration classes for RegisterConfig functionality
/// </summary>
public class RegisterDatabaseConfig
{
    public string ConnectionString { get; set; } = string.Empty;
    public int CommandTimeout { get; set; } = 30;
    public int MaxRetryCount { get; set; } = 3;
    public bool EnableLogging { get; [UsedImplicitly] set; }
}

public class ApiConfig
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public int Timeout { get; set; } = 5000;
    public bool EnableCompression { get; set; } = true;
}

public class AppConfig
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public RegisterDatabaseConfig Database { get; set; } = new();
    public Dictionary<string, string> Settings { get; set; } = new();
}

/// <summary>
/// Test services to verify dependency injection works correctly
/// </summary>
public class DatabaseService(RegisterDatabaseConfig config)
{
    public RegisterDatabaseConfig Config { get; } = config;
}

public class ApiService(ApiConfig config)
{
    public ApiConfig Config { get; } = config;
}

public class AppService(AppConfig config)
{
    public AppConfig Config { get; } = config;
}

/// <summary>
/// Test service with property injection to verify ConfigurationModule integration
/// </summary>
public class ServiceWithPropertyInjection
{
    public IFlexConfig? FlexConfiguration { get; [UsedImplicitly] set; }
    [UsedImplicitly] public RegisterDatabaseConfig? DatabaseConfig { get; set; }
    public ApiConfig? ApiConfig { get; set; }
}

/// <summary>
/// Comprehensive tests for RegisterConfig extension methods
/// </summary>
public class RegisterConfigExtensionTests : UnitTestBase
{
    [Fact]
    public void RegisterConfig_WithRootBinding_RegistersCorrectly()
    {
        // Arrange
        var testData = new Dictionary<string, string?>
        {
            ["Name"] = "Test Application",
            ["Version"] = "1.0.0",
            ["Environment"] = "Testing",
            ["Database:ConnectionString"] = "Server=localhost;Database=TestDb;",
            ["Database:CommandTimeout"] = "60",
            ["Database:EnableLogging"] = "true",
            ["Settings:Key1"] = "Value1",
            ["Settings:Key2"] = "Value2"
        };

        var containerBuilder = new ContainerBuilder();

        // Act
        containerBuilder.AddFlexConfig(config => config
            .AddSource(new MemoryConfigurationSource { InitialData = testData }))
            .RegisterConfig<AppConfig>();

        // Register a service that depends on the config
        containerBuilder.RegisterType<AppService>().AsSelf();

        using var container = containerBuilder.Build();

        // Assert
        var appConfig = container.Resolve<AppConfig>();
        appConfig.Should().NotBeNull();
        appConfig.Name.Should().Be("Test Application");
        appConfig.Version.Should().Be("1.0.0");
        appConfig.Environment.Should().Be("Testing");
        appConfig.Database.ConnectionString.Should().Be("Server=localhost;Database=TestDb;");
        appConfig.Database.CommandTimeout.Should().Be(60);
        appConfig.Database.EnableLogging.Should().BeTrue();
        appConfig.Settings.Should().ContainKeys("Key1", "Key2");

        // Verify the service can be resolved with injected config
        var appService = container.Resolve<AppService>();
        appService.Config.Should().BeSameAs(appConfig);
    }

    [Fact]
    public void RegisterConfig_WithSectionBinding_RegistersCorrectly()
    {
        // Arrange
        var testData = new Dictionary<string, string?>
        {
            ["Database:ConnectionString"] = "Server=prod;Database=ProdDb;",
            ["Database:CommandTimeout"] = "45",
            ["Database:MaxRetryCount"] = "5",
            ["Database:EnableLogging"] = "false",
            ["External:Api:BaseUrl"] = "https://api.example.com",
            ["External:Api:ApiKey"] = "secret-key-123",
            ["External:Api:Timeout"] = "10000",
            ["External:Api:EnableCompression"] = "false"
        };

        var containerBuilder = new ContainerBuilder();

        // Act
        containerBuilder.AddFlexConfig(config => config
            .AddSource(new MemoryConfigurationSource { InitialData = testData }))
            .RegisterConfig<RegisterDatabaseConfig>("Database")
            .RegisterConfig<ApiConfig>("External:Api");

        // Register services that depend on configs
        containerBuilder.RegisterType<DatabaseService>().AsSelf();
        containerBuilder.RegisterType<ApiService>().AsSelf();

        using var container = containerBuilder.Build();

        // Assert - Database config
        var dbConfig = container.Resolve<RegisterDatabaseConfig>();
        dbConfig.Should().NotBeNull();
        dbConfig.ConnectionString.Should().Be("Server=prod;Database=ProdDb;");
        dbConfig.CommandTimeout.Should().Be(45);
        dbConfig.MaxRetryCount.Should().Be(5);
        dbConfig.EnableLogging.Should().BeFalse();

        // Assert - API config  
        var apiConfig = container.Resolve<ApiConfig>();
        apiConfig.Should().NotBeNull();
        apiConfig.BaseUrl.Should().Be("https://api.example.com");
        apiConfig.ApiKey.Should().Be("secret-key-123");
        apiConfig.Timeout.Should().Be(10000);
        apiConfig.EnableCompression.Should().BeFalse();

        // Verify services get injected configs
        var dbService = container.Resolve<DatabaseService>();
        dbService.Config.Should().BeSameAs(dbConfig);

        var apiService = container.Resolve<ApiService>();
        apiService.Config.Should().BeSameAs(apiConfig);
    }

    [Fact]
    public void RegisterConfig_WithMissingSection_CreatesDefaultInstance()
    {
        // Arrange
        var testData = new Dictionary<string, string?>
        {
            ["SomeOtherSection:Value"] = "exists"
        };

        var containerBuilder = new ContainerBuilder();

        // Act
        containerBuilder.AddFlexConfig(config => config
            .AddSource(new MemoryConfigurationSource { InitialData = testData }))
            .RegisterConfig<RegisterDatabaseConfig>("NonExistentSection");

        using var container = containerBuilder.Build();

        // Assert
        var dbConfig = container.Resolve<RegisterDatabaseConfig>();
        dbConfig.Should().NotBeNull();
        // Should have default values since a section doesn't exist
        dbConfig.ConnectionString.Should().BeEmpty();
        dbConfig.CommandTimeout.Should().Be(30); // Default from constructor
        dbConfig.MaxRetryCount.Should().Be(3);   // Default from constructor
        dbConfig.EnableLogging.Should().BeFalse(); // Default bool value
    }

    [Fact]
    public void RegisterConfig_MultipleConfigs_AllRegisteredAsSingletons()
    {
        // Arrange
        var testData = ConfigurationTestDataBuilder.CreateConfigurationDictionary();
        var containerBuilder = new ContainerBuilder();

        // Act
        containerBuilder.AddFlexConfig(config => config
            .AddSource(new MemoryConfigurationSource { InitialData = testData! }))
            .RegisterConfig<RegisterDatabaseConfig>("Database")
            .RegisterConfig<ApiConfig>("External:PaymentApi")
            .RegisterConfig<AppConfig>(); // Root binding

        using var container = containerBuilder.Build();

        // Assert - Multiple resolutions should return the same instances (singleton)
        var dbConfig1 = container.Resolve<RegisterDatabaseConfig>();
        var dbConfig2 = container.Resolve<RegisterDatabaseConfig>();
        dbConfig1.Should().BeSameAs(dbConfig2);

        var apiConfig1 = container.Resolve<ApiConfig>();
        var apiConfig2 = container.Resolve<ApiConfig>();
        apiConfig1.Should().BeSameAs(apiConfig2);

        var appConfig1 = container.Resolve<AppConfig>();
        var appConfig2 = container.Resolve<AppConfig>();
        appConfig1.Should().BeSameAs(appConfig2);
    }

    [Fact]
    public void RegisterConfig_IntegratesWithConfigurationModule_PropertyInjectionWorks()
    {
        // Arrange
        var testData = new Dictionary<string, string?>
        {
            ["Database:ConnectionString"] = "Server=test;",
            ["External:Api:BaseUrl"] = "https://test-api.com"
        };

        var containerBuilder = new ContainerBuilder();

        // Act
        containerBuilder.AddFlexConfig(config => config
            .AddSource(new MemoryConfigurationSource { InitialData = testData }))
            .RegisterConfig<RegisterDatabaseConfig>("Database")
            .RegisterConfig<ApiConfig>("External:Api");

        // Register service with property injection
        containerBuilder.RegisterType<ServiceWithPropertyInjection>().AsSelf();

        using var container = containerBuilder.Build();

        // Assert
        var service = container.Resolve<ServiceWithPropertyInjection>();

        // FlexConfiguration should be injected by ConfigurationModule
        service.FlexConfiguration.Should().NotBeNull();
        service.FlexConfiguration!["Database:ConnectionString"].Should().Be("Server=test;");

        // Note: DatabaseConfig and ApiConfig properties won't be auto-injected unless we extend
        // ConfigurationModule to handle config types too. This is expected behavior.
    }

    [Fact]
    public void RegisterConfig_WithInvalidConfiguration_ThrowsExceptionDuringResolution()
    {
        // Arrange
        var testData = new Dictionary<string, string?>
        {
            ["Database:CommandTimeout"] = "not-a-number",
            ["Database:MaxRetryCount"] = "also-not-a-number"
        };

        var containerBuilder = new ContainerBuilder();
        containerBuilder.AddFlexConfig(config => config
            .AddSource(new MemoryConfigurationSource { InitialData = testData }))
            .RegisterConfig<RegisterDatabaseConfig>("Database");

        using var container = containerBuilder.Build();

        // Act & Assert
        // ReSharper disable once AccessToDisposedClosure
        Func<RegisterDatabaseConfig> action = () => container.Resolve<RegisterDatabaseConfig>();
        action.Should().Throw<DependencyResolutionException>()
            .WithInnerException<InvalidOperationException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RegisterConfig_WithInvalidSectionPath_ThrowsArgumentException(string invalidPath)
    {
        // Arrange
        var containerBuilder = new ContainerBuilder();

        // Act & Assert
        var action = () => containerBuilder.RegisterConfig<RegisterDatabaseConfig>(invalidPath);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be an empty string or composed entirely of whitespace*");
    }

    [Fact]
    public void RegisterConfig_WithNullBuilder_ThrowsArgumentNullException()
    {
        // Arrange
        ContainerBuilder? nullBuilder = null;

        // Act & Assert
        var action = () => nullBuilder!.RegisterConfig<RegisterDatabaseConfig>();
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("builder");
    }

    [Fact]
    public void RegisterConfig_FluentInterface_EnablesMethodChaining()
    {
        // Arrange
        var testData = ConfigurationTestDataBuilder.CreateConfigurationDictionary();
        var containerBuilder = new ContainerBuilder();

        // Act - Should not throw and should return ContainerBuilder for chaining
        var builder = containerBuilder
            .AddFlexConfig(config => config
                .AddSource(new MemoryConfigurationSource { InitialData = testData! }))
            .RegisterConfig<RegisterDatabaseConfig>("Database")
            .RegisterConfig<ApiConfig>("External:PaymentApi")
            .RegisterConfig<AppConfig>();

        builder.RegisterType<DatabaseService>().AsSelf();
        builder.RegisterType<ApiService>().AsSelf();

        // Assert
        builder.Should().BeSameAs(containerBuilder);

        // Verify everything works together
        using var container = builder.Build();
        var dbService = container.Resolve<DatabaseService>();
        var apiService = container.Resolve<ApiService>();

        dbService.Should().NotBeNull();
        apiService.Should().NotBeNull();
        dbService.Config.Should().NotBeNull();
        apiService.Config.Should().NotBeNull();
    }

    [Fact]
    public void RegisterConfigs_BatchRegistration_WorksCorrectly()
    {
        // Arrange
        var testData = new Dictionary<string, string?>
        {
            ["Database:ConnectionString"] = "Server=batch;",
            ["External:Api:BaseUrl"] = "https://batch-api.com"
        };

        var configMappings = new[]
        {
            (typeof(RegisterDatabaseConfig), "Database"),
            (typeof(ApiConfig), "External:Api")
        };

        var containerBuilder = new ContainerBuilder();

        // Act
        containerBuilder.AddFlexConfig(config => config
            .AddSource(new MemoryConfigurationSource { InitialData = testData }))
            .RegisterConfigs(configMappings);

        using var container = containerBuilder.Build();

        // Assert
        var dbConfig = container.Resolve<RegisterDatabaseConfig>();
        dbConfig.ConnectionString.Should().Be("Server=batch;");

        var apiConfig = container.Resolve<ApiConfig>();
        apiConfig.BaseUrl.Should().Be("https://batch-api.com");
    }
}
