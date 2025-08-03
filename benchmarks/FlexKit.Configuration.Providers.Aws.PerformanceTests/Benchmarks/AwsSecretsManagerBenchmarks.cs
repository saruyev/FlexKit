using BenchmarkDotNet.Attributes;
using FlexKit.Configuration.Core;
using FlexKit.Configuration.Providers.Aws.Extensions;
using FlexKit.Configuration.Providers.Aws.Sources;
using Microsoft.Extensions.Configuration;
using Amazon.Extensions.NETCore.Setup;
using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Amazon.Runtime;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace FlexKit.Configuration.Providers.Aws.PerformanceTests.Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
public class AwsSecretsManagerBenchmarks
{
    private const int LocalStackPort = 4566;
    private const string LocalStackImage = "localstack/localstack:latest";
    
    private string _testSecretName = null!;
    private string _testSecretValue = null!;
    private AWSOptions _localstackOptions = null!;
    private IAmazonSecretsManager _secretsManagerClient = null!;
    private IContainer? _container;

    [GlobalSetup]
    public async Task Setup()
    {
        // Load test secret value from TestData folder
        var secretDataPath = Path.Combine("TestData", "secrets-config.json");
        _testSecretValue = await File.ReadAllTextAsync(secretDataPath);
        _testSecretName = "flexkit-test-secret";

        // Start a localstack container
        await StartLocalstackAsync();

        // Setup localstack connection options
        var mappedPort = _container!.GetMappedPublicPort(LocalStackPort);
        var endpointUrl = $"http://localhost:{mappedPort}";
        
        _localstackOptions = new AWSOptions
        {
            Credentials = new AnonymousAWSCredentials(),
            Region = RegionEndpoint.USEast1
        };

        // Create Secrets Manager client for localstack
        var config = new AmazonSecretsManagerConfig
        {
            RegionEndpoint = RegionEndpoint.USEast1,
            ServiceURL = endpointUrl,
            UseHttp = true,
            MaxErrorRetry = 0,
            Timeout = TimeSpan.FromSeconds(30)
        };
        
        _secretsManagerClient = new AmazonSecretsManagerClient(new AnonymousAWSCredentials(), config);

        // Health check - ensure the localstack is running and accessible
        await EnsureLocalstackHealthy();

        // Setup test secret in a localstack
        await SetupTestSecret();
        
        // Update AWS options with the correct endpoint for the benchmarks
        _localstackOptions.DefaultClientConfig.ServiceURL = endpointUrl;
        _localstackOptions.DefaultClientConfig.UseHttp = true;
    }

    private async Task StartLocalstackAsync()
    {
        _container = new ContainerBuilder()
            .WithImage(LocalStackImage)
            .WithPortBinding(LocalStackPort, true)
            .WithEnvironment("SERVICES", "secretsmanager")
            .WithEnvironment("DEBUG", "1")
            .WithEnvironment("LOCALSTACK_HOST", "localhost")
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilHttpRequestIsSucceeded(request => request
                    .ForPort(LocalStackPort)
                    .ForPath("/_localstack/health")
                    .ForStatusCode(System.Net.HttpStatusCode.OK)))
            .Build();

        await _container.StartAsync();
        
        // Wait a bit for services to be ready
        await Task.Delay(2000);
    }

    private async Task EnsureLocalstackHealthy()
    {
        try
        {
            // Try to list secrets to verify connectivity
            await _secretsManagerClient.ListSecretsAsync(new ListSecretsRequest { MaxResults = 1 });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Localstack health check failed. Ensure localstack is running", ex);
        }
    }

    private async Task SetupTestSecret()
    {
        try
        {
            // Delete an existing secret if it exists
            try
            {
                await _secretsManagerClient.DeleteSecretAsync(new DeleteSecretRequest
                {
                    SecretId = _testSecretName,
                    ForceDeleteWithoutRecovery = true
                });
            }
            catch (ResourceNotFoundException)
            {
                // Secret doesn't exist, which is fine
            }

            // Create the test secret
            await _secretsManagerClient.CreateSecretAsync(new CreateSecretRequest
            {
                Name = _testSecretName,
                SecretString = _testSecretValue,
                Description = "FlexKit benchmark test secret"
            });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to setup test secret '{_testSecretName}' in localstack", ex);
        }
    }

    [Benchmark(Baseline = true)]
    public IConfiguration LoadSecretsManagerConfiguration()
    {
        var builder = new ConfigurationBuilder();
        builder.Add(new AwsSecretsManagerConfigurationSource
        {
            SecretNames = [_testSecretName],
            Optional = false,
            AwsOptions = _localstackOptions
        });
        return builder.Build();
    }

    [Benchmark]
    public FlexConfiguration LoadSecretsManagerFlexConfiguration()
    {
        var builder = new ConfigurationBuilder();
        builder.Add(new AwsSecretsManagerConfigurationSource
        {
            SecretNames = [_testSecretName],
            Optional = false,
            AwsOptions = _localstackOptions
        });
        var standardConfig = builder.Build();
        return new FlexConfiguration(standardConfig);
    }

    [Benchmark]
    public IFlexConfig LoadSecretsManagerWithFlexConfigurationBuilder()
    {
        return new FlexConfigurationBuilder()
            .AddAwsSecretsManager(options =>
            {
                options.SecretNames = [_testSecretName];
                options.Optional = false;
                options.AwsOptions = _localstackOptions;
            })
            .Build();
    }

    [Benchmark]
    public IFlexConfig LoadSecretsManagerWithJsonProcessing()
    {
        return new FlexConfigurationBuilder()
            .AddAwsSecretsManager(options =>
            {
                options.SecretNames = [_testSecretName];
                options.Optional = false;
                options.JsonProcessor = true;
                options.AwsOptions = _localstackOptions;
            })
            .Build();
    }

    [GlobalCleanup]
    public async Task Cleanup()
    {
        try
        {
            await _secretsManagerClient.DeleteSecretAsync(new DeleteSecretRequest
            {
                SecretId = _testSecretName,
                ForceDeleteWithoutRecovery = true
            });
        }
        catch (Exception)
        {
            // Ignore cleanup errors
        }
        finally
        {
            _secretsManagerClient.Dispose();
            
            // Stop and dispose container
            if (_container != null)
            {
                await _container.StopAsync();
                await _container.DisposeAsync();
            }
        }
    }
}