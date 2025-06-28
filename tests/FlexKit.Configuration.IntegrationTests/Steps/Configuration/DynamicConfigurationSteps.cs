using FlexKit.Configuration.Core;
using FlexKit.Configuration.Conversion;
using FlexKit.Configuration.IntegrationTests.Utils;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Reqnroll;
using System.Diagnostics;
using Xunit;

namespace FlexKit.Configuration.IntegrationTests.Steps.Configuration;

/// <summary>
/// Step definitions for dynamic configuration access scenarios.
/// Tests advanced dynamic access patterns, property navigation, and type conversion.
/// Uses completely different step patterns from BasicConfigurationSteps to avoid conflicts.
/// </summary>
[Binding]
public class DynamicConfigurationSteps
{
    private readonly ScenarioContext _scenarioContext;
    private readonly TestConfigurationBuilder _configurationBuilder;
    private IConfiguration? _configuration;
    private IFlexConfig? _flexConfiguration;
    private object? _lastPropertyValue;
    private object? _lastConvertedValue;
    private string? _lastStringResult;
    private IFlexConfig? _lastSection;
    private object? _currentNavigationContext;
    private readonly List<object?> _navigationResults = new();
    private readonly List<long> _performanceMetrics = new();
    private string? _traditionalAccessResult;

    public DynamicConfigurationSteps(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
        _configurationBuilder = new TestConfigurationBuilder(_scenarioContext);
    }

    #region Given Steps - Setup

    [Given(@"I setup a complex configuration structure:")]
    public void GivenISetupAComplexConfigurationStructure(Table table)
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

