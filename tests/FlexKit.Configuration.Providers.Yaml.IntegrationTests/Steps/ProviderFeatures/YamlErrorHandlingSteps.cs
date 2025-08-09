using FlexKit.Configuration.Core;
using FlexKit.Configuration.Providers.Yaml.IntegrationTests.Utils;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Reqnroll;
// ReSharper disable TooManyDeclarations
// ReSharper disable MethodTooLong
// ReSharper disable NullableWarningSuppressionIsUsed

namespace FlexKit.Configuration.Providers.Yaml.IntegrationTests.Steps.ProviderFeatures;

/// <summary>
/// Step definitions for YAML error handling scenarios.
/// Tests error conditions including invalid syntax, missing files, corrupted data,
/// and edge cases that should be handled gracefully by the YAML provider.
/// Uses distinct step patterns ("error handling module", "process error", "verify error") 
/// to avoid conflicts with other configuration step classes.
/// </summary>
[Binding]
public class YamlErrorHandlingSteps(ScenarioContext scenarioContext)
{
    private YamlTestConfigurationBuilder? _errorHandlingBuilder;
    private IConfiguration? _errorHandlingConfiguration;
    private IFlexConfig? _errorHandlingFlexConfiguration;
    private Exception? _lastErrorException;
    private readonly List<string> _errorValidationResults = new();

    #region Given Steps - Setup

    [Given(@"I have established an error handling module environment")]
    public void GivenIHaveEstablishedAnErrorHandlingModuleEnvironment()
    {
        _errorHandlingBuilder = new YamlTestConfigurationBuilder(scenarioContext);
        scenarioContext.Set(_errorHandlingBuilder, "ErrorHandlingBuilder");
    }

    [Given(@"I have an error handling module with valid YAML from file ""(.*)""")]
    public void GivenIHaveAnErrorHandlingModuleWithValidYamlFromFile(string filePath)
    {
        _errorHandlingBuilder.Should().NotBeNull("Error handling builder should be established");

        var normalizedPath = filePath.Replace('/', Path.DirectorySeparatorChar);
        var testDataPath = Path.Combine("TestData", normalizedPath);

        _errorHandlingBuilder!.AddYamlFile(testDataPath, optional: false);
        
        scenarioContext.Set(_errorHandlingBuilder, "ErrorHandlingBuilder");
    }

    [Given(@"I have an error handling module with valid configuration:")]
    public void GivenIHaveAnErrorHandlingModuleWithValidConfiguration(string yamlContent)
    {
        _errorHandlingBuilder.Should().NotBeNull("Error handling builder should be established");

        _errorHandlingBuilder!.AddTempYamlFile(yamlContent, optional: false);
        
        scenarioContext.Set(_errorHandlingBuilder, "ErrorHandlingBuilder");
    }

    #endregion

    #region When Steps - Actions

    [When(@"I process error handling module configuration")]
    public void WhenIProcessErrorHandlingModuleConfiguration()
    {
        _errorHandlingBuilder.Should().NotBeNull("Error handling builder should be established");

        try
        {
            _errorHandlingConfiguration = _errorHandlingBuilder!.Build();
            _errorHandlingFlexConfiguration = _errorHandlingConfiguration.GetFlexConfiguration();
            
            scenarioContext.Set(_errorHandlingConfiguration, "ErrorHandlingConfiguration");
            scenarioContext.Set(_errorHandlingFlexConfiguration, "ErrorHandlingFlexConfiguration");
        }
        catch (Exception ex)
        {
            _lastErrorException = ex;
            scenarioContext.Set(ex, "ErrorHandlingException");
        }
    }

    [When(@"I attempt error handling with invalid YAML from file ""(.*)""")]
    public void WhenIAttemptErrorHandlingWithInvalidYamlFromFile(string filePath)
    {
        _errorHandlingBuilder.Should().NotBeNull("Error handling builder should be established");

        var normalizedPath = filePath.Replace('/', Path.DirectorySeparatorChar);
        var testDataPath = Path.Combine("TestData", normalizedPath);

        try
        {
            _errorHandlingBuilder!.AddYamlFile(testDataPath, optional: false);
            _errorHandlingConfiguration = _errorHandlingBuilder.Build();
            _errorHandlingFlexConfiguration = _errorHandlingConfiguration.GetFlexConfiguration();
        }
        catch (Exception ex)
        {
            _lastErrorException = ex;
            scenarioContext.Set(ex, "ErrorHandlingException");
        }
    }

    [When(@"I attempt error handling with missing required file ""(.*)""")]
    public void WhenIAttemptErrorHandlingWithMissingRequiredFile(string filePath)
    {
        _errorHandlingBuilder.Should().NotBeNull("Error handling builder should be established");

        var normalizedPath = filePath.Replace('/', Path.DirectorySeparatorChar);
        var testDataPath = Path.Combine("TestData", normalizedPath);

        try
        {
            _errorHandlingBuilder!.AddYamlFile(testDataPath, optional: false);
            _errorHandlingConfiguration = _errorHandlingBuilder.Build();
            _errorHandlingFlexConfiguration = _errorHandlingConfiguration.GetFlexConfiguration();
        }
        catch (Exception ex)
        {
            _lastErrorException = ex;
            scenarioContext.Set(ex, "ErrorHandlingException");
        }
    }

