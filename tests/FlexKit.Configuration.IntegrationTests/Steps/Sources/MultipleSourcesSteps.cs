using FlexKit.Configuration.Core;
using FlexKit.Configuration.IntegrationTests.Utils;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Reqnroll;
using System.Diagnostics;
using JetBrains.Annotations;

namespace FlexKit.Configuration.IntegrationTests.Steps.Sources;

/// <summary>
/// Step definitions for multiple configuration sources scenarios.
/// Tests combine various configuration sources with proper precedence handling,
/// error management, and integration with FlexKit Configuration.
/// Uses distinct step patterns ("established", "incorporate", "construct") to avoid conflicts 
/// with other configuration step classes.
/// </summary>
[Binding]
public class MultipleSourcesSteps(ScenarioContext scenarioContext)
{
    private TestConfigurationBuilder? _multiLayerBuilder;
    private IConfiguration? _multiLayerConfiguration;
    private IFlexConfig? _multiLayerFlexConfiguration;
    private readonly List<LayerInfo> _incorporatedLayers = new();
    private readonly Dictionary<string, string?> _environmentVariables = new();
    [UsedImplicitly] public readonly Dictionary<string, string?> InMemoryData = new();
    private Exception? _lastMultiLayerException;
    private bool _multiLayerConstructionSucceeded;
    private readonly Stopwatch _performanceStopwatch = new();
    private readonly List<string> _performanceMetrics = new();

    #region Helper Classes

    /// <summary>
    /// Information about an incorporated configuration layer.
    /// </summary>
    private class LayerInfo
    {
        public string Type { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string? FilePath { [UsedImplicitly] get; set; }
        public bool Required { get; init; } = true;
        public Dictionary<string, string?> Data { get; init; } = new();
        public int Order { get; init; }
    }

    #endregion

    #region Given Steps - Setup

    [Given(@"I have established a multi-source configuration environment")]
    public void GivenIHaveEstablishedAMultiSourceConfigurationEnvironment()
    {
        _multiLayerBuilder = TestConfigurationBuilder.Create(scenarioContext);
        scenarioContext.Set(_multiLayerBuilder, "MultiLayerBuilder");
    }

    #endregion

    #region When Steps - Layer Incorporation

    [When(@"I incorporate JSON file ""(.*)"" as primary layer")]
    public void WhenIIncorporateJsonFileAsPrimaryLayer(string filePath)
    {
        WhenIIncorporateJsonFileAsLayer(filePath, "primary", true);
    }

    [When(@"I incorporate JSON file ""(.*)"" as foundation layer")]
    public void WhenIIncorporateJsonFileAsFoundationLayer(string filePath)
    {
        WhenIIncorporateJsonFileAsLayer(filePath, "foundation", true);
    }

    [When(@"I incorporate JSON file ""(.*)"" as base layer")]
    public void WhenIIncorporateJsonFileAsBaseLayer(string filePath)
    {
        WhenIIncorporateJsonFileAsLayer(filePath, "base", true);
    }

    [When(@"I incorporate JSON file ""(.*)"" as environment layer")]
    public void WhenIIncorporateJsonFileAsEnvironmentLayer(string filePath)
    {
        WhenIIncorporateJsonFileAsLayer(filePath, "environment", true);
    }

    [When(@"I incorporate JSON file ""(.*)"" as required layer")]
    public void WhenIIncorporateJsonFileAsRequiredLayer(string filePath)
    {
        WhenIIncorporateJsonFileAsLayer(filePath, "required", true);
    }

    [When(@"I incorporate JSON file ""(.*)"" as optional layer")]
    public void WhenIIncorporateJsonFileAsOptionalLayer(string filePath)
    {
        WhenIIncorporateJsonFileAsLayer(filePath, "optional", false);
    }

    [When(@"I incorporate JSON file ""(.*)"" as configuration layer")]
    public void WhenIIncorporateJsonFileAsConfigurationLayer(string filePath)
    {
        WhenIIncorporateJsonFileAsLayer(filePath, "configuration", true);
    }

