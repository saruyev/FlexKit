using Autofac;
using Amazon.Extensions.NETCore.Setup;
using FlexKit.IntegrationTests.Utils;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Reqnroll;

namespace FlexKit.Configuration.Providers.Aws.IntegrationTests.Utils;

/// <summary>
/// Specialized container builder for infrastructure module testing.
/// Extends BaseTestContainerBuilder with AWS and LocalStack specific functionality.
/// </summary>
public class TestContainerBuilder : BaseTestContainerBuilder<TestContainerBuilder>
{
    private LocalStackContainerHelper? _localStackHelper;
    private bool _autoStartLocalStack = true;
    private readonly ScenarioContext? _scenarioContext;

    /// <summary>
    /// Creates a new infrastructure module test container builder.
    /// </summary>
    /// <param name="scenarioContext">Optional scenario context for automatic cleanup</param>
    public TestContainerBuilder(ScenarioContext? scenarioContext = null) : base(scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    public TestContainerBuilder()
    {
    }

    /// <summary>
    /// Configures the builder to use LocalStack for AWS services.
    /// </summary>
    /// <param name="autoStart">Whether to automatically start LocalStack when building the container</param>
    /// <param name="services">Comma-separated list of AWS services to enable (default: ssm,secretsmanager)</param>
    /// <returns>This builder for method chaining</returns>
    [UsedImplicitly]
    public TestContainerBuilder WithLocalStack(bool autoStart = true, string services = "ssm,secretsmanager")
    {
        _autoStartLocalStack = autoStart;

        Registrations.Add(builder =>
        {
            _localStackHelper ??= new LocalStackContainerHelper(_scenarioContext);

            builder.RegisterInstance(_localStackHelper)
                .As<LocalStackContainerHelper>()
                .SingleInstance();
        });

        return this;
    }

    /// <summary>
    /// Configures AWS options for LocalStack integration.
    /// </summary>
    /// <param name="configureOptions">Action to configure AWS options</param>
    /// <returns>This builder for method chaining</returns>
    [UsedImplicitly]
    public TestContainerBuilder WithAwsOptions(Action<AWSOptions>? configureOptions = null)
    {
        Registrations.Add(builder =>
        {
            builder.Register(_ =>
            {
                var awsOptions = _localStackHelper != null ? _localStackHelper.CreateAwsOptions() : new AWSOptions();

                configureOptions?.Invoke(awsOptions);
                return awsOptions;
            })
            .As<AWSOptions>()
            .SingleInstance();
        });

        return this;
    }

    /// <summary>
    /// Configures the builder with infrastructure module test defaults.
    /// Includes LocalStack, AWS options, and common test services.
    /// </summary>
    /// <returns>This builder for method chaining</returns>
    [UsedImplicitly]
    public TestContainerBuilder WithInfrastructureModuleDefaults()
    {
        return this
            .WithTestDefaults()
            .WithLocalStack()
            .WithAwsOptions()
            .WithInfrastructureModuleLogging();
    }

    /// <summary>
    /// Configures logging specifically for infrastructure module testing.
    /// </summary>
    /// <returns>This builder for method chaining</returns>
    [UsedImplicitly]
    public TestContainerBuilder WithInfrastructureModuleLogging()
    {
        Registrations.Add(builder =>
        {
            builder.Register(_ =>
            {
                return LoggerFactory.Create(loggingBuilder =>
                {
                    loggingBuilder
                        .AddConsole()
                        .SetMinimumLevel(LogLevel.Debug)
                        .AddFilter("FlexKit.Configuration.Providers.Aws", LogLevel.Debug)
                        .AddFilter("InfrastructureModule", LogLevel.Information);
                });
            })
            .As<ILoggerFactory>()
            .SingleInstance();

            builder.RegisterGeneric(typeof(Logger<>))
                .As(typeof(ILogger<>))
                .SingleInstance();
        });

        return this;
    }

    /// <summary>
    /// Adds test data configuration from the TestData directory.
    /// </summary>
    /// <param name="configFileName">Configuration file name in the TestData directory</param>
    /// <returns>This builder for method chaining</returns>
    [UsedImplicitly]
    public TestContainerBuilder WithInfrastructureModuleTestData(string configFileName)
    {
        if (_scenarioContext == null)
        {
            throw new InvalidOperationException("ScenarioContext is required for test data loading");
        }

        var testDataPath = _scenarioContext.GetInfrastructureModuleTestDataPath(configFileName);
        var configData = LoadTestDataConfiguration(testDataPath);

        WithConfiguration(configData);

        return this;
    }

    /// <summary>
    /// Configures mock AWS services for testing without LocalStack.
    /// Useful for unit testing AWS provider logic.
    /// </summary>
    /// <returns>This builder for method chaining</returns>
    public TestContainerBuilder WithMockAwsServices()
    {
        Registrations.Add(builder =>
        {
            // Registers mock AWS service clients
            // This would be implemented based on your mocking strategy,
            // For example, using NSubstitute or Moq

            builder.RegisterType<MockAwsServiceProvider>()
                .As<IAwsServiceProvider>()
                .SingleInstance();
        });

        return this;
    }

    /// <summary>
    /// Builds the container with all infrastructure module configurations.
    /// Handles auto-starting LocalStack if configured.
    /// </summary>
    /// <returns>The configured Autofac container</returns>
    public override IContainer Build()
    {
        RegisterServices();
        var container = ApplyRegistrations();

        // Handle auto-start LocalStack after the container is built
        if (_autoStartLocalStack && _localStackHelper != null)
        {
            try
            {
                _localStackHelper.StartAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to auto-start LocalStack container", ex);
            }
        }

        return container;
    }

    /// <summary>
    /// Creates a minimal infrastructure module container for basic testing.
    /// </summary>
    /// <param name="scenarioContext">Scenario context for cleanup</param>
    /// <returns>A minimal test container with LocalStack</returns>
    public static async Task<IContainer> CreateMinimalInfrastructureModuleAsync(ScenarioContext scenarioContext)
    {
        var builder = Create(scenarioContext)
            .WithTestDefaults()
            .WithLocalStack(autoStart: false); // Don't auto-start for minimal setup

        var container = builder.Build();

        // Manually start LocalStack for minimal setup
        var localStackHelper = container.Resolve<LocalStackContainerHelper>();
        await localStackHelper.StartAsync();

        return container;
    }

    /// <summary>
    /// Creates a full infrastructure module container with all testing capabilities.
    /// </summary>
    /// <param name="scenarioContext">Scenario context for cleanup</param>
    /// <param name="testDataConfig">Optional test data configuration file</param>
    /// <returns>A fully configured test container</returns>
    public static IContainer CreateFullInfrastructureModule(ScenarioContext scenarioContext, string? testDataConfig = null)
    {
        var builder = Create(scenarioContext)
            .WithInfrastructureModuleDefaults();

        if (!string.IsNullOrEmpty(testDataConfig))
        {
            builder.WithInfrastructureModuleTestData(testDataConfig);
        }

        return builder.Build();
    }

    private Dictionary<string, string?> LoadTestDataConfiguration(string filePath)
    {
        var fullPath = Path.GetFullPath(filePath);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Infrastructure module test data file not found: {filePath}");
        }

        var jsonContent = File.ReadAllText(fullPath);
        var config = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent);

