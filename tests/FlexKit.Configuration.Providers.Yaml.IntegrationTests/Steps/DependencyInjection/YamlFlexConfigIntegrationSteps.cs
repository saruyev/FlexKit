using Autofac;
using FlexKit.Configuration.Core;
using FlexKit.Configuration.Providers.Yaml.IntegrationTests.Utils;
using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Reqnroll;
// ReSharper disable TooManyDeclarations
// ReSharper disable MethodTooLong
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassTooBig

namespace FlexKit.Configuration.Providers.Yaml.IntegrationTests.Steps.DependencyInjection;

/// <summary>
/// Step definitions for YAML FlexConfig integration with Autofac dependency injection scenarios.
/// Tests YAML configuration integration with FlexConfig and Autofac container registration,
/// including service resolution, property injection, and configuration access patterns.
/// Uses distinct step patterns ("integration module") to avoid conflicts with other step classes.
/// </summary>
[Binding]
public class YamlFlexConfigIntegrationSteps(ScenarioContext scenarioContext)
{
    private YamlTestConfigurationBuilder? _integrationBuilder;
    private ContainerBuilder? _containerBuilder;
    private IContainer? _container;
    private IConfiguration? _integrationConfiguration;
    private IFlexConfig? _integrationFlexConfiguration;
    private Exception? _lastIntegrationException;
    private readonly List<string> _integrationValidationResults = new();
    
    // Test services for dependency injection
    private TestServiceWithIFlexConfig? _resolvedServiceWithIFlexConfig;
    private TestServiceWithDynamicConfig? _resolvedServiceWithDynamicConfig;
    private TestServiceWithPropertyInjection? _resolvedServiceWithPropertyInjection;
    private IFlexConfig? _resolvedIFlexConfig;
    private dynamic? _resolvedDynamicConfig;

    #region Test Service Classes

    /// <summary>
    /// Test service that depends on IFlexConfig through constructor injection.
    /// </summary>
    public class TestServiceWithIFlexConfig(IFlexConfig configuration)
    {
        public IFlexConfig Configuration { get; } = configuration;
        
        public string? GetConfigValue(string key) => Configuration[key];
        
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        public bool HasConfiguration() => Configuration != null;
    }

    /// <summary>
    /// Test service that depends on dynamic configuration through constructor injection.
    /// </summary>
    public class TestServiceWithDynamicConfig(dynamic configuration)
    {
        public dynamic Configuration { get; } = configuration;
        
        public object? GetDynamicValue(string path) => 
            YamlTestConfigurationBuilder.GetDynamicProperty(Configuration, path);
        
        public bool HasDynamicConfiguration() => Configuration != null;
    }

    /// <summary>
    /// Test service with FlexConfig property injection.
    /// </summary>
    public class TestServiceWithPropertyInjection
    {
        public IFlexConfig? FlexConfig { get; [UsedImplicitly] set; }
        public string ServiceName { get; set; } = "YamlIntegrationTestService";
        
        public bool HasPropertyInjection() => FlexConfig != null;
        public string? GetPropertyValue(string key) => FlexConfig?[key];
    }

    #endregion

    #region Given Steps - Setup

    [Given(@"I have established an integration module environment")]
    public void GivenIHaveEstablishedAnIntegrationModuleEnvironment()
    {
        _integrationBuilder = new YamlTestConfigurationBuilder(scenarioContext);
        _containerBuilder = new ContainerBuilder();
        
        scenarioContext.Set(_integrationBuilder, "IntegrationBuilder");
        scenarioContext.Set(_containerBuilder, "ContainerBuilder");
    }

    [Given(@"I have an integration module with YAML configuration from file ""(.*)""")]
    public void GivenIHaveAnIntegrationModuleWithYamlConfigurationFromFile(string filePath)
    {
        _integrationBuilder.Should().NotBeNull("Integration builder should be established");

        var normalizedPath = filePath.Replace('/', Path.DirectorySeparatorChar);
        var testDataPath = Path.Combine("TestData", normalizedPath);

        _integrationBuilder!.AddYamlFile(testDataPath, optional: false);
        
        scenarioContext.Set(_integrationBuilder, "IntegrationBuilder");
    }