    [When(@"I attempt error handling with missing optional file ""(.*)""")]
    public void WhenIAttemptErrorHandlingWithMissingOptionalFile(string filePath)
    {
        _errorHandlingBuilder.Should().NotBeNull("Error handling builder should be established");

        var normalizedPath = filePath.Replace('/', Path.DirectorySeparatorChar);
        var testDataPath = Path.Combine("TestData", normalizedPath);

        try
        {
            _errorHandlingBuilder!.AddYamlFile(testDataPath, optional: true);
            _errorHandlingConfiguration = _errorHandlingBuilder.Build();
            _errorHandlingFlexConfiguration = _errorHandlingConfiguration.GetFlexConfiguration();
        }
        catch (Exception ex)
        {
            _lastErrorException = ex;
            scenarioContext.Set(ex, "ErrorHandlingException");
        }
    }

    [When(@"I attempt error handling with corrupted YAML content:")]
    public void WhenIAttemptErrorHandlingWithCorruptedYamlContent(string corruptedYamlContent)
    {
        _errorHandlingBuilder.Should().NotBeNull("Error handling builder should be established");

        try
        {
            _errorHandlingBuilder!.AddTempYamlFile(corruptedYamlContent, optional: false);
            _errorHandlingConfiguration = _errorHandlingBuilder.Build();
            _errorHandlingFlexConfiguration = _errorHandlingConfiguration.GetFlexConfiguration();
        }
        catch (Exception ex)
        {
            _lastErrorException = ex;
            scenarioContext.Set(ex, "ErrorHandlingException");
        }
    }

    [When(@"I verify error handling module graceful degradation")]
    public void WhenIVerifyErrorHandlingModuleGracefulDegradation()
    {
        _errorHandlingFlexConfiguration.Should().NotBeNull("Error handling FlexConfiguration should be available");

        try
        {
            // Test accessing non-existent keys returns null gracefully
            var nonExistentString = _errorHandlingFlexConfiguration!["NonExistent:Key"];
            _errorValidationResults.Add($"NonExistentString: {nonExistentString ?? "null"}");

            // Test dynamic access to missing properties returns null
            dynamic errorConfig = _errorHandlingFlexConfiguration;
            var missingProperty = YamlTestConfigurationBuilder.GetDynamicProperty(errorConfig, "missing");
            _errorValidationResults.Add($"MissingProperty: {missingProperty ?? "null"}");

            // Test section access for non-existent sections
            var missingSection = _errorHandlingFlexConfiguration.Configuration.GetSection("NonExistent");
            var sectionExists = missingSection.Exists();
            _errorValidationResults.Add($"MissingSectionExists: {sectionExists}");

            // Test array access beyond bounds
            var outOfBoundsArray = _errorHandlingFlexConfiguration["features:999"];
            _errorValidationResults.Add($"OutOfBoundsArray: {outOfBoundsArray ?? "null"}");
        }
        catch (Exception ex)
        {
            _lastErrorException = ex;
            _errorValidationResults.Add($"GracefulDegradationError: {ex.Message}");
        }
    }

    [When(@"I verify error handling module empty file handling")]
    public void WhenIVerifyErrorHandlingModuleEmptyFileHandling()
    {
        _errorHandlingFlexConfiguration.Should().NotBeNull("Error handling FlexConfiguration should be available");

        try
        {
            // Verify that empty files result in an empty but valid configuration
            var allKeys = _errorHandlingConfiguration!.AsEnumerable().ToList();
            _errorValidationResults.Add($"ConfigurationKeysCount: {allKeys.Count}");

            // Verify FlexConfig functionality works with an empty configuration
            var emptyKeyAccess = _errorHandlingFlexConfiguration!["any:key"];
            _errorValidationResults.Add($"EmptyConfigAccess: {emptyKeyAccess ?? "null"}");

            // Verify dynamic access works with an empty configuration
            dynamic emptyConfig = _errorHandlingFlexConfiguration;
            var dynamicEmptyAccess = YamlTestConfigurationBuilder.GetDynamicProperty(emptyConfig, "any");
            _errorValidationResults.Add($"DynamicEmptyAccess: {dynamicEmptyAccess ?? "null"}");
        }
        catch (Exception ex)
        {
            _lastErrorException = ex;
            _errorValidationResults.Add($"EmptyFileHandlingError: {ex.Message}");
        }
    }

    #endregion

    #region Then Steps - Assertions

