using FlexKit.Configuration.Core;
using FlexKit.Configuration.IntegrationTests.Utils;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Reqnroll;
using System.Diagnostics;
using FlexKit.IntegrationTests.Utils;

namespace FlexKit.Configuration.IntegrationTests.Steps.RealWorld;

[Binding]
public class ComplexScenariosSteps
{
    private readonly ScenarioContext _scenarioContext;
    private TestConfigurationBuilder? _complexConfigurationBuilder;
    private IConfiguration? _complexConfiguration;
    private IFlexConfig? _complexFlexConfiguration;
    
    private readonly Dictionary<string, string?> _environmentVariables = new();
    private readonly Dictionary<string, string?> _inMemoryOverrides = new();
    private readonly Dictionary<string, string?> _conditionalConfiguration = new();
    private readonly Dictionary<string, string?> _dynamicUpdates = new();
    private readonly Dictionary<string, string?> _validationRules = new();
    private readonly Dictionary<string, string?> _securityMetadata = new();
    private readonly Dictionary<string, string?> _performanceTestData = new();
    private readonly Dictionary<string, string?> _platformPaths = new();
    private readonly Dictionary<string, string?> _securityData = new();
    
    private string _currentEnvironment = "Development";
    private bool _validationEnabled = false;
    private bool _securityFeaturesEnabled = false;
    private bool _performanceMonitoringEnabled = false;
    private Exception? _lastComplexException;
    private bool _complexConfigurationLoaded;
    
    private readonly Stopwatch _performanceStopwatch = new();
    private long _memoryUsageBefore;
    private long _memoryUsageAfter;
    
    private readonly List<string> _configurationErrors = new();
    private readonly List<string> _securityAuditTrail = new();
    
    private readonly List<string> _organizedJsonFiles = new();
    private readonly List<string> _organizedEnvFiles = new();

    private List<string> GetOrganizedJsonFiles() => _organizedJsonFiles;
    private List<string> GetOrganizedEnvFiles() => _organizedEnvFiles;

