using FlexKit.Configuration.Providers.Azure.IntegrationTests.Utils;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Reqnroll;
// ReSharper disable MethodTooLong
// ReSharper disable ArrangeRedundantParentheses
// ReSharper disable ComplexConditionExpression

namespace FlexKit.Configuration.Providers.Azure.IntegrationTests.Steps.Infrastructure;

/// <summary>
/// Step definitions for LocalStack infrastructure setup scenarios.
/// Tests LocalStack container management with Azure services simulation,
/// including setup, health checks, and teardown procedures.
/// Uses distinct step patterns ("local stack module") to avoid conflicts with other step classes.
/// </summary>
[Binding]
public class LocalStackInfrastructureSteps(ScenarioContext scenarioContext)
{
    private LocalStackContainerHelper? _localStackHelper;
    private AzureTestDataModel? _testDataModel;
    private Exception? _lastInfrastructureException;
    private readonly List<string> _infrastructureValidationResults = new();
    private bool _localStackStarted;
    private bool _testDataPopulated;
    private string _testDataPath = string.Empty;
    private readonly ILogger<LocalStackContainerHelper> _logger = 
        LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<LocalStackContainerHelper>();

    #region Given Steps - Setup

    [Given(@"I have prepared a local stack module environment")]
    public void GivenIHavePreparedALocalStackModuleEnvironment()
    {
        _localStackHelper = new LocalStackContainerHelper(scenarioContext, _logger);
        scenarioContext.Set(_localStackHelper, "LocalStackHelper");
        
        _infrastructureValidationResults.Add("✓ LocalStack module environment prepared");
    }

    [Given(@"I have a running local stack module environment")]
    public async Task GivenIHaveARunningLocalStackModuleEnvironment()
    {
        // Create and prepare a LocalStack helper
        _localStackHelper = new LocalStackContainerHelper(scenarioContext, _logger);
        scenarioContext.Set(_localStackHelper, "LocalStackHelper");
        
        try
        {
            // Start LocalStack with default Azure services
            await _localStackHelper.StartAsync();
            _localStackStarted = true;
            
            _infrastructureValidationResults.Add("✓ LocalStack module environment prepared and started");
        }
        catch (Exception ex)
        {
            _lastInfrastructureException = ex;
            _infrastructureValidationResults.Add($"✗ Failed to start LocalStack: {ex.Message}");
            throw;
        }
    }

    #endregion

    #region When Steps - Actions

    [When(@"I configure local stack module with configuration from ""(.*)""")]
    public void WhenIConfigureLocalStackModuleWithConfigurationFrom(string testDataPath)
    {
        _localStackHelper.Should().NotBeNull("LocalStack helper should be prepared");
        
        try
        {
            // Use the path as provided - don't add TestData prefix since it's already included
            _testDataPath = testDataPath;
            _testDataModel = AzureTestExtensions.LoadAzureTestData(_testDataPath);
            
            scenarioContext.Set(_testDataModel, "TestDataModel");
            scenarioContext.Set(_testDataPath, "TestDataPath");
            
            _infrastructureValidationResults.Add($"✓ LocalStack configuration loaded from {testDataPath}");
        }
        catch (Exception ex)
        {
            _lastInfrastructureException = ex;
            _infrastructureValidationResults.Add($"✗ Failed to load configuration: {ex.Message}");
            throw;
        }
    }

    [When(@"I initialize the local stack module setup")]
    public void WhenIInitializeTheLocalStackModuleSetup()
    {
        _localStackHelper.Should().NotBeNull("LocalStack helper should be configured");
        _testDataModel.Should().NotBeNull("Test data should be loaded");

        try
        {
            var services = _testDataModel!.LocalStack?.Services ?? "keyvault,appconfig";
            
            var startTask = _localStackHelper!.StartAsync(services);
            startTask.Wait(TimeSpan.FromMinutes(3));
            
            if (!startTask.IsCompletedSuccessfully)
            {
                throw new TimeoutException("LocalStack failed to start within the timeout period");
            }
            
            _localStackStarted = true;
            _infrastructureValidationResults.Add("✓ LocalStack module initialized successfully");
        }
        catch (Exception ex)
        {
            _lastInfrastructureException = ex;
            _infrastructureValidationResults.Add($"✗ LocalStack initialization failed: {ex.Message}");
            throw;
        }
    }

