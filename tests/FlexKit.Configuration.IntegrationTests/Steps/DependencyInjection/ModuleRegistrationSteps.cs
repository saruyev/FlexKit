using Autofac;
using FlexKit.Configuration.Core;
using FlexKit.Configuration.IntegrationTests.Utils;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Reqnroll;
using FlexKit.IntegrationTests.Utils;
using JetBrains.Annotations;
// ReSharper disable NullableWarningSuppressionIsUsed

namespace FlexKit.Configuration.IntegrationTests.Steps.DependencyInjection;

/// <summary>
/// Step definitions for module registration scenarios.
/// Tests FlexConfig module registration with Autofac container.
/// Uses completely unique step patterns ("established", "compose", "deploy") to avoid all conflicts.
/// </summary>
[Binding]
public class ModuleRegistrationSteps(ScenarioContext scenarioContext)
{
    private TestContainerBuilder? _testContainerBuilder;
    private ContainerBuilder? _containerBuilder;
    private IContainer? _container;
    private TestConfigurationBuilder? _configurationBuilder;
    private readonly Dictionary<string, TestConfigurationModule> _testModules = new();
    private readonly Dictionary<string, Dictionary<string, string?>> _moduleConfigurations = new();
    private bool _containerBuildSucceeded;

    #region Test Module Class

    /// <summary>
    /// Test configuration module for module registration scenarios.
    /// </summary>
    [UsedImplicitly]
    public class TestConfigurationModule(string moduleName, IConfiguration configuration) : Module
    {
        public string ModuleName => moduleName;

        protected override void Load(ContainerBuilder builder)
        {
            // Register the configuration instance
            builder.RegisterInstance(configuration)
                .As<IConfiguration>()
                .Named<IConfiguration>(moduleName)
                .SingleInstance();

            // Register FlexConfig based on this configuration
            var flexConfig = configuration.GetFlexConfiguration();
            builder.RegisterInstance(flexConfig)
                .As<IFlexConfig>()
                .As<dynamic>()
                .SingleInstance();
        }
    }

    /// <summary>
    /// Test service that depends on module configuration.
    /// </summary>
    public class TestServiceWithModuleConfig(IFlexConfig configuration)
    {
        public IFlexConfig Configuration { get; } = configuration;
        public string ServiceName { get; } = "ModuleConfigService";
    }

    #endregion

    #region Given Steps

    [Given(@"I have established a module registration test environment")]
    public void GivenIHaveEstablishedAModuleRegistrationTestEnvironment()
    {
        _testContainerBuilder = TestContainerBuilder.Create(scenarioContext);
        _containerBuilder = new ContainerBuilder();
        _configurationBuilder = TestConfigurationBuilder.Create(scenarioContext);
        
        scenarioContext.Set(_testContainerBuilder, "TestContainerBuilder");
        scenarioContext.Set(_containerBuilder, "ContainerBuilder");
        scenarioContext.Set(_configurationBuilder, "ConfigurationBuilder");
    }

    #endregion

    #region When Steps

    [When(@"I compose a test configuration module with settings:")]
    public void WhenIComposeATestConfigurationModuleWithSettings(Table table)
    {
        _configurationBuilder.Should().NotBeNull("Configuration builder should be established");
        
        var configData = new Dictionary<string, string?>();
        foreach (var row in table.Rows)
        {
            configData[row["Key"]] = row["Value"];
        }

        var configuration = _configurationBuilder!
            .AddInMemoryCollection(configData)
            .Build();

        var moduleName = "TestModule";
        var testModule = new TestConfigurationModule(moduleName, configuration);
        
        _testModules[moduleName] = testModule;
        _moduleConfigurations[moduleName] = configData;
    }

    [When(@"I compose configuration module ""(.*)"" with settings:")]
    public void WhenIComposeConfigurationModuleWithSettings(string moduleName, Table table)
    {
        _configurationBuilder.Should().NotBeNull("Configuration builder should be established");
        
        var configData = new Dictionary<string, string?>();
        foreach (var row in table.Rows)
        {
            configData[row["Key"]] = row["Value"];
        }

        var configuration = _configurationBuilder!
            .AddInMemoryCollection(configData)
            .Build();

        var testModule = new TestConfigurationModule(moduleName, configuration);
        
        _testModules[moduleName] = testModule;
        _moduleConfigurations[moduleName] = configData;
    }

    [When(@"I deploy the test module to the container builder")]
    public void WhenIDeployTheTestModuleToTheContainerBuilder()
    {
        _containerBuilder.Should().NotBeNull("Container builder should be established");
        _testModules.Should().NotBeEmpty("At least one test module should be composed");

        var firstModule = _testModules.Values.First();
        _containerBuilder!.RegisterModule(firstModule);
    }

    [When(@"I deploy both test modules to the container builder")]
    public void WhenIDeployBothTestModulesToTheContainerBuilder()
    {
        _containerBuilder.Should().NotBeNull("Container builder should be established");
        _testModules.Should().HaveCountGreaterThanOrEqualTo(2, "At least two modules should be composed");

        foreach (var module in _testModules.Values)
        {
            _containerBuilder!.RegisterModule(module);
        }
    }

    [When(@"I deploy a test service depending on module configuration")]
    public void WhenIDeployATestServiceDependingOnModuleConfiguration()
    {
        _containerBuilder.Should().NotBeNull("Container builder should be established");

        _containerBuilder!.RegisterType<TestServiceWithModuleConfig>()
            .AsSelf()
            .SingleInstance();
    }

