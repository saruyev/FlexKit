using FlexKit.Configuration.Core;
using FlexKit.Configuration.IntegrationTests.Utils;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Reqnroll;
// ReSharper disable NullableWarningSuppressionIsUsed

namespace FlexKit.Configuration.IntegrationTests.Steps.Sources;

/// <summary>
/// Step definitions for environment variable configuration source scenarios.
/// Tests environment variable loading, hierarchical key mapping, prefix filtering,
/// and integration with FlexKit Configuration.
/// Uses distinct step patterns ("setup", "register", "build") to avoid conflicts 
/// with other configuration step classes.
/// </summary>
[Binding]
public class EnvironmentVariableSteps(ScenarioContext scenarioContext)
{
    private TestConfigurationBuilder? _envConfigurationBuilder;
    private IConfiguration? _envConfiguration;
    private IFlexConfig? _envFlexConfiguration;
    private readonly Dictionary<string, string?> _environmentVariables = new();
    private readonly Dictionary<string, string?> _additionalEnvironmentVariables = new();
    private readonly Dictionary<string, string?> _baseConfigurationData = new();
    private Exception? _lastEnvException;
    private bool _envLoadingSucceeded;

    #region Given Steps - Setup

    [Given(@"I have prepared an environment variable configuration source environment")]
    public void GivenIHavePreparedAnEnvironmentVariableConfigurationSourceEnvironment()
    {
        _envConfigurationBuilder = TestConfigurationBuilder.Create(scenarioContext);
        scenarioContext.Set(_envConfigurationBuilder, "EnvironmentConfigurationBuilder");
    }

    #endregion

    #region When Steps - Setup Actions

    [When(@"I setup environment variables:")]
    public void WhenISetupEnvironmentVariables(Table table)
    {
        _envConfigurationBuilder.Should().NotBeNull("Environment configuration builder should be prepared");

        foreach (var row in table.Rows)
        {
            var name = row["Name"];
            var value = row["Value"];
            _environmentVariables[name] = value;
        }
    }

    [When(@"I setup additional environment variables:")]
    public void WhenISetupAdditionalEnvironmentVariables(Table table)
    {
        _envConfigurationBuilder.Should().NotBeNull("Environment configuration builder should be prepared");

        foreach (var row in table.Rows)
        {
            var name = row["Name"];
            var value = row["Value"];
            _additionalEnvironmentVariables[name] = value;
        }
    }

    [When(@"I setup base configuration data:")]
    public void WhenISetupBaseConfigurationData(Table table)
    {
        _envConfigurationBuilder.Should().NotBeNull("Environment configuration builder should be prepared");

        foreach (var row in table.Rows)
        {
            var key = row["Key"];
            var value = row["Value"];
            _baseConfigurationData[key] = value;
        }
    }

    #endregion

    #region When Steps - Registration Actions

    [When(@"I register environment variables as configuration source")]
    public void WhenIRegisterEnvironmentVariablesAsConfigurationSource()
    {
        _envConfigurationBuilder.Should().NotBeNull("Environment configuration builder should be prepared");

        // Apply environment variables to the test builder
        _envConfigurationBuilder!.WithEnvironmentVariables(_environmentVariables);

        // Add environment variables source
        _envConfigurationBuilder.AddEnvironmentVariables();
    }

    [When(@"I register environment variables with prefix ""(.*)"" as configuration source")]
    public void WhenIRegisterEnvironmentVariablesWithPrefixAsConfigurationSource(string prefix)
    {
        _envConfigurationBuilder.Should().NotBeNull("Environment configuration builder should be prepared");

        // Apply environment variables to the test builder
        _envConfigurationBuilder!.WithEnvironmentVariables(_environmentVariables);

        // Add environment variables source with prefix
        _envConfigurationBuilder.AddEnvironmentVariables(prefix);
    }

    [When(@"I register additional environment variables as configuration source")]
    public void WhenIRegisterAdditionalEnvironmentVariablesAsConfigurationSource()
    {
        _envConfigurationBuilder.Should().NotBeNull("Environment configuration builder should be prepared");

        // Apply additional environment variables to the test builder
        _envConfigurationBuilder!.WithEnvironmentVariables(_additionalEnvironmentVariables);

        // Add environment variables source
        _envConfigurationBuilder.AddEnvironmentVariables();
    }

