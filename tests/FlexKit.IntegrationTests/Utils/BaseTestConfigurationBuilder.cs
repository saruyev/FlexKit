using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Reqnroll;
using System.Text.Json;
using System.IO.Abstractions;
using FlexKit.IntegrationTests.Hooks;
using JetBrains.Annotations;
// ReSharper disable MemberCanBeProtected.Global
// ReSharper disable ClassTooBig
// ReSharper disable NullableWarningSuppressionIsUsed

namespace FlexKit.IntegrationTests.Utils;

public abstract class BaseTestConfigurationBuilder<T> where T : BaseTestConfigurationBuilder<T>, new()
{
    public List<IConfigurationSource> Sources { get; }
    protected readonly Dictionary<string, string?> InMemoryData;
    protected readonly List<string> TempFiles;
    private readonly Dictionary<string, string?> _environmentVariables;
    [UsedImplicitly] protected ScenarioContext? ScenarioContext;
    private IFileSystem? _fileSystem;

    /// <summary>
    /// Creates a new TestConfigurationBuilder.
    /// </summary>
    /// <param name="scenarioContext">Optional scenario context for automatic cleanup</param>
    protected BaseTestConfigurationBuilder(ScenarioContext? scenarioContext = null)
    {
        Sources = new List<IConfigurationSource>();
        InMemoryData = new Dictionary<string, string?>();
        TempFiles = new List<string>();
        _environmentVariables = new Dictionary<string, string?>();
        ScenarioContext = scenarioContext;
    }

    /// <summary>
    /// Creates a TestConfigurationBuilder with automatic cleanup registration.
    /// </summary>
    /// <param name="scenarioContext">Scenario context for cleanup registration</param>
    /// <returns>New TestConfigurationBuilder instance</returns>
    public static T Create(ScenarioContext scenarioContext)
    {
        return new T{ ScenarioContext = scenarioContext };
    }

    /// <summary>
    /// Creates a TestConfigurationBuilder without automatic cleanup registration.
    /// </summary>
    /// <returns>New TestConfigurationBuilder instance</returns>
    public static T Create()
    {
        return new T();
    }

    /// <summary>
    /// Sets the file system for file operations.
    /// </summary>
    /// <param name="fileSystem">The file system implementation</param>
    /// <returns>This builder for method chaining</returns>
    public T WithFileSystem(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
        return (T)this;
    }

    /// <summary>
    /// Adds key-value pairs to the in-memory configuration.
    /// </summary>
    /// <param name="data">Dictionary of configuration data</param>
    /// <returns>This builder for method chaining</returns>
    public T AddInMemoryCollection(Dictionary<string, string?> data)
    {
        foreach (var kvp in data)
        {
            InMemoryData[kvp.Key] = kvp.Value;
        }
        return (T)this;
    }

    /// <summary>
    /// Adds a single key-value pair to the in-memory configuration.
    /// </summary>
    /// <param name="key">Configuration key</param>
    /// <param name="value">Configuration value</param>
    /// <returns>This builder for method chaining</returns>
    public T AddKeyValue(string key, string? value)
    {
        InMemoryData[key] = value;
        return (T)this;
    }

    /// <summary>
    /// Adds a configuration section with nested values.
    /// </summary>
    /// <param name="sectionName">Name of the configuration section</param>
    /// <param name="sectionData">Dictionary of section data</param>
    /// <returns>This builder for method chaining</returns>
    public T AddSection(string sectionName, Dictionary<string, string?> sectionData)
    {
        foreach (var kvp in sectionData)
        {
            var key = $"{sectionName}:{kvp.Key}";
            InMemoryData[key] = kvp.Value;
        }
        return (T)this;
    }

    /// <summary>
    /// Creates a temporary JSON file with the provided content and adds it as a configuration source.
    /// </summary>
    /// <param name="jsonContent">JSON content as string</param>
    /// <param name="optional">Whether the file is optional</param>
    /// <param name="reloadOnChange">Whether to reload on file changes</param>
    /// <returns>This builder for method chaining</returns>
    public T AddTempJsonFile(string jsonContent, bool optional = true, bool reloadOnChange = false)
    {
        var tempFile = CreateTempFile(jsonContent, ".json");
        AddJsonFile(tempFile, optional, reloadOnChange);
        return (T)this;
    }