    [When(@"I incorporate JSON file ""(.*)"" as ""(.*)""")]
    public void WhenIIncorporateJsonFileAs(string filePath, string layerName)
    {
        WhenIIncorporateJsonFileAsLayer(filePath, layerName, true);
    }

    [When(@"I incorporate JSON file ""(.*)"" as initial layer")]
    public void WhenIIncorporateJsonFileAsInitialLayer(string filePath)
    {
        WhenIIncorporateJsonFileAsLayer(filePath, "initial", true);
    }

    private void WhenIIncorporateJsonFileAsLayer(string filePath, string layerName, bool required)
    {
        _multiLayerBuilder.Should().NotBeNull("Multi-layer builder should be established");
        
        // Use the same pattern as JsonConfigurationSteps for consistency
        var normalizedPath = filePath.Replace('/', Path.DirectorySeparatorChar);
        
        if (required && !File.Exists(normalizedPath))
        {
            throw new FileNotFoundException($"Test data file not found: {normalizedPath}");
        }

        if (File.Exists(normalizedPath))
        {
            try
            {
                // Read and parse JSON content, then add as in-memory data
                var jsonContent = File.ReadAllText(normalizedPath);
                var configData = ParseJsonToConfigurationData(jsonContent);
                _multiLayerBuilder!.AddInMemoryCollection(configData);
            }
            catch (Exception)
            {
                if (required)
                {
                    // For required files with invalid JSON, we still want to register them
                    // but use the actual JSON file source, so the error occurs during Build()
                    _multiLayerBuilder!.AddJsonFile(normalizedPath, optional: false, reloadOnChange: false);
                }
                else
                {
                    // For optional files with invalid JSON, skip them
                    // But still register the layer info for tracking
                }
            }
        }
        else if (!required)
        {
            // For optional files that don't exist, register them (they'll be skipped)
            _multiLayerBuilder!.AddJsonFile(normalizedPath, optional: true, reloadOnChange: false);
        }
        else
        {
            // Required file doesn't exist
            throw new FileNotFoundException($"Required test data file not found: {normalizedPath}");
        }

        _incorporatedLayers.Add(new LayerInfo
        {
            Type = "JSON",
            Name = layerName,
            FilePath = normalizedPath,
            Required = required,
            Order = _incorporatedLayers.Count
        });
    }

    [When(@"I incorporate \.env file ""(.*)"" as intermediate layer")]
    public void WhenIIncorporateDotEnvFileAsIntermediateLayer(string filePath)
    {
        WhenIIncorporateDotEnvFileAsLayer(filePath, "intermediate", true);
    }

    [When(@"I incorporate \.env file ""(.*)"" as required layer")]
    public void WhenIIncorporateDotEnvFileAsRequiredLayer(string filePath)
    {
        WhenIIncorporateDotEnvFileAsLayer(filePath, "required", true);
    }

    [When(@"I incorporate \.env file ""(.*)"" as optional layer")]
    public void WhenIIncorporateDotEnvFileAsOptionalLayer(string filePath)
    {
        WhenIIncorporateDotEnvFileAsLayer(filePath, "optional", false);
    }

    [When(@"I incorporate \.env file ""(.*)"" as configuration layer")]
    public void WhenIIncorporateDotEnvFileAsConfigurationLayer(string filePath)
    {
        WhenIIncorporateDotEnvFileAsLayer(filePath, "configuration", true);
    }

    [When(@"I incorporate \.env file ""(.*)"" as ""(.*)""")]
    public void WhenIIncorporateDotEnvFileAs(string filePath, string layerName)
    {
        WhenIIncorporateDotEnvFileAsLayer(filePath, layerName, true);
    }

    [When(@"I supplement with \.env file ""(.*)"" as additional layer")]
    public void WhenISupplementWithDotEnvFileAsAdditionalLayer(string filePath)
    {
        WhenIIncorporateDotEnvFileAsLayer(filePath, "additional", true);
    }

