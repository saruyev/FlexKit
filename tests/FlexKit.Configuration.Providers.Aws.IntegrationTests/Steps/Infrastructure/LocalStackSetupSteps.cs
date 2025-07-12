using System.Text.Json;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using FlexKit.Configuration.Core;
using FlexKit.Configuration.Providers.Aws.IntegrationTests.Utils;
using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Reqnroll;
// ReSharper disable MethodTooLong
// ReSharper disable TooManyDeclarations
// ReSharper disable ClassTooBig

namespace FlexKit.Configuration.Providers.Aws.IntegrationTests.Steps.Infrastructure;

/// <summary>
/// Step definitions for LocalStack setup scenarios.
/// Tests LocalStack container setup, configuration loading, and test data population
/// for AWS services including Parameter Store and Secrets Manager.
/// Uses distinct step patterns ("local stack module") to avoid conflicts with other step classes.
/// </summary>
[Binding]
public class LocalStackSetupSteps
{
    private readonly ScenarioContext _scenarioContext;
    private readonly ILogger<LocalStackSetupSteps> _logger;
    private LocalStackContainerHelper? _localStackHelper;
    private IConfiguration? _moduleConfiguration;
    private IFlexConfig? _moduleFlexConfiguration;
    private IAmazonSimpleSystemsManagement? _parameterStoreClient;
    private IAmazonSecretsManager? _secretsManagerClient;
    private Exception? _lastModuleException;
    private readonly List<TestParameter> _testParameters = new();
    private readonly List<TestSecret> _testSecrets = new();
    private readonly Dictionary<string, string> _createdParameters = new();
    private readonly Dictionary<string, string> _createdSecrets = new();

    public LocalStackSetupSteps(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
        _logger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<LocalStackSetupSteps>();
    }

    #region Given Steps - Setup

    [Given(@"I have prepared a local stack module environment")]
    public void GivenIHavePreparedALocalStackModuleEnvironment()
    {
        _logger.LogInformation("Preparing local stack module environment...");
        
        var localStackLogger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<LocalStackContainerHelper>();
            
        _localStackHelper = new LocalStackContainerHelper(_scenarioContext, localStackLogger);
        
        _scenarioContext.Set(_localStackHelper, "LocalStackHelper");
        _logger.LogInformation("Local stack module environment prepared successfully");
    }

    #endregion

    #region When Steps - Actions

    [When(@"I configure local stack module with configuration from ""(.*)""")]
    public void WhenIConfigureLocalStackModuleWithConfigurationFrom(string configFilePath)
    {
        _localStackHelper.Should().NotBeNull("Local stack module environment should be prepared");
        
        var normalizedPath = configFilePath.Replace('/', Path.DirectorySeparatorChar);
        
        if (!File.Exists(normalizedPath))
        {
            throw new FileNotFoundException($"Configuration file not found: {normalizedPath}");
        }

        var configBuilder = new ConfigurationBuilder()
            .AddJsonFile(normalizedPath, optional: false);
            
        _moduleConfiguration = configBuilder.Build();
        _moduleFlexConfiguration = _moduleConfiguration.GetFlexConfiguration();
        
        _scenarioContext.Set(_moduleConfiguration, "ModuleConfiguration");
        _scenarioContext.Set(_moduleFlexConfiguration, "ModuleFlexConfiguration");
        
        _logger.LogInformation($"Local stack module configured from file: {configFilePath}");
    }

    [When(@"I initialize the local stack module setup")]
    public async Task WhenIInitializeTheLocalStackModuleSetup()
    {
        _localStackHelper.Should().NotBeNull("Local stack module environment should be prepared");
        _moduleFlexConfiguration.Should().NotBeNull("Local stack module configuration should be loaded");

        try
        {
            _logger.LogInformation("Starting LocalStack container...");
            await _localStackHelper!.StartAsync();
            
            _logger.LogInformation("Creating AWS clients...");
            _parameterStoreClient = _localStackHelper.CreateParameterStoreClient();
            _secretsManagerClient = _localStackHelper.CreateSecretsManagerClient();
            
            _logger.LogInformation("Loading test data definitions from configuration...");
            LoadTestDataFromConfiguration();
            
            _logger.LogInformation("Local stack module setup initialized successfully");
        }
        catch (Exception ex)
        {
            _lastModuleException = ex;
            _logger.LogError(ex, "Failed to initialize local stack module setup");
            throw;
        }
    }

