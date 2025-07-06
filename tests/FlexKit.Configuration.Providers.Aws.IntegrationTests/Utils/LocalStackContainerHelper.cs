using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
using Amazon.SecretsManager;
using Amazon.SimpleSystemsManagement;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Reqnroll;
using Amazon;
using FlexKit.IntegrationTests.Utils;
using Microsoft.Extensions.Logging;
// ReSharper disable HollowTypeName
// ReSharper disable MethodTooLong

namespace FlexKit.Configuration.Providers.Aws.IntegrationTests.Utils;

/// <summary>
/// Helper class for managing LocalStack containers in AWS integration tests.
/// Provides containerized AWS services for isolated testing without real AWS dependencies.
/// </summary>
public class LocalStackContainerHelper : IDisposable
{
    private const int LocalStackPort = 4566;
    private const string LocalStackImage = "localstack/localstack:latest";
    private const string HealthEndpoint = "/_localstack/health";
    private const string InfrastructureModulePrefix = "infrastructure_module";
    
    private readonly ILogger<LocalStackContainerHelper> _logger;
    private IContainer? _container;
    private INetwork? _network;
    private bool _disposed;

    /// <summary>
    /// Gets the LocalStack container endpoint URL.
    /// </summary>
    public string EndpointUrl => $"http://localhost:{GetMappedPort()}";
    
    /// <summary>
    /// Gets whether the LocalStack container is currently running.
    /// </summary>
    public bool IsRunning => _container?.State == TestcontainersStates.Running;

    /// <summary>
    /// Initializes a new LocalStack container helper.
    /// </summary>
    /// <param name="scenarioContext">Optional scenario context for automatic cleanup</param>
    /// <param name="logger">Logger for diagnostics</param>
    public LocalStackContainerHelper(ScenarioContext? scenarioContext = null, ILogger<LocalStackContainerHelper>? logger = null)
    {
        var scenarioContext1 = scenarioContext;
        _logger = logger ?? CreateDefaultLogger();
        
        // Register for cleanup if scenario context is available - cast to IDisposable
        if (scenarioContext1 != null)
        {
            scenarioContext1.RegisterForCleanup(this);
        }
    }

    /// <summary>
    /// Creates a default logger for LocalStackContainerHelper when none is provided.
    /// </summary>
    /// <returns>A console logger for LocalStackContainerHelper</returns>
    private static ILogger<LocalStackContainerHelper> CreateDefaultLogger()
    {
        return LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<LocalStackContainerHelper>();
    }

    /// <summary>
    /// Starts the LocalStack container with AWS services.
    /// </summary>
    /// <param name="services">Comma-separated list of AWS services to enable (default: ssm,secretsmanager)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task that completes when the container is started and healthy</returns>
    public async Task StartAsync(string services = "ssm,secretsmanager", CancellationToken cancellationToken = default)
    {
        if (IsRunning)
        {
            _logger.LogInformation($"{InfrastructureModulePrefix}: LocalStack container is already running");
            return;
        }

        try
        {
            _logger.LogInformation($"{InfrastructureModulePrefix}: Starting LocalStack container with services: {services}");

            // Create a custom network for better isolation
            _network = new NetworkBuilder()
                .WithName($"{InfrastructureModulePrefix}_localstack_network_{Guid.NewGuid():N}")
                .Build();

            await _network.CreateAsync(cancellationToken);
            _logger.LogInformation($"{InfrastructureModulePrefix}: Created LocalStack network");

            // Build LocalStack container
            _container = new ContainerBuilder()
                .WithImage(LocalStackImage)
                .WithName($"{InfrastructureModulePrefix}_localstack_{Guid.NewGuid():N}")
                .WithPortBinding(LocalStackPort, true) // Use random host port
                .WithNetwork(_network)
                .WithEnvironment("SERVICES", services)
                .WithEnvironment("DEBUG", "1")
                .WithEnvironment("LOCALSTACK_HOST", "localhost")
                .WithEnvironment("EDGE_PORT", LocalStackPort.ToString())
                .WithEnvironment("DATA_DIR", "/tmp/localstack/data")
                .WithEnvironment("SKIP_INFRA_DOWNLOADS", "1") // Skip downloads to speed up startup
                .WithWaitStrategy(Wait.ForUnixContainer()
                    .UntilHttpRequestIsSucceeded(request => request
                        .ForPort(LocalStackPort)
                        .ForPath(HealthEndpoint)
                        .ForStatusCode(System.Net.HttpStatusCode.OK)))
                .WithStartupCallback(async (_, ct) =>
                {
                    _logger.LogInformation($"{InfrastructureModulePrefix}: LocalStack container started, waiting for health check...");
                    await WaitForHealthCheckAsync(ct);
                })
                .Build();

            _logger.LogInformation($"{InfrastructureModulePrefix}: Starting LocalStack container...");
            await _container.StartAsync(cancellationToken);
            
            _logger.LogInformation($"{InfrastructureModulePrefix}: LocalStack container is running at {EndpointUrl}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{InfrastructureModulePrefix}: Failed to start LocalStack container");
            await CleanupAsync();
            throw new InvalidOperationException($"{InfrastructureModulePrefix}: LocalStack container startup failed", ex);
        }
    }

