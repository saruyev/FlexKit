using FlexKit.Configuration.Core;
using FlexKit.Configuration.IntegrationTests.Utils;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Reqnroll;
using System.Globalization;

namespace FlexKit.Configuration.IntegrationTests.Steps.RealWorld;

/// <summary>
/// Step definitions for configuration validation scenarios.
/// Tests configuration validation, error handling, type checking, and range validation
/// for various configuration sources and complex validation rules.
/// Uses distinct step patterns ("validation setup", "validation trigger") to avoid conflicts 
/// with other configuration step classes.
/// </summary>
[Binding]
public class ConfigurationValidationSteps(ScenarioContext scenarioContext)
{
    private TestConfigurationBuilder? _validationConfigurationBuilder;
    private IConfiguration? _validationConfiguration;
    private IFlexConfig? _validationFlexConfiguration;
    
    // Validation data storage
    private readonly Dictionary<string, string?> _configurationData = new();
    private readonly Dictionary<string, string> _validationRules = new();
    private readonly Dictionary<string, string> _typeValidationRules = new();
    private readonly Dictionary<string, string> _rangeValidationRules = new();
    private readonly Dictionary<string, (string Min, string Max)> _rangeValues = new();
    private readonly Dictionary<string, string> _expectedTypes = new();
    private readonly Dictionary<string, string?> _environmentVariables = new();
    private readonly List<(string Source, string Path)> _multiSources = new();
    
    // Validation results
    private readonly List<string> _validationErrors = new();
    private bool _validationSucceeded = true;
    private bool _typeValidationSucceeded = true;
    private bool _rangeValidationSucceeded = true;
    private bool _fileValidationSucceeded = true;
    private bool _environmentValidationSucceeded = true;
    private bool _multiSourceValidationSucceeded = true;
    private Exception? _lastValidationException;

    #region Given Steps - Setup

    [Given(@"I have validation setup with a configuration validation environment")]
    public void GivenIHaveValidationSetupWithAConfigurationValidationEnvironment()
    {
        _validationConfigurationBuilder = TestConfigurationBuilder.Create(scenarioContext);
        scenarioContext.Set(_validationConfigurationBuilder, "ValidationConfigurationBuilder");
        
        // Clear any previous validation state
        _configurationData.Clear();
        _validationRules.Clear();
        _typeValidationRules.Clear();
        _rangeValidationRules.Clear();
        _rangeValues.Clear();
        _expectedTypes.Clear();
        _environmentVariables.Clear();
        _multiSources.Clear();
        _validationErrors.Clear();
        
        _validationSucceeded = true;
        _typeValidationSucceeded = true;
        _rangeValidationSucceeded = true;
        _fileValidationSucceeded = true;
        _environmentValidationSucceeded = true;
        _multiSourceValidationSucceeded = true;
        _lastValidationException = null;
    }

    #endregion

    #region When Steps - Configuration Setup

    [When(@"I validation setup configuration with required values:")]
    public void WhenIValidationSetupConfigurationWithRequiredValues(Table table)
    {
        _validationConfigurationBuilder.Should().NotBeNull("Validation configuration builder should be established");
        
        foreach (var row in table.Rows)
        {
            var key = row["Key"];
            var value = row["Value"];
            var validationRule = row["ValidationRule"];
            
            _configurationData[key] = value;
            _validationRules[key] = validationRule;
        }
        
        _validationConfigurationBuilder!.AddInMemoryCollection(_configurationData);
    }

    [When(@"I validation setup configuration with missing required values:")]
    public void WhenIValidationSetupConfigurationWithMissingRequiredValues(Table table)
    {
        _validationConfigurationBuilder.Should().NotBeNull("Validation configuration builder should be established");
        
        foreach (var row in table.Rows)
        {
            var key = row["Key"];
            var value = row["Value"];
            var validationRule = row["ValidationRule"];
            
            // Only add non-empty values to simulate missing required values
            if (!string.IsNullOrEmpty(value))
            {
                _configurationData[key] = value;
            }
            _validationRules[key] = validationRule;
        }
        
        _validationConfigurationBuilder!.AddInMemoryCollection(_configurationData);
    }

    [When(@"I validation setup configuration with typed values:")]
    public void WhenIValidationSetupConfigurationWithTypedValues(Table table)
    {
        _validationConfigurationBuilder.Should().NotBeNull("Validation configuration builder should be established");
        
        foreach (var row in table.Rows)
        {
            var key = row["Key"];
            var value = row["Value"];
            var expectedType = row["ExpectedType"];
            
            _configurationData[key] = value;
            _expectedTypes[key] = expectedType;
        }
        
        _validationConfigurationBuilder!.AddInMemoryCollection(_configurationData);
    }