        // Store in scenario context
        _scenarioContext.Set(_configuration, "DynamicConfiguration");
        _scenarioContext.Set(_flexConfiguration, "DynamicFlexConfiguration");
    }

    #endregion

    #region When Steps - Navigation Actions

    [When(@"I navigate dynamically to ""(.*)""")]
    public void WhenINavigateDynamicallyTo(string propertyPath)
    {
        _flexConfiguration.Should().NotBeNull("FlexConfiguration should be initialized");
        
        dynamic config = _flexConfiguration!;
        _lastPropertyValue = NavigateToProperty(config, propertyPath);
    }

    [When(@"I get dynamic section ""(.*)""")]
    public void WhenIGetDynamicSection(string sectionPath)
    {
        _flexConfiguration.Should().NotBeNull("FlexConfiguration should be initialized");
        
        dynamic config = _flexConfiguration!;
        var sectionResult = NavigateToProperty(config, sectionPath);
        _lastSection = sectionResult as IFlexConfig;
    }

    [When(@"I start navigation at root")]
    public void WhenIStartNavigationAtRoot()
    {
        _flexConfiguration.Should().NotBeNull("FlexConfiguration should be initialized");
        dynamic config = _flexConfiguration!;
        _currentNavigationContext = config;
    }

    [When(@"I move to section ""(.*)""")]
    public void WhenIMoveToSection(string sectionName)
    {
        _currentNavigationContext.Should().NotBeNull("Navigation context should not be null");
        _currentNavigationContext = GetPropertyFromContext(_currentNavigationContext!, sectionName);
    }

    [When(@"I get final property ""(.*)""")]
    public void WhenIGetFinalProperty(string propertyName)
    {
        _currentNavigationContext.Should().NotBeNull("Navigation context should not be null");
        _lastPropertyValue = GetPropertyFromContext(_currentNavigationContext!, propertyName);
    }

    [When(@"I navigate to property ""(.*)"" as integer")]
    public void WhenINavigateToPropertyAsInteger(string propertyPath)
    {
        WhenINavigateDynamicallyTo(propertyPath);
        var stringValue = _lastPropertyValue?.ToString();
        _lastConvertedValue = stringValue.ToType<int>();
    }

    [When(@"I navigate to property ""(.*)"" as boolean")]
    public void WhenINavigateToPropertyAsBoolean(string propertyPath)
    {
        WhenINavigateDynamicallyTo(propertyPath);
        var stringValue = _lastPropertyValue?.ToString();
        _lastConvertedValue = stringValue.ToType<bool>();
    }

    [When(@"I navigate to property ""(.*)"" as decimal")]
    public void WhenINavigateToPropertyAsDecimal(string propertyPath)
    {
        WhenINavigateDynamicallyTo(propertyPath);
        var stringValue = _lastPropertyValue?.ToString();
        _lastConvertedValue = stringValue.ToType<decimal>();
    }

    [When(@"I navigate to property ""(.*)"" and convert to string")]
    public void WhenINavigateToPropertyAndConvertToString(string propertyPath)
    {
        WhenINavigateDynamicallyTo(propertyPath);
        _lastStringResult = _lastPropertyValue?.ToString();
    }

    [When(@"I access traditionally ""(.*)""")]
    public void WhenIAccessTraditionally(string key)
    {
        _flexConfiguration.Should().NotBeNull("FlexConfiguration should be initialized");
        _traditionalAccessResult = _flexConfiguration![key];
    }

    [When(@"I convert the result to string representation")]
    public void WhenIConvertTheResultToStringRepresentation()
    {
        _lastStringResult = _lastPropertyValue?.ToString();
    }

    #endregion

    #region When Steps - Performance Actions

    [When(@"I perform multiple dynamic navigations to ""(.*)""")]
    public void WhenIPerformMultipleDynamicNavigationsTo(string propertyPath)
    {
        for (int i = 0; i < 5; i++)
        {
            WhenINavigateDynamicallyTo(propertyPath);
            _navigationResults.Add(_lastPropertyValue?.ToString());
        }
    }

    [When(@"I perform (\d+) dynamic property accesses")]
    public void WhenIPerformMultipleDynamicPropertyAccesses(int count)
    {
        _flexConfiguration.Should().NotBeNull("FlexConfiguration should be initialized");
        dynamic config = _flexConfiguration!;
        
        var stopwatch = Stopwatch.StartNew();
        
        for (int i = 0; i < count; i++)
        {
            var iterationStart = stopwatch.ElapsedMilliseconds;
            
            // Perform various property accesses
            var result1 = NavigateToProperty(config, "Database.Primary.ConnectionString")?.ToString();
            var result2 = NavigateToProperty(config, "External.PaymentGateway.ApiKey")?.ToString();
            var result3 = NavigateToProperty(config, "Features.Payment.Enabled")?.ToString();
            
            var iterationEnd = stopwatch.ElapsedMilliseconds;
            _performanceMetrics.Add(iterationEnd - iterationStart);
            
            // Store some results for validation
            if (i % 10 == 0)
            {
                _navigationResults.Add(result1);
                _navigationResults.Add(result2);
                _navigationResults.Add(result3);
            }
        }
        
        stopwatch.Stop();
    }

    #endregion

    #region Then Steps - Assertions

    [Then(@"the property value should be ""(.*)""")]
    public void ThenThePropertyValueShouldBe(string expectedValue)
    {
        var actualValue = _lastPropertyValue?.ToString();
        actualValue.Should().Be(expectedValue);
    }

    [Then(@"the property value should be null")]
    public void ThenThePropertyValueShouldBeNull()
    {
        _lastPropertyValue.Should().BeNull();
    }

    [Then(@"the section should be valid")]
    public void ThenTheSectionShouldBeValid()
    {
        _lastSection.Should().NotBeNull("Section should not be null");
        _lastSection.Should().BeAssignableTo<IFlexConfig>("Section should be a valid FlexConfig");
    }

    [Then(@"the section should contain ""(.*)"" with value ""(.*)""")]
    public void ThenTheSectionShouldContainWithValue(string key, string expectedValue)
    {
        _lastSection.Should().NotBeNull("Section should not be null");
        var actualValue = _lastSection![key];
        actualValue.Should().Be(expectedValue);
    }

    [Then(@"the navigation result should be ""(.*)""")]
    public void ThenTheNavigationResultShouldBe(string expectedValue)
    {
        var actualValue = _lastPropertyValue?.ToString();
        actualValue.Should().Be(expectedValue);
    }

    [Then(@"the integer value should be (.*)")]
    public void ThenTheIntegerValueShouldBe(int expectedValue)
    {
        _lastConvertedValue.Should().Be(expectedValue);
    }

    [Then(@"the boolean value should be (.*)")]
    public void ThenTheBooleanValueShouldBe(bool expectedValue)
    {
        _lastConvertedValue.Should().Be(expectedValue);
    }

    [Then(@"the decimal value should be (.*)")]
    public void ThenTheDecimalValueShouldBe(decimal expectedValue)
    {
        _lastConvertedValue.Should().Be(expectedValue);
    }

    [Then(@"both approaches should yield the same result")]
    public void ThenBothApproachesShouldYieldTheSameResult()
    {
        var dynamicResult = _lastPropertyValue?.ToString();
        dynamicResult.Should().Be(_traditionalAccessResult, "Dynamic and traditional access should return the same value");
    }

    [Then(@"the text result should be ""(.*)""")]
    public void ThenTheTextResultShouldBe(string expectedValue)
    {
        _lastStringResult.Should().Be(expectedValue);
    }

    [Then(@"no errors should occur")]
    public void ThenNoErrorsShouldOccur()
    {
        // This is implicitly verified by reaching this step
        Assert.True(true, "No errors occurred during dynamic navigation");
    }

    [Then(@"all navigation attempts should return consistent results")]
    public void ThenAllNavigationAttemptsShouldReturnConsistentResults()
    {
        _navigationResults.Should().NotBeEmpty("Should have navigation results");
        _navigationResults.Should().AllSatisfy(result => 
            result.Should().Be(_navigationResults[0], "All navigation results should be consistent"));
    }

    [Then(@"all access operations should complete successfully")]
    public void ThenAllAccessOperationsShouldCompleteSuccessfully()
    {
        _performanceMetrics.Should().NotBeEmpty("Should have performance metrics");
        _navigationResults.Should().NotBeEmpty("Should have navigation results");
        
        // Verify we got expected results
        _navigationResults.Should().Contain("Server=primary.db.com;Database=AppPrimary;");
        _navigationResults.Should().Contain("pk_test_dynamic_12345");
        _navigationResults.Should().Contain("true");
    }

    [Then(@"the string output should be ""(.*)""")]
    public void ThenTheStringOutputShouldBe(string expectedValue)
    {
        _lastStringResult.Should().Be(expectedValue);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Navigates to a property using dot notation path.
    /// </summary>
    /// <param name="root">The root configuration object</param>
    /// <param name="propertyPath">The dot-separated property path</param>
    /// <returns>The property value or null if not found</returns>
    private static object? NavigateToProperty(object? root, string propertyPath)
    {
        if (root == null || string.IsNullOrEmpty(propertyPath))
            return null;

        var parts = propertyPath.Split('.');
        object? current = root;

        foreach (var part in parts)
        {
            if (current == null) 
                return null;
                
            current = GetPropertyFromContext(current, part);
        }

        return current;
    }

    /// <summary>
    /// Gets a property from the current navigation context.
    /// </summary>
    /// <param name="context">The current navigation context</param>
    /// <param name="propertyName">The property name to access</param>
    /// <returns>The property value or null if not found</returns>
    private static object? GetPropertyFromContext(object context, string propertyName)
    {
        if (context is IFlexConfig flexConfig)
        {
            // Use CurrentConfig for FlexConfig navigation
            return flexConfig.Configuration.CurrentConfig(propertyName);
        }

        // For other types, return null (safe navigation)
        return null;
    }

    /// <summary>
    /// Cleans up resources after each scenario.
    /// </summary>
    [AfterScenario]
    public void Cleanup()
    {
        _navigationResults.Clear();
        _performanceMetrics.Clear();
        _lastPropertyValue = null;
        _lastConvertedValue = null;
        _lastStringResult = null;
        _lastSection = null;
        _currentNavigationContext = null;
        _traditionalAccessResult = null;
        _configuration = null;
        _flexConfiguration = null;
    }

    #endregion
}