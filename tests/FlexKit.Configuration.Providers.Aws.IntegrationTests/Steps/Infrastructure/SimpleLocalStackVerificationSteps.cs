using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using FlexKit.Configuration.Providers.Aws.IntegrationTests.Utils;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Reqnroll;

namespace FlexKit.Configuration.Providers.Aws.IntegrationTests.Steps;

[Binding]
public class SimpleLocalStackVerificationSteps
{
    private readonly ScenarioContext _scenarioContext;
    private readonly ILogger<SimpleLocalStackVerificationSteps> _logger;
    private LocalStackContainerHelper? _localStackHelper;
    private IAmazonSimpleSystemsManagement? _ssmClient;
    private string? _testParameterValue;

    public SimpleLocalStackVerificationSteps(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
        _logger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<SimpleLocalStackVerificationSteps>();
    }

    [When(@"I start LocalStack container")]
    public async Task WhenIStartLocalStackContainer()
    {
        _logger.LogInformation("Creating LocalStack container helper...");
        
        var localStackLogger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<LocalStackContainerHelper>();
            
        _localStackHelper = new LocalStackContainerHelper(_scenarioContext, localStackLogger);
        
        _logger.LogInformation("Starting LocalStack container...");
        await _localStackHelper.StartAsync();
        
        _logger.LogInformation("LocalStack container started successfully");
    }

    [Then(@"LocalStack should be running")]
    public void ThenLocalStackShouldBeRunning()
    {
        _localStackHelper.Should().NotBeNull("LocalStack helper should be created");
        _localStackHelper!.IsRunning.Should().BeTrue("LocalStack container should be running");
        
        _logger.LogInformation("Verified LocalStack is running");
    }

    [Then(@"LocalStack health check should pass")]
    public async Task ThenLocalStackHealthCheckShouldPass()
    {
        _localStackHelper.Should().NotBeNull("LocalStack helper should be created");
        
        var isHealthy = await _localStackHelper!.IsHealthyAsync();
        isHealthy.Should().BeTrue("LocalStack health check should pass");
        
        _logger.LogInformation("Verified LocalStack health check passes");
        
        // Additional verification: try to make a direct HTTP request to LocalStack
        var endpointUrl = _localStackHelper.EndpointUrl;
        _logger.LogInformation($"Testing direct HTTP access to LocalStack at: {endpointUrl}");
        
        try
        {
            using var httpClient = new HttpClient();
            var healthResponse = await httpClient.GetAsync($"{endpointUrl}/_localstack/health");
            _logger.LogInformation($"Direct health check status: {healthResponse.StatusCode}");
            
            if (healthResponse.IsSuccessStatusCode)
            {
                var healthContent = await healthResponse.Content.ReadAsStringAsync();
                _logger.LogInformation($"Health response: {healthContent}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to make direct HTTP request to LocalStack");
        }
    }

    [Then(@"I should be able to create a test parameter")]
    public async Task ThenIShouldBeAbleToCreateATestParameter()
    {
        _localStackHelper.Should().NotBeNull("LocalStack helper should be created");
        
        _logger.LogInformation($"LocalStack endpoint: {_localStackHelper!.EndpointUrl}");
        _logger.LogInformation($"LocalStack mapped port: {_localStackHelper.GetMappedPort()}");
        
        _ssmClient = _localStackHelper!.CreateParameterStoreClient();
        _ssmClient.Should().NotBeNull("Parameter Store client should be created");
        
        // Log the actual client configuration to debug the issue
        _logger.LogInformation($"Client ServiceURL: {_ssmClient.Config.ServiceURL}");
        _logger.LogInformation($"Client RegionEndpoint: {_ssmClient.Config.RegionEndpoint}");
        _logger.LogInformation($"Client UseHttp: {_ssmClient.Config.UseHttp}");
        
        var parameterName = "/test/simple/parameter";
        _testParameterValue = "test-value-123";
        
        _logger.LogInformation($"Creating test parameter: {parameterName} = {_testParameterValue}");
        
        try
        {
            await _ssmClient.PutParameterAsync(new PutParameterRequest
            {
                Name = parameterName,
                Value = _testParameterValue,
                Type = ParameterType.String,
                Overwrite = true
            });
            
            _logger.LogInformation("Successfully created test parameter");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create test parameter");
            throw;
        }
    }

    [Then(@"I should be able to read the test parameter back")]
    public async Task ThenIShouldBeAbleToReadTheTestParameterBack()
    {
        _ssmClient.Should().NotBeNull("Parameter Store client should be available");
        _testParameterValue.Should().NotBeNullOrEmpty("Test parameter value should be set");
        
        var parameterName = "/test/simple/parameter";
        
        _logger.LogInformation($"Reading test parameter: {parameterName}");
        
        var response = await _ssmClient!.GetParameterAsync(new GetParameterRequest
        {
            Name = parameterName
        });
        
        response.Should().NotBeNull("Get parameter response should not be null");
        response.Parameter.Should().NotBeNull("Parameter should not be null");
        response.Parameter.Value.Should().Be(_testParameterValue, "Parameter value should match what was stored");
        
        _logger.LogInformation($"Successfully read parameter value: {response.Parameter.Value}");
    }
}