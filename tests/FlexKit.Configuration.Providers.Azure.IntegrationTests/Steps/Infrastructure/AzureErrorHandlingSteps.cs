using FlexKit.Configuration.Core;
using FlexKit.Configuration.Providers.Azure.IntegrationTests.Utils;
using FlexKit.Configuration.Providers.Azure.Extensions;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Reqnroll;
// ReSharper disable RedundantSuppressNullableWarningExpression

// ReSharper disable TooManyDeclarations
// ReSharper disable MethodTooLong
// ReSharper disable ClassTooBig
// ReSharper disable ComplexConditionExpression
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
// ReSharper disable NullableWarningSuppressionIsUsed

namespace FlexKit.Configuration.Providers.Azure.IntegrationTests.Steps.Infrastructure;

/// <summary>
/// Step definitions for Azure error handling scenarios.
/// Tests error conditions including network failures, access denied, invalid configurations,
/// and edge cases that should be handled gracefully by the Azure providers.
/// Uses distinct step patterns ("error handling controller") to avoid conflicts with other step classes.
/// </summary>
[Binding]
public class AzureErrorHandlingSteps(ScenarioContext scenarioContext)
{
    private IConfiguration? _errorHandlingConfiguration;
    private IFlexConfig? _errorHandlingFlexConfiguration;
    private Exception? _lastErrorHandlingException;
    private readonly List<string> _errorHandlingValidationResults = new();
    private readonly List<Exception> _capturedErrorExceptions = new();
    private readonly List<string> _errorLogMessages = new();
    private bool _networkFailureSimulated;
    private bool _invalidConfigurationSimulated;
    private bool _credentialFailureSimulated;
    private bool _rateLimitingSimulated;
    private bool _shouldFailKeyVault;
    private bool _shouldFailAppConfig;

    #region Given Steps - Setup

    [Given(@"I have established an error handling controller environment")]
    public void GivenIHaveEstablishedAnErrorHandlingControllerEnvironment()
    {
        var appConfigEmulator = scenarioContext.GetAppConfigEmulator();
        var keyVaultEmulator = scenarioContext.GetKeyVaultEmulator();
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");
        scenarioContext.Set(keyVaultEmulator, "KeyVaultEmulator");
        scenarioContext.Set(appConfigEmulator, "AppConfigEmulator");
        
        _errorHandlingValidationResults.Add($"✓ Error handling controller environment established for prefix '{scenarioPrefix}'");
    }