    [Then(@"the error handling module should be configured successfully")]
    public void ThenTheErrorHandlingModuleShouldBeConfiguredSuccessfully()
    {
        _errorHandlingConfiguration.Should().NotBeNull("Error handling configuration should be built");
        _errorHandlingFlexConfiguration.Should().NotBeNull("Error handling FlexConfiguration should be available");
        _lastErrorException.Should().BeNull("No exceptions should occur during valid configuration");
    }

    [Then(@"the error handling module should fail with YAML parsing error")]
    public void ThenTheErrorHandlingModuleShouldFailWithYamlParsingError()
    {
        _lastErrorException.Should().NotBeNull("An exception should have occurred for invalid YAML");
        _lastErrorException!.Should().BeOfType<InvalidDataException>("Should throw InvalidDataException for parsing errors");
        _lastErrorException.Message.Should().Contain("Failed to parse YAML", "Error message should indicate YAML parsing failure");
    }

    [Then(@"the error handling module should either fail with YAML parsing error or succeed")]
    public void ThenTheErrorHandlingModuleShouldEitherFailWithYamlParsingErrorOrSucceed()
    {
        // Some YAML content that might seem invalid (like control characters) might actually parse successfully
        // This step handles cases where YamlDotNet is more tolerant than expected
        if (_lastErrorException != null)
        {
            _lastErrorException.Should().BeOfType<InvalidDataException>("Should throw InvalidDataException for parsing errors");
            _lastErrorException.Message.Should().Contain("Failed to parse YAML", "Error message should indicate YAML parsing failure");
        }
        else
        {
            // If it didn't fail, ensure we still have a valid configuration
            _errorHandlingConfiguration.Should().NotBeNull("Configuration should be built if YAML was parsed successfully");
            _errorHandlingFlexConfiguration.Should().NotBeNull("FlexConfiguration should be available if YAML was parsed successfully");
        }
    }

    [Then(@"the error handling module should fail with file not found error")]
    public void ThenTheErrorHandlingModuleShouldFailWithFileNotFoundError()
    {
        _lastErrorException.Should().NotBeNull("An exception should have occurred for missing file");
        _lastErrorException!.Should().BeOfType<FileNotFoundException>("Should throw FileNotFoundException for missing required files");
        _lastErrorException.Message.Should().Contain("was not found and is not optional", "Error message should indicate file not found");
    }

    [Then(@"the error handling module should handle missing optional files gracefully")]
    public void ThenTheErrorHandlingModuleShouldHandleMissingOptionalFilesGracefully()
    {
        _lastErrorException.Should().BeNull("Missing optional files should not cause exceptions");
        _errorHandlingConfiguration.Should().NotBeNull("Configuration should be built even with missing optional files");
        _errorHandlingFlexConfiguration.Should().NotBeNull("FlexConfiguration should be available with missing optional files");
        
        // Verify an empty configuration is created
        var configKeys = _errorHandlingConfiguration!.AsEnumerable().ToList();
        configKeys.Should().BeEmpty("Missing optional files should result in empty configuration");
    }

    [Then(@"the error handling module should support graceful degradation")]
    public void ThenTheErrorHandlingModuleShouldSupportGracefulDegradation()
    {
        _lastErrorException.Should().BeNull("Graceful degradation should not throw exceptions");
        _errorValidationResults.Should().Contain(result => result.Contains("NonExistentString: null"));
        _errorValidationResults.Should().Contain(result => result.Contains("MissingProperty: null"));
        _errorValidationResults.Should().Contain(result => result.Contains("MissingSectionExists: False"));
        _errorValidationResults.Should().Contain(result => result.Contains("OutOfBoundsArray: null"));
    }

    [Then(@"the error handling module should handle empty files correctly")]
    public void ThenTheErrorHandlingModuleShouldHandleEmptyFilesCorrectly()
    {
        _lastErrorException.Should().BeNull("Empty files should not cause exceptions");
        _errorValidationResults.Should().Contain(result => result.Contains("ConfigurationKeysCount: 0"));
        _errorValidationResults.Should().Contain(result => result.Contains("EmptyConfigAccess: null"));
        _errorValidationResults.Should().Contain(result => result.Contains("DynamicEmptyAccess: null"));
    }

    [Then(@"the error handling module should maintain FlexConfig functionality")]
    public void ThenTheErrorHandlingModuleShouldMaintainFlexConfigFunctionality()
    {
        _errorHandlingFlexConfiguration.Should().NotBeNull("FlexConfiguration should be available");
        
        // Verify that basic FlexConfig operations work even in error scenarios
        var flexConfig = _errorHandlingFlexConfiguration!;
        flexConfig.Configuration.Should().NotBeNull("Underlying IConfiguration should be accessible");
        
        // Verify that indexer access doesn't throw
        var indexerTest = () => flexConfig["test:key"];
        indexerTest.Should().NotThrow("Indexer access should not throw for missing keys");
        
        // Verify that sections access works
        var sectionTest = () => flexConfig.Configuration.GetSection("test");
        sectionTest.Should().NotThrow("Section access should not throw for missing sections");
    }

    #endregion
}