        // Flatten the configuration for use with IConfiguration
        return FlattenConfiguration(config ?? new Dictionary<string, object>());
    }

    private Dictionary<string, string?> FlattenConfiguration(Dictionary<string, object> config, string prefix = "")
    {
        var flattened = new Dictionary<string, string?>();

        foreach (var kvp in config)
        {
            var key = string.IsNullOrEmpty(prefix) ? kvp.Key : $"{prefix}:{kvp.Key}";

            if (kvp.Value is Dictionary<string, object> nestedDict)
            {
                var nested = FlattenConfiguration(nestedDict, key);
                foreach (var nestedKvp in nested)
                {
                    flattened[nestedKvp.Key] = nestedKvp.Value;
                }
            }
            else if (kvp.Value is System.Text.Json.JsonElement jsonElement)
            {
                flattened[key] = jsonElement.ToString();
            }
            else
            {
                flattened[key] = kvp.Value.ToString();
            }
        }

        return flattened;
    }
}

/// <summary>
/// Mock AWS service provider for testing without LocalStack.
/// </summary>
public interface IAwsServiceProvider
{
    T CreateService<T>() where T : class;
}

/// <summary>
/// Implementation of a mock AWS service provider.
/// </summary>
public class MockAwsServiceProvider : IAwsServiceProvider
{
    public T CreateService<T>() where T : class
    {
        // This would create mock implementations of AWS services
        // Implementation depends on your mocking framework choice
        throw new NotImplementedException("Mock AWS service provider not implemented");
    }
}