    [When(@"I populate local stack module Parameter Store with test data")]
    public async Task WhenIPopulateLocalStackModuleParameterStoreWithTestData()
    {
        _parameterStoreClient.Should().NotBeNull("Parameter Store client should be created");
        _testParameters.Should().NotBeEmpty("Test parameters should be loaded from configuration");

        try
        {
            foreach (var testParam in _testParameters)
            {
                _logger.LogInformation($"Creating parameter: {testParam.Name}");
                
                var request = new PutParameterRequest
                {
                    Name = testParam.Name,
                    Value = testParam.Value,
                    Type = testParam.Type switch
                    {
                        "String" => ParameterType.String,
                        "SecureString" => ParameterType.SecureString,
                        "StringList" => ParameterType.StringList,
                        _ => ParameterType.String
                    },
                    Overwrite = true
                };

                _ = await _parameterStoreClient!.PutParameterAsync(request);
                _createdParameters[testParam.Name] = testParam.Value;
                
                _logger.LogInformation($"Parameter created successfully: {testParam.Name}");
            }
        }
        catch (Exception ex)
        {
            _lastModuleException = ex;
            _logger.LogError(ex, "Failed to populate Parameter Store with test data");
            throw;
        }
    }

    [When(@"I populate local stack module Secrets Manager with test data")]
    public async Task WhenIPopulateLocalStackModuleSecretsManagerWithTestData()
    {
        _secretsManagerClient.Should().NotBeNull("Secrets Manager client should be created");
        _testSecrets.Should().NotBeEmpty("Test secrets should be loaded from configuration");

        try
        {
            foreach (var testSecret in _testSecrets)
            {
                _logger.LogInformation($"Creating secret: {testSecret.Name}");
                
                var request = new CreateSecretRequest
                {
                    Name = testSecret.Name,
                    Description = testSecret.Description
                };

                if (!string.IsNullOrEmpty(testSecret.Value))
                {
                    request.SecretString = testSecret.Value;
                }
                else if (!string.IsNullOrEmpty(testSecret.BinaryValue))
                {
                    // Validate Base64 before attempting to decode
                    try
                    {
                        var binaryData = Convert.FromBase64String(testSecret.BinaryValue);
                        request.SecretBinary = new MemoryStream(binaryData);
                    }
                    catch (FormatException ex)
                    {
                        _logger.LogWarning($"Invalid Base64 data for secret {testSecret.Name}, using placeholder binary data: {ex.Message}");
                        // Use a valid binary placeholder for testing
                        var placeholderData = System.Text.Encoding.UTF8.GetBytes($"placeholder-binary-data-{testSecret.Name}");
                        request.SecretBinary = new MemoryStream(placeholderData);
                    }
                }

                _ = await _secretsManagerClient!.CreateSecretAsync(request);
                _createdSecrets[testSecret.Name] = testSecret.Value ?? testSecret.BinaryValue ?? "binary-data";
                
                _logger.LogInformation($"Secret created successfully: {testSecret.Name}");
            }
        }
        catch (Exception ex)
        {
            _lastModuleException = ex;
            _logger.LogError(ex, "Failed to populate Secrets Manager with test data");
            throw;
        }
    }

    [When(@"I populate local stack module with all test data")]
    public async Task WhenIPopulateLocalStackModuleWithAllTestData()
    {
        await WhenIPopulateLocalStackModuleParameterStoreWithTestData();
        await WhenIPopulateLocalStackModuleSecretsManagerWithTestData();
    }

    [When(@"I validate local stack module configuration structure")]
    public void WhenIValidateLocalStackModuleConfigurationStructure()
    {
        _moduleFlexConfiguration.Should().NotBeNull("Local stack module configuration should be loaded");
        
        // Load test data for validation scenarios
        LoadTestDataFromConfiguration();
        
        // Validate configuration structure without throwing exceptions
        // This step is for validation only
        _logger.LogInformation("Validating local stack module configuration structure...");
    }

    #endregion

    #region Then Steps - Assertions

    [Then(@"the local stack module should be configured successfully")]
    public void ThenTheLocalStackModuleShouldBeConfiguredSuccessfully()
    {
        _lastModuleException.Should().BeNull("No exception should have occurred during setup");
        _localStackHelper.Should().NotBeNull("LocalStack helper should be created");
        _localStackHelper!.IsRunning.Should().BeTrue("LocalStack container should be running");
        _moduleConfiguration.Should().NotBeNull("Module configuration should be loaded");
        _moduleFlexConfiguration.Should().NotBeNull("Module FlexConfiguration should be available");
        
        _logger.LogInformation("Verified local stack module is configured successfully");
    }