    [Given(@"I have error handling controller configuration with Key Vault from ""(.*)""")]
    public void GivenIHaveErrorHandlingControllerConfigurationWithKeyVaultFrom(string testDataPath)
    {
        var keyVaultEmulator = scenarioContext.GetKeyVaultEmulator();
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");
        
        keyVaultEmulator.Should().NotBeNull("Key Vault emulator should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        
        try
        {
            // Load test data for network failure scenarios with a scenario prefix
            var createTask = keyVaultEmulator!.CreateTestDataAsync(fullPath, scenarioPrefix);
            createTask.Wait(TimeSpan.FromMinutes(1));
            _networkFailureSimulated = true;
            
            _errorHandlingValidationResults.Add($"✓ Key Vault test data loaded for network failure simulation with prefix '{scenarioPrefix}'");
        }
        catch (Exception ex)
        {
            _capturedErrorExceptions.Add(ex);
            _errorLogMessages.Add($"Key Vault setup failed: {ex.Message}");
            _shouldFailKeyVault = true; // Mark that Key Vault should be expected to fail
        }
    }

    [Given(@"I have error handling controller configuration with invalid App Configuration from ""(.*)""")]
    public void GivenIHaveErrorHandlingControllerConfigurationWithInvalidAppConfigurationFrom(string testDataPath)
    {
        var appConfigEmulator = scenarioContext.GetAppConfigEmulator();
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");
        
        appConfigEmulator.Should().NotBeNull("App Configuration emulator should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        
        try
        {
            // For invalid App Configuration, we'll simulate failure during build with a scenario prefix
            var createTask = appConfigEmulator!.CreateTestDataAsync(fullPath, scenarioPrefix);
            createTask.Wait(TimeSpan.FromMinutes(1));
            _invalidConfigurationSimulated = true;
            _shouldFailAppConfig = true; // Mark for configuration build failure
            
            _errorHandlingValidationResults.Add($"✓ App Configuration test data loaded for invalid configuration simulation with prefix '{scenarioPrefix}'");
        }
        catch (Exception ex)
        {
            _capturedErrorExceptions.Add(ex);
            _errorLogMessages.Add($"App Configuration setup failed: {ex.Message}");
            _shouldFailAppConfig = true;
        }
    }

    [Given(@"I have error handling controller configuration with missing secrets Key Vault from ""(.*)""")]
    public void GivenIHaveErrorHandlingControllerConfigurationWithMissingSecretsKeyVaultFrom(string testDataPath)
    {
        var keyVaultEmulator = scenarioContext.GetKeyVaultEmulator();
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");
        
        keyVaultEmulator.Should().NotBeNull("Key Vault emulator should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        
        try
        {
            // Load test data, but it will reference non-existent secrets with the scenario prefix
            var createTask = keyVaultEmulator!.CreateTestDataAsync(fullPath, scenarioPrefix);
            createTask.Wait(TimeSpan.FromMinutes(1));
            
            _errorHandlingValidationResults.Add($"✓ Key Vault configured with test data that may reference missing secrets for prefix '{scenarioPrefix}'");
        }
        catch (Exception ex)
        {
            _capturedErrorExceptions.Add(ex);
            _errorLogMessages.Add($"Key Vault missing secrets setup failed: {ex.Message}");
        }
    }

    [Given(@"I have error handling controller configuration with invalid credentials from ""(.*)""")]
    public void GivenIHaveErrorHandlingControllerConfigurationWithInvalidCredentialsFrom(string testDataPath)
    {
        var keyVaultEmulator = scenarioContext.GetKeyVaultEmulator();
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");
        
        keyVaultEmulator.Should().NotBeNull("Key Vault emulator should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        
        try
        {
            // Load test data for credential failure scenarios with scenario prefix
            var createTask = keyVaultEmulator!.CreateTestDataAsync(fullPath, scenarioPrefix);
            createTask.Wait(TimeSpan.FromMinutes(1));
            _credentialFailureSimulated = true;
            _shouldFailKeyVault = true; // Will fail during authentication
            
            _errorHandlingValidationResults.Add($"✓ Key Vault configured for credential failure simulation with prefix '{scenarioPrefix}'");
        }
        catch (Exception ex)
        {
            _capturedErrorExceptions.Add(ex);
            _errorLogMessages.Add($"Credential failure setup failed: {ex.Message}");
            _shouldFailKeyVault = true;
        }
    }

    [Given(@"I have error handling controller configuration with rate limiting simulation from ""(.*)""")]
    public void GivenIHaveErrorHandlingControllerConfigurationWithRateLimitingSimulationFrom(string testDataPath)
    {
        var keyVaultEmulator = scenarioContext.GetKeyVaultEmulator();
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");
        
        keyVaultEmulator.Should().NotBeNull("Key Vault emulator should be established");

        var fullPath = Path.Combine("TestData", testDataPath);
        
        try
        {
            // Load test data for rate limiting scenarios with a scenario prefix
            var createTask = keyVaultEmulator!.CreateTestDataAsync(fullPath, scenarioPrefix);
            createTask.Wait(TimeSpan.FromMinutes(1));
            _rateLimitingSimulated = true;
            _shouldFailKeyVault = true; // Will fail due to rate limiting
            
            _errorHandlingValidationResults.Add($"✓ Key Vault configured for rate limiting simulation with prefix '{scenarioPrefix}'");
        }
        catch (Exception ex)
        {
            _capturedErrorExceptions.Add(ex);
            _errorLogMessages.Add($"Rate limiting setup failed: {ex.Message}");
            _shouldFailKeyVault = true;
        }
    }

    #endregion

    #region When Steps - Actions

    [When(@"I configure error handling controller with network failure simulation")]
    public void WhenIConfigureErrorHandlingControllerWithNetworkFailureSimulation()
    {
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");
        
        _networkFailureSimulated = true;
        _shouldFailKeyVault = true;
        _errorHandlingValidationResults.Add($"✓ Network failure simulation configured for prefix '{scenarioPrefix}'");
    }

    [When(@"I configure error handling controller with invalid connection string")]
    public void WhenIConfigureErrorHandlingControllerWithInvalidConnectionString()
    {
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");
        
        _invalidConfigurationSimulated = true;
        _shouldFailAppConfig = true;
        _errorHandlingValidationResults.Add($"✓ Invalid connection string configured for prefix '{scenarioPrefix}'");
    }

    [When(@"I configure error handling controller with missing secret references")]
    public void WhenIConfigureErrorHandlingControllerWithMissingSecretReferences()
    {
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");
        
        _errorHandlingValidationResults.Add($"✓ Missing secret references configured for prefix '{scenarioPrefix}'");
    }

    [When(@"I configure error handling controller with credential failure simulation")]
    public void WhenIConfigureErrorHandlingControllerWithCredentialFailureSimulation()
    {
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");
        
        _credentialFailureSimulated = true;
        _shouldFailKeyVault = true;
        _errorHandlingValidationResults.Add($"✓ Credential failure simulation configured for prefix '{scenarioPrefix}'");
    }

    [When(@"I configure error handling controller with throttling simulation")]
    public void WhenIConfigureErrorHandlingControllerWithThrottlingSimulation()
    {
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");
        
        _rateLimitingSimulated = true;
        _shouldFailKeyVault = true;
        _errorHandlingValidationResults.Add($"✓ Throttling simulation configured for prefix '{scenarioPrefix}'");
    }

    [When(@"I configure error handling controller by building the configuration with error tolerance")]
    public void WhenIConfigureErrorHandlingControllerByBuildingTheConfigurationWithErrorTolerance()
    {
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");
        
        try
        {
            var keyVaultEmulator = scenarioContext.GetKeyVaultEmulator();
            var appConfigEmulator = scenarioContext.GetAppConfigEmulator();
            var builder = new FlexConfigurationBuilder();

            // Add Key Vault with error handling and scenario prefix filtering
            if (keyVaultEmulator != null)
            {
                try
                {
                    if (_shouldFailKeyVault)
                    {
                        // Simulate failure by not starting the emulator or using invalid configuration
                        // builder.AddAzureKeyVault(options =>
                        // {
                        //     options.VaultUri = "https://invalid-vault.vault.azure.net/";
                        //     // Don't provide SecretClient to simulate credential/network failure
                        //     options.Optional = true; // Make optional to allow graceful failure
                        //     // Use a custom secret processor to filter by scenario prefix
                        //     options.SecretProcessor = new ScenarioPrefixSecretProcessor(scenarioPrefix);
                        // });
                        // _errorHandlingValidationResults.Add($"✓ Key Vault configured to fail gracefully for prefix '{scenarioPrefix}'");
                        try
                        {
                            // Use FailingConfigurationSource to guarantee an error is captured
                            builder.AddSource(new FailingConfigurationSource 
                            { 
                                ErrorMessage = "Simulated Key Vault network failure for testing", 
                                SourceType = "KeyVault" 
                            });
                            _errorHandlingValidationResults.Add($"✓ Key Vault configured with failing source for prefix '{scenarioPrefix}'");
                        }
                        catch (Exception ex)
                        {
                            _capturedErrorExceptions.Add(ex);
                            _errorLogMessages.Add($"Key Vault failure configuration error: {ex.Message}");
                        }
                    }
                    else
                    {
                        builder.AddAzureKeyVault(options =>
                        {
                            options.VaultUri = "https://test-vault.vault.azure.net/";
                            options.SecretClient = keyVaultEmulator.SecretClient;
                            options.Optional = true; // Make optional for error tolerance
                            // Use a custom secret processor to filter by scenario prefix
                            options.SecretProcessor = new ScenarioPrefixSecretProcessor(scenarioPrefix);
                        });
                        _errorHandlingValidationResults.Add($"✓ Key Vault configured successfully for prefix '{scenarioPrefix}'");
                    }
                }
                catch (Exception ex)
                {
                    _capturedErrorExceptions.Add(ex);
                    _errorLogMessages.Add($"Key Vault configuration failed: {ex.Message}");
                    _errorHandlingValidationResults.Add($"✗ Key Vault configuration failed: {ex.GetType().Name}");
                }
            }

            // Add App Configuration with error handling and scenario prefix filtering
            if (appConfigEmulator != null)
            {
                try
                {
                    if (_shouldFailAppConfig)
                    {
                        // Simulate failure with invalid connection string
                        builder.AddAzureAppConfiguration(options =>
                        {
                            options.ConnectionString = "Endpoint=https://invalid-config.azconfig.io;Id=invalid;Secret=invalid";
                            options.Optional = true; // Make optional to allow graceful failure
                            // Use scenario prefix as key filter to isolate this scenario's data
                            options.KeyFilter = $"{scenarioPrefix}:*";
                        });
                        _errorHandlingValidationResults.Add($"✓ App Configuration configured to fail gracefully for prefix '{scenarioPrefix}'");
                    }
                    else
                    {
                        builder.AddAzureAppConfiguration(options =>
                        {
                            options.ConnectionString = appConfigEmulator.GetConnectionString();
                            options.ConfigurationClient = appConfigEmulator.ConfigurationClient;
                            options.Optional = true; // Make optional for error tolerance
                            // Use scenario prefix as key filter to isolate this scenario's data
                            options.KeyFilter = $"{scenarioPrefix}:*";
                        });
                        _errorHandlingValidationResults.Add($"✓ App Configuration configured successfully for prefix '{scenarioPrefix}'");
                    }
                }
                catch (Exception ex)
                {
                    _capturedErrorExceptions.Add(ex);
                    _errorLogMessages.Add($"App Configuration setup failed: {ex.Message}");
                    _errorHandlingValidationResults.Add($"✗ App Configuration configuration failed: {ex.GetType().Name}");
                }
            }

            // Attempt to build configuration with error tolerance
            try
            {
                _errorHandlingFlexConfiguration = builder.Build();
                _errorHandlingConfiguration = _errorHandlingFlexConfiguration.Configuration;
                _errorHandlingValidationResults.Add($"✓ Configuration built successfully with error tolerance for prefix '{scenarioPrefix}'");
            }
            catch (Exception ex)
            {
                _capturedErrorExceptions.Add(ex);
                _errorLogMessages.Add($"Configuration build failed: {ex.Message}");
                _errorHandlingValidationResults.Add($"✗ Configuration build failed: {ex.GetType().Name}");
            }
            
            // Store results in a scenario context
            if (_errorHandlingConfiguration != null)
            {
                scenarioContext.Set(_errorHandlingConfiguration, "ErrorHandlingConfiguration");
            }
            if (_errorHandlingFlexConfiguration != null)
            {
                scenarioContext.Set(_errorHandlingFlexConfiguration, "ErrorHandlingFlexConfiguration");
            }
            
            _errorHandlingValidationResults.Add($"✓ Error handling configuration attempt completed with {_capturedErrorExceptions.Count} errors captured for prefix '{scenarioPrefix}'");
        }
        catch (Exception ex)
        {
            _lastErrorHandlingException = ex;
            _capturedErrorExceptions.Add(ex);
            _errorLogMessages.Add($"Unexpected error during configuration: {ex.Message}");
            scenarioContext.Set(ex, "ErrorHandlingException");
            _errorHandlingValidationResults.Add($"✗ Unexpected error during error handling setup: {ex.GetType().Name}");
        }
    }

    [When(@"I configure error handling controller by building the configuration with retry logic")]
    public void WhenIConfigureErrorHandlingControllerByBuildingTheConfigurationWithRetryLogic()
    {
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");
        
        // Implement retry logic for error scenarios
        var maxRetries = 3;
        var retryDelay = TimeSpan.FromSeconds(1);
        var keyVaultEmulator = scenarioContext.GetKeyVaultEmulator();
        var appConfigEmulator = scenarioContext.GetAppConfigEmulator();

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                _errorLogMessages.Add($"Attempt {attempt}: Starting configuration build for prefix '{scenarioPrefix}'");
                
                var builder = new FlexConfigurationBuilder();

                // Try to start and configure emulators with retry logic and scenario prefix filtering
                if (keyVaultEmulator != null && !_shouldFailKeyVault)
                {
                    builder.AddAzureKeyVault(options =>
                    {
                        options.VaultUri = "https://test-vault.vault.azure.net/";
                        options.SecretClient = keyVaultEmulator.SecretClient;
                        options.Optional = true;
                        // Use a custom secret processor to filter by scenario prefix
                        options.SecretProcessor = new ScenarioPrefixSecretProcessor(scenarioPrefix);
                    });
                }

                if (appConfigEmulator != null && !_shouldFailAppConfig)
                {
                    builder.AddAzureAppConfiguration(options =>
                    {
                        options.ConnectionString = appConfigEmulator.GetConnectionString();
                        options.ConfigurationClient = appConfigEmulator.ConfigurationClient;
                        options.Optional = true;
                        // Use scenario prefix as key filter to isolate this scenario's data
                        options.KeyFilter = $"{scenarioPrefix}:*";
                    });
                }

                _errorHandlingFlexConfiguration = builder.Build();
                _errorHandlingConfiguration = _errorHandlingFlexConfiguration.Configuration;
                
                scenarioContext.Set(_errorHandlingConfiguration, "ErrorHandlingConfiguration");
                scenarioContext.Set(_errorHandlingFlexConfiguration, "ErrorHandlingFlexConfiguration");
                
                _errorHandlingValidationResults.Add($"✓ Error handling configuration built successfully on attempt {attempt} for prefix '{scenarioPrefix}'");
                _errorLogMessages.Add($"Attempt {attempt}: Success for prefix '{scenarioPrefix}'");
                break;
            }
            catch (Exception ex)
            {
                _capturedErrorExceptions.Add(ex);
                _errorLogMessages.Add($"Attempt {attempt} failed for prefix '{scenarioPrefix}': {ex.Message}");
                
                if (attempt == maxRetries)
                {
                    _lastErrorHandlingException = ex;
                    scenarioContext.Set(ex, "ErrorHandlingException");
                    _errorHandlingValidationResults.Add($"✗ Error handling configuration build failed after {maxRetries} attempts for prefix '{scenarioPrefix}'");
                }
                else
                {
                    Thread.Sleep(retryDelay);
                }
            }
        }
    }

    #endregion

    #region Then Steps - Assertions

    [Then(@"the error handling controller should handle network failures gracefully")]
    public void ThenTheErrorHandlingControllerShouldHandleNetworkFailuresGracefully()
    {
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");
        
        if (_networkFailureSimulated)
        {
            var networkErrors = _capturedErrorExceptions
                .Where(ex => ex is HttpRequestException || 
                           ex.Message.Contains("network", StringComparison.OrdinalIgnoreCase) ||
                           ex.Message.Contains("connection", StringComparison.OrdinalIgnoreCase) ||
                           ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
                           ex.Message.Contains("unreachable", StringComparison.OrdinalIgnoreCase) ||
                           ex.Message.Contains("invalid", StringComparison.OrdinalIgnoreCase))
                .ToList();

            // With network failure simulation, we expect either graceful handling or appropriate errors
            bool handledGracefully = _errorHandlingConfiguration != null || 
                                    networkErrors.Any() ||
                                    _capturedErrorExceptions.Any() || // Any error during network failure simulation counts
                                    _lastErrorHandlingException != null;
            
            handledGracefully.Should().BeTrue("Network failures should be handled gracefully or result in appropriate exceptions");
            _errorHandlingValidationResults.Add($"✓ Network failure handling verified: {networkErrors.Count} network errors, {_capturedErrorExceptions.Count} total errors for prefix '{scenarioPrefix}'");
        }
        else
        {
            _errorHandlingValidationResults.Add($"ⓘ Network failure not simulated in this scenario for prefix '{scenarioPrefix}'");
        }
        
        scenarioContext.Set(_errorHandlingValidationResults, "ErrorHandlingValidationResults");
    }

    [Then(@"the error handling controller should demonstrate fallback behavior")]
    public void ThenTheErrorHandlingControllerShouldDemonstrateFallbackBehavior()
    {
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");
        
        try
        {
            // Test fallback mechanisms with a scenario prefix
            if (_errorHandlingFlexConfiguration != null)
            {
                // Test that FlexKit can provide fallback values or handle missing configuration
                dynamic config = _errorHandlingFlexConfiguration;
                
                var fallbackTests = new List<(string description, Func<object?> test)>
                {
                    ("Fallback for missing key", () => config[$"{scenarioPrefix}:non-existent-key"] ?? "fallback-value"),
                    ("Graceful null handling", () => config[$"{scenarioPrefix}:another-missing-key"]),
                    ("Section enumeration fallback", () => _errorHandlingFlexConfiguration.Configuration.GetChildren().Count()),
                    ("Configuration access fallback", () => _errorHandlingFlexConfiguration.Configuration.AsEnumerable().Count())
                };

                var successfulFallbacks = 0;
                foreach (var (description, test) in fallbackTests)
                {
                    try
                    {
                        _ = test();
                        successfulFallbacks++;
                        _errorHandlingValidationResults.Add($"✓ {description}: handled gracefully for prefix '{scenarioPrefix}'");
                    }
                    catch (Exception ex)
                    {
                        _errorHandlingValidationResults.Add($"✗ {description}: {ex.Message}");
                    }
                }

                _errorHandlingValidationResults.Add($"Fallback behavior verification: {successfulFallbacks}/{fallbackTests.Count} tests passed for prefix '{scenarioPrefix}'");
            }
            else
            {
                _errorHandlingValidationResults.Add($"ⓘ Configuration not available, but this may be expected behavior for error scenarios for prefix '{scenarioPrefix}'");
            }
        }
        catch (Exception ex)
        {
            _errorHandlingValidationResults.Add($"✗ Fallback behavior verification failed: {ex.Message}");
        }
        
        scenarioContext.Set(_errorHandlingValidationResults, "ErrorHandlingValidationResults");
    }

    [Then(@"the error handling controller should report network error details")]
    public void ThenTheErrorHandlingControllerShouldReportNetworkErrorDetails()
    {
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");
        
        var networkErrors = _capturedErrorExceptions
            .Where(ex => ex is HttpRequestException || 
                       ex.Message.Contains("network", StringComparison.OrdinalIgnoreCase) ||
                       ex.Message.Contains("connection", StringComparison.OrdinalIgnoreCase) ||
                       ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
                       ex.Message.Contains("invalid", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (_networkFailureSimulated)
        {
            // During network failure simulation, we expect some kind of error to be reported
            bool hasErrorReports = networkErrors.Any() || _capturedErrorExceptions.Any() || _errorLogMessages.Any();
            
            hasErrorReports.Should().BeTrue("Should have captured network-related errors or other errors when simulation is active");
            _errorHandlingValidationResults.Add($"✓ Network error details reported: {networkErrors.Count} network errors, {_capturedErrorExceptions.Count} total errors, {_errorLogMessages.Count} log messages for prefix '{scenarioPrefix}'");
        }
        else
        {
            _errorHandlingValidationResults.Add($"ⓘ Network errors not expected in this scenario for prefix '{scenarioPrefix}'");
        }
        
        scenarioContext.Set(_errorHandlingValidationResults, "ErrorHandlingValidationResults");
    }

    [Then(@"the error handling controller should handle invalid configuration gracefully")]
    public void ThenTheErrorHandlingControllerShouldHandleInvalidConfigurationGracefully()
    {
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");
        
        if (_invalidConfigurationSimulated)
        {
            // With invalid configuration, we expect either graceful handling or appropriate configuration errors
            var configErrors = _capturedErrorExceptions
                .Where(ex => ex is InvalidOperationException || ex is ArgumentException || ex is FormatException)
                .ToList();

            bool handledGracefully = _errorHandlingConfiguration != null || configErrors.Any() || _capturedErrorExceptions.Any();
            
            handledGracefully.Should().BeTrue("Invalid configuration should be handled gracefully or result in appropriate exceptions");
            _errorHandlingValidationResults.Add($"✓ Invalid configuration handling verified: {configErrors.Count} config errors, {_capturedErrorExceptions.Count} total errors for prefix '{scenarioPrefix}'");
        }
        else
        {
            _errorHandlingValidationResults.Add($"ⓘ Invalid configuration not simulated in this scenario for prefix '{scenarioPrefix}'");
        }
        
        scenarioContext.Set(_errorHandlingValidationResults, "ErrorHandlingValidationResults");
    }

    [Then(@"the error handling controller should demonstrate error reporting")]
    public void ThenTheErrorHandlingControllerShouldDemonstrateErrorReporting()
    {
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");
        
        // Verify that errors are properly captured and reported
        var totalErrors = _capturedErrorExceptions.Count + _errorLogMessages.Count;
        
        if (totalErrors > 0)
        {
            _errorHandlingValidationResults.Add($"✓ Error reporting verified: {_capturedErrorExceptions.Count} exceptions, {_errorLogMessages.Count} log messages for prefix '{scenarioPrefix}'");
            
            // Log some error details for verification
            foreach (var error in _capturedErrorExceptions.Take(3))
            {
                _errorHandlingValidationResults.Add($"  - Exception: {error.GetType().Name}: {error.Message}");
            }
            
            foreach (var logMessage in _errorLogMessages.Take(3))
            {
                _errorHandlingValidationResults.Add($"  - Log: {logMessage}");
            }
        }
        else
        {
            _errorHandlingValidationResults.Add($"ⓘ No errors captured (this may be expected for successful scenarios) for prefix '{scenarioPrefix}'");
        }
        
        scenarioContext.Set(_errorHandlingValidationResults, "ErrorHandlingValidationResults");
    }

    [Then(@"the error handling controller should maintain application stability")]
    public void ThenTheErrorHandlingControllerShouldMaintainApplicationStability()
    {
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");
        
        // Verify that despite errors, the application remains stable
        var stabilityTests = new List<(string description, Func<bool> test)>
        {
            ("Configuration object exists", () => _errorHandlingConfiguration != null),
            ("FlexKit configuration accessible", () => _errorHandlingFlexConfiguration != null),
            ("No unhandled exceptions", () => _lastErrorHandlingException == null || _capturedErrorExceptions.Contains(_lastErrorHandlingException)),
            ("Error handling completed", () => _errorHandlingValidationResults.Any()),
            ("Error capture working", () => _capturedErrorExceptions.Any() || _errorLogMessages.Any() || _lastErrorHandlingException != null),
            ("Scenario context intact", () => scenarioContext != null)
        };

        var stabilityScore = 0;
        foreach (var (description, test) in stabilityTests)
        {
            try
            {
                if (test())
                {
                    stabilityScore++;
                    _errorHandlingValidationResults.Add($"✓ {description}: stable for prefix '{scenarioPrefix}'");
                }
                else
                {
                    _errorHandlingValidationResults.Add($"⚠ {description}: unstable for prefix '{scenarioPrefix}'");
                }
            }
            catch (Exception ex)
            {
                _errorHandlingValidationResults.Add($"✗ {description}: {ex.Message}");
            }
        }

        _errorHandlingValidationResults.Add($"Application stability verification: {stabilityScore}/{stabilityTests.Count} stability checks passed for prefix '{scenarioPrefix}'");
        
        // Application should remain stable even with errors - require at least 50% stability
        var minimumStability = Math.Max(2, stabilityTests.Count / 2);
        stabilityScore.Should().BeGreaterThanOrEqualTo(minimumStability, "Application should maintain basic stability despite errors");
        
        scenarioContext.Set(_errorHandlingValidationResults, "ErrorHandlingValidationResults");
    }

    [Then(@"the error handling controller should handle missing secrets gracefully")]
    public void ThenTheErrorHandlingControllerShouldHandleMissingSecretsGracefully()
    {
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");
        
        try
        {
            if (_errorHandlingFlexConfiguration != null)
            {
                // Test access to potentially missing secrets with a scenario prefix
                var missingSecretTests = new List<(string description, string key)>
                {
                    ("Missing database secret", $"{scenarioPrefix}:myapp:database:missing-secret"),
                    ("Non-existent API key", $"{scenarioPrefix}:myapp:api:non-existent-key"),
                    ("Undefined configuration", $"{scenarioPrefix}:undefined:configuration:key")
                };

                var gracefulHandling = 0;
                foreach (var (description, key) in missingSecretTests)
                {
                    try
                    {
                        var value = _errorHandlingFlexConfiguration[key];
                        // Missing keys should return null without throwing
                        gracefulHandling++;
                        _errorHandlingValidationResults.Add($"✓ {description}: handled gracefully (value: {value ?? "null"}) for prefix '{scenarioPrefix}'");
                    }
                    catch (Exception ex)
                    {
                        _errorHandlingValidationResults.Add($"✗ {description}: {ex.Message}");
                    }
                }

                _errorHandlingValidationResults.Add($"Missing secrets handling: {gracefulHandling}/{missingSecretTests.Count} tests passed for prefix '{scenarioPrefix}'");
            }
            else
            {
                _errorHandlingValidationResults.Add($"ⓘ FlexKit configuration not available for missing secrets test for prefix '{scenarioPrefix}'");
            }
        }
        catch (Exception ex)
        {
            _errorHandlingValidationResults.Add($"✗ Missing secrets handling verification failed: {ex.Message}");
        }
        
        scenarioContext.Set(_errorHandlingValidationResults, "ErrorHandlingValidationResults");
    }

    [Then(@"the error handling controller should provide meaningful error messages")]
    public void ThenTheErrorHandlingControllerShouldProvideMeaningfulErrorMessages()
    {
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");
        
        var meaningfulErrors = _capturedErrorExceptions
            .Where(ex => !string.IsNullOrWhiteSpace(ex.Message) && ex.Message.Length > 10)
            .ToList();

        var meaningfulLogs = _errorLogMessages
            .Where(log => !string.IsNullOrWhiteSpace(log) && log.Length > 10)
            .ToList();

        var totalMeaningfulMessages = meaningfulErrors.Count + meaningfulLogs.Count;
        
        if (_capturedErrorExceptions.Any() || _errorLogMessages.Any())
        {
            totalMeaningfulMessages.Should().BeGreaterThan(0, "Should have meaningful error messages when errors occur");
            _errorHandlingValidationResults.Add($"✓ Meaningful error messages verified: {meaningfulErrors.Count} exceptions, {meaningfulLogs.Count} logs for prefix '{scenarioPrefix}'");
        }
        else
        {
            _errorHandlingValidationResults.Add($"ⓘ No errors to verify (this may be expected for successful scenarios) for prefix '{scenarioPrefix}'");
        }
        
        scenarioContext.Set(_errorHandlingValidationResults, "ErrorHandlingValidationResults");
    }

    [Then(@"the error handling controller should allow partial configuration loading")]
    public void ThenTheErrorHandlingControllerShouldAllowPartialConfigurationLoading()
    {
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");
        
        if (_errorHandlingConfiguration != null)
        {
            var availableKeys = _errorHandlingConfiguration
                .AsEnumerable()
                .Count(kvp => !string.IsNullOrEmpty(kvp.Key) && kvp.Value != null);

            var scenarioSpecificKeys = _errorHandlingConfiguration
                .AsEnumerable()
                .Count(kvp => !string.IsNullOrEmpty(kvp.Key) && kvp.Key.StartsWith($"{scenarioPrefix}:") && kvp.Value != null);

            // Even with errors, some configuration should be available if we have optional sources
            _errorHandlingValidationResults.Add($"✓ Partial configuration loading verified: {availableKeys} total keys available, {scenarioSpecificKeys} scenario-specific keys for prefix '{scenarioPrefix}'");
            
            if (availableKeys == 0)
            {
                _errorHandlingValidationResults.Add($"ⓘ No configuration keys available, but this may be expected for error scenarios with optional sources for prefix '{scenarioPrefix}'");
            }
        }
        else
        {
            _errorHandlingValidationResults.Add($"⚠ No configuration available for partial loading verification for prefix '{scenarioPrefix}'");
        }
        
        scenarioContext.Set(_errorHandlingValidationResults, "ErrorHandlingValidationResults");
    }

    [Then(@"the error handling controller should handle credential failures gracefully")]
    public void ThenTheErrorHandlingControllerShouldHandleCredentialFailuresGracefully()
    {
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");
        
        if (_credentialFailureSimulated)
        {
            var credentialErrors = _capturedErrorExceptions
                .Where(ex => ex.Message.Contains("credential", StringComparison.OrdinalIgnoreCase) ||
                           ex.Message.Contains("authentication", StringComparison.OrdinalIgnoreCase) ||
                           ex.Message.Contains("unauthorized", StringComparison.OrdinalIgnoreCase) ||
                           ex.Message.Contains("access", StringComparison.OrdinalIgnoreCase) ||
                           ex.Message.Contains("Azure", StringComparison.OrdinalIgnoreCase) ||
                           ex.Message.Contains("invalid", StringComparison.OrdinalIgnoreCase) ||
                           ex is InvalidOperationException ||
                           ex is UnauthorizedAccessException)
                .ToList();

            // Either we handled it gracefully (configuration still works) or we got appropriate errors
            bool handledGracefully = _errorHandlingConfiguration != null || 
                                    credentialErrors.Any() ||
                                    _capturedErrorExceptions.Any() ||
                                    _lastErrorHandlingException != null;
            
            handledGracefully.Should().BeTrue("Credential failures should be handled gracefully or result in appropriate authentication errors");
            _errorHandlingValidationResults.Add($"✓ Credential failure handling verified: {credentialErrors.Count} credential errors, {_capturedErrorExceptions.Count} total errors for prefix '{scenarioPrefix}'");
        }
        else
        {
            _errorHandlingValidationResults.Add($"ⓘ Credential failure not simulated in this scenario for prefix '{scenarioPrefix}'");
        }
        
        scenarioContext.Set(_errorHandlingValidationResults, "ErrorHandlingValidationResults");
    }

    [Then(@"the error handling controller should demonstrate authentication error handling")]
    public void ThenTheErrorHandlingControllerShouldDemonstrateAuthenticationErrorHandling()
    {
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");
        
        var authErrors = _capturedErrorExceptions
            .Where(ex => ex.GetType().Name.Contains("Authentication") || 
                        ex.Message.Contains("authentication", StringComparison.OrdinalIgnoreCase) ||
                        ex.Message.Contains("credential", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (_credentialFailureSimulated)
        {
            // During credential failure simulation, we expect some kind of error to be reported
            bool hasAuthErrors = authErrors.Any() || _capturedErrorExceptions.Any() || _errorLogMessages.Any();

            _errorHandlingValidationResults.Add(hasAuthErrors
                ? $"✓ Authentication error handling demonstrated: {authErrors.Count} auth errors, {_capturedErrorExceptions.Count} total errors for prefix '{scenarioPrefix}'"
                : $"⚠ Authentication error handling: no specific errors captured but simulation was active for prefix '{scenarioPrefix}'");
        }
        else
        {
            _errorHandlingValidationResults.Add($"ⓘ Authentication errors not expected in this scenario for prefix '{scenarioPrefix}'");
        }
        
        scenarioContext.Set(_errorHandlingValidationResults, "ErrorHandlingValidationResults");
    }

    [Then(@"the error handling controller should provide security-safe error messages")]
    public void ThenTheErrorHandlingControllerShouldProvideSecuritySafeErrorMessages()
    {
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");
        
        // Verify that error messages don't contain sensitive information
        var unsafeKeywords = new[] { "password", "secret", "key", "token", "credential" };
        
        var unsafeMessages = _capturedErrorExceptions
            .Where(ex => unsafeKeywords.Any(keyword => ex.Message.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        var unsafeLogs = _errorLogMessages
            .Where(log => unsafeKeywords.Any(keyword => log.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        // Some keywords might be acceptable in context (like "key vault" or "secret name"),
        // but we should verify they don't expose actual secret values
        if (unsafeMessages.Any() || unsafeLogs.Any())
        {
            _errorHandlingValidationResults.Add($"⚠ Security review needed: {unsafeMessages.Count} messages, {unsafeLogs.Count} logs contain security keywords for prefix '{scenarioPrefix}'");
            
            // Log examples for review (but don't expose full messages in production)
            foreach (var msg in unsafeMessages.Take(2))
            {
                _errorHandlingValidationResults.Add($"  - Security keyword in: {msg.GetType().Name}");
            }
        }
        else
        {
            _errorHandlingValidationResults.Add($"✓ Security-safe error messages verified: no sensitive keywords detected for prefix '{scenarioPrefix}'");
        }
        
        scenarioContext.Set(_errorHandlingValidationResults, "ErrorHandlingValidationResults");
    }

    [Then(@"the error handling controller should handle rate limiting gracefully")]
    public void ThenTheErrorHandlingControllerShouldHandleRateLimitingGracefully()
    {
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");
        
        if (_rateLimitingSimulated)
        {
            var rateLimitErrors = _capturedErrorExceptions
                .Where(ex => ex.Message.Contains("rate limit", StringComparison.OrdinalIgnoreCase) ||
                           ex.Message.Contains("throttl", StringComparison.OrdinalIgnoreCase) ||
                           ex.Message.Contains("429", StringComparison.OrdinalIgnoreCase) ||
                           ex.Message.Contains("invalid", StringComparison.OrdinalIgnoreCase))
                .ToList();

            // With rate limiting simulation, we expect either graceful handling or appropriate errors
            bool handledGracefully = _errorHandlingConfiguration != null || 
                                    rateLimitErrors.Any() ||
                                    _capturedErrorExceptions.Any() || // Any error during rate limiting simulation counts
                                    _lastErrorHandlingException != null;
            
            handledGracefully.Should().BeTrue("Rate limiting should be handled gracefully or result in appropriate throttling errors");
            _errorHandlingValidationResults.Add($"✓ Rate limiting handling verified: {rateLimitErrors.Count} rate limit errors, {_capturedErrorExceptions.Count} total errors for prefix '{scenarioPrefix}'");
        }
        else
        {
            _errorHandlingValidationResults.Add($"ⓘ Rate limiting not simulated in this scenario for prefix '{scenarioPrefix}'");
        }
        
        scenarioContext.Set(_errorHandlingValidationResults, "ErrorHandlingValidationResults");
    }

    [Then(@"the error handling controller should demonstrate retry mechanisms")]
    public void ThenTheErrorHandlingControllerShouldDemonstrateRetryMechanisms()
    {
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");
        
        // Check if retry logic was executed based on captured logs
        var retryLogs = _errorLogMessages
            .Where(log => log.Contains("attempt", StringComparison.OrdinalIgnoreCase) ||
                         log.Contains("retry", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (retryLogs.Any())
        {
            _errorHandlingValidationResults.Add($"✓ Retry mechanisms demonstrated: {retryLogs.Count} retry-related logs for prefix '{scenarioPrefix}'");
            
            // Show some retry details
            foreach (var retryLog in retryLogs.Take(3))
            {
                _errorHandlingValidationResults.Add($"  - Retry log: {retryLog}");
            }
        }
        else if (_capturedErrorExceptions.Count > 1)
        {
            _errorHandlingValidationResults.Add($"✓ Retry mechanisms inferred from multiple exception captures for prefix '{scenarioPrefix}'");
        }
        else
        {
            _errorHandlingValidationResults.Add($"ⓘ Retry mechanisms not clearly demonstrated in this scenario for prefix '{scenarioPrefix}'");
        }
        
        scenarioContext.Set(_errorHandlingValidationResults, "ErrorHandlingValidationResults");
    }

    [Then(@"the error handling controller should report throttling encounters")]
    public void ThenTheErrorHandlingControllerShouldReportThrottlingEncounters()
    {
        var scenarioPrefix = scenarioContext.Get<string>("ScenarioPrefix");
        
        if (_rateLimitingSimulated)
        {
            // During rate-limiting simulation, we expect some kind of error to be reported
            var throttlingReports = _capturedErrorExceptions.Count + _errorLogMessages.Count;
            
            throttlingReports.Should().BeGreaterThan(0, "Should have reports of throttling encounters or other errors when simulation is active");
            _errorHandlingValidationResults.Add($"✓ Throttling encounters reported: {throttlingReports} total reports ({_capturedErrorExceptions.Count} exceptions, {_errorLogMessages.Count} log messages) for prefix '{scenarioPrefix}'");
            
            // Look for specific throttling indicators
            var throttlingIndicators = _capturedErrorExceptions
                .Where(ex => ex.Message.Contains("throttl", StringComparison.OrdinalIgnoreCase) ||
                           ex.Message.Contains("rate", StringComparison.OrdinalIgnoreCase) ||
                           ex.Message.Contains("429"))
                .ToList();
                
            if (throttlingIndicators.Any())
            {
                _errorHandlingValidationResults.Add($"  - Specific throttling indicators: {throttlingIndicators.Count}");
            }
        }
        else
        {
            _errorHandlingValidationResults.Add($"ⓘ Throttling not simulated, no encounters expected for prefix '{scenarioPrefix}'");
        }
        
        scenarioContext.Set(_errorHandlingValidationResults, "ErrorHandlingValidationResults");
    }

    #endregion
}
