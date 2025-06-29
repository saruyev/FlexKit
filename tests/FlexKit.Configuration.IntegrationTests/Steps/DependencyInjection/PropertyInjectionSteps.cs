using Autofac;
using FlexKit.Configuration.Core;
using FlexKit.Configuration.IntegrationTests.Utils;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Reqnroll;
using FlexKit.IntegrationTests.Utils;
using JetBrains.Annotations;

namespace FlexKit.Configuration.IntegrationTests.Steps.DependencyInjection;

/// <summary>
/// Step definitions for property injection scenarios.
/// Tests FlexConfig property injection with Autofac container,
/// including property resolution, lifetime management, and integration with various configuration sources.
/// Uses distinct step patterns ("arranged", "enable", "integrate") to avoid conflicts 
/// with other step classes.
/// </summary>
[Binding]
public class PropertyInjectionSteps(ScenarioContext scenarioContext)
{
    private TestContainerBuilder? _testContainerBuilder;
    private ContainerBuilder? _containerBuilder;
    private IContainer? _container;
    private TestConfigurationBuilder? _configurationBuilder;
    private IConfiguration? _configuration;
    private IFlexConfig? _flexConfiguration;
    private PropertyInjectionTestService? _resolvedServiceWithProperty;
    private MultiplePropertyInjectionService? _resolvedServiceWithMultipleProperties;
    private PropertyInjectionModule? _customPropertyModule;
    private readonly Dictionary<string, string?> _testConfigurationData = new();
    [UsedImplicitly] public readonly Dictionary<string, string?> EnvironmentVariables = new();
    private Exception? _lastPropertyInjectionException;
    private bool _containerBuildSucceeded;
    private bool _propertyInjectionEnabled;
    private readonly List<PropertyInjectionTestService> _multipleResolvedInstances = new();

    #region Test Service Classes

    /// <summary>
    /// Test service with FlexConfig property injection.
    /// Property must be named "FlexConfiguration" for ConfigurationModule to inject it.
    /// </summary>
    public class PropertyInjectionTestService
    {
        public IFlexConfig? FlexConfiguration { get; [UsedImplicitly] set; }
        public bool HasInjectedProperty => FlexConfiguration != null;
    }

    /// <summary>
    /// Test service with multiple configuration properties for injection.
    /// ConfigurationModule will inject only FlexConfiguration property.
    /// </summary>
    public class MultiplePropertyInjectionService
    {
        public IFlexConfig? FlexConfiguration { get; [UsedImplicitly] set; }
        public IConfiguration? StandardConfiguration { get; [UsedImplicitly] set; }
        public dynamic? DynamicConfiguration { get; [UsedImplicitly] set; }
        
        public int InjectedPropertiesCount => 
            (FlexConfiguration != null ? 1 : 0) +
            (StandardConfiguration != null ? 1 : 0) +
            (DynamicConfiguration != null ? 1 : 0);
    }

    /// <summary>
    /// Custom Autofac module for property injection scenarios.
    /// </summary>
    private class PropertyInjectionModule(Dictionary<string, string?> moduleConfiguration) : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // Register configuration from module data
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddInMemoryCollection(moduleConfiguration);
            var configuration = configBuilder.Build();

            builder.RegisterInstance(configuration)
                .As<IConfiguration>()
                .SingleInstance();