    [Given(@"I have an integration module with YAML configuration data:")]
    public void GivenIHaveAnIntegrationModuleWithYamlConfigurationData(string yamlContent)
    {
        _integrationBuilder.Should().NotBeNull("Integration builder should be established");

        _integrationBuilder!.AddTempYamlFile(yamlContent, optional: false);
        
        scenarioContext.Set(_integrationBuilder, "IntegrationBuilder");
    }

    [Given(@"I have an integration module with multiple YAML sources:")]
    public void GivenIHaveAnIntegrationModuleWithMultipleYamlSources(Table table)
    {
        _integrationBuilder.Should().NotBeNull("Integration builder should be established");

        foreach (var row in table.Rows)
        {
            var filePath = row["FilePath"];
            var optional = bool.Parse(row["Optional"]);
            
            var normalizedPath = filePath.Replace('/', Path.DirectorySeparatorChar);
            var testDataPath = Path.Combine("TestData", normalizedPath);
            
            _integrationBuilder!.AddYamlFile(testDataPath, optional);
        }
        
        scenarioContext.Set(_integrationBuilder, "IntegrationBuilder");
    }

    [Given(@"I have an integration module with configuration hierarchy:")]
    public void GivenIHaveAnIntegrationModuleWithConfigurationHierarchy(Table table)
    {
        _integrationBuilder.Should().NotBeNull("Integration builder should be established");
        
        var configData = new Dictionary<string, string?>();
        foreach (var row in table.Rows)
        {
            configData[row["Key"]] = row["Value"];
        }

        _integrationBuilder!.AddTempYamlFile(configData, optional: false);
        
        scenarioContext.Set(_integrationBuilder, "IntegrationBuilder");
    }

    #endregion

    #region When Steps - Actions

    [When(@"I configure integration module FlexConfig with Autofac")]
    public void WhenIConfigureIntegrationModuleFlexConfigWithAutofac()
    {
        _integrationBuilder.Should().NotBeNull("Integration builder should be established");
        _containerBuilder.Should().NotBeNull("Container builder should be established");

        try
        {
            // Register FlexConfig with Autofac using AddFlexConfig pattern
            _containerBuilder!.AddFlexConfig(config => 
            {
                // Copy all sources from the test configuration builder
                foreach (var source in _integrationBuilder!.Sources)
                {
                    config.AddSource(source);
                }
            });

            // Build the test configuration for validation purposes
            _integrationConfiguration = _integrationBuilder!.Build();
            _integrationFlexConfiguration = _integrationConfiguration.GetFlexConfiguration();

            scenarioContext.Set(_integrationConfiguration, "IntegrationConfiguration");
            scenarioContext.Set(_integrationFlexConfiguration, "IntegrationFlexConfiguration");
        }
        catch (Exception ex)
        {
            _lastIntegrationException = ex;
            scenarioContext.Set(ex, "IntegrationException");
        }
    }

    [When(@"I configure integration module with property injection enabled")]
    public void WhenIConfigureIntegrationModuleWithPropertyInjectionEnabled()
    {
        _containerBuilder.Should().NotBeNull("Container builder should be established");

        // Register the ConfigurationModule for property injection
        _containerBuilder!.RegisterModule<ConfigurationModule>();
    }

    [When(@"I register integration module test services")]
    public void WhenIRegisterIntegrationModuleTestServices()
    {
        _containerBuilder.Should().NotBeNull("Container builder should be established");

        // Register test services
        _containerBuilder!.RegisterType<TestServiceWithIFlexConfig>()
            .AsSelf()
            .SingleInstance();

        _containerBuilder.RegisterType<TestServiceWithDynamicConfig>()
            .AsSelf()
            .SingleInstance();

        _containerBuilder.RegisterType<TestServiceWithPropertyInjection>()
            .AsSelf()
            .SingleInstance()
            .PropertiesAutowired(); // Enable property injection
    }

