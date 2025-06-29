using FlexKit.Configuration.Core;
using FlexKit.Configuration.IntegrationTests.Utils;
using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Reqnroll;

namespace FlexKit.Configuration.IntegrationTests.Steps.Sources;

/// <summary>
/// Step definitions for .env file configuration source scenarios.
/// Tests .env file loading, parsing, and integration with FlexKit Configuration.
/// Uses distinct step patterns ("established", "specify", "load") to avoid conflicts 
/// with other configuration step classes.
/// </summary>
[Binding]
public class DotEnvFileSteps(ScenarioContext scenarioContext)
{
    private TestConfigurationBuilder? _dotEnvConfigurationBuilder;
    private IConfiguration? _dotEnvConfiguration;
    private IFlexConfig? _dotEnvFlexConfiguration;
    [UsedImplicitly] public readonly List<string> RegisteredDotEnvFiles = new();
    private readonly List<string> _dynamicDotEnvContent = new();
    private readonly List<string> _baseDotEnvContent = new();
    private readonly List<string> _overrideDotEnvContent = new();
    private Exception? _lastDotEnvException;
    private bool _dotEnvLoadingSucceeded;

    #region Given Steps - Setup

    [Given(@"I have established a \.env file configuration source environment")]
    public void GivenIHaveEstablishedADotEnvFileConfigurationSourceEnvironment()
    {
        _dotEnvConfigurationBuilder = TestConfigurationBuilder.Create(scenarioContext);
        scenarioContext.Set(_dotEnvConfigurationBuilder, "DotEnvConfigurationBuilder");
    }

    #endregion

    #region When Steps - Content Provision

    [When(@"I specify \.env content with basic configuration:")]
    public void WhenISpecifyDotEnvContentWithBasicConfiguration(string dotEnvContent)
    {
        _dotEnvConfigurationBuilder.Should().NotBeNull(".env configuration builder should be established");
        _dynamicDotEnvContent.Add(dotEnvContent);
    }

    [When(@"I specify \.env content with quoted values:")]
    public void WhenISpecifyDotEnvContentWithQuotedValues(string dotEnvContent)
    {
        WhenISpecifyDotEnvContentWithBasicConfiguration(dotEnvContent);
    }

    [When(@"I specify \.env content with escape sequences:")]
    public void WhenISpecifyDotEnvContentWithEscapeSequences(string dotEnvContent)
    {
        WhenISpecifyDotEnvContentWithBasicConfiguration(dotEnvContent);
    }

    [When(@"I specify \.env content with comments and empty lines:")]
    public void WhenISpecifyDotEnvContentWithCommentsAndEmptyLines(string dotEnvContent)
    {
        WhenISpecifyDotEnvContentWithBasicConfiguration(dotEnvContent);
    }

    [When(@"I specify \.env content with special values:")]
    public void WhenISpecifyDotEnvContentWithSpecialValues(string dotEnvContent)
    {
        WhenISpecifyDotEnvContentWithBasicConfiguration(dotEnvContent);
    }

    [When(@"I specify \.env content with complex configuration:")]
    public void WhenISpecifyDotEnvContentWithComplexConfiguration(string dotEnvContent)
    {
        WhenISpecifyDotEnvContentWithBasicConfiguration(dotEnvContent);
    }

    [When(@"I specify \.env content with hierarchical-style configuration:")]
    public void WhenISpecifyDotEnvContentWithHierarchicalStyleConfiguration(string dotEnvContent)
    {
        WhenISpecifyDotEnvContentWithBasicConfiguration(dotEnvContent);
    }

    [When(@"I specify \.env content with invalid format:")]
    public void WhenISpecifyDotEnvContentWithInvalidFormat(string dotEnvContent)
    {
        WhenISpecifyDotEnvContentWithBasicConfiguration(dotEnvContent);
    }

    [When(@"I specify \.env content with base configuration:")]
    public void WhenISpecifyDotEnvContentWithBaseConfiguration(string dotEnvContent)
    {
        _dotEnvConfigurationBuilder.Should().NotBeNull(".env configuration builder should be established");
        _baseDotEnvContent.Add(dotEnvContent);
    }