    /// <summary>
    /// Creates a temporary JSON file from an object and adds it as a configuration source.
    /// </summary>
    /// <param name="configObject">Object to serialize to JSON</param>
    /// <param name="optional">Whether the file is optional</param>
    /// <param name="reloadOnChange">Whether to reload on file changes</param>
    /// <returns>This builder for method chaining</returns>
    public T AddTempJsonFile(object configObject, bool optional = true, bool reloadOnChange = false)
    {
        var jsonContent = JsonSerializer.Serialize(configObject, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        return AddTempJsonFile(jsonContent, optional, reloadOnChange);
    }

    /// <summary>
    /// Adds an existing JSON file as a configuration source.
    /// </summary>
    /// <param name="path">Path to the JSON file</param>
    /// <param name="optional">Whether the file is optional</param>
    /// <param name="reloadOnChange">Whether to reload on file changes</param>
    /// <returns>This builder for method chaining</returns>
    public T AddJsonFile(string path, bool optional = true, bool reloadOnChange = false)
    {
        Sources.Add(new Microsoft.Extensions.Configuration.Json.JsonConfigurationSource
        {
            Path = path,
            Optional = optional,
            ReloadOnChange = reloadOnChange
        });
        return (T)this;
    }

    /// <summary>
    /// Adds environment variables as a configuration source.
    /// </summary>
    /// <param name="prefix">Optional prefix to filter environment variables</param>
    /// <returns>This builder for method chaining</returns>
    public T AddEnvironmentVariables(string? prefix = null)
    {
        Sources.Add(new Microsoft.Extensions.Configuration.EnvironmentVariables.EnvironmentVariablesConfigurationSource
        {
            Prefix = prefix
        });
        return (T)this;
    }

    /// <summary>
    /// Sets up test environment variables that will be restored after the scenario.
    /// </summary>
    /// <param name="variables">Dictionary of environment variables to set</param>
    /// <returns>This builder for method chaining</returns>
    public T WithEnvironmentVariables(Dictionary<string, string?> variables)
    {
        foreach (var kvp in variables)
        {
            _environmentVariables[kvp.Key] = kvp.Value;
        }
        return (T)this;
    }

    /// <summary>
    /// Sets up a single test environment variable that will be restored after the scenario.
    /// </summary>
    /// <param name="name">Environment variable name</param>
    /// <param name="value">Environment variable value</param>
    /// <returns>This builder for method chaining</returns>
    public T WithEnvironmentVariable(string name, string? value)
    {
        _environmentVariables[name] = value;
        return (T)this;
    }

    /// <summary>
    /// Adds a custom configuration source.
    /// </summary>
    /// <param name="source">The configuration source to add</param>
    /// <returns>This builder for method chaining</returns>
    public T AddSource(IConfigurationSource source)
    {
        Sources.Add(source);
        return (T)this;
    }

    /// <summary>
    /// Adds a common database configuration for testing.
    /// </summary>
    /// <param name="connectionString">Database connection string</param>
    /// <param name="commandTimeout">Command timeout in seconds</param>
    /// <param name="maxRetryCount">Maximum retry count</param>
    /// <returns>This builder for method chaining</returns>
    public T AddDatabaseConfig(
        string connectionString = "Data Source=:memory:",
        int commandTimeout = 30,
        int maxRetryCount = 3)
    {
        return AddSection("Database", new Dictionary<string, string?>
        {
            ["ConnectionString"] = connectionString,
            ["CommandTimeout"] = commandTimeout.ToString(),
            ["MaxRetryCount"] = maxRetryCount.ToString(),
            ["EnableLogging"] = "true"
        });
    }

    /// <summary>
    /// Adds a common API configuration for testing.
    /// </summary>
    /// <param name="baseUrl">API base URL</param>
    /// <param name="apiKey">API key</param>
    /// <param name="timeout">Timeout in milliseconds</param>
    /// <returns>This builder for method chaining</returns>
    public T AddApiConfig(
        string baseUrl = "https://test.api.com",
        string apiKey = "test-api-key",
        int timeout = 5000)
    {
        return AddSection("External:Api", new Dictionary<string, string?>
        {
            ["BaseUrl"] = baseUrl,
            ["ApiKey"] = apiKey,
            ["Timeout"] = timeout.ToString(),
            ["RetryCount"] = "3",
            ["EnableCompression"] = "true"
        });
    }

    /// <summary>
    /// Adds a common logging configuration for testing.
    /// </summary>
    /// <param name="defaultLevel">Default log level</param>
    /// <param name="microsoftLevel">Microsoft libraries log level</param>
    /// <returns>This builder for method chaining</returns>
    public T AddLoggingConfig(
        string defaultLevel = "Debug",
        string microsoftLevel = "Warning")
    {
        return AddSection("Logging:LogLevel", new Dictionary<string, string?>
        {
            ["Default"] = defaultLevel,
            ["Microsoft"] = microsoftLevel,
            ["System"] = "Warning"
        });
    }

    /// <summary>
    /// Adds feature flag configuration for testing.
    /// </summary>
    /// <param name="features">Dictionary of feature flags</param>
    /// <returns>This builder for method chaining</returns>
    public T AddFeatureFlags(Dictionary<string, bool> features)
    {
        var featureData = features.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.ToString());
        
        return AddSection("Features", featureData!);
    }

