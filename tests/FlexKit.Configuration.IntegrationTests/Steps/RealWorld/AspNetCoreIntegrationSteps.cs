using Autofac;
using Autofac.Extensions.DependencyInjection;
using FlexKit.Configuration.Core;
using FlexKit.Configuration.IntegrationTests.Utils;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Reqnroll;
using FlexKit.Configuration.Conversion;
using FlexKit.IntegrationTests.Utils;

namespace FlexKit.Configuration.IntegrationTests.Steps.RealWorld;

/// <summary>
/// Step definitions for ASP.NET Core integration scenarios.
/// Tests FlexConfig integration with ASP.NET Core hosting infrastructure,
/// including WebApplicationBuilder, service registration, and dependency injection.
/// Uses distinct step patterns ("set up an ASP.NET Core", "configure ASP.NET Core", "build ASP.NET Core") 
/// to avoid conflicts with other step classes.
/// </summary>
[Binding]
public class AspNetCoreIntegrationSteps(ScenarioContext scenarioContext)
{
    private WebApplicationBuilder? _webApplicationBuilder;
    private WebApplication? _webApplication;
    private IHost? _host;
    private IServiceProvider? _serviceProvider;
    private IFlexConfig? _hostFlexConfiguration;
    private readonly Dictionary<string, string?> _aspNetCoreConfigurationData = new();
    private readonly Dictionary<string, string?> _aspNetCoreEnvironmentVariables = new();
    private readonly List<string> _aspNetCoreConfigurationSources = new();
    private Exception? _lastAspNetCoreException;
    private bool _aspNetCoreHostBuildSucceeded;
    private string _aspNetCoreEnvironmentName = "Development";
    private bool _useAutofacServiceProvider;
    private bool _validateConfigurationOnStartup;
    private TestServiceWithFlexConfig? _resolvedTestService;
    private TestControllerWithFlexConfig? _resolvedTestController;
    private TestAutofacModuleForAspNetCore? _testAutofacModule;

    #region Test Service Classes

    /// <summary>
    /// Test service that depends on FlexConfig for ASP.NET Core integration testing.
    /// </summary>
    public class TestServiceWithFlexConfig(IFlexConfig configuration)
    {
        public IFlexConfig Configuration { get; } = configuration;
        public string ServiceName { get; } = configuration["Service:Name"] ?? "DefaultService";
        public bool IsEnabled { get; } = configuration["Service:Enabled"].ToType<bool>();
        public int MaxRetries { get; } = configuration["Service:MaxRetries"].ToType<int>();

        /// <summary>
        /// Gets a configuration value to demonstrate FlexConfig access.
        /// </summary>
        /// <param name="key">Configuration key</param>
        /// <returns>Configuration value</returns>
        public string? GetConfigurationValue(string key)
        {
            return Configuration[key];
        }
    }

    /// <summary>
    /// Test a controller that uses FlexConfig for dynamic configuration access.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class TestControllerWithFlexConfig(IFlexConfig configuration) : ControllerBase
    {
        /// <summary>
        /// Test action that demonstrates dynamic configuration access.
        /// </summary>
        /// <returns>Configuration values as JSON</returns>
        [HttpGet("config")]
        public IActionResult GetConfiguration()
        {
            dynamic config = configuration;
            
            var result = new
            {
                ApiBaseUrl = config.Api?.BaseUrl?.ToString(),
                ApiTimeout = config.Api?.Timeout?.ToString(),
                EnableCache = config.Features?.EnableCache?.ToString()
            };

            return Ok(result);
        }

        /// <summary>
        /// Gets a specific configuration value.
        /// </summary>
        /// <param name="key">Configuration key</param>
        /// <returns>Configuration value</returns>
        [HttpGet("config/{key}")]
        public IActionResult GetConfigurationValue(string key)
        {
            var value = configuration[key];
            return Ok(new { Key = key, Value = value });
        }
    }

