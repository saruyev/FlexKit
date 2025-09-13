using BenchmarkDotNet.Attributes;
using FlexKit.Configuration.Core;
using FlexKit.Configuration.Providers.Aws.Extensions;
using FlexKit.Configuration.Providers.Aws.Sources;
using FlexKit.Configuration.Conversion;
using Microsoft.Extensions.Configuration;
using Amazon.Extensions.NETCore.Setup;
using Amazon;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using Amazon.Runtime;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
// ReSharper disable ClassTooBig
// ReSharper disable ComplexConditionExpression
// ReSharper disable MethodTooLong
// ReSharper disable NullableWarningSuppressionIsUsed

namespace FlexKit.Configuration.Providers.Aws.PerformanceTests.Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
public class AwsParameterStoreBenchmarks
{
    private const int LocalStackPort = 4566;
    private const string LocalStackImage = "localstack/localstack:latest";

    private string _testParameterPath = null!;
    private string _testParameterValue = null!;
    private AWSOptions _localstackOptions = null!;
    private IAmazonSimpleSystemsManagement _ssmClient = null!;
    private IContainer? _container;

    [GlobalSetup]
    public async Task Setup()
    {
        // Load test parameter value from TestData folder
        var parameterDataPath = Path.Combine("TestData", "secrets-config.json");
        _testParameterValue = await File.ReadAllTextAsync(parameterDataPath);
        _testParameterPath = "/flexkit_test/config";

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

        // Create SSM client for localstack
        var config = new AmazonSimpleSystemsManagementConfig
        {
            RegionEndpoint = RegionEndpoint.USEast1,
            ServiceURL = endpointUrl,
            UseHttp = true,
            MaxErrorRetry = 0,
            Timeout = TimeSpan.FromSeconds(30)
        };

        _ssmClient = new AmazonSimpleSystemsManagementClient(new AnonymousAWSCredentials(), config);

        // Health check - ensure the localstack is running and accessible
        await EnsureLocalstackHealthy();

        // Setup test parameter in a localstack
        await SetupTestParameter();

        // Update AWS options with the correct endpoint for the benchmarks
        _localstackOptions.DefaultClientConfig.ServiceURL = endpointUrl;
        _localstackOptions.DefaultClientConfig.UseHttp = true;
    }