    [When(@"I specify \.env content with override configuration:")]
    public void WhenISpecifyDotEnvContentWithOverrideConfiguration(string dotEnvContent)
    {
        _dotEnvConfigurationBuilder.Should().NotBeNull(".env configuration builder should be established");
        _overrideDotEnvContent.Add(dotEnvContent);
    }

    #endregion

    #region When Steps - Registration Actions

    [When(@"I add the dynamic \.env content to configuration")]
    [Then(@"I add the dynamic \.env content to configuration")]
    public void WhenIAddTheDynamicDotEnvContentToConfiguration()
    {
        _dotEnvConfigurationBuilder.Should().NotBeNull(".env configuration builder should be established");
        _dynamicDotEnvContent.Should().NotBeEmpty("Dynamic .env content should be specified");
        
        foreach (var dotEnvContent in _dynamicDotEnvContent)
        {
            _dotEnvConfigurationBuilder!.AddTempEnvFile(dotEnvContent, optional: false);
        }
    }

    [When(@"I add base \.env content to configuration")]
    public void WhenIAddBaseDotEnvContentToConfiguration()
    {
        _dotEnvConfigurationBuilder.Should().NotBeNull(".env configuration builder should be established");
        _baseDotEnvContent.Should().NotBeEmpty("Base .env content should be specified");
        
        foreach (var dotEnvContent in _baseDotEnvContent)
        {
            _dotEnvConfigurationBuilder!.AddTempEnvFile(dotEnvContent, optional: false);
        }
    }

    [When(@"I add override \.env content to configuration")]
    public void WhenIAddOverrideDotEnvContentToConfiguration()
    {
        _dotEnvConfigurationBuilder.Should().NotBeNull(".env configuration builder should be established");
        _overrideDotEnvContent.Should().NotBeEmpty("Override .env content should be specified");
        
        foreach (var dotEnvContent in _overrideDotEnvContent)
        {
            _dotEnvConfigurationBuilder!.AddTempEnvFile(dotEnvContent, optional: false);
        }
    }

    [When(@"I load \.env file ""(.*)"" into configuration")]
    public void WhenILoadDotEnvFileIntoConfiguration(string filePath)
    {
        _dotEnvConfigurationBuilder.Should().NotBeNull(".env configuration builder should be established");
        
        // Use the same pattern as JsonConfigurationSteps for consistency
        var normalizedPath = filePath.Replace('/', Path.DirectorySeparatorChar);
        
        if (!File.Exists(normalizedPath))
        {
            throw new FileNotFoundException($"Test data file not found: {normalizedPath}");
        }

        _dotEnvConfigurationBuilder!.AddDotEnvFile(normalizedPath, optional: false);
        RegisteredDotEnvFiles.Add(normalizedPath);
    }

    [When(@"I load non-existent \.env file ""(.*)"" as optional")]
    public void WhenILoadNonExistentDotEnvFileAsOptional(string filePath)
    {
        _dotEnvConfigurationBuilder.Should().NotBeNull(".env configuration builder should be established");
        
        // Use the same simple path normalization as JsonConfigurationSteps
        var normalizedPath = filePath.Replace('/', Path.DirectorySeparatorChar);
        
        // Don't validate the existence for optional files - just add to the builder
        _dotEnvConfigurationBuilder!.AddDotEnvFile(normalizedPath, optional: true);
    }

    [When(@"I load non-existent \.env file ""(.*)"" as required")]
    public void WhenILoadNonExistentDotEnvFileAsRequired(string filePath)
    {
        _dotEnvConfigurationBuilder.Should().NotBeNull(".env configuration builder should be established");
        
        // Use the same simple path normalization as JsonConfigurationSteps
        var normalizedPath = filePath.Replace('/', Path.DirectorySeparatorChar);
        
        // Add as required file - will fail during build if not found
        _dotEnvConfigurationBuilder!.AddDotEnvFile(normalizedPath, optional: false);
    }

    #endregion

    #region When Steps - Build Actions