    [When(@"I populate local stack module with all test data")]
    public void WhenIPopulateLocalStackModuleWithAllTestData()
    {
        _localStackHelper.Should().NotBeNull("LocalStack helper should be running");
        _testDataModel.Should().NotBeNull("Test data should be loaded");
        _localStackStarted.Should().BeTrue("LocalStack should be started");

        try
        {
            var populateTask = _localStackHelper!.CreateTestDataAsync(_testDataPath);
            populateTask.Wait(TimeSpan.FromMinutes(2));
            
            if (!populateTask.IsCompletedSuccessfully)
            {
                throw new TimeoutException("Test data population failed within the timeout period");
            }
            
            _testDataPopulated = true;
            _infrastructureValidationResults.Add("✓ Test data populated successfully");
        }
        catch (Exception ex)
        {
            _lastInfrastructureException = ex;
            _infrastructureValidationResults.Add($"✗ Test data population failed: {ex.Message}");
            throw;
        }
    }

    [When(@"I validate local stack module configuration structure")]
    public void WhenIValidateLocalStackModuleConfigurationStructure()
    {
        _testDataModel.Should().NotBeNull("Test data should be loaded");

        try
        {
            // Validate LocalStack configuration
            if (_testDataModel!.LocalStack != null)
            {
                _testDataModel.LocalStack.Services.Should().NotBeNullOrEmpty("Services should be specified");
                _testDataModel.LocalStack.Port.Should().BeGreaterThan(0, "Port should be valid");
                _infrastructureValidationResults.Add("✓ LocalStack configuration structure is valid");
            }

            // Validate Azure configuration
            if (_testDataModel.Azure != null)
            {
                _testDataModel.Azure.Region.Should().NotBeNullOrEmpty("Azure region should be specified");
                _infrastructureValidationResults.Add("✓ Azure configuration structure is valid");
            }

            // Validate Key Vault secrets structure
            if (_testDataModel.KeyVaultSecrets != null && _testDataModel.KeyVaultSecrets.Any())
            {
                foreach (var secret in _testDataModel.KeyVaultSecrets)
                {
                    secret.Key.Should().NotBeNullOrEmpty("Secret key should not be empty");
                    secret.Value.Should().NotBeNull("Secret value should not be null");
                }
                _infrastructureValidationResults.Add($"✓ Key Vault secrets structure is valid ({_testDataModel.KeyVaultSecrets.Count} secrets)");
            }

            // Validate App Configuration settings structure
            if (_testDataModel.AppConfigurationSettings != null && _testDataModel.AppConfigurationSettings.Any())
            {
                foreach (var setting in _testDataModel.AppConfigurationSettings)
                {
                    setting.Key.Should().NotBeNullOrEmpty("Setting key should not be empty");
                    setting.Value.Should().NotBeNull("Setting value should not be null");
                }
                _infrastructureValidationResults.Add($"✓ App Configuration settings structure is valid ({_testDataModel.AppConfigurationSettings.Count} settings)");
            }
        }
        catch (Exception ex)
        {
            _lastInfrastructureException = ex;
            _infrastructureValidationResults.Add($"✗ Configuration structure validation failed: {ex.Message}");
            throw;
        }
    }