    [When(@"I build integration module Autofac container")]
    public void WhenIBuildIntegrationModuleAutofacContainer()
    {
        _containerBuilder.Should().NotBeNull("Container builder should be established");

        try
        {
            _container = _containerBuilder!.Build();
            scenarioContext.Set(_container, "Container");
        }
        catch (Exception ex)
        {
            _lastIntegrationException = ex;
            scenarioContext.Set(ex, "IntegrationException");
        }
    }

    [When(@"I resolve integration module services from container")]
    public void WhenIResolveIntegrationModuleServicesFromContainer()
    {
        _container.Should().NotBeNull("Container should be built");

        try
        {
            // Resolve FlexConfig directly
            _resolvedIFlexConfig = _container!.Resolve<IFlexConfig>();
            _resolvedDynamicConfig = _resolvedIFlexConfig;

            // Resolve services that depend on FlexConfig
            _resolvedServiceWithIFlexConfig = _container.Resolve<TestServiceWithIFlexConfig>();
            _resolvedServiceWithDynamicConfig = _container.Resolve<TestServiceWithDynamicConfig>();
            _resolvedServiceWithPropertyInjection = _container.Resolve<TestServiceWithPropertyInjection>();

            _integrationValidationResults.Add($"IFlexConfig resolved: {_resolvedIFlexConfig != null}");
            _integrationValidationResults.Add($"Dynamic config resolved: {_resolvedDynamicConfig != null}");
            _integrationValidationResults.Add($"Service with IFlexConfig resolved: {_resolvedServiceWithIFlexConfig != null}");
            _integrationValidationResults.Add($"Service with dynamic resolved: {_resolvedServiceWithDynamicConfig != null}");
            _integrationValidationResults.Add($"Service with property injection resolved: {_resolvedServiceWithPropertyInjection != null}");
        }
        catch (Exception ex)
        {
            _lastIntegrationException = ex;
            scenarioContext.Set(ex, "IntegrationException");
        }
    }

    [When(@"I validate integration module configuration access patterns")]
    public void WhenIValidateIntegrationModuleConfigurationAccessPatterns()
    {
        _resolvedIFlexConfig.Should().NotBeNull("IFlexConfig should be resolved");

        // Test string indexer access
        var stringValue = _resolvedIFlexConfig!["app:name"];
        _integrationValidationResults.Add($"StringIndexer app:name = {stringValue}");

        // Test dynamic access
        dynamic dynamicConfig = _resolvedIFlexConfig;
        var dynamicValue = YamlTestConfigurationBuilder.GetDynamicProperty(dynamicConfig, "app");
        var appName = YamlTestConfigurationBuilder.GetDynamicProperty(dynamicValue, "name");
        _integrationValidationResults.Add($"DynamicAccess app.name = {appName}");

        // Test section navigation
        var section = _resolvedIFlexConfig.Configuration.GetSection("database");
        var sectionFlexConfig = section.GetFlexConfiguration();
        var dbHost = sectionFlexConfig["host"];
        _integrationValidationResults.Add($"SectionAccess database.host = {dbHost}");

        // Test array access
        var firstFeature = _resolvedIFlexConfig["features:0"];
        _integrationValidationResults.Add($"ArrayAccess features[0] = {firstFeature}");
    }

    [When(@"I validate integration module service dependency injection")]
    public void WhenIValidateIntegrationModuleServiceDependencyInjection()
    {
        _resolvedServiceWithIFlexConfig.Should().NotBeNull("Service with IFlexConfig should be resolved");
        _resolvedServiceWithDynamicConfig.Should().NotBeNull("Service with dynamic config should be resolved");

        // Test constructor injection with IFlexConfig
        var hasIFlexConfig = _resolvedServiceWithIFlexConfig!.HasConfiguration();
        var configValue = _resolvedServiceWithIFlexConfig.GetConfigValue("app:name");
        _integrationValidationResults.Add($"Constructor IFlexConfig injection: {hasIFlexConfig}");
        _integrationValidationResults.Add($"Constructor IFlexConfig value: {configValue}");

        // Test constructor injection with dynamic config
        var hasDynamicConfig = _resolvedServiceWithDynamicConfig!.HasDynamicConfiguration();
        var dynamicValue = _resolvedServiceWithDynamicConfig.GetDynamicValue("app");
        _integrationValidationResults.Add($"Constructor dynamic injection: {hasDynamicConfig}");
        _integrationValidationResults.Add($"Constructor dynamic value: {dynamicValue != null}");
    }