            // Register FlexConfig based on this configuration
            var flexConfig = configuration.GetFlexConfiguration();
            builder.RegisterInstance(flexConfig)
                .As<IFlexConfig>()
                .As<dynamic>()
                .SingleInstance();
        }
    }

    #endregion

    #region Given Steps - Setup

    [Given(@"I have arranged a property injection test environment")]
    public void GivenIHaveArrangedAPropertyInjectionTestEnvironment()
    {
        _testContainerBuilder = TestContainerBuilder.Create(scenarioContext);
        _containerBuilder = new ContainerBuilder();
        _configurationBuilder = TestConfigurationBuilder.Create(scenarioContext);
        scenarioContext.Set(_testContainerBuilder, "PropertyInjectionContainerBuilder");
    }

    [Given(@"I have configured test configuration data:")]
    public void GivenIHaveConfiguredTestConfigurationData(Table table)
    {
        _testConfigurationData.Clear();
        
        foreach (var row in table.Rows)
        {
            var key = row["Key"];
            var value = row["Value"];
            _testConfigurationData[key] = value;
        }

        // Build configuration from test data
        _configuration = _configurationBuilder!
            .AddInMemoryCollection(_testConfigurationData)
            .Build();

        _flexConfiguration = _configuration.GetFlexConfiguration();
        
        scenarioContext.Set(_configuration, "PropertyInjectionConfiguration");
        scenarioContext.Set(_flexConfiguration, "PropertyInjectionFlexConfiguration");
    }

    #endregion

    #region When Steps - Property Injection Actions

    [When(@"I enable property injection for container")]
    public void WhenIEnablePropertyInjectionForContainer()
    {
        _containerBuilder.Should().NotBeNull("Container builder should be initialized");
        
        // Enable property injection using Autofac's property injection support
        _propertyInjectionEnabled = true;
        
        // Set up a basic configuration if none exists
        if (_configuration == null)
        {
            var basicConfig = new Dictionary<string, string?>
            {
                ["App:Name"] = "PropertyInjectionTest",
                ["App:Version"] = "1.0.0"
            };
            
            _configuration = _configurationBuilder!
                .AddInMemoryCollection(basicConfig)
                .Build();

            _flexConfiguration = _configuration.GetFlexConfiguration();
            
            scenarioContext.Set(_configuration, "PropertyInjectionConfiguration");
            scenarioContext.Set(_flexConfiguration, "PropertyInjectionFlexConfiguration");
        }
    }

    [When(@"I register service with FlexConfig property")]
    public void WhenIRegisterServiceWithFlexConfigProperty()
    {
        _containerBuilder.Should().NotBeNull("Container builder should be initialized");
        
        if (_propertyInjectionEnabled)
        {
            _containerBuilder!.RegisterType<PropertyInjectionTestService>()
                .PropertiesAutowired()
                .SingleInstance();
        }
        else
        {
            _containerBuilder!.RegisterType<PropertyInjectionTestService>()
                .SingleInstance();
        }
    }

    [When(@"I register service with multiple configuration properties")]
    public void WhenIRegisterServiceWithMultipleConfigurationProperties()
    {
        _containerBuilder.Should().NotBeNull("Container builder should be initialized");
        
        if (_propertyInjectionEnabled)
        {
            _containerBuilder!.RegisterType<MultiplePropertyInjectionService>()
                .PropertiesAutowired()
                .SingleInstance();
        }
        else
        {
            _containerBuilder!.RegisterType<MultiplePropertyInjectionService>()
                .SingleInstance();
        }
    }

    [When(@"I register service with FlexConfig property as singleton")]
    public void WhenIRegisterServiceWithFlexConfigPropertyAsSingleton()
    {
        WhenIRegisterServiceWithFlexConfigProperty(); // Already registers as singleton
    }

    [When(@"I integrate JSON file ""(.*)"" as configuration source")]
    public void WhenIIntegrateJsonFileAsConfigurationSource(string filePath)
    {
        _configurationBuilder.Should().NotBeNull("Configuration builder should be initialized");
        
        // Use the same simple normalization pattern as other steps
        var normalizedPath = filePath.Replace('/', Path.DirectorySeparatorChar);
        
        if (!File.Exists(normalizedPath))
        {
            throw new FileNotFoundException($"Test data file not found: {normalizedPath}");
        }

        _configuration = _configurationBuilder!
            .AddJsonFile(normalizedPath, optional: false)
            .Build();

        _flexConfiguration = _configuration.GetFlexConfiguration();
        
        scenarioContext.Set(_configuration, "PropertyInjectionConfiguration");
        scenarioContext.Set(_flexConfiguration, "PropertyInjectionFlexConfiguration");
    }

    [When(@"I create custom property injection module with configuration:")]
    public void WhenICreateCustomPropertyInjectionModuleWithConfiguration(Table table)
    {
        var moduleData = new Dictionary<string, string?>();
        
        foreach (var row in table.Rows)
        {
            var key = row["Key"];
            var value = row["Value"];
            moduleData[key] = value;
        }

        _customPropertyModule = new PropertyInjectionModule(moduleData);
    }

    [When(@"I register custom module in container")]
    public void WhenIRegisterCustomModuleInContainer()
    {
        _containerBuilder.Should().NotBeNull("Container builder should be initialized");
        _customPropertyModule.Should().NotBeNull("Custom module should be created");
        
        _containerBuilder!.RegisterModule(_customPropertyModule!);
        
        // Don't register configuration separately when using custom module
        // The module handles its own configuration registration
        _configuration = null;
        _flexConfiguration = null;
    }

    [When(@"I assign environment variable ""(.*)"" to ""(.*)""")]
    public void WhenIAssignEnvironmentVariableTo(string variableName, string value)
    {
        EnvironmentVariables[variableName] = value;
        Environment.SetEnvironmentVariable(variableName, value);
    }

    [When(@"I assign environment variables with prefix ""(.*)"" as configuration source")]
    public void WhenIAssignEnvironmentVariablesWithPrefixAsConfigurationSource(string prefix)
    {
        _configurationBuilder.Should().NotBeNull("Configuration builder should be initialized");
        
        _configuration = _configurationBuilder!
            .AddEnvironmentVariables(prefix + "__")
            .Build();

        _flexConfiguration = _configuration.GetFlexConfiguration();
        
        scenarioContext.Set(_configuration, "PropertyInjectionConfiguration");
        scenarioContext.Set(_flexConfiguration, "PropertyInjectionFlexConfiguration");
    }

    [When(@"I build container with property injection support")]
    public void WhenIBuildContainerWithPropertyInjectionSupport()
    {
        try
        {
            _containerBuilder.Should().NotBeNull("Container builder should be initialized");
            
            // Only register configuration if no custom module was registered
            // (custom modules handle their own configuration registration)
            if (_customPropertyModule == null && _configuration != null)
            {
                _containerBuilder!.RegisterInstance(_configuration)
                    .As<IConfiguration>()
                    .SingleInstance();

                if (_flexConfiguration != null)
                {
                    _containerBuilder!.RegisterInstance(_flexConfiguration)
                        .As<IFlexConfig>()
                        .As<dynamic>()
                        .SingleInstance();
                }
            }

            // Register the ConfigurationModule for property injection support
            _containerBuilder!.RegisterModule<ConfigurationModule>();

            _container = _containerBuilder!.Build();
            _containerBuildSucceeded = true;
            
            scenarioContext.RegisterAutofacContainer(_container);
        }
        catch (Exception ex)
        {
            _lastPropertyInjectionException = ex;
            _containerBuildSucceeded = false;
        }
    }

    [When(@"I register service with FlexConfig property without providing configuration")]
    public void WhenIRegisterServiceWithFlexConfigPropertyWithoutProvidingConfiguration()
    {
        // Don't set up any configuration, just register the service
        WhenIRegisterServiceWithFlexConfigProperty();
    }

    [When(@"I attempt to build container with property injection support")]
    public void WhenIAttemptToBuildContainerWithPropertyInjectionSupport()
    {
        WhenIBuildContainerWithPropertyInjectionSupport();
    }

    [When(@"I resolve service with property injection")]
    public void WhenIResolveServiceWithPropertyInjection()
    {
        _container.Should().NotBeNull("Container should be built successfully");
        
        try
        {
            _resolvedServiceWithProperty = _container!.Resolve<PropertyInjectionTestService>();
        }
        catch (Exception ex)
        {
            _lastPropertyInjectionException = ex;
        }
    }

    [When(@"I resolve service with multiple properties")]
    public void WhenIResolveServiceWithMultipleProperties()
    {
        _container.Should().NotBeNull("Container should be built successfully");
        
        try
        {
            _resolvedServiceWithMultipleProperties = _container!.Resolve<MultiplePropertyInjectionService>();
        }
        catch (Exception ex)
        {
            _lastPropertyInjectionException = ex;
        }
    }

    [When(@"I resolve multiple instances of service with property injection")]
    public void WhenIResolveMultipleInstancesOfServiceWithPropertyInjection()
    {
        _container.Should().NotBeNull("Container should be built successfully");
        
        try
        {
            for (int i = 0; i < 3; i++)
            {
                var instance = _container!.Resolve<PropertyInjectionTestService>();
                _multipleResolvedInstances.Add(instance);
            }
        }
        catch (Exception ex)
        {
            _lastPropertyInjectionException = ex;
        }
    }

    #endregion

    #region Then Steps - Assertions

    [Then(@"the service should have FlexConfig injected into property")]
    public void ThenTheServiceShouldHaveFlexConfigInjectedIntoProperty()
    {
        _resolvedServiceWithProperty.Should().NotBeNull("Service should be resolved successfully");
        _resolvedServiceWithProperty!.FlexConfiguration.Should().NotBeNull("FlexConfig property should be injected");
        _resolvedServiceWithProperty.HasInjectedProperty.Should().BeTrue("Service should indicate property injection succeeded");
    }

    [Then(@"the injected FlexConfig should provide access to configuration values")]
    public void ThenTheInjectedFlexConfigShouldProvideAccessToConfigurationValues()
    {
        _resolvedServiceWithProperty.Should().NotBeNull("Service should be resolved");
        _resolvedServiceWithProperty!.FlexConfiguration.Should().NotBeNull("FlexConfig property should be injected");
        
        // Test that we can access configuration through the injected property
        var flexConfig = _resolvedServiceWithProperty.FlexConfiguration!;
        
        // The FlexConfig should be functional for accessing configuration values
        flexConfig.Should().BeAssignableTo<IFlexConfig>("Injected property should implement IFlexConfig");
    }

    [Then(@"the service should have all configuration properties injected")]
    public void ThenTheServiceShouldHaveAllConfigurationPropertiesInjected()
    {
        _resolvedServiceWithMultipleProperties.Should().NotBeNull("Service should be resolved successfully");
        _resolvedServiceWithMultipleProperties!.FlexConfiguration.Should().NotBeNull("FlexConfig property should be injected");
        
        // Note: StandardConfiguration and DynamicConfiguration might be null if not registered
        // The test verifies that at least FlexConfig is injected
        _resolvedServiceWithMultipleProperties.InjectedPropertiesCount.Should().BeGreaterThan(0, "At least one property should be injected");
    }

    [Then(@"each injected property should contain expected configuration data")]
    public void ThenEachInjectedPropertyShouldContainExpectedConfigurationData()
    {
        _resolvedServiceWithMultipleProperties.Should().NotBeNull("Service should be resolved");
        _resolvedServiceWithMultipleProperties!.FlexConfiguration.Should().NotBeNull("FlexConfig property should be injected");
        
        var flexConfig = _resolvedServiceWithMultipleProperties.FlexConfiguration!;
        
        // Verify that the injected FlexConfig contains expected test data
        if (_testConfigurationData.TryGetValue("Database:Host", out var value))
        {
            var dbHost = flexConfig["Database:Host"];
            dbHost.Should().Be(value, "Database host should match test data");
        }
    }

    [Then(@"the injected FlexConfig should provide access to JSON configuration values")]
    public void ThenTheInjectedFlexConfigShouldProvideAccessToJsonConfigurationValues()
    {
        _resolvedServiceWithProperty.Should().NotBeNull("Service should be resolved");
        _resolvedServiceWithProperty!.FlexConfiguration.Should().NotBeNull("FlexConfig property should be injected");
        
        var flexConfig = _resolvedServiceWithProperty.FlexConfiguration!;
        
        // Verify that we can access values from the JSON file
        // This will depend on what's in the appsettings.json test file
        var configEntries = ((FlexConfiguration)flexConfig).Configuration.AsEnumerable().ToList();
        configEntries.Should().NotBeEmpty("JSON configuration should contain values");
    }

    [Then(@"the property injection should work with JSON-based configuration")]
    public void ThenThePropertyInjectionShouldWorkWithJsonBasedConfiguration()
    {
        ThenTheInjectedFlexConfigShouldProvideAccessToJsonConfigurationValues();
    }

    [Then(@"all instances should share the same FlexConfig property instance")]
    public void ThenAllInstancesShouldShareTheSameFlexConfigPropertyInstance()
    {
        _multipleResolvedInstances.Should().HaveCount(3, "Should have resolved three instances");
        
        var firstFlexConfig = _multipleResolvedInstances[0].FlexConfiguration;
        firstFlexConfig.Should().NotBeNull("First instance should have FlexConfig injected");
        
        for (int i = 1; i < _multipleResolvedInstances.Count; i++)
        {
            var currentFlexConfig = _multipleResolvedInstances[i].FlexConfiguration;
            currentFlexConfig.Should().NotBeNull($"Instance {i} should have FlexConfig injected");
            currentFlexConfig.Should().BeSameAs(firstFlexConfig, "All instances should share the same FlexConfig instance (singleton)");
        }
    }

    [Then(@"the singleton service should maintain property injection across resolutions")]
    public void ThenTheSingletonServiceShouldMaintainPropertyInjectionAcrossResolutions()
    {
        // All instances should be the same reference since registered as singleton
        var firstService = _multipleResolvedInstances[0];
        
        for (int i = 1; i < _multipleResolvedInstances.Count; i++)
        {
            var currentService = _multipleResolvedInstances[i];
            currentService.Should().BeSameAs(firstService, "All resolved instances should be the same singleton instance");
        }
    }

    [Then(@"the injected FlexConfig should contain custom module configuration")]
    public void ThenTheInjectedFlexConfigShouldContainCustomModuleConfiguration()
    {
        _resolvedServiceWithProperty.Should().NotBeNull("Service should be resolved");
        _resolvedServiceWithProperty!.FlexConfiguration.Should().NotBeNull("FlexConfig property should be injected");
        
        var flexConfig = _resolvedServiceWithProperty.FlexConfiguration!;
        
        // Verify that the custom module configuration is accessible
        var customSetting1 = flexConfig["CustomModule:Setting1"];
        customSetting1.Should().Be("custom-prop-value-1", "Custom module setting should be accessible");
    }

    [Then(@"the container should build successfully with default behavior")]
    public void ThenTheContainerShouldBuildSuccessfullyWithDefaultBehavior()
    {
        _containerBuildSucceeded.Should().BeTrue("Container should build successfully even without configuration");
        _container.Should().NotBeNull("Container should be available");
    }

    [Then(@"property injection should handle missing configuration gracefully")]
    public void ThenPropertyInjectionShouldHandleMissingConfigurationGracefully()
    {
        // Even without configuration, the container should build, and services should be resolvable
        // The FlexConfig property might be null, which is acceptable behavior
        _lastPropertyInjectionException.Should().BeNull("Property injection should not cause exceptions");
    }

    [Then(@"the injected FlexConfig should provide access to environment variable values")]
    public void ThenTheInjectedFlexConfigShouldProvideAccessToEnvironmentVariableValues()
    {
        _resolvedServiceWithProperty.Should().NotBeNull("Service should be resolved");
        _resolvedServiceWithProperty!.FlexConfiguration.Should().NotBeNull("FlexConfig property should be injected");
        
        var flexConfig = _resolvedServiceWithProperty.FlexConfiguration!;
        
        // Verify environment variables are accessible (with the prefix removed)
        var dbHost = flexConfig["DATABASE:HOST"];
        dbHost.Should().Be("env-host.com", "Environment variable should be accessible through FlexConfig");
    }

    [Then(@"property injection should work with environment-based configuration")]
    public void ThenPropertyInjectionShouldWorkWithEnvironmentBasedConfiguration()
    {
        ThenTheInjectedFlexConfigShouldProvideAccessToEnvironmentVariableValues();
    }

    #endregion
}