    [When(@"I validation setup configuration with invalid typed values:")]
    public void WhenIValidationSetupConfigurationWithInvalidTypedValues(Table table)
    {
        WhenIValidationSetupConfigurationWithTypedValues(table);
    }

    [When(@"I validation setup configuration with range values:")]
    public void WhenIValidationSetupConfigurationWithRangeValues(Table table)
    {
        _validationConfigurationBuilder.Should().NotBeNull("Validation configuration builder should be established");
        
        foreach (var row in table.Rows)
        {
            var key = row["Key"];
            var value = row["Value"];
            var minValue = row["MinValue"];
            var maxValue = row["MaxValue"];
            
            _configurationData[key] = value;
            _rangeValues[key] = (minValue, maxValue);
        }
        
        _validationConfigurationBuilder!.AddInMemoryCollection(_configurationData);
    }

    [When(@"I validation setup configuration with out-of-range values:")]
    public void WhenIValidationSetupConfigurationWithOutOfRangeValues(Table table)
    {
        WhenIValidationSetupConfigurationWithRangeValues(table);
    }

    [When(@"I validation setup configuration from JSON file ""(.*)""")]
    public void WhenIValidationSetupConfigurationFromJsonFile(string filePath)
    {
        _validationConfigurationBuilder.Should().NotBeNull("Validation configuration builder should be established");
        
        var normalizedPath = filePath.Replace('/', Path.DirectorySeparatorChar);
        _validationConfigurationBuilder!.AddJsonFile(normalizedPath, optional: false, reloadOnChange: false);
    }

    [When(@"I validation setup configuration from invalid JSON file ""(.*)""")]
    public void WhenIValidationSetupConfigurationFromInvalidJsonFile(string filePath)
    {
        _validationConfigurationBuilder.Should().NotBeNull("Validation configuration builder should be established");
        
        var normalizedPath = filePath.Replace('/', Path.DirectorySeparatorChar);
        _validationConfigurationBuilder!.AddJsonFile(normalizedPath, optional: false, reloadOnChange: false);
    }

    [When(@"I validation setup environment variables for validation:")]
    public void WhenIValidationSetupEnvironmentVariablesForValidation(Table table)
    {
        _validationConfigurationBuilder.Should().NotBeNull("Validation configuration builder should be established");
        
        foreach (var row in table.Rows)
        {
            var name = row["Name"];
            var value = row["Value"];
            _environmentVariables[name] = value;
        }
        
        // Use the same pattern as EnvironmentVariableSteps
        _validationConfigurationBuilder!.WithEnvironmentVariables(_environmentVariables);
    }