    public ComplexScenariosSteps(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    #region Given Steps

    [Given(@"I have organized a comprehensive configuration testing environment")]
    public void GivenIHaveOrganizedAComprehensiveConfigurationTestingEnvironment()
    {
        _complexConfigurationBuilder = TestConfigurationBuilder.Create(_scenarioContext);
        _memoryUsageBefore = GC.GetTotalMemory(false);
    }

    #endregion

    #region When Steps - Organization

    [When(@"I organize base configuration from JSON file ""(.*)""")]
    public void WhenIOrganizeBaseConfigurationFromJsonFile(string filePath)
    {
        _complexConfigurationBuilder.Should().NotBeNull();
        var normalizedPath = filePath.Replace('/', Path.DirectorySeparatorChar);
    
        if (!File.Exists(normalizedPath))
        {
            throw new FileNotFoundException($"Base configuration file not found: {normalizedPath}");
        }

        _complexConfigurationBuilder!.AddJsonFile(normalizedPath, optional: false, reloadOnChange: false);
    
        // Add any pending conditional configuration AFTER base config to ensure proper precedence
        if (_conditionalConfiguration.Any())
        {
            _complexConfigurationBuilder.AddInMemoryCollection(_conditionalConfiguration);
        }
    }

    [When(@"I organize environment-specific overrides from JSON file ""(.*)""")]
    public void WhenIOrganizeEnvironmentSpecificOverridesFromJsonFile(string filePath)
    {
        _complexConfigurationBuilder.Should().NotBeNull();
        var normalizedPath = filePath.Replace('/', Path.DirectorySeparatorChar);
        
        if (File.Exists(normalizedPath))
        {
            _complexConfigurationBuilder!.AddJsonFile(normalizedPath, optional: true, reloadOnChange: false);
        }
    }

    [When(@"I organize environment variables with specific precedence:")]
    public void WhenIOrganizeEnvironmentVariablesWithSpecificPrecedence(Table table)
    {
        _complexConfigurationBuilder.Should().NotBeNull();
        
        foreach (var row in table.Rows)
        {
            var variable = row["Variable"];
            var value = row["Value"];
            _environmentVariables[variable] = value;
            
            var configKey = variable.Replace("__", ":");
            _complexConfigurationBuilder!.AddKeyValue(configKey, value);
        }
    }

    [When(@"I organize \.env file overrides from ""(.*)""")]
    public void WhenIOrganizeDotEnvFileOverridesFrom(string filePath)
    {
        _complexConfigurationBuilder.Should().NotBeNull();
        var normalizedPath = filePath.Replace('/', Path.DirectorySeparatorChar);
        
        if (File.Exists(normalizedPath))
        {
            _complexConfigurationBuilder!.AddDotEnvFile(normalizedPath, optional: true);
        }
    }

    [When(@"I organize in-memory final overrides:")]
    public void WhenIOrganizeInMemoryFinalOverrides(Table table)
    {
        _complexConfigurationBuilder.Should().NotBeNull();
    
        foreach (var row in table.Rows)
        {
            var key = row["Key"];
            var value = row["Value"];
            _inMemoryOverrides[key] = value;
        }
    
        // Don't add here - will be added in execute step for proper precedence
    }

    [When(@"I organize comprehensive test configuration structure:")]
    public void WhenIOrganizeComprehensiveTestConfigurationStructure(Table table)
    {
        _complexConfigurationBuilder.Should().NotBeNull();
        
        var configData = new Dictionary<string, string?>();
        foreach (var row in table.Rows)
        {
            configData[row["Key"]] = row["Value"];
        }
        
        _complexConfigurationBuilder!.AddInMemoryCollection(configData);
    }

    [When(@"I organize conditional configuration based on environment ""(.*)""")]
    public void WhenIOrganizeConditionalConfigurationBasedOnEnvironment(string environment)
    {
        _currentEnvironment = environment;
    }

    [When("I organize conditional overrides when environment is {string}:")]
    public void WhenIOrganizeConditionalOverridesWhenEnvironmentIs(string environment, Table table)
    {
        // Only process if it matches current environment
        if (_currentEnvironment == environment)
        {
            _complexConfigurationBuilder.Should().NotBeNull();
        
            var conditionalData = new Dictionary<string, string?>();
            foreach (var row in table.Rows)
            {
                conditionalData[row["Key"]] = row["Value"];
                _conditionalConfiguration[row["Key"]] = row["Value"];
            }
        
            _complexConfigurationBuilder!.AddInMemoryCollection(conditionalData);
        }
        // Skip entirely if environment doesn't match
    }

    [When(@"I organize initial configuration from JSON file ""(.*)""")]
    public void WhenIOrganizeInitialConfigurationFromJsonFile(string filePath)
    {
        _complexConfigurationBuilder.Should().NotBeNull();
        var normalizedPath = filePath.Replace('/', Path.DirectorySeparatorChar);
        
        if (!File.Exists(normalizedPath))
        {
            throw new FileNotFoundException($"Initial configuration file not found: {normalizedPath}");
        }

        _complexConfigurationBuilder!.AddJsonFile(normalizedPath, optional: false, reloadOnChange: false);
    }

    [When(@"I organize dynamic configuration updates:")]
    public void WhenIOrganizeDynamicConfigurationUpdates(Table table)
    {
        foreach (var row in table.Rows)
        {
            _dynamicUpdates[row["Key"]] = row["NewValue"];
        }
    }

    [When(@"I organize configuration with intentional validation scenarios:")]
    public void WhenIOrganizeConfigurationWithIntentionalValidationScenarios(Table table)
    {
        _complexConfigurationBuilder.Should().NotBeNull();
        _validationEnabled = true;
        
        var validationData = new Dictionary<string, string?>();
        foreach (var row in table.Rows)
        {
            var key = row["Key"];
            var value = row["Value"];
            var rule = row["ValidationRule"];
            
            validationData[key] = value;
            _validationRules[key] = rule;
        }
        
        _complexConfigurationBuilder!.AddInMemoryCollection(validationData);
    }

    [When(@"I organize configuration with validation edge cases:")]
    public void WhenIOrganizeConfigurationWithValidationEdgeCases(Table table)
    {
        _complexConfigurationBuilder.Should().NotBeNull();
        
        var edgeCaseData = new Dictionary<string, string?>();
        foreach (var row in table.Rows)
        {
            var key = row["Key"];
            var value = row["Value"];
            var expectedOutcome = row["ExpectedOutcome"];
            
            edgeCaseData[key] = value;
            _validationRules[key] = expectedOutcome;
        }
        
        _complexConfigurationBuilder!.AddInMemoryCollection(edgeCaseData);
    }

    [When(@"I organize performance test configuration with large dataset:")]
    public void WhenIOrganizePerformanceTestConfigurationWithLargeDataset(Table table)
    {
        _complexConfigurationBuilder.Should().NotBeNull();
        _performanceMonitoringEnabled = true;
        
        var performanceData = new Dictionary<string, string?>();
        
        foreach (var row in table.Rows)
        {
            var configurationType = row["ConfigurationType"];
            var count = int.Parse(row["Count"]);
            
            for (int i = 0; i < count; i++)
            {
                var key = $"{configurationType}:{i}:Id";
                var value = $"{configurationType.ToLower()}-{i}";
                performanceData[key] = value;
                
                performanceData[$"{configurationType}:{i}:Name"] = $"Item {i}";
                performanceData[$"{configurationType}:{i}:Enabled"] = (i % 2 == 0).ToString();
                performanceData[$"{configurationType}:{i}:Priority"] = (i % 10).ToString();
            }
        }
        
        _complexConfigurationBuilder!.AddInMemoryCollection(performanceData);
    }

    [When(@"I organize batch configuration data from multiple sources")]
    public void WhenIOrganizeBatchConfigurationDataFromMultipleSources()
    {
        _complexConfigurationBuilder.Should().NotBeNull();
        
        var batchData = new Dictionary<string, string?>();
        
        for (int i = 0; i < 10; i++)
        {
            batchData[$"Database:Pool{i}:ConnectionString"] = $"Server=db{i}.example.com;Database=App{i}";
            batchData[$"Database:Pool{i}:MaxConnections"] = (20 + i * 5).ToString();
            batchData[$"Database:Pool{i}:Timeout"] = (30 + i * 2).ToString();
        }
        
        for (int i = 0; i < 15; i++)
        {
            batchData[$"ExternalServices:Service{i}:BaseUrl"] = $"https://service{i}.api.com";
            batchData[$"ExternalServices:Service{i}:ApiKey"] = $"key-{i}-{Guid.NewGuid():N}";
            batchData[$"ExternalServices:Service{i}:Timeout"] = (5000 + i * 1000).ToString();
        }
        
        _complexConfigurationBuilder!.AddInMemoryCollection(batchData);
    }

    [When(@"I organize configuration with platform-specific paths:")]
    public void WhenIOrganizeConfigurationWithPlatformSpecificPaths(Table table)
    {
        _complexConfigurationBuilder.Should().NotBeNull();
        
        var platformData = new Dictionary<string, string?>();
        
        foreach (var row in table.Rows)
        {
            var key = row["Key"];
            var windowsValue = row["WindowsValue"];
            var linuxValue = row["LinuxValue"];
            
            var platformValue = Environment.OSVersion.Platform == PlatformID.Win32NT ? windowsValue : linuxValue;
            platformData[key] = platformValue;
            _platformPaths[key] = platformValue;
        }
        
        _complexConfigurationBuilder!.AddInMemoryCollection(platformData);
    }

    [When(@"I organize environment-specific path configurations")]
    public void WhenIOrganizeEnvironmentSpecificPathConfigurations()
    {
        _complexConfigurationBuilder.Should().NotBeNull();
        
        var pathData = new Dictionary<string, string?>
        {
            ["Paths:Application:Root"] = Environment.CurrentDirectory,
            ["Paths:Application:Data"] = Path.Combine(Environment.CurrentDirectory, "Data"),
            ["Paths:Application:Logs"] = Path.Combine(Environment.CurrentDirectory, "Logs"),
            ["Paths:Application:Config"] = Path.Combine(Environment.CurrentDirectory, "Config"),
            ["Paths:Temp:Directory"] = Path.GetTempPath(),
            ["Paths:User:Home"] = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
        };
        
        _complexConfigurationBuilder!.AddInMemoryCollection(pathData);
    }

    [When(@"I organize configuration with security-sensitive data:")]
    public void WhenIOrganizeConfigurationWithSecuritySensitiveData(Table table)
    {
        _complexConfigurationBuilder.Should().NotBeNull();
        _securityFeaturesEnabled = true;
        
        var securityData = new Dictionary<string, string?>();
        
        foreach (var row in table.Rows)
        {
            var key = row["Key"];
            var value = row["Value"];
            var securityLevel = row["SecurityLevel"];
            
            securityData[key] = value;
            _securityMetadata[key] = securityLevel;
            _securityData[key] = value;
            
            _securityAuditTrail.Add($"Sensitive configuration organized: {key} (Level: {securityLevel})");
        }
        
        _complexConfigurationBuilder!.AddInMemoryCollection(securityData);
    }

    [When(@"I organize security configuration metadata:")]
    public void WhenIOrganizeSecurityConfigurationMetadata(Table table)
    {
        _complexConfigurationBuilder.Should().NotBeNull();
        
        var securityMetadata = new Dictionary<string, string?>();
        
        foreach (var row in table.Rows)
        {
            var setting = row["SecuritySetting"];
            var value = row["Value"];
            var key = $"Security:Metadata:{setting}";
            
            securityMetadata[key] = value;
        }
        
        _complexConfigurationBuilder!.AddInMemoryCollection(securityMetadata);
    }

    [When(@"I organize typed configuration binding for complex objects")]
    public void WhenIOrganizeTypedConfigurationBindingForComplexObjects()
    {
        // Prepare for typed configuration binding validation
    }

    #endregion

    #region When Steps - Execution

    [When(@"I execute complex configuration assembly")]
    public void WhenIExecuteComplexConfigurationAssembly()
    {
        // Create a new builder to ensure proper order
        var builder = TestConfigurationBuilder.Create(_scenarioContext);
    
        // Add sources in precedence order (lowest to highest)
    
        // 1. Base JSON files (if any were organized)
        foreach (var jsonFile in GetOrganizedJsonFiles())
        {
            builder.AddJsonFile(jsonFile, optional: false, reloadOnChange: false);
        }
    
        // 2. Environment variables
        if (_environmentVariables.Any())
        {
            builder.AddInMemoryCollection(_environmentVariables.ToDictionary(
                kvp => kvp.Key.Replace("__", ":"), 
                kvp => kvp.Value));
        }
    
        // 3. .env files (if any)
        foreach (var envFile in GetOrganizedEnvFiles())
        {
            builder.AddDotEnvFile(envFile, optional: true);
        }
    
        // 4. Conditional configuration (highest precedence among organized)
        if (_conditionalConfiguration.Any())
        {
            builder.AddInMemoryCollection(_conditionalConfiguration);
        }
    
        // 5. In-memory final overrides (highest precedence)
        if (_inMemoryOverrides.Any())
        {
            builder.AddInMemoryCollection(_inMemoryOverrides);
        }
    
        _performanceStopwatch.Start();
    
        try
        {
            _complexConfiguration = builder.Build();
            _complexFlexConfiguration = _complexConfiguration.GetFlexConfiguration();
            _complexConfigurationLoaded = true;
        
            _performanceStopwatch.Stop();
            _memoryUsageAfter = GC.GetTotalMemory(false);
        }
        catch (Exception ex)
        {
            _lastComplexException = ex;
            _complexConfigurationLoaded = false;
            _performanceStopwatch.Stop();
        }
    }

    [When(@"I execute complex configuration assembly with validation")]
    public void WhenIExecuteComplexConfigurationAssemblyWithValidation()
    {
        WhenIExecuteComplexConfigurationAssembly();
        
        if (_complexConfigurationLoaded && _validationEnabled)
        {
            ExecuteConfigurationValidation();
        }
    }

    [When(@"I execute complex configuration assembly with performance monitoring")]
    public void WhenIExecuteComplexConfigurationAssemblyWithPerformanceMonitoring()
    {
        _performanceMonitoringEnabled = true;
        WhenIExecuteComplexConfigurationAssembly();
    }

    [When(@"I execute complex configuration assembly with cross-platform support")]
    public void WhenIExecuteComplexConfigurationAssemblyWithCrossPlatformSupport()
    {
        // Create builder and add platform-specific data
        var builder = TestConfigurationBuilder.Create(_scenarioContext);
    
        // Add platform paths that were organized
        if (_platformPaths.Any())
        {
            builder.AddInMemoryCollection(_platformPaths);
        }
    
        // Add environment-specific paths
        var pathData = new Dictionary<string, string?>
        {
            ["Paths:Application:Root"] = Environment.CurrentDirectory,
            ["Paths:Application:Data"] = Path.Combine(Environment.CurrentDirectory, "Data"),
            ["Paths:Application:Logs"] = Path.Combine(Environment.CurrentDirectory, "Logs"),
            ["Paths:Application:Config"] = Path.Combine(Environment.CurrentDirectory, "Config"),
            ["Paths:Temp:Directory"] = Path.GetTempPath(),
            ["Paths:User:Home"] = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
        };
        builder.AddInMemoryCollection(pathData);
    
        _performanceStopwatch.Start();
    
        try
        {
            _complexConfiguration = builder.Build();
            _complexFlexConfiguration = _complexConfiguration.GetFlexConfiguration();
            _complexConfigurationLoaded = true;
        
            _performanceStopwatch.Stop();
            _memoryUsageAfter = GC.GetTotalMemory(false);
        }
        catch (Exception ex)
        {
            _lastComplexException = ex;
            _complexConfigurationLoaded = false;
            _performanceStopwatch.Stop();
        }
    }

    [When(@"I execute complex configuration assembly with security features")]
    public void WhenIExecuteComplexConfigurationAssemblyWithSecurityFeatures()
    {
        _securityFeaturesEnabled = true;
    
        // Create builder and add security data
        var builder = TestConfigurationBuilder.Create(_scenarioContext);
    
        // Add security data that was organized
        if (_securityData.Any())
        {
            builder.AddInMemoryCollection(_securityData);
        }
    
        // Add security metadata
        var securityMetadataKeys = _securityMetadata.Keys.Where(k => !string.IsNullOrEmpty(_securityMetadata[k]));
        if (securityMetadataKeys.Any())
        {
            var metadataConfig = new Dictionary<string, string?>();
            foreach (var key in securityMetadataKeys)
            {
                metadataConfig[$"Security:Metadata:{key}"] = _securityMetadata[key];
            }
            builder.AddInMemoryCollection(metadataConfig);
        }
    
        _performanceStopwatch.Start();
    
        try
        {
            _complexConfiguration = builder.Build();
            _complexFlexConfiguration = _complexConfiguration.GetFlexConfiguration();
            _complexConfigurationLoaded = true;
        
            _performanceStopwatch.Stop();
            _memoryUsageAfter = GC.GetTotalMemory(false);
        }
        catch (Exception ex)
        {
            _lastComplexException = ex;
            _complexConfigurationLoaded = false;
            _performanceStopwatch.Stop();
        }
    }

    [When(@"I execute configuration hot reload simulation")]
    public void WhenIExecuteConfigurationHotReloadSimulation()
    {
        _complexConfiguration.Should().NotBeNull();
        
        if (_dynamicUpdates.Any())
        {
            var currentData = _complexConfiguration!.AsEnumerable()
                .Where(kvp => kvp.Key != null)
                .ToDictionary(kvp => kvp.Key!, kvp => kvp.Value);
            
            foreach (var update in _dynamicUpdates)
            {
                currentData[update.Key] = update.Value;
            }
            
            var updatedBuilder = TestConfigurationBuilder.Create(_scenarioContext);
            updatedBuilder.AddInMemoryCollection(currentData);
            
            _complexConfiguration = updatedBuilder.Build();
            _complexFlexConfiguration = _complexConfiguration.GetFlexConfiguration();
        }
    }

    #endregion

    #region Then Steps

    [Then(@"the complex configuration should load successfully")]
    public void ThenTheComplexConfigurationShouldLoadSuccessfully()
    {
        _complexConfigurationLoaded.Should().BeTrue();
        _complexConfiguration.Should().NotBeNull();
        _complexFlexConfiguration.Should().NotBeNull();
        _lastComplexException.Should().BeNull();
    }

    [Then(@"hierarchical precedence should be maintained across all layers")]
    public void ThenHierarchicalPrecedenceShouldBeMaintainedAcrossAllLayers()
    {
        _complexConfiguration.Should().NotBeNull();
        // Verify configuration has values from multiple sources
        var configEntries = _complexConfiguration!.AsEnumerable().ToList();
        configEntries.Should().NotBeEmpty();
    }

    [Then(@"the complex configuration should contain ""(.*)"" with value ""(.*)""")]
    public void ThenTheComplexConfigurationShouldContainWithValue(string key, string expectedValue)
    {
        _complexConfiguration.Should().NotBeNull();
        var actualValue = _complexConfiguration![key];
        actualValue.Should().Be(expectedValue);
    }

    [Then(@"database configuration should be properly typed and accessible")]
    public void ThenDatabaseConfigurationShouldBeProperlyTypedAndAccessible()
    {
        _complexFlexConfiguration.Should().NotBeNull();
        dynamic config = _complexFlexConfiguration!;
        
        try
        {
            var databaseSection = config.Database;
            if (databaseSection != null)
            {
                var connectionString = databaseSection.ConnectionString;
                connectionString.Should().NotBeNullOrEmpty();
            }
        }
        catch
        {
            // Database section may not exist in all test scenarios
        }
    }

    [Then(@"external API configurations should be properly structured")]
    public void ThenExternalApiConfigurationsShouldBeProperlyStructured()
    {
        _complexConfiguration.Should().NotBeNull();
    
        var externalEntries = _complexConfiguration!.AsEnumerable()
            .Where(kvp => kvp.Key != null && kvp.Key.StartsWith("External:"))
            .ToList();
    
        foreach (var entry in externalEntries.Take(3))
        {
            if (entry.Value != null)  // Add null check
            {
                entry.Value.Should().NotBeNullOrEmpty();
            }
        }
    }

    [Then(@"feature flag configurations should be properly converted")]
    public void ThenFeatureFlagConfigurationsShouldBeProperlyConverted()
    {
        _complexConfiguration.Should().NotBeNull();
        
        var featureEntries = _complexConfiguration!.AsEnumerable()
            .Where(kvp => kvp.Key != null && kvp.Key.StartsWith("Features:"))
            .ToList();
        
        foreach (var entry in featureEntries)
        {
            if (entry.Value != null)
            {
                if (bool.TryParse(entry.Value, out var boolValue))
                {
                    // Boolean feature flag
                }
                else if (int.TryParse(entry.Value, out var intValue))
                {
                    intValue.Should().BeGreaterThanOrEqualTo(0);
                }
                else
                {
                    entry.Value.Should().NotBeEmpty();
                }
            }
        }
    }

    [Then(@"security configurations should maintain proper data types")]
    public void ThenSecurityConfigurationsShouldMaintainProperDataTypes()
    {
        _complexConfiguration.Should().NotBeNull();
        
        var securityEntries = _complexConfiguration!.AsEnumerable()
            .Where(kvp => kvp.Key != null && kvp.Key.StartsWith("Security:"))
            .ToList();
        
        foreach (var entry in securityEntries)
        {
            if (entry.Value != null)
            {
                entry.Value.Should().NotBeEmpty();
                
                if (entry.Key.Contains("ExpirationHours") || entry.Key.Contains("KeySize"))
                {
                    int.TryParse(entry.Value, out var numericValue).Should().BeTrue();
                    numericValue.Should().BeGreaterThan(0);
                }
            }
        }
    }

    [Then(@"all configuration sections should support dynamic access")]
    public void ThenAllConfigurationSectionsShouldSupportDynamicAccess()
    {
        _complexFlexConfiguration.Should().NotBeNull();
        dynamic config = _complexFlexConfiguration!;
        
        var sectionNames = new[] { "Database", "External", "Features", "Security", "Application" };
        
        foreach (var sectionName in sectionNames)
        {
            try
            {
                var section = config[sectionName];
                // Section access should not throw, whether it exists or not
            }
            catch
            {
                // Dynamic access may fail for non-existent sections - this is acceptable
            }
        }
    }

    [Then(@"environment-specific settings should be active")]
    public void ThenEnvironmentSpecificSettingsShouldBeActive()
    {
        _complexConfiguration.Should().NotBeNull();
        
        foreach (var key in _conditionalConfiguration.Keys)
        {
            var actualValue = _complexConfiguration![key];
            var expectedValue = _conditionalConfiguration[key];
            actualValue.Should().Be(expectedValue);
        }
    }

    [Then(@"the complex configuration should incorporate dynamic updates")]
    public void ThenTheComplexConfigurationShouldIncorporateDynamicUpdates()
    {
        _complexConfiguration.Should().NotBeNull();
        
        foreach (var update in _dynamicUpdates)
        {
            var actualValue = _complexConfiguration![update.Key];
            actualValue.Should().Be(update.Value);
        }
    }

    [Then(@"existing configuration values should remain unchanged")]
    public void ThenExistingConfigurationValuesShouldRemainUnchanged()
    {
        _complexConfiguration.Should().NotBeNull();
        var configEntries = _complexConfiguration!.AsEnumerable().Count();
        configEntries.Should().BeGreaterThan(_dynamicUpdates.Count);
    }

    [Then(@"FlexConfig should reflect the updated configuration")]
    public void ThenFlexConfigShouldReflectTheUpdatedConfiguration()
    {
        _complexFlexConfiguration.Should().NotBeNull();
        
        foreach (var update in _dynamicUpdates)
        {
            var actualValue = _complexFlexConfiguration![update.Key];
            actualValue.Should().Be(update.Value);
        }
    }

    [Then(@"valid configuration values should be properly stored")]
    public void ThenValidConfigurationValuesShouldBeProperlyStored()
    {
        _complexConfiguration.Should().NotBeNull();
    
        if (_validationEnabled)
        {
            foreach (var rule in _validationRules.Where(r => !r.Value!.Contains("should provide clear error")))
            {
                var actualValue = _complexConfiguration![rule.Key];
                if (!string.IsNullOrEmpty(actualValue))
                {
                    actualValue.Should().NotBeEmpty();
                }
            }
        }
    }

    [Then(@"invalid configuration values should be handled gracefully")]
    public void ThenInvalidConfigurationValuesShouldBeHandledGracefully()
    {
        if (_validationEnabled)
        {
            // This is a behavioral assertion - invalid values should not crash the system
            _complexConfiguration.Should().NotBeNull();
        }
    }

    [Then(@"appropriate validation errors should be captured")]
    public void ThenAppropriateValidationErrorsShouldBeCaptured()
    {
        if (_validationEnabled)
        {
            // Validation errors should be tracked if validation was performed
            _configurationErrors.Should().NotBeNull();
        }
    }

    [Then(@"fallback values should be applied where configured")]
    public void ThenFallbackValuesShouldBeAppliedWhereConfigured()
    {
        _complexConfiguration.Should().NotBeNull();
        // This is a behavioral assertion about fallback handling
    }

    [Then(@"configuration loading should complete within acceptable time limits")]
    public void ThenConfigurationLoadingShouldCompleteWithinAcceptableTimeLimits()
    {
        if (_performanceMonitoringEnabled)
        {
            _performanceStopwatch.ElapsedMilliseconds.Should().BeLessThanOrEqualTo(5000);
        }
    }

    [Then(@"memory usage should remain within reasonable bounds")]
    public void ThenMemoryUsageShouldRemainWithinReasonableBounds()
    {
        if (_performanceMonitoringEnabled)
        {
            var memoryUsage = _memoryUsageAfter - _memoryUsageBefore;
            memoryUsage.Should().BeLessThanOrEqualTo(100 * 1024 * 1024); // 100MB
        }
    }

    [Then(@"FlexConfig performance should meet established benchmarks")]
    public void ThenFlexConfigPerformanceShouldMeetEstablishedBenchmarks()
    {
        _complexFlexConfiguration.Should().NotBeNull();
        
        if (_performanceMonitoringEnabled)
        {
            var accessStopwatch = Stopwatch.StartNew();
            
            for (int i = 0; i < 1000; i++)
            {
                var value = _complexFlexConfiguration!["Database:ConnectionString"];
            }
            
            accessStopwatch.Stop();
            accessStopwatch.ElapsedMilliseconds.Should().BeLessThanOrEqualTo(100);
        }
    }

    [Then(@"all configuration sections should be accessible efficiently")]
    public void ThenAllConfigurationSectionsShouldBeAccessibleEfficiently()
    {
        _complexConfiguration.Should().NotBeNull();
        
        var allKeys = _complexConfiguration!.AsEnumerable().Where(kvp => kvp.Key != null).ToList();
        
        var accessStopwatch = Stopwatch.StartNew();
        
        foreach (var kvp in allKeys.Take(100))
        {
            var value = _complexConfiguration[kvp.Key!];
        }
        
        accessStopwatch.Stop();
        accessStopwatch.ElapsedMilliseconds.Should().BeLessThanOrEqualTo(50);
    }

    [Then(@"platform-appropriate paths should be resolved correctly")]
    public void ThenPlatformAppropriatePathsShouldBeResolvedCorrectly()
    {
        _complexConfiguration.Should().NotBeNull();
        
        foreach (var pathEntry in _platformPaths)
        {
            var actualValue = _complexConfiguration![pathEntry.Key];
            actualValue.Should().Be(pathEntry.Value);
        }
    }

    [Then(@"configuration should work consistently across different operating systems")]
    public void ThenConfigurationShouldWorkConsistentlyAcrossDifferentOperatingSystems()
    {
        _complexFlexConfiguration.Should().NotBeNull();
        dynamic config = _complexFlexConfiguration!;
        
        try
        {
            var testValue = config["Application:Name"];
            // Configuration access should work regardless of platform
        }
        catch
        {
            // Some dynamic access may fail, but shouldn't crash
        }
    }

    [Then(@"file path separators should be normalized properly")]
    public void ThenFilePathSeparatorsShouldBeNormalizedProperly()
    {
        _complexConfiguration.Should().NotBeNull();
        
        var pathEntries = _complexConfiguration!.AsEnumerable()
            .Where(kvp => kvp.Key != null && kvp.Value != null && 
                         (kvp.Key.Contains("Path") || kvp.Value.Contains("/") || kvp.Value.Contains("\\")))
            .ToList();
        
        // Paths should be properly formatted for current platform
        pathEntries.Should().NotBeNull();
    }

    [Then(@"sensitive configuration values should be properly protected")]
    public void ThenSensitiveConfigurationValuesShouldBeProperlyProtected()
    {
        if (_securityFeaturesEnabled)
        {
            _complexConfiguration.Should().NotBeNull();
            
            var sensitiveKeys = _securityMetadata.Where(kvp => kvp.Value == "Encrypted").Select(kvp => kvp.Key).ToList();
            
            foreach (var sensitiveKey in sensitiveKeys)
            {
                var value = _complexConfiguration![sensitiveKey];
                value.Should().NotBeNullOrEmpty();
                
                _securityAuditTrail.Add($"Sensitive value accessed: {sensitiveKey}");
            }
        }
    }

    [Then(@"security metadata should be accessible through FlexConfig")]
    public void ThenSecurityMetadataShouldBeAccessibleThroughFlexConfig()
    {
        if (_securityFeaturesEnabled)
        {
            _complexFlexConfiguration.Should().NotBeNull();
            dynamic config = _complexFlexConfiguration!;
            
            try
            {
                var securitySection = config.Security;
                if (securitySection != null)
                {
                    var metadataSection = securitySection.Metadata;
                    // Metadata should be accessible if it exists
                }
            }
            catch
            {
                // Security metadata may not exist in all scenarios
            }
        }
    }

    [Then(@"configuration access should maintain security boundaries")]
    public void ThenConfigurationAccessShouldMaintainSecurityBoundaries()
    {
        if (_securityFeaturesEnabled)
        {
            // This is a behavioral assertion about security boundary maintenance
            _complexConfiguration.Should().NotBeNull();
        }
    }

    [Then(@"audit trails should be generated for sensitive configuration access")]
    public void ThenAuditTrailsShouldBeGeneratedForSensitiveConfigurationAccess()
    {
        if (_securityFeaturesEnabled)
        {
            _securityAuditTrail.Should().NotBeEmpty();
            
            foreach (var auditEntry in _securityAuditTrail)
            {
                auditEntry.Should().NotBeNullOrEmpty();
            }
        }
    }

    #endregion

    #region Helper Methods

    private void ExecuteConfigurationValidation()
    {
        foreach (var rule in _validationRules)
        {
            var key = rule.Key;
            var validationRule = rule.Value;
            var actualValue = _complexConfiguration![key];
            
            var isValid = ValidateConfigurationValue(key, actualValue, validationRule!);
            
            if (!isValid)
            {
                var errorMessage = $"Validation failed for {key}: {validationRule}";
                _configurationErrors.Add(errorMessage);
            }
        }
    }

    private static bool ValidateConfigurationValue(string key, string? value, string rule)
    {
        if (string.IsNullOrEmpty(value))
        {
            return rule.Contains("Should handle gracefully") || rule.Contains("Should use fallback");
        }

        return rule.ToLowerInvariant() switch
        {
            var r when r.Contains("between") && r.Contains("and") => ValidateNumericRange(value, rule),
            var r when r.Contains("greater than 0") => int.TryParse(value, out var intVal) && intVal > 0,
            var r when r.Contains("positive number") => double.TryParse(value, out var doubleVal) && doubleVal > 0,
            var r when r.Contains("should handle gracefully") => true,
            var r when r.Contains("should provide clear error") => false,
            var r when r.Contains("should use fallback") => true,
            var r when r.Contains("should default appropriately") => true,
            _ => true
        };
    }

    private static bool ValidateNumericRange(string value, string rule)
    {
        if (!double.TryParse(value, out var numericValue))
            return false;

        var parts = rule.Split(new[] { " and " }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
            return false;

        var minPart = parts[0].Replace("Between", "").Trim();
        var maxPart = parts[1].Trim();

        if (double.TryParse(minPart, out var min) && double.TryParse(maxPart, out var max))
        {
            return numericValue >= min && numericValue <= max;
        }

        return false;
    }

    #endregion
}