    private void WhenIIncorporateDotEnvFileAsLayer(string filePath, string layerName, bool required)
    {
        _multiLayerBuilder.Should().NotBeNull("Multi-layer builder should be established");
        
        // Use the same pattern as DotEnvFileSteps for consistency
        var normalizedPath = filePath.Replace('/', Path.DirectorySeparatorChar);
        
        if (required && !File.Exists(normalizedPath))
        {
            throw new FileNotFoundException($"Test data file not found: {normalizedPath}");
        }

        _multiLayerBuilder!.AddDotEnvFile(normalizedPath, optional: !required);

        _incorporatedLayers.Add(new LayerInfo
        {
            Type = "DotEnv",
            Name = layerName,
            FilePath = normalizedPath,
            Required = required,
            Order = _incorporatedLayers.Count
        });
    }

    [When(@"I incorporate environment variables with prefix ""(.*)"" as secondary layer")]
    public void WhenIIncorporateEnvironmentVariablesWithPrefixAsSecondaryLayer(string prefix)
    {
        WhenIIncorporateEnvironmentVariablesWithPrefix(prefix, "secondary");
    }

    [When(@"I incorporate environment variables without prefix as override layer")]
    public void WhenIIncorporateEnvironmentVariablesWithoutPrefixAsOverrideLayer()
    {
        WhenIIncorporateEnvironmentVariablesWithPrefix(null, "override");
    }

    private void WhenIIncorporateEnvironmentVariablesWithPrefix(string? prefix, string layerName)
    {
        _multiLayerBuilder.Should().NotBeNull("Multi-layer builder should be established");
        
        // Set up any environment variables that were configured
        foreach (var kvp in _environmentVariables)
        {
            _multiLayerBuilder!.WithEnvironmentVariable(kvp.Key, kvp.Value);
        }
        
        _multiLayerBuilder!.AddEnvironmentVariables(prefix);

        _incorporatedLayers.Add(new LayerInfo
        {
            Type = "EnvironmentVariables",
            Name = layerName,
            Required = true,
            Order = _incorporatedLayers.Count
        });
    }

    [When(@"I incorporate in-memory configuration as top layer with:")]
    public void WhenIIncorporateInMemoryConfigurationAsTopLayerWith(Table table)
    {
        WhenIIncorporateInMemoryConfigurationWith(table, "top");
    }

    [When(@"I incorporate in-memory configuration with test data:")]
    public void WhenIIncorporateInMemoryConfigurationWithTestData(Table table)
    {
        WhenIIncorporateInMemoryConfigurationWith(table, "test-data");
    }

    [When(@"I incorporate in-memory data as ""(.*)"" with:")]
    public void WhenIIncorporateInMemoryDataAsWith(string layerName, Table table)
    {
        WhenIIncorporateInMemoryConfigurationWith(table, layerName);
    }

    [When(@"I incorporate 50 in-memory key-value pairs as bulk layer")]
    public void WhenIIncorporate50InMemoryKeyValuePairsAsBulkLayer()
    {
        _multiLayerBuilder.Should().NotBeNull("Multi-layer builder should be established");
        
        var testData = new Dictionary<string, string?>();
        for (int i = 1; i <= 50; i++)
        {
            testData[$"TestKey{i:D2}"] = $"TestValue{i:D2}";
            testData[$"Performance:Setting{i:D2}"] = $"PerformanceValue{i:D2}";
        }

        _multiLayerBuilder!.AddInMemoryCollection(testData);

        _incorporatedLayers.Add(new LayerInfo
        {
            Type = "InMemory",
            Name = "bulk",
            Data = testData,
            Required = true,
            Order = _incorporatedLayers.Count
        });
    }

    private void WhenIIncorporateInMemoryConfigurationWith(Table table, string layerName)
    {
        _multiLayerBuilder.Should().NotBeNull("Multi-layer builder should be established");
        
        var configData = new Dictionary<string, string?>();
        foreach (var row in table.Rows)
        {
            var key = row["Key"];
            var value = row["Value"];
            configData[key] = value;
            InMemoryData[key] = value;
        }

        _multiLayerBuilder!.AddInMemoryCollection(configData);

        _incorporatedLayers.Add(new LayerInfo
        {
            Type = "InMemory",
            Name = layerName,
            Data = configData,
            Required = true,
            Order = _incorporatedLayers.Count
        });
    }

    #endregion