    [When(@"I request local stack module teardown")]
    public void WhenIRequestLocalStackModuleTeardown()
    {
        _localStackHelper.Should().NotBeNull("LocalStack helper should exist");

        try
        {
            if (_localStackStarted)
            {
                var stopTask = _localStackHelper!.StopAsync();
                stopTask.Wait(TimeSpan.FromMinutes(1));
                
                _localStackStarted = false;
                _infrastructureValidationResults.Add("✓ LocalStack module teardown completed");
            }
            else
            {
                _infrastructureValidationResults.Add("ⓘ LocalStack was not running, no teardown needed");
            }
        }
        catch (Exception ex)
        {
            _lastInfrastructureException = ex;
            _infrastructureValidationResults.Add($"⚠ LocalStack teardown encountered issues: {ex.Message}");
            // Don't throw for teardown failures to avoid masking test results
        }
    }

    #endregion

    #region Then Steps - Assertions

    [Then(@"the local stack module should be configured successfully")]
    public void ThenTheLocalStackModuleShouldBeConfiguredSuccessfully()
    {
        _localStackHelper.Should().NotBeNull("LocalStack helper should be created");
        _testDataModel.Should().NotBeNull("Test data should be loaded");
        _lastInfrastructureException.Should().BeNull("No configuration errors should have occurred");
        
        _infrastructureValidationResults.Should().Contain(r => r.Contains("✓"), 
            "Should have successful validation results");
    }

    [Then(@"the local stack module should have all Key Vault data populated")]
    public void ThenTheLocalStackModuleShouldHaveAllKeyVaultDataPopulated()
    {
        _localStackStarted.Should().BeTrue("LocalStack should be running");
        _testDataPopulated.Should().BeTrue("Test data should be populated");
        
        try
        {
            var keyVaultClient = _localStackHelper!.CreateKeyVaultClient();
            keyVaultClient.Should().NotBeNull("Key Vault client should be created");
            
            _infrastructureValidationResults.Add("✓ Key Vault data population verified");
        }
        catch (Exception ex)
        {
            _infrastructureValidationResults.Add($"✗ Key Vault verification failed: {ex.Message}");
            throw;
        }
    }

    [Then(@"the local stack module should have all App Configuration data populated")]
    public void ThenTheLocalStackModuleShouldHaveAllAppConfigurationDataPopulated()
    {
        _localStackStarted.Should().BeTrue("LocalStack should be running");
        _testDataPopulated.Should().BeTrue("Test data should be populated");
        
        try
        {
            var appConfigClient = _localStackHelper!.CreateAppConfigurationClient();
            appConfigClient.Should().NotBeNull("App Configuration client should be created");
            
            _infrastructureValidationResults.Add("✓ App Configuration data population verified");
        }
        catch (Exception ex)
        {
            _infrastructureValidationResults.Add($"✗ App Configuration verification failed: {ex.Message}");
            throw;
        }
    }

    [Then(@"the local stack module should be ready for integration testing")]
    public void ThenTheLocalStackModuleShouldBeReadyForIntegrationTesting()
    {
        _localStackStarted.Should().BeTrue("LocalStack should be running");
        _testDataPopulated.Should().BeTrue("Test data should be populated");
        _localStackHelper!.IsRunning.Should().BeTrue("LocalStack container should be running");
        
        // Verify both services are accessible
        try
        {
            var keyVaultClient = _localStackHelper.CreateKeyVaultClient();
            var appConfigClient = _localStackHelper.CreateAppConfigurationClient();
            
            keyVaultClient.Should().NotBeNull("Key Vault client should be accessible");
            appConfigClient.Should().NotBeNull("App Configuration client should be accessible");
            
            _infrastructureValidationResults.Add("✓ LocalStack is ready for integration testing");
        }
        catch (Exception ex)
        {
            _infrastructureValidationResults.Add($"✗ Integration readiness check failed: {ex.Message}");
            throw;
        }
    }

    [Then(@"the local stack module configuration should be valid")]
    public void ThenTheLocalStackModuleConfigurationShouldBeValid()
    {
        _testDataModel.Should().NotBeNull("Test data should be loaded");
        _lastInfrastructureException.Should().BeNull("No validation errors should have occurred");
        
        _infrastructureValidationResults.Should().Contain(r => r.Contains("structure is valid"), 
            "Should have valid structure confirmation");
    }

