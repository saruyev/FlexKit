using Azure.Data.AppConfiguration;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using FlexKit.IntegrationTests.Utils;
using Microsoft.Extensions.Logging;
using Reqnroll;
// ReSharper disable HollowTypeName

namespace FlexKit.Configuration.Providers.Azure.IntegrationTests.Utils;

/// <summary>
/// Helper class for managing LocalStack containers with Azure services in integration tests.
/// Provides containerized Azure Key Vault and App Configuration services for isolated testing
/// without real Azure dependencies.
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
    /// Initializes a new LocalStack container helper for Azure services.
    /// </summary>
    /// <param name="scenarioContext">Optional scenario context for automatic cleanup</param>
    /// <param name="logger">Logger for diagnostics</param>
    public LocalStackContainerHelper(ScenarioContext? scenarioContext = null, ILogger<LocalStackContainerHelper>? logger = null)
    {
        _logger = logger ?? CreateDefaultLogger();
        
        // Register for cleanup if scenario context is available
        if (scenarioContext != null)
        {
            scenarioContext.RegisterForCleanup(this);
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
    /// Starts the LocalStack container with Azure services.
    /// </summary>
    /// <param name="services">Comma-separated list of Azure services to enable (default: keyvault,appconfig)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task that completes when the container is started and healthy</returns>
    public async Task StartAsync(string services = "keyvault,appconfig", CancellationToken cancellationToken = default)
    {
        if (IsRunning)
        {
            _logger.LogInformation($"{InfrastructureModulePrefix}: LocalStack container is already running");
            return;
        }

        try
        {
            _logger.LogInformation($"{InfrastructureModulePrefix}: Starting LocalStack container with Azure services: {services}");

            // Set up environment variables for LocalStack Azure authentication
            Environment.SetEnvironmentVariable("AZURE_CLIENT_ID", "test");
            Environment.SetEnvironmentVariable("AZURE_CLIENT_SECRET", "test");
            Environment.SetEnvironmentVariable("AZURE_TENANT_ID", "test");
            Environment.SetEnvironmentVariable("AZURE_SUBSCRIPTION_ID", "test");

            // Create a custom network for better isolation
            _network = new NetworkBuilder()
                .WithName($"{InfrastructureModulePrefix}_localstack_network_{Guid.NewGuid():N}")
                .Build();

            await _network.CreateAsync(cancellationToken);
            _logger.LogInformation($"{InfrastructureModulePrefix}: Created LocalStack network");

            // Build LocalStack container with Azure services
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
                // Azure-specific configuration
                .WithEnvironment("PROVIDER", "azure") // Explicitly set to Azure provider
                .WithEnvironment("AZURE_DEFAULT_REGION", "eastus") // Set default Azure region
                .WithEnvironment("AZURE_CLIENT_ID", "test") // Test Azure client ID
                .WithEnvironment("AZURE_CLIENT_SECRET", "test") // Test Azure client secret
                .WithEnvironment("AZURE_TENANT_ID", "test") // Test Azure tenant ID
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
    /// <returns>True if health check passes, false otherwise</returns>
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
    /// Creates a Key Vault SecretClient configured for LocalStack.
    /// </summary>
    /// <param name="vaultUri">Key Vault URI (will be redirected to LocalStack)</param>
    /// <returns>SecretClient configured for LocalStack</returns>
    public SecretClient CreateKeyVaultClient(string vaultUri = "https://test-vault.vault.azure.net/")
    {
        EnsureRunning();
        
        _logger.LogInformation($"{InfrastructureModulePrefix}: Creating Key Vault client for LocalStack at {EndpointUrl}");
        
        // For LocalStack Azure, we need to use the LocalStack endpoint directly
        var localStackKeyVaultUri = $"{EndpointUrl.TrimEnd('/')}/";
        
        // Create a simple credential for LocalStack - use ChainedTokenCredential with environment variables
        var credential = new ChainedTokenCredential(
            new EnvironmentCredential(),
            new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ExcludeEnvironmentCredential = false,
                ExcludeInteractiveBrowserCredential = true,
                ExcludeManagedIdentityCredential = true,
                ExcludeSharedTokenCacheCredential = true,
                ExcludeVisualStudioCredential = true,
#pragma warning disable CS0618 // Type or member is obsolete
                ExcludeVisualStudioCodeCredential = true,
#pragma warning restore CS0618 // Type or member is obsolete
                ExcludeAzureCliCredential = true,
                ExcludeAzurePowerShellCredential = true
            })
        );
        
        var clientOptions = new SecretClientOptions
        {
            Retry = { MaxRetries = 0 } // Disable retries for faster failure
        };
        
        // Use LocalStack endpoint instead of real Azure endpoint
        return new SecretClient(new Uri(localStackKeyVaultUri), credential, clientOptions);
    }

    /// <summary>
    /// Creates an App Configuration ConfigurationClient configured for LocalStack.
    /// </summary>
    /// <param name="connectionString">App Configuration connection string (will be redirected to LocalStack)</param>
    /// <returns>ConfigurationClient configured for LocalStack</returns>
    public ConfigurationClient CreateAppConfigurationClient(string connectionString = "Endpoint=https://test-appconfig.azconfig.io;Id=test;Secret=test")
    {
        EnsureRunning();
        
        _logger.LogInformation($"{InfrastructureModulePrefix}: Creating App Configuration client for LocalStack at {EndpointUrl}");
        
        // For LocalStack, create a connection string that points to LocalStack endpoint
        var localStackConnectionString = $"Endpoint={EndpointUrl.TrimEnd('/')}/;Id=test;Secret=test";
        
        var clientOptions = new ConfigurationClientOptions
        {
            Retry = { MaxRetries = 0 } // Disable retries for faster failure
        };
        
        return new ConfigurationClient(localStackConnectionString, clientOptions);
    }

    /// <summary>
    /// Creates test data in LocalStack Azure services.
    /// </summary>
    /// <param name="testDataPath">Path to test data configuration file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task that completes when test data is created</returns>
    public async Task CreateTestDataAsync(string testDataPath, CancellationToken cancellationToken = default)
    {
        EnsureRunning();
        
        _logger.LogInformation($"{InfrastructureModulePrefix}: Creating test data from {testDataPath}");
        
        try
        {
            var testData = await LoadTestDataAsync(testDataPath, cancellationToken);
            
            // Create Key Vault secrets
            if (testData.KeyVaultSecrets?.Any() == true)
            {
                await CreateKeyVaultSecretsAsync(testData.KeyVaultSecrets, cancellationToken);
            }
            
            // Create App Configuration settings
            if (testData.AppConfigurationSettings?.Any() == true)
            {
                await CreateAppConfigurationSettingsAsync(testData.AppConfigurationSettings, cancellationToken);
            }
            
            _logger.LogInformation($"{InfrastructureModulePrefix}: Test data created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{InfrastructureModulePrefix}: Failed to create test data");
            throw;
        }
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

    /// <summary>
    /// Ensures the LocalStack container is running.
    /// </summary>
    private void EnsureRunning()
    {
        if (!IsRunning)
        {
            throw new InvalidOperationException($"{InfrastructureModulePrefix}: LocalStack container is not running");
        }
    }

    /// <summary>
    /// Waits for LocalStack health check to pass.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
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

    /// <summary>
    /// Loads test data from configuration file.
    /// </summary>
    /// <param name="testDataPath">Path to test data file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Parsed test data</returns>
    private async Task<AzureTestDataModel> LoadTestDataAsync(string testDataPath, CancellationToken cancellationToken)
    {
        var jsonContent = await File.ReadAllTextAsync(testDataPath, cancellationToken);
        return System.Text.Json.JsonSerializer.Deserialize<AzureTestDataModel>(jsonContent) ?? new AzureTestDataModel();
    }

    /// <summary>
    /// Creates Key Vault secrets in LocalStack.
    /// </summary>
    /// <param name="secrets">Dictionary of secret names and values</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task that completes when secrets are created</returns>
    private async Task CreateKeyVaultSecretsAsync(Dictionary<string, string> secrets, CancellationToken cancellationToken)
    {
        var secretClient = CreateKeyVaultClient();
        
        foreach (var secret in secrets)
        {
            await secretClient.SetSecretAsync(secret.Key, secret.Value, cancellationToken);
            _logger.LogDebug($"{InfrastructureModulePrefix}: Created Key Vault secret: {secret.Key}");
        }
    }

    /// <summary>
    /// Creates App Configuration settings in LocalStack.
    /// </summary>
    /// <param name="settings">Dictionary of setting keys and values</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task that completes when settings are created</returns>
    private async Task CreateAppConfigurationSettingsAsync(Dictionary<string, string> settings, CancellationToken cancellationToken)
    {
        var configClient = CreateAppConfigurationClient();
        
        foreach (var setting in settings)
        {
            var configSetting = new ConfigurationSetting(setting.Key, setting.Value);
            await configClient.SetConfigurationSettingAsync(configSetting, false, cancellationToken);
            _logger.LogDebug($"{InfrastructureModulePrefix}: Created App Configuration setting: {setting.Key}");
        }
    }

    /// <summary>
    /// Disposes resources asynchronously.
    /// </summary>
    /// <returns>Task that completes when disposal is finished</returns>
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