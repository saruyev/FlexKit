using FlexKit.Configuration.Core;
using FlexKit.Configuration.Providers.Yaml.IntegrationTests.Utils;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Reqnroll;
// ReSharper disable ClassTooBig
// ReSharper disable MethodTooLong

namespace FlexKit.Configuration.Providers.Yaml.IntegrationTests.Steps.Configuration;

/// <summary>
/// Step definitions for YAML basic access scenarios.
/// Tests fundamental YAML configuration access patterns including string indexer,
/// dynamic access, and section navigation.
/// Uses distinct step patterns ("yaml module", "utilize yaml", "examine yaml") to avoid conflicts 
/// with other configuration step classes.
/// </summary>
[Binding]
public class YamlBasicAccessSteps(ScenarioContext scenarioContext)
{
    private YamlTestConfigurationBuilder? _yamlConfigurationBuilder;
    private IConfiguration? _yamlConfiguration;
    private IFlexConfig? _yamlFlexConfiguration;
    private string? _lastYamlStringResult;
    private object? _lastYamlDynamicResult;
    private IFlexConfig? _lastYamlSectionResult;
    private readonly List<string> _yamlAccessResults = new();

    #region Given Steps - Setup

    [Given(@"I have initialized a yaml module configuration environment")]
    public void GivenIHaveInitializedAYamlModuleConfigurationEnvironment()
    {
        _yamlConfigurationBuilder = new YamlTestConfigurationBuilder(scenarioContext);
        scenarioContext.Set(_yamlConfigurationBuilder, "YamlConfigurationBuilder");
    }

    [Given(@"I have a yaml module source with the following data:")]
    public void GivenIHaveAYamlModuleSourceWithTheFollowingData(Table table)
    {
        _yamlConfigurationBuilder.Should().NotBeNull("YAML configuration builder should be initialized");
        
        var yamlData = new Dictionary<string, string?>();
        foreach (var row in table.Rows)
        {
            var key = row["Key"];
            var value = row["Value"];
            yamlData[key] = value;
        }

        _yamlConfiguration = _yamlConfigurationBuilder!
            .AddTempYamlFile(yamlData)
            .Build();

        _yamlFlexConfiguration = _yamlConfiguration.GetFlexConfiguration();

        // Store in the scenario context for potential use by other steps
        scenarioContext.Set(_yamlConfiguration, "YamlConfiguration");
        scenarioContext.Set(_yamlFlexConfiguration, "YamlFlexConfiguration");
    }

    [Given(@"I have a yaml module source with content:")]
    public void GivenIHaveAYamlModuleSourceWithContent(string yamlContent)
    {
        _yamlConfigurationBuilder.Should().NotBeNull("YAML configuration builder should be initialized");

        _yamlConfiguration = _yamlConfigurationBuilder!
            .AddTempYamlFile(yamlContent)
            .Build();

        _yamlFlexConfiguration = _yamlConfiguration.GetFlexConfiguration();

        // Store in the scenario context for potential use by other steps
        scenarioContext.Set(_yamlConfiguration, "YamlConfiguration");
        scenarioContext.Set(_yamlFlexConfiguration, "YamlFlexConfiguration");
    }

    [Given(@"I have a yaml module source from file ""(.*)""")]
    public void GivenIHaveAYamlModuleSourceFromFile(string filePath)
    {
        _yamlConfigurationBuilder.Should().NotBeNull("YAML configuration builder should be initialized");

        var normalizedPath = filePath.Replace('/', Path.DirectorySeparatorChar);
        var testDataPath = Path.Combine("TestData", normalizedPath);

        _yamlConfiguration = _yamlConfigurationBuilder!
            .AddYamlFile(testDataPath, optional: false)
            .Build();

        _yamlFlexConfiguration = _yamlConfiguration.GetFlexConfiguration();

        // Store in the scenario context for potential use by other steps
        scenarioContext.Set(_yamlConfiguration, "YamlConfiguration");
        scenarioContext.Set(_yamlFlexConfiguration, "YamlFlexConfiguration");
    }

