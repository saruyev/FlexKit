using System.IO.Abstractions.TestingHelpers;
using Autofac;
using Microsoft.Extensions.Logging;
using Reqnroll;
// ReSharper disable ComplexConditionExpression
// ReSharper disable MethodTooLong
// ReSharper disable NullableWarningSuppressionIsUsed

namespace FlexKit.IntegrationTests.Hooks;

/// <summary>
/// Handles cleanup of resources created during scenario execution.
/// This includes temporary files, containers, services, and other disposable resources.
/// </summary>
[Binding]
public class ScenarioCleanupHooks
{
    private static readonly ILogger<ScenarioCleanupHooks> Logger = 
        LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<ScenarioCleanupHooks>();

    private const string TempFilesKey = "TempFiles";
    private const string TempDirectoriesKey = "TempDirectories";
    private const string AutofacContainersKey = "AutofacContainers";
    private const string ServiceProvidersKey = "ServiceProviders";
    private const string MockFileSystemsKey = "MockFileSystems";
    private const string ConfigurationBuildersKey = "ConfigurationBuilders";
    private const string EnvironmentVariablesKey = "EnvironmentVariables";

    [BeforeScenario(Order = 1000)] // Run early to set up cleanup tracking
    public void InitializeCleanupTracking(ScenarioContext scenarioContext)
    {
        try
        {
            // Initialize collections for tracking resources that need cleanup
            scenarioContext.Set(new List<string>(), TempFilesKey);
            scenarioContext.Set(new List<string>(), TempDirectoriesKey);
            scenarioContext.Set(new List<IContainer>(), AutofacContainersKey);
            scenarioContext.Set(new List<IServiceProvider>(), ServiceProvidersKey);
            scenarioContext.Set(new List<MockFileSystem>(), MockFileSystemsKey);
            scenarioContext.Set(new List<object>(), ConfigurationBuildersKey);
            scenarioContext.Set(new Dictionary<string, string?>(), EnvironmentVariablesKey);

            Logger.LogDebug("Initialized cleanup tracking for scenario: {ScenarioTitle}", 
                scenarioContext.ScenarioInfo.Title);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize cleanup tracking for scenario: {ScenarioTitle}", 
                scenarioContext.ScenarioInfo.Title);
            throw;
        }
    }

    [AfterScenario(Order = 0)] // Run last to ensure all cleanup happens
    public void CleanupScenarioResources(ScenarioContext scenarioContext)
    {
        var scenarioTitle = scenarioContext.ScenarioInfo.Title;
        var cleanupResults = new List<string>();

        try
        {
            Logger.LogDebug("Starting cleanup for scenario: {ScenarioTitle}", scenarioTitle);

            // Clean up in reverse order of importance (the least critical first)
            CleanupEnvironmentVariables(scenarioContext, cleanupResults);
            CleanupMockFileSystems(scenarioContext, cleanupResults);
            CleanupTempFiles(scenarioContext, cleanupResults);
            CleanupTempDirectories(scenarioContext, cleanupResults);
            CleanupServiceProviders(scenarioContext, cleanupResults);
            CleanupAutofacContainers(scenarioContext, cleanupResults);

            var successCount = cleanupResults.Count(r => r.StartsWith("✅"));
            var errorCount = cleanupResults.Count(r => r.StartsWith("❌"));

            if (errorCount == 0)
            {
                Logger.LogDebug("Cleanup completed successfully for scenario: {ScenarioTitle} ({SuccessCount} items)", 
                    scenarioTitle, successCount);
            }
            else
            {
                Logger.LogWarning("Cleanup completed with {ErrorCount} errors for scenario: {ScenarioTitle}", 
                    errorCount, scenarioTitle);
                
                foreach (var error in cleanupResults.Where(r => r.StartsWith("❌")))
                {
                    Logger.LogWarning("Cleanup issue: {Error}", error);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Critical error during cleanup for scenario: {ScenarioTitle}", scenarioTitle);
        }
    }

    private void CleanupTempFiles(ScenarioContext scenarioContext, List<string> results)
    {
        if (!scenarioContext.TryGetValue(TempFilesKey, out List<string>? tempFiles) || tempFiles!.Count == 0)
            return;

        foreach (var filePath in tempFiles.ToList())
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.SetAttributes(filePath, FileAttributes.Normal); // Remove read-only if set
                    File.Delete(filePath);
                    results.Add($"✅ Deleted temp file: {Path.GetFileName(filePath)}");
                }
                else
                {
                    results.Add($"ℹ️ Temp file already removed: {Path.GetFileName(filePath)}");
                }
            }
            catch (Exception ex)
            {
                results.Add($"❌ Failed to delete temp file {Path.GetFileName(filePath)}: {ex.Message}");
            }
        }

        tempFiles.Clear();
    }

    private void CleanupTempDirectories(ScenarioContext scenarioContext, List<string> results)
    {
        if (!scenarioContext.TryGetValue(TempDirectoriesKey, out List<string>? tempDirs) || tempDirs!.Count == 0)
            return;

        // Sort by depth (deepest first) to avoid deletion order issues
        var sortedDirs = tempDirs.OrderByDescending(dir => dir.Split(Path.DirectorySeparatorChar).Length);

        foreach (var dirPath in sortedDirs.ToList())
        {
            try
            {
                if (Directory.Exists(dirPath))
                {
                    // Remove read-only attributes from all files and subdirectories
                    RemoveReadOnlyAttributes(dirPath);
                    Directory.Delete(dirPath, recursive: true);
                    results.Add($"✅ Deleted temp directory: {Path.GetFileName(dirPath)}");
                }
                else
                {
                    results.Add($"ℹ️ Temp directory already removed: {Path.GetFileName(dirPath)}");
                }
            }
            catch (Exception ex)
            {
                results.Add($"❌ Failed to delete temp directory {Path.GetFileName(dirPath)}: {ex.Message}");
            }
        }

        tempDirs.Clear();
    }

    private void CleanupAutofacContainers(ScenarioContext scenarioContext, List<string> results)
    {
        if (!scenarioContext.TryGetValue(AutofacContainersKey, out List<IContainer>? containers) || containers!.Count == 0)
            return;

        foreach (var container in containers.ToList())
        {
            try
            {
                container.Dispose();
                results.Add("✅ Disposed Autofac container");
            }
            catch (Exception ex)
            {
                results.Add($"❌ Failed to dispose Autofac container: {ex.Message}");
            }
        }

        containers.Clear();
    }

    private void CleanupServiceProviders(ScenarioContext scenarioContext, List<string> results)
    {
        if (!scenarioContext.TryGetValue(ServiceProvidersKey, out List<IServiceProvider>? providers) || providers!.Count == 0)
            return;

        foreach (var provider in providers.ToList())
        {
            try
            {
                if (provider is IDisposable disposableProvider)
                {
                    disposableProvider.Dispose();
                    results.Add("✅ Disposed service provider");
                }
            }
            catch (Exception ex)
            {
                results.Add($"❌ Failed to dispose service provider: {ex.Message}");
            }
        }

        providers.Clear();
    }

    private void CleanupMockFileSystems(ScenarioContext scenarioContext, List<string> results)
    {
        if (!scenarioContext.TryGetValue(MockFileSystemsKey, out List<MockFileSystem>? fileSystems) || fileSystems!.Count == 0)
            return;

        foreach (var fileSystem in fileSystems.ToList())
        {
            try
            {
                // MockFileSystem doesn't need explicit disposal, but we can clear its contents
                fileSystem.Directory.Delete("/", recursive: true);
                results.Add("✅ Cleared mock file system");
            }
            catch (Exception ex)
            {
                results.Add($"❌ Failed to clear mock file system: {ex.Message}");
            }
        }

        fileSystems.Clear();
    }

    private void CleanupEnvironmentVariables(ScenarioContext scenarioContext, List<string> results)
    {
        if (!scenarioContext.TryGetValue(EnvironmentVariablesKey, out Dictionary<string, string?>? envVars) || envVars!.Count == 0)
            return;

        foreach (var kvp in envVars.ToList())
        {
            try
            {
                Environment.SetEnvironmentVariable(kvp.Key, kvp.Value); // Restore original value
                results.Add($"✅ Restored environment variable: {kvp.Key}");
            }
            catch (Exception ex)
            {
                results.Add($"❌ Failed to restore environment variable {kvp.Key}: {ex.Message}");
            }
        }

        envVars.Clear();
    }

    private static void RemoveReadOnlyAttributes(string dirPath)
    {
        try
        {
            foreach (var file in Directory.GetFiles(dirPath, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }

            foreach (var dir in Directory.GetDirectories(dirPath, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(dir, FileAttributes.Normal);
            }
        }
        catch (Exception ex)
        {
            // Log but don't throw - this is a cleanup helper
            Logger.LogWarning(ex, "Failed to remove read-only attributes from directory: {DirPath}", dirPath);
        }
    }

    // Helper methods for step definitions to register resources for cleanup

    /// <summary>
    /// Register a temporary file for cleanup after the scenario.
    /// </summary>
    public static void RegisterTempFile(ScenarioContext scenarioContext, string filePath)
    {
        if (scenarioContext.TryGetValue(TempFilesKey, out List<string>? tempFiles))
        {
            tempFiles!.Add(filePath);
        }
    }

    /// <summary>
    /// Register a temporary directory for cleanup after the scenario.
    /// </summary>
    public static void RegisterTempDirectory(ScenarioContext scenarioContext, string dirPath)
    {
        if (scenarioContext.TryGetValue(TempDirectoriesKey, out List<string>? tempDirs))
        {
            tempDirs!.Add(dirPath);
        }
    }

    /// <summary>
    /// Register an Autofac container for disposal after the scenario.
    /// </summary>
    public static void RegisterAutofacContainer(ScenarioContext scenarioContext, IContainer container)
    {
        if (scenarioContext.TryGetValue(AutofacContainersKey, out List<IContainer>? containers))
        {
            containers!.Add(container);
        }
    }

    /// <summary>
    /// Register a service provider for disposal after the scenario.
    /// </summary>
    public static void RegisterServiceProvider(ScenarioContext scenarioContext, IServiceProvider serviceProvider)
    {
        if (scenarioContext.TryGetValue(ServiceProvidersKey, out List<IServiceProvider>? providers))
        {
            providers!.Add(serviceProvider);
        }
    }

    /// <summary>
    /// Register a mock file system for cleanup after the scenario.
    /// </summary>
    public static void RegisterMockFileSystem(ScenarioContext scenarioContext, MockFileSystem mockFileSystem)
    {
        if (scenarioContext.TryGetValue(MockFileSystemsKey, out List<MockFileSystem>? fileSystems))
        {
            fileSystems!.Add(mockFileSystem);
        }
    }

    /// <summary>
    /// Register an environment variable change for restoration after the scenario.
    /// </summary>
    public static void RegisterEnvironmentVariableChange(ScenarioContext scenarioContext, string variableName)
    {
        if (scenarioContext.TryGetValue(EnvironmentVariablesKey, out Dictionary<string, string?>? envVars))
        {
            var originalValue = Environment.GetEnvironmentVariable(variableName);
            envVars![variableName] = originalValue;
        }
    }
}