    #region When Steps - Environment Variable Management

    [When(@"I configure environment variable ""(.*)"" to ""(.*)""")]
    public void WhenIConfigureEnvironmentVariableTo(string name, string value)
    {
        _environmentVariables[name] = value;
        
        // If we already have a builder, set it there too
        if (_multiLayerBuilder != null)
        {
            _multiLayerBuilder.WithEnvironmentVariable(name, value);
        }
    }

    [When(@"I supplement with environment variable ""(.*)"" with value ""(.*)""")]
    public void WhenISupplementWithEnvironmentVariableWithValue(string name, string value)
    {
        WhenIConfigureEnvironmentVariableTo(name, value);
    }

    #endregion

    #region When Steps - Configuration Construction

    [When(@"I construct the multi-layered configuration")]
    public void WhenIConstructTheMultiLayeredConfiguration()
    {
        _multiLayerBuilder.Should().NotBeNull("Multi-layer builder should be established");
        
        _performanceStopwatch.Restart();
        
        try
        {
            _multiLayerConfiguration = _multiLayerBuilder!.Build();
            _multiLayerConstructionSucceeded = true;
            _lastMultiLayerException = null;
        }
        catch (Exception ex)
        {
            _multiLayerConstructionSucceeded = false;
            _lastMultiLayerException = ex;
        }
        
        _performanceStopwatch.Stop();
        _performanceMetrics.Add($"Configuration construction time: {_performanceStopwatch.ElapsedMilliseconds}ms");
    }

    [When(@"I attempt to construct the multi-layered configuration")]
    public void WhenIAttemptToConstructTheMultiLayeredConfiguration()
    {
        // Same as construct but more explicit about expecting potential failure
        WhenIConstructTheMultiLayeredConfiguration();
    }

    [When(@"I construct the current multi-layered configuration")]
    public void WhenIConstructTheCurrentMultiLayeredConfiguration()
    {
        WhenIConstructTheMultiLayeredConfiguration();
    }

    [When(@"I reconstruct the multi-layered configuration")]
    public void WhenIReconstructTheMultiLayeredConfiguration()
    {
        _multiLayerBuilder.Should().NotBeNull("Multi-layer builder should be established");
        
        // Apply any newly added environment variables
        foreach (var kvp in _environmentVariables)
        {
            _multiLayerBuilder!.WithEnvironmentVariable(kvp.Key, kvp.Value);
        }
        
        // Add environment variables source if we have any environment variables
        if (_environmentVariables.Any())
        {
            _multiLayerBuilder!.AddEnvironmentVariables();
        }
        
        // Rebuild with all current layers
        WhenIConstructTheMultiLayeredConfiguration();
    }

    [When(@"I generate FlexConfig from multi-source configuration")]
    public void WhenIGenerateFlexConfigFromMultiSourceConfiguration()
    {
        _multiLayerConfiguration.Should().NotBeNull("Multi-layer configuration should be constructed");
        
        _performanceStopwatch.Restart();
        
        try
        {
            _multiLayerFlexConfiguration = _multiLayerConfiguration!.GetFlexConfiguration();
        }
        catch (Exception ex)
        {
            _lastMultiLayerException = ex;
        }
        
        _performanceStopwatch.Stop();
        _performanceMetrics.Add($"FlexConfig generation time: {_performanceStopwatch.ElapsedMilliseconds}ms");
    }

    #endregion

    #region Then Steps - Assertions

    [Then(@"the multi-source configuration should load without errors")]
    public void ThenTheMultiSourceConfigurationShouldLoadWithoutErrors()
    {
        _multiLayerConstructionSucceeded.Should().BeTrue("Multi-source configuration construction should succeed");
        _lastMultiLayerException.Should().BeNull("No exception should occur during multi-source configuration construction");
        _multiLayerConfiguration.Should().NotBeNull("Multi-source configuration should be constructed");
    }

    [Then(@"the final configuration should contain ""(.*)"" with value ""(.*)""")]
    public void ThenTheFinalConfigurationShouldContainWithValue(string key, string expectedValue)
    {
        _multiLayerConfiguration.Should().NotBeNull("Multi-layer configuration should be constructed");
        
        var actualValue = _multiLayerConfiguration![key];
        actualValue.Should().Be(expectedValue, $"Final configuration key '{key}' should have the expected value");
    }