    /// <summary>
    /// Stops the LocalStack container.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_container != null && IsRunning)
        {
            _logger.LogInformation($"{InfrastructureModulePrefix}: Stopping LocalStack container");
            await _container.StopAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Checks if LocalStack health endpoint is accessible.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if a health check passes, false otherwise</returns>
    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        if (!IsRunning)
        {
            return false;
        }

        try
        {
            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync($"{EndpointUrl}{HealthEndpoint}", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"{InfrastructureModulePrefix}: Health check failed for LocalStack");
            return false;
        }
    }

    /// <summary>
    /// Creates AWS options configured for LocalStack.
    /// </summary>
    /// <param name="region">AWS region (default: us-east-1)</param>
    /// <returns>AWSOptions configured for LocalStack</returns>
    public AWSOptions CreateAwsOptions(RegionEndpoint? region = null)
    {
        EnsureRunning();
        
        var awsOptions = new AWSOptions
        {
            Credentials = new AnonymousAWSCredentials(),
            Region = region ?? RegionEndpoint.USEast1
        };

        _logger.LogInformation($"{InfrastructureModulePrefix}: Created AWS options for LocalStack with anonymous credentials and region {awsOptions.Region.SystemName}");
        
        // Service endpoint configuration will be handled per-client
        return awsOptions;
    }

    /// <summary>
    /// Creates a Parameter Store client configured for LocalStack.
    /// </summary>
    /// <param name="region">AWS region (default: us-east-1)</param>
    /// <returns>IAmazonSimpleSystemsManagement client for LocalStack</returns>
    public IAmazonSimpleSystemsManagement CreateParameterStoreClient(RegionEndpoint? region = null)
    {
        EnsureRunning();
        
        var targetRegion = region ?? RegionEndpoint.USEast1;
        var credentials = new AnonymousAWSCredentials();
        
        _logger.LogInformation($"{InfrastructureModulePrefix}: Creating Parameter Store client for LocalStack at {EndpointUrl}");
        
        // Try an explicit configuration approach
        var config = new AmazonSimpleSystemsManagementConfig
        {
            RegionEndpoint = targetRegion,
            ServiceURL = EndpointUrl,
            UseHttp = true,
            MaxErrorRetry = 0, // Disable retries for faster failure
            Timeout = TimeSpan.FromSeconds(30)
        };
        
        _logger.LogInformation($"{InfrastructureModulePrefix}: Config ServiceURL set to: {config.ServiceURL}");
        _logger.LogInformation($"{InfrastructureModulePrefix}: Config UseHttp set to: {config.UseHttp}");
        _logger.LogInformation($"{InfrastructureModulePrefix}: Config Region set to: {config.RegionEndpoint?.SystemName}");
        
        var client = new AmazonSimpleSystemsManagementClient(credentials, config);
        
        // Verify the configuration after client creation
        _logger.LogInformation($"{InfrastructureModulePrefix}: Final client ServiceURL: {client.Config.ServiceURL}");
        
        return client;
    }