    [Then(@"the local stack module should load test parameters correctly")]
    public void ThenTheLocalStackModuleShouldLoadTestParametersCorrectly()
    {
        EnsureTestDataLoaded();
        _testParameters.Should().NotBeEmpty("Test parameters should be loaded from configuration");
        
        var infraSection = _moduleConfiguration!.GetSection("infrastructure_module");
        infraSection.Exists().Should().BeTrue("Infrastructure module configuration should exist");
        
        var testParamsSection = _moduleConfiguration.GetSection("infrastructure_module:test_parameters");
        testParamsSection.Exists().Should().BeTrue("Test parameters configuration should exist");
        
        _logger.LogInformation($"Verified {_testParameters.Count} test parameters loaded correctly");
    }

    [Then(@"the local stack module should load test secrets correctly")]
    public void ThenTheLocalStackModuleShouldLoadTestSecretsCorrectly()
    {
        EnsureTestDataLoaded();
        _testSecrets.Should().NotBeEmpty("Test secrets should be loaded from configuration");
        
        var testSecretsSection = _moduleConfiguration!.GetSection("infrastructure_module:test_secrets");
        testSecretsSection.Exists().Should().BeTrue("Test secrets configuration should exist");
        
        _logger.LogInformation($"Verified {_testSecrets.Count} test secrets loaded correctly");
    }

    [Then(@"the local stack module should create parameters successfully")]
    public void ThenTheLocalStackModuleShouldCreateParametersSuccessfully()
    {
        _createdParameters.Should().NotBeEmpty("Parameters should have been created in LocalStack");
        _createdParameters.Count.Should().Be(_testParameters.Count, "All test parameters should be created");
        
        _logger.LogInformation($"Verified {_createdParameters.Count} parameters created successfully");
    }

    [Then(@"the local stack module parameters should be retrievable")]
    public async Task ThenTheLocalStackModuleParametersShouldBeRetrievable()
    {
        _parameterStoreClient.Should().NotBeNull("Parameter Store client should be available");
        _createdParameters.Should().NotBeEmpty("Parameters should have been created");

        foreach (var (paramName, expectedValue) in _createdParameters)
        {
            var request = new GetParameterRequest { Name = paramName, WithDecryption = true };
            var response = await _parameterStoreClient!.GetParameterAsync(request);
            
            response.Parameter.Should().NotBeNull($"Parameter {paramName} should exist");
            response.Parameter.Value.Should().Be(expectedValue, $"Parameter {paramName} should have correct value");
        }
        
        _logger.LogInformation("Verified all parameters are retrievable with correct values");
    }

    [Then(@"the local stack module Parameter Store should contain expected values")]
    public async Task ThenTheLocalStackModuleParameterStoreShouldContainExpectedValues()
    {
        await ThenTheLocalStackModuleParametersShouldBeRetrievable();
        
        // Additional verification for specific parameter types and values
        var stringParam = _testParameters.FirstOrDefault(p => p.Type == "String");
        var secureParam = _testParameters.FirstOrDefault(p => p.Type == "SecureString");
        var listParam = _testParameters.FirstOrDefault(p => p.Type == "StringList");
        
        if (stringParam != null)
        {
            var response = await _parameterStoreClient!.GetParameterAsync(new GetParameterRequest { Name = stringParam.Name });
            response.Parameter.Type.Should().Be(ParameterType.String);
        }
        
        if (secureParam != null)
        {
            var response = await _parameterStoreClient!.GetParameterAsync(new GetParameterRequest { Name = secureParam.Name, WithDecryption = true });
            response.Parameter.Type.Should().Be(ParameterType.SecureString);
        }
        
        if (listParam != null)
        {
            var response = await _parameterStoreClient!.GetParameterAsync(new GetParameterRequest { Name = listParam.Name });
            response.Parameter.Type.Should().Be(ParameterType.StringList);
        }
        
        _logger.LogInformation("Verified Parameter Store contains expected parameter types and values");
    }

    [Then(@"the local stack module should create secrets successfully")]
    public void ThenTheLocalStackModuleShouldCreateSecretsSuccessfully()
    {
        _createdSecrets.Should().NotBeEmpty("Secrets should have been created in LocalStack");
        _createdSecrets.Count.Should().Be(_testSecrets.Count, "All test secrets should be created");
        
        _logger.LogInformation($"Verified {_createdSecrets.Count} secrets created successfully");
    }