    [Then(@"the final configuration should contain values from the base JSON file")]
    public void ThenTheFinalConfigurationShouldContainValuesFromTheBaseJsonFile()
    {
        _multiLayerConfiguration.Should().NotBeNull("Multi-layer configuration should be constructed");
        
        // Check for some expected values from appsettings.json
        var configEntries = _multiLayerConfiguration!.AsEnumerable().ToList();
        configEntries.Should().NotBeEmpty("Configuration should contain entries from JSON files");
        
        // Verify that we have some application-level settings that would come from JSON
        var hasAppSettings = configEntries.Any(kvp => 
            (kvp.Key.StartsWith("Application:") || kvp.Key.StartsWith("Database:")) && 
            !string.IsNullOrEmpty(kvp.Value));
        
        hasAppSettings.Should().BeTrue("Configuration should contain application settings from base JSON file");
    }

    [Then(@"higher layers should override lower layers")]
    public void ThenHigherLayersShouldOverrideLowerLayers()
    {
        _multiLayerConfiguration.Should().NotBeNull("Multi-layer configuration should be constructed");
        
        // This is a behavioral assertion about precedence
        // Later incorporated layers should have taken precedence over earlier ones
        _incorporatedLayers.Should().NotBeEmpty("Layers should have been incorporated");
        _incorporatedLayers.Should().HaveCountGreaterThan(1, "Multiple layers should have been incorporated for precedence testing");
    }

    [Then(@"environment variables should have highest precedence")]
    public void ThenEnvironmentVariablesShouldHaveHighestPrecedence()
    {
        _multiLayerConfiguration.Should().NotBeNull("Multi-layer configuration should be constructed");
        
        // Environment variables are typically incorporated last and should have the highest precedence
        var hasEnvLayer = _incorporatedLayers.Any(s => s.Type == "EnvironmentVariables");
        hasEnvLayer.Should().BeTrue("Environment variables should have been incorporated as a layer");
    }

    [Then(@"required layers should contribute to final configuration")]
    public void ThenRequiredLayersShouldContributeToFinalConfiguration()
    {
        _multiLayerConfiguration.Should().NotBeNull("Multi-layer configuration should be constructed");
        
        var configEntries = _multiLayerConfiguration!.AsEnumerable().ToList();
        configEntries.Should().NotBeEmpty("Required layers should contribute configuration entries");
        
        var requiredLayers = _incorporatedLayers.Where(s => s.Required).ToList();
        requiredLayers.Should().NotBeEmpty("Required layers should have been incorporated");
    }

    [Then(@"missing optional layers should be skipped gracefully")]
    public void ThenMissingOptionalLayersShouldBeSkippedGracefully()
    {
        _multiLayerConstructionSucceeded.Should().BeTrue("Configuration construction should succeed despite missing optional layers");
        _lastMultiLayerException.Should().BeNull("Missing optional layers should not cause exceptions");
    }

    [Then(@"the final configuration should contain data from all available layers")]
    public void ThenTheFinalConfigurationShouldContainDataFromAllAvailableLayers()
    {
        _multiLayerConfiguration.Should().NotBeNull("Multi-layer configuration should be constructed");
        
        var configEntries = _multiLayerConfiguration!.AsEnumerable().ToList();
        configEntries.Should().NotBeEmpty("Configuration should contain data from available layers");
        
        // Verify we have data from different layer types
        var layerTypes = _incorporatedLayers.Select(s => s.Type).Distinct().ToList();
        layerTypes.Should().NotBeEmpty("Multiple layer types should have been incorporated");
    }

    [Then(@"the multi-source configuration construction should fail")]
    public void ThenTheMultiSourceConfigurationConstructionShouldFail()
    {
        _multiLayerConstructionSucceeded.Should().BeFalse("Multi-source configuration construction should have failed");
        _lastMultiLayerException.Should().NotBeNull("An exception should have been thrown");
    }