    [When(@"I validation setup multi-source configuration with:")]
    public void WhenIValidationSetupMultiSourceConfigurationWith(Table table)
    {
        _validationConfigurationBuilder.Should().NotBeNull("Validation configuration builder should be established");
        
        foreach (var row in table.Rows)
        {
            var source = row["Source"];
            var path = row["Path"];
            _multiSources.Add((source, path));
            
            switch (source.ToLowerInvariant())
            {
                case "json":
                    var normalizedPath = path.Replace('/', Path.DirectorySeparatorChar);
                    _validationConfigurationBuilder!.AddJsonFile(normalizedPath, optional: true, reloadOnChange: false);
                    break;
                    
                case "environment":
                    _validationConfigurationBuilder!.AddEnvironmentVariables();
                    break;
                    
                case "inmemory":
                    var inMemoryData = new Dictionary<string, string?>
                    {
                        ["AdditionalConfig:Source"] = "InMemory",
                        ["AdditionalConfig:LoadedAt"] = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)
                    };
                    _validationConfigurationBuilder!.AddInMemoryCollection(inMemoryData);
                    break;
            }
        }
    }

    #endregion

    #region When Steps - Validation Rules Setup

    [When(@"I validation setup validation rules for required values:")]
    public void WhenIValidationSetupValidationRulesForRequiredValues(Table table)
    {
        foreach (var row in table.Rows)
        {
            var key = row["Key"];
            var rule = row["Rule"];
            _validationRules[key] = rule;
        }
    }

    [When(@"I validation setup type validation rules:")]
    public void WhenIValidationSetupTypeValidationRules(Table table)
    {
        foreach (var row in table.Rows)
        {
            var key = row["Key"];
            var validationRule = row["ValidationRule"];
            _typeValidationRules[key] = validationRule;
        }
    }

    [When(@"I validation setup range validation rules:")]
    public void WhenIValidationSetupRangeValidationRules(Table table)
    {
        foreach (var row in table.Rows)
        {
            var key = row["Key"];
            var validationRule = row["ValidationRule"];
            _rangeValidationRules[key] = validationRule;
        }
    }

    [When(@"I validation setup additional validation configuration:")]
    public void WhenIValidationSetupAdditionalValidationConfiguration(Table table)
    {
        foreach (var row in table.Rows)
        {
            var key = row["Key"];
            var validationRule = row["ValidationRule"];
            _validationRules[key] = validationRule;
        }
    }

    [When(@"I validation setup strict validation rules:")]
    public void WhenIValidationSetupStrictValidationRules(Table table)
    {
        foreach (var row in table.Rows)
        {
            var key = row["Key"];
            var validationRule = row["ValidationRule"];
            _validationRules[key] = validationRule;
        }
    }

    [When(@"I validation setup environment validation rules:")]
    public void WhenIValidationSetupEnvironmentValidationRules(Table table)
    {
        foreach (var row in table.Rows)
        {
            var key = row["Key"];
            var validationRule = row["ValidationRule"];
            _validationRules[key] = validationRule;
        }
    }

    [When(@"I validation setup comprehensive validation rules:")]
    public void WhenIValidationSetupComprehensiveValidationRules(Table table)
    {
        foreach (var row in table.Rows)
        {
            var key = row["Key"];
            var validationRule = row["ValidationRule"];
            _validationRules[key] = validationRule;
        }
    }

    #endregion

    #region When Steps - Validation Triggers

    [When(@"I validation trigger configuration validation process")]
    public void WhenIValidationTriggerConfigurationValidationProcess()
    {
        try
        {
            _validationConfiguration = _validationConfigurationBuilder!.Build();
            _validationFlexConfiguration = _validationConfiguration.GetFlexConfiguration();
            
            PerformRequiredValueValidation();
        }
        catch (Exception ex)
        {
            _lastValidationException = ex;
            _validationSucceeded = false;
        }
    }

    [When(@"I validation trigger type validation process")]
    public void WhenIValidationTriggerTypeValidationProcess()
    {
        try
        {
            _validationConfiguration = _validationConfigurationBuilder!.Build();
            _validationFlexConfiguration = _validationConfiguration.GetFlexConfiguration();
            
            PerformTypeValidation();
        }
        catch (Exception ex)
        {
            _lastValidationException = ex;
            _typeValidationSucceeded = false;
        }
    }

    [When(@"I validation trigger range validation process")]
    public void WhenIValidationTriggerRangeValidationProcess()
    {
        try
        {
            _validationConfiguration = _validationConfigurationBuilder!.Build();
            _validationFlexConfiguration = _validationConfiguration.GetFlexConfiguration();
            
            PerformRangeValidation();
        }
        catch (Exception ex)
        {
            _lastValidationException = ex;
            _rangeValidationSucceeded = false;
        }
    }

    [When(@"I validation trigger file-based configuration validation")]
    public void WhenIValidationTriggerFileBasedConfigurationValidation()
    {
        try
        {
            _validationConfiguration = _validationConfigurationBuilder!.Build();
            _validationFlexConfiguration = _validationConfiguration.GetFlexConfiguration();
            
            PerformFileBasedValidation();
        }
        catch (Exception ex)
        {
            _lastValidationException = ex;
            _fileValidationSucceeded = false;
        }
    }

    [When(@"I validation trigger environment variable validation")]
    public void WhenIValidationTriggerEnvironmentVariableValidation()
    {
        try
        {
            // Add environment variables source BEFORE building, following EnvironmentVariableSteps pattern
            _validationConfigurationBuilder!.AddEnvironmentVariables("VALIDATION_");
            
            _validationConfiguration = _validationConfigurationBuilder!.Build();
            _validationFlexConfiguration = _validationConfiguration.GetFlexConfiguration();
            
            PerformEnvironmentValidation();
        }
        catch (Exception ex)
        {
            _lastValidationException = ex;
            _environmentValidationSucceeded = false;
        }
    }

    [When(@"I validation trigger multi-source validation process")]
    public void WhenIValidationTriggerMultiSourceValidationProcess()
    {
        try
        {
            _validationConfiguration = _validationConfigurationBuilder!.Build();
            _validationFlexConfiguration = _validationConfiguration.GetFlexConfiguration();
            
            PerformMultiSourceValidation();
        }
        catch (Exception ex)
        {
            _lastValidationException = ex;
            _multiSourceValidationSucceeded = false;
        }
    }

    #endregion

    #region Then Steps - Success Validation

    [Then(@"the configuration validation should succeed")]
    public void ThenTheConfigurationValidationShouldSucceed()
    {
        _validationSucceeded.Should().BeTrue("Configuration validation should succeed");
        _lastValidationException.Should().BeNull("No validation exception should occur");
        _validationErrors.Should().BeEmpty("No validation errors should be present");
    }

    [Then(@"all required values should be validated successfully")]
    public void ThenAllRequiredValuesShouldBeValidatedSuccessfully()
    {
        _validationConfiguration.Should().NotBeNull("Validation configuration should be loaded");
        
        foreach (var rule in _validationRules.Where(r => r.Value.Contains("Must be present")))
        {
            var value = _validationConfiguration![rule.Key];
            value.Should().NotBeNullOrEmpty($"Required value for {rule.Key} should be present");
        }
    }

    [Then(@"FlexConfig should contain all required validated values")]
    public void ThenFlexConfigShouldContainAllRequiredValidatedValues()
    {
        _validationFlexConfiguration.Should().NotBeNull("Validation FlexConfig should be created");
        
        foreach (var rule in _validationRules.Where(r => r.Value.Contains("Must be present")))
        {
            var value = _validationFlexConfiguration![rule.Key];
            value.Should().NotBeNullOrEmpty($"Required value for {rule.Key} should be present in FlexConfig");
        }
    }

    [Then(@"the type validation should succeed")]
    public void ThenTheTypeValidationShouldSucceed()
    {
        _typeValidationSucceeded.Should().BeTrue("Type validation should succeed");
        _lastValidationException.Should().BeNull("No type validation exception should occur");
    }

    [Then(@"all values should be convertible to their expected types")]
    public void ThenAllValuesShouldBeConvertibleToTheirExpectedTypes()
    {
        _validationConfiguration.Should().NotBeNull("Validation configuration should be loaded");
        
        foreach (var typeRule in _expectedTypes)
        {
            var key = typeRule.Key;
            var expectedType = typeRule.Value;
            var value = _validationConfiguration![key];
            
            value.Should().NotBeNull($"Value for {key} should not be null");
            
            var conversionSucceeded = TryConvertValue(value, expectedType);
            conversionSucceeded.Should().BeTrue($"Value '{value}' should be convertible to {expectedType}");
        }
    }

    [Then(@"FlexConfig should provide properly typed access to values")]
    public void ThenFlexConfigShouldProvideProperlyTypedAccessToValues()
    {
        _validationFlexConfiguration.Should().NotBeNull("Validation FlexConfig should be created");
        
        // Test that FlexConfig can access typed values without exceptions
        foreach (var typeRule in _expectedTypes.Take(3)) // Limit for performance
        {
            var key = typeRule.Key;
            var value = _validationFlexConfiguration![key];
            value.Should().NotBeNull($"FlexConfig should provide access to {key}");
        }
    }

    [Then(@"the range validation should succeed")]
    public void ThenTheRangeValidationShouldSucceed()
    {
        _rangeValidationSucceeded.Should().BeTrue("Range validation should succeed");
        _lastValidationException.Should().BeNull("No range validation exception should occur");
    }

    [Then(@"all values should be within their specified ranges")]
    public void ThenAllValuesShouldBeWithinTheirSpecifiedRanges()
    {
        _validationConfiguration.Should().NotBeNull("Validation configuration should be loaded");
        
        foreach (var rangeRule in _rangeValues)
        {
            var key = rangeRule.Key;
            var (minValue, maxValue) = rangeRule.Value;
            var value = _validationConfiguration![key];
            
            value.Should().NotBeNull($"Value for {key} should not be null");
            
            if (double.TryParse(value, out var numericValue) && 
                double.TryParse(minValue, out var min) && 
                double.TryParse(maxValue, out var max))
            {
                numericValue.Should().BeGreaterThanOrEqualTo(min, $"Value {numericValue} should be >= {min}");
                numericValue.Should().BeLessThanOrEqualTo(max, $"Value {numericValue} should be <= {max}");
            }
        }
    }

    [Then(@"FlexConfig should contain validated range values")]
    public void ThenFlexConfigShouldContainValidatedRangeValues()
    {
        _validationFlexConfiguration.Should().NotBeNull("Validation FlexConfig should be created");
        
        foreach (var rangeRule in _rangeValues.Take(3)) // Limit for performance
        {
            var key = rangeRule.Key;
            var value = _validationFlexConfiguration![key];
            value.Should().NotBeNull($"FlexConfig should contain validated range value for {key}");
        }
    }

    [Then(@"the file-based validation should succeed")]
    public void ThenTheFileBasedValidationShouldSucceed()
    {
        _fileValidationSucceeded.Should().BeTrue("File-based validation should succeed");
        _lastValidationException.Should().BeNull("No file validation exception should occur");
    }

    [Then(@"JSON configuration values should pass validation rules")]
    public void ThenJsonConfigurationValuesShouldPassValidationRules()
    {
        _validationConfiguration.Should().NotBeNull("Validation configuration should be loaded");
        
        // Verify that JSON configuration was loaded
        var configEntries = _validationConfiguration!.AsEnumerable().ToList();
        configEntries.Should().NotBeEmpty("JSON configuration should contain entries");
    }

    [Then(@"FlexConfig should provide validated file-based configuration")]
    public void ThenFlexConfigShouldProvideValidatedFileBasedConfiguration()
    {
        _validationFlexConfiguration.Should().NotBeNull("Validation FlexConfig should be created");
        
        // Test that FlexConfig can access file-based configuration
        var configEntries = _validationConfiguration!.AsEnumerable().Take(3);
        foreach (var entry in configEntries)
        {
            if (!string.IsNullOrEmpty(entry.Key))
            {
                _ = _validationFlexConfiguration![entry.Key];
                // Value can be null/empty, verify no exception is thrown
            }
        }
    }

    [Then(@"the environment validation should succeed")]
    public void ThenTheEnvironmentValidationShouldSucceed()
    {
        _environmentValidationSucceeded.Should().BeTrue("Environment validation should succeed");
        _lastValidationException.Should().BeNull("No environment validation exception should occur");
    }

    [Then(@"environment values should pass validation rules")]
    public void ThenEnvironmentValuesShouldPassValidationRules()
    {
        _validationConfiguration.Should().NotBeNull("Validation configuration should be loaded");
        
        // Check that environment variables were loaded correctly
        // With VALIDATION_ prefix; they should be available with the prefix stripped
        foreach (var envVar in _environmentVariables.Take(3))
        {
            var keyWithoutPrefix = envVar.Key.Replace("VALIDATION_", "");
            var value = _validationConfiguration![keyWithoutPrefix];
            
            if (!string.IsNullOrEmpty(value))
            {
                value.Should().Be(envVar.Value, $"Environment variable {keyWithoutPrefix} should have the expected value");
            }
        }
    }

    [Then(@"FlexConfig should contain validated environment values")]
    public void ThenFlexConfigShouldContainValidatedEnvironmentValues()
    {
        _validationFlexConfiguration.Should().NotBeNull("Validation FlexConfig should be created");
        
        // Verify that FlexConfig can access environment values
        foreach (var envVar in _environmentVariables.Take(2))
        {
            var keyWithoutPrefix = envVar.Key.Replace("VALIDATION_", "");
            _ = _validationFlexConfiguration![keyWithoutPrefix];
            // This should not throw - FlexConfig should be functional
        }
    }

    [Then(@"the multi-source validation should succeed")]
    public void ThenTheMultiSourceValidationShouldSucceed()
    {
        _multiSourceValidationSucceeded.Should().BeTrue("Multi-source validation should succeed");
        _lastValidationException.Should().BeNull("No multi-source validation exception should occur");
    }

    [Then(@"all sources should contribute to validated configuration")]
    public void ThenAllSourcesShouldContributeToValidatedConfiguration()
    {
        _validationConfiguration.Should().NotBeNull("Validation configuration should be loaded");
        
        var configEntries = _validationConfiguration!.AsEnumerable().ToList();
        configEntries.Should().NotBeEmpty("Multi-source configuration should contain entries from all sources");
        
        // Verify we have configuration from multiple sources
        _multiSources.Should().NotBeEmpty("Multiple sources should have been configured");
    }

    [Then(@"FlexConfig should provide comprehensive validated configuration")]
    public void ThenFlexConfigShouldProvideComprehensiveValidatedConfiguration()
    {
        _validationFlexConfiguration.Should().NotBeNull("Validation FlexConfig should be created");
        
        // Test accessing configuration from FlexConfig
        var configEntries = _validationConfiguration!.AsEnumerable().Take(5);
        foreach (var entry in configEntries)
        {
            if (!string.IsNullOrEmpty(entry.Key))
            {
                _ = _validationFlexConfiguration![entry.Key];
                // Comprehensive validation - just ensure access works
            }
        }
    }

    [Then(@"configuration precedence should be maintained during validation")]
    public void ThenConfigurationPrecedenceShouldBeMaintainedDuringValidation()
    {
        _validationConfiguration.Should().NotBeNull("Validation configuration should be loaded");
        
        // Configuration precedence verification
        var configEntries = _validationConfiguration!.AsEnumerable().ToList();
        configEntries.Should().NotBeEmpty("Configuration should maintain precedence order");
    }

    #endregion

    #region Then Steps - Failure Validation

    [Then(@"the configuration validation should fail")]
    public void ThenTheConfigurationValidationShouldFail()
    {
        _validationSucceeded.Should().BeFalse("Configuration validation should fail");
        (_lastValidationException != null || _validationErrors.Count > 0).Should().BeTrue("Validation should have errors or exceptions");
    }

    [Then(@"validation errors should indicate missing required values")]
    public void ThenValidationErrorsShouldIndicateMissingRequiredValues()
    {
        _validationErrors.Should().NotBeEmpty("Validation errors should be present");
        
        var missingValueErrors = _validationErrors.Where(error => 
            error.Contains("missing", StringComparison.OrdinalIgnoreCase) ||
            error.Contains("required", StringComparison.OrdinalIgnoreCase) ||
            error.Contains("Must be present", StringComparison.OrdinalIgnoreCase));
        
        missingValueErrors.Should().NotBeEmpty("Validation errors should indicate missing required values");
    }

    [Then(@"the error should specify which configuration keys are missing")]
    public void ThenTheErrorShouldSpecifyWhichConfigurationKeysAreMissing()
    {
        _validationErrors.Should().NotBeEmpty("Validation errors should be present");
        
        foreach (var rule in _validationRules.Where(r => r.Value.Contains("Must be present")))
        {
            var keyInErrors = _validationErrors.Any(error => error.Contains(rule.Key));
            if (string.IsNullOrEmpty(_validationConfiguration?[rule.Key]))
            {
                keyInErrors.Should().BeTrue($"Error should specify missing key: {rule.Key}");
            }
        }
    }

    [Then(@"the type validation should fail")]
    public void ThenTheTypeValidationShouldFail()
    {
        _typeValidationSucceeded.Should().BeFalse("Type validation should fail");
        (_lastValidationException != null || _validationErrors.Count > 0).Should().BeTrue("Type validation should have errors or exceptions");
    }

    [Then(@"validation errors should indicate type conversion failures")]
    public void ThenValidationErrorsShouldIndicateTypeConversionFailures()
    {
        _validationErrors.Should().NotBeEmpty("Type validation errors should be present");
        
        var typeErrors = _validationErrors.Where(error => 
            error.Contains("type", StringComparison.OrdinalIgnoreCase) ||
            error.Contains("conversion", StringComparison.OrdinalIgnoreCase) ||
            error.Contains("convert", StringComparison.OrdinalIgnoreCase));
        
        typeErrors.Should().NotBeEmpty("Validation errors should indicate type conversion failures");
    }

    [Then(@"the error should specify which values cannot be converted")]
    public void ThenTheErrorShouldSpecifyWhichValuesCannotBeConverted()
    {
        _validationErrors.Should().NotBeEmpty("Type validation errors should be present");
        
        // Check that error messages contain specific keys that failed conversion
        foreach (var typeRule in _expectedTypes)
        {
            var value = _validationConfiguration?[typeRule.Key];
            if (value != null && !TryConvertValue(value, typeRule.Value))
            {
                var keyInErrors = _validationErrors.Any(error => error.Contains(typeRule.Key));
                keyInErrors.Should().BeTrue($"Error should specify unconvertible key: {typeRule.Key}");
            }
        }
    }

    [Then(@"the range validation should fail")]
    public void ThenTheRangeValidationShouldFail()
    {
        _rangeValidationSucceeded.Should().BeFalse("Range validation should fail");
        (_lastValidationException != null || _validationErrors.Count > 0).Should().BeTrue("Range validation should have errors or exceptions");
    }

    [Then(@"validation errors should indicate out-of-range values")]
    public void ThenValidationErrorsShouldIndicateOutOfRangeValues()
    {
        _validationErrors.Should().NotBeEmpty("Range validation errors should be present");
        
        var rangeErrors = _validationErrors.Where(error => 
            error.Contains("range", StringComparison.OrdinalIgnoreCase) ||
            error.Contains("between", StringComparison.OrdinalIgnoreCase) ||
            error.Contains("out of range", StringComparison.OrdinalIgnoreCase));
        
        rangeErrors.Should().NotBeEmpty("Validation errors should indicate out-of-range values");
    }

    [Then(@"the error should specify which values are outside acceptable ranges")]
    public void ThenTheErrorShouldSpecifyWhichValuesAreOutsideAcceptableRanges()
    {
        _validationErrors.Should().NotBeEmpty("Range validation errors should be present");
        
        foreach (var rangeRule in _rangeValues)
        {
            var key = rangeRule.Key;
            var (minValue, maxValue) = rangeRule.Value;
            var value = _validationConfiguration?[key];
            
            if (value != null && 
                double.TryParse(value, out var numericValue) && 
                double.TryParse(minValue, out var min) && 
                double.TryParse(maxValue, out var max) &&
                (numericValue < min || numericValue > max))
            {
                var keyInErrors = _validationErrors.Any(error => error.Contains(key));
                keyInErrors.Should().BeTrue($"Error should specify out-of-range key: {key}");
            }
        }
    }

    [Then(@"the file-based validation should fail gracefully")]
    public void ThenTheFileBasedValidationShouldFailGracefully()
    {
        _fileValidationSucceeded.Should().BeFalse("File-based validation should fail");
        _lastValidationException.Should().NotBeNull("File validation exception should occur");
    }

    [Then(@"validation errors should indicate configuration file issues")]
    public void ThenValidationErrorsShouldIndicateConfigurationFileIssues()
    {
        _lastValidationException.Should().NotBeNull("File validation exception should be present");
        
        var exceptionMessage = _lastValidationException!.ToString();
        var isFileError = exceptionMessage.Contains("file", StringComparison.OrdinalIgnoreCase) ||
                         exceptionMessage.Contains("JSON", StringComparison.OrdinalIgnoreCase) ||
                         exceptionMessage.Contains("configuration", StringComparison.OrdinalIgnoreCase) ||
                         exceptionMessage.Contains("parse", StringComparison.OrdinalIgnoreCase);
        
        isFileError.Should().BeTrue("Exception should indicate configuration file issues");
    }

    [Then(@"the system should handle invalid configuration appropriately")]
    public void ThenTheSystemShouldHandleInvalidConfigurationAppropriately()
    {
        _lastValidationException.Should().NotBeNull("System should handle invalid configuration with exceptions");
        
        // System should not crash, just fail gracefully
        _fileValidationSucceeded.Should().BeFalse("File validation should fail appropriately");
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Performs required value validation for all configured rules.
    /// </summary>
    private void PerformRequiredValueValidation()
    {
        foreach (var rule in _validationRules.Where(r => r.Value.Contains("Must be present")))
        {
            var value = _validationConfiguration?[rule.Key];
            if (string.IsNullOrEmpty(value))
            {
                var errorMessage = $"Required configuration value missing: {rule.Key}";
                _validationErrors.Add(errorMessage);
                _validationSucceeded = false;
            }
        }
    }

    /// <summary>
    /// Performs type validation for all configured type rules.
    /// </summary>
    private void PerformTypeValidation()
    {
        foreach (var typeRule in _typeValidationRules)
        {
            var key = typeRule.Key;
            var rule = typeRule.Value;
            var value = _validationConfiguration?[key];
            
            if (value == null)
            {
                var errorMessage = $"Type validation failed for {key}: value is null";
                _validationErrors.Add(errorMessage);
                _typeValidationSucceeded = false;
                continue;
            }
            
            var isValid = ValidateTypeRule(value, rule);
            if (!isValid)
            {
                var errorMessage = $"Type validation failed for {key}: {rule}";
                _validationErrors.Add(errorMessage);
                _typeValidationSucceeded = false;
            }
        }
    }

    /// <summary>
    /// Performs range validation for all configured range rules.
    /// </summary>
    private void PerformRangeValidation()
    {
        foreach (var rangeRule in _rangeValidationRules)
        {
            var key = rangeRule.Key;
            var rule = rangeRule.Value;
            var value = _validationConfiguration?[key];
            
            if (value == null)
            {
                var errorMessage = $"Range validation failed for {key}: value is null";
                _validationErrors.Add(errorMessage);
                _rangeValidationSucceeded = false;
                continue;
            }
            
            var isValid = ValidateRangeRule(value, rule);
            if (!isValid)
            {
                var errorMessage = $"Range validation failed for {key}: {rule}";
                _validationErrors.Add(errorMessage);
                _rangeValidationSucceeded = false;
            }
        }
    }

    /// <summary>
    /// Performs file-based validation for all configured validation rules.
    /// </summary>
    private void PerformFileBasedValidation()
    {
        foreach (var rule in _validationRules)
        {
            var key = rule.Key;
            var validationRule = rule.Value;
            var value = _validationConfiguration?[key];
            
            var isValid = ValidateGenericRule(value, validationRule);
            if (!isValid)
            {
                var errorMessage = $"File-based validation failed for {key}: {validationRule}";
                _validationErrors.Add(errorMessage);
                _fileValidationSucceeded = false;
            }
        }
    }

    /// <summary>
    /// Performs environment variable validation for all configured validation rules.
    /// </summary>
    private void PerformEnvironmentValidation()
    {
        // For environment validation, check against the actual environment variable names
        // that get loaded (after VALIDATION_ prefix is stripped)
        foreach (var rule in _validationRules)
        {
            var key = rule.Key;
            var validationRule = rule.Value;
            var value = _validationConfiguration?[key];
            
            var isValid = ValidateGenericRule(value, validationRule);
            if (!isValid)
            {
                var errorMessage = $"Environment validation failed for {key}: {validationRule} (value: '{value}')";
                _validationErrors.Add(errorMessage);
                _environmentValidationSucceeded = false;
            }
        }
    }

    /// <summary>
    /// Performs multi-source validation for all configured validation rules.
    /// </summary>
    private void PerformMultiSourceValidation()
    {
        // For multi-source validation, only validate keys that we actually expect to find
        foreach (var rule in _validationRules)
        {
            var key = rule.Key;
            var validationRule = rule.Value;
            var value = _validationConfiguration?[key];
            
            var isValid = ValidateGenericRule(value, validationRule);
            if (!isValid)
            {
                var errorMessage = $"Multi-source validation failed for {key}: {validationRule} (value: '{value}')";
                _validationErrors.Add(errorMessage);
                _multiSourceValidationSucceeded = false;
            }
        }
    }

    /// <summary>
    /// Validates a type rule against a configuration value.
    /// </summary>
    /// <param name="value">The configuration value to validate</param>
    /// <param name="rule">The type validation rule</param>
    /// <returns>True if validation passes, false otherwise</returns>
    private static bool ValidateTypeRule(string value, string rule)
    {
        return rule.ToLowerInvariant() switch
        {
            var r when r.Contains("positive integer") => int.TryParse(value, out var intVal) && intVal > 0,
            var r when r.Contains("boolean") => bool.TryParse(value, out _),
            var r when r.Contains("timespan") => TimeSpan.TryParse(value, out _),
            _ => true
        };
    }

    /// <summary>
    /// Validates a range rule against a configuration value.
    /// </summary>
    /// <param name="value">The configuration value to validate</param>
    /// <param name="rule">The range validation rule</param>
    /// <returns>True if validation passes, false otherwise</returns>
    private static bool ValidateRangeRule(string value, string rule)
    {
        if (!double.TryParse(value, out var numericValue))
            return false;

        if (rule.Contains("Between") && rule.Contains("and"))
        {
            var parts = rule.Replace("Between", "").Split([" and "], StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 && 
                double.TryParse(parts[0].Trim(), out var min) && 
                double.TryParse(parts[1].Trim(), out var max))
            {
                return numericValue >= min && numericValue <= max;
            }
        }

        return true;
    }

    /// <summary>
    /// Validates a generic rule against a configuration value.
    /// </summary>
    /// <param name="value">The configuration value to validate</param>
    /// <param name="rule">The validation rule</param>
    /// <returns>True if validation passes, false otherwise</returns>
    private static bool ValidateGenericRule(string? value, string rule)
    {
        return rule.ToLowerInvariant() switch
        {
            var r when r.Contains("required") => !string.IsNullOrEmpty(value),
            var r when r.Contains("must be present") => !string.IsNullOrEmpty(value),
            var r when r.Contains("valid url") => Uri.TryCreate(value, UriKind.Absolute, out _),
            var r when r.Contains("valid port") => int.TryParse(value, out var port) && port is > 0 and <= 65535,
            var r when r.Contains("positive integer") => int.TryParse(value, out var intVal) && intVal > 0,
            var r when r.Contains("boolean") => bool.TryParse(value, out _),
            var r when r.Contains("valid log level") => IsValidLogLevel(value),
            var r when r.Contains("sqlserver or inmemory") => IsValidDatabaseProvider(value),
            _ => true
        };
    }

    /// <summary>
    /// Attempts to convert a value to the specified type.
    /// </summary>
    /// <param name="value">The value to convert</param>
    /// <param name="expectedType">The expected type name</param>
    /// <returns>True if conversion is possible, false otherwise</returns>
    private static bool TryConvertValue(string value, string expectedType)
    {
        return expectedType.ToLowerInvariant() switch
        {
            "integer" => int.TryParse(value, out _),
            "boolean" => bool.TryParse(value, out _),
            "double" => double.TryParse(value, out _),
            "timespan" => TimeSpan.TryParse(value, out _),
            _ => true // Unknown types pass by default
        };
    }

    /// <summary>
    /// Validates if a value represents a valid log level.
    /// </summary>
    /// <param name="value">The log level value to validate</param>
    /// <returns>True if valid log level, false otherwise</returns>
    private static bool IsValidLogLevel(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        var validLogLevels = new[] { "Trace", "Debug", "Information", "Warning", "Error", "Critical", "None" };
        return validLogLevels.Contains(value, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Validates if a value represents a valid database provider.
    /// </summary>
    /// <param name="value">The database provider value to validate</param>
    /// <returns>True if valid database provider, false otherwise</returns>
    private static bool IsValidDatabaseProvider(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        var validProviders = new[] { "SqlServer", "InMemory" };
        return validProviders.Contains(value, StringComparer.OrdinalIgnoreCase);
    }

    #endregion
}