    [When(@"I build the \.env file configuration")]
    [Then(@"I build the \.env file configuration")]
    public void WhenIBuildTheDotEnvFileConfiguration()
    {
        _dotEnvConfigurationBuilder.Should().NotBeNull(".env configuration builder should be established");
        
        try
        {
            _dotEnvConfiguration = _dotEnvConfigurationBuilder!.Build();
            _dotEnvLoadingSucceeded = true;
            _lastDotEnvException = null;
        }
        catch (Exception ex)
        {
            _dotEnvLoadingSucceeded = false;
            _lastDotEnvException = ex;
        }
    }

    [When(@"I attempt to build the \.env file configuration")]
    public void WhenIAttemptToBuildTheDotEnvFileConfiguration()
    {
        // Same as build but more explicit about expecting potential failure
        WhenIBuildTheDotEnvFileConfiguration();
    }

    [When(@"I generate FlexConfig from \.env configuration")]
    public void WhenIGenerateFlexConfigFromDotEnvConfiguration()
    {
        _dotEnvConfiguration.Should().NotBeNull(".env configuration should be built");
        
        try
        {
            _dotEnvFlexConfiguration = _dotEnvConfiguration!.GetFlexConfiguration();
        }
        catch (Exception ex)
        {
            _lastDotEnvException = ex;
        }
    }

    #endregion

    #region Then Steps - Assertions

    [Then(@"the \.env file configuration loads without errors")]
    public void ThenTheDotEnvFileConfigurationLoadsWithoutErrors()
    {
        _dotEnvLoadingSucceeded.Should().BeTrue(".env configuration loading should succeed");
        _lastDotEnvException.Should().BeNull("No exception should occur during .env configuration loading");
        _dotEnvConfiguration.Should().NotBeNull(".env configuration should be loaded");
    }

    [Then(@"the \.env file configuration fails to load")]
    public void ThenTheDotEnvFileConfigurationFailsToLoad()
    {
        _dotEnvLoadingSucceeded.Should().BeFalse(".env configuration loading should fail");
        _lastDotEnvException.Should().NotBeNull("An exception should occur during .env configuration loading");
    }

    [Then(@"the \.env loading error indicates missing file")]
    public void ThenTheDotEnvLoadingErrorIndicatesMissingFile()
    {
        _lastDotEnvException.Should().NotBeNull("An exception should have occurred");
        _lastDotEnvException.Should().BeOfType<FileNotFoundException>("Exception should indicate missing file");
    }

    [Then(@"the \.env configuration includes ""(.*)"" having value ""(.*)""")]
    public void ThenTheDotEnvConfigurationIncludesHavingValue(string key, string expectedValue)
    {
        _dotEnvConfiguration.Should().NotBeNull(".env configuration should be loaded");
        
        var actualValue = _dotEnvConfiguration![key];
        actualValue.Should().Be(expectedValue, $".env configuration key '{key}' should have the expected value");
    }

    [Then(@"the \.env configuration excludes ""(.*)""")]
    public void ThenTheDotEnvConfigurationExcludes(string key)
    {
        _dotEnvConfiguration.Should().NotBeNull(".env configuration should be loaded");
        
        var actualValue = _dotEnvConfiguration![key];
        actualValue.Should().BeNull($".env configuration should not contain key '{key}'");
    }

    [Then(@"the FlexConfig loads from \.env successfully")]
    public void ThenTheFlexConfigLoadsFromDotEnvSuccessfully()
    {
        _dotEnvFlexConfiguration.Should().NotBeNull("FlexConfig should be created from .env configuration");
        _lastDotEnvException.Should().BeNull("No exception should occur during FlexConfig creation");
    }

    [Then(@"FlexConfig includes ""(.*)"" having value ""(.*)""")]
    public void ThenFlexConfigIncludesHavingValue(string key, string expectedValue)
    {
        _dotEnvFlexConfiguration.Should().NotBeNull("FlexConfig should be loaded");
        
        var actualValue = _dotEnvFlexConfiguration![key];
        actualValue.Should().Be(expectedValue, $"FlexConfig key '{key}' should have the expected value");
    }

    #endregion
}