using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Reqnroll;

namespace FlexKit.IntegrationTests.Hooks;

[Binding]
public class TestHooks
{
    private static ILoggerFactory? _loggerFactory;
    private static ILogger<TestHooks>? _logger;
    private static IConfiguration? _testConfiguration;

    [BeforeTestRun]
    public static void BeforeTestRun()
    {
        try
        {
            // Initialize test configuration
            InitializeTestConfiguration();
            
            // Initialize logging with configuration
            InitializeLogging();
            
            _logger?.LogInformation("=== Starting Integration Test Run ===");
            _logger?.LogInformation("Test Environment: {Environment}", 
                Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown");
            _logger?.LogInformation("Machine Name: {MachineName}", Environment.MachineName);
            _logger?.LogInformation("Test Run Started at: {StartTime:yyyy-MM-dd HH:mm:ss}", DateTime.Now);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing test run: {ex.Message}");
            throw;
        }
    }

    [AfterTestRun]
    public static void AfterTestRun()
    {
        try
        {
            _logger?.LogInformation("=== Completed Integration Test Run ===");
            _logger?.LogInformation("Test Run Completed at: {EndTime:yyyy-MM-dd HH:mm:ss}", DateTime.Now);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during test run cleanup: {ex.Message}");
        }
        finally
        {
            // Clean up resources
            _loggerFactory?.Dispose();
            _loggerFactory = null;
            _logger = null;
            _testConfiguration = null;
        }
    }

    [BeforeFeature]
    public static void BeforeFeature(FeatureContext featureContext)
    {
        try
        {
            var featureInfo = featureContext.FeatureInfo;
            _logger?.LogInformation("--- Starting Feature: {FeatureName} ---", featureInfo.Title);
            
            if (!string.IsNullOrEmpty(featureInfo.Description))
            {
                _logger?.LogDebug("Feature Description: {Description}", featureInfo.Description);
            }

            // Store feature start time for performance tracking
            featureContext.Set(DateTime.Now, "FeatureStartTime");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in BeforeFeature for {FeatureName}", featureContext.FeatureInfo.Title);
        }
    }

    [AfterFeature]
    public static void AfterFeature(FeatureContext featureContext)
    {
        try
        {
            var featureInfo = featureContext.FeatureInfo;
            var duration = DateTime.Now - featureContext.Get<DateTime>("FeatureStartTime");
            
            _logger?.LogInformation("--- Completed Feature: {FeatureName} (Duration: {Duration:mm\\:ss}) ---", 
                featureInfo.Title, duration);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in AfterFeature for {FeatureName}", featureContext.FeatureInfo.Title);
        }
    }

    [BeforeScenario]
    public void BeforeScenario(ScenarioContext scenarioContext)
    {
        try
        {
            var scenarioInfo = scenarioContext.ScenarioInfo;
            _logger?.LogInformation("▶ Starting Scenario: {ScenarioName}", scenarioInfo.Title);
            
            // Log scenario tags if any
            if (scenarioInfo.Tags.Length > 0)
            {
                _logger?.LogDebug("Scenario Tags: [{Tags}]", string.Join(", ", scenarioInfo.Tags));
            }

            // Store scenario start time for performance tracking
            scenarioContext.Set(DateTime.Now, "ScenarioStartTime");

            // Initialize scenario-specific cleanup list
            scenarioContext.Set(new List<IDisposable>(), "DisposableResources");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in BeforeScenario for {ScenarioName}", scenarioContext.ScenarioInfo.Title);
        }
    }

    [AfterScenario]
    public void AfterScenario(ScenarioContext scenarioContext)
    {
        try
        {
            var scenarioInfo = scenarioContext.ScenarioInfo;
            var duration = DateTime.Now - scenarioContext.Get<DateTime>("ScenarioStartTime");
            var status = scenarioContext.ScenarioExecutionStatus;

            var statusIcon = status switch
            {
                ScenarioExecutionStatus.OK => "✅",
                ScenarioExecutionStatus.TestError => "❌",
                ScenarioExecutionStatus.BindingError => "⚠️",
                ScenarioExecutionStatus.UndefinedStep => "❓",
                ScenarioExecutionStatus.StepDefinitionPending => "⏭️",
                _ => "❔"
            };

            _logger?.LogInformation("{StatusIcon} Completed Scenario: {ScenarioName} - {Status} (Duration: {Duration:mm\\:ss\\.fff})", 
                statusIcon, scenarioInfo.Title, status, duration);

            // Log scenario error if failed
            if (status == ScenarioExecutionStatus.TestError && scenarioContext.TestError != null)
            {
                _logger?.LogError("Scenario failed with error: {ErrorMessage}", scenarioContext.TestError.Message);
                _logger?.LogDebug("Full error details: {ErrorDetails}", scenarioContext.TestError.ToString());
            }

            // Clean up scenario-specific resources
            CleanupScenarioResources(scenarioContext);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in AfterScenario for {ScenarioName}", scenarioContext.ScenarioInfo.Title);
        }
    }

    private static void InitializeTestConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Test.json", optional: true)
            .AddEnvironmentVariables("FLEXKIT_TEST_");

        _testConfiguration = builder.Build();
    }

    private static void InitializeLogging()
    {
        var logLevel = GetLogLevelFromConfiguration();
        
        _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .SetMinimumLevel(logLevel)
                .AddConsole(options =>
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    options.IncludeScopes = true;
                    options.TimestampFormat = "[HH:mm:ss.fff] ";
#pragma warning restore CS0618 // Type or member is obsolete
                })
                .AddFilter("Microsoft", LogLevel.Warning)
                .AddFilter("System", LogLevel.Warning);

            // Add file logging if configured
            var logFilePath = _testConfiguration?["Logging:FilePath"];
            if (!string.IsNullOrEmpty(logFilePath))
            {
                // Note: This would require Serilog or another file provider
                // For now, just log that file logging is requested
            }
        });

        _logger = _loggerFactory.CreateLogger<TestHooks>();
    }

    private static LogLevel GetLogLevelFromConfiguration()
    {
        var logLevelString = _testConfiguration?["Logging:LogLevel:Default"] ?? "Information";
        
        return logLevelString.ToUpperInvariant() switch
        {
            "TRACE" => LogLevel.Trace,
            "DEBUG" => LogLevel.Debug,
            "INFORMATION" => LogLevel.Information,
            "WARNING" => LogLevel.Warning,
            "ERROR" => LogLevel.Error,
            "CRITICAL" => LogLevel.Critical,
            "NONE" => LogLevel.None,
            _ => LogLevel.Information
        };
    }

    private static void CleanupScenarioResources(ScenarioContext scenarioContext)
    {
        try
        {
            if (scenarioContext.TryGetValue("DisposableResources", out List<IDisposable>? resources))
            {
                foreach (var resource in resources!)
                {
                    try
                    {
                        resource.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Error disposing resource of type {ResourceType}", resource.GetType().Name);
                    }
                }
                resources.Clear();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during scenario resource cleanup");
        }
    }

    // Helper method for step definitions to register disposable resources
    public static void RegisterDisposableResource(ScenarioContext scenarioContext, IDisposable resource)
    {
        if (scenarioContext.TryGetValue("DisposableResources", out List<IDisposable>? resources))
        {
            resources!.Add(resource);
        }
    }
}
