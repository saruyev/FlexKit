using FlexKit.Configuration.Core;
using FlexKit.Configuration.IntegrationTests.Utils;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Reqnroll;
using System.Dynamic;
// ReSharper disable ClassTooBig
// ReSharper disable MethodTooLong
// ReSharper disable NullableWarningSuppressionIsUsed

namespace FlexKit.Configuration.IntegrationTests.Steps.Configuration;

/// <summary>
/// Step definitions for basic configuration access scenarios.
/// Tests fundamental configuration access patterns including string indexer,
/// dynamic access, and section navigation.
/// </summary>
[Binding]
public class BasicConfigurationSteps
{
    private readonly ScenarioContext _scenarioContext;
    private readonly TestConfigurationBuilder _configurationBuilder;
    private IConfiguration? _configuration;
    private IFlexConfig? _flexConfiguration;
    private string? _lastStringResult;
    private object? _lastDynamicResult;
    private IFlexConfig? _lastSectionResult;
    private readonly List<string> _accessResults = new();

    public BasicConfigurationSteps(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
        _configurationBuilder = new TestConfigurationBuilder(_scenarioContext);
    }

    #region Given Steps - Setup

    [Given(@"I have a configuration source with the following data:")]
    public void GivenIHaveAConfigurationSourceWithTheFollowingData(Table table)
    {
        var configData = new Dictionary<string, string?>();
        
        foreach (var row in table.Rows)
        {
            var key = row["Key"];
            var value = row["Value"];
            configData[key] = value;
        }

        _configuration = _configurationBuilder
            .AddInMemoryCollection(configData)
            .Build();

        _flexConfiguration = _configuration.GetFlexConfiguration();

        // Store in the scenario context for potential use by other steps
        _scenarioContext.Set(_configuration, "Configuration");
        _scenarioContext.Set(_flexConfiguration, "FlexConfiguration");
    }

    [Given(@"I have additional configuration data:")]
    public void GivenIHaveAdditionalConfigurationData(Table table)
    {
        var existingConfig = _scenarioContext.Get<IConfiguration>("Configuration");
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

        _configuration = _configurationBuilder
            .Clear()
            .AddInMemoryCollection(allData)
            .Build();

        _flexConfiguration = _configuration.GetFlexConfiguration();

        // Update scenario context
        _scenarioContext.Set(_configuration, "Configuration");
        _scenarioContext.Set(_flexConfiguration, "FlexConfiguration");
    }

    #endregion

    #region When Steps - Actions

    [When(@"I access the configuration using the string indexer with key ""(.*)""")]
    public void WhenIAccessTheConfigurationUsingTheStringIndexerWithKey(string key)
    {
        _flexConfiguration.Should().NotBeNull("FlexConfiguration should be initialized");
        _lastStringResult = _flexConfiguration![key];
        _accessResults.Add(_lastStringResult ?? "null");
    }

    [When(@"I access the configuration dynamically as ""(.*)""")]
    public void WhenIAccessTheConfigurationDynamically(string expression)
    {
        _flexConfiguration.Should().NotBeNull("FlexConfiguration should be initialized");
        
        // Parse the dynamic expression and navigate through the configuration
        // Expected format: "config.Section.SubSection.Property"
        var parts = expression.Replace("config.", "").Split('.');
        
        dynamic config = _flexConfiguration!;
        object? current = config;

        foreach (var part in parts)
        {
            if (current == null) break;
            
            // Use reflection to get the property value dynamically
            current = GetDynamicProperty(current, part);
        }

        _lastDynamicResult = current;
        _accessResults.Add(current?.ToString() ?? "null");
    }

    [When(@"I access the configuration section ""(.*)"" using numeric indexer (.*)")]
    public void WhenIAccessTheConfigurationSectionUsingNumericIndexer(string sectionName, int index)
    {
        _flexConfiguration.Should().NotBeNull("FlexConfiguration should be initialized");
        
        var section = _configuration!.GetSection(sectionName);
        var sectionFlexConfig = section.GetFlexConfiguration();
        
        _lastSectionResult = sectionFlexConfig[index];
    }

    [When(@"I get the current config for section ""(.*)""")]
    public void WhenIGetTheCurrentConfigForSection(string sectionName)
    {
        _configuration.Should().NotBeNull("Configuration should be initialized");
        _lastSectionResult = _configuration!.CurrentConfig(sectionName);
    }

    [When(@"I get the current config for section ""(.*)"" from the previous result")]
    public void WhenIGetTheCurrentConfigForSectionFromThePreviousResult(string sectionName)
    {
        _lastSectionResult.Should().NotBeNull("Previous section result should not be null");
        _lastSectionResult = _lastSectionResult!.Configuration.CurrentConfig(sectionName);
    }

    [When(@"I get the FlexConfiguration from IConfiguration")]
    public void WhenIGetTheFlexConfigurationFromIConfiguration()
    {
        _configuration.Should().NotBeNull("Configuration should be initialized");
        _flexConfiguration = _configuration!.GetFlexConfiguration();
    }

    [When(@"I access the FlexConfiguration using string indexer with key ""(.*)""")]
    public void WhenIAccessTheFlexConfigurationUsingStringIndexerWithKey(string key)
    {
        _flexConfiguration.Should().NotBeNull("FlexConfiguration should be initialized");
        _lastStringResult = _flexConfiguration![key];
    }

    [When(@"I access the FlexConfiguration dynamically as ""(.*)""")]
    public void WhenIAccessTheFlexConfigurationDynamically(string expression)
    {
        WhenIAccessTheConfigurationDynamically(expression);
    }

    [When(@"I access ""(.*)"" using string indexer")]
    public void WhenIAccessUsingStringIndexer(string key)
    {
        WhenIAccessTheConfigurationUsingTheStringIndexerWithKey(key);
    }