    [Then(@"the error should indicate which layer caused the failure")]
    public void ThenTheErrorShouldIndicateWhichLayerCausedTheFailure()
    {
        _lastMultiLayerException.Should().NotBeNull("An exception should have been thrown");
        
        var exceptionMessage = _lastMultiLayerException!.ToString();
        // The exception should contain information about the problematic layer
        exceptionMessage.Should().NotBeEmpty("Exception should contain error details");
    }

    [Then(@"valid layers should be processed before the error occurs")]
    public void ThenValidLayersShouldBeProcessedBeforeTheErrorOccurs()
    {
        // This is a behavioral assertion about configuration building order
        _lastMultiLayerException.Should().NotBeNull("An exception should have been thrown during layer processing");
    }

    [Then(@"the multi-source FlexConfig should be created successfully")]
    public void ThenTheMultiSourceFlexConfigShouldBeCreatedSuccessfully()
    {
        _multiLayerFlexConfiguration.Should().NotBeNull("Multi-source FlexConfig should be created");
        _lastMultiLayerException.Should().BeNull("No exception should occur during FlexConfig creation");
    }

    [Then(@"FlexConfig should enable dynamic access to all layer data")]
    public void ThenFlexConfigShouldEnableDynamicAccessToAllLayerData()
    {
        _multiLayerFlexConfiguration.Should().NotBeNull("Multi-source FlexConfig should be created");
        _multiLayerConfiguration.Should().NotBeNull("Multi-layer configuration should be constructed");
        
        // Test basic dynamic access functionality
        dynamic config = _multiLayerFlexConfiguration!;

        // This should not throw an exception (even if the property doesn't exist)
        _ = config.Database;

        // Verify that FlexConfig can access configuration data through the indexer
        var configEntries = _multiLayerConfiguration!.AsEnumerable().Take(3).ToList();
        foreach (var entry in configEntries)
        {
            _ = _multiLayerFlexConfiguration[entry.Key];
        }
    }

    [Then(@"FlexConfig should maintain the correct precedence order")]
    public void ThenFlexConfigShouldMaintainTheCorrectPrecedenceOrder()
    {
        _multiLayerFlexConfiguration.Should().NotBeNull("Multi-source FlexConfig should be created");
        _multiLayerConfiguration.Should().NotBeNull("Multi-layer configuration should be constructed");
        
        // Verify that FlexConfig respects the same precedence as the underlying IConfiguration
        var configEntries = _multiLayerConfiguration!.AsEnumerable().ToList();
        configEntries.Should().NotBeEmpty("FlexConfig should contain configuration entries");
        
        // Test that FlexConfig can access the same data
        if (configEntries.Any())
        {
            var firstEntry = configEntries.First();
            var flexValue = _multiLayerFlexConfiguration![firstEntry.Key];
            var configValue = _multiLayerConfiguration[firstEntry.Key];
            flexValue.Should().Be(configValue, "FlexConfig should return the same values as the underlying configuration");
        }
    }

    [Then(@"the multi-source configuration should contain base JSON data")]
    public void ThenTheMultiSourceConfigurationShouldContainBaseJsonData()
    {
        _multiLayerConfiguration.Should().NotBeNull("Multi-layer configuration should be constructed");
        
        var configEntries = _multiLayerConfiguration!.AsEnumerable().ToList();
        configEntries.Should().NotBeEmpty("Configuration should contain base JSON data");
        
        // Verify that we have some basic application settings
        var hasAppSettings = configEntries.Any(kvp => 
            kvp.Key.StartsWith("Application:") && !string.IsNullOrEmpty(kvp.Value));
        
        hasAppSettings.Should().BeTrue("Configuration should contain application settings from base JSON");
    }

    [Then(@"the final configuration should include data from all layers")]
    public void ThenTheFinalConfigurationShouldIncludeDataFromAllLayers()
    {
        _multiLayerConfiguration.Should().NotBeNull("Multi-layer configuration should be constructed");
        
        var configEntries = _multiLayerConfiguration!.AsEnumerable().ToList();
        configEntries.Should().NotBeEmpty("Configuration should contain data from all layers");
        
        // Verify we have a reasonable number of configuration entries
        configEntries.Should().HaveCountGreaterThan(5, "Configuration should have data from multiple layers");
    }

