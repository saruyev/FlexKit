using FlexKit.IntegrationTests.Utils;
using Reqnroll;
// ReSharper disable ComplexConditionExpression

namespace FlexKit.Configuration.Providers.Aws.IntegrationTests.Utils;

/// <summary>
/// Extension methods for ScenarioContext to handle infrastructure module-specific operations.
/// Provides a unique prefix to avoid conflicts with other test modules.
/// </summary>
public static class ScenarioExtensions
{
    private const string InfrastructureModulePrefix = "infrastructure_module";

    /// <summary>
    /// Registers an environment variable change for restoration after the scenario.
    /// Uses infrastructure module prefix to avoid conflicts.
    /// </summary>
    /// <param name="scenarioContext">The current scenario context</param>
    /// <param name="variableName">The environment variable name</param>
    public static void RegisterEnvironmentVariableChange(this ScenarioContext scenarioContext, string variableName)
    {
        var originalValue = Environment.GetEnvironmentVariable(variableName);
        var key = $"{InfrastructureModulePrefix}_env_{variableName}";
        scenarioContext.Set(originalValue, key);
        
        // Register for cleanup
        scenarioContext.RegisterForCleanup(new EnvironmentVariableRestorer(variableName, originalValue));
    }

    /// <summary>
    /// Sets an infrastructure module specific context value.
    /// </summary>
    /// <typeparam name="T">Type of the value</typeparam>
    /// <param name="scenarioContext">The current scenario context</param>
    /// <param name="key">The context key (will be prefixed)</param>
    /// <param name="value">The value to store</param>
    public static void SetInfrastructureModuleValue<T>(this ScenarioContext scenarioContext, string key, T value)
    {
        var prefixedKey = $"{InfrastructureModulePrefix}_{key}";
        scenarioContext.Set(value, prefixedKey);
    }

    /// <summary>
    /// Gets an infrastructure module specific context value.
    /// </summary>
    /// <typeparam name="T">Type of the value</typeparam>
    /// <param name="scenarioContext">The current scenario context</param>
    /// <param name="key">The context key (will be prefixed)</param>
    /// <returns>The stored value</returns>
    public static T GetInfrastructureModuleValue<T>(this ScenarioContext scenarioContext, string key)
    {
        var prefixedKey = $"{InfrastructureModulePrefix}_{key}";
        return scenarioContext.Get<T>(prefixedKey);
    }

    /// <summary>
    /// Tries to get an infrastructure module specific context value.
    /// </summary>
    /// <typeparam name="T">Type of the value</typeparam>
    /// <param name="scenarioContext">The current scenario context</param>
    /// <param name="key">The context key (will be prefixed)</param>
    /// <param name="value">The retrieved value if found</param>
    /// <returns>True if the value was found, false otherwise</returns>
    public static bool TryGetInfrastructureModuleValue<T>(this ScenarioContext scenarioContext, string key, out T? value)
    {
        var prefixedKey = $"{InfrastructureModulePrefix}_{key}";
        return scenarioContext.TryGetValue(prefixedKey, out value);
    }

    /// <summary>
    /// Creates a temporary file path for infrastructure module testing.
    /// The file path uses relative paths and infrastructure module prefix.
    /// </summary>
    /// <param name="scenarioContext">The current scenario context</param>
    /// <param name="fileName">The file name</param>
    /// <returns>A relative file path in the TestData directory</returns>
    public static string CreateInfrastructureModuleTempFilePath(this ScenarioContext scenarioContext, string fileName)
    {
        var tempFileName = $"{InfrastructureModulePrefix}_{Guid.NewGuid():N}_{fileName}";
        var relativePath = Path.Combine("TestData", "temp", tempFileName);
        
        // Ensure the directory exists
        var fullPath = Path.GetFullPath(relativePath);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        // Register for cleanup
        scenarioContext.RegisterForCleanup(new TempFileCleanup(fullPath));
        
        return relativePath;
    }

    /// <summary>
    /// Gets a test data file path using a relative path from the TestData directory.
    /// </summary>
    /// <param name="scenarioContext">The current scenario context</param>
    /// <param name="fileName">The test data file name</param>
    /// <returns>A relative file path in the TestData directory</returns>
    public static string GetInfrastructureModuleTestDataPath(this ScenarioContext scenarioContext, string fileName)
    {
        return Path.Combine("TestData", fileName);
    }

    /// <summary>
    /// Loads infrastructure module test configuration from the TestData directory.
    /// </summary>
    /// <param name="scenarioContext">The current scenario context</param>
    /// <param name="configFileName">The configuration file name (relative to TestData)</param>
    /// <returns>The loaded configuration as a dictionary</returns>
    public static Dictionary<string, object> LoadInfrastructureModuleTestConfig(this ScenarioContext scenarioContext, string configFileName)
    {
        var filePath = scenarioContext.GetInfrastructureModuleTestDataPath(configFileName);
        var fullPath = Path.GetFullPath(filePath);
        
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Infrastructure module test configuration file not found: {filePath}");
        }
        
        var jsonContent = File.ReadAllText(fullPath);
        var config = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent);
        
        return config ?? new Dictionary<string, object>();
    }
}

/// <summary>
/// Helper class to restore environment variables after tests.
/// </summary>
internal class EnvironmentVariableRestorer(string variableName, string? originalValue) : IDisposable
{
    private bool _disposed;

    public void Dispose()
    {
        if (!_disposed)
        {
            Environment.SetEnvironmentVariable(variableName, originalValue);
            _disposed = true;
        }
    }
}

/// <summary>
/// Helper class to clean up temporary files after tests.
/// </summary>
internal class TempFileCleanup(string filePath) : IDisposable
{
    private bool _disposed;

    public void Dispose()
    {
        if (!_disposed)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
            _disposed = true;
        }
    }
}