    [When(@"I finalize the container with deployed modules")]
    public void WhenIFinalizeTheContainerWithDeployedModules()
    {
        _containerBuilder.Should().NotBeNull("Container builder should be established");

        try
        {
            _container = _containerBuilder!.Build();
            _containerBuildSucceeded = true;
            scenarioContext.RegisterAutofacContainer(_container);
        }
        catch (Exception)
        {
            _containerBuildSucceeded = false;
        }
    }

    #endregion

    #region Then Steps

    [Then(@"the container should reflect deployed test module registrations")]
    public void ThenTheContainerShouldReflectDeployedTestModuleRegistrations()
    {
        _container.Should().NotBeNull("Container should be finalized");
        _containerBuildSucceeded.Should().BeTrue("Container finalization should have succeeded");
        
        _container!.IsRegistered<IFlexConfig>().Should().BeTrue("Test module should have registered IFlexConfig");
    }

    [Then(@"the deployed test module should expose FlexConfig correctly")]
    public void ThenTheDeployedTestModuleShouldExposeFlexConfigCorrectly()
    {
        _container.Should().NotBeNull("Container should be finalized");
        
        var resolvedFlexConfig = _container!.Resolve<IFlexConfig>();
        resolvedFlexConfig.Should().NotBeNull("FlexConfig should be resolvable from test module deployment");
    }

    [Then(@"I should access configuration through the deployed test module")]
    public void ThenIShouldAccessConfigurationThroughTheDeployedTestModule()
    {
        _container.Should().NotBeNull("Container should be finalized");
        
        var resolvedFlexConfig = _container!.Resolve<IFlexConfig>();
        resolvedFlexConfig.Should().NotBeNull("FlexConfig should be resolved");
        
        // Verify we can access configuration data from any deployed module
        var anyModuleConfig = _moduleConfigurations.Values.FirstOrDefault();
        if (anyModuleConfig != null)
        {
            foreach (var kvp in anyModuleConfig)
            {
                resolvedFlexConfig[kvp.Key].Should().Be(kvp.Value);
            }
        }
    }

    [Then(@"the container should encompass both test module and service deployments")]
    public void ThenTheContainerShouldEncompassBothTestModuleAndServiceDeployments()
    {
        _container.Should().NotBeNull("Container should be finalized");
        
        _container!.IsRegistered<IFlexConfig>().Should().BeTrue("Test module should have registered IFlexConfig");
        _container.IsRegistered<TestServiceWithModuleConfig>().Should().BeTrue("Service should be registered");
    }

    [Then(@"the service should obtain configuration from the deployed test module")]
    public void ThenTheServiceShouldObtainConfigurationFromTheDeployedTestModule()
    {
        _container.Should().NotBeNull("Container should be finalized");
        
        var resolvedServiceWithModuleConfig = _container!.Resolve<TestServiceWithModuleConfig>();
        resolvedServiceWithModuleConfig.Should().NotBeNull("Service should be resolved");
        resolvedServiceWithModuleConfig.Configuration.Should().NotBeNull("Service should have received module configuration");
    }

    [Then(@"the deployed test module configuration should be available to dependent services")]
    public void ThenTheDeployedTestModuleConfigurationShouldBeAvailableToDependentServices()
    {
        _container.Should().NotBeNull("Container should be finalized");
        
        var resolvedServiceWithModuleConfig = _container!.Resolve<TestServiceWithModuleConfig>();
        resolvedServiceWithModuleConfig.Should().NotBeNull("Service should be resolved");
        
        // Verify service can access module configuration data
        var anyModuleConfig = _moduleConfigurations.Values.FirstOrDefault();
        if (anyModuleConfig != null)
        {
            foreach (var kvp in anyModuleConfig)
            {
                resolvedServiceWithModuleConfig.Configuration[kvp.Key].Should().Be(kvp.Value);
            }
        }
    }

    [Then(@"the container should encompass deployments from both test modules")]
    public void ThenTheContainerShouldEncompassDeploymentsFromBothTestModules()
    {
        _container.Should().NotBeNull("Container should be finalized");
        _containerBuildSucceeded.Should().BeTrue("Container finalization should have succeeded");
        
        _testModules.Should().HaveCountGreaterThanOrEqualTo(2, "At least two modules should be deployed");
        _container!.IsRegistered<IFlexConfig>().Should().BeTrue("Test modules should have registered IFlexConfig");
    }

    [Then(@"configuration from test module ""(.*)"" should be reachable")]
    public void ThenConfigurationFromTestModuleShouldBeReachable(string moduleName)
    {
        _container.Should().NotBeNull("Container should be finalized");
        _moduleConfigurations.Should().ContainKey(moduleName, $"Test module {moduleName} should have configuration data");
        
        var flexConfig = _container!.Resolve<IFlexConfig>();
        var moduleConfig = _moduleConfigurations[moduleName];
        
        foreach (var kvp in moduleConfig)
        {
            flexConfig[kvp.Key].Should().Be(kvp.Value);
        }
    }

    [Then(@"deployed test modules should work independently without interference")]
    public void ThenDeployedTestModulesShouldWorkIndependentlyWithoutInterference()
    {
        // Verify that each module's configuration is preserved and accessible
        foreach (var moduleName in _testModules.Keys)
        {
            ThenConfigurationFromTestModuleShouldBeReachable(moduleName);
        }
    }

    #endregion
}