    /// <summary>
    /// Creates a Secrets Manager client configured for LocalStack.
    /// </summary>
    /// <param name="region">AWS region (default: us-east-1)</param>
    /// <returns>IAmazonSecretsManager client for LocalStack</returns>
    public IAmazonSecretsManager CreateSecretsManagerClient(RegionEndpoint? region = null)
    {
        EnsureRunning();
        
        var targetRegion = region ?? RegionEndpoint.USEast1;
        var credentials = new AnonymousAWSCredentials();
        
        _logger.LogInformation($"{InfrastructureModulePrefix}: Creating Secrets Manager client for LocalStack at {EndpointUrl}");
        
        // Try an explicit configuration approach
        var config = new AmazonSecretsManagerConfig
        {
            RegionEndpoint = targetRegion,
            ServiceURL = EndpointUrl,
            UseHttp = true,
            MaxErrorRetry = 0, // Disable retries for faster failure
            Timeout = TimeSpan.FromSeconds(30)
        };
        
        _logger.LogInformation($"{InfrastructureModulePrefix}: Config ServiceURL set to: {config.ServiceURL}");
        
        var client = new AmazonSecretsManagerClient(credentials, config);
        
        // Verify the configuration after client creation
        _logger.LogInformation($"{InfrastructureModulePrefix}: Final client ServiceURL: {client.Config.ServiceURL}");
        
        return client;
    }

    /// <summary>
    /// Gets the mapped host port for the LocalStack container.
    /// </summary>
    /// <returns>The host port that maps to LocalStack port 4566</returns>
    public ushort GetMappedPort()
    {
        EnsureRunning();
        return _container!.GetMappedPublicPort(LocalStackPort);
    }

    /// <summary>
    /// Waits for LocalStack services to be ready.
    /// </summary>
    /// <param name="timeout">Maximum time to wait (default: 30 seconds)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task WaitForServicesAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        var actualTimeout = timeout ?? TimeSpan.FromSeconds(30);
        var startTime = DateTime.UtcNow;

        while (DateTime.UtcNow - startTime < actualTimeout)
        {
            if (await IsHealthyAsync(cancellationToken))
            {
                _logger.LogInformation($"{InfrastructureModulePrefix}: LocalStack services are ready");
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }

        throw new TimeoutException($"{InfrastructureModulePrefix}: LocalStack services did not become ready within {actualTimeout}");
    }

    private void EnsureRunning()
    {
        if (!IsRunning)
        {
            throw new InvalidOperationException($"{InfrastructureModulePrefix}: LocalStack container is not running");
        }
    }

    private async Task WaitForHealthCheckAsync(CancellationToken cancellationToken)
    {
        var maxAttempts = 30;
        var delay = TimeSpan.FromSeconds(2);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                if (await IsHealthyAsync(cancellationToken))
                {
                    _logger.LogInformation($"{InfrastructureModulePrefix}: LocalStack health check passed after {attempt} attempts");
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, $"{InfrastructureModulePrefix}: Health check attempt {attempt} failed");
            }

            if (attempt < maxAttempts)
            {
                await Task.Delay(delay, cancellationToken);
            }
        }

        throw new TimeoutException($"{InfrastructureModulePrefix}: LocalStack health check failed after {maxAttempts} attempts");
    }

    private async Task CleanupAsync()
    {
        try
        {
            if (_container != null)
            {
                await _container.DisposeAsync();
                _container = null;
            }

            if (_network != null)
            {
                await _network.DisposeAsync();
                _network = null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"{InfrastructureModulePrefix}: Error during LocalStack cleanup");
        }
    }

    /// <summary>
    /// Disposes the LocalStack container and associated resources.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _logger.LogInformation($"{InfrastructureModulePrefix}: Disposing LocalStack container helper");
            CleanupAsync().GetAwaiter().GetResult();
            _disposed = true;
        }
    }
}