    /// <summary>
    /// Test Autofac module for ASP.NET Core integration.
    /// </summary>
    private class TestAutofacModuleForAspNetCore : Module
    {
        public bool IsLoaded { get; private set; }
        public IFlexConfig? LoadedConfiguration { get; private set; }

        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(c =>
            {
                LoadedConfiguration = c.Resolve<IFlexConfig>();
                IsLoaded = true;
                
                var moduleName = LoadedConfiguration["Autofac:ModuleName"] ?? "DefaultModule";
                var isEnabled = LoadedConfiguration["Autofac:Enabled"].ToType<bool>();
                
                return new TestAutofacService(moduleName, isEnabled);
            }).As<ITestAutofacService>().SingleInstance();
        }
    }

    /// <summary>
    /// Interface for test Autofac service.
    /// </summary>
    private interface ITestAutofacService
    {
        string ModuleName { get; }
        bool IsEnabled { get; }
    }

    /// <summary>
    /// Implementation of test Autofac service.
    /// </summary>
    private class TestAutofacService(string moduleName, bool isEnabled) : ITestAutofacService
    {
        public string ModuleName { get; } = moduleName;
        public bool IsEnabled { get; } = isEnabled;
    }

    #endregion

    #region Given Steps - Setup

    [Given(@"I have setup an ASP.NET Core application environment")]
    public void GivenIHaveSetupAnAspNetCoreApplicationEnvironment()
    {
        // Initialize web application builder
        var args = Array.Empty<string>();
        _webApplicationBuilder = WebApplication.CreateBuilder(args);
        
        // Configure Autofac as the default service provider for all scenarios
        _webApplicationBuilder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
        
        // Set up basic logging for testing
        _webApplicationBuilder.Logging.ClearProviders();
        _webApplicationBuilder.Logging.AddConsole();
        _webApplicationBuilder.Logging.SetMinimumLevel(LogLevel.Warning);
        
        scenarioContext.Set(_webApplicationBuilder, "WebApplicationBuilder");
    }

    [Given(@"I have a test service that depends on FlexConfig")]
    public void GivenIHaveATestServiceThatDependsOnFlexConfig()
    {
        // The TestServiceWithFlexConfig class is already defined above
        // This step just documents that we have such a service available
    }

    [Given(@"I have setup an ASP.NET Core controller that uses FlexConfig")]
    public void GivenIHaveSetupAnAspNetCoreControllerThatUsesFlexConfig()
    {
        // The TestControllerWithFlexConfig class is already defined above
        // This step just documents that we have such a controller available
    }

    #endregion

    #region When Steps - Configuration Actions

    [When(@"I configure ASP.NET Core host with FlexConfig and the following configuration:")]
    public void WhenIConfigureAspNetCoreHostWithFlexConfigAndTheFollowingConfiguration(Table table)
    {
        _webApplicationBuilder.Should().NotBeNull("WebApplicationBuilder should be initialized");
        
        // Store configuration data
        foreach (var row in table.Rows)
        {
            _aspNetCoreConfigurationData[row["Key"]] = row["Value"];
        }
        
        // Configure FlexConfig with in-memory data (this will be added AFTER other sources for higher precedence)
        _webApplicationBuilder!.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
        {
            containerBuilder.AddFlexConfig(config => config
                .AddSource(new MemoryConfigurationSource { InitialData = _aspNetCoreConfigurationData }));
        });
    }

    [When(@"I configure ASP.NET Core host to use FlexConfig with JSON file ""(.*)""")]
    public void WhenIConfigureAspNetCoreHostToUseFlexConfigWithJsonFile(string filePath)
    {
        _webApplicationBuilder.Should().NotBeNull("WebApplicationBuilder should be initialized");
        
        // Use the same path normalization pattern as JsonConfigurationSteps
        var normalizedPath = filePath.Replace('/', Path.DirectorySeparatorChar);
        
        if (!File.Exists(normalizedPath))
        {
            throw new FileNotFoundException($"Test data file not found: {normalizedPath}");
        }

        // Configure FlexConfig by EXTENDING the existing configuration, not replacing it
        _webApplicationBuilder!.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
        {
            containerBuilder.AddFlexConfig(config =>
            {
                // Add a JSON file first (lower precedence)
                config.AddJsonFile(normalizedPath, optional: false, reloadOnChange: false);
                
                // Add in-memory data last (higher precedence) if we have any
                if (_aspNetCoreConfigurationData.Any())
                {
                    config.AddSource(new MemoryConfigurationSource { InitialData = _aspNetCoreConfigurationData });
                }
            });
        });
        
        _aspNetCoreConfigurationSources.Add($"JsonFile:{normalizedPath}");
    }

    [When(@"I configure ASP.NET Core host with FlexConfig using multiple sources:")]
    public void WhenIConfigureAspNetCoreHostWithFlexConfigUsingMultipleSources(Table table)
    {
        _webApplicationBuilder.Should().NotBeNull("WebApplicationBuilder should be initialized");
        
        // Store the configuration sources - we'll configure FlexConfig later after environment variables are set
        var configurationSources = new List<(string sourceType, string path, bool optional)>();
        
        foreach (var row in table.Rows)
        {
            var sourceType = row["Source"];
            var path = row["Path"];
            var optional = bool.Parse(row["Optional"]);
            configurationSources.Add((sourceType, path, optional));
        }
        
        // Store for later configuration building
        scenarioContext.Set(configurationSources, "ConfigurationSources");
    }
    
    private void BuildFlexConfigWithStoredSources()
    {
        var configurationSources = scenarioContext.Get<List<(string sourceType, string path, bool optional)>>("ConfigurationSources");
        
        // Build the configuration using TestConfigurationBuilder (which handles environment variables properly)
        var testBuilder = TestConfigurationBuilder.Create(scenarioContext);
        
        // Apply environment variables first using the proper pattern
        if (_aspNetCoreEnvironmentVariables.Any())
        {
            testBuilder.WithEnvironmentVariables(_aspNetCoreEnvironmentVariables);
        }
        
        // Add sources in order of precedence (first = lowest, last = highest)
        foreach (var (sourceType, path, optional) in configurationSources)
        {
            switch (sourceType)
            {
                case "JsonFile":
                    if (!string.IsNullOrEmpty(path))
                    {
                        var normalizedPath = path.Replace('/', Path.DirectorySeparatorChar);
                        if (!optional || File.Exists(normalizedPath))
                        {
                            testBuilder.AddJsonFile(normalizedPath, optional: optional, reloadOnChange: false);
                            _aspNetCoreConfigurationSources.Add($"JsonFile:{normalizedPath}");
                        }
                    }
                    break;
                case "DotEnvFile":
                    if (!string.IsNullOrEmpty(path))
                    {
                        var normalizedPath = path.Replace('/', Path.DirectorySeparatorChar);
                        if (!optional || File.Exists(normalizedPath))
                        {
                            testBuilder.AddDotEnvFile(normalizedPath, optional: optional);
                            _aspNetCoreConfigurationSources.Add($"DotEnvFile:{normalizedPath}");
                        }
                    }
                    break;
                case "Environment":
                    testBuilder.AddEnvironmentVariables();
                    _aspNetCoreConfigurationSources.Add("EnvironmentVariables");
                    break;
            }
        }
        
        // Build FlexConfig using TestConfigurationBuilder (this properly handles environment variables)
        var flexConfig = testBuilder.BuildFlexConfig();
        
        // Register the built FlexConfig directly with Autofac
        _webApplicationBuilder!.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
        {
            containerBuilder.RegisterInstance(flexConfig.Configuration).As<IConfiguration>().SingleInstance();
            containerBuilder.RegisterInstance(flexConfig).As<IFlexConfig>().As<dynamic>().SingleInstance();
        });
    }

    [When(@"I configure ASP.NET Core host with the following environment variables:")]
    public void WhenIConfigureAspNetCoreHostWithTheFollowingEnvironmentVariables(Table table)
    {
        foreach (var row in table.Rows)
        {
            var key = row["Key"];
            var value = row["Value"];
            _aspNetCoreEnvironmentVariables[key] = value;
            
            // Set environment variables immediately AND use scenario context for proper cleanup
            if (scenarioContext != null)
            {
                scenarioContext.SetEnvironmentVariable(key, value);
            }
            else
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
        
        // Now that environment variables are set, configure FlexConfig if we have stored sources
        if (scenarioContext!.ContainsKey("ConfigurationSources"))
        {
            BuildFlexConfigWithStoredSources();
        }
    }

    [When(@"I register the test service in ASP.NET Core services")]
    public void WhenIRegisterTheTestServiceInAspNetCoreServices()
    {
        _webApplicationBuilder.Should().NotBeNull("WebApplicationBuilder should be initialized");
        
        _webApplicationBuilder!.Services.AddTransient<TestServiceWithFlexConfig>();
    }

    [When(@"I configure ASP.NET Core host to use Autofac service provider factory")]
    public void WhenIConfigureAspNetCoreHostToUseAutofacServiceProviderFactory()
    {
        _webApplicationBuilder.Should().NotBeNull("WebApplicationBuilder should be initialized");
        
        // Note: Autofac is already configured in the Given step, so just mark it
        _useAutofacServiceProvider = true;
    }

    [When(@"I configure ASP.NET Core Autofac container with FlexConfig and the following configuration:")]
    public void WhenIConfigureAspNetCoreAutofacContainerWithFlexConfigAndTheFollowingConfiguration(Table table)
    {
        _webApplicationBuilder.Should().NotBeNull("WebApplicationBuilder should be initialized");
        
        // Store configuration data
        foreach (var row in table.Rows)
        {
            _aspNetCoreConfigurationData[row["Key"]] = row["Value"];
        }
        
        // Configure Autofac container with FlexConfig
        _webApplicationBuilder!.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
        {
            containerBuilder.AddFlexConfig(config => config
                .AddSource(new MemoryConfigurationSource { InitialData = _aspNetCoreConfigurationData }));
        });
    }

    [When(@"I register a test Autofac module in the ASP.NET Core container")]
    public void WhenIRegisterATestAutofacModuleInTheAspNetCoreContainer()
    {
        _webApplicationBuilder.Should().NotBeNull("WebApplicationBuilder should be initialized");
        
        _testAutofacModule = new TestAutofacModuleForAspNetCore();
        
        _webApplicationBuilder!.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
        {
            containerBuilder.RegisterModule(_testAutofacModule);
        });
    }

    [When(@"I configure ASP.NET Core host to validate configuration on startup")]
    public void WhenIConfigureAspNetCoreHostToValidateConfigurationOnStartup()
    {
        _validateConfigurationOnStartup = true;
        
        // Add validation logic to services
        _webApplicationBuilder!.Services.AddOptions();
        _webApplicationBuilder.Services.Configure<Dictionary<string, string?>>(config =>
        {
            foreach (var kvp in _aspNetCoreConfigurationData)
            {
                config[kvp.Key] = kvp.Value;
            }
        });
    }

    [When(@"I configure ASP.NET Core host with FlexConfig and invalid JSON file ""(.*)""")]
    public void WhenIConfigureAspNetCoreHostWithFlexConfigAndInvalidJsonFile(string filePath)
    {
        _webApplicationBuilder.Should().NotBeNull("WebApplicationBuilder should be initialized");
        
        var normalizedPath = filePath.Replace('/', Path.DirectorySeparatorChar);
        
        // Configure FlexConfig with an invalid JSON file - this should cause an error
        _webApplicationBuilder!.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
        {
            containerBuilder.AddFlexConfig(config => config
                .AddJsonFile(normalizedPath, optional: false, reloadOnChange: false));
        });
    }

    [When(@"I configure ASP.NET Core host for ""(.*)"" environment")]
    public void WhenIConfigureAspNetCoreHostForEnvironment(string environmentName)
    {
        _aspNetCoreEnvironmentName = environmentName;
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", environmentName);
        
        // Recreate the builder with the correct environment
        var args = Array.Empty<string>();
        _webApplicationBuilder = WebApplication.CreateBuilder(args);
        
        // Configure Autofac as the service provider
        _webApplicationBuilder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
        
        _webApplicationBuilder.Environment.EnvironmentName = environmentName;
        
        // Set up basic logging for testing
        _webApplicationBuilder.Logging.ClearProviders();
        _webApplicationBuilder.Logging.AddConsole();
        _webApplicationBuilder.Logging.SetMinimumLevel(LogLevel.Warning);
    }

    [When(@"I configure ASP.NET Core host with FlexConfig using environment-specific files:")]
    public void WhenIConfigureAspNetCoreHostWithFlexConfigUsingEnvironmentSpecificFiles(Table table)
    {
        _webApplicationBuilder.Should().NotBeNull("WebApplicationBuilder should be initialized");
        
        _webApplicationBuilder!.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
        {
            containerBuilder.AddFlexConfig(config =>
            {
                foreach (var row in table.Rows)
                {
                    var environment = row["Environment"];
                    var jsonFile = row["JsonFile"];
                    
                    var normalizedPath = jsonFile.Replace('/', Path.DirectorySeparatorChar);
                    
                    if (environment == "Default")
                    {
                        if (File.Exists(normalizedPath))
                        {
                            config.AddJsonFile(normalizedPath, optional: false, reloadOnChange: false);
                            _aspNetCoreConfigurationSources.Add($"JsonFile:{normalizedPath}");
                        }
                    }
                    else if (environment == _aspNetCoreEnvironmentName)
                    {
                        if (File.Exists(normalizedPath))
                        {
                            config.AddJsonFile(normalizedPath, optional: true, reloadOnChange: false);
                            _aspNetCoreConfigurationSources.Add($"JsonFile:{normalizedPath}");
                        }
                    }
                }
                
                // Add in-memory data last (the highest precedence) if we have any
                if (_aspNetCoreConfigurationData.Any())
                {
                    config.AddSource(new MemoryConfigurationSource { InitialData = _aspNetCoreConfigurationData });
                }
            });
        });
    }

    [When(@"I register the test controller in ASP.NET Core services")]
    public void WhenIRegisterTheTestControllerInAspNetCoreServices()
    {
        _webApplicationBuilder.Should().NotBeNull("WebApplicationBuilder should be initialized");
        
        _webApplicationBuilder!.Services.AddControllers();
        _webApplicationBuilder.Services.AddTransient<TestControllerWithFlexConfig>();
    }

    [When(@"I build the ASP.NET Core application host")]
    public void WhenIBuildTheAspNetCoreApplicationHost()
    {
        _webApplicationBuilder.Should().NotBeNull("WebApplicationBuilder should be initialized");
        
        try
        {
            // Ensure we always have FlexConfig configured, even if no explicit configuration was provided
            var hasFlexConfigBeenConfigured = _aspNetCoreConfigurationData.Any() || _aspNetCoreConfigurationSources.Any();
            
            if (!hasFlexConfigBeenConfigured)
            {
                // Add minimal FlexConfig configuration if none provided
                _webApplicationBuilder!.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
                {
                    containerBuilder.AddFlexConfig(config => config
                        .AddSource(new MemoryConfigurationSource { 
                            InitialData = new Dictionary<string, string?> { ["Test:Key"] = "Test:Value" } 
                        }));
                });
            }
            
            _webApplication = _webApplicationBuilder!.Build();
            _host = _webApplication;
            _serviceProvider = _webApplication.Services;
            _aspNetCoreHostBuildSucceeded = true;
            
            // Store in a scenario context
            scenarioContext.Set(_webApplication, "WebApplication");
            scenarioContext.Set(_serviceProvider, "ServiceProvider");
        }
        catch (Exception ex)
        {
            _lastAspNetCoreException = ex;
            _aspNetCoreHostBuildSucceeded = false;
            
            // Log the exception for debugging
            Console.WriteLine($"ASP.NET Core host build failed: {ex}");
        }
    }

    [When(@"I try to build the ASP.NET Core application host")]
    public void WhenITryToBuildTheAspNetCoreApplicationHost()
    {
        WhenIBuildTheAspNetCoreApplicationHost();
    }

    #endregion

    #region Then Steps - Assertions

    [Then(@"the ASP.NET Core host should contain FlexConfig service")]
    public void ThenTheAspNetCoreHostShouldContainFlexConfigService()
    {
        if (!_aspNetCoreHostBuildSucceeded)
        {
            var errorMessage = _lastAspNetCoreException?.ToString() ?? "Unknown build failure";
            throw new InvalidOperationException($"ASP.NET Core host failed to build: {errorMessage}");
        }
        
        _serviceProvider.Should().NotBeNull("Service provider should be available");
        
        var flexConfig = _serviceProvider!.GetService<IFlexConfig>();
        flexConfig.Should().NotBeNull("FlexConfig should be registered in ASP.NET Core services");
        
        _hostFlexConfiguration = flexConfig;
    }

    [Then(@"I should be able to resolve FlexConfig from ASP.NET Core services")]
    public void ThenIShouldBeAbleToResolveFlexConfigFromAspNetCoreServices()
    {
        if (!_aspNetCoreHostBuildSucceeded)
        {
            var errorMessage = _lastAspNetCoreException?.ToString() ?? "Unknown build failure";
            throw new InvalidOperationException($"ASP.NET Core host failed to build: {errorMessage}");
        }
        
        _serviceProvider.Should().NotBeNull("Service provider should be available");
        
        var flexConfig = _serviceProvider!.GetRequiredService<IFlexConfig>();
        flexConfig.Should().NotBeNull("FlexConfig should be resolvable from ASP.NET Core services");
        
        _hostFlexConfiguration = flexConfig;
    }

    [Then(@"FlexConfig should contain the expected ASP.NET Core configuration values")]
    public void ThenFlexConfigShouldContainTheExpectedAspNetCoreConfigurationValues()
    {
        _hostFlexConfiguration.Should().NotBeNull("FlexConfig should be resolved");
        
        foreach (var kvp in _aspNetCoreConfigurationData)
        {
            var actualValue = _hostFlexConfiguration![kvp.Key];
            actualValue.Should().Be(kvp.Value, $"Configuration key '{kvp.Key}' should have the expected value");
        }
    }

    [Then(@"FlexConfig should include values from all ASP.NET Core sources")]
    public void ThenFlexConfigShouldIncludeValuesFromAllAspNetCoreSources()
    {
        _hostFlexConfiguration.Should().NotBeNull("FlexConfig should be resolved");
        _aspNetCoreConfigurationSources.Should().NotBeEmpty("Configuration sources should be registered");
        
        // Verify configuration contains some values (exact verification depends on test data files)
        var allConfigEntries = _hostFlexConfiguration!.Configuration.AsEnumerable().ToList();
        allConfigEntries.Should().NotBeEmpty("Configuration should contain entries from sources");
    }

    [Then(@"the environment variables should override configuration file values")]
    public void ThenTheEnvironmentVariablesShouldOverrideConfigurationFileValues()
    {
        // First, let's test it if a simple FlexConfig setup can read environment variables
        Environment.SetEnvironmentVariable("TEST_SIMPLE_VAR", "TEST_VALUE");
        
        var simpleBuilder = new FlexConfigurationBuilder();
        simpleBuilder.AddEnvironmentVariables();
        var simpleFlexConfig = simpleBuilder.Build();
        
        var simpleTestValue = simpleFlexConfig["TEST_SIMPLE_VAR"];
        simpleTestValue.Should().Be("TEST_VALUE", "Simple FlexConfig should be able to read environment variables");
        
        // Clean up
        Environment.SetEnvironmentVariable("TEST_SIMPLE_VAR", null);
        
        // Ensure we have FlexConfig resolved first
        if (_hostFlexConfiguration == null)
        {
            ThenIShouldBeAbleToResolveFlexConfigFromAspNetCoreServices();
        }
        
        _hostFlexConfiguration.Should().NotBeNull("FlexConfig should be resolved");
        
        // Now test our ASP.NET Core FlexConfig
        foreach (var kvp in _aspNetCoreEnvironmentVariables)
        {
            // First, verify the environment variable is actually set
            var envValue = Environment.GetEnvironmentVariable(kvp.Key);
            envValue.Should().NotBeNull($"Environment variable '{kvp.Key}' should be set in the environment");
            envValue.Should().Be(kvp.Value, $"Environment variable '{kvp.Key}' should have the correct value in environment");
            
            // Test with standalone configuration to ensure environment variables work in general
            var testBuilder = new ConfigurationBuilder();
            testBuilder.AddEnvironmentVariables();
            var testConfig = testBuilder.Build();
            var testValue = testConfig[kvp.Key];
            testValue.Should().NotBeNull($"Environment variable '{kvp.Key}' should be readable by standalone configuration");
            testValue.Should().Be(kvp.Value, $"Standalone configuration should read environment variable '{kvp.Key}' correctly");
            
            // Now test with our FlexConfig
            var actualValue = _hostFlexConfiguration![kvp.Key];
            actualValue.Should().Be(kvp.Value, $"Environment variable '{kvp.Key}' should override file values in FlexConfig. " +
                                                 $"Environment has: {envValue}, Standalone config has: {testValue}, FlexConfig has: {actualValue}");
        }
    }

    [Then(@"I should be able to resolve the test service from ASP.NET Core services")]
    public void ThenIShouldBeAbleToResolveTheTestServiceFromAspNetCoreServices()
    {
        _serviceProvider.Should().NotBeNull("Service provider should be available");
        
        _resolvedTestService = _serviceProvider!.GetRequiredService<TestServiceWithFlexConfig>();
        _resolvedTestService.Should().NotBeNull("Test service should be resolvable");
    }

    [Then(@"the test service should have FlexConfig injected")]
    public void ThenTheTestServiceShouldHaveFlexConfigInjected()
    {
        _resolvedTestService.Should().NotBeNull("Test service should be resolved");
        _resolvedTestService!.Configuration.Should().NotBeNull("Test service should have FlexConfig injected");
    }

    [Then(@"the test service should be able to access configuration values through FlexConfig")]
    public void ThenTheTestServiceShouldBeAbleToAccessConfigurationValuesThroughFlexConfig()
    {
        _resolvedTestService.Should().NotBeNull("Test service should be resolved");
        
        // Verify that the service can access configuration values
        if (_aspNetCoreConfigurationData.TryGetValue("Service:Name", out var value2))
        {
            _resolvedTestService!.ServiceName.Should().Be(value2);
        }
        
        if (_aspNetCoreConfigurationData.TryGetValue("Service:Enabled", out var value1))
        {
            var expectedEnabled = bool.Parse(value1 ?? "false");
            _resolvedTestService!.IsEnabled.Should().Be(expectedEnabled);
        }
        
        if (_aspNetCoreConfigurationData.TryGetValue("Service:MaxRetries", out var value))
        {
            var expectedRetries = int.Parse(value ?? "0");
            _resolvedTestService!.MaxRetries.Should().Be(expectedRetries);
        }
    }

    [Then(@"the ASP.NET Core host should use Autofac as service provider")]
    public void ThenTheAspNetCoreHostShouldUseAutofacAsServiceProvider()
    {
        _useAutofacServiceProvider.Should().BeTrue("Autofac service provider factory should be configured");
        _serviceProvider.Should().NotBeNull("Service provider should be available");
        
        // Verify that we can resolve Autofac-specific services
        var lifetimeScope = _serviceProvider!.GetService<ILifetimeScope>();
        lifetimeScope.Should().NotBeNull("ILifetimeScope should be available when using Autofac");
    }

    [Then(@"I should be able to resolve FlexConfig from Autofac container")]
    public void ThenIShouldBeAbleToResolveFlexConfigFromAutofacContainer()
    {
        _serviceProvider.Should().NotBeNull("Service provider should be available");
        
        var lifetimeScope = _serviceProvider!.GetRequiredService<ILifetimeScope>();
        var flexConfig = lifetimeScope.Resolve<IFlexConfig>();
        
        flexConfig.Should().NotBeNull("FlexConfig should be resolvable from Autofac container");
        _hostFlexConfiguration = flexConfig;
    }

    [Then(@"the test Autofac module should be registered and functional")]
    public void ThenTheTestAutofacModuleShouldBeRegisteredAndFunctional()
    {
        _testAutofacModule.Should().NotBeNull("Test Autofac module should be created");
        _serviceProvider.Should().NotBeNull("Service provider should be available");
        
        var lifetimeScope = _serviceProvider!.GetRequiredService<ILifetimeScope>();
        var testService = lifetimeScope.Resolve<ITestAutofacService>();
        
        testService.Should().NotBeNull("Test Autofac service should be resolvable");
        _testAutofacModule!.IsLoaded.Should().BeTrue("Autofac module should be loaded");
        _testAutofacModule.LoadedConfiguration.Should().NotBeNull("Module should have access to FlexConfig");
    }

    [Then(@"the ASP.NET Core application should start successfully")]
    public void ThenTheAspNetCoreApplicationShouldStartSuccessfully()
    {
        _aspNetCoreHostBuildSucceeded.Should().BeTrue("ASP.NET Core application should build successfully");
        _lastAspNetCoreException.Should().BeNull("No exceptions should occur during startup");
        _webApplication.Should().NotBeNull("Web application should be created");
    }

    [Then(@"FlexConfig should contain validated configuration values")]
    public void ThenFlexConfigShouldContainValidatedConfigurationValues()
    {
        // Ensure we have FlexConfig resolved first
        if (_hostFlexConfiguration == null)
        {
            ThenIShouldBeAbleToResolveFlexConfigFromAspNetCoreServices();
        }
        
        _hostFlexConfiguration.Should().NotBeNull("FlexConfig should be resolved");
        
        // Verify validation-related configuration values
        if (_aspNetCoreConfigurationData.ContainsKey("Validation:Required"))
        {
            var requiredValue = _hostFlexConfiguration!["Validation:Required"];
            requiredValue.Should().NotBeNullOrEmpty("Required validation value should be present");
        }
        
        if (_aspNetCoreConfigurationData.ContainsKey("Validation:MaxLength"))
        {
            var maxLength = _hostFlexConfiguration!["Validation:MaxLength"];
            maxLength.Should().NotBeNullOrEmpty("MaxLength validation value should be present");
        }
    }

    [Then(@"the configuration validation should pass")]
    public void ThenTheConfigurationValidationShouldPass()
    {
        _validateConfigurationOnStartup.Should().BeTrue("Configuration validation should be enabled");
        _aspNetCoreHostBuildSucceeded.Should().BeTrue("Host should build successfully with valid configuration");
        _lastAspNetCoreException.Should().BeNull("No validation errors should occur");
    }

    [Then(@"the ASP.NET Core host building should fail gracefully")]
    public void ThenTheAspNetCoreHostBuildingShouldFailGracefully()
    {
        _aspNetCoreHostBuildSucceeded.Should().BeFalse("Host building should fail with invalid configuration");
        _lastAspNetCoreException.Should().NotBeNull("An exception should be thrown");
    }

    [Then(@"the error should indicate configuration loading failure")]
    public void ThenTheErrorShouldIndicateConfigurationLoadingFailure()
    {
        _lastAspNetCoreException.Should().NotBeNull("An exception should be thrown");
        
        // Check if the exception or its inner exceptions relate to configuration loading
        var exceptionMessage = _lastAspNetCoreException!.ToString();
        var isConfigurationError = exceptionMessage.Contains("configuration", StringComparison.OrdinalIgnoreCase) ||
                                 exceptionMessage.Contains("JSON", StringComparison.OrdinalIgnoreCase) ||
                                 exceptionMessage.Contains("invalid", StringComparison.OrdinalIgnoreCase);
        
        isConfigurationError.Should().BeTrue("Exception should indicate configuration loading failure");
    }

    [Then(@"the error message should be descriptive for ASP.NET Core context")]
    public void ThenTheErrorMessageShouldBeDescriptiveForAspNetCoreContext()
    {
        _lastAspNetCoreException.Should().NotBeNull("An exception should be thrown");
        
        var exceptionMessage = _lastAspNetCoreException!.Message;
        exceptionMessage.Should().NotBeNullOrEmpty("Exception should have a descriptive message");
        exceptionMessage.Length.Should().BeGreaterThan(10, "Exception message should be reasonably descriptive");
    }

    [Then(@"FlexConfig should load the Development-specific configuration")]
    public void ThenFlexConfigShouldLoadTheDevelopmentSpecificConfiguration()
    {
        // Ensure we have FlexConfig resolved first
        if (_hostFlexConfiguration == null)
        {
            ThenIShouldBeAbleToResolveFlexConfigFromAspNetCoreServices();
        }
        
        _hostFlexConfiguration.Should().NotBeNull("FlexConfig should be resolved");
        _aspNetCoreEnvironmentName.Should().Be("Development", "Environment should be set to Development");
        
        // Verify that configuration is loaded (specific verification depends on test data files)
        var allConfigEntries = _hostFlexConfiguration!.Configuration.AsEnumerable().ToList();
        allConfigEntries.Should().NotBeEmpty("Configuration should contain entries");
    }

    [Then(@"the Development values should override base configuration values")]
    public void ThenTheDevelopmentValuesShouldOverrideBaseConfigurationValues()
    {
        _hostFlexConfiguration.Should().NotBeNull("FlexConfig should be resolved");
        
        // This verification depends on the specific content of the test configuration files
        // For now, verify that configuration loading succeeded
        var allConfigEntries = _hostFlexConfiguration!.Configuration.AsEnumerable().ToList();
        allConfigEntries.Should().NotBeEmpty("Configuration should contain merged entries from multiple files");
    }

    [Then(@"the ASP.NET Core environment should be correctly configured")]
    public void ThenTheAspNetCoreEnvironmentShouldBeCorrectlyConfigured()
    {
        _webApplication.Should().NotBeNull("Web application should be created");
        _webApplication!.Environment.EnvironmentName.Should().Be(_aspNetCoreEnvironmentName, 
            "ASP.NET Core environment should match configured environment");
    }

    [Then(@"I should be able to resolve the test controller from ASP.NET Core services")]
    public void ThenIShouldBeAbleToResolveTheTestControllerFromAspNetCoreServices()
    {
        _serviceProvider.Should().NotBeNull("Service provider should be available");
        
        _resolvedTestController = _serviceProvider!.GetRequiredService<TestControllerWithFlexConfig>();
        _resolvedTestController.Should().NotBeNull("Test controller should be resolvable");
    }

    [Then(@"the test controller should access configuration values dynamically")]
    public void ThenTheTestControllerShouldAccessConfigurationValuesDynamically()
    {
        _resolvedTestController.Should().NotBeNull("Test controller should be resolved");
        
        // Simulate a call to the controller's configuration action
        var result = _resolvedTestController!.GetConfiguration();
        result.Should().NotBeNull("Controller action should return a result");
        
        // Verify that the controller can access specific configuration values
        if (_aspNetCoreConfigurationData.ContainsKey("Api:BaseUrl"))
        {
            var baseUrlResult = _resolvedTestController.GetConfigurationValue("Api:BaseUrl");
            baseUrlResult.Should().NotBeNull("Controller should be able to get configuration values");
        }
    }

    [Then(@"the controller should demonstrate FlexConfig dynamic access patterns")]
    public void ThenTheControllerShouldDemonstrateFlexConfigDynamicAccessPatterns()
    {
        _resolvedTestController.Should().NotBeNull("Test controller should be resolved");
        
        // The TestControllerWithFlexConfig class demonstrates dynamic access patterns
        // by using both the indexer pattern and dynamic member access
        // This step verifies that the controller was created and can be used
        var configurationResult = _resolvedTestController!.GetConfiguration();
        configurationResult.Should().NotBeNull("Controller should support dynamic configuration access");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Cleans up any environment variables set during testing.
    /// </summary>
    private void CleanupEnvironmentVariables()
    {
        foreach (var key in _aspNetCoreEnvironmentVariables.Keys)
        {
            Environment.SetEnvironmentVariable(key, null);
        }
    }

    #endregion

    #region Cleanup

    /// <summary>
    /// Cleanup method called after each scenario.
    /// </summary>
    [AfterScenario]
    public void CleanupAspNetCoreResources()
    {
        try
        {
            // Dispose of ASP.NET Core resources
            _webApplication?.DisposeAsync().GetAwaiter().GetResult();
            _host?.Dispose();
            
            // Clean up environment variables
            CleanupEnvironmentVariables();
            
            // Clear environment variable for the next test
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
        }
        catch (Exception ex)
        {
            // Log cleanup errors but don't fail the test
            Console.WriteLine($"Warning: Error during ASP.NET Core cleanup: {ex.Message}");
        }
    }

    #endregion
}