    [Given(@"I have additional yaml module data:")]
    public void GivenIHaveAdditionalYamlModuleData(Table table)
    {
        var existingConfig = scenarioContext.Get<IConfiguration>("YamlConfiguration");
        var additionalData = new Dictionary<string, string?>();
        
        foreach (var row in table.Rows)
        {
            var key = row["Key"];
            var value = row["Value"];
            additionalData[key] = value;
        }

        // Create a new configuration builder with existing data plus additional data
        var allData = new Dictionary<string, string?>();
        
        // Copy existing configuration data
        foreach (var kvp in existingConfig.AsEnumerable())
        {
            allData[kvp.Key] = kvp.Value;
        }
        
        // Add new data
        foreach (var kvp in additionalData)
        {
            allData[kvp.Key] = kvp.Value;
        }

        _yamlConfiguration = _yamlConfigurationBuilder!
            .Clear()
            .AddTempYamlFile(allData)
            .Build();

        _yamlFlexConfiguration = _yamlConfiguration.GetFlexConfiguration();

        // Update scenario context
        scenarioContext.Set(_yamlConfiguration, "YamlConfiguration");
        scenarioContext.Set(_yamlFlexConfiguration, "YamlFlexConfiguration");
    }

    #endregion

    #region When Steps - Actions

    [When(@"I utilize yaml access with string indexer key ""(.*)""")]
    public void WhenIUtilizeYamlAccessWithStringIndexerKey(string key)
    {
        _yamlFlexConfiguration.Should().NotBeNull("YAML FlexConfiguration should be initialized");
        _lastYamlStringResult = _yamlFlexConfiguration![key];
        _yamlAccessResults.Add($"StringIndexer[{key}] = {_lastYamlStringResult ?? "null"}");
    }

    [When(@"I utilize yaml access with dynamic property ""(.*)""")]
    public void WhenIUtilizeYamlAccessWithDynamicProperty(string propertyPath)
    {
        _yamlFlexConfiguration.Should().NotBeNull("YAML FlexConfiguration should be initialized");
        
        dynamic yamlConfig = _yamlFlexConfiguration!;
        _lastYamlDynamicResult = NavigateToYamlProperty(yamlConfig, propertyPath);
        _yamlAccessResults.Add($"Dynamic[{propertyPath}] = {_lastYamlDynamicResult ?? "null"}");
    }

    [When(@"I utilize yaml section access for ""(.*)""")]
    public void WhenIUtilizeYamlSectionAccessFor(string sectionName)
    {
        _yamlFlexConfiguration.Should().NotBeNull("YAML FlexConfiguration should be initialized");
        _lastYamlSectionResult = _yamlFlexConfiguration.Configuration.GetSection(sectionName).GetFlexConfiguration();
        _yamlAccessResults.Add($"Section[{sectionName}] = {(_lastYamlSectionResult != null ? "Section" : "null")}");
    }

    [When(@"I utilize yaml indexed access for ""(.*)""")]
    public void WhenIUtilizeYamlIndexedAccessFor(string indexedKey)
    {
        _yamlFlexConfiguration.Should().NotBeNull("YAML FlexConfiguration should be initialized");
        _lastYamlStringResult = _yamlFlexConfiguration![indexedKey];
        _yamlAccessResults.Add($"IndexedAccess[{indexedKey}] = {_lastYamlStringResult ?? "null"}");
    }

    [When(@"I utilize yaml dynamic navigation to ""(.*)""")]
    public void WhenIUtilizeYamlDynamicNavigationTo(string navigationPath)
    {
        _yamlFlexConfiguration.Should().NotBeNull("YAML FlexConfiguration should be initialized");
        
        dynamic yamlConfig = _yamlFlexConfiguration!;
        _lastYamlDynamicResult = NavigateToYamlProperty(yamlConfig, navigationPath);
        _yamlAccessResults.Add($"Navigation[{navigationPath}] = {_lastYamlDynamicResult ?? "null"}");
    }

    #endregion

    #region Then Steps - Assertions