    [When(@"I register base configuration as source")]
    public void WhenIRegisterBaseConfigurationAsSource()
    {
        _envConfigurationBuilder.Should().NotBeNull("Environment configuration builder should be prepared");

        if (_baseConfigurationData.Count > 0)
        {
            _envConfigurationBuilder!.AddInMemoryCollection(_baseConfigurationData);
        }
    }

    [When(@"I register \.env file ""(.*)"" as configuration source")]
    public void WhenIRegisterDotEnvFileAsConfigurationSource(string filePath)
    {
        _envConfigurationBuilder.Should().NotBeNull("Environment configuration builder should be prepared");

        // Use the same pattern as JsonConfigurationSteps for consistency
        var normalizedPath = filePath.Replace('/', Path.DirectorySeparatorChar);

        if (!File.Exists(normalizedPath))
        {
            throw new FileNotFoundException($"Test data file not found: {normalizedPath}");
        }

        _envConfigurationBuilder!.AddDotEnvFile(normalizedPath, optional: false);
    }

    #endregion

    #region When Steps - Build Actions

    [When(@"I build the environment configuration")]
    public void WhenIBuildTheEnvironmentConfiguration()
    {
        _envConfigurationBuilder.Should().NotBeNull("Environment configuration builder should be prepared");

        try
        {
            _envConfiguration = _envConfigurationBuilder!.Build();
            _envLoadingSucceeded = true;
            _lastEnvException = null;
        }
        catch (Exception ex)
        {
            _envLoadingSucceeded = false;
            _lastEnvException = ex;
        }
    }

    [When(@"I create FlexConfig from environment configuration")]
    public void WhenICreateFlexConfigFromEnvironmentConfiguration()
    {
        _envConfiguration.Should().NotBeNull("Environment configuration should be built");

        try
        {
            _envFlexConfiguration = _envConfiguration!.GetFlexConfiguration();
        }
        catch (Exception ex)
        {
            _lastEnvException = ex;
        }
    }

    #endregion

    #region Then Steps - Assertions

    [Then(@"the environment configuration should be loaded successfully")]
    public void ThenTheEnvironmentConfigurationShouldBeLoadedSuccessfully()
    {
        _envLoadingSucceeded.Should().BeTrue("Environment configuration loading should succeed");
        _lastEnvException.Should().BeNull("No exception should occur during environment configuration loading");
        _envConfiguration.Should().NotBeNull("Environment configuration should be loaded");
    }

    [Then(@"the environment configuration should contain ""(.*)"" with value ""(.*)""")]
    public void ThenTheEnvironmentConfigurationShouldContainWithValue(string key, string expectedValue)
    {
        _envConfiguration.Should().NotBeNull("Environment configuration should be loaded");

        var actualValue = _envConfiguration![key];
        actualValue.Should().Be(expectedValue, $"Environment configuration key '{key}' should have the expected value");
    }

    [Then(@"the environment configuration should not contain ""(.*)""")]
    public void ThenTheEnvironmentConfigurationShouldNotContain(string key)
    {
        _envConfiguration.Should().NotBeNull("Environment configuration should be loaded");

        var actualValue = _envConfiguration![key];
        actualValue.Should().BeNull($"Environment configuration should not contain key '{key}'");
    }

    [Then(@"the FlexConfig should be loaded successfully")]
    public void ThenTheFlexConfigShouldBeLoadedSuccessfully()
    {
        _envFlexConfiguration.Should().NotBeNull("FlexConfig should be created from environment configuration");
        _lastEnvException.Should().BeNull("No exception should occur during FlexConfig creation");
    }

    [Then(@"FlexConfig should contain ""(.*)"" with value ""(.*)""")]
    public void ThenFlexConfigShouldContainWithValue(string key, string expectedValue)
    {
        _envFlexConfiguration.Should().NotBeNull("FlexConfig should be loaded");

        var actualValue = _envFlexConfiguration![key];
        actualValue.Should().Be(expectedValue, $"FlexConfig key '{key}' should have the expected value");
    }

    #endregion
}
