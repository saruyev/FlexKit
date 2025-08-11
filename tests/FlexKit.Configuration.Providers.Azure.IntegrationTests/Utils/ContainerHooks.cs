using Reqnroll;

// ReSharper disable FlagArgument
// ReSharper disable MethodTooLong
// ReSharper disable HollowTypeName
// ReSharper disable NullableWarningSuppressionIsUsed

namespace FlexKit.Configuration.Providers.Azure.IntegrationTests.Utils;

/// <summary>
/// Shared container management hooks for Azure integration tests.
/// Manages per-scenario container lifecycle with robust error handling and port conflict resolution.
/// </summary>
[Binding]
public class ContainerHooks
{
    private static AppConfigurationEmulatorContainer? _appConfigEmulator;
    private static KeyVaultEmulatorContainer? _keyVaultEmulator;

    /// <summary>
    /// Gets the App Configuration emulator for the current scenario.
    /// </summary>
    public static AppConfigurationEmulatorContainer GetAppConfigEmulator(ScenarioContext scenarioContext)
    {
        return scenarioContext.Get<AppConfigurationEmulatorContainer>("AppConfigEmulator");
    }

    /// <summary>
    /// Gets the Key Vault emulator for the current scenario.
    /// </summary>
    public static KeyVaultEmulatorContainer GetKeyVaultEmulator(ScenarioContext scenarioContext)
    {
        return scenarioContext.Get<KeyVaultEmulatorContainer>("KeyVaultEmulator");
    }

    /// <summary>
    /// Checks if App Configuration emulator is available for the current scenario.
    /// </summary>
    public static bool HasAppConfigEmulator(ScenarioContext scenarioContext)
    {
        return scenarioContext.TryGetValue("AppConfigEmulator", out AppConfigurationEmulatorContainer _);
    }

    /// <summary>
    /// Checks if Key Vault emulator is available for the current scenario.
    /// </summary>
    public static bool HasKeyVaultEmulator(ScenarioContext scenarioContext)
    {
        return scenarioContext.TryGetValue("KeyVaultEmulator", out KeyVaultEmulatorContainer _);
    }
    
    [BeforeScenario]
    public void BeforeScenario(ScenarioContext scenarioContext)
    {
        var scenarioId = Guid.NewGuid().ToString("N")[..8];
        scenarioContext.Set($"test-{scenarioId}", "ScenarioPrefix");
    }

    [BeforeTestRun]
    public static async Task BeforeTestRun()
    {
        await StartContainersWithRetry();
    }

    [AfterTestRun]
    public static async Task AfterTestRun()
    {
        await CleanupContainers();
    }

    private static async Task StartContainersWithRetry()
    {
        const int maxRetries = 5;
        var random = new Random();

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                Console.WriteLine($"Starting containers (attempt {attempt + 1})");

                // Add random delay to reduce race conditions
                if (attempt > 0)
                {
                    var delay = random.Next(1000, 3000);
                    Console.WriteLine($"Waiting {delay}ms before retry...");
                    await Task.Delay(delay);
                }

                // Create containers
                var startTasks = new List<Task>();

                _appConfigEmulator = new AppConfigurationEmulatorContainer();
                startTasks.Add(StartContainerWithTimeout(
                    _appConfigEmulator.StartAsync(), 
                    "AppConfig", 
                    TimeSpan.FromMinutes(2)));

                _keyVaultEmulator = new KeyVaultEmulatorContainer();
                startTasks.Add(StartContainerWithTimeout(
                    _keyVaultEmulator.StartAsync(), 
                    "KeyVault", 
                    TimeSpan.FromMinutes(2)));

                // Start all containers in parallel
                await Task.WhenAll(startTasks);

                Console.WriteLine($"Containers started successfully on attempt {attempt + 1}");
                return; // Success!
            }
            catch (Exception ex) when (IsPortConflictError(ex) && attempt < maxRetries - 1)
            {
                Console.WriteLine($"Port conflict on attempt {attempt + 1}: {ex.Message}");
                await CleanupFailedContainers();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Container startup failed on attempt {attempt + 1}: {ex.Message}");
                await CleanupFailedContainers();
                
                if (attempt == maxRetries - 1)
                {
                    throw new InvalidOperationException(
                        $"Failed to start containers after {maxRetries} attempts", ex);
                }
            }
        }
    }

    private static bool IsPortConflictError(Exception ex)
    {
        return ex.Message.Contains("port is already allocated", StringComparison.OrdinalIgnoreCase) ||
               ex.Message.Contains("bind", StringComparison.OrdinalIgnoreCase) ||
               ex.Message.Contains("address already in use", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task StartContainerWithTimeout(Task startTask, string containerName, TimeSpan timeout)
    {
        try
        {
            using var cts = new CancellationTokenSource(timeout);
            await startTask.WaitAsync(cts.Token);
        }
        catch (TimeoutException)
        {
            throw new InvalidOperationException($"{containerName} container startup timed out after {timeout.TotalSeconds} seconds");
        }
        catch (OperationCanceledException)
        {
            throw new InvalidOperationException($"{containerName} container startup was cancelled");
        }
    }

    private static async Task CleanupFailedContainers()
    {
        var cleanupTasks = new List<Task>();
        
        if (_appConfigEmulator != null)
        {
            cleanupTasks.Add(SafeDisposeAsync(_appConfigEmulator, "AppConfig"));
            _appConfigEmulator = null;
        }
        
        if (_keyVaultEmulator != null)
        {
            cleanupTasks.Add(SafeDisposeAsync(_keyVaultEmulator, "KeyVault"));
            _keyVaultEmulator = null;
        }

        if (cleanupTasks.Count > 0)
        {
            await Task.WhenAll(cleanupTasks);
        }
    }

    private static async Task CleanupContainers()
    {
        var cleanupTasks = new List<Task>
        {
            SafeDisposeAsync(_appConfigEmulator!, "AppConfig"),
            SafeDisposeAsync(_keyVaultEmulator!, "KeyVault")
        };

        if (cleanupTasks.Count > 0)
        {
            await Task.WhenAll(cleanupTasks);
            Console.WriteLine($"Cleaned up containers");
        }
    }

    private static async Task SafeDisposeAsync(IAsyncDisposable disposable, string name)
    {
        try
        {
            await disposable.DisposeAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error disposing {name} container: {ex.Message}");
            // Don't rethrow disposal errors to avoid masking test failures
        }
    }
    
    public static AppConfigurationEmulatorContainer GetAppConfigEmulator()
    {
        return _appConfigEmulator ?? throw new InvalidOperationException("App Config emulator not started");
    }

    public static KeyVaultEmulatorContainer GetKeyVaultEmulator()
    {
        return _keyVaultEmulator ?? throw new InvalidOperationException("Key Vault emulator not started");
    }
}

/// <summary>
/// Helper methods for step definitions to easily access emulator containers.
/// </summary>
public static class AzureEmulatorHelper
{
    /// <summary>
    /// Gets the App Configuration emulator, throwing a helpful error if not available.
    /// </summary>
    public static AppConfigurationEmulatorContainer GetAppConfigEmulator(this ScenarioContext _) => ContainerHooks.GetAppConfigEmulator();

    /// <summary>
    /// Gets the Key Vault emulator, throwing a helpful error if not available.
    /// </summary>
    public static KeyVaultEmulatorContainer GetKeyVaultEmulator(this ScenarioContext _) => ContainerHooks.GetKeyVaultEmulator();
}
