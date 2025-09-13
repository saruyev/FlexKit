using BenchmarkDotNet.Attributes;
using FlexKit.Configuration.Core;
using FlexKit.Configuration.Providers.Aws.Extensions;
using FlexKit.Configuration.Providers.Aws.Sources;
using FlexKit.Configuration.Conversion;
using Microsoft.Extensions.Configuration;
using Amazon.Extensions.NETCore.Setup;
using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Amazon.Runtime;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
// ReSharper disable ComplexConditionExpression
// ReSharper disable ClassTooBig
// ReSharper disable MethodTooLong
// ReSharper disable NullableWarningSuppressionIsUsed

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
        _testSecretName = "flexkit_test_secret";

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

    private IConfiguration? _standardConfig;
    private FlexConfiguration? _flexConfig;
    private IFlexConfig? _flexConfigFromBuilder;
    private IFlexConfig? _flexConfigWithJson;

    [IterationSetup]
    public void IterationSetup()
    {
        // Setup configs for access pattern benchmarks
        if (_standardConfig == null)
        {
            var builder = new ConfigurationBuilder();
            builder.Add(new AwsSecretsManagerConfigurationSource
            {
                SecretNames = [_testSecretName],
                Optional = false,
                AwsOptions = _localstackOptions
            });
            _standardConfig = builder.Build();
        }

        _flexConfig ??= new FlexConfiguration(_standardConfig);

        _flexConfigFromBuilder ??= new FlexConfigurationBuilder()
            .AddAwsSecretsManager(options =>
            {
                options.SecretNames = [_testSecretName];
                options.Optional = false;
                options.AwsOptions = _localstackOptions;
            })
            .Build();

        _flexConfigWithJson ??= new FlexConfigurationBuilder()
            .AddAwsSecretsManager(options =>
            {
                options.SecretNames = [_testSecretName];
                options.Optional = false;
                options.JsonProcessor = true;
                options.AwsOptions = _localstackOptions;
            })
            .Build();
    }

    // === Secret Access Pattern Benchmarks ===

    [Benchmark]
    public string? StandardConfigurationSecretAccess()
    {
        return _standardConfig![_testSecretName];
    }

    [Benchmark]
    public string? FlexConfigurationIndexerSecretAccess()
    {
        return _flexConfig![_testSecretName];
    }

    [Benchmark]
    public string? FlexConfigurationDynamicSecretAccess()
    {
        dynamic config = _flexConfig!;
        return config.flexkit_test_secret;
    }

    [Benchmark]
    public string? FlexConfigurationBuilderSecretAccess()
    {
        return _flexConfigFromBuilder![_testSecretName];
    }

    // === JSON Processing Performance Tests ===

    [Benchmark]
    public string? SecretAccessWithoutJsonProcessing()
    {
        return _flexConfigFromBuilder![_testSecretName];
    }

    [Benchmark]
    public string? SecretAccessWithJsonProcessing()
    {
        return _flexConfigWithJson![_testSecretName];
    }

    // === Type Conversion Performance Tests ===

    [Benchmark]
    public int StandardConfigurationIntParsing()
    {
        // Use a fallback since the actual config contains JSON, not a simple number
        var value = _standardConfig![_testSecretName];
        // If it's not a simple number, use a test value
        if (value == null || value.StartsWith("{"))
        {
            value = "123";
        }
        return int.Parse(value);
    }

    [Benchmark]
    public int FlexConfigurationToTypeInt()
    {
        // Use a fallback since the actual config contains JSON, not a simple number
        var value = _flexConfig![_testSecretName];
        // If it's not a simple number, use a test value
        if (value == null || value.StartsWith("{"))
        {
            value = "123";
        }
        return value.ToType<int>();
    }

    [Benchmark]
    public int DynamicAccessWithTypeConversion()
    {
        dynamic config = _flexConfig!;
        string? value = config.flexkit_test_secret;
        // If it's not a simple number, use a test value
        if (value == null || value.StartsWith("{"))
        {
            value = "123";
        }
        return value.ToType<int>();
    }

    // === Multiple Secret Access Scenarios ===

    [Benchmark]
    public (string?, string?, string?) MultipleStandardConfigurationAccess()
    {
        var config1 = _standardConfig![_testSecretName];
        var config2 = _standardConfig[_testSecretName];
        var config3 = _standardConfig[_testSecretName];
        return (config1, config2, config3);
    }

    [Benchmark]
    public (string?, string?, string?) MultipleFlexConfigurationAccess()
    {
        var config1 = _flexConfig![_testSecretName];
        var config2 = _flexConfig[_testSecretName];
        var config3 = _flexConfig[_testSecretName];
        return (config1, config2, config3);
    }

    [Benchmark]
    public (string?, string?, string?) MultipleDynamicAccess()
    {
        dynamic config = _flexConfig!;
        var config1 = (string?)config.flexkit_test_secret;
        var config2 = (string?)config.flexkit_test_secret;
        var config3 = (string?)config.flexkit_test_secret;
        return (config1, config2, config3);
    }

    // === Deep Configuration Navigation ===

    [Benchmark]
    public string? DeepStandardConfigurationAccess()
    {
        return _standardConfig![_testSecretName];
    }

    [Benchmark]
    public string? DeepFlexConfigurationDynamicAccess()
    {
        dynamic config = _flexConfig!;
        return config.flexkit_test_secret;
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