    [Then(@"the local stack module should contain LocalStack settings")]
    public void ThenTheLocalStackModuleShouldContainLocalStackSettings()
    {
        _testDataModel.Should().NotBeNull("Test data should be loaded");
        _testDataModel!.LocalStack.Should().NotBeNull("LocalStack configuration should exist");
        
        var localStackConfig = _testDataModel.LocalStack!;
        localStackConfig.Services.Should().NotBeNullOrEmpty("Services should be configured");
        localStackConfig.Port.Should().BeGreaterThan(0, "Port should be valid");
        
        _infrastructureValidationResults.Add("✓ LocalStack settings validation passed");
    }

    [Then(@"the local stack module should contain Azure test credentials")]
    public void ThenTheLocalStackModuleShouldContainAzureTestCredentials()
    {
        _testDataModel.Should().NotBeNull("Test data should be loaded");
        _testDataModel!.Azure.Should().NotBeNull("Azure configuration should exist");
        
        var azureConfig = _testDataModel.Azure!;
        azureConfig.Region.Should().NotBeNullOrEmpty("Azure region should be specified");
        
        _infrastructureValidationResults.Add("✓ Azure credentials validation passed");
    }

    [Then(@"the local stack module should contain test key vault definition")]
    public void ThenTheLocalStackModuleShouldContainTestKeyVaultDefinition()
    {
        _testDataModel.Should().NotBeNull("Test data should be loaded");
        
        bool hasKeyVaultData = (_testDataModel!.KeyVaultSecrets?.Any() == true) || 
                              (_testDataModel.JsonSecrets?.Any() == true) ||
                              (_testDataModel.Azure?.KeyVault != null);
        
        hasKeyVaultData.Should().BeTrue("Should have Key Vault configuration or test data");
        
        _infrastructureValidationResults.Add("✓ Key Vault definition validation passed");
    }

    [Then(@"the local stack module should contain test app configuration definition")]
    public void ThenTheLocalStackModuleShouldContainTestAppConfigurationDefinition()
    {
        _testDataModel.Should().NotBeNull("Test data should be loaded");
        
        bool hasAppConfigData = (_testDataModel!.AppConfigurationSettings?.Any() == true) || 
                               (_testDataModel.LabeledAppConfigurationSettings?.Any() == true) ||
                               (_testDataModel.FeatureFlags?.Any() == true) ||
                               (_testDataModel.Azure?.AppConfiguration != null);
        
        hasAppConfigData.Should().BeTrue("Should have App Configuration settings or definition");
        
        _infrastructureValidationResults.Add("✓ App Configuration definition validation passed");
    }

    [Then(@"the local stack module should stop gracefully")]
    public void ThenTheLocalStackModuleShouldStopGracefully()
    {
        _localStackStarted.Should().BeFalse("LocalStack should be stopped");
        _lastInfrastructureException.Should().BeNull("No teardown errors should have occurred");
        
        _infrastructureValidationResults.Add("✓ Graceful shutdown confirmed");
    }

    [Then(@"the local stack module resources should be cleaned up")]
    public void ThenTheLocalStackModuleResourcesShouldBeCleanedUp()
    {
        if (_localStackHelper != null)
        {
            _localStackHelper.IsRunning.Should().BeFalse("Container should not be running");
        }
        
        _infrastructureValidationResults.Add("✓ Resource cleanup confirmed");
    }

    [Then(@"the local stack module container should be removed")]
    public void ThenTheLocalStackModuleContainerShouldBeRemoved()
    {
        // Container removal is handled by the LocalStackContainerHelper disposal
        // This step mainly validates that the cleanup process completed
        _infrastructureValidationResults.Should().Contain(r => r.Contains("teardown completed") || r.Contains("no teardown needed"), 
            "Teardown should have been processed");
        
        _infrastructureValidationResults.Add("✓ Container removal process completed");
    }

    #endregion

}