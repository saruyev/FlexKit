using Autofac;
using Autofac.Core;
using FlexKit.Configuration.Core;
using FlexKit.Configuration.IntegrationTests.Utils;
using FluentAssertions;
using Microsoft.Extensions.Configuration.Memory;
using Reqnroll;
using FlexKit.IntegrationTests.Utils;
using JetBrains.Annotations;
// ReSharper disable TooManyDeclarations
// ReSharper disable ClassTooBig
// ReSharper disable NullableWarningSuppressionIsUsed

namespace FlexKit.Configuration.IntegrationTests.Steps.DependencyInjection;

/// <summary>
/// Step definitions for RegisterConfig extension integration scenarios.
/// Tests the RegisterConfig extension methods with Autofac integration,
/// including type registration, service injection, and error handling.
/// Uses distinct step patterns ("registration module") to avoid conflicts.
/// </summary>
[Binding]
public class RegisterConfigIntegrationSteps(ScenarioContext scenarioContext)
{
    private TestConfigurationBuilder? _configurationBuilder;
    private ContainerBuilder? _containerBuilder;
    private IContainer? _container;

    private readonly List<(Type ConfigType, string SectionPath)> _configRegistrations = new();
    private readonly Dictionary<string, string?> _configurationData = new();

    private Exception? _lastException;
    private bool _containerBuildSucceeded;

    // Test configuration classes
    private AppConfig? _resolvedAppConfig;
    private DatabaseConfig? _resolvedDatabaseConfig;
    private PaymentApiConfig? _resolvedPaymentApiConfig;
    private FeatureConfig? _resolvedFeatureConfig;
    private ApiConfig? _resolvedApiConfig;

    // Test services
    private DatabaseService? _resolvedDatabaseService;
    private ApiService? _resolvedApiService;

    #region Test Configuration Classes

