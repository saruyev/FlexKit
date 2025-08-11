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
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Amazon.Runtime;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using ResourceNotFoundException = Amazon.SecretsManager.Model.ResourceNotFoundException;
// ReSharper disable TooManyDeclarations
// ReSharper disable NullableWarningSuppressionIsUsed

// ReSharper disable ClassTooBig
// ReSharper disable ComplexConditionExpression
// ReSharper disable MethodTooLong

namespace FlexKit.Configuration.Providers.Aws.PerformanceTests.Benchmarks;

/// <summary>
/// Performance benchmarks for mixed AWS configuration loading scenarios.
/// Tests the performance characteristics of combining Parameter Store and Secrets Manager
/// using LocalStack to simulate realistic multi-source AWS configuration patterns.
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class AwsConfigurationLoadingBenchmarks
{
    private const int LocalStackPort = 4566;
    private const string LocalStackImage = "localstack/localstack:latest";
    
    private string _simpleConfigData = null!;
    private string _complexConfigData = null!;
    private string _largeConfigData = null!;
    
    private const string SimpleParameterPath = "/flexkit_test/simple_config";
    private const string ComplexParameterPath = "/flexkit_test/complex_config";
    private const string LargeParameterPath = "/flexkit_test/large_config";
    private const string OverlapParameterPath = "/flexkit_test/overlap_config";
    
    private const string SimpleSecretName = "flexkit_test_simple_secret";
    private const string ComplexSecretName = "flexkit_test_complex_secret";
    private const string LargeSecretName = "flexkit_test_large_secret";
    private const string OverlapSecretName = "flexkit_test_overlap_secret";
    
    private AWSOptions _localstackOptions = null!;
    private IAmazonSimpleSystemsManagement _ssmClient = null!;
    private IAmazonSecretsManager _secretsManagerClient = null!;
    private IContainer? _container;

    [GlobalSetup]
    public async Task Setup()
    {
        // Load test data from existing TestData files
        _simpleConfigData = await File.ReadAllTextAsync(Path.Combine("TestData", "secrets-config.json"));
        _complexConfigData = await File.ReadAllTextAsync(Path.Combine("TestData", "complex-secret.json"));
        _largeConfigData = await File.ReadAllTextAsync(Path.Combine("TestData", "large-secret.json"));

        // Start a LocalStack container with both SSM and Secrets Manager
        await StartLocalstackAsync();

        // Setup LocalStack connection options
        var mappedPort = _container!.GetMappedPublicPort(LocalStackPort);
        var endpointUrl = $"http://localhost:{mappedPort}";
        
        _localstackOptions = new AWSOptions
        {
            Credentials = new AnonymousAWSCredentials(),
            Region = RegionEndpoint.USEast1
        };

        // Create AWS clients for LocalStack
        CreateAwsClients(endpointUrl);

        // Health check - ensure LocalStack is running and accessible
        await EnsureLocalstackHealthy();

        // Setup test data in both Parameter Store and Secrets Manager
        await SetupTestData();
        
        // Update AWS options with the correct endpoint for the benchmarks
        _localstackOptions.DefaultClientConfig.ServiceURL = endpointUrl;
        _localstackOptions.DefaultClientConfig.UseHttp = true;
    }

    private async Task StartLocalstackAsync()
    {
        _container = new ContainerBuilder()
            .WithImage(LocalStackImage)
            .WithPortBinding(LocalStackPort, true)
            .WithEnvironment("SERVICES", "ssm,secretsmanager")
            .WithEnvironment("DEBUG", "1")
            .WithEnvironment("LOCALSTACK_HOST", "localhost")
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilHttpRequestIsSucceeded(request => request
                    .ForPort(LocalStackPort)
                    .ForPath("/_localstack/health")
                    .ForStatusCode(System.Net.HttpStatusCode.OK)))
            .Build();

        await _container.StartAsync();
        
        // Wait for services to be ready
        await Task.Delay(3000);
    }

    private void CreateAwsClients(string endpointUrl)
    {
        var ssmConfig = new AmazonSimpleSystemsManagementConfig
        {
            RegionEndpoint = RegionEndpoint.USEast1,
            ServiceURL = endpointUrl,
            UseHttp = true,
            MaxErrorRetry = 0,
            Timeout = TimeSpan.FromSeconds(30)
        };
        
        var secretsConfig = new AmazonSecretsManagerConfig
        {
            RegionEndpoint = RegionEndpoint.USEast1,
            ServiceURL = endpointUrl,
            UseHttp = true,
            MaxErrorRetry = 0,
            Timeout = TimeSpan.FromSeconds(30)
        };
        
        _ssmClient = new AmazonSimpleSystemsManagementClient(new AnonymousAWSCredentials(), ssmConfig);
        _secretsManagerClient = new AmazonSecretsManagerClient(new AnonymousAWSCredentials(), secretsConfig);
    }

    private async Task EnsureLocalstackHealthy()
    {
        try
        {
            // Test SSM connectivity
            await _ssmClient.GetParametersByPathAsync(new GetParametersByPathRequest 
            { 
                Path = "/", 
                MaxResults = 1 
            });
            
            // Test Secrets Manager connectivity
            await _secretsManagerClient.ListSecretsAsync(new ListSecretsRequest { MaxResults = 1 });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "LocalStack health check failed. Ensure LocalStack is running with SSM and Secrets Manager", ex);
        }
    }

    private async Task SetupTestData()
    {
        try
        {
            // Setup Parameters in SSM
            await SetupParameter(SimpleParameterPath, _simpleConfigData);
            await SetupParameter(ComplexParameterPath, _complexConfigData);
            await SetupParameter(LargeParameterPath, _largeConfigData);
            await SetupParameter(OverlapParameterPath, _simpleConfigData); // Same data for precedence testing
            
            // Setup Secrets in Secrets Manager
            await SetupSecret(SimpleSecretName, _simpleConfigData);
            await SetupSecret(ComplexSecretName, _complexConfigData);
            await SetupSecret(LargeSecretName, _largeConfigData);
            await SetupSecret(OverlapSecretName, _simpleConfigData); // Same data for precedence testing
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to setup test data in LocalStack", ex);
        }
    }

    private async Task SetupParameter(string parameterName, string value)
    {
        try
        {
            await _ssmClient.DeleteParameterAsync(new DeleteParameterRequest { Name = parameterName });
        }
        catch (ParameterNotFoundException)
        {
            // Parameter doesn't exist, which is fine
        }

        await _ssmClient.PutParameterAsync(new PutParameterRequest
        {
            Name = parameterName,
            Value = value,
            Type = ParameterType.String,
            Description = "FlexKit mixed benchmark test parameter"
        });
    }

    private async Task SetupSecret(string secretName, string value)
    {
        try
        {
            await _secretsManagerClient.DeleteSecretAsync(new DeleteSecretRequest
            {
                SecretId = secretName,
                ForceDeleteWithoutRecovery = true
            });
        }
        catch (ResourceNotFoundException)
        {
            // Secret doesn't exist, which is fine
        }

        await _secretsManagerClient.CreateSecretAsync(new CreateSecretRequest
        {
            Name = secretName,
            SecretString = value,
            Description = "FlexKit mixed benchmark test secret"
        });
    }

    // === Mixed Source Loading Benchmarks ===

    [Benchmark(Baseline = true)]
    public IConfiguration LoadSimpleMixedSourcesConfiguration()
    {
        var builder = new ConfigurationBuilder();
        
        // Add Parameter Store
        builder.Add(new AwsParameterStoreConfigurationSource
        {
            Path = "/flexkit_test/",
            Optional = false,
            AwsOptions = _localstackOptions
        });
        
        // Add Secrets Manager
        builder.Add(new AwsSecretsManagerConfigurationSource
        {
            SecretNames = [SimpleSecretName],
            Optional = false,
            AwsOptions = _localstackOptions
        });
        
        return builder.Build();
    }

    [Benchmark]
    public FlexConfiguration LoadSimpleMixedSourcesFlexConfiguration()
    {
        var builder = new ConfigurationBuilder();
        
        builder.Add(new AwsParameterStoreConfigurationSource
        {
            Path = "/flexkit_test/",
            Optional = false,
            AwsOptions = _localstackOptions
        });
        
        builder.Add(new AwsSecretsManagerConfigurationSource
        {
            SecretNames = [SimpleSecretName],
            Optional = false,
            AwsOptions = _localstackOptions
        });
        
        var standardConfig = builder.Build();
        return new FlexConfiguration(standardConfig);
    }

    [Benchmark]
    public IFlexConfig LoadSimpleMixedSourcesWithFlexConfigurationBuilder()
    {
        return new FlexConfigurationBuilder()
            .AddAwsParameterStore(options =>
            {
                options.Path = "/flexkit_test/";
                options.Optional = false;
                options.AwsOptions = _localstackOptions;
            })
            .AddAwsSecretsManager(options =>
            {
                options.SecretNames = [SimpleSecretName];
                options.Optional = false;
                options.AwsOptions = _localstackOptions;
            })
            .Build();
    }

    // === Complex Mixed Sources Benchmarks ===

    [Benchmark]
    public IFlexConfig LoadComplexMixedSourcesWithJsonProcessing()
    {
        return new FlexConfigurationBuilder()
            .AddAwsParameterStore(options =>
            {
                options.Path = "/flexkit_test/";
                options.Optional = false;
                options.JsonProcessor = true;
                options.AwsOptions = _localstackOptions;
            })
            .AddAwsSecretsManager(options =>
            {
                options.SecretNames = [ComplexSecretName];
                options.Optional = false;
                options.JsonProcessor = true;
                options.AwsOptions = _localstackOptions;
            })
            .Build();
    }

    [Benchmark]
    public IFlexConfig LoadLargeMixedSourcesConfiguration()
    {
        return new FlexConfigurationBuilder()
            .AddAwsParameterStore(options =>
            {
                options.Path = "/flexkit_test/";
                options.Optional = false;
                options.JsonProcessor = true;
                options.AwsOptions = _localstackOptions;
            })
            .AddAwsSecretsManager(options =>
            {
                options.SecretNames = [LargeSecretName];
                options.Optional = false;
                options.JsonProcessor = true;
                options.AwsOptions = _localstackOptions;
            })
            .Build();
    }

    // === Configuration Precedence Benchmarks ===

    [Benchmark]
    public IFlexConfig LoadConfigurationWithParameterStorePrecedence()
    {
        return new FlexConfigurationBuilder()
            .AddAwsSecretsManager(options =>
            {
                options.SecretNames = [OverlapSecretName];
                options.Optional = false;
                options.AwsOptions = _localstackOptions;
            })
            .AddAwsParameterStore(options =>
            {
                options.Path = "/flexkit_test/";
                options.Optional = false;
                options.AwsOptions = _localstackOptions; // Parameters override secrets
            })
            .Build();
    }

    [Benchmark]
    public IFlexConfig LoadConfigurationWithSecretsManagerPrecedence()
    {
        return new FlexConfigurationBuilder()
            .AddAwsParameterStore(options =>
            {
                options.Path = "/flexkit_test/";
                options.Optional = false;
                options.AwsOptions = _localstackOptions;
            })
            .AddAwsSecretsManager(options =>
            {
                options.SecretNames = [OverlapSecretName];
                options.Optional = false;
                options.AwsOptions = _localstackOptions; // Secrets override parameters
            })
            .Build();
    }

    // === Multiple Sources of the Same Type Benchmarks ===

    [Benchmark]
    public IFlexConfig LoadMultipleSecretSources()
    {
        return new FlexConfigurationBuilder()
            .AddAwsSecretsManager(options =>
            {
                options.SecretNames = [SimpleSecretName];
                options.Optional = false;
                options.JsonProcessor = true;
                options.AwsOptions = _localstackOptions;
            })
            .AddAwsSecretsManager(options =>
            {
                options.SecretNames = [ComplexSecretName];
                options.Optional = false;
                options.JsonProcessor = true;
                options.AwsOptions = _localstackOptions;
            })
            .AddAwsSecretsManager(options =>
            {
                options.SecretNames = [LargeSecretName];
                options.Optional = false;
                options.JsonProcessor = true;
                options.AwsOptions = _localstackOptions;
            })
            .Build();
    }

    [Benchmark]
    public IFlexConfig LoadMultipleParameterPaths()
    {
        return new FlexConfigurationBuilder()
            .AddAwsParameterStore(options =>
            {
                options.Path = "/flexkit_test/simple/";
                options.Optional = false;
                options.JsonProcessor = true;
                options.AwsOptions = _localstackOptions;
            })
            .AddAwsParameterStore(options =>
            {
                options.Path = "/flexkit_test/complex/";
                options.Optional = false;
                options.JsonProcessor = true;
                options.AwsOptions = _localstackOptions;
            })
            .Build();
    }

    // === Mixed Configuration Access Pattern Benchmarks ===

    private IFlexConfig? _mixedConfig;
    private IFlexConfig? _complexMixedConfig;

    [IterationSetup]
    public void IterationSetup()
    {
        _mixedConfig ??= new FlexConfigurationBuilder()
            .AddAwsParameterStore(options =>
            {
                options.Path = "/flexkit_test/";
                options.Optional = false;
                options.JsonProcessor = true;
                options.AwsOptions = _localstackOptions;
            })
            .AddAwsSecretsManager(options =>
            {
                options.SecretNames = [SimpleSecretName];
                options.Optional = false;
                options.JsonProcessor = true;
                options.AwsOptions = _localstackOptions;
            })
            .Build();

        _complexMixedConfig ??= new FlexConfigurationBuilder()
            .AddAwsParameterStore(options =>
            {
                options.Path = "/flexkit_test/";
                options.Optional = false;
                options.JsonProcessor = true;
                options.AwsOptions = _localstackOptions;
            })
            .AddAwsSecretsManager(options =>
            {
                options.SecretNames = [ComplexSecretName];
                options.Optional = false;
                options.JsonProcessor = true;
                options.AwsOptions = _localstackOptions;
            })
            .Build();
    }

    [Benchmark]
    public string? MixedSourceDirectAccess()
    {
        // Access parameter from Parameter Store
        return _mixedConfig!["simple_config"];
    }

    [Benchmark]
    public string? MixedSourceSecretAccess()
    {
        // Access secret from Secrets Manager
        return _mixedConfig![SimpleSecretName];
    }

    [Benchmark]
    public string? MixedSourceDynamicParameterAccess()
    {
        dynamic config = _mixedConfig!;
        return config.Application?.Name;
    }

    [Benchmark]
    public string? MixedSourceDynamicSecretAccess()
    {
        dynamic config = _mixedConfig!;
        return config.flexkit_test_simple_secret?.Application?.Name;
    }

    [Benchmark]
    public string? ComplexMixedSourceDeepAccess()
    {
        dynamic config = _complexMixedConfig!;
        return config.flexkit_test_complex_secret?.database?.primary?.host;
    }

    // === Memory Allocation Pattern Benchmarks ===

    [Benchmark]
    public object MultipleMixedConfigurationBuilds()
    {
        var configs = new List<IFlexConfig>();
        
        // Simulate building multiple mixed configurations (e.g., per-tenant scenarios)
        for (int i = 0; i < 5; i++)
        {
            var config = new FlexConfigurationBuilder()
                .AddAwsParameterStore(options =>
                {
                    options.Path = "/flexkit_test/";
                    options.Optional = false;
                    options.AwsOptions = _localstackOptions;
                })
                .AddAwsSecretsManager(options =>
                {
                    options.SecretNames = [SimpleSecretName];
                    options.Optional = false;
                    options.AwsOptions = _localstackOptions;
                })
                .Build();
            configs.Add(config);
        }
        
        return configs;
    }

    [Benchmark]
    public object SingleMixedConfigurationMultipleAccess()
    {
        var results = new List<string?>();
        
        // Simulate multiple configuration accesses from mixed sources
        for (int i = 0; i < 50; i++)
        {
            results.Add(_mixedConfig!["simple_config"]);
            results.Add(_mixedConfig[SimpleSecretName]);
            
            dynamic config = _mixedConfig;
            results.Add(config.Application?.Name);
        }
        
        return results;
    }

    // === Mixed Source Type Conversion Benchmarks ===

    [Benchmark]
    public int MixedSourceTypeConversionFromParameter()
    {
        // Convert timeout value from parameter to int
        // When JSON processing is enabled, nested values become flattened keys
        var timeoutValue = _mixedConfig!["simple_config:Database:Timeout"] ?? "30";
        return timeoutValue.ToType<int>();
    }

    [Benchmark]
    public bool MixedSourceTypeConversionFromSecret()
    {
        // Convert boolean value from secret
        // When JSON processing is enabled, nested values become flattened keys
        var enabledValue = _mixedConfig![SimpleSecretName + ":Features:SecretFeature"] ?? "true";
        return enabledValue.ToType<bool>();
    }

    [Benchmark]
    public (string?, int, bool) MixedSourceMultipleTypeConversions()
    {
        // Access and convert multiple values from different sources using flattened keys
        var appName = _mixedConfig!["simple_config:Application:Name"];
        
        var timeoutValue = _mixedConfig!["simple_config:Database:Timeout"] ?? "30";
        var timeout = timeoutValue.ToType<int>();
        
        var cacheEnabledValue = _mixedConfig![SimpleSecretName + ":Features:CacheEnabled"] ?? "true";
        var cacheEnabled = cacheEnabledValue.ToType<bool>();
        
        return (appName, timeout, cacheEnabled);
    }

    [GlobalCleanup]
    public async Task Cleanup()
    {
        try
        {
            // Cleanup parameters
            await CleanupParameter(SimpleParameterPath);
            await CleanupParameter(ComplexParameterPath);
            await CleanupParameter(LargeParameterPath);
            await CleanupParameter(OverlapParameterPath);
            
            // Cleanup secrets
            await CleanupSecret(SimpleSecretName);
            await CleanupSecret(ComplexSecretName);
            await CleanupSecret(LargeSecretName);
            await CleanupSecret(OverlapSecretName);
        }
        catch (Exception)
        {
            // Ignore cleanup errors
        }
        finally
        {
            _ssmClient.Dispose();
            _secretsManagerClient.Dispose();
            
            // Stop and dispose container
            if (_container != null)
            {
                await _container.StopAsync();
                await _container.DisposeAsync();
            }
        }
    }

    private async Task CleanupParameter(string parameterName)
    {
        try
        {
            await _ssmClient.DeleteParameterAsync(new DeleteParameterRequest { Name = parameterName });
        }
        catch (Exception)
        {
            // Ignore cleanup errors
        }
    }

    private async Task CleanupSecret(string secretName)
    {
        try
        {
            await _secretsManagerClient.DeleteSecretAsync(new DeleteSecretRequest
            {
                SecretId = secretName,
                ForceDeleteWithoutRecovery = true
            });
        }
        catch (Exception)
        {
            // Ignore cleanup errors
        }
    }
}