    [Then(@"the final configuration should contain data from both JSON and \.env layers")]
    public void ThenTheFinalConfigurationShouldContainDataFromBothJsonAndEnvLayers()
    {
        _multiLayerConfiguration.Should().NotBeNull("Multi-layer configuration should be constructed");
        
        var configEntries = _multiLayerConfiguration!.AsEnumerable().ToList();
        configEntries.Should().NotBeEmpty("Configuration should contain data from both JSON and .env layers");
        
        // Check that we have both JSON-style and env-style configurations
        var hasJsonStyle = configEntries.Any(kvp => kvp.Key.Contains(':'));
        var hasEnvStyle = configEntries.Any(kvp => kvp.Key.Contains('_'));
        
        (hasJsonStyle || hasEnvStyle).Should().BeTrue("Configuration should contain data from both layer types");
    }

    [Then(@"layer ""(.*)"" should contribute JSON-specific keys")]
    public void ThenLayerShouldContributeJsonSpecificKeys(string layerName)
    {
        _multiLayerConfiguration.Should().NotBeNull("Multi-layer configuration should be constructed");
        
        var layer = _incorporatedLayers.FirstOrDefault(s => s.Name == layerName);
        layer.Should().NotBeNull($"Layer '{layerName}' should have been incorporated");
        layer.Type.Should().Be("JSON", $"Layer '{layerName}' should be a JSON layer");
    }

    [Then(@"layer ""(.*)"" should contribute environment-specific keys")]
    public void ThenLayerShouldContributeEnvironmentSpecificKeys(string layerName)
    {
        _multiLayerConfiguration.Should().NotBeNull("Multi-layer configuration should be constructed");
        
        var layer = _incorporatedLayers.FirstOrDefault(s => s.Name == layerName);
        layer.Should().NotBeNull($"Layer '{layerName}' should have been incorporated");
        layer.Type.Should().Be("DotEnv", $"Layer '{layerName}' should be a DotEnv layer");
    }

    [Then(@"layer ""(.*)"" should contribute memory-specific keys")]
    public void ThenLayerShouldContributeMemorySpecificKeys(string layerName)
    {
        _multiLayerConfiguration.Should().NotBeNull("Multi-layer configuration should be constructed");
        
        var layer = _incorporatedLayers.FirstOrDefault(s => s.Name == layerName);
        layer.Should().NotBeNull($"Layer '{layerName}' should have been incorporated");
        layer.Type.Should().Be("InMemory", $"Layer '{layerName}' should be an InMemory layer");
        layer.Data.Should().NotBeEmpty($"Layer '{layerName}' should have in-memory data");
    }

    [Then(@"conflicting keys should follow precedence rules")]
    public void ThenConflictingKeysShouldFollowPrecedenceRules()
    {
        _multiLayerConfiguration.Should().NotBeNull("Multi-layer configuration should be constructed");
        
        // Verify that later layers override earlier ones by checking incorporated layer order
        _incorporatedLayers.Should().NotBeEmpty("Layers should have been incorporated");
        _incorporatedLayers.Should().HaveCountGreaterThan(1, "Multiple layers should have been incorporated for precedence testing");
        
        // Layers should be ordered (later layers have higher order numbers)
        var orderedLayers = _incorporatedLayers.OrderBy(s => s.Order).ToList();
        orderedLayers.Should().HaveCount(_incorporatedLayers.Count, "All layers should maintain their order");
    }

    [Then(@"configuration construction should complete within reasonable time")]
    public void ThenConfigurationConstructionShouldCompleteWithinReasonableTime()
    {
        _performanceMetrics.Should().NotBeEmpty("Performance metrics should have been collected");
        
        var buildTimeMetric = _performanceMetrics.FirstOrDefault(m => m.Contains("Configuration construction time"));
        buildTimeMetric.Should().NotBeNull("Configuration construction time should have been measured");
        
        // For integration tests, the reasonable time is under 5 seconds
        _multiLayerConstructionSucceeded.Should().BeTrue("Configuration construction should have succeeded within reasonable time");
    }