    [When(@"I validate integration module property injection")]
    public void WhenIValidateIntegrationModulePropertyInjection()
    {
        _resolvedServiceWithPropertyInjection.Should().NotBeNull("Service with property injection should be resolved");

        // Test property injection
        var hasPropertyInjection = _resolvedServiceWithPropertyInjection!.HasPropertyInjection();
        var propertyValue = _resolvedServiceWithPropertyInjection.GetPropertyValue("app:name");
        _integrationValidationResults.Add($"Property injection: {hasPropertyInjection}");
        _integrationValidationResults.Add($"Property injection value: {propertyValue}");
    }

    [When(@"I test integration module advanced YAML features")]
    public void WhenITestIntegrationModuleAdvancedYamlFeatures()
    {
        _resolvedIFlexConfig.Should().NotBeNull("IFlexConfig should be resolved");

        // Test boolean and null handling
        var boolValue = _resolvedIFlexConfig!["settings:enabled"];
        var nullValue = _resolvedIFlexConfig["settings:optional"];
        _integrationValidationResults.Add($"Boolean value: {boolValue}");
        _integrationValidationResults.Add($"Null value: {nullValue ?? "null"}");

        // Test numeric formats
        var intValue = _resolvedIFlexConfig["settings:maxConnections"];
        var floatValue = _resolvedIFlexConfig["settings:timeout"];
        _integrationValidationResults.Add($"Integer value: {intValue}");
        _integrationValidationResults.Add($"Float value: {floatValue}");

        // Test multi-line strings (if configured)
        var description = _resolvedIFlexConfig["app:description"];
        if (description != null)
        {
            _integrationValidationResults.Add($"Multi-line string length: {description.Length}");
        }
    }

    #endregion

    #region Then Steps - Assertions

    [Then(@"the integration module should be configured successfully")]
    public void ThenTheIntegrationModuleShouldBeConfiguredSuccessfully()
    {
        _integrationConfiguration.Should().NotBeNull("Integration configuration should be built");
        _integrationFlexConfiguration.Should().NotBeNull("Integration FlexConfiguration should be available");
        _container.Should().NotBeNull("Autofac container should be built");
        _lastIntegrationException.Should().BeNull("No exceptions should occur during integration setup");
    }

    [Then(@"the integration module should resolve FlexConfig from Autofac")]
    public void ThenTheIntegrationModuleShouldResolveFlexConfigFromAutofac()
    {
        _resolvedIFlexConfig.Should().NotBeNull("IFlexConfig should be resolvable from container");
        _resolvedDynamicConfig!.Should().NotBeNull("Dynamic config should be resolvable from container");
        
        // Verify the resolved config contains expected data
        _integrationValidationResults.Should().Contain(result => result.Contains("IFlexConfig resolved: True"));
        _integrationValidationResults.Should().Contain(result => result.Contains("Dynamic config resolved: True"));
    }

    [Then(@"the integration module should support all FlexConfig access patterns")]
    public void ThenTheIntegrationModuleShouldSupportAllFlexConfigAccessPatterns()
    {
        // Verify all access patterns worked
        _integrationValidationResults.Should().Contain(result => result.StartsWith("StringIndexer app:name ="));
        _integrationValidationResults.Should().Contain(result => result.StartsWith("DynamicAccess app.name ="));
        _integrationValidationResults.Should().Contain(result => result.StartsWith("SectionAccess database.host ="));
        _integrationValidationResults.Should().Contain(result => result.StartsWith("ArrayAccess features[0] ="));
    }