    [When(@"I access ""(.*)"" using dynamic access")]
    public void WhenIAccessUsingDynamicAccess(string expression)
    {
        WhenIAccessTheConfigurationDynamically($"config.{expression}");
    }

    [When(@"I get section ""(.*)"" and access ""(.*)"" key")]
    public void WhenIGetSectionAndAccessKey(string sectionName, string key)
    {
        _configuration.Should().NotBeNull("Configuration should be initialized");
        var section = _configuration!.CurrentConfig(sectionName);
        section.Should().NotBeNull($"Section '{sectionName}' should exist");
        var value = section[key];
        _accessResults.Add(value ?? "null");
    }

    [When(@"I access the section using string indexer with key ""(.*)""")]
    public void WhenIAccessTheSectionUsingStringIndexerWithKey(string key)
    {
        _lastSectionResult.Should().NotBeNull("Section result should not be null");
        _lastStringResult = _lastSectionResult![key];
    }

    #endregion

    #region Then Steps - Assertions

    [Then(@"the returned value should be ""(.*)""")]
    public void ThenTheReturnedValueShouldBe(string expectedValue)
    {
        _lastStringResult.Should().Be(expectedValue);
    }

    [Then(@"the returned value should be null")]
    public void ThenTheReturnedValueShouldBeNull()
    {
        _lastStringResult.Should().BeNull();
    }

    [Then(@"the returned value should be empty")]
    public void ThenTheReturnedValueShouldBeEmpty()
    {
        _lastStringResult.Should().Be("");
    }

    [Then(@"the dynamic result should be ""(.*)""")]
    public void ThenTheDynamicResultShouldBe(string expectedValue)
    {
        var resultString = _lastDynamicResult?.ToString();
        resultString.Should().Be(expectedValue);
    }

    [Then(@"the dynamic result should be null")]
    public void ThenTheDynamicResultShouldBeNull()
    {
        _lastDynamicResult.Should().BeNull();
    }

    [Then(@"the server configuration should have Name ""(.*)""")]
    public void ThenTheServerConfigurationShouldHaveName(string expectedName)
    {
        _lastSectionResult.Should().NotBeNull("Server configuration should not be null");
        var name = _lastSectionResult!["Name"];
        name.Should().Be(expectedName);
    }

    [Then(@"the server configuration should have Host ""(.*)""")]
    public void ThenTheServerConfigurationShouldHaveHost(string expectedHost)
    {
        _lastSectionResult.Should().NotBeNull("Server configuration should not be null");
        var host = _lastSectionResult!["Host"];
        host.Should().Be(expectedHost);
    }

    [Then(@"the server configuration should have Port ""(.*)""")]
    public void ThenTheServerConfigurationShouldHavePort(string expectedPort)
    {
        _lastSectionResult.Should().NotBeNull("Server configuration should not be null");
        var port = _lastSectionResult!["Port"];
        port.Should().Be(expectedPort);
    }

    [Then(@"the section should contain key ""(.*)"" with value ""(.*)""")]
    public void ThenTheSectionShouldContainKeyWithValue(string key, string expectedValue)
    {
        _lastSectionResult.Should().NotBeNull("Section should not be null");
        var value = _lastSectionResult![key];
        value.Should().Be(expectedValue);
    }

    [Then(@"the result should be null")]
    public void ThenTheResultShouldBeNull()
    {
        _lastSectionResult.Should().BeNull();
    }

    [Then(@"the result should not be null")]
    public void ThenTheResultShouldNotBeNull()
    {
        _lastSectionResult.Should().NotBeNull();
    }

    [Then(@"the FlexConfiguration should not be null")]
    public void ThenTheFlexConfigurationShouldNotBeNull()
    {
        _flexConfiguration.Should().NotBeNull();
    }

    [Then(@"the underlying IConfiguration should be accessible")]
    public void ThenTheUnderlyingIConfigurationShouldBeAccessible()
    {
        _flexConfiguration.Should().NotBeNull("FlexConfiguration should not be null");
        _flexConfiguration!.Configuration.Should().NotBeNull();
        _flexConfiguration.Configuration.Should().BeSameAs(_configuration);
    }

    [Then(@"all three access methods should return the same value ""(.*)""")]
    public void ThenAllThreeAccessMethodsShouldReturnTheSameValue(string expectedValue)
    {
        _accessResults.Should().HaveCount(3, "Should have results from three access methods");
        _accessResults.Should().AllSatisfy(result => result.Should().Be(expectedValue));
        
        // Clear results for the next scenario
        _accessResults.Clear();
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Gets a property value from a dynamic object using reflection.
    /// This simulates the dynamic access that would happen in real usage.
    /// </summary>
    /// <param name="target">The target object to access</param>
    /// <param name="propertyName">The property name to access</param>
    /// <returns>The property value or null if not found</returns>
    private static object? GetDynamicProperty(object target, string propertyName)
    {
        if (target is IDynamicMetaObjectProvider)
        {
            // For FlexConfiguration, we need to simulate dynamic access
            if (target is IFlexConfig flexConfig)
            {
                // Use the CurrentConfig method to navigate to the section
                var section = flexConfig.Configuration.CurrentConfig(propertyName);
                return section;
            }
        }

        // Fallback: try to access as a regular property
        var type = target.GetType();
        var property = type.GetProperty(propertyName);
        return property?.GetValue(target);
    }

    /// <summary>
    /// Cleans up resources after each scenario.
    /// </summary>
    [AfterScenario]
    public void Cleanup()
    {
        _accessResults.Clear();
        _lastStringResult = null;
        _lastDynamicResult = null;
        _lastSectionResult = null;
        _configuration = null;
        _flexConfiguration = null;
    }

    #endregion
}