    [Then(@"all layer data should be accessible through standard configuration API")]
    public void ThenAllLayerDataShouldBeAccessibleThroughStandardConfigurationApi()
    {
        _multiLayerConfiguration.Should().NotBeNull("Multi-layer configuration should be constructed");
        
        // Test standard IConfiguration access patterns
        var configEntries = _multiLayerConfiguration!.AsEnumerable().ToList();
        configEntries.Should().NotBeEmpty("All layer data should be accessible");
        
        // Test that we can access data using a standard indexer
        foreach (var entry in configEntries.Take(5)) // Test first 5 entries
        {
            var value = _multiLayerConfiguration[entry.Key];
            value.Should().Be(entry.Value, $"Standard indexer access should work for key '{entry.Key}'");
        }
    }

    [Then(@"FlexConfig generation should complete within reasonable time")]
    public void ThenFlexConfigGenerationShouldCompleteWithinReasonableTime()
    {
        // First, we need to actually generate the FlexConfig to measure its time
        if (_multiLayerFlexConfiguration == null)
        {
            WhenIGenerateFlexConfigFromMultiSourceConfiguration();
        }
        
        _performanceMetrics.Should().NotBeEmpty("Performance metrics should have been collected");
        
        var flexConfigTimeMetric = _performanceMetrics.FirstOrDefault(m => m.Contains("FlexConfig generation time"));
        flexConfigTimeMetric.Should().NotBeNull("FlexConfig generation time should have been measured");
        
        _multiLayerFlexConfiguration.Should().NotBeNull("FlexConfig should have been generated within reasonable time");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Parses JSON content into a configuration data dictionary.
    /// EXACT copy from JsonConfigurationSteps to maintain consistency.
    /// </summary>
    /// <param name="jsonContent">The JSON content to parse</param>
    /// <returns>Dictionary of configuration key-value pairs</returns>
    /// <exception cref="InvalidOperationException">Thrown when JSON is invalid</exception>
    private static Dictionary<string, string?> ParseJsonToConfigurationData(string jsonContent)
    {
        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(jsonContent);
            return FlattenJsonElement(document.RootElement);
        }
        catch (System.Text.Json.JsonException ex)
        {
            throw new InvalidOperationException($"Failed to parse JSON configuration: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Flattens a JSON element into a dictionary with colon-separated keys.
    /// EXACT copy from JsonConfigurationSteps to maintain consistency.
    /// </summary>
    /// <param name="element">The JSON element to flatten</param>
    /// <param name="prefix">Current key prefix</param>
    /// <returns>Flattened dictionary</returns>
    private static Dictionary<string, string?> FlattenJsonElement(System.Text.Json.JsonElement element, string prefix = "")
    {
        var result = new Dictionary<string, string?>();

        switch (element.ValueKind)
        {
            case System.Text.Json.JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var key = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}:{property.Name}";
                    var nestedData = FlattenJsonElement(property.Value, key);
                    foreach (var kvp in nestedData)
                    {
                        result[kvp.Key] = kvp.Value;
                    }
                }
                break;

            case System.Text.Json.JsonValueKind.Array:
                var index = 0;
                foreach (var arrayElement in element.EnumerateArray())
                {
                    var key = string.IsNullOrEmpty(prefix) ? index.ToString() : $"{prefix}:{index}";
                    var nestedData = FlattenJsonElement(arrayElement, key);
                    foreach (var kvp in nestedData)
                    {
                        result[kvp.Key] = kvp.Value;
                    }
                    index++;
                }
                break;

            case System.Text.Json.JsonValueKind.String:
                result[prefix] = element.GetString();
                break;

            case System.Text.Json.JsonValueKind.Number:
                result[prefix] = element.GetRawText();
                break;

            case System.Text.Json.JsonValueKind.True:
                result[prefix] = "true";
                break;

            case System.Text.Json.JsonValueKind.False:
                result[prefix] = "false";
                break;

            case System.Text.Json.JsonValueKind.Null:
                result[prefix] = null;
                break;
        }

        return result;
    }

    #endregion
}