    [Then(@"the integration module should inject FlexConfig into services")]
    public void ThenTheIntegrationModuleShouldInjectFlexConfigIntoServices()
    {
        _resolvedServiceWithIFlexConfig.Should().NotBeNull("Service with IFlexConfig dependency should be resolved");
        _resolvedServiceWithDynamicConfig.Should().NotBeNull("Service with dynamic dependency should be resolved");
        
        // Verify dependency injection worked
        _integrationValidationResults.Should().Contain(result => result.Contains("Constructor IFlexConfig injection: True"));
        _integrationValidationResults.Should().Contain(result => result.Contains("Constructor dynamic injection: True"));
        _integrationValidationResults.Should().Contain(result => result.StartsWith("Constructor IFlexConfig value:"));
        _integrationValidationResults.Should().Contain(result => result.Contains("Constructor dynamic value: True"));
    }

    [Then(@"the integration module should support property injection")]
    public void ThenTheIntegrationModuleShouldSupportPropertyInjection()
    {
        _resolvedServiceWithPropertyInjection.Should().NotBeNull("Service with property injection should be resolved");
        
        // Verify property injection worked
        _integrationValidationResults.Should().Contain(result => result.Contains("Property injection: True"));
        _integrationValidationResults.Should().Contain(result => result.StartsWith("Property injection value:"));
    }

    [Then(@"the integration module should handle YAML data types correctly")]
    public void ThenTheIntegrationModuleShouldHandleYamlDataTypesCorrectly()
    {
        // Verify YAML-specific features are handled correctly
        _integrationValidationResults.Should().Contain(result => result.StartsWith("Boolean value:"));
        _integrationValidationResults.Should().Contain(result => result.StartsWith("Integer value:"));
        _integrationValidationResults.Should().Contain(result => result.StartsWith("Float value:"));
    }

    [Then(@"the integration module should provide FlexConfig dynamic access")]
    public void ThenTheIntegrationModuleShouldProvideFlexConfigDynamicAccess()
    {
        _resolvedIFlexConfig.Should().NotBeNull("IFlexConfig should be resolved");
        
        // Test that the resolved FlexConfig supports dynamic access
        dynamic dynamicConfig = _resolvedIFlexConfig!;
        dynamicConfig.Should().NotBeNull("Dynamic access should be supported");
        
        // Verify dynamic access results
        _integrationValidationResults.Should().Contain(result => result.StartsWith("DynamicAccess app.name ="));
    }

    [Then(@"the integration module should maintain configuration consistency")]
    public void ThenTheIntegrationModuleShouldMaintainConfigurationConsistency()
    {
        // Verify that all resolved instances contain the same configuration data
        var directValue = _resolvedIFlexConfig!["app:name"];
        var serviceValue = _resolvedServiceWithIFlexConfig!.GetConfigValue("app:name");
        var propertyValue = _resolvedServiceWithPropertyInjection!.GetPropertyValue("app:name");
        
        directValue.Should().Be(serviceValue, "Direct resolution and constructor injection should have same values");
        directValue.Should().Be(propertyValue, "Direct resolution and property injection should have same values");
    }

    [Then(@"the integration module should fail with YAML parsing error")]
    public void ThenTheIntegrationModuleShouldFailWithYamlParsingError()
    {
        _lastIntegrationException.Should().NotBeNull("An exception should have occurred");
        _lastIntegrationException!.Message.Should().Contain("Failed to parse YAML", "Error should indicate YAML parsing failure");
    }

    [Then(@"the integration module should support multiple YAML sources")]
    public void ThenTheIntegrationModuleShouldSupportMultipleYamlSources()
    {
        _integrationConfiguration.Should().NotBeNull("Configuration should be built from multiple sources");
        
        // Verify configuration values from different sources are accessible
        var valueFromSource1 = _resolvedIFlexConfig!["app:name"];
        var valueFromSource2 = _resolvedIFlexConfig["database:host"];
        
        valueFromSource1.Should().NotBeNull("Value from first YAML source should be accessible");
        valueFromSource2.Should().NotBeNull("Value from second YAML source should be accessible");
    }

    #endregion
}