    [UsedImplicitly]
    public class AppConfig
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Environment { get; set; } = string.Empty;
    }

    public class DatabaseConfig
    {
        public string ConnectionString { get; [UsedImplicitly] set; } = string.Empty;
        public int CommandTimeout { get; [UsedImplicitly] set; } = 30;
        public int MaxRetryCount { get; [UsedImplicitly] set; } = 3;
        public bool EnableLogging { get; [UsedImplicitly] set; }
    }

    public class ApiConfig
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public int Timeout { get; set; } = 5000;
        public bool EnableCompression { get; set; } = true;
    }

    [UsedImplicitly]
    public class PaymentApiConfig
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public int Timeout { get; set; } = 5000;
        public bool EnableCompression { get; set; } = true;
    }

    [UsedImplicitly]
    public class FeatureConfig
    {
        public bool EnableCaching { get; [UsedImplicitly] set; }
        public bool EnableMetrics { get; set; }
        public int MaxCacheSize { get; [UsedImplicitly] set; } = 500;
    }

    #endregion

    #region Test Service Classes

    public class DatabaseService(DatabaseConfig config)
    {
        public DatabaseConfig Config { get; } = config;
    }

    public class ApiService(ApiConfig config)
    {
        public ApiConfig Config { get; } = config;
    }

    #endregion

    #region Background Steps

    [Given(@"I have established a registration module testing environment")]
    public void GivenIHaveEstablishedARegistrationModuleTestingEnvironment()
    {
        // Quote: "public static T Create(ScenarioContext scenarioContext)"
        _configurationBuilder = TestConfigurationBuilder.Create(scenarioContext);
        _containerBuilder = new ContainerBuilder();

        _configRegistrations.Clear();
        _configurationData.Clear();

        _lastException = null;
        _containerBuildSucceeded = false;
    }

    #endregion

    #region Configuration Data Steps

    [When(@"I provision registration module test data from ""(.*)""")]
    public void WhenIProvisionRegistrationModuleTestDataFrom(string filePath)
    {
        _configurationBuilder.Should().NotBeNull("Configuration builder should be established");

        var normalizedPath = filePath.Replace('/', Path.DirectorySeparatorChar);

        // Quote: "public T AddJsonFile(string path, bool optional = true, bool reloadOnChange = false)"
        _configurationBuilder!.AddJsonFile(normalizedPath, optional: false);
    }

    [When(@"I provision additional registration module data:")]
    public void WhenIProvisionAdditionalRegistrationModuleData(Table table)
    {
        _configurationBuilder.Should().NotBeNull("Configuration builder should be established");

        var configData = new Dictionary<string, string?>();
        foreach (var row in table.Rows)
        {
            configData[row["Key"]] = row["Value"];
        }

        // Quote: "public T AddInMemoryCollection(Dictionary<string, string?> data)"
        _configurationBuilder!.AddInMemoryCollection(configData);

        // Store for later use in container registration
        foreach (var kvp in configData)
        {
            _configurationData[kvp.Key] = kvp.Value;
        }
    }

    [When(@"I provision registration module test data:")]
    public void WhenIProvisionRegistrationModuleTestData(Table table)
    {
        WhenIProvisionAdditionalRegistrationModuleData(table);
    }

    [When(@"I provision registration module minimal data:")]
    public void WhenIProvisionRegistrationModuleMinimalData(Table table)
    {
        WhenIProvisionAdditionalRegistrationModuleData(table);
    }

    [When(@"I provision registration module invalid data:")]
    public void WhenIProvisionRegistrationModuleInvalidData(Table table)
    {
        WhenIProvisionAdditionalRegistrationModuleData(table);
    }

    #endregion

    #region Registration Steps

    [When(@"I registration module configure typed configuration mappings:")]
    public void WhenIRegistrationModuleConfigureTypedConfigurationMappings(Table table)
    {
        foreach (var row in table.Rows)
        {
            var configTypeName = row["ConfigType"];
            var sectionPath = row["SectionPath"];

            var configType = GetConfigurationType(configTypeName);
            _configRegistrations.Add((configType, sectionPath));
        }

        RegisterConfigurations();
    }

    [When(@"I registration module use fluent interface for multiple configurations")]
    public void WhenIRegistrationModuleUseFluentInterfaceForMultipleConfigurations()
    {
        // Quote from RegisterConfigExtensionTests: "containerBuilder.AddFlexConfig(config => config.AddSource(new MemoryConfigurationSource { InitialData = testData }))"
        var builder = _containerBuilder!
            .AddFlexConfig(config => config.AddSource(new MemoryConfigurationSource { InitialData = _configurationData }))
            .RegisterConfig<DatabaseConfig>("Database")
            .RegisterConfig<ApiConfig>("Api");

        builder.Should().BeSameAs(_containerBuilder, "fluent interface should return the same builder");
    }

    [When(@"I registration module define batch configuration mappings:")]
    public void WhenIRegistrationModuleDefineBatchConfigurationMappings(Table table)
    {
        foreach (var row in table.Rows)
        {
            var configTypeName = row["ConfigType"];
            var sectionPath = row["SectionPath"];

            var configType = GetConfigurationType(configTypeName);
            _configRegistrations.Add((configType, sectionPath));
        }
    }

    [When(@"I registration module register configurations using batch method")]
    public void WhenIRegistrationModuleRegisterConfigurationsUsingBatchMethod()
    {
        // Quote from RegisterConfigExtensionTests: ".RegisterConfigs(configMappings);"
        _containerBuilder!
            .AddFlexConfig(config => config.AddSource(new MemoryConfigurationSource { InitialData = _configurationData }))
            .RegisterConfigs(_configRegistrations.Select(r => (r.ConfigType, r.SectionPath)).ToArray());
    }

    #endregion

    #region Service Registration Steps

    [When(@"I registration module register dependent services")]
    public void WhenIRegistrationModuleRegisterDependentServices()
    {
        // Quote from RegisterConfigExtensionTests: "containerBuilder.RegisterType<DatabaseService>().AsSelf();"
        _containerBuilder!.RegisterType<DatabaseService>().AsSelf();
        _containerBuilder!.RegisterType<ApiService>().AsSelf();
    }

    [When(@"I registration module register services that depend on configurations")]
    public void WhenIRegistrationModuleRegisterServicesThatDependOnConfigurations()
    {
        WhenIRegistrationModuleRegisterDependentServices();
    }

    #endregion

    #region Container Build Steps

    [When(@"I registration module finalize container construction")]
    public void WhenIRegistrationModuleFinalizeContainerConstruction()
    {
        BuildContainer();
    }

    [When(@"I attempt to resolve DatabaseConfig")]
    public void WhenIAttemptToResolveDatabaseConfig()
    {
        try
        {
            _resolvedDatabaseConfig = _container!.Resolve<DatabaseConfig>();
        }
        catch (Exception ex)
        {
            _lastException = ex;
        }
    }

    #endregion

    #region Assertion Steps - Build Success

    [Then(@"the registration module container should build successfully")]
    public void ThenTheRegistrationModuleContainerShouldBuildSuccessfully()
    {
        _containerBuildSucceeded.Should().BeTrue("Container should build successfully");
        _container.Should().NotBeNull("Container should be available");
        _lastException.Should().BeNull("No exception should occur during container build");
    }

    #endregion

    #region Assertion Steps - Resolution

    [Then(@"I should resolve all registered configuration types")]
    public void ThenIShouldResolveAllRegisteredConfigurationTypes()
    {
        _resolvedAppConfig = _container!.Resolve<AppConfig>();
        _resolvedDatabaseConfig = _container!.Resolve<DatabaseConfig>();
        _resolvedPaymentApiConfig = _container!.Resolve<PaymentApiConfig>();
        _resolvedFeatureConfig = _container!.Resolve<FeatureConfig>();

        _resolvedAppConfig.Should().NotBeNull();
        _resolvedDatabaseConfig.Should().NotBeNull();
        _resolvedPaymentApiConfig.Should().NotBeNull();
        _resolvedFeatureConfig.Should().NotBeNull();
    }

    [Then(@"I should resolve services with injected configurations")]
    public void ThenIShouldResolveServicesWithInjectedConfigurations()
    {
        _resolvedDatabaseService = _container!.Resolve<DatabaseService>();
        _resolvedApiService = _container!.Resolve<ApiService>();

        _resolvedDatabaseService.Should().NotBeNull();
        _resolvedApiService.Should().NotBeNull();

        _resolvedDatabaseService!.Config.Should().NotBeNull();
        _resolvedApiService!.Config.Should().NotBeNull();
    }

    [Then(@"I should resolve configurations with missing sections")]
    public void ThenIShouldResolveConfigurationsWithMissingSections()
    {
        _resolvedDatabaseConfig = _container!.Resolve<DatabaseConfig>();
        _resolvedApiConfig = _container!.Resolve<ApiConfig>();

        _resolvedDatabaseConfig.Should().NotBeNull();
        _resolvedApiConfig.Should().NotBeNull();
    }

    [Then(@"all fluently registered configurations should be available")]
    public void ThenAllFluentlyRegisteredConfigurationsShouldBeAvailable()
    {
        _resolvedDatabaseConfig = _container!.Resolve<DatabaseConfig>();
        _resolvedApiConfig = _container!.Resolve<ApiConfig>();

        _resolvedDatabaseConfig.Should().NotBeNull();
        _resolvedApiConfig.Should().NotBeNull();
    }

    [Then(@"all batch registered configurations should be available")]
    public void ThenAllBatchRegisteredConfigurationsShouldBeAvailable()
    {
        _resolvedDatabaseConfig = _container!.Resolve<DatabaseConfig>();

        _resolvedDatabaseConfig.Should().NotBeNull();
    }

    #endregion

    #region Assertion Steps - Configuration Values

    [Then(@"the AppConfig should contain correct root values")]
    public void ThenTheAppConfigShouldContainCorrectRootValues()
    {
        _resolvedAppConfig.Should().NotBeNull();
        _resolvedAppConfig!.Name.Should().NotBeEmpty();
        _resolvedAppConfig.Version.Should().NotBeEmpty();
    }

    [Then(@"the DatabaseConfig should contain correct database values")]
    public void ThenTheDatabaseConfigShouldContainCorrectDatabaseValues()
    {
        _resolvedDatabaseConfig.Should().NotBeNull();
        _resolvedDatabaseConfig!.CommandTimeout.Should().Be(60);
        _resolvedDatabaseConfig.MaxRetryCount.Should().Be(5);
        _resolvedDatabaseConfig.EnableLogging.Should().BeTrue();
    }

    [Then(@"the PaymentApiConfig should contain correct payment API values")]
    public void ThenThePaymentApiConfigShouldContainCorrectPaymentApiValues()
    {
        _resolvedPaymentApiConfig.Should().NotBeNull();
        _resolvedPaymentApiConfig!.ApiKey.Should().Be("payment-key-12345");
        _resolvedPaymentApiConfig.Timeout.Should().Be(8000);
    }

    [Then(@"the FeatureConfig should contain correct feature values")]
    public void ThenTheFeatureConfigShouldContainCorrectFeatureValues()
    {
        _resolvedFeatureConfig.Should().NotBeNull();
        _resolvedFeatureConfig!.EnableCaching.Should().BeTrue();
        _resolvedFeatureConfig.MaxCacheSize.Should().Be(1000);
    }

    [Then(@"the DatabaseService should have correct database configuration")]
    public void ThenTheDatabaseServiceShouldHaveCorrectDatabaseConfiguration()
    {
        _resolvedDatabaseService.Should().NotBeNull();
        _resolvedDatabaseService!.Config.Should().NotBeNull();

        var config = _resolvedDatabaseService.Config;
        config.ConnectionString.Should().Contain("Server=test;Database=ServiceTest;");
        config.CommandTimeout.Should().Be(45);
        config.EnableLogging.Should().BeTrue();
    }

    [Then(@"the ApiService should have correct API configuration")]
    public void ThenTheApiServiceShouldHaveCorrectApiConfiguration()
    {
        _resolvedApiService.Should().NotBeNull();
        _resolvedApiService!.Config.Should().NotBeNull();

        var config = _resolvedApiService.Config;
        config.BaseUrl.Should().Be("https://service-test.api.com");
        config.ApiKey.Should().Be("service-test-key");
        config.Timeout.Should().Be(5000);
    }

    [Then(@"the DatabaseConfig should use default values")]
    public void ThenTheDatabaseConfigShouldUseDefaultValues()
    {
        _resolvedDatabaseConfig.Should().NotBeNull();
        _resolvedDatabaseConfig!.ConnectionString.Should().BeEmpty();
        _resolvedDatabaseConfig.CommandTimeout.Should().Be(30);
        _resolvedDatabaseConfig.MaxRetryCount.Should().Be(3);
        _resolvedDatabaseConfig.EnableLogging.Should().BeFalse();
    }

    [Then(@"the ApiConfig should use default values")]
    public void ThenTheApiConfigShouldUseDefaultValues()
    {
        _resolvedApiConfig.Should().NotBeNull();
        _resolvedApiConfig!.BaseUrl.Should().BeEmpty();
        _resolvedApiConfig.ApiKey.Should().BeEmpty();
        _resolvedApiConfig.Timeout.Should().Be(5000);
        _resolvedApiConfig.EnableCompression.Should().BeTrue();
    }

    #endregion

    #region Assertion Steps - Singleton Behavior

    [Then(@"all configuration instances should be singletons")]
    public void ThenAllConfigurationInstancesShouldBeSingletons()
    {
        var appConfig1 = _container!.Resolve<AppConfig>();
        var appConfig2 = _container!.Resolve<AppConfig>();
        appConfig1.Should().BeSameAs(appConfig2);

        var dbConfig1 = _container!.Resolve<DatabaseConfig>();
        var dbConfig2 = _container!.Resolve<DatabaseConfig>();
        dbConfig1.Should().BeSameAs(dbConfig2);
    }

    [Then(@"configuration instances in services should match directly resolved ones")]
    public void ThenConfigurationInstancesInServicesShouldMatchDirectlyResolvedOnes()
    {
        var directDbConfig = _container!.Resolve<DatabaseConfig>();
        var directApiConfig = _container!.Resolve<ApiConfig>();

        _resolvedDatabaseService!.Config.Should().BeSameAs(directDbConfig);
        _resolvedApiService!.Config.Should().BeSameAs(directApiConfig);
    }

    #endregion

    #region Assertion Steps - Fluent Interface

    [Then(@"the fluent interface should return the container builder for chaining")]
    public void ThenTheFluentInterfaceShouldReturnTheContainerBuilderForChaining()
    {
        _containerBuilder.Should().NotBeNull("Fluent interface should maintain builder reference");
    }

    #endregion

    #region Assertion Steps - Error Handling

    [Then(@"resolving DatabaseConfig should throw a dependency resolution exception")]
    public void ThenResolvingDatabaseConfigShouldThrowADependencyResolutionException()
    {
        _lastException.Should().NotBeNull();
        _lastException.Should().BeOfType<DependencyResolutionException>();
    }

    [Then(@"the exception should contain binding error information")]
    public void ThenTheExceptionShouldContainBindingErrorInformation()
    {
        _lastException.Should().NotBeNull();
        _lastException.Should().BeOfType<DependencyResolutionException>();
        _lastException!.InnerException.Should().BeOfType<InvalidOperationException>();
    }

    [Then(@"each configuration type should be properly bound to its section")]
    public void ThenEachConfigurationTypeShouldBeProperlyBoundToItsSection()
    {
        _resolvedDatabaseConfig.Should().NotBeNull();
        _resolvedDatabaseConfig!.ConnectionString.Should().Contain("Server=batch;Database=BatchTest;");
        _resolvedDatabaseConfig.CommandTimeout.Should().Be(25);
    }

    #endregion

    #region Helper Methods

    private Type GetConfigurationType(string typeName)
    {
        return typeName switch
        {
            "AppConfig" => typeof(AppConfig),
            "DatabaseConfig" => typeof(DatabaseConfig),
            "ApiConfig" => typeof(ApiConfig),
            "PaymentApiConfig" => typeof(PaymentApiConfig),
            "FeatureConfig" => typeof(FeatureConfig),
            _ => throw new ArgumentException($"Unknown configuration type: {typeName}")
        };
    }

    private void RegisterConfigurations()
    {
        // Quote from RegisterConfigExtensionTests: "containerBuilder.AddFlexConfig(config => config.AddSource(new MemoryConfigurationSource { InitialData = testData }))"
        var builder = _containerBuilder!.AddFlexConfig(config => config.AddSource(new MemoryConfigurationSource { InitialData = _configurationData }));

        foreach (var (configType, sectionPath) in _configRegistrations)
        {
            if (string.IsNullOrEmpty(sectionPath))
            {
                // Quote from RegisterConfigExtensionTests: ".RegisterConfig<AppConfig>();"
                var method = typeof(ContainerBuilderConfigurationExtensions)
                    .GetMethod("RegisterConfig", [typeof(ContainerBuilder)])!
                    .MakeGenericMethod(configType);
                method.Invoke(null, [builder]);
            }
            else
            {
                // Quote from RegisterConfigExtensionTests: ".RegisterConfig<RegisterDatabaseConfig>("Database")"
                var method = typeof(ContainerBuilderConfigurationExtensions)
                    .GetMethod("RegisterConfig", [typeof(ContainerBuilder), typeof(string)])!
                    .MakeGenericMethod(configType);
                method.Invoke(null, [builder, sectionPath]);
            }
        }
    }

    private void BuildContainer()
    {
        try
        {
            _container = _containerBuilder!.Build();
            _containerBuildSucceeded = true;

            // Quote: "public static void RegisterAutofacContainer(this ScenarioContext scenarioContext, IContainer container)"
            scenarioContext.RegisterAutofacContainer(_container);
        }
        catch (Exception ex)
        {
            _lastException = ex;
            _containerBuildSucceeded = false;
            throw;
        }
    }

    #endregion
}