    /// <summary>
    /// Adds assembly mapping configuration for FlexKit integration.
    /// </summary>
    /// <param name="prefix">Assembly prefix to scan</param>
    /// <param name="names">Specific assembly names to scan</param>
    /// <returns>This builder for method chaining</returns>
    [UsedImplicitly]
    public T AddAssemblyMappingConfig(string? prefix = null, string[]? names = null)
    {
        var mappingData = new Dictionary<string, string?>();
        
        if (!string.IsNullOrEmpty(prefix))
        {
            mappingData["Prefix"] = prefix;
        }

        if (names?.Length > 0)
        {
            for (int i = 0; i < names.Length; i++)
            {
                mappingData[$"Names:{i}"] = names[i];
            }
        }

        return AddSection("Application:Mapping", mappingData);
    }

    /// <summary>
    /// Builds the standard Microsoft.Extensions.Configuration.IConfiguration.
    /// </summary>
    /// <returns>The built configuration</returns>
    public IConfiguration Build()
    {
        ApplyEnvironmentVariables();
        
        var builder = new ConfigurationBuilder();

        // Add in-memory data first (the lowest priority)
        if (InMemoryData.Count > 0)
        {
            builder.Add(new MemoryConfigurationSource { InitialData = InMemoryData });
        }

        // Add all other sources in the order they were added
        foreach (var source in Sources)
        {
            builder.Add(source);
        }

        return builder.Build();
    }

    protected string CreateTempFile(string content, string extension)
    {
        var fs = _fileSystem ?? new FileSystem();
        var tempPath = Path.Combine(Path.GetTempPath(), $"flexkit_test_{Guid.NewGuid():N}{extension}");
        
        fs.File.WriteAllText(tempPath, content);
        
        TempFiles.Add(tempPath);
        
        // Register for cleanup if a scenario context is available
        if (ScenarioContext != null)
        {
            ScenarioCleanupHooks.RegisterTempFile(ScenarioContext, tempPath);
        }

        return tempPath;
    }

    protected void ApplyEnvironmentVariables()
    {
        foreach (var kvp in _environmentVariables)
        {
            if (ScenarioContext != null)
            {
                ScenarioContext.SetEnvironmentVariable(kvp.Key, kvp.Value);
            }
            else
            {
                Environment.SetEnvironmentVariable(kvp.Key, kvp.Value);
            }
        }
    }

    // Static factory methods for common scenarios

    /// <summary>
    /// Creates a simple in-memory configuration with the provided data.
    /// </summary>
    /// <param name="data">Configuration data</param>
    /// <param name="scenarioContext">Optional scenario context</param>
    /// <returns>Built configuration</returns>
    public static IConfiguration CreateSimple(Dictionary<string, string?> data, ScenarioContext? scenarioContext = null)
    {
        return Create(scenarioContext!)
            .AddInMemoryCollection(data)
            .Build();
    }

    /// <summary>
    /// Creates a configuration with common test defaults.
    /// </summary>
    /// <param name="scenarioContext">Optional scenario context</param>
    /// <returns>Built configuration</returns>
    public static IConfiguration CreateWithDefaults(ScenarioContext? scenarioContext = null)
    {
        return Create(scenarioContext!)
            .AddLoggingConfig()
            .AddDatabaseConfig()
            .AddApiConfig()
            .AddFeatureFlags(new Dictionary<string, bool>
            {
                ["EnableCaching"] = true,
                ["EnableMetrics"] = true,
                ["DebugMode"] = true
            })
            .Build();
    }
    
    /// <summary>
    /// Clears all configuration sources and environment variables from the builder.
    /// </summary>
    /// <returns>This builder for method chaining</returns>
    public T Clear()
    {
        Sources.Clear();
        InMemoryData.Clear();
        _environmentVariables.Clear();
        TempFiles.Clear();
        
        return (T)this;
    }
}