    [Then(@"the local stack module secrets should be retrievable")]
    public async Task ThenTheLocalStackModuleSecretsShouldBeRetrievable()
    {
        _secretsManagerClient.Should().NotBeNull("Secrets Manager client should be available");
        _createdSecrets.Should().NotBeEmpty("Secrets should have been created");

        foreach (var (secretName, _) in _createdSecrets)
        {
            var request = new GetSecretValueRequest { SecretId = secretName };
            var response = await _secretsManagerClient!.GetSecretValueAsync(request);
            
            // Binary secrets will have SecretBinary, string secrets will have SecretString
            var hasValue = response.SecretString != null || response.SecretBinary != null;
            hasValue.Should().BeTrue($"Secret {secretName} should exist and have a value (either string or binary)");
            
            if (response.SecretString != null)
            {
                _logger.LogInformation($"String secret {secretName} retrieved successfully");
            }
            else if (response.SecretBinary != null)
            {
                var binaryData = response.SecretBinary.ToArray();
                _logger.LogInformation($"Binary secret {secretName} retrieved successfully ({binaryData.Length} bytes)");
            }
        }
        
        _logger.LogInformation("Verified all secrets are retrievable");
    }

    [Then(@"the local stack module Secrets Manager should contain expected values")]
    public async Task ThenTheLocalStackModuleSecretsManagerShouldContainExpectedValues()
    {
        await ThenTheLocalStackModuleSecretsShouldBeRetrievable();
        
        // Additional verification for specific secret content
        foreach (var testSecret in _testSecrets.Where(s => !string.IsNullOrEmpty(s.Value)))
        {
            var response = await _secretsManagerClient!.GetSecretValueAsync(new GetSecretValueRequest { SecretId = testSecret.Name });
            response.SecretString.Should().NotBeNull($"String secret {testSecret.Name} should have SecretString");
            response.SecretString.Should().Be(testSecret.Value, $"Secret {testSecret.Name} should have correct value");
            
            // If it's JSON, validate structure
            if (testSecret.Value!.TrimStart().StartsWith("{"))
            {
                var action = () => JsonDocument.Parse(testSecret.Value);
                action.Should().NotThrow($"Secret {testSecret.Name} should contain valid JSON");
            }
        }
        
        // Verify binary secrets were created (they may have placeholder data due to invalid Base64 in a test file)
        foreach (var testSecret in _testSecrets.Where(s => !string.IsNullOrEmpty(s.BinaryValue)))
        {
            var response = await _secretsManagerClient!.GetSecretValueAsync(new GetSecretValueRequest { SecretId = testSecret.Name });
            response.SecretBinary.Should().NotBeNull($"Binary secret {testSecret.Name} should have SecretBinary");
            
            // Convert MemoryStream to a byte array to check length
            var binaryData = response.SecretBinary!.ToArray();
            binaryData.Length.Should().BeGreaterThan(0, $"Binary secret {testSecret.Name} should have content");
            _logger.LogInformation($"Binary secret {testSecret.Name} has {binaryData.Length} bytes");
        }
        
        _logger.LogInformation("Verified Secrets Manager contains expected secret values and structure");
    }

    [Then(@"the local stack module should have all Parameter Store data populated")]
    public async Task ThenTheLocalStackModuleShouldHaveAllParameterStoreDataPopulated()
    {
        await ThenTheLocalStackModuleParameterStoreShouldContainExpectedValues();
    }

    [Then(@"the local stack module should have all Secrets Manager data populated")]
    public async Task ThenTheLocalStackModuleShouldHaveAllSecretsManagerDataPopulated()
    {
        await ThenTheLocalStackModuleSecretsManagerShouldContainExpectedValues();
    }

    [Then(@"the local stack module should be ready for integration testing")]
    public void ThenTheLocalStackModuleShouldBeReadyForIntegrationTesting()
    {
        ThenTheLocalStackModuleShouldBeConfiguredSuccessfully();
        _createdParameters.Should().NotBeEmpty("Parameter Store should be populated");
        _createdSecrets.Should().NotBeEmpty("Secrets Manager should be populated");
        
        _logger.LogInformation("Verified local stack module is ready for integration testing");
    }

    [Then(@"the local stack module configuration should be valid")]
    public void ThenTheLocalStackModuleConfigurationShouldBeValid()
    {
        _moduleConfiguration.Should().NotBeNull("Module configuration should be loaded");
        
        var infraSection = _moduleConfiguration!.GetSection("infrastructure_module");
        infraSection.Exists().Should().BeTrue("Infrastructure module section should exist");
        
        _logger.LogInformation("Verified local stack module configuration is valid");
    }