    private async Task StartLocalstackAsync()
    {
        _container = new ContainerBuilder()
            .WithImage(LocalStackImage)
            .WithPortBinding(LocalStackPort, true)
            .WithEnvironment("SERVICES", "ssm")
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
            // Try to get parameters to verify connectivity
            await _ssmClient.GetParametersByPathAsync(new GetParametersByPathRequest
            {
                Path = "/",
                MaxResults = 1
            });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Localstack health check failed. Ensure localstack is running", ex);
        }
    }

    private async Task SetupTestParameter()
    {
        try
        {
            // Delete existing parameter if it exists
            try
            {
                await _ssmClient.DeleteParameterAsync(new DeleteParameterRequest
                {
                    Name = _testParameterPath
                });
            }
            catch (ParameterNotFoundException)
            {
                // Parameter doesn't exist, which is fine
            }

            // Create the test parameter
            await _ssmClient.PutParameterAsync(new PutParameterRequest
            {
                Name = _testParameterPath,
                Value = _testParameterValue,
                Type = ParameterType.String,
                Description = "FlexKit benchmark test parameter"
            });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to setup test parameter '{_testParameterPath}' in localstack", ex);
        }
    }

    [Benchmark(Baseline = true)]
    public IConfiguration LoadParameterStoreConfiguration()
    {
        var builder = new ConfigurationBuilder();
        builder.Add(new AwsParameterStoreConfigurationSource
        {
            Path = "/flexkit_test/",
            Optional = false,
            AwsOptions = _localstackOptions
        });
        return builder.Build();
    }

    [Benchmark]
    public FlexConfiguration LoadParameterStoreFlexConfiguration()
    {
        var builder = new ConfigurationBuilder();
        builder.Add(new AwsParameterStoreConfigurationSource
        {
            Path = "/flexkit_test/",
            Optional = false,
            AwsOptions = _localstackOptions
        });
        var standardConfig = builder.Build();
        return new FlexConfiguration(standardConfig);
    }

    [Benchmark]
    public IFlexConfig LoadParameterStoreWithFlexConfigurationBuilder()
    {
        return new FlexConfigurationBuilder()
            .AddAwsParameterStore(options =>
            {
                options.Path = "/flexkit_test/";
                options.Optional = false;
                options.AwsOptions = _localstackOptions;
            })
            .Build();
    }

    [Benchmark]
    public IFlexConfig LoadParameterStoreWithJsonProcessing()
    {
        return new FlexConfigurationBuilder()
            .AddAwsParameterStore(options =>
            {
                options.Path = "/flexkit_test/";
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
            builder.Add(new AwsParameterStoreConfigurationSource
            {
                Path = "/flexkit_test/",
                Optional = false,
                AwsOptions = _localstackOptions
            });
            _standardConfig = builder.Build();
        }

        _flexConfig ??= new FlexConfiguration(_standardConfig);

        _flexConfigFromBuilder ??= new FlexConfigurationBuilder()
            .AddAwsParameterStore(options =>
            {
                options.Path = "/flexkit_test/";
                options.Optional = false;
                options.AwsOptions = _localstackOptions;
            })
            .Build();

        _flexConfigWithJson ??= new FlexConfigurationBuilder()
            .AddAwsParameterStore(options =>
            {
                options.Path = "/flexkit_test/";
                options.Optional = false;
                options.JsonProcessor = true;
                options.AwsOptions = _localstackOptions;
            })
            .Build();
    }

    // === Parameter Access Pattern Benchmarks ===

    [Benchmark]
    public string? StandardConfigurationParameterAccess()
    {
        return _standardConfig!["config"];
    }

    [Benchmark]
    public string? FlexConfigurationIndexerParameterAccess()
    {
        return _flexConfig!["config"];
    }

    [Benchmark]
    public string? FlexConfigurationDynamicParameterAccess()
    {
        dynamic config = _flexConfig!;
        return config.config;
    }

    [Benchmark]
    public string? FlexConfigurationBuilderParameterAccess()
    {
        return _flexConfigFromBuilder!["config"];
    }

    // === JSON Processing Performance Tests ===

    [Benchmark]
    public string? ParameterAccessWithoutJsonProcessing()
    {
        return _flexConfigFromBuilder!["config"];
    }

    [Benchmark]
    public string? ParameterAccessWithJsonProcessing()
    {
        return _flexConfigWithJson!["config"];
    }

    // === Type Conversion Performance Tests ===

    [Benchmark]
    public int StandardConfigurationIntParsing()
    {
        // Use a fallback since the actual config contains JSON, not a simple number
        var value = _standardConfig!["config"];
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
        var value = _flexConfig!["config"];
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
        string? value = config.config;
        // If it's not a simple number, use a test value
        if (value == null || value.StartsWith("{"))
        {
            value = "123";
        }
        return value.ToType<int>();
    }

    // === Multiple Parameter Access Scenarios ===

    [Benchmark]
    public (string?, string?, string?) MultipleStandardConfigurationAccess()
    {
        var config1 = _standardConfig!["config"];
        var config2 = _standardConfig["config"];
        var config3 = _standardConfig["config"];
        return (config1, config2, config3);
    }

    [Benchmark]
    public (string?, string?, string?) MultipleFlexConfigurationAccess()
    {
        var config1 = _flexConfig!["config"];
        var config2 = _flexConfig["config"];
        var config3 = _flexConfig["config"];
        return (config1, config2, config3);
    }

    [Benchmark]
    public (string?, string?, string?) MultipleDynamicAccess()
    {
        dynamic config = _flexConfig!;
        var config1 = (string?)config.config;
        var config2 = (string?)config.config;
        var config3 = (string?)config.config;
        return (config1, config2, config3);
    }

    // === Deep Configuration Navigation ===

    [Benchmark]
    public string? DeepStandardConfigurationAccess()
    {
        return _standardConfig!["config"];
    }

    [Benchmark]
    public string? DeepFlexConfigurationDynamicAccess()
    {
        dynamic config = _flexConfig!;
        return config.config;
    }

    [GlobalCleanup]
    public async Task Cleanup()
    {
        try
        {
            await _ssmClient.DeleteParameterAsync(new DeleteParameterRequest
            {
                Name = _testParameterPath
            });
        }
        catch (Exception)
        {
            // Ignore cleanup errors
        }
        finally
        {
            _ssmClient.Dispose();

            // Stop and dispose container
            if (_container != null)
            {
                await _container.StopAsync();
                await _container.DisposeAsync();
            }
        }
    }
}