    [Then(@"the yaml string result should be ""(.*)""")]
    public void ThenTheYamlStringResultShouldBe(string expectedValue)
    {
        _lastYamlStringResult.Should().Be(expectedValue);
    }

    [Then(@"the yaml string result should be null")]
    public void ThenTheYamlStringResultShouldBeNull()
    {
        _lastYamlStringResult.Should().BeNull();
    }

    [Then(@"the yaml dynamic result should be ""(.*)""")]
    public void ThenTheYamlDynamicResultShouldBe(string expectedValue)
    {
        _lastYamlDynamicResult?.ToString().Should().Be(expectedValue);
    }

    [Then(@"the yaml dynamic result should be null")]
    public void ThenTheYamlDynamicResultShouldBeNull()
    {
        _lastYamlDynamicResult.Should().BeNull();
    }

    [Then(@"the yaml section result should not be null")]
    public void ThenTheYamlSectionResultShouldNotBeNull()
    {
        _lastYamlSectionResult.Should().NotBeNull();
    }

    [Then(@"the yaml section result should be null")]
    public void ThenTheYamlSectionResultShouldBeNull()
    {
        _lastYamlSectionResult.Should().BeNull();
    }

    [Then(@"the yaml section ""(.*)"" should have value ""(.*)""")]
    public void ThenTheYamlSectionShouldHaveValue(string key, string expectedValue)
    {
        _lastYamlSectionResult.Should().NotBeNull();
        _lastYamlSectionResult![key].Should().Be(expectedValue);
    }

    [Then(@"the yaml configuration should contain key ""(.*)""")]
    public void ThenTheYamlConfigurationShouldContainKey(string key)
    {
        _yamlConfiguration.Should().NotBeNull();
        _yamlConfiguration![key].Should().NotBeNull();
    }

    [Then(@"the yaml configuration should not contain key ""(.*)""")]
    public void ThenTheYamlConfigurationShouldNotContainKey(string key)
    {
        _yamlConfiguration.Should().NotBeNull();
        _yamlConfiguration![key].Should().BeNull();
    }

    [Then(@"the yaml access results should include:")]
    public void ThenTheYamlAccessResultsShouldInclude(Table table)
    {
        foreach (var row in table.Rows)
        {
            var expectedEntry = row["Result"];
            _yamlAccessResults.Should().Contain(expectedEntry);
        }
    }

    [Then(@"the yaml dynamic value at ""(.*)"" should be ""(.*)""")]
    public void ThenTheYamlDynamicValueAtShouldBe(string propertyPath, string expectedValue)
    {
        _yamlFlexConfiguration.Should().NotBeNull();
        dynamic yamlConfig = _yamlFlexConfiguration!;
        var actualValue = NavigateToYamlProperty(yamlConfig, propertyPath);
        actualValue?.ToString().Should().Be(expectedValue);
    }

    [Then(@"the yaml dynamic value at ""(.*)"" should be null")]
    public void ThenTheYamlDynamicValueAtShouldBeNull(string propertyPath)
    {
        _yamlFlexConfiguration.Should().NotBeNull();
        dynamic yamlConfig = _yamlFlexConfiguration!;
        var actualValue = NavigateToYamlProperty(yamlConfig, propertyPath);
        actualValue.Should().BeNull();
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Navigates to a property using dynamic access with dot notation.
    /// </summary>
    /// <param name="config">The dynamic configuration object</param>
    /// <param name="propertyPath">The property path with dot notation (e.g., "Database.Host")</param>
    /// <returns>The property value or null if not found</returns>
    private static object? NavigateToYamlProperty(dynamic config, string propertyPath)
    {
        if (config is not IFlexConfig flexConfig) return null;
    
        var properties = propertyPath.Split('.');
        IFlexConfig? current = flexConfig;

        foreach (var property in properties)
        {
            if (current == null) return null;
        
            // Use CurrentConfig, which returns IFlexConfig or null for missing sections
            current = current.Configuration.CurrentConfig(property);
        }

        // Get the actual value from the final section
        if (current?.Configuration is IConfigurationSection section)
        {
            return section.Value;
        }

        return current?.ToString();
    }

    #endregion
}