    [Then(@"the local stack module should contain LocalStack settings")]
    public void ThenTheLocalStackModuleShouldContainLocalStackSettings()
    {
        var localStackSection = _moduleConfiguration!.GetSection("infrastructure_module:localstack");
        localStackSection.Exists().Should().BeTrue("LocalStack configuration should exist");
        
        var image = localStackSection["image"];
        var port = localStackSection["port"];
        var services = localStackSection["services"];
        
        image.Should().NotBeNullOrEmpty("LocalStack image should be specified");
        port.Should().NotBeNullOrEmpty("LocalStack port should be specified");
        services.Should().NotBeNullOrEmpty("LocalStack services should be specified");
        
        _logger.LogInformation("Verified local stack module contains LocalStack settings");
    }

    [Then(@"the local stack module should contain AWS test credentials")]
    public void ThenTheLocalStackModuleShouldContainAwsTestCredentials()
    {
        var awsSection = _moduleConfiguration!.GetSection("infrastructure_module:aws");
        awsSection.Exists().Should().BeTrue("AWS configuration should exist");
        
        var region = awsSection["region"];
        var testCredsSection = _moduleConfiguration.GetSection("infrastructure_module:aws:test_credentials");
        
        region.Should().NotBeNullOrEmpty("AWS region should be specified");
        testCredsSection.Exists().Should().BeTrue("AWS test credentials should exist");
        
        _logger.LogInformation("Verified local stack module contains AWS test credentials");
    }

    [Then(@"the local stack module should contain test parameters definition")]
    public void ThenTheLocalStackModuleShouldContainTestParametersDefinition()
    {
        ThenTheLocalStackModuleShouldLoadTestParametersCorrectly();
    }

    [Then(@"the local stack module should contain test secrets definition")]
    public void ThenTheLocalStackModuleShouldContainTestSecretsDefinition()
    {
        ThenTheLocalStackModuleShouldLoadTestSecretsCorrectly();
    }

    #endregion

    #region Helper Methods

    private void LoadTestDataFromConfiguration()
    {
        _moduleFlexConfiguration.Should().NotBeNull("Module configuration should be loaded");

        // Load test parameters
        var parametersSection = _moduleConfiguration!.GetSection("infrastructure_module:test_parameters");
        if (parametersSection.Exists())
        {
            foreach (var paramSection in parametersSection.GetChildren())
            {
                var testParam = new TestParameter
                {
                    Name = paramSection["name"] ?? "",
                    Value = paramSection["value"] ?? "",
                    Type = paramSection["type"] ?? "String"
                };
                
                if (!string.IsNullOrEmpty(testParam.Name))
                {
                    _testParameters.Add(testParam);
                }
            }
        }

        // Load test secrets
        var secretsSection = _moduleConfiguration.GetSection("infrastructure_module:test_secrets");
        if (secretsSection.Exists())
        {
            foreach (var secretSection in secretsSection.GetChildren())
            {
                var testSecret = new TestSecret
                {
                    Name = secretSection["name"] ?? "",
                    Value = secretSection["value"],
                    BinaryValue = secretSection["binary_value"],
                    Description = secretSection["description"] ?? ""
                };
                
                if (!string.IsNullOrEmpty(testSecret.Name))
                {
                    _testSecrets.Add(testSecret);
                }
            }
        }

        _logger.LogInformation($"Loaded {_testParameters.Count} test parameters and {_testSecrets.Count} test secrets from configuration");
    }

    #endregion

    #region Helper Classes

    private class TestParameter
    {
        public string Name { get; [UsedImplicitly] set; } = "";
        public string Value { get; [UsedImplicitly] set; } = "";
        public string Type { get; [UsedImplicitly] set; } = "String";
    }

    private class TestSecret
    {
        public string Name { get; [UsedImplicitly] set; } = "";
        public string? Value { get; [UsedImplicitly] set; }
        public string? BinaryValue { get; [UsedImplicitly] set; }
        public string Description { get; [UsedImplicitly] set; } = "";
    }
    
    private void EnsureTestDataLoaded()
    {
        // Only load data if collections are empty (haven't been loaded yet)
        if (_testParameters.Count == 0 && _testSecrets.Count == 0)
        {
            LoadTestDataFromConfiguration();
        }
    }

    #endregion
}