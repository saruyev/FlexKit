using Microsoft.Extensions.Configuration.Memory;
using FlexKit.Configuration.Core;
using FlexKit.Configuration.Sources;
using Reqnroll;
using FlexKit.IntegrationTests.Utils;

namespace FlexKit.Configuration.IntegrationTests.Utils;

/// <summary>
/// Builder for creating test configurations with FlexKit.Configuration.
/// Provides a fluent API for setting up configuration sources, including
/// in-memory data, temporary files, and environment variables.
/// </summary>
public class TestConfigurationBuilder(ScenarioContext? scenarioContext = null) : BaseTestConfigurationBuilder<TestConfigurationBuilder>(scenarioContext)
{
    public TestConfigurationBuilder() : this(null) { }
    
    /// <summary>
    /// Adds an existing .env file as a configuration source.
    /// </summary>
    /// <param name="path">Path to the .env file</param>
    /// <param name="optional">Whether the file is optional</param>
    /// <returns>This builder for method chaining</returns>
    public TestConfigurationBuilder AddDotEnvFile(string path, bool optional = true)
    {
        Sources.Add(new DotEnvConfigurationSource
        {
            Path = path,
            Optional = optional
        });
        return this;
    }
    
    /// <summary>
    /// Creates a temporary .env file with the provided content and adds it as a configuration source.
    /// </summary>
    /// <param name="envContent">Environment file content</param>
    /// <param name="optional">Whether the file is optional</param>
    /// <returns>This builder for method chaining</returns>
    public TestConfigurationBuilder AddTempEnvFile(string envContent, bool optional = true)
    {
        var tempFile = CreateTempFile(envContent, ".env");
        AddDotEnvFile(tempFile, optional);
        return this;
    }
    
    /// <summary>
    /// Creates a temporary .env file from key-value pairs and adds it as a configuration source.
    /// </summary>
    /// <param name="envData">Dictionary of environment variables</param>
    /// <param name="optional">Whether the file is optional</param>
    /// <returns>This builder for method chaining</returns>
    public TestConfigurationBuilder AddTempEnvFile(Dictionary<string, string> envData, bool optional = true)
    {
        var envContent = string.Join(Environment.NewLine, 
            envData.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        return AddTempEnvFile(envContent, optional);
    }
    
    /// <summary>
    /// Builds a FlexConfiguration instance.
    /// </summary>
    /// <returns>The built FlexConfiguration</returns>
    public IFlexConfig BuildFlexConfig()
    {
        var configuration = Build();
        return new FlexConfiguration(configuration);
    }

    /// <summary>
    /// Builds a FlexConfiguration using FlexConfigurationBuilder.
    /// </summary>
    /// <param name="configureBuilder">Action to configure the FlexConfigurationBuilder</param>
    /// <returns>The built FlexConfiguration</returns>
    public IFlexConfig BuildFlexConfig(Action<FlexConfigurationBuilder> configureBuilder)
    {
        ApplyEnvironmentVariables();
        
        var flexBuilder = new FlexConfigurationBuilder();

        // Add in-memory data
        if (InMemoryData.Count > 0)
        {
            flexBuilder.AddSource(new MemoryConfigurationSource { InitialData = InMemoryData });
        }

        // Add temporary files
        foreach (var tempFile in TempFiles)
        {
            if (tempFile.EndsWith(".json"))
            {
                flexBuilder.AddJsonFile(tempFile, optional: true);
            }
            else if (tempFile.EndsWith(".env"))
            {
                flexBuilder.AddDotEnvFile(tempFile, optional: true);
            }
        }

        // Apply additional configuration
        configureBuilder(flexBuilder);

        return flexBuilder.Build();
    }
    
    /// <summary>
    /// Creates a FlexConfiguration with test defaults.
    /// </summary>
    /// <param name="scenarioContext">Optional scenario context</param>
    /// <returns>Built FlexConfiguration</returns>
    public static IFlexConfig CreateFlexConfigWithDefaults(ScenarioContext? scenarioContext = null)
    {
        return Create(scenarioContext!)
            .AddLoggingConfig()
            .AddDatabaseConfig()
            .AddApiConfig()
            .AddAssemblyMappingConfig("FlexKit")
            .BuildFlexConfig();
    }
}